using System.Net;
using System.Net.Sockets;
using System.Text;
using Spectre.Console;

switch (args[0]) {
    case "server": {
        var client = new TcpClient();
        client.Connect(IPAddress.Loopback, 1337);
        AnsiConsole.MarkupLine("[green]Connected to main server[/]");
        
        var stream = client.GetStream();
        using (var memory = new MemoryStream())
        using (var writer = new BinaryWriter(memory)) {
            var bytes = Encoding.UTF8.GetBytes("The best server");
            writer.Write((byte) 0x00);
            writer.Write(bytes.Length);
            writer.Write(bytes);
            stream.Write(memory.ToArray());
        }
        
        AnsiConsole.MarkupLine("[green]Sent server creation request[/]");

        var data = new byte[256];
        // ReSharper disable once MustUseReturnValue
        stream.Read(data, 0, 256);
        using (var memory1 = new MemoryStream(data.ToArray()))
        using (var reader = new BinaryReader(memory1))
        using (var memory2 = new MemoryStream())
        using (var writer = new BinaryWriter(memory2)) {
            var x = reader.ReadInt32();
            var y = reader.ReadInt32();
            writer.Write(x + 26 * 54 / 24 - 78);
            writer.Write(y + 79 * 67 / 53 - 38);
            client.GetStream().Write(memory2.ToArray());
        }
        AnsiConsole.MarkupLine("[green]Verification packet sent[/]");

        var token = new byte[16]; int port;
        // ReSharper disable once MustUseReturnValue
        stream.Read(data, 0, 256);
        using (var memory = new MemoryStream( data.ToArray()))
        using (var reader = new BinaryReader(memory)) {
            if (reader.ReadBoolean()) {
                token = reader.ReadBytes(16);
                port = reader.ReadInt32();
                AnsiConsole.MarkupLine($"[green]Server created, peer port {port}[/]");
                AnsiConsole.MarkupLine($"[green]Token: 0x{Convert.ToHexString(token)}[/]");
            } else {
                AnsiConsole.MarkupLine("[red]Verification failed[/]");
                break;
            }
        }
        
        client.Close();
        client = new TcpClient();
        client.Connect(IPAddress.Loopback, port);
        AnsiConsole.MarkupLine("[green]Connected to the peer[/]");
        stream = client.GetStream();
        using (var memory = new MemoryStream())
        using (var writer = new BinaryWriter(memory)) {
            writer.Write((byte) 0x00);
            writer.Write(token);
            stream.Write(memory.ToArray());
        }
        
        AnsiConsole.MarkupLine("[green]Sent peer authentication request[/]");
        // ReSharper disable once MustUseReturnValue
        stream.Read(data, 0, 256);
        using (var memory1 = new MemoryStream(data.ToArray()))
        using (var reader = new BinaryReader(memory1)) {
            if (reader.ReadBoolean()) 
                AnsiConsole.MarkupLine("[green]Peer connection accepted[/]");
            else {
                AnsiConsole.MarkupLine("[red]Token verification failed[/]");
                break;
            }
        }

        while (true) {
            while (!stream.DataAvailable) {}
            // ReSharper disable once MustUseReturnValue
            stream.Read(data, 0, 256);
            Console.WriteLine(Encoding.UTF8.GetString(data));
            using var memory1 = new MemoryStream(data.ToArray());
            using var reader = new BinaryReader(memory1);
            switch (reader.ReadByte()) {
                case 0x00: // User message
                    var uuid1 = reader.ReadString();
                    var ping = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
                    stream.Write(Encoding.UTF8.GetBytes("pong"));
                    AnsiConsole.MarkupLine($"[green]Received {ping} from {uuid1}, send pong[/]");
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
        }
        break;
    }
    case "client": {
        var client = new TcpClient();
        client.Connect(IPAddress.Loopback, 1337);
        AnsiConsole.MarkupLine("[green]Connected to main server[/]");
        
        var stream = client.GetStream();
        var data = new byte[256];
        using (var memory2 = new MemoryStream())
        using (var writer = new BinaryWriter(memory2)) {
            writer.Write((byte) 0x02);
            writer.Write(0);
            client.GetStream().Write(memory2.ToArray());
        }
        AnsiConsole.MarkupLine("[green]Peer port request sent[/]");

        int port;
        // ReSharper disable once MustUseReturnValue
        stream.Read(data, 0, 256);
        using (var memory = new MemoryStream( data.ToArray()))
        using (var reader = new BinaryReader(memory)) {
            if (reader.ReadBoolean()) {
                port = reader.ReadInt32();
                AnsiConsole.MarkupLine($"[green]Peer port {port}[/]");
            } else {
                AnsiConsole.MarkupLine("[red]Server doesn't exist???[/]");
                break;
            }
        }
        
        client.Close();
        client = new TcpClient();
        client.Connect(IPAddress.Loopback, port);
        AnsiConsole.MarkupLine("[green]Connected to the peer[/]");
        stream = client.GetStream();
        using (var memory = new MemoryStream())
        using (var writer = new BinaryWriter(memory)) {
            writer.Write((byte) 0x01);
            writer.Write(0);
            var bebra = Encoding.UTF8.GetBytes("TheBlowJob");
            writer.Write(bebra.Length);
            writer.Write(bebra);
            stream.Write(memory.ToArray());
        }
        
        AnsiConsole.MarkupLine("[green]Sent server connection request[/]");
        // ReSharper disable once MustUseReturnValue
        stream.Read(data, 0, 256);
        using (var memory1 = new MemoryStream(data.ToArray()))
        using (var reader = new BinaryReader(memory1)) {
            if (reader.ReadBoolean()) {
                AnsiConsole.MarkupLine("[green]Server connection accepted, sent ping[/]");
                stream.Write(Encoding.UTF8.GetBytes("ping"));
            } else {
                AnsiConsole.MarkupLine($"[red]Error occured: 0x{Convert.ToHexString(new[] { reader.ReadByte() })}[/]");
                break;
            }
        }

        while (!stream.DataAvailable) {}
        var len = stream.Read(data, 0, 256);
        var pong = Encoding.UTF8.GetString(data, 0, len);
        AnsiConsole.MarkupLine($"[green]Received {pong}[/]");
        break;
    }
        
}