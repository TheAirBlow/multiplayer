using System.Net.Sockets;
using NetCoreServer;

namespace MultiplayerP2P.Main;

public class MainSession : TcpSession
{
    public MainSession(TcpServer server) : base(server) { }
    private bool _canCreateServer = true;
    private byte[] _serverInformation = null!;
    private bool _verification;
    private int _serverId = -1; 
    private byte[] _question = null!;
    
    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        using var memory1 = new MemoryStream(buffer);
        using var reader = new BinaryReader(memory1);
        using var memory2 = new MemoryStream();
        using var writer = new BinaryWriter(memory2);
        try {
            if (_verification) {
                if (Security.VerifyAnswer(_question, buffer)) {
                    var leastLoadedPeer = Program.PeerPool.OrderBy(x => x.PeopleConnected).ToList()[0];
                    var index = Program.PeerPool.IndexOf(leastLoadedPeer);
                    _serverId = Program.Servers.Count;
                    Program.Servers.Add(_serverId, new Server());
                    Program.Servers[_serverId].Information = _serverInformation;
                    Program.Servers[_serverId].ServerId = _serverId;
                    Program.Servers[_serverId].PeerId = index;
                    Program.Servers[_serverId].Timer.Start();
                    _canCreateServer = false;
                       
                    writer.Write(true);
                    writer.Write(Convert.FromHexString(Program.Servers[_serverId].Token));
                    writer.Write(leastLoadedPeer.Port);
                    SendAsync(memory2.ToArray());

                    memory2.Position = 0;
                    writer.Write((byte) 0x01);
                    writer.Write((byte) 0x01);
                    writer.Write(_serverId);
                    Server.Multicast(memory2.ToArray());
                    Program.Logger.Information($"A user created a server (ID {_serverId}) on peer {index}");
                    return;
                }
                
                writer.Write(false);
                SendAsync(memory2.ToArray());
                Disconnect();
            }

            switch (reader.ReadByte()) {
                case 0x00: // Create server
                    if (!_canCreateServer || _verification) {
                        writer.Write((byte) 0x00);
                        writer.Write(false); // Just why
                        SendAsync(memory2.ToArray());
                        break;
                    }

                    _serverInformation = reader.ReadBytes(reader.ReadInt32());
                    _question = Security.GenerateQuestion();
                    writer.Write((byte) 0x00);
                    writer.Write(_question);
                    SendAsync(memory2.ToArray()); 
                    _verification = true;
                    break;
                case 0x01: // Get server port
                    var id = reader.ReadInt32();
                    if (Program.Servers.ContainsKey(id)) {
                        writer.Write((byte) 0x00);
                        writer.Write(true); // Server exists
                        writer.Write(Program.PeerPool[Program.Servers[id].PeerId].Port);
                        SendAsync(memory2.ToArray());
                        break;
                    }

                    writer.Write((byte) 0x00);
                    writer.Write(false); // Server doesn't exist
                    SendAsync(memory2.ToArray());
                    break;
            }
        } catch (EndOfStreamException) {
            Disconnect();
        } catch (Exception e) {
            Program.Logger.Error("Unknown error occured (main session): {0}", e);
            Disconnect();
        }
    }

    protected override void OnError(SocketError error)
        => Program.Logger.Error("Socket error occured (main session): {0}", error);
}