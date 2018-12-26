using System;

namespace WebhookProxy.Client.Endpoints
{
    public class ProxyServerEndpoint
    {
        public Uri URL { get; set; }
        public TimeSpan ConnectionTimeout { get; set; }
        public TimeSpan KeepAliveInterval { get; set; }

    }
}