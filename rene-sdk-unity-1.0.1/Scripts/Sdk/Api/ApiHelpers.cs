using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rene.Sdk.Primitives;

namespace Rene.Sdk.Api.Helpers
{
    public static class ExtensionMethods
    {
        public static bool IsNotNullOrEmpty(this string value)
        {
            if (value == null)
            {
                return false;
            }
            return value?.Length > 0;
        }

        public static string EncodeBase64(this string value)
        {
            var valueBytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(valueBytes);
        }

        public static string DecodeBase64(this string value)
        {
            var valueBytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(valueBytes);
        }

        public static ArraySegment<byte> ArraySegmentBuffer(this string value)
        {
            return new ArraySegment<byte>(Encoding.UTF8.GetBytes(value));
        }

        public static async Task AppSyncSendAsync(this ClientWebSocket clientWebSocket, string data)
        {
            await clientWebSocket.SendAsync(data.ArraySegmentBuffer(),
                WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public static async Task AppSyncSubscribe<T>(this ClientWebSocket clientWebSocket, AppSyncHeaderData appSyncHeader, GraphQLRequest request, Func<GraphQLResponse<T>, bool> onMessageCallback)
        {
            Guid subscriptionId = Guid.NewGuid();
            while (clientWebSocket.State == WebSocketState.Open)
            {
                ArraySegment<Byte> buffer = new ArraySegment<byte>(new Byte[8192]);
                WebSocketReceiveResult result = null;
                using (var ms = new MemoryStream())
                {
                    // This loop is needed because the server might send chunks of data that need to be assembled by the client
                    // see: https://stackoverflow.com/questions/23773407/a-websockets-receiveasync-method-does-not-await-the-entire-message
                    do
                    {
                        result = await clientWebSocket.ReceiveAsync(buffer, CancellationToken.None);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    }
                    while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);

                    using (var reader = new StreamReader(ms, Encoding.UTF8))
                    {
                        // convert stream to string
                        var message = reader.ReadToEnd();
                        Console.WriteLine($"Websocket message: {message}");
                        // quick and dirty way to check response
                        if (message.Contains("connection_ack"))
                        {
                            var data = $@"{{
                                ""id"": ""{subscriptionId}"",
                                ""payload"": {{
                                    ""data"": ""{{\""query\"":\""{request.Query}\"",\""operationName\"":\""{request.OperationName}\"",\""variables\"":{JsonConvert.SerializeObject(request.Variables).Replace(@"""", @"\""")}}}"",
                                    ""extensions"": {{
                                        ""authorization"": {JsonConvert.SerializeObject(appSyncHeader)}
                                    }}
                                }},
                                ""type"": ""start""
                            }}";
                            // Step 4
                            await clientWebSocket.AppSyncSendAsync(data);
                        }
                        else if (message.Contains("data"))  // Step 6
                        {
                            WebSocketPayload<GraphQLResponse<T>> webSocketPayload = JsonConvert.DeserializeObject<WebSocketPayload<GraphQLResponse<T>>>(message);

                            var disconnect = (bool)onMessageCallback?.Invoke(webSocketPayload.Payload);

                            if (disconnect)
                            {
                                // Step 7 
                                await clientWebSocket.AppSyncSendAsync($@"{{""type"": ""stop"",""id"": ""{subscriptionId}""}}");
                                // Step 8
                                await clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            }
                        }
                    }
                }
            }
        }
    }

    public class AppSyncHeaderData
    {
        [JsonProperty("host")]
        public string Host { get; set; }

        [JsonProperty("Authorization")]
        public string Authorization { get; set; }
    }

    class WebSocketPayload<T>
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public T Payload { get; set; }
    }
}