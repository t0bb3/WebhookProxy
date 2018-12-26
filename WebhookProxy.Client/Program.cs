using System;
using System.IO;
using System.Linq;
using WebhookProxy.Client;
using WebhookProxy.Client.Configuration;

namespace WebhookProxy
{
    class Program
    {

        private static ProxyClient _proxyClient;
        private static bool _outputHeaders = false;
        private static bool _outputBody = false;
        private static string _configFile = "config.json";

        private static void Main(string[] args)
        {
            ConfigurationFactory.Log += Console.WriteLine;

            StartProxyClient(_configFile);

            ListenForUserCommands();
        }

        private static void StartProxyClient(string configFile)
        {
            if (!ConfigurationFactory.LoadFile(configFile, out ProxyClientConfiguration proxyClientConfig, out Exception configError))
            {
                Console.WriteLine($"Configuration Error:\r\n{configError?.Message ?? "unknown"}");
                return;
            }

            _proxyClient = CreateProxyClient(proxyClientConfig);
            _proxyClient.ConnectAsync().Wait();
        }

        private static ProxyClient CreateProxyClient(ProxyClientConfiguration config)
        {
            var proxyClient = new ProxyClient(config);

            proxyClient.Events.Connecting += (endpoint) =>
            {
                Console.WriteLine($"Connecting to proxy server: {endpoint}");
            };
            proxyClient.Events.Connected += (connectionId) => 
            {
                Console.Clear();

                PrintLogo();
                PrintEndpointInformation();
            };

            proxyClient.Events.Disconnected += (error) =>
            {
                Console.WriteLine($"Disconnected from proxy server. {error?.Message}. {error?.InnerException?.Message}");
            };

            proxyClient.Events.Warning += (message) => 
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(message);
                Console.ResetColor();
            };
            proxyClient.Events.Error += (message, exception) => 
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{message} {exception?.Message}  {exception?.InnerException?.Message}");
                Console.ResetColor();
            };
            proxyClient.Events.RequestReceived += (request) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("____________________________________________________________________________________________________");
                Console.WriteLine();
                Console.WriteLine($"{DateTime.Now}   HTTP {request.Method} REQUEST FROM WEBHOOK ENDPOINT (forwarded to destination endpoint)");
                Console.ResetColor();
                Console.WriteLine();

                if (_outputHeaders)
                {

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("-- HEADERS --");
                    Console.ResetColor();
                    Console.WriteLine("");
                    request.Headers.ToList().ForEach(header => Console.WriteLine($"{header.Key,-25} {header.Value}"));
                    Console.WriteLine();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("(http headers hidden, press 'h' to enable header output)");
                    Console.ResetColor();
                    Console.WriteLine();
                }
                if (_outputBody)
                {

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("-- BODY --");
                    Console.ResetColor();
                    Console.WriteLine();
                    Console.WriteLine($"{request.Body}");
                    Console.WriteLine();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("(http body hidden, press 'b' to enable body output)");
                    Console.ResetColor();
                    Console.WriteLine();
                }

            };
            proxyClient.Events.RequestForwarding += (request) =>
            {
                //Console.ForegroundColor = ConsoleColor.DarkGray;
                //Console.WriteLine($"Forwarding {request.Method} request to destination endpoint");
                //Console.ResetColor();
            };
            proxyClient.Events.ResponseReceived += (response) =>
            {
                //Console.ForegroundColor = ConsoleColor.DarkGray;
                //Console.WriteLine($"Received status {(int)response.StatusCode} from destination endpoint");
                //Console.ResetColor();
            };

            proxyClient.Events.ResponseForwarding += (response) =>
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("____________________________________________________________________________________________________");
                Console.WriteLine();
                Console.WriteLine($"{DateTime.Now}   HTTP {response.StatusCode} RESPONSE FROM DESTINATION ENDPOINT (forwarded to webhook endpoint)");
                Console.ResetColor();
                Console.WriteLine();
                if (_outputHeaders)
                {

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("-- HEADERS --");
                    Console.ResetColor();
                    Console.WriteLine("");
                    response.Headers.ToList().ForEach(header => Console.WriteLine($"{header.Key,-25} {header.Value}"));
                    Console.WriteLine();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("(http headers hidden, press 'h' to enable header output)");
                    Console.ResetColor();
                    Console.WriteLine();
                }
                if (_outputBody)
                {

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("-- BODY --");
                    Console.ResetColor();
                    Console.WriteLine();
                    Console.WriteLine($"{response.Body}");
                    Console.WriteLine();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("(http body hidden, press 'b' to enable body output)");
                    Console.ResetColor();
                    Console.WriteLine();
                }

            };

            return proxyClient;
        }

        private static void PrintLogo()
        {
            var logo = @"
 ██╗    ██╗███████╗██████╗ ██╗  ██╗ ██████╗  ██████╗ ██╗  ██╗    ██████╗ ██████╗  ██████╗ ██╗  ██╗██╗   ██╗
 ██║    ██║██╔════╝██╔══██╗██║  ██║██╔═══██╗██╔═══██╗██║ ██╔╝    ██╔══██╗██╔══██╗██╔═══██╗╚██╗██╔╝╚██╗ ██╔╝
 ██║ █╗ ██║█████╗  ██████╔╝███████║██║   ██║██║   ██║█████╔╝     ██████╔╝██████╔╝██║   ██║ ╚███╔╝  ╚████╔╝ 
 ██║███╗██║██╔══╝  ██╔══██╗██╔══██║██║   ██║██║   ██║██╔═██╗     ██╔═══╝ ██╔══██╗██║   ██║ ██╔██╗   ╚██╔╝  
 ╚███╔███╔╝███████╗██████╔╝██║  ██║╚██████╔╝╚██████╔╝██║  ██╗    ██║     ██║  ██║╚██████╔╝██╔╝ ██╗   ██║   
  ╚══╝╚══╝ ╚══════╝╚═════╝ ╚═╝  ╚═╝ ╚═════╝  ╚═════╝ ╚═╝  ╚═╝    ╚═╝     ╚═╝  ╚═╝ ╚═════╝ ╚═╝  ╚═╝   ╚═╝   
                                                                                                          
";
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(logo);
            Console.ResetColor();
        }

        private static void PrintEndpointInformation()
        {
            var scheme = _proxyClient.Configuration.ServerEndpoint.URL.Scheme;
            var authority = _proxyClient.Configuration.ServerEndpoint.URL.Authority;

            var webhookEndpoint = $"{scheme}://{authority}/{_proxyClient.ProxyServerConnectionId}";
            var destinationEndpoint = _proxyClient.Configuration.DestinationEndpoint.URL;

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"  WEBHOOK ENDPOINT\t{webhookEndpoint}");
            Console.WriteLine($"  DESTINATION ENDPOINT\t{destinationEndpoint}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine();
        }

        private static void PrintConfiguration()
        {
            Console.Clear();
            PrintLogo();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"CONFIGURATION FILE: {_configFile}");
            Console.WriteLine();
            if (File.Exists(_configFile))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(File.ReadAllText(_configFile));
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("CAN'T FIND CONFIGURATION FILE");
            }
            Console.ResetColor();
            Console.WriteLine();
        }

        private static void ResetWindow()
        {
            Console.Clear();
            PrintLogo();
            PrintEndpointInformation();
        }

        private static void ListenForUserCommands()
        {
            ConsoleKey pressedKey;

            while (true)
            {
                pressedKey = Console.ReadKey().Key;

                switch (pressedKey)
                {
                    case ConsoleKey.Escape:
                        ResetWindow();
                        break;

                    case ConsoleKey.Q:
                        return;

                    case ConsoleKey.H:
                        _outputHeaders = !_outputHeaders;
                        Console.WriteLine($"Showing http headers = {_outputHeaders}");
                        break;

                    case ConsoleKey.B:
                        _outputBody = !_outputBody;
                        Console.WriteLine($"Showing http body = {_outputBody}");
                        break;

                    case ConsoleKey.C:
                        PrintConfiguration();
                        break;

                    default: break;
                }
            }
        }

    }

}