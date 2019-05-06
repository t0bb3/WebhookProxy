using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using WebhookProxy.Server.Controllers;
using WebhookProxy.Server.Models;

namespace WebhookProxy.Server.IO
{
    public static class EndpointSubscriptions
    {
        
        private static ConcurrentDictionary<string, string> _endpointSubscribers = new ConcurrentDictionary<string, string>();


        public static List<string> GetEndpointSubscribers(string endpoint)
        {
            var subscribers = _endpointSubscribers.Where(e => e.Value.ToLower() == endpoint.ToLower()).Select(e => e.Key).ToList();

            return subscribers;
        }

        public static bool AddSubscriber(string endpoint, string proxyClientId)
        {

            if(_endpointSubscribers.ContainsKey(proxyClientId))
                _endpointSubscribers.Remove(proxyClientId, out string previouseEndpoint);

            return _endpointSubscribers.TryAdd(proxyClientId, endpoint);

        }

        public static string GetClientEndpoint(string proxyClientId)
        {
            return _endpointSubscribers[proxyClientId];
        }

    }

    public static class RequestPool
    {

        private static ConcurrentDictionary<string, EventWaitHandle> _requestWaitHandlers = new ConcurrentDictionary<string, EventWaitHandle>();
        private static ConcurrentDictionary<string, ProxyClientResponse> _proxyClientResponses = new ConcurrentDictionary<string, ProxyClientResponse>();

        public static ProxyClientResponse WaitForProxyClientResponse(string endpoint, TimeSpan timeout)
        {
            var requestWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            _requestWaitHandlers.TryAdd(endpoint, requestWaitHandle);

            requestWaitHandle.WaitOne(timeout);

            _requestWaitHandlers.TryRemove(endpoint, out requestWaitHandle);
            requestWaitHandle?.Dispose();

            _proxyClientResponses.TryRemove(endpoint, out ProxyClientResponse proxyClientWebhookResponse);

            return proxyClientWebhookResponse;
        }

        public static void SetProxyClientResponse(string endpoint, ProxyClientResponse proxyClientResponse)
        {
            if(!_requestWaitHandlers.TryRemove(endpoint, out EventWaitHandle proxyClientWaitHandler)) return;

            _proxyClientResponses.TryAdd(endpoint, proxyClientResponse);
            proxyClientWaitHandler.Set();
            proxyClientWaitHandler.Dispose();
        }

    }

}