using System;
using System.Net;
using System.Threading.Tasks;
using DeviceTestApp.OAuth;
using DeviceTestApp.Controllers;
using Microsoft.Extensions.Configuration;
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
    private static EventService _eventService;

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

        // Initialize EventService
        _eventService = new EventService();

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

    // Method to start the HTTP server and route requests
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

            // Separate logic for WebSocket requests
            if (request.Url.AbsolutePath.StartsWith("/events/") && request.IsWebSocketRequest)
            {
                // Extract event name from the URL
                string eventName = GetEventNameFromUrl(request.Url.AbsolutePath);
                if (!string.IsNullOrEmpty(eventName))
                {
                    Console.WriteLine($"WebSocket request received for /events/{eventName}.");
                    _ = Task.Run(() => HandleWebSocketRequest(context, eventName));
                }
                else
                {
                    Console.WriteLine("Invalid WebSocket request path.");
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Close();
                }
            }
            // Logic for regular HTTP requests
            else
            {
                await HandleHttpRequest(request, response);
            }
        }
    }

    // Helper method to extract the event name from the WebSocket URL
    private static string GetEventNameFromUrl(string urlPath)
    {
        if (urlPath.StartsWith("/events/"))
        {
            return urlPath.Substring("/events/".Length); // Extract the event name after "/events/"
        }
        return null;
    }

    // Method to handle regular HTTP requests
    private static async Task HandleHttpRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/token")
        {
            await _oauthServer.HandleTokenRequest(request, response);
        }
        else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/deposit")
        {
            // Handle deposit and emit event in case of failure
            bool depositSuccess = await _depositController.HandleDepositRequest(request, response);
            if (!depositSuccess)
            {
                await _eventService.EmitEvent("deviceFailure", "Device failure detected during deposit.");
            }
        }
        else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/status")
        {
            await _statusController.HandleStatusRequest(request, response);
        }
        else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/thing-description")
        {
            await _thingDescriptionController.HandleThingDescriptionRequest(response);
        }
        else
        {
            // Return a 404 for any other requests
            response.StatusCode = (int)HttpStatusCode.NotFound;
        }
        response.Close();
    }

    // Method to handle WebSocket requests with event name
    private static async Task HandleWebSocketRequest(HttpListenerContext context, string eventName)
    {
        // Use the existing _eventService to handle the WebSocket request for a specific event
        try
        {
            await _eventService.HandleWebSocketRequest(context, eventName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling WebSocket request for event {eventName}: {ex.Message}");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.Close();
        }
    }
}