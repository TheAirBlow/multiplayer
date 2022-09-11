using System.Net;
using System.Net.Sockets;
using MultiplayerP2P.P2P;
using NetCoreServer;

namespace MultiplayerP2P.Main;

public class MainServer : TcpServer
{
    public MainServer(IPAddress address, int port) : base(address, port) {}

    protected override TcpSession CreateSession()
        => new PeerSession(this);

    protected override void OnError(SocketError error)
        => Program.Logger.Error("Unknown error occured (notifier server): {0}", error);
}