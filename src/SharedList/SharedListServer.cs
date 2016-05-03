using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
#if WEBSOCKETS
using System.Net.WebSockets;
#endif // WEBSOCKETS
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharedList
{
    public class SharedListServer : IDisposable
    {
        const string Path = "/Invoke";
        private IWebHost host;
        private List<string> list = new List<string>();
        private DataContractJsonSerializer invokeRequestSerializer = new DataContractJsonSerializer(typeof(InvokeRequest));

        public SharedListServer(int port)
        {
            host = new WebHostBuilder()
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .Configure(app =>
                {
#if WEBSOCKETS
                    app.UseWebSockets();

                    app.Use(async (HttpContext context, Func<Task> next) =>
                    {
                        if (context.WebSockets.IsWebSocketRequest)
                        {
                            var socket = await context.WebSockets.AcceptWebSocketAsync();
                            Console.WriteLine($"WebSocket connection accepted"); 
                            await ListenToWebSocket(socket);
                        }
                        await next();
                    });
#else // WEBSOCKETS
                    app.Use(async (HttpContext context, Func<Task> next) =>
                    {
                        if (context.Request.Path.Value == Path && context.Request.ContentLength > 0)
                        {
                            InvokeRequest invokeRequest = default(InvokeRequest);
                            invokeRequest = (InvokeRequest)invokeRequestSerializer.ReadObject(context.Request.Body);

                            object reply = null;
                            if (Invoke(invokeRequest, ref reply))
                            {
                                var replySerializer = new DataContractJsonSerializer(reply.GetType());
                                using (var ms = new MemoryStream())
                                {
                                    replySerializer.WriteObject(ms, reply);
                                    await context.Response.WriteAsync(new string(Encoding.UTF8.GetChars(ms.ToArray())));

                                    // Halt the HTTP request processing pipeline
                                    return;
                                }
                            }
                        }
                        await next();
                    });
#endif // WEBSOCKETS
                })
                .Start(new[] { $"http://0.0.0.0:{port}" });            
        }

        public void Dispose()
        {
            host?.Dispose();
        }

#if WEBSOCKETS
        private async Task ListenToWebSocket(WebSocket socket)
        {
            byte[] buffer = new byte[1024 * 4];
            List<byte> message = new List<byte>();
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                Console.WriteLine($"Received {result.MessageType} message containing {result.Count} bytes");
                message.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));
                if (result.EndOfMessage)
                {
                    var invokeRequestSerializer = new DataContractJsonSerializer(typeof(InvokeRequest));
                    InvokeRequest invokeRequest = default(InvokeRequest);
                    using (var ms = new MemoryStream(message.ToArray()))
                    {
                        invokeRequest = (InvokeRequest)invokeRequestSerializer.ReadObject(ms);
                    }                        
                    message.Clear();
                    object reply = null;
                    if (Invoke(invokeRequest, ref reply))
                    {
                        var replySerializer = new DataContractJsonSerializer(reply.GetType());
                        using (var ms = new MemoryStream())
                        {
                            replySerializer.WriteObject(ms, reply);
                            // TODO : Pull this out for chunking
                            await socket.SendAsync(new ArraySegment<byte>(ms.ToArray()), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                }

                // Get next request
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
#endif // WEBSOCKETS

        private bool Invoke(InvokeRequest invokeRequest, ref object reply)
        {
            // Could use reflection, but that seems unnecessarilly slow given the set of methods to be called is a small, known set
            switch (invokeRequest.MethodName)
            {
                case "Add":
                    Console.WriteLine("Invoking 'Add'");
                    list.Add(invokeRequest.Arguments[0] as string);
                    return false;
                case "GetEnumerator":
                    Console.WriteLine("Invoking 'GetEnumerator'");
                    reply = list.ToArray();
                    return true;
                default:
                    Console.WriteLine($"Unknown method: {invokeRequest.MethodName}");
                    return false;
            }
        }        
    }
}
