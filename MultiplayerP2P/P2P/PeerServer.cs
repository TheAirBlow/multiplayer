using System.Net;
using System.Net.Sockets;
using NetCoreServer;

namespace MultiplayerP2P.P2P;

public class PeerServer : TcpServer
{
    public int PeopleConnected = 0;
    
    public PeerServer(IPAddress address, int port) : base(address, port) {}

    protected override TcpSession CreateSession()
        => new PeerSession(this);

    protected override void OnError(SocketError error)
        => Program.Logger.Error("Unknown error occured (peer server): {0}", error);
}