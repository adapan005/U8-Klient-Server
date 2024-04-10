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
using MySqlConnector;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.CompilerServices;
using System.Data;
using System.Xml.Linq;

namespace AnimalObservingServer
{
    class AnimalObservingServer
    {
        private readonly List<Socket> _clientConnections;
        private readonly List<Message> _messages;
        private readonly int _port;

        public AnimalObservingServer(int port)
        {
            _messages = new List<Message>();
            _port = port;
            _clientConnections = new List<Socket>();
        }

        public void Start()
        {
            LoadMessagesOnStartup();
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

            GetFromDatabaseTest();

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

        public void GetFromDatabaseTest()
        {
            List<(int, string)> species = new List<(int, string)>();
            string _connectionString = "server=localhost;port=4723;Database=animals;uid=root;pwd=Heslo123;Allow User Variables=true;";
            MySqlConnection conn = null;
            MySqlCommand cmd = null;
            try
            {
                conn = new MySqlConnection(_connectionString);
                conn.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            MySqlDataReader reader;
            cmd = new MySqlCommand("SELECT * FROM Species", conn);
            MySqlDataReader citac = null;
            try
            {
                citac = cmd.ExecuteReader();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            while (citac.Read())
            {
                int id = citac.GetInt32("SpeciesID");
                string description = citac.GetString("SpeciesName");
                species.Add((id, description));
            }
            citac.Close();
            foreach (var specie in species)
            {
                Console.WriteLine($"ID: {specie.Item1}, Name: {specie.Item2}");
            }
        }

        private void LoadMessagesOnStartup()
        {
            string savesFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "saves");
            //Get file with highest number
            // Check if the directory exists
            long highestNumber = 0;
            if (Directory.Exists(savesFolder))
            {
                // Get all files in the directory
                string[] files = Directory.GetFiles(savesFolder);

                // Perform an action for each file
                foreach (string filePath in files)
                {
                    long currentNumber = long.Parse(Path.GetFileName(filePath).Split(".")[0]);
                    if (currentNumber > highestNumber)
                    {
                        highestNumber = currentNumber;
                    }
                }
                if (highestNumber > 1)
                {
                    Console.WriteLine($"Loading messages from saves/{highestNumber}.json");
                    try
                    {
                        // Read all lines from the file and store them in an array of strings
                        string[] lines = File.ReadAllLines(savesFolder + $"\\{highestNumber}.json");

                        // Display the loaded strings
                        foreach (string line in lines)
                        {
                            //Console.WriteLine(line);
                            _messages.Add(StringToMessage(line));
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle exceptions, such as file not found or access denied
                        Console.WriteLine($"Error reading the file: {ex.Message}");
                    }
                }
            }
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
                    ProcessMessage(StringToMessage(messageString));
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

        private void ProcessMessage(Message message)
        {
            if (message == null)
            {
                return;
            }
            Console.WriteLine(message.ToString());
            SendMessageToAllClients(message);
            if (message.Text[0] == '/')
            {
                switch (message.Text)
                {
                    case "/save":
                        SaveMessagesToFile();
                        SendMessageToAllClients(new Message("Saving messages", DateTime.Now, "SERVER"));
                        break;
                    default:
                        Console.WriteLine("Unknown command");
                        SendMessageToAllClients(new Message("Unknown command", DateTime.Now, "SERVER"));
                        break;
                }
            }
            else
            {
                _messages.Add(message);
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

        private void SaveMessagesToFile()
        {
            if (_messages.Count <= 1)
            {
                return;
            }
            Console.WriteLine("Saving messages");
            using (var mutex = new Mutex())
            {
                try
                {
                    mutex.WaitOne();

                    string savesFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "saves");
                    Directory.CreateDirectory(savesFolder);

                    string filePath = Path.Combine(savesFolder, $"{DateTime.Now:yyyyMMddHHmmssfff}.json");

                    using (StreamWriter writer = new StreamWriter(filePath, true))
                    {
                        foreach (Message message in _messages)
                        {
                            writer.WriteLine(message.ToJsonString());
                        }
                    }
                    _messages.Clear();
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
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

        private void SendToEndpoint(Socket clientSocket, byte[] messageBytes)
        {
            clientSocket.Send(messageBytes, 0, messageBytes.Length, SocketFlags.None);
        }
    }
}
