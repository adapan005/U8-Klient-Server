using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using U8_Library;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.CompilerServices;
using System.Data;
using System.Xml.Linq;
using AnimalObservingServer.Marker;
using System.Text.RegularExpressions;
using U8_Library.Species;

namespace AnimalObservingServer
{
    class AnimalObservingServer
    {
        private readonly List<Socket> _clientConnections;
        private readonly List<Message> _messages;
        private readonly DatabaseHandler databaseHandler = new DatabaseHandler("localhost", 4723, "animals", "root", "Heslo123");
        private readonly int _port;

        public AnimalObservingServer(int port)
        {
            _messages = new List<Message>();
            _port = port;
            _clientConnections = new List<Socket>();
        }

        public void Start()
        {
            Socket serverListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, _port);
            try
            {
                serverListener.Bind(endPoint);
            }
            catch (SocketException e)
            {
                Console.WriteLine($"{e}\nError: Port {_port} is already in use.");
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                Environment.Exit(1);
            }
            serverListener.Listen(0);
            Console.WriteLine("AnimalObserving-Server started");

            while (true)
            {
                try
                {
                    Socket clientSocket = serverListener.Accept();
                    _clientConnections.Add(clientSocket);
                    Console.WriteLine("Client joined");
                    OnClientConnect(clientSocket);

                    Thread clientThread = new Thread(new ThreadStart(() =>
                    ReceiveClientMessagess(clientSocket)));
                    clientThread.Start();
                }
                catch
                {
                    break;
                }
            }
        }

        private Message? StringToMessage(string jsonString)
        {
            Message? newMessage;
            if (jsonString != null)
            {
                try
                {
                    newMessage = JsonSerializer.Deserialize<Message>(jsonString);
                    return newMessage;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return null;
                }
            }
            else
                return null;
        }

        private void ReceiveClientMessagess(Socket client)
        {
            while (true)
            {
                byte[] message = new byte[96000];
                try
                {
                    int size = client.Receive(message);
                    if (size == 0)
                    {
                        Console.WriteLine("Client disconnected");
                        _clientConnections.Remove(client);
                        break;
                    }
                    string messageString = Encoding.ASCII.GetString(message, 0, size);
                    Console.WriteLine("RAW RECEIVED: " + messageString);
                    ProcessMessage(StringToMessage(messageString), client);
                }
                catch
                {
                    Console.WriteLine("Client disconnected");
                    _clientConnections.Remove(client);
                    break;
                }
            }
        }

        private void ProcessMessage(Message message, Socket clientSocket)
        {
            if (message == null)
            {
                return;
            }
            Console.WriteLine(message.Text);

            string text = message.Text;
            switch (message.MessageType)
            {
                //Zparsuj nejak podla typu requestu
                case MessageType.RequestAllMarkers:
                    Console.WriteLine("REQUESTED ALL MARKERS");
                    List<MapMarker> allMarkers = databaseHandler.GetAllMarkers();
                    foreach (var marker in allMarkers)
                    {
                        Message newMessage = new Message(marker.ToString(), DateTime.UtcNow, "", MessageType.MapMarkerInfo);
                        SendToEndpoint(clientSocket, newMessage);
                    }
                    break;

                case MessageType.RequestMarkers:
                    Console.WriteLine("REQUESTED MARKERS");
                    string pattern = @"\d+";
                    MatchCollection matches = Regex.Matches(text, pattern);
                    int[] numbers = new int[4];
                    for (int i = 0; i < 4; i++)
                    {
                        numbers[i] = int.Parse(matches[i].Value);
                    }
                    int lat1 = numbers[0];
                    int lng1 = numbers[1];
                    int lat2 = numbers[2];
                    int lng2 = numbers[3];
                    List<MapMarker> markers = databaseHandler.GetMarkers(lat1, lng1, lat2, lng2);
                    foreach (var marker in markers)
                    {
                        Message newMessage = new Message(marker.ToString(), DateTime.UtcNow, "", MessageType.MapMarkerInfo);
                        SendToEndpoint(clientSocket, newMessage);
                    }

                    break;
                case MessageType.RequestDetailedMarker:
                    Console.WriteLine("REQUESTED DETAILED RECORD");
                    DetailedRecord record = databaseHandler.GetDetailedRecord(Int32.Parse(message.Text));
                    SendToEndpoint(clientSocket, record.ToString(), MessageType.RequestDetailedMarker);
                    break;
                case MessageType.RequestAllSpecies:
                    Console.WriteLine("REQUESTED ALL SPECIES");
                    List<Specie> allSpecies = databaseHandler.GetSpecies();
                    foreach (var specie in allSpecies)
                    {
                        Message newMessage = new Message(specie.ToString(), DateTime.UtcNow, "", MessageType.MapMarkerInfo);
                        SendToEndpoint(clientSocket, newMessage);
                    }
                    break;
                case MessageType.AddRecordWithMarker:
                    Console.WriteLine("RECORD WRITING REQEUSTED");
                    string[] polia = text.Split(';');
                    int speciesID = Int32.Parse(polia[0]);
                    double lat = Double.Parse(polia[1]);
                    double lon = Double.Parse(polia[2]);
                    string label = polia[3];
                    string description = polia[4];
                    databaseHandler.AddRecordWithMarker(
                        speciesID,
                        lat,
                        lon,
                        label,
                        description
                    );

                    break;
                default:
                    //Console.WriteLine("Unknown command");
                    //SendMessageToAllClients(new Message("Unknown command", DateTime.Now, "SERVER", MessageType.Informative));
                    break;
            }
        }

        private void OnClientConnect(Socket clientSocket)
        {
            //Posle klientom po pripojeni spravy z _messages aby videli poslednu konverzaciu
            //Podobne instrukcie ako v SendMessageToAllClients()
            foreach (var message in _messages)
            {
                Console.WriteLine(message.ToJsonString());
                byte[] messageBytes = Encoding.ASCII.GetBytes(message.ToJsonString());
                SendToEndpoint(clientSocket, messageBytes);
            }
        }

        private void SendMessageToAllClients(Message message)
        {
            foreach (Socket clientSocket in _clientConnections)
            {
                byte[] messageBytes = Encoding.ASCII.GetBytes(message.ToJsonString());
                SendToEndpoint(clientSocket, messageBytes);
            }
        }

        private void SendToEndpoint(Socket clientSocket, Message message)
        {
            string messageString = message.ToJsonString() + "\n";
            byte[] messageBytes = Encoding.ASCII.GetBytes(messageString);
            clientSocket.Send(messageBytes, 0, messageBytes.Length, SocketFlags.None);
            Console.WriteLine($"Sending: {messageString} to {clientSocket.ToString()}");
        }

        private void SendToEndpoint(Socket clientSocket, byte[] messageBytes)
        {
            clientSocket.Send(messageBytes, 0, messageBytes.Length, SocketFlags.None);
        }

        private void SendToEndpoint(Socket clientSocket, string text, MessageType type)
        {
            Message message = new Message(text, DateTime.UtcNow, "", type);
            SendToEndpoint(clientSocket, message);
        }
    }
}
