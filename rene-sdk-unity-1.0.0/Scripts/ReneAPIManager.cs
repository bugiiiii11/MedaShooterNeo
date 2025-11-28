using System;
using System.Threading.Tasks;
using Rene.Sdk;
using Rene.Sdk.Api.Game;
using Rene.Sdk.Api.Game.Data;
using ReneSdk.Rene.Sdk.Api;
using UnityEngine;


namespace ReneVerse
{
    public class ReneAPIManager
    {
        private static ApiEnv _apiEnv = ApiEnv.PROD;
        private static ReneAPICreds _reneAPICreds;


        public static ReneAPICreds ReneAPICreds
        {
            get
            {
                if (_reneAPICreds == null)
                {
                    _reneAPICreds = (ReneAPICreds)Resources.Load
                        (nameof(ReneVerse.ReneAPICreds), typeof(ScriptableObject));
                    if (_reneAPICreds == null)
                    {
                        Debug.LogError("Failed to load ReneAPICreds.");
                    }
                }

                return _reneAPICreds;
            }
            private set => _reneAPICreds = value;
        }

        private static bool _connect;
        private static API api;


        /// <summary>
        /// Initializes the ReneVerse API with the stored API credentials.
        /// Main connection to ReneVerse service. Once called check <see cref="SiteURL"/> for the notification.
        /// Use retrieved <see cref="InitializeAPI"/> to get all the information needed.
        /// Check <see cref="ReneVerseServiceExample"/> in the Demo Scene for implementation
        /// </summary>
        /// <returns></returns>
        public static API InitializeAPI()
        {
            api = API.Init(ReneAPICreds.APIKey, ReneAPICreds.PrivateKey, apiEnv: _apiEnv, new UnityLogger());
            return api;
        }

        public static API InitializeAPI(string authToken)
        {
            api = InitializeAPI();
            api.AuthToken = authToken;
            return api;
        }

        public static async Task<ServeAdResponse.ServeAdData> ServeAd(string authToken)
        {
            api ??= InitializeAPI();

            ServeAdResponse.ServeAdData serveAdData = await api.Game().ServeAd(authToken);
            return serveAdData;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Connects to the ReneVerse game with the provided email.
        /// </summary>
        /// <param name="email">The email to connect with.</param>
        /// <param name="reneAPI"></param>
        /// <returns>A task that represents the asynchronous operation.
        /// The task result contains a boolean indicating whether the connection was successful.</returns>
        public static async Task<bool> GameConnect(string email)
        {
            GameAPI gameAPI = api.Game();
            _connect = await gameAPI.Connect(email);
            if (_connect) _reneAPICreds.SaveGameApi(gameAPI);
            return _connect;
        }
#endif


        /// <summary>
        /// Mints a random asset from the available asset templates.
        /// Check <see cref="ReneVerseServiceExample"/> in the Demo Scene for implementation
        /// </summary>
        /// <param name="reneAPI">The ReneVerse API instance.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static async Task MintRandom(API reneAPI)
        {
            if (reneAPI == null)
            {
                Debug.Log($"{nameof(MintRandom)} is not possible since {nameof(reneAPI)} is null." +
                          $"Connect to ReneVerse first using name {nameof(InitializeAPI)}");
                return;
            }

            var assetTemplates = await reneAPI.Game().OwnableAssets();
            if (assetTemplates?.Items?.Count > 0)
            {
                var random = new System.Random();
                int randomIndex = random.Next(assetTemplates.Items.Count);

                var assetMinted = await reneAPI.Game().AssetMint
                    (assetTemplates.Items[randomIndex].OwnableAssetId);
                Debug.Log($"{nameof(MintRandom)} is performed");
            }
        }
    }
}