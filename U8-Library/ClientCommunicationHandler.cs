using System.Net.Sockets;

namespace U8_Library
{
    public class ClientCommunicationHandler
    {
        private readonly List<Socket> _clientConnections;
        private readonly List<Message> _messages;
        private readonly int _port;

        public ClientCommunicationHandler(int port)
        {
            _port = port;
            _clientConnections = new List<Socket>();
            _messages = new List<Message>();
        }
    }
}
