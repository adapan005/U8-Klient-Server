using TestClient;

TestClient.TestClient client = new TestClient.TestClient("127.0.0.1", 55557);
client.Start();
//client.SendMessage("RequestMarkers;48.22125544689768;18.801564227965887;48.64992632520529;19.319728467757017");
client.RequestMarkers(38, 14, 50, 20);

client.RequestDetailedRecord(25);