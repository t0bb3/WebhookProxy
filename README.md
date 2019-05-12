# WEBHOOK PROXY
Reverse proxy. Forwarding webhooks from internet to endpoints within NAT networks.

## HOW IT WORKS
1. Deploy *PROXY SERVER* on a server accessible from the Internet
2. Browse to the server and configure public + destination endpoints

*WEB BROWSER* using AJAX to forward webhooks.
*WEBHOOK RECEIVER* need to support CORS *or* you have to disable CORS in the browser.

![](https://github.com/t0bb3/webhook-proxy/blob/master/overview.PNG)


## HOW IT LOOKS
![](https://github.com/t0bb3/webhook-proxy/blob/master/screenshot.PNG)


### DEALING WITH CORS
The proxy client is based on javascript and forwards webhooks to the destination endpoint using AJAX. Therefore, the recipient (destination endpoint) must support CORS requests or the CORS protection must be disabled in the browser.
