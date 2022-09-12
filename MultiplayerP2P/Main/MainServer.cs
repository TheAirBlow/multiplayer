using System.Net;
using System.Net.Sockets;
using MultiplayerP2P.P2P;
using NetCoreServer;

namespace MultiplayerP2P.Main;

public class MainServer : TcpServer
{
    public MainServer(IPAddress address, int port) : base(address, port) {}

    protected override TcpSession CreateSession()
        => new MainSession(this);

    protected override void OnError(SocketError error)
        => Program.Logger.Error("Socket error occured (main server): {0}", error);
}