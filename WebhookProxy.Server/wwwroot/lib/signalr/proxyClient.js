var proxyClient = {};

proxyClient.connection = new signalR.HubConnectionBuilder()
                                  .withUrl('/proxy-client')
                                  .configureLogging(signalR.LogLevel.Debug)
                                  .build();

proxyClient.connection.onclose(async () => {
    app.serverConnectionState = 'disconnected';
    await proxyClient.connect();
});

proxyClient.connection.on("OnConnected", (connectionId) => { 
    console.info("Connected to proxy server (connectionId=" + connectionId + ")");

    app.serverConnectionId = connectionId;
    app.serverConnectionState = 'connected';
});

proxyClient.connection.on("OnProxyRequest", (proxyRequest) => { 

    console.info("Request received from proxy server:");
    console.log(proxyRequest);

    proxyClient.forwardRequest(proxyRequest);

});

proxyClient.subscribeEndpoint = function()
{
    console.info("Subscribing to endpoint " + app.webhookEndpointUrl);
    proxyClient.connection.invoke("OnProxyClientSubscribeEndpoint", app.webhookEndpointUrl).catch(err => console.error(err.toString()));
}

proxyClient.connection.on("OnEndpointSubscription", (endpoint) => { 
    console.info("Subscription confirmed, now listening on " + endpoint);
    app.webhookEndpointUrl = endpoint;
});

proxyClient.forwardRequest = function(proxyRequest)
{
    console.info("Forwarding request to " + app.destinationEndpointUrl);
    
    var destinationTransaction = {
        created: new Date(),
        createdAgo: '',
        proxyRequest: proxyRequest,
        request: {
            created: new Date(),
            ajax: new XMLHttpRequest(),
            method: proxyRequest.method
        },
        status: '',
        response: {
            status: 'sending',
            statusCode: 0,
            created: null,
            headers: [],
            body: ''
        },
        expandedView: false
    };

    app.currentTransaction = destinationTransaction;
    
    app.currentTransaction.request.ajax.open(proxyRequest.method, app.destinationEndpointUrl);
    app.currentTransaction.request.ajax.timeout = app.destinationRequestTimeout;

    for(i = 0;i<proxyRequest.headers.length;i++)
    {
        var header = proxyRequest.headers[i];
        app.currentTransaction.request.ajax.setRequestHeader(header.key, header.value);
    }

    app.currentTransaction.request.ajax.onprogress = function()
    {
        app.currentTransaction.response.status = "progress";
    }

    app.currentTransaction.request.ajax.ontimeout = function ()
    {
        app.currentTransaction.response.statusCode = 408;
        app.currentTransaction.response.status = "timeout";
        proxyClient.forwardResponse();
    }

    app.currentTransaction.request.ajax.onerror = function(err)
    {
        app.currentTransaction.response.statusCode = app.currentTransaction.request.ajax.status;
        app.currentTransaction.response.status = "error";
        proxyClient.forwardResponse();
    }

    app.currentTransaction.request.ajax.onload = function()
    {
        app.currentTransaction.response.statusCode = app.currentTransaction.request.ajax.status;
        app.currentTransaction.response.status = "ok";
        proxyClient.forwardResponse();
    }
    
    app.currentTransaction.request.ajax.send(proxyRequest.body);
}

proxyClient.forwardResponse = function() {

    console.info("Forwarding response to proxy server:");
    console.log(app.currentTransaction);

    app.currentTransaction.response.created = new Date();
    app.currentTransaction.response.headers = app.currentTransaction.request.ajax.getAllResponseHeaders().split('\r\n');
    app.currentTransaction.response.body = app.currentTransaction.request.ajax.responseText;

    var proxyResponse = {
        proxyClientId: app.serverConnectionId,
        statusCode: app.currentTransaction.response.statusCode,
        headers: app.currentTransaction.response.headers,
        body: app.currentTransaction.response.body
    };

    proxyClient.connection.invoke("OnProxyWebClientResponse", proxyResponse).catch(err => console.error(err.toString()));

    app.transactionLog.push(app.currentTransaction);
    app.currentTransaction = null;
}

proxyClient.connect = function()
{
    try {
        app.serverConnectionState = 'connecting';
        proxyClient.connection.start();
    } catch (err) {
        
        app.serverConnectionState = 'disconnected';
        setTimeout('proxyClient.connect()', 1000);
    }
}

setTimeout('proxyClient.connect()', 1000);