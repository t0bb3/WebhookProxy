using System;
using System.Collections.Generic;

namespace WebhookProxy.Server.Models
{
    public class ProxyClientResponse
    {
        public ProxyClientResponse(string proxyClientId, IDictionary<string,string> headers, string body, int statusCode)
        {
            ProxyClientId = proxyClientId;
            Headers = headers;
            Body = body;
            StatusCode = statusCode;
        }

        public string ProxyClientId { get; set; }
        public IDictionary<string,string> Headers { get; set; }
        public dynamic Body { get; set; }
        public int StatusCode { get; set; }
    }

}
