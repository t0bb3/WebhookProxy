using System.Collections.Generic;
using System.Net.Http;

namespace WebhookProxy.Server
{
    public class ProxyClientRequest
    {

        public ProxyClientRequest(string proxyClientId, HttpMethod httpMethod, Dictionary<string, string> headers, string body)
        {
            ProxyClientId = proxyClientId;
            Method = httpMethod.Method;
            Headers = headers;
            Body = body;
        }

        public string ProxyClientId { get; set; }
        public string Method { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public dynamic Body { get; set; }

    }
}
