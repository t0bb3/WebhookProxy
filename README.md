# WEBHOOK PROXY
Reverse proxy. Forwarding webhooks from internet services to endpoints within NAT networks.

## HOW IT WORKS
1. Deploy *PROXY SERVER* on a server accessible from the Internet
2. Browse to server and configure webhook and destination endpoint of your choice
3. Point your internet service webhooks to configured webhook endpoint

*PROXY SERVER* forwards webhooks via websocket to *WEB BROWSER* (proxy client) which in turn forwards to *WEBHOOK RECEIVER* (destination endpoint) via AJAX. The connection between *PROXY SERVER* and *WEB BROWSER* (proxy client) is established on websocket which fallback to  longpolling and server-side-events on bad connections.

![](https://github.com/t0bb3/webhook-proxy/blob/master/overview.PNG)


## HOW IT LOOKS
![](https://github.com/t0bb3/webhook-proxy/blob/master/screenshot.PNG)


### DEALING WITH CORS
The web based proxy client forwarding webhooks to destination endpoints using AJAX. Therefore, the destination endpoint must support CORS requests or CORS protection must be disabled in the browser.

(e.g. start chrome using --disable-web-security)
