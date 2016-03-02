using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WireTalk.HTTP
{
    public class ParserState
    {
        public string Method;
        public string RequestURL;
        public string QueryURL;
        public string Version;

        public Dictionary<string, string> Headers;

        public string CurrentHeader;
        public StringBuilder Buffer;

        public ParserAutomatonState AutomatonState;

        public Exception Error;

        public ParserState()
        {
            Error = new Exception();
            Buffer = new StringBuilder(128);
            Headers = new Dictionary<string, string>();
            QueryURL = "";
            AutomatonState = ParserAutomatonState.Method;
        }
    }
}
