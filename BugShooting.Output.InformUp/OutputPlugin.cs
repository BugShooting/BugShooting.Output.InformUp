using BugShooting.Output.InformUp.InformUp.WorkItem;
using System;
using System.Drawing;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using BS.Plugin.V3.Output;
using BS.Plugin.V3.Common;
using BS.Plugin.V3.Utilities;

namespace BugShooting.Output.InformUp
{
  public class OutputPlugin: OutputPlugin<Output>
  {

    protected override string Name
    {
      get { return "informUp"; }
    }

    protected override Image Image64
    {
      get  { return Properties.Resources.logo_64; }
    }

    protected override Image Image16
    {
      get { return Properties.Resources.logo_16 ; }
    }

    protected override bool Editable
    {
      get { return true; }
    }

    protected override string Description
    {
      get { return "Attach screenshots to informUp items."; }
    }
    
    protected override Output CreateOutput(IWin32Window Owner)
    {
      
      Output output = new Output(Name, 
                                 String.Empty, 
                                 String.Empty, 
                                 String.Empty, 
                                 "Screenshot",
                                 String.Empty, 
                                 true,
                                 String.Empty,
                                 1);

      return EditOutput(Owner, output);

    }

    protected override Output EditOutput(IWin32Window Owner, Output Output)
    {

      Edit edit = new Edit(Output);

      var ownerHelper = new System.Windows.Interop.WindowInteropHelper(edit);
      ownerHelper.Owner = Owner.Handle;
      
      if (edit.ShowDialog() == true) {

        return new Output(edit.OutputName,
                          edit.Url,
                          edit.UserName,
                          edit.Password,
                          edit.FileName,
                          edit.FileFormat,
                          edit.OpenItemInBrowser,
                          Output.LastItemType,
                          Output.LastItemID);
      }
      else
      {
        return null; 
      }

    }

    protected override OutputValues SerializeOutput(Output Output)
    {

      OutputValues outputValues = new OutputValues();

      outputValues.Add("Name", Output.Name);
      outputValues.Add("Url", Output.Url);
      outputValues.Add("UserName", Output.UserName);
      outputValues.Add("Password",Output.Password, true);
      outputValues.Add("OpenItemInBrowser", Convert.ToString(Output.OpenItemInBrowser));
      outputValues.Add("FileName", Output.FileName);
      outputValues.Add("FileFormat", Output.FileFormat);
      outputValues.Add("LastItemType", Output.LastItemType);
      outputValues.Add("LastItemID", Output.LastItemID.ToString());

      return outputValues;
      
    }

    protected override Output DeserializeOutput(OutputValues OutputValues)
    {

      return new Output(OutputValues["Name", this.Name],
                        OutputValues["Url", ""], 
                        OutputValues["UserName", ""],
                        OutputValues["Password", ""], 
                        OutputValues["FileName", "Screenshot"], 
                        OutputValues["FileFormat", ""],
                        Convert.ToBoolean(OutputValues["OpenItemInBrowser", Convert.ToString(true)]),
                        OutputValues["LastItemType", string.Empty],
                        Convert.ToInt32(OutputValues["LastItemID", "1"]));

    }

    protected override async Task<SendResult> Send(IWin32Window Owner, Output Output, ImageData ImageData)
    {

      try
      {

        HttpBindingBase binding;
        if (Output.Url.StartsWith("https", StringComparison.InvariantCultureIgnoreCase))
        {
          binding = new BasicHttpsBinding();
        }
        else
        {
          binding = new BasicHttpBinding();
        }
        binding.MaxBufferSize = int.MaxValue;
        binding.MaxReceivedMessageSize = int.MaxValue;

        WorkItemSoapClient informUp = new WorkItemSoapClient(binding, new EndpointAddress(Output.Url + "/Services/WorkItem.asmx"));


        string fileName = AttributeHelper.ReplaceAttributes(Output.FileName, ImageData);


        // Show send window
        Send send = new Send(Output.Url, Output.LastItemType, Output.LastItemID, fileName);

        var sendOwnerHelper = new System.Windows.Interop.WindowInteropHelper(send);
        sendOwnerHelper.Owner = Owner.Handle;

        if (!send.ShowDialog() == true)
        {
          return new SendResult(Result.Canceled);
        }


        string userName = Output.UserName;
        string password = Output.Password;
        bool showLogin = string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password);
        bool rememberCredentials = false;
        
        while (true)
        {

          if (showLogin)
          {

            // Show credentials window
            Credentials credentials = new Credentials(Output.Url, userName, password, rememberCredentials);

            var credentialsOwnerHelper = new System.Windows.Interop.WindowInteropHelper(credentials);
            credentialsOwnerHelper.Owner = Owner.Handle;

            if (credentials.ShowDialog() != true)
            {
              return new SendResult(Result.Canceled);
            }

            userName = credentials.UserName;
            password = credentials.Password;
            rememberCredentials = credentials.Remember;

          }

          string itemType = null;
          int itemID;

          if (send.CreateNewItem)
          {

            itemType = send.ItemType;

            AddWorkItemResponse addWorkItemResult = await informUp.AddWorkItemAsync(userName, password, send.ItemType, send.ItemTitle, send.Description);

            if (!int.TryParse(addWorkItemResult.Body.AddWorkItemResult, out itemID))
            {

              if (addWorkItemResult.Body.AddWorkItemResult.Equals("Login or Password is incorrect", StringComparison.InvariantCultureIgnoreCase))
              {
                showLogin = true;
                continue;
              }
              else
              {
                return new SendResult(Result.Failed, addWorkItemResult.Body.AddWorkItemResult);
              }
              
            }
                         
          }
          else
          {
            itemType = Output.LastItemType;
            itemID = send.ItemID;
          }

          string fullFileName = String.Format("{0}.{1}", send.FileName, FileHelper.GetFileExtension(Output.FileFormat));
          byte[] fileBytes = FileHelper.GetFileBytes(Output.FileFormat, ImageData);

          UploadFileResponse uploadFileResult = await informUp.UploadFileAsync(userName, password, fileBytes, fullFileName, itemID);
          
          if (!uploadFileResult.Body.UploadFileResult.Equals("OK", StringComparison.InvariantCultureIgnoreCase))
          {
            return new SendResult(Result.Failed, uploadFileResult.ToString());
          }
          

          // Open item in browser
          if (Output.OpenItemInBrowser)
          {
            WebHelper.OpenUrl(String.Format("{0}/Main.aspx?ID={1}&Window=PopupItem", Output.Url, itemID));
          }
          
          return new SendResult(Result.Success,
                                new Output(Output.Name,
                                           Output.Url,
                                           (rememberCredentials) ? userName : Output.UserName,
                                           (rememberCredentials) ? password : Output.Password,
                                           Output.FileName,
                                           Output.FileFormat,
                                           Output.OpenItemInBrowser,
                                           itemType,
                                           itemID));

        }

      }
      catch (Exception ex)
      {
        return new SendResult(Result.Failed, ex.Message);
      }

    }

  }
}
