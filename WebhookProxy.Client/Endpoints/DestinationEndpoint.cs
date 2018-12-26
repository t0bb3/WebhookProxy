using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace WebhookProxy.Client.Endpoints
{
    public class DestinationEndpoint
    {
        public Uri URL { get; set; }
        public bool ValidateSsl { get; set; }
        public TimeSpan RequestTimeout { get; set; }
        public int MaxRedirects { get; set; }
        public NetworkCredential Credentials { get; set; }
        public X509CertificateCollection ClientCertificates { get; set; }
    }
}