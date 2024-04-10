using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace U8_Klient_Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        Klient klient;
        public MainWindow()
        {
            InitializeComponent();
            klient = new Klient("127.0.0.1", 55557, this);
            klient.Start();
        }

        private void SendButtonClicked(object sender, RoutedEventArgs e)
        {
            klient.SendMessage(NameInput.Text, MessageInput.Text);
            MessageInput.Text = "";
        }

        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                klient.SendMessage(NameInput.Text, MessageInput.Text);
                MessageInput.Text = "";
            }
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            klient.Stop();
        }
    }
}
