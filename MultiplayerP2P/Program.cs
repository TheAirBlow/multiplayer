using System.Diagnostics;
using System.Net;
using MultiplayerP2P.Main;
using MultiplayerP2P.P2P;
using MultiplayerP2P.Peer;
using Serilog;
using Serilog.Core;

namespace MultiplayerP2P;

public static class Program
{
    public static Logger Logger = new LoggerConfiguration()
        .WriteTo.File("main.log")
        .WriteTo.Console()
        .CreateLogger();

    public static MainServer MainServer;
    public static List<PeerServer> PeerPool = new();
    public static Dictionary<int, Server> Servers = new();

    public static void Main(string[] args)
    {
        Logger.Information("Loading up configuration...");
        Configuration.Load();
        
        MainServer = new(IPAddress.Any, Configuration.Data.MainPort);
        foreach (var i in Configuration.Data.PeerPorts)
            PeerPool.Add(new PeerServer(IPAddress.Any, i));
        
        Logger.Information("Starting up the main server...");
        MainServer.Start();
        
        Logger.Information("Starting up all peers...");
        foreach (var i in PeerPool)
            i.Start();
        
        Logger.Information("Starting up watchdog thread...");
        new Thread(WatchdogThread).Start();
        
        Logger.Information("Done, now waiting for people to create servers");
        while (true) {}
    }

    private static void WatchdogThread()
    {
        while (true) {
            var valuesList = Servers.Values.ToList();
            var keysList = Servers.Keys.ToList();
            for (var i = 0; i < valuesList.Count; i++) {
                var value = valuesList[i];
                if (value.Session != null) continue;
                var key = keysList[i];
                if (value.Timer.Elapsed.Seconds > 10) {
                    Servers.Remove(key);
                    using var memory = new MemoryStream();
                    using var writer = new BinaryWriter(memory);
                    writer.Write((byte) 0x00); writer.Write(key);
                    MainServer.Multicast(memory.ToArray());
                    Logger.Information($"Removed ghost server (ID {key}) on peer {value.PeerId}");
                }
            }

            Thread.Sleep(5);
        }
    }
}