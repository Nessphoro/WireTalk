using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WireTalk.Routing
{
    public delegate Task<Response> RoutingDelegate(Request request);
    public class RoutingInfo
    {
        public Regex Expression;
        public List<string> Parameters;
        public RoutingDelegate Callback;
        public string Method;
    }
    public class Router
    {
        Dictionary<string, List<RoutingInfo>> routings;
        public Router()
        {
            routings = new Dictionary<string, List<RoutingInfo>>();
            routings.Add("GET", new List<RoutingInfo>());
            routings.Add("POST", new List<RoutingInfo>());
            routings.Add("HEAD", new List<RoutingInfo>());
        }

        private RoutingInfo getRountingInfo(string path)
        {
            StringBuilder regularExpression = new StringBuilder(path.Length * 2);
            List<string> parameters = new List<string>();

            regularExpression.Append("^");
            if (path[0] != '/')
                regularExpression.Append('/');

            bool regexpMode = false;
            StringBuilder nameBuffer = new StringBuilder();
            for (int i = 0; i < path.Length; i++)
            {
                if (regexpMode)
                {
                    if (path[i] == '/')
                    {
                        parameters.Add(nameBuffer.ToString());
                        regularExpression.AppendFormat("(?<{0}>.+?)/", nameBuffer.ToString());
                        nameBuffer.Clear();
                        regexpMode = false;
                    }
                    else
                    {
                        nameBuffer.Append(path[i]);
                    }
                }
                else
                {
                    if (path[i] == ':')
                    {
                        regexpMode = true;
                    }
                    else
                    {
                        regularExpression.Append(path[i]);
                    }
                }
            }

            if (regexpMode)
            {
                parameters.Add(nameBuffer.ToString());
                regularExpression.AppendFormat("(?<{0}>.+?)/", nameBuffer.ToString());
                nameBuffer.Clear();
                regexpMode = false;
            }

            if (regularExpression.ToString().Last() == '/')
            {
                regularExpression.Append("?$");
            }
            else
            {
                regularExpression.Append("/?$");
            }

            return new RoutingInfo() { Expression = new Regex(regularExpression.ToString(), RegexOptions.Compiled | RegexOptions.ExplicitCapture), Parameters = parameters };
        }

        public void Get(string path, RoutingDelegate callback)
        {
            RoutingInfo rinfo = getRountingInfo(path);
            rinfo.Callback = callback;
            rinfo.Method = "GET";

            routings["GET"].Add(rinfo);
        }

        public Task<Response> Route(Request request)
        {
            List<RoutingInfo> rinfos = routings[request.Method];
            foreach(RoutingInfo routing in rinfos)
            {
                Match match = routing.Expression.Match(request.Path);
                if(match.Success)
                {
                    request.Params = new Dictionary<string, string>();
                    foreach(string param in routing.Parameters)
                    {
                        request.Params.Add(param, match.Groups[param].Value);
                    }
                    return routing.Callback(request);
                }
                
            }

            return null;
        }
    }
}
