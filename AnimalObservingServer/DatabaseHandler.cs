using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnimalObservingServer.Marker;
using MySqlConnector;

namespace AnimalObservingServer
{
    internal class DatabaseHandler
    {
        private MySqlConnection? connection;

        public DatabaseHandler(string databaseServerIP, int databaseServerPort,  string databaseName, string uid, string databasePassword)
        {
            this.connection = new MySqlConnection(
                $"server={databaseServerIP};" +
                $"port={databaseServerPort};" +
                $"Database={databaseName};" +
                $"uid={uid};pwd={databasePassword};" +
                $"Allow User Variables=true;");
        }

        public List<MapMarker> GetMarkers(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
        {
            List<MapMarker> animalRecords = new List<MapMarker>();
            MySqlCommand cmd = null;
            try
            {
                connection.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            cmd = new MySqlCommand("SELECT * FROM AnimalRecord AR JOIN Marker M ON AR.MarkerID = M.MarkerID WHERE (Latitude BETWEEN @lat1 AND @lat2) AND (Longitude BETWEEN @lon1 AND @lon2);", connection);
            cmd.Parameters.AddWithValue("@lat1", lat1.ToString());
            cmd.Parameters.AddWithValue("@lat2", lat2.ToString());
            cmd.Parameters.AddWithValue("@lon1", lng1.ToString());
            cmd.Parameters.AddWithValue("@lon2", lng2.ToString());
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
                int id = citac.GetInt32("RecordID");
                decimal latitude = citac.GetDecimal("Latitude");
                decimal longitude = citac.GetDecimal("Longitude");
                animalRecords.Add(new MapMarker(id, latitude, longitude));
            }
            citac.Close();
            connection.Close();
            Console.WriteLine(connection.ConnectionString);
            return animalRecords;
        }

        public DetailedRecord GetDetailedRecord(int recordID)
        {
            DetailedRecord detailedRecord = null;
            MySqlCommand cmd = new MySqlCommand("SELECT * FROM AnimalRecord AR JOIN Marker M ON AR.MarkerID = M.MarkerID WHERE RecordID = @id;", connection);
            cmd.Parameters.AddWithValue("@id", recordID.ToString());
            try
            {
                connection.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
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
                int id = citac.GetInt32("RecordID");
                decimal latitude = citac.GetDecimal("Latitude");
                decimal longitude = citac.GetDecimal("Longitude");
                int speciesID = citac.GetInt32("SpeciesID");
                string text = citac.GetString("Description");
                string label = citac.GetString("RecordLabel");
                DateTime date = citac.GetDateTime("Date");
                detailedRecord = new DetailedRecord(id, latitude, longitude, speciesID, text, date, label);
            }
            citac.Close();
            return detailedRecord;
        }
    }
}
