using System.Net;
using System.Net.Sockets;
using System.Text;
using Spectre.Console;
using TcpClient = NetCoreServer.TcpClient;

namespace MultiplayerP2P.TestApp.Server;

public class MainClient : TcpClient
{
    public MainClient(IPAddress address, int port) : base(address, port) {}
    private bool verificationCheckPass;
    private bool verified;
    public byte[]? Token;
    public int Port;

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
        if (!verified && !verificationCheckPass) {
            var x = reader.ReadInt32();
            var y = reader.ReadInt32();
            writer.Write(x + 26 * 54 / 24 - 78);
            writer.Write(y + 79 * 67 / 53 - 38);
            SendAsync(memory2.ToArray());
            AnsiConsole.MarkupLine("[green]Verification packet sent[/]");
            verified = true;
        } else if (verified && !verificationCheckPass) {
            if (reader.ReadBoolean()) {
                Token = reader.ReadBytes(16);
                Port = reader.ReadInt32();
                AnsiConsole.MarkupLine($"[green]Server created, peer port {Port}[/]");
                AnsiConsole.MarkupLine($"[green]Token: 0x{Convert.ToHexString(Token)}[/]");
                verificationCheckPass = true;
            } else AnsiConsole.MarkupLine("[red]Verification failed[/]");
            Disconnect();
        }
    }
}