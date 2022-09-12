using System.Net;
using System.Net.Sockets;
using System.Text;
using Spectre.Console;
using TcpClient = NetCoreServer.TcpClient;

namespace MultiplayerP2P.TestApp.Server;

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
            switch (reader.ReadByte()) {
                case 0x00: // User message
                    var uuid1 = reader.ReadString();
                    var ping = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
                    writer.Write((byte) 0x01);
                    writer.Write(uuid1);
                    var bebra = Encoding.UTF8.GetBytes("pong");
                    writer.Write(bebra.Length);
                    writer.Write(bebra);
                    Send(memory2.ToArray());
                    AnsiConsole.MarkupLine($"[green]Received {ping} from {uuid1}, sent pong[/]");
                    break;
                case 0x01: // MultiplayerP2P message
                    switch (reader.ReadByte()) {
                        case 0x00: // Connected
                            var uuid2 = reader.ReadString();
                            var username = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
                            AnsiConsole.MarkupLine($"[green]{uuid2} connected with username {username}[/]");
                            break;
                        case 0x01: // Disconnected
                            var uuid3 = reader.ReadString();
                            AnsiConsole.MarkupLine($"[green]{uuid3} disconnected[/]");
                            break;
                    }
                    break;
            }
        } else {
            if (reader.ReadBoolean()) {
                AnsiConsole.MarkupLine("[green]Peer connection accepted[/]");
                connectionAccepted = true;
            } else {
                AnsiConsole.MarkupLine("[red]Token verification failed[/]");
                Disconnect();
            }
        }
    }
}