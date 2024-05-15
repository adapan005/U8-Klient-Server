using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnimalObservingServer.Marker;
using MySqlConnector;
using U8_Library.Species;

namespace AnimalObservingServer
{
    internal class DatabaseHandler
    {
        private readonly MySqlConnection connection;

        public DatabaseHandler(string databaseServerIP, int databaseServerPort, string databaseName, string uid, string databasePassword)
        {
            this.connection = new MySqlConnection(
                $"server={databaseServerIP};" +
                $"port={databaseServerPort};" +
                $"Database={databaseName};" +
                $"uid={uid};pwd={databasePassword};" +
                $"Allow User Variables=true;");
        }

        public List<MapMarker> GetAllMarkers()
        {
            List<MapMarker> animalRecords = new List<MapMarker>();
            MySqlCommand cmd = null;
            try
            {
                connection.Open();
                cmd = new MySqlCommand("SELECT * FROM AnimalRecord AR JOIN Marker M ON AR.MarkerID = M.MarkerID", connection);
                using (MySqlDataReader citac = cmd.ExecuteReader())
                {
                    while (citac.Read())
                    {
                        int id = citac.GetInt32("RecordID");
                        decimal latitude = citac.GetDecimal("Latitude");
                        decimal longitude = citac.GetDecimal("Longitude");
                        string recordLabel = citac.GetString("RecordLabel");
                        animalRecords.Add(new MapMarker(id, latitude, longitude, recordLabel));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
            return animalRecords;
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
                string recordLabel = citac.GetString("RecordLabel");
                animalRecords.Add(new MapMarker(id, latitude, longitude, recordLabel));
            }
            citac.Close();
            connection.Close();
            return animalRecords;
        }

        public void AddRecordWithMarker(int speciesID, double latitude, double longitude, string recordLabel, string recordDescription)
        {
            if (speciesID < 32 || recordLabel.Length <= 1)
            {
                Console.WriteLine("NOT ADDED!");
                return;
            }
            MySqlCommand cmd = new MySqlCommand("CALL AddRecordWithMarker(@speciesID, @latitude, @longitude, @recordLabel, @recordDescription)", connection);
            
            cmd.Parameters.AddWithValue("@speciesID", speciesID);
            cmd.Parameters.AddWithValue("@latitude", latitude);
            cmd.Parameters.AddWithValue("@longitude", longitude);
            cmd.Parameters.AddWithValue("@recordLabel", recordLabel);
            cmd.Parameters.AddWithValue("@recordDescription", recordDescription);

            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    connection.Open();
                }
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
            connection.Close();
        }


        public DetailedRecord GetDetailedRecord(int recordID)
        {
            DetailedRecord detailedRecord = null;
            //MySqlCommand cmd = new MySqlCommand("SELECT * FROM AnimalRecord AR JOIN Marker M ON AR.MarkerID = M.MarkerID WHERE RecordID = @id;", connection);
            MySqlCommand cmd = new MySqlCommand("SELECT AnimalRecord.RecordID, Marker.Latitude, Marker.Longitude, Species.SpeciesName, AnimalRecord.Description, AnimalRecord.RecordLabel, AnimalRecord.Date FROM Marker JOIN AnimalRecord ON Marker.MarkerID = AnimalRecord.MarkerID JOIN Species ON AnimalRecord.SpeciesID = Species.SpeciesID WHERE RecordID =  @id;", connection);
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
                string speciesName = citac.GetString("SpeciesName");
                string text = citac.GetString("Description");
                string label = citac.GetString("RecordLabel");
                DateTime date = citac.GetDateTime("Date");
                detailedRecord = new DetailedRecord(id, latitude, label, longitude, speciesName, text, date);
            }
            citac.Close();
            connection.Close();
            return detailedRecord;
        }

        public List<Specie> GetSpecies()
        {
            List<Specie> speciesList = new List<Specie>();
            MySqlCommand cmd = new MySqlCommand("SELECT * FROM Species", connection);

            try
            {
                connection.Open();
                MySqlDataReader citac = cmd.ExecuteReader();

                if (citac != null)
                {
                    while (citac.Read())
                    {
                        int id = citac.GetInt32("SpeciesID");
                        string name = citac.GetString("SpeciesName");
                        speciesList.Add(new Specie(id, name));
                    }

                    citac.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                connection.Close();
            }

            return speciesList;
        }
    }
}
