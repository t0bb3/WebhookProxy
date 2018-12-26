using System.Net.Http;

namespace WebhookProxy.Client
{
    public class DestinationTransaction
    {
        public DestinationTransaction()
        {
        }

        public HttpRequestMessage Request { get; set; }
        public HttpResponseMessage Response { get; set; }
        public string Error { get; set; }
    }
}