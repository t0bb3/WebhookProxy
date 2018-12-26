using WebhookProxy.Client.Endpoints;

namespace WebhookProxy.Client.Configuration
{
    public class ProxyClientConfiguration
    {

        public ProxyServerEndpoint ServerEndpoint { get; set; }
        public DestinationEndpoint  DestinationEndpoint { get; set; }

    }
}
