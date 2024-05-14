namespace AnimalObservingServer.Marker
{
    public class MapMarker
    {
        public int MarkerId { get; private set; }
        public decimal Latitude { get; private set; }
        public decimal Longitude { get; private set; }
        public string RecordLabel { get; private set; }

        public MapMarker(int id, decimal latitude, decimal longitude, string recordLabel)
        {
            MarkerId = id;
            Latitude = latitude;
            Longitude = longitude;
            RecordLabel = recordLabel;
        }

        public override string ToString()
        {
            return $"{MarkerId};{Latitude};{Longitude};{RecordLabel}";
        }
    }
}
