using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WireTalk.Routing
{
    public class Request
    {
        public string Path;
        public string Method;
        public Dictionary<string, string> Headers;
        public Dictionary<string, string> Params = new Dictionary<string,string>();
    }
}
