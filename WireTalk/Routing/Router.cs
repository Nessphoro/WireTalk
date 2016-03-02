using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WireTalk.Routing
{
    public delegate Task<Response> RoutingDelegate(Request request);
    public enum MountPoint
    {
        Function, Router
    }

    public class RoutingInfo
    {
        public MountPoint MountType;
        public Regex Expression;
        public List<string> Parameters;

        public RoutingDelegate Callback;
        public Router Router;

        public string Method;
    }
    public class Router
    {
        static Response NotFound = new Response() { Status = 404, Headers = new Dictionary<string, string>(), Data = null };

        Dictionary<string, List<RoutingInfo>> routings;
        public Router()
        {
            routings = new Dictionary<string, List<RoutingInfo>>();
            routings.Add("GET", new List<RoutingInfo>());
            routings.Add("POST", new List<RoutingInfo>());
            routings.Add("HEAD", new List<RoutingInfo>());
        }

        private RoutingInfo getRountingInfo(string path, bool terminator = true)
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
                if (terminator)
                {
                    regularExpression.Append("?$");
                }
      
            }
            else
            {
                if (terminator)
                {
                    regularExpression.Append("/?$");
                }
                else
                {
                    regularExpression.Append("/?");
                }
            }

            return new RoutingInfo() { Expression = new Regex(regularExpression.ToString(), RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase), Parameters = parameters };
        }

        public void Get(string path, RoutingDelegate callback)
        {
            RoutingInfo rinfo = getRountingInfo(path);
            rinfo.Callback = callback;
            rinfo.Method = "GET";

            routings["GET"].Add(rinfo);
        }

        public void Mount(string path, Router router)
        {
            RoutingInfo rinfo = getRountingInfo(path, false);
            rinfo.MountType = MountPoint.Router;
            rinfo.Router = router;
            rinfo.Method = "GET";

            routings["GET"].Add(rinfo);
        }

        public Task<Response> Route(Request request, string mt)
        {
            List<RoutingInfo> rinfos = routings[request.Method];
            foreach(RoutingInfo routing in rinfos)
            {
                Match match = routing.Expression.Match(mt);
                if(match.Success)
                {
                    foreach(string param in routing.Parameters)
                    {
                        request.Params.Add(param, match.Groups[param].Value);
                    }

                    if (routing.MountType == MountPoint.Function)
                        return routing.Callback(request);
                    else
                        return routing.Router.Route(request, mt.Substring(match.Length));
                }
                
            }

            return Task.FromResult(NotFound);
        }
    }
}
