using System.Diagnostics;
using System.Security.Cryptography;
using MultiplayerP2P.P2P;

namespace MultiplayerP2P;

public class Server
{
    public string Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
    public Dictionary<string, PeerSession> Players = new();
    public Stopwatch Timer = new();
    public PeerSession? Session;
    public byte[] Information;
    public int ServerId;
    public int PeerId;
}