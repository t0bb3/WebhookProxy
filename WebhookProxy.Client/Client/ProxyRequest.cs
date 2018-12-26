using System.Collections.Generic;
using System.Linq;

namespace WebhookProxy.Client
{
    public class ProxyRequest    
    {

        public ProxyRequest(string proxyClientId, string method, Dictionary<string, string> headers, string body)
        {
            ProxyClientId = proxyClientId;
            Method = method.ToUpper();
            Headers = headers;
            Body = body;
        }

        public string ProxyClientId { get; set; }
        public string Method { get; set; }
        public Dictionary<string,string> Headers { get; set; }
        public dynamic Body { get; set; }

    }
}