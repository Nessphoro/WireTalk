using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace WireTalk.HTTP
{
    public enum ParserAutomatonState
    {
        Method, BeforeURI, URI, QueryStart, Query, AfterURI,
        HTTP_H, HTTP_HT, HTTP_HTT, HTTP_HTTP, HTTP_SLASH, HTTP_1, HTTP_Dot,HTTP_Dot1,
        HTTP_CR, HTTP_LF, HeaderStart, HeaderMiddle, HeaderComment, HeaderQuote, HeaderEnd, Header_CR, Header_LF,
        END_CR, END_LF
    }
    //TODO: Errors
    public class Parser
    {
        public int ParseBuffer(ParserState state, byte[] buffer)
        {
            for(int i=0;i<buffer.Length; i++)
            {
                switch (state.AutomatonState)
                {
                    case ParserAutomatonState.Method:
                        if (buffer[i] != ' ')
                        {
                            state.Buffer.Append((char)buffer[i]);
                        }
                        else
                        {
                            state.Method = state.Buffer.ToString();
                            state.Buffer.Clear();
                            state.AutomatonState = ParserAutomatonState.BeforeURI;
                        }
                        break;
                    case ParserAutomatonState.BeforeURI:
                        if (buffer[i] != ' ')
                        {
                            state.Buffer.Append((char)buffer[i]);
                            state.AutomatonState = ParserAutomatonState.URI;
                        }
                        break;
                    case ParserAutomatonState.URI:
                        if (buffer[i] != ' ' && buffer[i] != '?')
                        {
                            state.Buffer.Append((char)buffer[i]);
                        }
                        else if (buffer[i] == '?')
                        {
                            state.RequestURL = state.Buffer.ToString();
                            state.AutomatonState = ParserAutomatonState.QueryStart;
                            state.Buffer.Clear();
                        }
                        else
                        {
                            state.RequestURL = state.Buffer.ToString();
                            state.AutomatonState = ParserAutomatonState.AfterURI;
                            state.Buffer.Clear();
                        }
                        break;
                    case ParserAutomatonState.QueryStart:
                        if (buffer[i] != ' ')
                        {
                            state.Buffer.Append((char)buffer[i]);
                            state.AutomatonState = ParserAutomatonState.Query;
                        }
                        else
                        {
                            state.QueryURL = "";
                            state.AutomatonState = ParserAutomatonState.AfterURI;
                        }
                        break;
                    case ParserAutomatonState.Query:
                        if (buffer[i] != ' ')
                        {
                            state.Buffer.Append((char)buffer[i]);
                        }
                        else
                        {
                            state.QueryURL = state.Buffer.ToString();
                            state.Buffer.Clear();
                            state.AutomatonState = ParserAutomatonState.AfterURI;
                        }
                        break;
                    case ParserAutomatonState.AfterURI:
                        if (buffer[i] == 'H')
                        {
                            state.Buffer.Append((char)buffer[i]);
                            state.AutomatonState = ParserAutomatonState.HTTP_H;
                        }
                        break;
                    case ParserAutomatonState.HTTP_H:
                        if (buffer[i] == 'T')
                        {
                            state.Buffer.Append((char)buffer[i]);
                            state.AutomatonState = ParserAutomatonState.HTTP_HT;
                        }
                        break;
                    case ParserAutomatonState.HTTP_HT:
                        if (buffer[i] == 'T')
                        {
                            state.Buffer.Append((char)buffer[i]);
                            state.AutomatonState = ParserAutomatonState.HTTP_HTT;
                        }
                        break;
                    case ParserAutomatonState.HTTP_HTT:
                        if (buffer[i] == 'P')
                        {
                            state.Buffer.Append((char)buffer[i]);
                            state.AutomatonState = ParserAutomatonState.HTTP_HTTP;
                        }
                        break;
                    case ParserAutomatonState.HTTP_HTTP:
                        if (buffer[i] == '/')
                        {
                            state.Buffer.Append((char)buffer[i]);
                            state.AutomatonState = ParserAutomatonState.HTTP_SLASH;
                        }
                        break;
                    case ParserAutomatonState.HTTP_SLASH:
                        if (buffer[i] == '1')
                        {
                            state.Buffer.Append((char)buffer[i]);
                            state.AutomatonState = ParserAutomatonState.HTTP_1;
                        }
                        break;
                    case ParserAutomatonState.HTTP_1:
                        if (buffer[i] == '.')
                        {
                            state.Buffer.Append((char)buffer[i]);
                            state.AutomatonState = ParserAutomatonState.HTTP_Dot;
                        }
                        break;
                    case ParserAutomatonState.HTTP_Dot:
                        if (buffer[i] == '1')
                        {
                            state.Buffer.Append((char)buffer[i]);
                            state.Version = state.Buffer.ToString();
                            state.Buffer.Clear();
                            state.AutomatonState = ParserAutomatonState.HTTP_Dot1;

                        }
                        break;
                    case ParserAutomatonState.HTTP_Dot1:
                        if (buffer[i] == '\r')
                        {
                            state.AutomatonState = ParserAutomatonState.HTTP_CR;
                        }
                        break;
                    case ParserAutomatonState.HTTP_CR:
                        if (buffer[i] == '\n')
                        {
                            state.AutomatonState = ParserAutomatonState.HTTP_LF;
                        }
                        break;
                    case ParserAutomatonState.HTTP_LF:
                        state.Buffer.Append((char)buffer[i]);
                        state.AutomatonState = ParserAutomatonState.HeaderStart;
                        break;
                    case ParserAutomatonState.HeaderStart:
                        if (buffer[i] == '\r')
                        {
                            state.AutomatonState = ParserAutomatonState.END_CR;
                        }
                        else if (buffer[i] != ':')
                        {
                            state.Buffer.Append((char)buffer[i]);
                        }
                        else
                        {
                            state.CurrentHeader = state.Buffer.ToString();
                            state.Buffer.Clear();
                            state.AutomatonState = ParserAutomatonState.HeaderMiddle;
                        }
                        break;
                    case ParserAutomatonState.HeaderMiddle:
                        if (buffer[i] == '(')
                        {
                            state.AutomatonState = ParserAutomatonState.HeaderComment;
                        }
                        else if (buffer[i] == '"')
                        {
                            state.AutomatonState = ParserAutomatonState.HeaderQuote;
                        }
                        else if (buffer[i] == '\r')
                        {
                            state.Headers[state.CurrentHeader] = "";
                            state.AutomatonState = ParserAutomatonState.Header_CR;
                        }
                        else if (buffer[i] != ' ')
                        {
                            state.Buffer.Append((char)buffer[i]);
                            state.AutomatonState = ParserAutomatonState.HeaderEnd;
                        }
                        break;
                    case ParserAutomatonState.HeaderComment:
                        if (buffer[i] == ')')
                        {
                            state.AutomatonState = ParserAutomatonState.HeaderEnd;
                        }
                        break;
                    case ParserAutomatonState.HeaderQuote:
                        if (buffer[i] == '"')
                        {
                            state.AutomatonState = ParserAutomatonState.HeaderEnd;
                        }
                        else
                        {
                            state.Buffer.Append((char)buffer[i]);
                        }
                        break;
                    case ParserAutomatonState.HeaderEnd:
                        if (buffer[i] == '(')
                        {
                            state.AutomatonState = ParserAutomatonState.HeaderComment;
                        }
                        else if (buffer[i] == '"')
                        {
                            state.AutomatonState = ParserAutomatonState.HeaderQuote;
                        }
                        else if (buffer[i] == '\r')
                        {
                            state.Headers[state.CurrentHeader] = state.Buffer.ToString();
                            state.Buffer.Clear();
                            state.AutomatonState = ParserAutomatonState.Header_CR;
                        }
                        else
                        {
                            state.Buffer.Append((char)buffer[i]);
                        }
                        break;
                    case ParserAutomatonState.Header_CR:
                        if (buffer[i] == '\n')
                        {
                            state.AutomatonState = ParserAutomatonState.Header_LF;
                        }
                        break;
                    case ParserAutomatonState.Header_LF:
                        if (buffer[i] == '\r')
                        {
                            state.AutomatonState = ParserAutomatonState.END_CR;
                        }
                        else
                        {
                            state.Buffer.Append((char)buffer[i]);
                            state.AutomatonState = ParserAutomatonState.HeaderStart;
                        }
                        break;
                    case ParserAutomatonState.END_CR:
                        if (buffer[i] == '\n')
                        {
                            state.AutomatonState = ParserAutomatonState.END_LF;
                            return i;
                        }
                        break;
                }
            }
            return -1;
        }
    }
}
