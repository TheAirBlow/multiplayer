using System.Net;
using System.Net.Sockets;
using System.Text;
using Spectre.Console;
using TcpClient = NetCoreServer.TcpClient;

namespace MultiplayerP2P.TestApp.Client;

public class MainClient : TcpClient
{
    public MainClient(IPAddress address, int port) : base(address, port) {}
    public int Port = -1;

    protected override void OnConnected()
    {
        AnsiConsole.MarkupLine("[green]Main client connected[/]");
        ReceiveAsync();
    }

    protected override void OnDisconnected()
        => AnsiConsole.MarkupLine("[red]Main client disconnected[/]");

    protected override void OnError(SocketError error)
        => AnsiConsole.MarkupLine($"[red]Main client error: {error}[/]");

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        using var memory1 = new MemoryStream(buffer, (int) offset, (int) size);
        using var reader = new BinaryReader(memory1, Encoding.UTF8);
        using var memory2 = new MemoryStream();
        using var writer = new BinaryWriter(memory2, Encoding.UTF8);
        if (reader.ReadBoolean()) {
            Port = reader.ReadInt32();
            AnsiConsole.MarkupLine($"[green]Peer port {Port}[/]");
        } else AnsiConsole.MarkupLine("[red]Server doesn't exist???[/]");
        Disconnect();
    }
}