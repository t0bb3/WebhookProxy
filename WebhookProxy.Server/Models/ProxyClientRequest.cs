using System.Collections.Generic;
using System.Net.Http;
using System.Linq;

namespace WebhookProxy.Server
{
    public class ProxyClientRequest
    {

        public ProxyClientRequest(string proxyClientId, HttpMethod httpMethod, Dictionary<string, string> headers, string body)
        {
            ProxyClientId = proxyClientId;
            Method = httpMethod.Method;
            Body = body;

            Headers = headers.Select(p=> new KeyValuePair<string,string>(p.Key,p.Value)).ToList();

        }

        public string ProxyClientId { get; set; }
        public string Method { get; set; }
        //public IDictionary<string, string> Headers { get; set; }
        public List<KeyValuePair<string,string>> Headers { get; set; }
        public dynamic Body { get; set; }

    }
}
