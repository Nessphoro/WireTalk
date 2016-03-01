using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WireTalk.Routing;

namespace WireTalk.Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            ApplicationServer appServer = new ApplicationServer(IPAddress.Any, 8080);
            Router router = new Router();
            appServer.MountRouter(router);
            router.Get("/", index);
            router.Get("/:habibi", habibi);
            appServer.Start().Wait();
        }

        static async Task<Response> index(Request request)
        {
            Response r = new Response();
            r.Status = 200;
            r.Data = "Hello world";

            return r;
        }
        static Random rnd = new Random();
        static async Task<Response> habibi(Request request)
        {
            Response r = new Response();
            r.Status = 200;
            r.Data = request.Params["habibi"];
            await Task.Delay(rnd.Next(10, 1000));
            return r;
        }
    }
}
