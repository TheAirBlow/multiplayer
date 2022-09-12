using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using NetCoreServer;

namespace MultiplayerP2P.Main;

public class MainSession : TcpSession
{
    public MainSession(TcpServer server) : base(server) { }
    private bool canCreateServer = true;
    private byte[] serverInformation;
    private bool verification;
    private int serverId = -1; 
    private byte[] question;
    
    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        using var memory1 = new MemoryStream(buffer);
        using var reader = new BinaryReader(memory1);
        using var memory2 = new MemoryStream();
        using var writer = new BinaryWriter(memory2);
        try {
            if (verification) {
                if (Security.VerifyAnswer(question, buffer)) {
                    var leastLoadedPeer = Program.PeerPool.OrderBy(x => x.PeopleConnected).ToList()[0];
                    var index = Program.PeerPool.IndexOf(leastLoadedPeer);
                    serverId = Program.Servers.Count;
                    Program.Servers.Add(serverId, new Server());
                    Program.Servers[serverId].Information = serverInformation;
                    Program.Servers[serverId].ServerId = serverId;
                    Program.Servers[serverId].PeerId = index;
                    Program.Servers[serverId].Timer.Start();
                    canCreateServer = false;
                       
                    writer.Write(true);
                    writer.Write(Convert.FromHexString(Program.Servers[serverId].Token));
                    writer.Write(leastLoadedPeer.Port);
                    SendAsync(memory2.ToArray());

                    memory2.Position = 0;
                    writer.Write((byte) 0x01);
                    writer.Write(serverId);
                    Server.Multicast(memory2.ToArray());
                    Program.Logger.Information($"A user created a server (ID {serverId}) on peer {index}");
                    return;
                }
                
                writer.Write(false);
                SendAsync(memory2.ToArray());
                Disconnect();
            }

            switch (reader.ReadByte()) {
                case 0x00: // Create server
                    if (!canCreateServer || verification) {
                        writer.Write(false); // Just why
                        SendAsync(memory2.ToArray());
                        break;
                    }

                    serverInformation = reader.ReadBytes(reader.ReadInt32());
                    question = Security.GenerateQuestion();
                    SendAsync(question); verification = true;
                    break;
                case 0x01: // Delete server
                    if (serverId == -1) {
                        writer.Write(false); // Just why
                        SendAsync(memory2.ToArray());
                        break;
                    }

                    Program.Servers.Remove(serverId);
                    writer.Write(true); // Success
                    SendAsync(memory2.ToArray());

                    memory2.Position = 0;
                    writer.Write(0x00); writer.Write(serverId);
                    Server.Multicast(memory2.ToArray());
                    break;
                case 0x02: // Get server port
                    var id = reader.ReadInt32();
                    if (Program.Servers.ContainsKey(id)) {
                        writer.Write(true); // Server exists
                        writer.Write(Program.PeerPool[Program.Servers[id].PeerId].Port);
                        SendAsync(memory2.ToArray());
                        break;
                    }

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