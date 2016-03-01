using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WireTalk.HTTP;
using WireTalk.Routing;

namespace WireTalk
{
    public class ApplicationServer
    {
        TcpListener _tcpServer;
        CancellationToken _globalCancelationToken;
        Router _router;
        Parser _parser;

        public ApplicationServer(IPAddress bindIp, int port)
        {
            _tcpServer = new TcpListener(bindIp, port);
            _parser = new Parser();
            _globalCancelationToken = new CancellationToken();
        }

        public void MountRouter(Router router)
        {
            _router = router;
        }

        public Task<Exception> Start()
        {
            _tcpServer.Start();

            return Run();
        }

        protected async Task<Exception> Run()
        {
            
            while (!_globalCancelationToken.IsCancellationRequested)
            {
                TcpClient client = await _tcpServer.AcceptTcpClientAsync();
                NetworkStream ns = client.GetStream();

                ParserState parserState = await GetParsedState(client, ns);

                Request request = new Request();
                request.Headers = parserState.Headers;
                request.Method = parserState.Method;
                request.Path = parserState.RequestURL;
                request.Params = new Dictionary<string, string>();

                Response response = await _router.Route(request);

                await SendResponse(ns, response);

                client.Close();
            }

            return new OperationCanceledException();
        }

        protected async Task SendResponse(NetworkStream ns, Response response)
        {
            response.Headers.Add("Connection", "close");
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            response.Headers.Add("Date", DateTime.Now.ToUniversalTime().ToString("r"));
            response.Headers.Add("Content-Length", Encoding.UTF8.GetByteCount(response.Data).ToString());

            StringBuilder head = new StringBuilder();
            head.Append("HTTP/1.1 ");
            head.Append(response.Status);
            head.Append(" OK \r\n");
            foreach(KeyValuePair<string, string> kvp in response.Headers)
            {
                head.AppendFormat("{0}: {1}\r\n", kvp.Key, kvp.Value);
            }
            head.Append("\r\n");
            head.Append(response.Data);

            byte[] data = Encoding.UTF8.GetBytes(head.ToString());
            await ns.WriteAsync(data, 0, data.Length);
        }

        protected async Task<ParserState> GetParsedState(TcpClient client, NetworkStream ns)
        {
            HTTP.ParserState parserState = new HTTP.ParserState();

            bool needMore = true;
            int parseIndex = -1;

            while (needMore)
            {
                if (ns.DataAvailable)
                {
                    int dataToRead = client.Available;
                    byte[] buffer = new byte[dataToRead];
                    int actual = await ns.ReadAsync(buffer, 0, dataToRead);

                    parseIndex = _parser.ParseBuffer(parserState, buffer);
                    needMore = parseIndex == -1;
                }
                else
                {
                    await Task.Yield();
                }
            }
            return parserState;
        }
    }
}
