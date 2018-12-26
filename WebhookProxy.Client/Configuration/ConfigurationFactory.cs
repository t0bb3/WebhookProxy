using System;
using System.IO;
using System.Net;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using WebhookProxy.Client.Endpoints;

namespace WebhookProxy.Client.Configuration
{

    public static class ConfigurationFactory
    {

        public delegate void LogEventHandler(string message);
        public static event LogEventHandler Log;

        public static bool LoadFile(string configFile, out ProxyClientConfiguration proxyClientConfig, out Exception error)
        {
            Log?.Invoke($"Loading configuration file: {configFile}");

            error = null;
            proxyClientConfig = new ProxyClientConfiguration();

            try
            {
                var config = new ConfigurationBuilder()
                                .SetBasePath(Path.Combine(AppContext.BaseDirectory))
                                .AddJsonFile(configFile, optional: false)
                                .Build();
                
                proxyClientConfig.ServerEndpoint = GetProxyServerEndpoint(config);
                proxyClientConfig.DestinationEndpoint = GetDestinationEndpoint(config);

            }
            catch(Exception ex)
            {
                error = ex;
                return false;
            }

            return true;
        }

        private static ProxyServerEndpoint GetProxyServerEndpoint(IConfigurationRoot config)
        {
            Log?.Invoke("Loading server endpoint configuration");

            var serverEndpoint = new ProxyServerEndpoint()
            {
                URL = new Uri(config["serverEndpoint:url"]),
                ConnectionTimeout = TimeSpan.FromMilliseconds(int.Parse(config["serverEndpoint:connectionTimeout"])),
                KeepAliveInterval = TimeSpan.FromMilliseconds(int.Parse(config["serverEndpoint:keepAliveInterval"])),
            };

            return serverEndpoint;
        }

        private static DestinationEndpoint GetDestinationEndpoint(IConfigurationRoot config)
        {
            Log?.Invoke("Loading destination endpoint configuration");

            var destinationEndpoint = new DestinationEndpoint()
            {
                URL = new Uri(config["destinationEndpoint:url"]),
                ValidateSsl = bool.Parse(config["destinationEndpoint:validateSsl"]),
                MaxRedirects = int.Parse(config["destinationEndpoint:maxRedirects"]),
                RequestTimeout = TimeSpan.FromMilliseconds(int.Parse(config["destinationEndpoint:requestTimeout"]))
            };

            destinationEndpoint.Credentials = GetNetworkCredentials(config);
            destinationEndpoint.ClientCertificates = GetClientCertificates(config);

            return destinationEndpoint;
        }

        private static NetworkCredential GetNetworkCredentials(IConfigurationRoot config)
        {
            Log?.Invoke("Loading destination endpoint network credentials");

            if (!string.IsNullOrEmpty(config["destinationEndpoint:credentials:user"]))
            {
                return new NetworkCredential(config["destinationEndpoint:credentials:user"], config["destinationEndpoint:credentials:pass"]);
            }

            return null;
        }

        private static X509Certificate2Collection GetClientCertificates(IConfigurationRoot config)
        {
            Log?.Invoke("Loading destination endpoint client certificates");

            var certificateThumbprints = config["destinationEndpoint:clientCertificates"].Split(",", StringSplitOptions.RemoveEmptyEntries);
            var clientCertificates = new X509Certificate2Collection();

            var certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);

            certStore.Open(OpenFlags.ReadOnly);

            foreach (var certificateThumbprint in certificateThumbprints)
            {
                X509Certificate2Collection certCollection = certStore.Certificates.Find(X509FindType.FindByThumbprint, certificateThumbprint.ToUpper(), false);

                if (certCollection.Count == 0)
                    throw new SecurityException($"Can't find certificate with thumbprint {certificateThumbprint}.");

                clientCertificates.Add(certCollection[0]);

                Log?.Invoke($"Loaded client certificate (thumbprint={certificateThumbprint})");
            }

            certStore.Close();

            return clientCertificates;
        }

    }
}