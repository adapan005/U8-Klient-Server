using static System.Net.Mime.MediaTypeNames;
using System.Text.Json;
using System.Text;
using System.Xml.Linq;

namespace U8_Library
{
    public enum MessageType
    {
        Informative,
        RequestMarkers,
        RequestAllMarkers,
        RequestDetailedMarker,
        MapMarkerInfo,
        RequestAllSpecies,
        AddRecordWithMarker
    }
    public class Message
    {
        public string Text { get; private set; }
        public DateTime Date { get; private set; }
        public string SenderName { get; private set; }

        public MessageType MessageType { get; private set; }

        public Message(string Text, DateTime Date, string SenderName, MessageType messageType = MessageType.Informative)
        {
            this.Text = Text;
            this.Date = Date;
            this.SenderName = SenderName;
            this.MessageType = messageType;
        }

        public string ToJsonString()
        {
            return JsonSerializer.Serialize(this);
        }

        public override string ToString()
        {
            return $"[{Date.ToString("HH:mm:ss")}] {SenderName}: {Text}";
        }
    }
}
