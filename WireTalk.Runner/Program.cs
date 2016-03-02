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
            Router router2 = new Router();

            appServer.MountRouter(router);
            router.Get("/", index);
            router.Mount("/:habibi/", router2);

            router2.Get("/", index);

            appServer.Start().Wait();
        }

        static async Task<Response> index(Request request)
        {
            Response r = new Response();
            r.Status = 200;
            r.Data = "Hello world";

            return r;
        }
    }
}
