using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using WireTalk.Routing;

namespace WireTalk.HTTP
{
    public class Helpers
    {
        public static Dictionary<int, string> HttpMessage = new Dictionary<int, string>() { { 200, "OK" }, { 404, "Not Found" }, { 101, "Switching Protocols" } };
        public static async Task SendResponse(NetworkStream ns, Response response)
        {
            if (response.Data != null)
            {
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                response.Headers.Add("Date", DateTime.Now.ToUniversalTime().ToString("r"));
                response.Headers.Add("Content-Length", Encoding.UTF8.GetByteCount(response.Data).ToString());
            }

            StringBuilder head = new StringBuilder();
            head.Append("HTTP/1.1 ");
            head.Append(response.Status);
            head.AppendFormat(" {0} \r\n", Helpers.HttpMessage[response.Status]);
            foreach (KeyValuePair<string, string> kvp in response.Headers)
            {
                head.AppendFormat("{0}: {1}\r\n", kvp.Key, kvp.Value);
            }
            head.Append("\r\n");
            head.Append(response.Data);

            byte[] data = Encoding.UTF8.GetBytes(head.ToString());
            await ns.WriteAsync(data, 0, data.Length);
        }
    }
}
