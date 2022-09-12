using System.Net;
using System.Net.Sockets;
using MultiplayerP2P.P2P;
using NetCoreServer;

namespace MultiplayerP2P.Peer;

public class PeerServer : TcpServer
{
    public int PeopleConnected = 0;
    
    public PeerServer(IPAddress address, int port) : base(address, port) {}

    protected override TcpSession CreateSession()
        => new PeerSession(this);

    protected override void OnError(SocketError error)
        => Program.Logger.Error("Socket error occured (peer server): {0}", error);
}