using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WireTalk.Routing
{
    public class Response
    {
        public int Status;
        public Dictionary<string, string> Headers;
        public string Data;

        public Response()
        {
            Status = 500;
            Headers = new Dictionary<string, string>();
            Data = "";
        }
    }
}
