using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace AnimalObservingServer.Marker
{
    public class DetailedRecord : MapMarker
    {
        public DetailedRecord(int id, decimal latitude, string recordLabel, decimal longitude, string speciesName, string description, DateTime dateTime) : base(id, latitude, longitude, recordLabel)
        {
            this.SpeciesName = speciesName;
            this.Description = description;
            this.Date = dateTime;
        }

        public string Description { get; private set; }
        public DateTime Date { get; private set; }
        public string SpeciesName { get; private set; }

        public override string ToString()
        {
            //markerID, lat, lon, recordLabel, SpeciesName, date, description
            //base.ToString(): $"{MarkerId};{Latitude};{Longitude};{RecordLabel}"
            return $"{base.ToString()};{SpeciesName};{Date};{Description}";
        }

    }
}
