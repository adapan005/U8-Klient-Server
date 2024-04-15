using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using U8_Library;
using static System.Net.Mime.MediaTypeNames;

namespace TestClient
{
    class TestClient
    {
        private readonly string _ip;
        private readonly int _port;
        private Socket? _clientSocket;

        public TestClient(string serverIp, int serverPort)
        {
            _ip = serverIp;
            _port = serverPort;
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
                Console.WriteLine(ex.Message);
            }
        }

        public void Stop()
        {
            _clientSocket?.Close();
        }

        public void RequestMarkers(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
        {
            Message newMessage = new Message($"{lat1};{lat2};{lng1};{lng2}", DateTime.Now, "", MessageType.RequestMarkers);
            SendMessage(newMessage);
        }

        public void SendMessage(string text)
        {
            if (text == "") { return; }
            try
            {
                Message newMessage = new Message(text, DateTime.Now, null);
                byte[] messageBytes = Encoding.ASCII.GetBytes(newMessage.ToJsonString());
                if (_clientSocket != null)
                {
                    _clientSocket.Send(messageBytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public void SendMessage(Message message)
        {
            try
            {
                byte[] messageBytes = Encoding.ASCII.GetBytes(message.ToJsonString());
                if (_clientSocket != null)
                {
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
                            if (receivedMessage != null)
                            {
                                Console.WriteLine(receivedMessage.ToString());
                            }
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