using BS.Plugin.V3.Output;
using System;

namespace BugShooting.Output.InformUp
{

  public class Output: IOutput 
  {
    
    string name;
    string url;
    string userName;
    string password;
    string fileName;
    Guid fileFormatID;
    bool openItemInBrowser;
    string lastItemType;
    int lastItemID;

    public Output(string name, 
                  string url, 
                  string userName,
                  string password, 
                  string fileName,
                  Guid fileFormatID,
                  bool openItemInBrowser, 
                  string lastItemType, 
                  int lastItemID)
    {
      this.name = name;
      this.url = url;
      this.userName = userName;
      this.password = password;
      this.fileName = fileName;
      this.fileFormatID = fileFormatID;
      this.openItemInBrowser = openItemInBrowser;
      this.lastItemType = lastItemType;
      this.lastItemID = lastItemID;
    }
    
    public string Name
    {
      get { return name; }
    }

    public string Information
    {
      get { return url; }
    }

    public string Url
    {
      get { return url; }
    }
       
    public string UserName
    {
      get { return userName; }
    }

    public string Password
    {
      get { return password; }
    }
          
    public string FileName
    {
      get { return fileName; }
    }

    public Guid FileFormatID
    {
      get { return fileFormatID; }
    }

    public bool OpenItemInBrowser
    {
      get { return openItemInBrowser; }
    }
    
    public string LastItemType
    {
      get { return lastItemType; }
    }

    public int LastItemID
    {
      get { return lastItemID; }
    }

  }
}
