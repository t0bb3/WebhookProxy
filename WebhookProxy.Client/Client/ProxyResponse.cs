using System.Collections.Generic;

namespace WebhookProxy.Client
{
    public class ProxyResponse
    {

        public ProxyResponse(int statusCode, Dictionary<string,string> headers, string body)
        {
            Headers = headers;
            Body = body;
            StatusCode = statusCode;
        }

        public int StatusCode { get; set; }
        public Dictionary<string,string> Headers { get; set; }
        public dynamic Body { get; set; }

    }
}