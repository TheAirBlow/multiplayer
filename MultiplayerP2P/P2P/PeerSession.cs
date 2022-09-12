using System.Net.Sockets;
using System.Text;
using NetCoreServer;
using Serilog.Core;

namespace MultiplayerP2P.P2P;

public class PeerSession : TcpSession
{
    public PeerSession(TcpServer server) : base(server) { }
    private int serverId;
    private enum SessionState
    {
        ConnectedToServer = 0,
        HostingServer = 1,
        Idle = 2
    }

    private SessionState _sessionState = SessionState.Idle;

    protected override void OnDisconnected()
    {
        switch (_sessionState) {
            case SessionState.HostingServer:
                Program.Logger.Information($"Server (ID {serverId}) on peer {Program.Servers[serverId].PeerId} disconnected");
                foreach (var i in Program.Servers[serverId].Players)
                    i.Value.Disconnect();
                
                Program.Servers.Remove(serverId);
            
                using (var memory = new MemoryStream())
                using (var writer = new BinaryWriter(memory)) {
                    writer.Write((byte) 0x00); writer.Write(serverId);
                    Program.MainServer.Multicast(memory.ToArray());
                }
                
                break;
            case SessionState.ConnectedToServer:
                if (!Program.Servers.ContainsKey(serverId)) break;
                Program.PeerPool[Program.Servers[serverId].PeerId].PeopleConnected--;
                Program.Servers[serverId].Players.Remove(Id.ToString());

                using (var memory = new MemoryStream())
                using (var writer = new BinaryWriter(memory)) {
                    writer.Write((byte) 0x01); 
                    writer.Write((byte) 0x01);
                    writer.Write(Id.ToString());
                    Program.Servers[serverId].Session!.SendAsync(memory.ToArray());
                }
                break;
        }
    }

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        try {
            using var memory1 = new MemoryStream(buffer, (int) offset, (int) size);
            using var reader = new BinaryReader(memory1, Encoding.UTF8);
            using var memory2 = new MemoryStream();
            using var writer = new BinaryWriter(memory2, Encoding.UTF8);
            switch (_sessionState) {
                case SessionState.HostingServer:
                    try {
                        switch (reader.ReadByte()) {
                            case 0x00: // Disconnect a user
                                Program.Servers[serverId].Players[reader.ReadString()].Disconnect();
                                break;
                            case 0x01: // Send a message to user
                                var player = Program.Servers[serverId].Players[reader.ReadString()];
                                player.SendAsync(reader.ReadBytes(reader.ReadInt32()));
                                break;
                            case 0x02: // Broadcast a message
                                Server.Multicast(reader.ReadBytes(reader.ReadInt32()));
                                break;
                        }
                    } catch (EndOfStreamException) {
                        Disconnect();
                    }
                    break;
                case SessionState.ConnectedToServer:
                    writer.Write((byte) 0x00);
                    writer.Write(Id.ToString());
                    writer.Write((int) size);
                    writer.Write(buffer, (int) offset, (int) size);
                    Program.Servers[serverId].Session!.SendAsync(memory2.ToArray());
                    break;
                case SessionState.Idle:
                    try {
                        switch (reader.ReadByte()) {
                            case 0x00: // Host a server
                                var token = Convert.ToHexString(reader.ReadBytes(16));
                                var server = Program.Servers.Values.FirstOrDefault(x => x.Token == token);
                                if (server == null) {
                                    writer.Write(false); // Just why
                                    SendAsync(memory2.ToArray());
                                    Disconnect();
                                    break;
                                }
                            
                                serverId = server.ServerId;
                                writer.Write(true); SendAsync(memory2.ToArray());
                                _sessionState = SessionState.HostingServer;
                                server.Session = this;
                                break;
                            case 0x01: // Connect to a server
                                serverId = reader.ReadInt32();
                                if (!Program.Servers.ContainsKey(serverId)) {
                                    writer.Write(false); // Server doesn't exist
                                    writer.Write((byte) 0x00);
                                    SendAsync(memory2.ToArray());
                                    break;
                                }
                            
                                if (Program.PeerPool[Program.Servers[serverId].PeerId]
                                        .Id.ToString() != Server.Id.ToString()) {
                                    writer.Write(false); // Wrong peer
                                    writer.Write((byte) 0x01);
                                    SendAsync(memory2.ToArray());
                                    break;
                                }

                                if (Program.Servers[serverId].Session == null) {
                                    writer.Write(false); // Server didn't start yet
                                    writer.Write((byte) 0x02);
                                    SendAsync(memory2.ToArray());
                                    break;
                                }

                                var data = reader.ReadBytes(reader.ReadInt32());
                                if (!Security.Authenticate(data)) {
                                    writer.Write(false); // Authentication failed
                                    writer.Write((byte) 0x03);
                                    SendAsync(memory2.ToArray());
                                    break;
                                }

                                Program.Servers[serverId].Players.Add(Id.ToString(), this);
                                Program.PeerPool[Program.Servers[serverId].PeerId].PeopleConnected++;
                                writer.Write(true); SendAsync(memory2.ToArray());
                                _sessionState = SessionState.ConnectedToServer;

                                memory2.Position = 0;
                                writer.Write((byte) 0x01); 
                                writer.Write((byte) 0x00);
                                writer.Write(Id.ToString());
                                writer.Write(data.Length);
                                writer.Write(data);
                                Program.Servers[serverId].Session!.SendAsync(memory2.ToArray());
                                break;
                        }
                    } catch (EndOfStreamException) {
                        Disconnect();
                    }

                    break;
            }
        } catch (Exception e) {
            Program.Logger.Error("Unknown error occured (peer session): {0}", e);
            Disconnect();
        }
    }

    protected override void OnError(SocketError error)
        => Program.Logger.Error("Socket error occured (peer session): {0}", error);
}