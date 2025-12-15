using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Cryptomeda.Minigames.BackendComs
{
    public enum RestfulEndpoint
    {
        ScoreAndAddress = 57201,
        Cheating = 48903,
        Timestamp = 95032,
        UserNfts = 41508,
        Staking = 60477,
        UserWeapons = 15300,
        BoostPackages = 95464,
    }

    public enum Devenv
    {
        ProdApi,
        StageApi,
        DevApi,
    }

    [Serializable]
    public struct EndpointData
    {
        public RestfulEndpoint Endpoint;
        public string Url;
    }

    public struct Response
    {
        public string Text;
        public long Code;
    }

    public class RestfulManager : Singleton<RestfulManager>
    {
        public Devenv CurrentEnvironment = Devenv.StageApi;

        [Header("Setup correct URLS!")]
        public List<EndpointData> Endpoints;

        public string XCrfTokenProd = "4pHsSRrbblstmRttIpN6NOMdcFCqRV7quqPMylgdPBRcx9x4kYHMuxvajkKJUO1p";
        public string XCrfTokenStage = "bLQ1W2SK3yPemALbodvG645RnaAhd1OgUYnN52MD1hhpJdEjUySIMsjf6P7r99oD";

        // FORCE OVERRIDE all problematic URLs at runtime
        private void Start()
        {
            Debug.Log("🔧 RestfulManager: Forcing URL override for Railway deployment");

            // Force update all endpoints to Railway
            ForceUpdateEndpointsToRailway();
        }

        private void ForceUpdateEndpointsToRailway()
        {
            string railwayBase = "https://swarm-resistance-backend-dev-production.up.railway.app";

            for (int i = 0; i < Endpoints.Count; i++)
            {
                var endpoint = Endpoints[i];
                string newUrl = "";

                switch (endpoint.Endpoint)
                {
                    case RestfulEndpoint.ScoreAndAddress:
                        newUrl = $"{railwayBase}/api/v1/minigames/medashooter/score/";
                        break;
                    case RestfulEndpoint.Cheating:
                        newUrl = $"{railwayBase}/api/v1/minigames/medashooter/blacklist/";
                        break;
                    case RestfulEndpoint.Timestamp:
                        newUrl = $"{railwayBase}/api/v1/minigames/medashooter/timestamp/";
                        break;
                    case RestfulEndpoint.UserNfts:
                        newUrl = $"{railwayBase}/api/v1/users/get_items/";
                        break;
                    case RestfulEndpoint.UserWeapons:
                        newUrl = $"{railwayBase}/api/v1/weapon_item/user_weapons/";
                        break;
                    case RestfulEndpoint.Staking:
                        newUrl = $"{railwayBase}/api/v1/stake/get_data/";
                        break;
                    case RestfulEndpoint.BoostPackages:
                        newUrl = $"{railwayBase}/api/v1/user/active_boost_cards";
                        break;
                    default:
                        continue; // Skip unknown endpoints
                }

                if (!string.IsNullOrEmpty(newUrl) && endpoint.Url != newUrl)
                {
                    Debug.Log($"🔄 URL Override: {endpoint.Endpoint} → {newUrl}");

                    // Create new endpoint data with updated URL
                    EndpointData updatedEndpoint = new EndpointData
                    {
                        Endpoint = endpoint.Endpoint,
                        Url = newUrl
                    };

                    // Update the list
                    Endpoints[i] = updatedEndpoint;
                }
            }

            Debug.Log("✅ All endpoints updated to Railway deployment");
        }

        public void SetApi(string api)
        {
            switch (api.ToLower())
            {
                case "prod-api":
                    CurrentEnvironment = Devenv.ProdApi;
                    break;
                case "dev-api":
                    CurrentEnvironment = Devenv.DevApi;
                    break;
                case "stage-api":
                    CurrentEnvironment = Devenv.StageApi;
                    break;
            }
        }

        private static string ConstructEnvUrl(string url)
        {
            // If URL already points to Railway, return as-is
            if (url.Contains("swarm-resistance-backend-dev-production.up.railway.app"))
            {
                Debug.Log($"🚀 Using Railway URL: {url}");
                return url;
            }

            // Legacy handling for old @ENV replacement
            string env = instance.CurrentEnvironment switch
            {
                Devenv.ProdApi => "api",
                Devenv.StageApi => "stage-api",
                Devenv.DevApi => "dev-api",
                _ => "api",
            };

            string constructedUrl = url.Replace("@ENV", env);
            Debug.Log($"🔄 Constructed URL: {url} → {constructedUrl}");
            return constructedUrl;
        }

        public static void Post(RestfulEndpoint endpoint, string json, Action<Response> onResult)
        {
            var endpointUrl = ConstructEnvUrl(instance.Endpoints.Find(x => x.Endpoint == endpoint).Url);
            Debug.Log($"🔗 POST Request to: {endpointUrl}");
            instance.StartCoroutine(instance.PostCo(endpointUrl, json, onResult));
        }

        public static void Get(RestfulEndpoint endpoint, Action<Response> onResult)
        {
            var endpointUrl = ConstructEnvUrl(instance.Endpoints.Find(x => x.Endpoint == endpoint).Url);
            Debug.Log($"🔗 GET Request to: {endpointUrl}");
            instance.StartCoroutine(instance.GetCo(endpointUrl, onResult));
        }

        public static void GetByUrlParam(RestfulEndpoint endpoint, string param, Action<Response> onResult)
        {
            var mainUrl = instance.Endpoints.Find(x => x.Endpoint == endpoint).Url;
            if (mainUrl[^1] != '/')
                mainUrl += '/';
            if (param[0] == '/')
                param = param.Substring(1);

            mainUrl += param;

            var endpointUrl = ConstructEnvUrl(mainUrl);
            Debug.Log($"🔗 GET Request with param to: {endpointUrl}");
            instance.StartCoroutine(instance.GetCo(endpointUrl, onResult));
        }

        public static void GetFromUrl(string url, Action<Response> onResult)
        {
            var endpointUrl = ConstructEnvUrl(url);
            Debug.Log($"🔗 GET Request from URL: {endpointUrl}");
            instance.StartCoroutine(instance.GetCo(endpointUrl, onResult));
        }

        public static void Get(RestfulEndpoint endpoint, string json, Action<Response> onResult)
        {
            var endpointUrl = ConstructEnvUrl(instance.Endpoints.Find(x => x.Endpoint == endpoint).Url);
            Debug.Log($"🔗 GET Request with JSON to: {endpointUrl}");
            instance.StartCoroutine(instance.GetCo(endpointUrl, json, onResult));
        }

        private IEnumerator GetCo(string endpointUrl, Action<Response> onResult)
        {
            Debug.Log($"🚀 Starting GET request to: {endpointUrl}");

            using var uwr = UnityWebRequest.Get(endpointUrl);

            uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

            uwr.SetRequestHeader("Accept", "*/*");
            uwr.SetRequestHeader("X-CSRFToken", CurrentEnvironment == Devenv.StageApi ? XCrfTokenStage : XCrfTokenProd);
            uwr.timeout = 25;

            yield return uwr.SendWebRequest();

            Debug.Log($"📡 GET Response - Code: {uwr.responseCode}, Result: {uwr.result}");

            if (uwr.result == UnityWebRequest.Result.ConnectionError)
            {
                var response = new Response();
                response.Text = uwr.error;
                response.Code = uwr.responseCode;
                Debug.LogError($"❌ Connection Error: {uwr.error}");
                onResult?.Invoke(response);
            }
            else if (uwr.result == UnityWebRequest.Result.DataProcessingError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                var response = new Response();
                response.Code = uwr.responseCode;
                response.Text = uwr.error;
                Debug.LogError($"❌ Protocol/Data Error: {uwr.error}");
                onResult?.Invoke(response);
            }
            else
            {
                var response = new Response();
                response.Code = uwr.responseCode;
                response.Text = uwr.downloadHandler.text;
                Debug.Log($"✅ GET Success: {uwr.responseCode} - {uwr.downloadHandler.text.Substring(0, Mathf.Min(200, uwr.downloadHandler.text.Length))}...");
                onResult?.Invoke(response);
            }
        }

        private IEnumerator GetCo(string endpointUrl, string json, Action<Response> onResult)
        {
            Debug.Log($"🚀 Starting GET request with JSON to: {endpointUrl}");

            using var uwr = UnityWebRequest.Get(endpointUrl);
            var jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);

            uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

            uwr.SetRequestHeader("Accept", "*/*");
            uwr.SetRequestHeader("X-CSRFToken", CurrentEnvironment == Devenv.StageApi ? XCrfTokenStage : XCrfTokenProd);

            yield return uwr.SendWebRequest();

            Debug.Log($"📡 GET with JSON Response - Code: {uwr.responseCode}, Result: {uwr.result}");

            if (uwr.result == UnityWebRequest.Result.ConnectionError)
            {
                var response = new Response();
                response.Code = uwr.responseCode;
                response.Text = uwr.error;
                Debug.LogError($"❌ Connection Error: {uwr.error}");
                onResult?.Invoke(response);
            }
            else if (uwr.result == UnityWebRequest.Result.DataProcessingError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                var response = new Response();
                response.Code = uwr.responseCode;
                response.Text = uwr.error;
                Debug.LogError($"❌ Protocol/Data Error: {uwr.error}");
                onResult?.Invoke(response);
            }
            else
            {
                var response = new Response();
                response.Code = uwr.responseCode;
                response.Text = uwr.downloadHandler.text;
                Debug.Log($"✅ GET with JSON Success: {uwr.responseCode} - {uwr.downloadHandler.text.Substring(0, Mathf.Min(200, uwr.downloadHandler.text.Length))}...");
                onResult?.Invoke(response);
            }
        }

        private IEnumerator PostCo(string endpointUrl, string json, Action<Response> onResult)
        {
            Debug.Log($"🚀 Starting POST request to: {endpointUrl}");
            Debug.Log($"📤 POST Data: {json.Substring(0, Mathf.Min(200, json.Length))}...");

            using var uwr = new UnityWebRequest(endpointUrl, "POST");
            var jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
            uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Accept", "application/json");
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.SetRequestHeader("X-CSRFToken", "aTuIPJaLJFwK3DVR3qYn8XzJuBE59Npu2E9QflBph4MRdbSqA2ayAfLKEQqdHhBs");

#if UNITY_EDITOR
            uwr.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.130 Safari/537.36");
#endif
            yield return uwr.SendWebRequest();

            Debug.Log($"📡 POST Response - Code: {uwr.responseCode}, Result: {uwr.result}");

            if (uwr.result == UnityWebRequest.Result.ConnectionError)
            {
                var response = new Response();
                response.Code = uwr.responseCode;
                response.Text = uwr.error;
                Debug.LogError($"❌ POST Connection Error: {uwr.error}");
                onResult?.Invoke(response);
            }
            else if (uwr.result == UnityWebRequest.Result.DataProcessingError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                var response = new Response();
                response.Code = uwr.responseCode;
                response.Text = uwr.error;
                Debug.LogError($"❌ POST Protocol/Data Error: {uwr.error}");
                onResult?.Invoke(response);
            }
            else
            {
                var response = new Response();
                response.Code = uwr.responseCode;
                response.Text = $"Code: {uwr.responseCode}:\n" + uwr.downloadHandler.text;
                Debug.Log($"✅ POST Success: {uwr.responseCode} - {uwr.downloadHandler.text.Substring(0, Mathf.Min(200, uwr.downloadHandler.text.Length))}...");
                onResult?.Invoke(response);
            }
        }
    }
}