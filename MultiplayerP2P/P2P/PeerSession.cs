using System.Net.Sockets;
using NetCoreServer;

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
                Program.ServerToInformation.Remove(serverId);
                Program.ServerToStopwatch.Remove(serverId);
                Program.ServerToSession.Remove(serverId);
                Program.ServerToPeer.Remove(serverId);
                Program.Tokens.Remove(serverId);
            
                using (var memory = new MemoryStream())
                using (var writer = new BinaryWriter(memory)) {
                    writer.Write((byte) 0x00); writer.Write(serverId);
                    Program.MainServer.Multicast(memory.ToArray());
                }
                break;
            case SessionState.ConnectedToServer:
                if (!Program.ServerToSession.ContainsKey(serverId)) {
                    Disconnect();
                    break;
                }

                using (var memory = new MemoryStream())
                using (var writer = new BinaryWriter(memory)) {
                    writer.Write((byte) 0x01); 
                    writer.Write((byte) 0x01);
                    writer.Write(Id.ToString());
                    Program.ServerToSession[serverId].Send(memory.ToArray());
                }
                break;
        }
    }

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        using var memory1 = new MemoryStream(buffer);
        using var reader = new BinaryReader(memory1);
        using var memory2 = new MemoryStream();
        using var writer = new BinaryWriter(memory2);
        switch (_sessionState) {
            case SessionState.HostingServer:
                try {
                    switch (reader.ReadByte()) {
                        case 0x00: // Disconnect a user
                            var id1 = new Guid(reader.ReadString());
                            Server.FindSession(id1).Disconnect();
                            break;
                        case 0x01: // Send a message to user
                            var id2 = new Guid(reader.ReadString());
                            Server.FindSession(id2).Send(reader.ReadBytes(reader.ReadInt32()));
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
                writer.Write(0x00);
                writer.Write(Id.ToString());
                writer.Write(buffer);
                Program.ServerToSession[serverId].Send(memory2.ToArray());
                break;
            case SessionState.Idle:
                try {
                    switch (reader.ReadByte()) {
                        case 0x00: // Host a server
                            var token = Convert.ToHexString(reader.ReadBytes(16));
                            if (!Program.Tokens.ContainsValue(token)) {
                                writer.Write(false); // Just why
                                Send(memory2.ToArray());
                                Disconnect();
                                break;
                            }

                            var index = Program.Tokens.Values.ToList().IndexOf(token);
                            serverId = Program.Tokens.Keys.ToList()[index];
                            writer.Write(true); Send(memory2.ToArray());
                            _sessionState = SessionState.HostingServer;
                            Program.ServerToSession.Add(serverId, this);
                            break;
                        case 0x01: // Connect to a server
                            serverId = reader.ReadInt32();
                            if (Program.PeerPool[Program.ServerToPeer[serverId]].Id != Server.Id) {
                                writer.Write(false); // Wrong peer
                                writer.Write((byte) 0x00);
                                Send(memory2.ToArray());
                                break;
                            }
                            
                            if (!Program.ServerToPeer.ContainsKey(serverId)) {
                                writer.Write(false); // Server doesn't exist
                                writer.Write((byte) 0x01);
                                Send(memory2.ToArray());
                                break;
                            }
                            
                            if (!Program.ServerToSession.ContainsKey(serverId)) {
                                writer.Write(false); // Server didn't start yet
                                writer.Write((byte) 0x02);
                                Send(memory2.ToArray());
                                break;
                            }

                            var data = reader.ReadBytes(reader.ReadInt32());
                            if (!Security.Authenticate(data)) {
                                writer.Write(false); // Authentication failed
                                writer.Write((byte) 0x03);
                                Send(memory2.ToArray());
                                break;
                            }

                            writer.Write(true); Send(memory2.ToArray());
                            _sessionState = SessionState.ConnectedToServer;

                            memory2.Position = 0;
                            writer.Write((byte) 0x01); 
                            writer.Write((byte) 0x00);
                            writer.Write(Id.ToString());
                            writer.Write(data.Length);
                            writer.Write(data);
                            Program.ServerToSession[serverId].Send(memory2.ToArray());
                            break;
                    }
                } catch (EndOfStreamException) {
                    Disconnect();
                }

                break;
        }
    }

    protected override void OnError(SocketError error)
        => Program.Logger.Error("Unknown error occured (peer session): {0}", error);
}