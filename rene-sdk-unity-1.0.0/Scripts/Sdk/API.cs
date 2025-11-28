using Rene.Sdk.Api.Game;
using Rene.Sdk.Api.Helpers;
using Rene.Sdk.Api.User;
using System;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.OpenSsl;
using System.IO;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Threading;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net;
using Rene.Sdk.Http;
using Rene.Sdk.Primitives;
using ReneSdk.Rene.Sdk.Api;
using System.Collections.Generic;
using System.Net.Http;
using ReneSdk.Rene.Sdk.LoggingService;
using UnityEngine.Networking;

namespace Rene.Sdk
{
    public class API
    {
        private static API api;
        public static ILogger Logger { get; private set; }
        public readonly string ApiKey;
        private readonly string PrivateKey;
        private readonly ApiEnv ApiEnv;
        private SubscriptionUrls SubscriptionUrls;
        public string UserId { get; set; }

        public event Action OnAuthorized;
        public string AuthToken
        {
            get => _authToken;
            set
            {
                _authToken = value;
                if (_authToken != null)
                {
                    OnAuthorized?.Invoke();
                }
            }
        }

        private string _authToken;

        private HttpClient httpClient;

        private GameAPI gameAPI;
        private UserAPI userAPI;

        private API(string apiKey, string privateKey, ApiEnv apiEnv, ILogger logger)
        {
            Logger = logger;
            this.ApiKey = apiKey;
            this.PrivateKey = privateKey;
            this.ApiEnv = apiEnv;

            this.httpClient = new HttpClient();
        }

        public static API Init(string apiKey, string privateKey, ApiEnv apiEnv = ApiEnv.PROD, ILogger logger = null)
        {
            api = new API(apiKey, privateKey, apiEnv, logger ?? new ConsoleLogger());
            return api;
        }

        public GameAPI Game()
        {
            return gameAPI ?? (gameAPI = new GameAPI(this));
        }

        public UserAPI User()
        {
            return userAPI ?? (userAPI = new UserAPI(this));
        }

        public bool IsAuthorized()
        {
            return AuthToken != null;
        }

        public async Task<AppSyncHeaderData> AppSyncHeader(string query, Dictionary<string, object> variables)
        {
            if (this.SubscriptionUrls == null)
            {
                this.SubscriptionUrls = await ApiEnv.GetSubscriptionUrls();
            }
            return new AppSyncHeaderData
            {
                Host = SubscriptionUrls.SubscriptionGraphQLHost(),
                Authorization = Base64EncodedAuthorizationToken(query, variables)
            };
        }

        public async Task<ClientWebSocket> CreateAppSyncWebsocketConnection(string query, Dictionary<string, object> variables)
        {
            var appSyncClientWebSocket = new ClientWebSocket();
            appSyncClientWebSocket.Options.AddSubProtocol("graphql-ws");

            this.SubscriptionUrls = await ApiEnv.GetSubscriptionUrls();

            var realtimeUri = new Uri(this.SubscriptionUrls.RealtimeUri()
                + $"?header={JsonConvert.SerializeObject(await AppSyncHeader(query, variables)).EncodeBase64()}"
                + "&payload=" + "{}".EncodeBase64()
            );

            await appSyncClientWebSocket.ConnectAsync(realtimeUri, CancellationToken.None);
            await appSyncClientWebSocket.AppSyncSendAsync($@"{{""type"": ""connection_init""}}");

            return appSyncClientWebSocket;
        }

        private string AuthorizationToken(string query, Dictionary<string, object> variables)
        {
            var base64EncodedData = Base64EncodedAuthorizationToken(query, variables);
            return base64EncodedData.Replace("=", "*");
        }

        private string Base64EncodedAuthorizationToken(string query, Dictionary<string, object> variables)
        {
            var queryInput = QueryInput(query, variables);
            var signedQueryInput = SignQueryInput(queryInput);
            var authorizationData = $"{ApiKey}.{signedQueryInput}" + (AuthToken != null ? $".{AuthToken}" : "");
            var base64EncodedData = (authorizationData).EncodeBase64();
            return base64EncodedData;
        }

        private string QueryInput(string query, Dictionary<string, object> variables)
        {
            var queryInput = $@"{{""query"":""{query}"",""variables"":{(variables != null ? JsonConvert.SerializeObject(variables) : "{}")}}}";
            return queryInput.Replace(@"\", string.Empty);
        }

        private string SignQueryInput(string queryInput)
        {
            PemReader pemReader = new PemReader(new StringReader(PrivateKeyPem()));
            RsaPrivateCrtKeyParameters rsaPrivateCrtKeyParameters = (RsaPrivateCrtKeyParameters)pemReader.ReadObject();
            RSAParameters rsaParameters = DotNetUtilities.ToRSAParameters(rsaPrivateCrtKeyParameters);

            RSACryptoServiceProvider rsaProvider = new RSACryptoServiceProvider();
            rsaProvider.ImportParameters(rsaParameters);

            var rsaFormatter = new RSAPKCS1SignatureFormatter(rsaProvider);
            rsaFormatter.SetHashAlgorithm("SHA256");

            var hashSignatureBytes = rsaFormatter.CreateSignature(new SHA256Managed().ComputeHash(Encoding.UTF8.GetBytes(queryInput)));
            return Convert.ToBase64String(hashSignatureBytes);
        }

        private string PrivateKeyPem()
        {
            return $@"-----BEGIN PRIVATE KEY-----
                    {PrivateKey}
                    -----END PRIVATE KEY-----";
        }

        private HttpRequestMessage ToHttpRequestMessage(GraphQLRequest request, string query, Dictionary<string, object> variables)
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, ApiEnv.GraphQLUri())
            {
                Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")
            };
            httpRequestMessage.Headers.Host = ApiEnv.Host();
            httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/graphql+json"));
            httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpRequestMessage.Headers.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(AuthorizationToken(query, variables));
            return httpRequestMessage;
        }

        public async Task<GraphQLResponse<TResponse>> SendGraphQLRequest<TResponse>(GraphQLRequest request, string query)
        {
            return await SendGraphQLRequest<TResponse>(request, query, null);
        }

        public async Task<GraphQLResponse<TResponse>> SendGraphQLRequest<TResponse>(GraphQLRequest request, string query, Dictionary<string, object> variables = null)
        {
            string requestJson = JsonConvert.SerializeObject(request);
            var uri = ApiEnv.GraphQLUri();

            using (UnityWebRequest webRequest = new UnityWebRequest(uri, "POST"))
            {
                byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(requestJson);
                webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
                webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                string authToken = AuthorizationToken(query, variables);
                webRequest.SetRequestHeader("Authorization", authToken);

                // Send the request
                var operation = webRequest.SendWebRequest();

                // Wait until the operation is done
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"Web request error: {webRequest.error}");
                }
                string responseText = webRequest.downloadHandler.text;
                var graphQLResponse = JsonConvert.DeserializeObject<GraphQLResponse<TResponse>>(responseText);
                return graphQLResponse;
            }
        }


    }
}