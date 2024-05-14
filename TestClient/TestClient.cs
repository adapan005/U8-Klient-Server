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
            Message newMessage = new Message($"{lat1};{lng1};{lat2};{lng2}", DateTime.Now, "", MessageType.RequestMarkers);
            SendMessage(newMessage);
        }

        public void RequstAllMarkers()
        {
            Message newMessage = new Message("GetAllMarkers", DateTime.Now, "", MessageType.RequestAllMarkers);
            SendMessage(newMessage);
        }

        internal void RequestAllSpecies()
        {
            Message newMessage = new Message("GetAllSpecies", DateTime.Now, "TestClient", MessageType.RequestAllSpecies);
            SendMessage(newMessage);
        }

        public void AddRecord(int speciesID, double latitude, double longitude, string recordLabel, string recordDescription)
        {
            Message message = new Message($"{speciesID};{latitude};{longitude};{recordLabel};{recordDescription}", DateTime.Now, "TestClient", MessageType.AddRecordWithMarker);
            SendMessage(message);
        }

        public void RequestDetailedRecord(int idOfRecord)
        {
            Message message = new Message($"{idOfRecord}", DateTime.UtcNow, "", MessageType.RequestDetailedMarker);
            SendMessage(message);
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
                                Console.WriteLine("RECEIVERD: "+receivedMessage.ToString());
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