using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WireTalk.HTTP;
using WireTalk.Routing;

namespace WireTalk.WebSocket
{
    public class Websocket
    {
        TcpClient _client;
        NetworkStream _ns;
        ParserState _ps;

        public Websocket(TcpClient client, NetworkStream ns, ParserState state)
        {
            _client = client;
            _ns = ns;
            _ps = state;
        }

        public async Task Negotiate()
        {
            if(_ps.Headers["Sec-WebSocket-Version"] != "13")
            {
                throw new ApplicationException("Invalid version");
            }

            if(_ps.Headers["Sec-WebSocket-Protocol"] != "wiretalk")
            {
                throw new ApplicationException("Invalid protocol");
            
            }

            string webSocketKey = _ps.Headers["Sec-WebSocket-Key"];
            webSocketKey += "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";


            webSocketKey = Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(webSocketKey)));
            Response handshake = new Response();
            handshake.Status = 101;
            handshake.Headers.Add("Upgrade", "websocket");
            handshake.Headers.Add("Connection", "Upgrade");
            handshake.Headers.Add("Sec-WebSocket-Accept", webSocketKey);
            handshake.Headers.Add("Sec-WebSocket-Protocol", "wiretalk");

            await Helpers.SendResponse(_ns, handshake);
            Run();
        }

        protected async Task Run()
        {
            StringBuilder message = new StringBuilder();
            bool closing = false;
            bool more = false;
            while(!closing)
            {
                byte[] buffer = new byte[32 * 1024];
                int actualData = await _ns.ReadAsync(buffer, 0, 32 * 1024);
                    

                byte opcode = (byte)(buffer[0] & 0xF);
                if (opcode == 0 || opcode == 1)
                {
                    int readOffset = 2;

                    int length = buffer[1] & 0x7F;
                    if (length == 126)
                    {
                        readOffset += 2;
                        length = BitConverter.ToUInt16(buffer.Skip(2).Take(2).Reverse().ToArray(), 0);
                    }
                    else if (length == 127)
                    {
                        //throw new InvalidOperationException("Won't read 64-bit long message");
                        length = (int)BitConverter.ToUInt64(buffer.Skip(2).Take(8).Reverse().ToArray(), 0);
                    }

                    int dataStart = readOffset + 4;

                    for (int i = 0; i < length; i++)
                    {
                        buffer[dataStart + i] ^= buffer[readOffset + i % 4];
                    }

                    message.Append(Encoding.UTF8.GetString(buffer, dataStart, length));
                    if((buffer[0] & 0x80) != 0)
                    {
                        more = false;
                    }
                    else
                    {
                        more = true;
                    }

                    if(!more)
                    {
                        await DispatchWebMessage(message.ToString());
                        message.Clear();
                    }
                }
                else if (opcode == 0x8)
                {
                    closing = true;
                    await SendControlFrame(0x8);
                    await _ns.FlushAsync();
                    _client.Close();
                }
                else if(opcode == 0xA)
                {
                    Console.WriteLine("PONG");
                }
                else if(opcode == 0x9)
                {
                    await SendControlFrame(0xA);
                }
                
            }
        }

        protected async Task SendControlFrame(byte opcode)
        {
            byte[] data = new byte[2];
            data[1] = 0;
            data[0] = (byte)(1<<8 | (opcode));
            await _ns.WriteAsync(data, 0, 2);
        }

        protected async Task DispatchWebMessage(string message)
        {

        }
    }
}
