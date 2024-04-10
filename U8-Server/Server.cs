using System.Net;
using System.Net.Sockets;
using System.Text;
using U8_Library;
using System.Text.Json;
using System.Reflection;

namespace U8_Server
{
    class Server
    {
        private readonly List<Socket> _clientConnections;
        private readonly List<Message> _messages;
        private readonly int _port;

        public Server(int port)
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
            } catch (SocketException e)
            {
                Console.WriteLine($"{e}\nError: Port {_port} is already in use.");
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
                Environment.Exit(1);
            }
            serverListener.Listen(0);
            Console.WriteLine("U8-Server started");

            while (true)
            {
                try {
                    Socket clientSocket = serverListener.Accept();
                    _clientConnections.Add(clientSocket);
                    Console.WriteLine("Client joined");
                    OnClientConnect(clientSocket);

                    Thread clientThread = new Thread(new ThreadStart(() =>
                    ReceiveClientMessagess(clientSocket)));
                    clientThread.Start();
                } catch
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
                } catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return null;
                }
            }
            else
                return null;
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
                try {
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
            if (message == null) { 
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
            } else
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