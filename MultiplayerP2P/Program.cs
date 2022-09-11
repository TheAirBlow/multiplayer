using System.Diagnostics;
using System.Net;
using MultiplayerP2P.Main;
using MultiplayerP2P.P2P;
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
    public static Dictionary<int, string> Tokens = new();
    public static Dictionary<int, int> ServerToPeer = new();
    public static Dictionary<int, byte[]> ServerToInformation = new();
    public static Dictionary<int, Stopwatch> ServerToStopwatch = new();
    public static Dictionary<int, PeerSession> ServerToSession = new();

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
            var valuesList = ServerToStopwatch.Values.ToList();
            var keysList = ServerToStopwatch.Keys.ToList();
            for (var i = 0; i < valuesList.Count; i++) {
                var value = valuesList[i];
                var key = keysList[i];
                if (value.Elapsed.Seconds > 10) {
                    ServerToStopwatch.Remove(key);
                    if (!ServerToSession.ContainsKey(key)) {
                        Logger.Information($"Removed ghost server (ID {key}) on peer {ServerToPeer[key]}");
                        ServerToInformation.Remove(key);
                        ServerToStopwatch.Remove(key);
                        ServerToPeer.Remove(key);
                        Tokens.Remove(key);

                        using var memory = new MemoryStream();
                        using var writer = new BinaryWriter(memory);
                        writer.Write((byte) 0x00); writer.Write(key);
                        MainServer.Multicast(memory.ToArray());
                    }
                }
            }

            Thread.Sleep(5);
        }
    }
}