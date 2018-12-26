using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebhookProxy.Server.IO
{
    public interface IProxyClient
    {

        Task OnConnected(string connectionId);

        Task OnProxyRequest(ProxyClientRequest request);

    }

}
