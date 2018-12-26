using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;
using System.Net;
using WebhookProxy.Server.Models;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using WebhookProxy.Server.IO;

namespace WebhookProxy.Server.Controllers
{
    [Route("")]
    [ApiController]
    public class WebhookController : Controller
    {

        public WebhookController(IHubContext<ProxyClientHub, IProxyClient> proxyClientHubContext, IHttpContextAccessor accessor)
        {
            _proxyClientHubContext = proxyClientHubContext;
            _httpContextAccessor = accessor;
        }

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHubContext<ProxyClientHub, IProxyClient> _proxyClientHubContext;
        private readonly TimeSpan _proxyClientForwardTimeout = TimeSpan.FromSeconds(60);
        private readonly List<string> _responseHeaderBlacklist = new List<string>()
        {
            "transfer-encoding",
            "content-type"
        };


        [HttpPost, HttpGet, HttpPut, HttpDelete, Route("{proxyClientId}")]
        public async Task<ActionResult> IncomingRequest(string proxyClientId)
        {
            var client = GetProxyClient(proxyClientId);
            if (client == null) return StatusCode((int)HttpStatusCode.BadGateway, Json(new { error = "Proxy client {proxyClientId} not found" }));

            dynamic requestBody = GetRequestBody();
            
            var proxyClientRequest = CreateProxyClientRequest(proxyClientId, new HttpMethod(Request.Method), Request.Headers, requestBody);

            await ForwardWebhookToProxyClient(client, proxyClientRequest);

            var webhookResponse = WaitForProxyClientResponse(proxyClientId);
            if (webhookResponse == null) return StatusCode((int)HttpStatusCode.GatewayTimeout, Json(new { error = "No response from proxy client" }));

            AddResponseHeaders(webhookResponse);

            return StatusCode(webhookResponse.StatusCode, webhookResponse.Body);
        }


        private dynamic GetRequestBody()
        {
            var bodyStream = new StreamReader(Request.Body);
            dynamic body = bodyStream.ReadToEnd();

            var contentType = Request.Headers.Where(p => p.Key.ToLower().Equals("content-type")).Select(p => p.Value.ToString()).FirstOrDefault() ?? "text/plain";
            if (contentType == "application/json")
                body = JsonConvert.DeserializeObject(body);

            return body;
        }

        private void AddResponseHeaders(ProxyClientResponse webhookResponse)
        {
            Response.Headers.Clear();

            foreach(var responseHeader in webhookResponse.Headers)
            {
                if (_responseHeaderBlacklist.Contains(responseHeader.Key.ToLower())) continue;

                try
                {
                    Response.Headers.TryAdd(responseHeader.Key, responseHeader.Value);
                }
                catch(Exception){}
            }
        }

        private IProxyClient GetProxyClient(string connectionId)
        {
            IProxyClient proxyClient = null;

            try
            {
                proxyClient = _proxyClientHubContext.Clients.All;
                //proxyClient = _proxyClientHubContext.Clients.Client(connectionId);
            }
            catch(Exception){}

            return proxyClient;
        }

        private ProxyClientRequest CreateProxyClientRequest(string proxyClientId, HttpMethod httpMethod, IHeaderDictionary httpHeaders, dynamic httpBody)
        {
            var headers = httpHeaders.ToDictionary(k => k.Key, v => v.Value.ToString());

            headers.Add("X-Forwarded-For", _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());

            var contentType = httpHeaders.Where(header => header.Key.ToLower().Equals("content-type")).Select(header => header.Value.ToString().ToLower()).FirstOrDefault();

            var body = contentType == "application/json" ? JsonConvert.SerializeObject(httpBody) : httpBody;

            var proxyClientRequest = new ProxyClientRequest(proxyClientId, httpMethod, headers, body);

            return proxyClientRequest;
        }

        private async Task ForwardWebhookToProxyClient(IProxyClient proxyClient, ProxyClientRequest webhookRequest)
        {
            await proxyClient.OnProxyRequest(webhookRequest);
        }

        private ProxyClientResponse WaitForProxyClientResponse(string proxyClientId)
        {
            return RequestPool.WaitForProxyClientResponse(proxyClientId, _proxyClientForwardTimeout);
        }

    }
}