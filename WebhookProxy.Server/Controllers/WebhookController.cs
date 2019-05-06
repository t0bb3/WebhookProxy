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

        [Route("{endpoint}")]
        [Consumes("application/json"), Produces("application/json")]
        [HttpPost, HttpGet, HttpPut, HttpDelete]
        public async Task<JsonResult> IncomingRequest(string endpoint)
        {
            try
            {
        
                dynamic requestBody = GetRequestBody();

                foreach(var proxyClientId in EndpointSubscriptions.GetEndpointSubscribers(endpoint))
                {
                    var client = GetProxyClient(proxyClientId);

                    var proxyClientRequest = CreateProxyClientRequest(proxyClientId, new HttpMethod(Request.Method), Request.Headers, requestBody);

                    await ForwardWebhookToProxyClient(client, proxyClientRequest);
                }

                var webhookResponse = WaitForProxyClientResponse(endpoint);
                if (webhookResponse == null) return Json(new { error = "No response from proxy client" });

                AddResponseHeaders(webhookResponse);

                var result = JsonConvert.DeserializeObject(webhookResponse.Body);

                return Json(result);
            }
            catch (Exception e)
            {
                return Json(new { HttpStatus = 500, Error = e.Message, StackTrace = e.StackTrace });
            }
        }
        
        private dynamic GetRequestBody()
        {
            var bodyStream = new StreamReader(Request.Body);

            dynamic requestBody = bodyStream.ReadToEnd();

            var expectJson = Request.Headers.Where(header => header.Key.ToLower().Equals("content-type"))
                                            .Any(header => header.Value.Contains("application/json"));

            if (expectJson)
                requestBody = JsonConvert.DeserializeObject(requestBody);

            bodyStream.Close();
            bodyStream.Dispose();

            return requestBody;
        }

        private void AddResponseHeaders(ProxyClientResponse webhookResponse)
        {
            Response.Headers.Clear();

            foreach(var responseHeader in webhookResponse.Headers)
            {
                if(responseHeader.Key == "transfer-encoding") continue;
                if(responseHeader.Key == "content-type") continue;
                
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
            var headers = httpHeaders.ToDictionary(header => header.Key, header => header.Value.ToString());

            var expectJson = headers.Where(header => header.Key.ToLower().Equals("content-type"))
                                    .Any(header => header.Value.Contains("application/json"));

            var body = expectJson ? JsonConvert.SerializeObject(httpBody) : httpBody;

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