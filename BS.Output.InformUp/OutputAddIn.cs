using BS.Output.InformUp.InformUp.WorkItem;
using System;
using System.Drawing;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BS.Output.InformUp
{
  public class OutputAddIn: V3.OutputAddIn<Output>
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

    protected override OutputValueCollection SerializeOutput(Output Output)
    {

      OutputValueCollection outputValues = new OutputValueCollection();

      outputValues.Add(new OutputValue("Name", Output.Name));
      outputValues.Add(new OutputValue("Url", Output.Url));
      outputValues.Add(new OutputValue("UserName", Output.UserName));
      outputValues.Add(new OutputValue("Password",Output.Password, true));
      outputValues.Add(new OutputValue("OpenItemInBrowser", Convert.ToString(Output.OpenItemInBrowser)));
      outputValues.Add(new OutputValue("FileName", Output.FileName));
      outputValues.Add(new OutputValue("FileFormat", Output.FileFormat));
      outputValues.Add(new OutputValue("LastItemType", Output.LastItemType));
      outputValues.Add(new OutputValue("LastItemID", Output.LastItemID.ToString()));

      return outputValues;
      
    }

    protected override Output DeserializeOutput(OutputValueCollection OutputValues)
    {

      return new Output(OutputValues["Name", this.Name].Value,
                        OutputValues["Url", ""].Value, 
                        OutputValues["UserName", ""].Value,
                        OutputValues["Password", ""].Value, 
                        OutputValues["FileName", "Screenshot"].Value, 
                        OutputValues["FileFormat", ""].Value,
                        Convert.ToBoolean(OutputValues["OpenItemInBrowser", Convert.ToString(true)].Value),
                        OutputValues["LastItemType", string.Empty].Value,
                        Convert.ToInt32(OutputValues["LastItemID", "1"].Value));

    }

    protected override async Task<V3.SendResult> Send(IWin32Window Owner, Output Output, V3.ImageData ImageData)
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


        string fileName = V3.FileHelper.GetFileName(Output.FileName, Output.FileFormat, ImageData);


        // Show send window
        Send send = new Send(Output.Url, Output.LastItemType, Output.LastItemID, fileName);

        var sendOwnerHelper = new System.Windows.Interop.WindowInteropHelper(send);
        sendOwnerHelper.Owner = Owner.Handle;

        if (!send.ShowDialog() == true)
        {
          return new V3.SendResult(V3.Result.Canceled);
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
              return new V3.SendResult(V3.Result.Canceled);
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
                return new V3.SendResult(V3.Result.Failed, addWorkItemResult.Body.AddWorkItemResult);
              }
              
            }
                         
          }
          else
          {
            itemType = Output.LastItemType;
            itemID = send.ItemID;
          }

          string fullFileName = String.Format("{0}.{1}", send.FileName, V3.FileHelper.GetFileExtention(Output.FileFormat));
          byte[] fileBytes = V3.FileHelper.GetFileBytes(Output.FileFormat, ImageData);

          UploadFileResponse uploadFileResult = await informUp.UploadFileAsync(userName, password, fileBytes, fullFileName, itemID);
          
          if (!uploadFileResult.Body.UploadFileResult.Equals("OK", StringComparison.InvariantCultureIgnoreCase))
          {
            return new V3.SendResult(V3.Result.Failed, uploadFileResult.ToString());
          }
          

          // Open item in browser
          if (Output.OpenItemInBrowser)
          {
            V3.WebHelper.OpenUrl(String.Format("{0}/Main.aspx?ID={1}&Window=PopupItem", Output.Url, itemID));
          }
          
          return new V3.SendResult(V3.Result.Success,
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
        return new V3.SendResult(V3.Result.Failed, ex.Message);
      }

    }

  }
}
