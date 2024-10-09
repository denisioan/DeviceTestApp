using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class EventService
{
    // A dictionary to keep track of WebSocket clients subscribed to specific events
    private ConcurrentDictionary<string, ConcurrentBag<WebSocket>> _eventSubscribers = new ConcurrentDictionary<string, ConcurrentBag<WebSocket>>();

    // Method to handle WebSocket connections independently
    public async Task HandleWebSocketRequest(HttpListenerContext context, string eventName)
    {
        if (context.Request.IsWebSocketRequest)
        {
            try
            {
                // Accept the WebSocket connection
                WebSocketContext webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
                WebSocket webSocket = webSocketContext.WebSocket;

                // Register the client for the specific event
                RegisterWebSocket(eventName, webSocket);

                Console.WriteLine($"Client connected to event WebSocket for event: {eventName}");

                // Start receiving messages asynchronously
                await ReceiveMessagesAsync(webSocket, eventName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in WebSocket connection: {ex.Message}");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.Close();
            }
        }
        else
        {
            // If it's not a WebSocket request, return a 400 Bad Request
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.Close();
        }
    }

    // Method to register a WebSocket for a specific event
    public void RegisterWebSocket(string eventName, WebSocket webSocket)
    {
        if (!_eventSubscribers.ContainsKey(eventName))
        {
            _eventSubscribers[eventName] = new ConcurrentBag<WebSocket>();
        }
        _eventSubscribers[eventName].Add(webSocket);
        Console.WriteLine($"WebSocket registered for event: {eventName}");
    }

    private async Task ReceiveMessagesAsync(WebSocket webSocket, string eventName)
    {
        var buffer = new byte[1024 * 4]; // 4 KB buffer

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                switch (result.MessageType)
                {
                    case WebSocketMessageType.Text:
                        // Handle incoming text message
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"Received message: {message} for event {eventName}");
                        break;

                    case WebSocketMessageType.Binary:
                        // Optionally handle binary messages if needed
                        Console.WriteLine("Binary message received, ignoring.");
                        break;

                    case WebSocketMessageType.Close:
                        Console.WriteLine("Client requested close.");
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None);
                        break;
                }
            }
        }
        catch (WebSocketException wsex)
        {
            Console.WriteLine($"WebSocket error: {wsex.Message}");
        }
        finally
        {
            if (webSocket.State != WebSocketState.Closed)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Error occurred", CancellationToken.None);
            }

            // Remove the WebSocket from the event's list of clients
            RemoveWebSocket(eventName, webSocket);
        }
    }

    // Method to remove a WebSocket from the event subscribers
    private void RemoveWebSocket(string eventName, WebSocket webSocket)
    {
        if (_eventSubscribers.ContainsKey(eventName))
        {
            _eventSubscribers[eventName].TryTake(out _);
        }
    }

    // Method to emit an event to all connected clients subscribed to that event
    public async Task EmitEvent(string eventName, string message)
    {
        if (_eventSubscribers.ContainsKey(eventName))
        {
            var buffer = Encoding.UTF8.GetBytes(message);

            foreach (var client in _eventSubscribers[eventName])
            {
                if (client.State == WebSocketState.Open)
                {
                    await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}