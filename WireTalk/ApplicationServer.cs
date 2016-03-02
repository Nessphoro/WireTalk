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
            while(!_globalCancelationToken.IsCancellationRequested)
            {
                TcpClient client = await _tcpServer.AcceptTcpClientAsync();
                DispatchClient(client);
            }

            return new OperationCanceledException();
        }

        protected async Task DispatchClient(TcpClient client)
        {
            try
            {
                client.Client.NoDelay = true;
                NetworkStream ns = client.GetStream();
                ParserState parserState = await GetParsedState(client, ns);
                bool close = true;
                if (parserState.Error.GetType() != typeof(TimeoutException))
                {
                    if(parserState.Headers["Connection"] == "upgrade")
                    {
                        if (parserState.Headers["Upgrade"] == "websocket")
                        {
                            Console.WriteLine("Negotiating websocket");
                            WebSocket.Websocket ws = new WebSocket.Websocket(client, ns, parserState);
                            try
                            {
                                await ws.Negotiate();
                                close = false;
                                return;
                            }
                            catch
                            {
                                close = true;
                            }
                        }
                    }

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(">>> {0} {1} {2}", parserState.Method, parserState.RequestURL, parserState.QueryURL);
                    foreach(var kvp in parserState.Headers)
                    {
                        Console.WriteLine("\t{0}: {1}", kvp.Key, kvp.Value);
                    }

                    Request request = new Request();


                    request.Headers = parserState.Headers;
                    request.Method = parserState.Method;
                    request.Path = parserState.RequestURL;
                    request.Params = new Dictionary<string, string>();

                    Response response = await _router.Route(request, request.Path);
                    response.Headers.Add("Connection", "close");
                    await Helpers.SendResponse(ns, response);
                }

                if(close)
                    client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        

        protected async Task<ParserState> GetParsedState(TcpClient client, NetworkStream ns)
        {
            DateTime start = DateTime.Now;
            HTTP.ParserState parserState = new HTTP.ParserState();
            bool needMore = true;
            int parseIndex = -1;

            while (needMore)
            {
                if ((DateTime.Now - start).TotalSeconds > 5)
                {
                    parserState.Error = new TimeoutException();
                    return parserState;
                }
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
