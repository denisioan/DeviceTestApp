using System;
using System.Net;
using System.Threading.Tasks;
using DeviceTestApp.OAuth;
using DeviceTestApp.Controllers;  // Assuming other controllers like DepositController, StatusController
using Microsoft.Extensions.Configuration;  // For loading config values
using System.IO;
using DeviceTestApp.Services;

class Program
{
    private static readonly string ServerAddress = "http://localhost:8080/";

    private static JwtTokenService _jwtTokenService;
    private static OAuthServer _oauthServer;
    private static DepositController _depositController;
    private static StatusController _statusController;
    private static ThingDescriptionController _thingDescriptionController;

    static async Task Main(string[] args)
    {
        // Load configuration
        var configuration = LoadConfiguration();

        // Initialize the JWT token service with secret key, issuer, and audience from the config
        _jwtTokenService = new JwtTokenService(
            configuration["Jwt:SecretKey"], 
            configuration["Jwt:Issuer"], 
            configuration["Jwt:Audience"]
        );

        // Initialize the OAuth server
        _oauthServer = new OAuthServer(_jwtTokenService);

        // Initialize other controllers (e.g., DepositController, StatusController)
        _depositController = new DepositController(new DeviceService(_jwtTokenService));
        _statusController = new StatusController(new DeviceService(_jwtTokenService));
        _thingDescriptionController = new ThingDescriptionController(new DeviceService(_jwtTokenService));

        // Start the HTTP server
        await StartHttpServer();
    }

    // Method to load configuration (e.g., secret key, issuer, audience)
    static IConfiguration LoadConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        return builder.Build();
    }

    static async Task StartHttpServer()
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add(ServerAddress);
        listener.Start();
        Console.WriteLine($"HTTP Server started. Listening on {ServerAddress}");

        while (true)
        {
            HttpListenerContext context = await listener.GetContextAsync();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // Route the request to the appropriate handler
            if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/token")
            {
                await _oauthServer.HandleTokenRequest(request, response);
            }
            else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/deposit")
            {
                await _depositController.HandleDepositRequest(request, response);
            }
            else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/status")
            {
                await _statusController.HandleStatusRequest(request, response);
            }
            else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/thing-description")
            {
                await _thingDescriptionController.HandleThingDescriptionRequest(response);
            }

            response.Close();
        }
    }
}