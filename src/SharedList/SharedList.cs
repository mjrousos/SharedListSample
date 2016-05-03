using System;
using System.Collections;
using System.Collections.Generic;
#if WEBSOCKETS
using System.Net.WebSockets;
#else // WEBSOCKETS
using System.Net.Http;
#endif // WEBSOCKETS
using System.IO;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SharedList
{
    public class SharedList : IList<string>, IDisposable
    {
#if WEBSOCKETS
        ClientWebSocket socket;
        Task connectionTask;
        int chunkSize = 1024;
#else // WEBSOCKETS
        HttpClient client;
        Uri serverAddress;
#endif // WEBSOCKETS

        public SharedList(Uri uri)
        {
#if WEBSOCKETS
            socket = new ClientWebSocket();
            connectionTask = socket.ConnectAsync(uri, CancellationToken.None);
#else // WEBSOCKETS
            client = new HttpClient();
            serverAddress = uri;
#endif // WEBSOCKETS
        }

        public void Add(string item)
        {
            AddAsync(item).Wait();
        }

        public async Task AddAsync(string item)
        {
            await InvokeAsync("Add", new[] { item });
        }

        public IEnumerator<string> GetEnumerator()
        {
            return GetEnumeratorAsync().Result;
        }

        public async Task<IEnumerator<string>> GetEnumeratorAsync()
        {
#if WEBSOCKETS
            await InvokeAsync("GetEnumerator", new object[0]);
            var strings = await ReceiveAsync<string[]>();
            return Enumerate(strings);
#else // WEBSOCKETS
            return Enumerate(await InvokeAsync<string[]>("GetEnumerator", new object[0]));
#endif // WEBSOCKETS
        }

        private IEnumerator<string> Enumerate(IEnumerable<string> strings) { foreach (string s in strings) yield return s; }

        private async Task<T> InvokeAsync<T>(string methodName, object[] args)
        {
            HttpResponseMessage response = await InvokeAsync(methodName, args);
            if (response?.IsSuccessStatusCode ?? false)
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    return (T)serializer.ReadObject(stream);
                }
            }

            return default(T);
        }

        private async Task<HttpResponseMessage> InvokeAsync(string methodName, object[] args)
        {
            var payload = new InvokeRequest()
            {
                MethodName = methodName,
                Arguments = args
            };

            DataContractJsonSerializer invokeRequestSerializer = new DataContractJsonSerializer(typeof(InvokeRequest));
            using (var ms = new MemoryStream())
            {
                invokeRequestSerializer.WriteObject(ms, payload);
#if WEBSOCKETS
                await connectionTask;
                await SendAsync(ms.ToArray());
                return null;
#else // WEBSOCKETS
                ms.Position = 0;
                return await client.PostAsync(serverAddress, new StreamContent(ms));
#endif // WEBSOCKETS
            }
        }


        public void Dispose()
        {
#if WEBSOCKETS
            if (socket?.State == WebSocketState.Open)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal closure", CancellationToken.None);
            }
            socket?.Dispose();
#else // WEBSOCKETS
            client.CancelPendingRequests();
            client.Dispose();
#endif // WEBSOCKETS
        }



#if WEBSOCKETS
        private async Task SendAsync(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i += chunkSize)
            {
                await socket.SendAsync(new ArraySegment<byte>(bytes, i, Math.Min(bytes.Length - i, chunkSize)), WebSocketMessageType.Text, i + chunkSize >= bytes.Length, CancellationToken.None);
            }
        }

        private async Task<T> ReceiveAsync<T>()
        {
            byte[] buffer = new byte[1024 * 4];
            List<byte> message = new List<byte>();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                message.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));
            }
            while (!result.EndOfMessage);
            var serializer = new DataContractJsonSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream(message.ToArray()))
            {
                return (T)serializer.ReadObject(ms);
            }
        }
#endif // WEBSOCKETS

#region NYI
        public string this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public int Count
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(string item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(string item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, string item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(string item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
#endregion // NYI
    }
}
