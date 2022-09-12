using System.Net;
using System.Net.Sockets;
using System.Text;
using Spectre.Console;
using TcpClient = NetCoreServer.TcpClient;

namespace MultiplayerP2P.TestApp.Client;

public class PeerClient : TcpClient
{
    public PeerClient(IPAddress address, int port) : base(address, port) {}
    private bool connectionAccepted;

    protected override void OnConnected()
    {
        AnsiConsole.MarkupLine("[green]Peer client connected[/]");
        ReceiveAsync();
    }

    protected override void OnDisconnected()
        => AnsiConsole.MarkupLine("[red]Peer client disconnected[/]");

    protected override void OnError(SocketError error)
        => AnsiConsole.MarkupLine($"[red]Peer client error: {error}[/]");
    
    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        using var memory1 = new MemoryStream(buffer, (int) offset, (int) size);
        using var reader = new BinaryReader(memory1, Encoding.UTF8);
        using var memory2 = new MemoryStream();
        using var writer = new BinaryWriter(memory2, Encoding.UTF8);
        if (connectionAccepted) {
            var pong = Encoding.UTF8.GetString(buffer, (int) offset, (int) size);
            AnsiConsole.MarkupLine($"[green]Received {pong}[/]");
        } else {
            if (reader.ReadBoolean()) {
                AnsiConsole.MarkupLine("[green]Server connection accepted, sent ping[/]");
                Send(Encoding.UTF8.GetBytes("ping"));
                connectionAccepted = true;
            } else {
                AnsiConsole.MarkupLine($"[red]Error occured: 0x{Convert.ToHexString(new[] { reader.ReadByte() })}[/]");
                Disconnect();
            }
        }
    }
}