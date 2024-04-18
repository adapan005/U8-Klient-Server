using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace AnimalObservingServer.Marker
{
    public class DetailedRecord : MapMarker
    {
        public DetailedRecord(int id, decimal latitude, decimal longitude, int speciesID, string Text, DateTime dateTime, string label) : base(id, latitude, longitude)
        {
            this.SpeciesID = speciesID;
            this.Text = Text;
            this.Date = dateTime;
            this.RecordLabel = label;
        }

        public string Text { get; private set; }
        public DateTime Date { get; private set; }
        public int SpeciesID { get; private set; }
        public string RecordLabel {  get; private set; }

        public string ToString()
        {
            return $"{SpeciesID};{Date};{RecordLabel};{Text}";
        }

    }
}
