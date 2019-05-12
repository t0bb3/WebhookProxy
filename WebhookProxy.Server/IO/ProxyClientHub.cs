using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebhookProxy.Server.Controllers;
using WebhookProxy.Server.Models;

namespace WebhookProxy.Server.IO
{
    public class ProxyClientHub : Hub<IProxyClient>
    {

        public ProxyClientHub()
        {
            
        }

        [HubMethodName("OnProxyClientSubscribeEndpoint")]
        public void OnProxyClientSubscribeEndpoint(string endpoint)
        {
            if(EndpointSubscriptions.AddSubscriber(endpoint, Context.ConnectionId))
            {
                Clients.Caller.OnEndpointSubscription(endpoint);
            }
            else
            {
                 throw new HubException("Failed to subscribe to endpoint.");
            }
        }

        [HubMethodName("OnProxyWebClientResponse")]
        public void OnProxyWebClientResponse(dynamic proxyWebClientResponse)
        {
            var responseBody = (string)proxyWebClientResponse.body;
            var responseHeaders = new Dictionary<string,string>();

            foreach(JValue header in (JArray)proxyWebClientResponse.headers)
            {
                var keyPosition = header.Value.ToString().IndexOf(" ");
                if(keyPosition < 0) continue;

                var key = header.Value.ToString().Substring(0,keyPosition).Replace(":", "").Trim();
                var value = header.Value.ToString().Substring(keyPosition).Replace(":", "").Trim();
                responseHeaders.Add(key, value);
            }

            var proxyClientResponse = new ProxyClientResponse(Context.ConnectionId, responseHeaders, responseBody, (int)proxyWebClientResponse.statusCode);

            var endpoint = EndpointSubscriptions.GetClientEndpoint(Context.ConnectionId);

            RequestPool.SetProxyClientResponse(endpoint, proxyClientResponse);
        }

        [HubMethodName("OnProxyClientResponse")]
        public void OnProxyClientResponse(ProxyClientResponse proxyClientResponse)
        {
            RequestPool.SetProxyClientResponse(Context.ConnectionId, proxyClientResponse);
        }

        [HubMethodName("OnProxyClientResponseError")]
        public void OnProxyClientResponseError(string error)
        {
            var proxyClientResponse = new ProxyClientResponse(Context.ConnectionId, new Dictionary<string, string>(), $"Destintion endpoint error: {error}", (int)HttpStatusCode.BadGateway);

            RequestPool.SetProxyClientResponse(Context.ConnectionId, proxyClientResponse);
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            await Clients.Caller.OnConnected(Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            EndpointSubscriptions.RemoveSubscriber(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public Task ThrowException()
        {
            throw new HubException("This error will be sent to the client!");
        }

    }

}
