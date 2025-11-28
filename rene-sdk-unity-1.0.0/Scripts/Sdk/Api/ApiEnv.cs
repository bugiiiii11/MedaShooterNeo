using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ReneSdk.Rene.Sdk.Api
{
	public enum ApiEnv
	{
		DEV, STAGE, PROD
    }

    public static class Extensions
    {
        public static string Host(this ApiEnv apiEnv)
        {
            switch (apiEnv)
            {
                case ApiEnv.DEV:
                    return "api.dev.reneverse.io";
                case ApiEnv.STAGE:
                    return "api.stg.reneverse.io";
                case ApiEnv.PROD:
                default:
                    return "api.reneverse.io";
                
            }
        }

        public static string GraphQLUri(this ApiEnv apiEnv)
        {
            return $"https://{apiEnv.Host()}/graphql";
        }

        public async static Task<SubscriptionUrls> GetSubscriptionUrls(this ApiEnv apiEnv)
        {
            var client = new HttpClient();
            HttpResponseMessage subscriptionUrlsResponse = await client.GetAsync($"https://{apiEnv.Host()}/subscriptions").ConfigureAwait(false);
            if (subscriptionUrlsResponse.IsSuccessStatusCode)
            {
                var contentString = await subscriptionUrlsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var subscriptionUrls = JsonConvert.DeserializeObject<SubscriptionUrls>(contentString);
                return subscriptionUrls;
            }
            return null;
        }

        public static string SubscriptionGraphQLHost(this SubscriptionUrls subscriptionUrls)
        {
            return subscriptionUrls.Domain;
        }

        public static string RealtimeUri(this SubscriptionUrls subscriptionUrls)
        {
            return $"wss://{subscriptionUrls.RealtimeDomain}{subscriptionUrls.Path}";
        }
    }

    public class SubscriptionUrls
    {
        public string RealtimeDomain { get; set; }
        public string Domain { get; set; }
        public string Path { get; set; }
        public string Region { get; set; }
    }
}

