using System;
using System.Net.Http;

namespace WebhookProxy.Client.Events
{

    public class ProxyClientEventEmitter
    {

        public delegate void ErrorEventHandler(string message, Exception ex);
        public event ErrorEventHandler Error;
        public void OnError(string message, Exception ex)
        {
            Error?.Invoke(message, ex);
        }

        public delegate void WarningEventHandler(string message);
        public event WarningEventHandler Warning;
        public void OnWarning(string message)
        {
            Warning?.Invoke(message);
        }

        public delegate void ConnectingEventHandler(Uri proxyServerEndpoint);
        public event ConnectingEventHandler Connecting;
        public void OnConnecting(Uri proxyServerEndpoint)
        {
            Connecting?.Invoke(proxyServerEndpoint);
        }
        
        public delegate void ConnectedEventHandler(string connectionId);
        public event ConnectedEventHandler Connected;
        public void OnConnected(string connectionId)
        {
            Connected?.Invoke(connectionId);
        }

        public delegate void DisconnectedEventHandler(Exception error);
        public event DisconnectedEventHandler Disconnected;
        public void OnDisconnected(Exception error)
        {
            Disconnected?.Invoke(error);
        }

        public delegate void RequestReceivedEventHandler(ProxyRequest proxyRequest);
        public event RequestReceivedEventHandler RequestReceived;
        public void OnRequestReceived(ProxyRequest proxyRequest)
        {
            RequestReceived?.Invoke(proxyRequest);
        }

        public delegate void RequestForwardingEventHandler(HttpRequestMessage request);
        public event RequestForwardingEventHandler RequestForwarding;
        public void OnRequestForwarding(HttpRequestMessage request)
        {
            RequestForwarding?.Invoke(request);
        }

        public delegate void ResponseReceivedEventHandler(HttpResponseMessage response);
        public event ResponseReceivedEventHandler ResponseReceived;
        public void OnResponseReceived(HttpResponseMessage response)
        {
            ResponseReceived?.Invoke(response);
        }

        public delegate void ResponseForwardingEventHandler(ProxyResponse response);
        public event ResponseForwardingEventHandler ResponseForwarding;
        public void OnResponseForwarding(ProxyResponse response)
        {
            ResponseForwarding?.Invoke(response);
        }

    }
}