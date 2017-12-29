using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace BugShooting.Output.InformUp
{
  partial class Send : Window
  {
 
    public Send(string url, string lastItemType, int lastItemID, string fileName)
    {
      InitializeComponent();
      
      Url.Text = url;
      NewItem.IsChecked = true;
      ItemTypeComboBox.SelectedValue = lastItemType;
      ItemIDTextBox.Text = lastItemID.ToString();
      FileNameTextBox.Text = fileName;

      ItemTypeComboBox.SelectionChanged += ValidateData;
      TitleTextBox.TextChanged += ValidateData;
      DescriptionTextBox.TextChanged += ValidateData;
      ItemIDTextBox.TextChanged += ValidateData;
      FileNameTextBox.TextChanged += ValidateData;
      ValidateData(null, null);

    }

    public bool CreateNewItem
    {
      get { return NewItem.IsChecked.Value; }
    }

    public string ItemType
    {
      get { return (string)ItemTypeComboBox.SelectedValue; }
    }

    public string ItemTitle
    {
      get { return TitleTextBox.Text; }
    }

    public string Description
    {
      get { return DescriptionTextBox.Text; }
    }
    
    public int ItemID
    {
      get { return Convert.ToInt32(ItemIDTextBox.Text); }
    }

    public string FileName
    {
      get { return FileNameTextBox.Text; }
    }
    
    private void NewItem_CheckedChanged(object sender, EventArgs e)
    {

      if (NewItem.IsChecked.Value)
      {
        ItemTypeControls.Visibility = Visibility.Visible;
        TitleControls.Visibility = Visibility.Visible;
        DescriptionControls.Visibility = Visibility.Visible;
        ItemIDControls.Visibility = Visibility.Collapsed;

        TitleTextBox.SelectAll();
        TitleTextBox.Focus();
      }
      else
      {
        ItemTypeControls.Visibility = Visibility.Collapsed;
        TitleControls.Visibility = Visibility.Collapsed;
        DescriptionControls.Visibility = Visibility.Collapsed;
        ItemIDControls.Visibility = Visibility.Visible;
        
        ItemIDTextBox.SelectAll();
        ItemIDTextBox.Focus();
      }

      ValidateData(null, null);

    }

    private void ItemID_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
    }
    
    private void ValidateData(object sender, EventArgs e)
    {
      OK.IsEnabled = ((CreateNewItem && Validation.IsValid(ItemTypeComboBox) && Validation.IsValid(TitleTextBox) && Validation.IsValid(DescriptionTextBox)) ||
                      (!CreateNewItem && Validation.IsValid(ItemIDTextBox))) &&
                     Validation.IsValid(FileNameTextBox);
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
      this.DialogResult = true;
    }

  }

}
