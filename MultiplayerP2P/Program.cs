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
    public static Dictionary<int, byte[]> Tokens = new();
    public static Dictionary<int, int> ServerToPeer = new();
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
        
        Logger.Information("Done, now waiting for people to create servers");
    }
}