using System.Windows;

namespace NetKit;

public partial class CustomMessageBox : Window
{
    public CustomMessageBox(string message, string title = "NetKit")
    {
        InitializeComponent();
        MessageText.Text = message;
        Title = title;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    public static void Show(string message, string title = "NetKit")
    {
        var messageBox = new CustomMessageBox(message, title);
        messageBox.ShowDialog();
    }

    public static void Show(Window owner, string message, string title = "NetKit")
    {
        var messageBox = new CustomMessageBox(message, title)
        {
            Owner = owner
        };
        messageBox.ShowDialog();
    }
}