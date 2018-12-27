using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebhookProxy.Client.Configuration;
using WebhookProxy.Client.Events;

namespace WebhookProxy.Client
{
    public class ProxyClient
    {
        
        public ProxyClient(ProxyClientConfiguration config)
        {
            Configuration = config;
            Events = new ProxyClientEventEmitter();
        }

        public ProxyClientEventEmitter Events { get; }
        public ProxyClientConfiguration Configuration { get; }
        public HubConnectionState ConnectionState => _proxyServerConnection.State;
        public string ProxyServerConnectionId { get; set; }

        private HubConnection _proxyServerConnection;
        private HttpClient _httpClient;
        private List<string> _destinationRequestHeaderBlacklist = new List<string>()
        {
            "content-type",
            "content-length"
        };

        public async Task ConnectAsync()
        {
            Events.OnConnecting(Configuration.ServerEndpoint.URL);

            if(_proxyServerConnection == null)
                InitializeServerConnection();

            if(_httpClient == null)
                InitializeHttpClient();
            
            try
            {
                await _proxyServerConnection.StartAsync();
            }
            catch (Exception ex)
            {
               Events.OnDisconnected(ex);
            }

        }

        private void InitializeHttpClient()
        {
            var httpHandler = new SocketsHttpHandler
            {
                Credentials = Configuration.DestinationEndpoint?.Credentials,
                MaxAutomaticRedirections = Configuration.DestinationEndpoint.MaxRedirects
            };

            if (!Configuration.DestinationEndpoint.ValidateSsl)
                httpHandler.SslOptions.RemoteCertificateValidationCallback += (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;

            httpHandler.SslOptions.ClientCertificates = Configuration.DestinationEndpoint.ClientCertificates;

            _httpClient = new HttpClient(httpHandler);
            _httpClient.Timeout = Configuration.DestinationEndpoint.RequestTimeout;
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Host = Configuration.DestinationEndpoint.URL.Host;

        }

        private void InitializeServerConnection()
        {
            _proxyServerConnection = new HubConnectionBuilder().WithUrl(Configuration.ServerEndpoint.URL).Build();
            _proxyServerConnection.ServerTimeout = Configuration.ServerEndpoint.ConnectionTimeout;
            _proxyServerConnection.KeepAliveInterval = Configuration.ServerEndpoint.KeepAliveInterval;

            _proxyServerConnection.Closed += async (error) =>
            {
                Events.OnDisconnected(error);

                await Task.Delay(new Random().Next(1, 5) * 1000);
                await ConnectAsync();
            };

            _proxyServerConnection.On<string>("OnConnected", (connectionId) =>
            {
                ProxyServerConnectionId = connectionId;
                Events.OnConnected(connectionId);
            });

            _proxyServerConnection.On<ProxyRequest>("OnProxyRequest", async (proxyRequest) => await OnProxyRequest(proxyRequest));

        }

        private async Task OnProxyRequest(ProxyRequest proxyRequest)
        {

            Events.OnRequestReceived(proxyRequest);

            var destinationTransaction = await ForwardToDestination(proxyRequest);
            
            if(destinationTransaction.Response == null)
            {
                var args = new List<object>() { destinationTransaction?.Error ?? "" };
                await _proxyServerConnection.InvokeCoreAsync("OnProxyClientResponseError", args.ToArray(), default(CancellationToken));

                Events.OnWarning("No response received from destination endpoint.");

                return;
            }

            Events.OnResponseReceived(destinationTransaction.Response);

           await ForwardToProxyServer(destinationTransaction.Response);

        }

        private async Task<DestinationTransaction> ForwardToDestination(ProxyRequest proxyRequest)
        {
            var destinationTransaction = CreateDestinationTransaction(proxyRequest);

            Events.OnRequestForwarding(destinationTransaction.Request);

            try
            {
                // TODO: Remove host-headers? Is set by HttpClient?
                _httpClient.DefaultRequestHeaders.Host = Configuration.DestinationEndpoint.URL.Host;
                destinationTransaction.Request.Headers.Host = Configuration.DestinationEndpoint.URL.Host;

                destinationTransaction.Response = await _httpClient.SendAsync(destinationTransaction.Request);
            }
            catch(TaskCanceledException ex)
            {
                destinationTransaction.Error = $"Forwarding request timeout. Configured timeout in proxy client = {Configuration.DestinationEndpoint.RequestTimeout.TotalMilliseconds}ms";
                Events.OnError("Timeout forwarding request to destination endpoint.", ex);
            }
            catch(Exception ex)
            {
                destinationTransaction.Error = ex.Message;
                Events.OnError("An error occurred while forwarding request to destination endpoint.", ex);
            }

            return destinationTransaction;
        }

        private void SetDestinationRequestHeaders(HttpRequestMessage destinationRequest, ProxyRequest proxyRequest)
        {
            destinationRequest.Headers.Clear();

            foreach (var requestHeader in proxyRequest.Headers)
            {
                if (_destinationRequestHeaderBlacklist.Contains(requestHeader.Key.ToLower()))
                    continue;

                if (!destinationRequest.Headers.TryAddWithoutValidation(requestHeader.Key, requestHeader.Value))
                    Events.OnWarning($"Failed to add header '{requestHeader.Key}' with value '{requestHeader.Value}' in request to destination endpoint.");
            }

            destinationRequest.Headers.Host = Configuration.DestinationEndpoint.URL.Host;
        }

        private void SetDestinationRequestBody(HttpRequestMessage destinationRequest, ProxyRequest proxyRequest)
        {
            var contentTypeHeader = proxyRequest.Headers.Where(p => p.Key.ToLower().Equals("content-type"))
                                                        .Select(p => p.Value)
                                                        .FirstOrDefault();
            
            if(proxyRequest?.Body != null && contentTypeHeader != null)
                destinationRequest.Content = new StringContent(proxyRequest.Body, Encoding.UTF8, contentTypeHeader);

            // TODO: Detect correct encoding? Always UTF8?
        }

        private DestinationTransaction CreateDestinationTransaction(ProxyRequest proxyRequest)
        {
            var destinationTransaction = new DestinationTransaction
            {
                Request = new HttpRequestMessage
                {
                    RequestUri = Configuration.DestinationEndpoint.URL,
                    Method = new HttpMethod(proxyRequest.Method),
                }
            };

            SetDestinationRequestHeaders(destinationTransaction.Request, proxyRequest);

            SetDestinationRequestBody(destinationTransaction.Request, proxyRequest);

            return destinationTransaction;
        }

        private async Task ForwardToProxyServer(HttpResponseMessage destinationResponse)
        {

            var body = await destinationResponse.Content.ReadAsStringAsync();

            var headers = new Dictionary<string,string>();
            destinationResponse.Headers.ToList().ForEach(header => headers.Add(header.Key, string.Join("; ", header.Value))); // TODO: correct header value separator?

            var proxyResponse = new ProxyResponse((int)destinationResponse.StatusCode, headers, body);

            Events.OnResponseForwarding(proxyResponse);
            
            var args = new List<object>() { proxyResponse };

            await _proxyServerConnection.InvokeCoreAsync("OnProxyClientResponse", args.ToArray(), default(CancellationToken));

        }

    }
}