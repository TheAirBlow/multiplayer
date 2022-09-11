using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using NetCoreServer;

namespace MultiplayerP2P.Main;

public class MainSession : TcpSession
{
    public MainSession(TcpServer server) : base(server) { }
    private bool canCreateServer = true;
    private bool verification;
    private int serverId = -1; 
    private byte[] question;

    protected override void OnConnected()
    {
        // TODO: Send list of servers
    }

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        using var memory1 = new MemoryStream(buffer);
        using var reader = new BinaryReader(memory1);
        using var memory2 = new MemoryStream();
        using var writer = new BinaryWriter(memory2);
        try {
            switch (reader.ReadByte()) {
                case 0x00: // Create server
                    if (!canCreateServer || verification) {
                        writer.Write(false); // Just why
                        Send(memory2.ToArray());
                        break;
                    }

                    question = Security.GenerateQuestion();
                    Send(question); verification = true;
                    break;
                case 0x01: // Delete server
                    if (serverId == -1) {
                        writer.Write(false); // Just why
                        Send(memory2.ToArray());
                        break;
                    }

                    if (Program.ServerToStopwatch.ContainsKey(serverId)) {
                        Program.ServerToStopwatch.Remove(serverId);
                        Program.ServerToSession.Remove(serverId);
                    }
                    Program.ServerToPeer.Remove(serverId);
                    Program.Tokens.Remove(serverId);
                    
                    writer.Write(true); // Success
                    Send(memory2.ToArray());

                    memory2.Position = 0;
                    writer.Write(0x00); writer.Write(serverId);
                    Program.MainServer.Multicast(memory2.ToArray());
                    break;
                case 0x02: // Get server port
                    var id = reader.ReadInt32();
                    if (Program.ServerToPeer.ContainsKey(id)) {
                        writer.Write(true); // Server exists
                        writer.Write(Program.ServerToPeer[id]);
                        Send(memory2.ToArray());
                        break;
                    }

                    writer.Write(false); // Server doesn't exist
                    Send(memory2.ToArray());
                    break;
                default:
                    if (verification && Security.VerifyAnswer(question, buffer)) {
                        var leastLoadedPeer = Program.PeerPool.OrderBy(x => x.PeopleConnected).ToList()[0];
                        var token = RandomNumberGenerator.GetBytes(32);
                        var index = Program.PeerPool.IndexOf(leastLoadedPeer);
                        serverId = Program.ServerToPeer.Count;
                        Program.ServerToStopwatch.Add(serverId, new Stopwatch());
                        Program.ServerToStopwatch[serverId].Start();
                        Program.ServerToPeer.Add(serverId, index);
                        Program.Tokens.Add(serverId, token);
                        canCreateServer = false;
                       
                        writer.Write(0x01);
                        writer.Write(serverId);
                        Program.MainServer.Multicast(memory2.ToArray());

                        memory2.Position = 0;
                        writer.Write(true);
                        writer.Write(32);
                        writer.Write(token);
                        Send(memory2.ToArray());
                        break;
                    } 
                    
                    Disconnect();
                    break;
            }
        } catch (EndOfStreamException) {
            Disconnect();
        }
    }

    protected override void OnError(SocketError error)
        => Program.Logger.Error("Unknown error occured (notifier session): {0}", error);
}