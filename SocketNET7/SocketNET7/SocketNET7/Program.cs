using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class WebSocketServer
{
    private static WebSocket[] _clients = new WebSocket[2];

    public static async Task Start()
    {
        var httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://localhost:8082/");

        try
        {
            httpListener.Start();
            Console.WriteLine("WebSocket server started.");

            while (true)
            {
                var context = await httpListener.GetContextAsync();

                if (context.Request.IsWebSocketRequest)
                {
                    await ProcessWebSocketRequest(context);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket server error: {ex.Message}");
        }
        finally
        {
            httpListener.Close();
        }
    }

    private static async Task ProcessWebSocketRequest(HttpListenerContext context)
    {
        var webSocketContext = await context.AcceptWebSocketAsync(null);
        var webSocket = webSocketContext.WebSocket;

        int clientIndex = GetNextAvailableClientIndex();

        if (clientIndex != -1)
        {
            _clients[clientIndex] = webSocket;
            await SendInitialMessage(webSocket, $"Client {clientIndex + 1} connected.");
            await HandleWebSocketConnection(webSocket, clientIndex);
        }
        else
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Server full", CancellationToken.None);
        }
    }

    private static int GetNextAvailableClientIndex()
    {
        for (int i = 0; i < _clients.Length; i++)
        {
            if (_clients[i] == null || _clients[i].State != WebSocketState.Open)
            {
                return i;
            }
        }
        return -1; // No available clients
    }

    private static async Task HandleWebSocketConnection(WebSocket webSocket, int clientIndex)
    {
        var buffer = new byte[4096];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
            else if (result.MessageType == WebSocketMessageType.Binary)
            {
                // Process the received blob
                byte[] receivedData = new byte[result.Count];
                Array.Copy(buffer, receivedData, result.Count);

                // Handle the received blob as needed
                HandleReceivedBlob(receivedData, clientIndex);

                // Re-send the blob to other connected clients
                for (int i = 0; i < _clients.Length; i++)
                {
                    if (_clients[i] != null && _clients[i].State == WebSocketState.Open)
                    {
                        await _clients[i].SendAsync(new ArraySegment<byte>(receivedData), WebSocketMessageType.Binary, true, CancellationToken.None);
                    }
                }
            }
        }
    }

    private static async Task SendInitialMessage(WebSocket webSocket, string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private static void HandleReceivedBlob(byte[] data, int clientIndex)
    {
        // Process the received blob data as needed
        Console.WriteLine($"Received blob from Client {clientIndex + 1} with length: {data.Length}");
    }

    public static async Task Main()
    {
        await WebSocketServer.Start();
    }
}
