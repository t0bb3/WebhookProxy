using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using WebhookProxy.Server.Controllers;
using WebhookProxy.Server.Models;

namespace WebhookProxy.Server.IO
{
    public static class RequestPool
    {

        private static ConcurrentDictionary<string, EventWaitHandle> _requestWaitHandlers = new ConcurrentDictionary<string, EventWaitHandle>();
        private static ConcurrentDictionary<string, ProxyClientResponse> _proxyClientResponses = new ConcurrentDictionary<string, ProxyClientResponse>();

        public static ProxyClientResponse WaitForProxyClientResponse(string proxyClientId, TimeSpan timeout)
        {
            var requestWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            _requestWaitHandlers.TryAdd(proxyClientId, requestWaitHandle);

            requestWaitHandle.WaitOne(timeout);

            _requestWaitHandlers.TryRemove(proxyClientId, out requestWaitHandle);

            if (!_proxyClientResponses.ContainsKey(proxyClientId))
                return null;

            _proxyClientResponses.TryRemove(proxyClientId, out ProxyClientResponse proxyClientWebhookResponse);

            return proxyClientWebhookResponse;
        }

        public static void SetProxyClientResponse(string proxyClientId, ProxyClientResponse proxyClientResponse)
        {
            _proxyClientResponses.TryAdd(proxyClientId, proxyClientResponse);

            if(_requestWaitHandlers.TryRemove(proxyClientId, out EventWaitHandle proxyClientWaitHandler))
            {
                proxyClientWaitHandler.Set();
                proxyClientWaitHandler.Dispose();
            }
        }

    }

}