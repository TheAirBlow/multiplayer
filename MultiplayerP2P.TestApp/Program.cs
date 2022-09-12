using System.Net;
using System.Text;
using MultiplayerP2P.TestApp.Server;
using Spectre.Console;

switch (args[0]) {
    case "server": {
        var mainClient = new MainClient(IPAddress.Loopback, 1337);
        mainClient.Connect();
        
        using (var memory = new MemoryStream())
        using (var writer = new BinaryWriter(memory)) {
            var bytes = Encoding.UTF8.GetBytes("The best server");
            writer.Write((byte) 0x00);
            writer.Write(bytes.Length);
            writer.Write(bytes);
            mainClient.SendAsync(memory.ToArray());
        }
        
        AnsiConsole.MarkupLine("[green]Sent server creation request[/]");
        while (mainClient.IsConnected) {}

        if (mainClient.Token == null) return;
        var peerClient = new PeerClient(IPAddress.Loopback, mainClient.Port);
        peerClient.Connect();
        
        using (var memory = new MemoryStream())
        using (var writer = new BinaryWriter(memory)) {
            writer.Write((byte) 0x00);
            writer.Write(mainClient.Token);
            peerClient.SendAsync(memory.ToArray());
        }
        
        AnsiConsole.MarkupLine("[green]Sent peer authentication request[/]");
        while (true) {}
    }
    case "client": {
        var mainClient = new MultiplayerP2P.TestApp.Client.MainClient(IPAddress.Loopback, 1337);
        mainClient.Connect();
        
        using (var memory2 = new MemoryStream())
        using (var writer = new BinaryWriter(memory2)) {
            writer.Write((byte) 0x01);
            writer.Write(0);
            mainClient.Send(memory2.ToArray());
        }
        
        AnsiConsole.MarkupLine("[green]Peer port request sent[/]");
        while (mainClient.IsConnected) {}

        if (mainClient.Port == -1) return;
        var peerClient = new MultiplayerP2P.TestApp.Client.PeerClient(IPAddress.Loopback, mainClient.Port);
        peerClient.Connect();
        
        using (var memory = new MemoryStream())
        using (var writer = new BinaryWriter(memory)) {
            writer.Write((byte) 0x01);
            writer.Write(0);
            var bebra = Encoding.UTF8.GetBytes("TheBlowJob");
            writer.Write(bebra.Length);
            writer.Write(bebra);
            peerClient.SendAsync(memory.ToArray());
        }
        
        AnsiConsole.MarkupLine("[green]Sent server connection request[/]");
        while (true) {}
    }
}