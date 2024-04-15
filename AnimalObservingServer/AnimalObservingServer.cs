﻿using System;
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

            foreach (var record in databaseHandler.GetMarkers(38,14,50,20))
            {
                Console.WriteLine(record.ToString());
            }
            Console.WriteLine(databaseHandler.GetDetailedRecord(25));

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
                //Deserialise json
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
                        // Connection closed by the client
                        Console.WriteLine("Client disconnected");
                        _clientConnections.Remove(client);
                        break;
                    }
                    string messageString = Encoding.ASCII.GetString(message, 0, size);
                    //Do something with messages
                    ProcessMessage(StringToMessage(messageString), client);
                }
                catch
                {
                    // Handle socket exception (client disconnected)
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
            Console.WriteLine(message.ToString());
            SendMessageToAllClients(message);
            switch (message.MessageType)
            {
                //Zparsuj nejak podla typu requestu
                case MessageType.RequestMarkers:
                    //SendMessageToAllClients(new Message("Saving messages", DateTime.Now, "SERVER", MessageType.Informative));
                    Console.WriteLine("REQUESTED MARKERS");
                    //TODO: Posli odpoved obsahujucu markers

                    //parse coords with regex
                    //lat1,lon1,lat2,lon2
                    //List<MapMarker> markers = 

                    //foreach marker posli message a potom zakonci nejakou ukoncovacou spravou aby vedel ze uz ma vsetky markery
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
            byte[] messageBytes = Encoding.ASCII.GetBytes(message.ToJsonString());
            clientSocket.Send(messageBytes, 0, messageBytes.Length, SocketFlags.None);
        }

        private void SendToEndpoint(Socket clientSocket, byte[] messageBytes)
        {
            clientSocket.Send(messageBytes, 0, messageBytes.Length, SocketFlags.None);
        }
    }
}
