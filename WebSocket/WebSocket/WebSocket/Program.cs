using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class WebSocketServer
{
    private static WebSocket _client1;
    private static WebSocket _client2;

    public static async Task Start()
    {
        var httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://localhost:8080/"); // Establece el puerto y la URL del servidor

        try
        {
            httpListener.Start();
            Console.WriteLine("Servidor WebSocket iniciado.");

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
            Console.WriteLine($"Error en el servidor WebSocket: {ex.Message}");
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

        if (_client1 == null)
        {
            _client1 = webSocket;
            await SendInitialMessage(webSocket, "Cliente 1 conectado.");
        }
        else if (_client2 == null)
        {
            _client2 = webSocket;
            await SendInitialMessage(webSocket, "Cliente 2 conectado.");
        }
        else
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Servidor completo", CancellationToken.None);
            return;
        }

        try
        {
            await HandleWebSocketConnection(webSocket);
        }
        finally
        {
            if (_client1 == webSocket)
            {
                _client1 = null;
                Console.WriteLine("Cliente 1 desconectado.");
            }
            else if (_client2 == webSocket)
            {
                _client2 = null;
                Console.WriteLine("Cliente 2 desconectado.");
            }
        }
    }

    private static async Task HandleWebSocketConnection(WebSocket webSocket)
    {
        var buffer = new byte[1024];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
            else if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Mensaje recibido: {message}");

                // Relevar el mensaje a los otros clientes conectados
                if (_client1 == webSocket && _client2 != null)
                {
                    await SendMessage(_client2, message);
                }
                else if (_client2 == webSocket && _client1 != null)
                {
                    await SendMessage(_client1, message);
                }
            }
        }
    }

    private static async Task SendInitialMessage(WebSocket webSocket, string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private static async Task SendMessage(WebSocket webSocket, string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }
}

public class Program
{
    public static async Task Main()
    {
        await WebSocketServer.Start();
    }
}