using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows;
using System.Windows.Documents;
using U8_Library;

namespace U8_Klient_Server
{
    internal class Klient
    {
        private readonly string _ip;
        private readonly int _port;
        private Socket? _clientSocket;
        private readonly MainWindow _mainWindow;
        public Klient(string serverIp, int serverPort, MainWindow mainWindow)
        {
            _ip = serverIp;
            _port = serverPort;
            _mainWindow = mainWindow;
        }

        public void Start()
        {
            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(_ip), _port);

            try
            {
                _clientSocket.Connect(serverEndPoint);
                // Start a separate thread for receiving messages from the server
                Thread receiveThread = new Thread(Listen);
                receiveThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void Stop()
        {
            _clientSocket?.Close();
        }

        public void SendMessage(string name, string text)
        {
            if (text == "") { return; }
            try
            {
                Message newMessage = new Message(text, DateTime.Now, name);
                byte[] messageBytes = Encoding.ASCII.GetBytes(newMessage.ToJsonString());
                if (_clientSocket != null) {
                    _clientSocket.Send(messageBytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void Listen()
        {
            if (_clientSocket == null)
            {
                return;
            }
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[96000];
                    int bytesRead = _clientSocket.Receive(buffer);

                    if (bytesRead > 0)
                    {
                        string jsonString = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                        string pattern = @"\{.*?\}";
                        Regex regex = new Regex(pattern);
                        MatchCollection matches = regex.Matches(jsonString);
                        foreach (Match match in matches)
                        {
                            Message? receivedMessage = JsonSerializer.Deserialize<Message>(match.Value);

                            // Process the received message
                            _mainWindow.Dispatcher.Invoke(new Action(() =>
                            {
                                if (receivedMessage != null)
                                {
                                    _mainWindow.OutputBox.Text += receivedMessage.ToString() + "\n";
                                }
                            }));
                        }
                    }
                }
                catch
                {
                    _clientSocket = null;
                    break;
                }
            }
        }
    }
}
