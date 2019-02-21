using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;
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

        [Route("{proxyClientId}")]
        [Consumes("application/json"), Produces("application/json")]
        [HttpPost, HttpGet, HttpPut, HttpDelete]
        public async Task<JsonResult> IncomingRequest(string proxyClientId)
        {
            try
            {
                var client = GetProxyClient(proxyClientId);
                if (client == null) return Json(new { error = "Proxy client {proxyClientId} not found" });

                dynamic requestBody = GetRequestBody();
                
                var proxyClientRequest = CreateProxyClientRequest(proxyClientId, new HttpMethod(Request.Method), Request.Headers, requestBody);

                await ForwardWebhookToProxyClient(client, proxyClientRequest);

                var webhookResponse = WaitForProxyClientResponse(proxyClientId);
                if (webhookResponse == null) return Json(new { error = "No response from proxy client" });

                AddResponseHeaders(webhookResponse);

                var result = JsonConvert.DeserializeObject(webhookResponse.Body);

                return Json(result, new JsonSerializerSettings() { });
            }
            catch (Exception e)
            {
                return Json(new { Status = 500, Error = e.Message });
            }
        }
        
        private dynamic GetRequestBody()
        {
            var bodyStream = new StreamReader(Request.Body);
            dynamic body = bodyStream.ReadToEnd();

            var contentType = Request.Headers.Where(p => p.Key.ToLower().Equals("content-type")).Select(p => p.Value.ToString()).FirstOrDefault() ?? string.Empty;
            if (contentType.Contains("application/json"))
                body = JsonConvert.DeserializeObject(body);

            return body;
        }

        private void AddResponseHeaders(ProxyClientResponse webhookResponse)
        {
            Response.Headers.Clear();

            foreach(var responseHeader in webhookResponse.Headers)
            {
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
                proxyClient = _proxyClientHubContext.Clients.Client(connectionId);
            }
            catch(Exception){}

            return proxyClient;
        }

        private ProxyClientRequest CreateProxyClientRequest(string proxyClientId, HttpMethod httpMethod, IHeaderDictionary httpHeaders, dynamic httpBody)
        {
            var headers = httpHeaders.ToDictionary(k => k.Key, v => v.Value.ToString());

            var contentType = httpHeaders.Where(header => header.Key.ToLower().Equals("content-type")).Select(header => header.Value.ToString().ToLower()).FirstOrDefault();

            var body = contentType.Contains("application/json") ? JsonConvert.SerializeObject(httpBody) : httpBody;

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