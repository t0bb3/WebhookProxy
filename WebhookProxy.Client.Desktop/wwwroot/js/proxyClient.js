var gui = {};

gui.connection = new signalR.HubConnectionBuilder()
                                  .withUrl('/gui')
                                  .configureLogging(signalR.LogLevel.Debug)
                                  .build();

gui.connection.onclose(async () => {
    app.serverConnectionState = 'disconnected';
    await proxyClient.connect();
});

gui.connection.on("OnConnected", (connectionId) => { 
    console.info("Connected to proxy server (connectionId=" + connectionId + ")");

    app.serverConnectionId = connectionId;
    app.serverConnectionState = 'connected';
});

gui.connect = function()
{
    try {
        app.serverConnectionState = 'connecting';
        proxyClient.connection.start();
    } catch (err) {
        
        app.serverConnectionState = 'disconnected';
        setTimeout('proxyClient.connect()', 1000);
    }
}

setTimeout('gui.connect()', 1000);