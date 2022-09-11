using Newtonsoft.Json;

namespace MultiplayerP2P;

public class Configuration
{
    public class DataClass
    {
        public class SecurityClass
        {
            [JsonProperty("checksAmount")] public int ChecksAmount;
            [JsonProperty("numbers")] public List<List<int>> Numbers = new() {
                new List<int> { 26, 54, 24, 78 },
                new List<int> { 79, 67, 53, 38 }
            };
        }

        [JsonProperty("security")] public SecurityClass Security = new();
        [JsonProperty("peerPorts")] public List<int> PeerPorts = new() {
            1338, 1339, 1340, 1341, 1342
        };
        [JsonProperty("mainPort")] public int MainPort = 1337;
    }

    public static DataClass Data = new();

    public static void Load()
    {
        if (!File.Exists("config.json"))
            File.WriteAllText("config.json", JsonConvert.SerializeObject(Data));
        
        try {
            var tmp = JsonConvert.DeserializeObject<DataClass>(File.ReadAllText("config.json"));
            Data = tmp ?? throw new Exception("Data is null");
        } catch {
            Program.Logger.Fatal("Unable to parse config, please edit or delete it");
            Environment.Exit(0);
        }

        foreach (var i in Data.Security.Numbers)
            if (i.Count != 4 || i[1] <= i[2]) {
                Program.Logger.Fatal($"Invalid number sequence: {string.Join(", ", i)}");
                Program.Logger.Fatal("There are less or more than 4 numbers, or " +
                                     "the third number is bigger than the second one");
                Environment.Exit(0);
            }

        if (Data.PeerPorts.Contains(Data.MainPort)) {
            Program.Logger.Fatal("Main port is included in peer ports list");
            Environment.Exit(0);
        }
    }
}