using static System.Net.Mime.MediaTypeNames;
using System.Text.Json;
using System.Text;
using System.Xml.Linq;

namespace U8_Library
{
    public class Message
    {
        public string Text { get; private set; }
        public DateTime Date { get; private set; }
        public string SenderName { get; private set; }
        public Message(string Text, DateTime Date, string SenderName)
        {
            this.Text = Text;
            this.Date = Date;
            this.SenderName = SenderName;
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
