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
using Microsoft.AspNetCore.Mvc.Formatters;
using WebhookProxy.Server.IO;

namespace WebhookProxy.Server.Controllers
{
    [Route("")]
    [ApiController]
    public class WebhookController : Controller
    {

        public WebhookController(IHubContext<ProxyClientHub, IProxyClient> proxyClientHubContext)
        {
            _proxyClientHubContext = proxyClientHubContext;
        }

        private readonly IHubContext<ProxyClientHub, IProxyClient> _proxyClientHubContext;
        private readonly TimeSpan _proxyClientForwardTimeout = TimeSpan.FromSeconds(60);
        protected internal static readonly MediaType jsonMediaType = new MediaType("application/json");

        [Route("{endpoint}")]
        [HttpPost, HttpGet, HttpPut, HttpDelete]
        public async Task<ActionResult> IncomingRequest(string endpoint)
        {
            try
            {
        
                dynamic requestBody = GetRequestBody();

                var endpointSubscribers = EndpointSubscriptions.GetEndpointSubscribers(endpoint);

                if(endpointSubscribers.Count == 0)
                {
                    return StatusCode(StatusCodes.Status502BadGateway, $"No proxy client connected to endpoint '{endpoint}'.");
                }

                foreach(var proxyClientId in endpointSubscribers)
                {
                    var client = GetProxyClient(proxyClientId);

                    var proxyClientRequest = CreateProxyClientRequest(proxyClientId, new HttpMethod(Request.Method), Request.Headers, requestBody, Request.ContentType);

                    await ForwardWebhookToProxyClient(client, proxyClientRequest);
                }

                var webhookResponse = WaitForProxyClientResponse(endpoint);
                if (webhookResponse == null) return Json(new { error = "No response from proxy client" });

                AddResponseHeaders(webhookResponse);
                
                var expectJson = (webhookResponse.Headers.Where(header => header.Key.ToLower().Equals("content-type")).Any(header => header.Value.Contains("application/json")));

                if(expectJson)
                {
                    var result = JsonConvert.DeserializeObject(webhookResponse.Body);
                    return Json(result);
                }
                else
                {
                    return StatusCode(webhookResponse.StatusCode, webhookResponse.Body);
                }

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

            if (IsJson(Request.ContentType))
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

        private ProxyClientRequest CreateProxyClientRequest(string proxyClientId, HttpMethod httpMethod, IHeaderDictionary httpHeaders, dynamic httpBody, string contentType)
        {
            var headers = httpHeaders.ToDictionary(header => header.Key, header => header.Value.ToString());

            var body = IsJson(contentType) ? JsonConvert.SerializeObject(httpBody) : httpBody;

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

        private static bool IsJson(string contentType)
        {
            var mediaType = new MediaType(contentType);
            var expectJson = mediaType.IsSubsetOf(jsonMediaType);
            return expectJson;
        }

    }
}