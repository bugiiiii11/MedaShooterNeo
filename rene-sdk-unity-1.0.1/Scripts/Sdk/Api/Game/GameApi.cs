using Rene.Sdk.Api.Game.Data;
using Rene.Sdk.Api.Helpers;
using Rene.Sdk.Http;
using Rene.Sdk.Primitives;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Rene.Sdk.Api.Game.Data.AdSurfacesResponse.AdSurfacesData;
using static Rene.Sdk.Api.Game.Data.AdSurfacesResponse;
using static Rene.Sdk.Api.Game.Data.AssetsResponse;
using static Rene.Sdk.Api.Game.Data.AssetsResponse.AssetsData;
using static Rene.Sdk.Api.Game.Data.OwnableAssetsResponse;
using static Rene.Sdk.Api.Game.Data.TransferAssetResponse;
using System.Linq;
using static Rene.Sdk.Api.Game.Data.BrandedAssetsResponse;
using static Rene.Sdk.Api.Game.Data.GameResponse;

namespace Rene.Sdk.Api.Game
{
    public class GameAPI
    {
        private API api;

        public GameAPI(API api)
        {
            this.api = api;
        }

        public async Task<bool> Connect(string email, Action<GraphQLHttpRequestException> onGraphQlHttpRequestException = null)
        {
            api.UserId = null;
            api.AuthToken = null;
            const string operationName = "GameConnect";
            const string varEmail = "email";

            var query = $@"mutation {operationName}(${varEmail}: String!) {{ " +
                            $@"GameConnect({varEmail}: ${varEmail}) {{" +
                                "gameId name userId status } }";

            var variables = new Dictionary<string, object>();
            variables[varEmail] = email;

            var gameConnectRequest = new GraphQLRequest
            {
                Query = query,
                OperationName = operationName,
                Variables = variables
            };

            var tcs = new TaskCompletionSource<bool>();

            // Subscribe to the OnAuthorized event
            api.OnAuthorized += () => tcs.SetResult(true);

            try
            {
                var gameConnectResponse = await api.SendGraphQLRequest<GameConnectResponse>(gameConnectRequest, query, variables);
                api.UserId = gameConnectResponse?.Data?.GameConnect?.UserId;

                // Run the GameConnectAsyncTask
                Task gameConnectTask = GameConnectAsyncTask();

                // Wait for either the GameConnectAsyncTask to complete or the OnAuthorized event to trigger
                var completedTask = await Task.WhenAny(gameConnectTask, tcs.Task);

                return completedTask == tcs.Task && tcs.Task.Result;
            }
            catch (GraphQLHttpRequestException graphQlHttpRequestException)
            {
                onGraphQlHttpRequestException?.Invoke(graphQlHttpRequestException);
                return false;
            }
        }

        public async Task<GameData> GameData()
        {
            const string operationName = "GetGameDetails";

            var query = $@"query {operationName} {{ " +
                        "Game { gameId name data { description } genres image { name url extension } " +
                        "stats { mintedAssets players value walletAssets } urls { type url } " +
                        "adSurfaces { items { adSurfaceId adType resolutionIab interactivity targetingTags } } " +
                        "collections { items { collectionId crossGameState description image { name url extension } name " +
                        "ownableAssets { items { ownableAssetId name attributes { displayType maxValue traitType values } data { description price supply } " +
                        "files { animations { name url extension } images { name url extension } } gameEngineFiles { name url extension } " +
                        "image { name url extension } metadataTemplates { backgroundColor description name } " +
                        "adSurfaces { items { adSurfaceId adType resolutionIab interactivity targetingTags } limit nextToken } } } " +
                        "brandedAssets { items { brandedAssetId name brand { brandId description image { name url extension } name website } description " +
                        "   gameEngineFiles { name url extension } image { name url extension } " +
                        "   adSurfaces { items { adSurfaceId adType resolutionIab interactivity targetingTags } limit nextToken } } } " +
                        "stats { adCampaigns assets games impressions interactions value } type updatedAt } limit nextToken } } } ";

            var gameRequest = new GraphQLRequest
            {
                Query = query,
                OperationName = operationName
            };

            var gameResponse = await api.SendGraphQLRequest<GameResponse>(gameRequest, query);

            return gameResponse?.Data?.Game;
        }

        #region Assets

        /// <summary>
        /// Retrieves a SINGLE Asset based on the provided asset ID.
        /// </summary>
        /// <param name="assetId">The unique identifier for the asset you want to retrieve.</param>
        /// <returns>An Asset object if an asset with the given ID exists, or null otherwise.</returns>
        public async Task<Asset> Asset(string assetId)
        {
            var assetsData = await Assets(assetId: assetId, limit: 1);
            return assetsData.Items.FirstOrDefault();
        }

        /// <summary>
        /// Retrieves a list of ALL Assets based on the provided parameters.
        /// </summary>
        /// <param name="limit">The maximum number of Assets to retrieve. Defaults to 10 if not specified.</param>
        /// <param name="nextToken">The token for pagination. Use this to retrieve the next set of Assets in a paginated response.</param>
        /// <returns>A list of Assets that match the provided parameters, including associated metadata and attributes.</returns>
        /// 
        public async Task<AssetsResponse.AssetsData> UserAssets(string nextToken = null, int limit = 10)
        {
            return await Assets(string.Empty, limit, nextToken);
        }

        private async Task<AssetsResponse.AssetsData> Assets(string assetId, int limit = 10, string nextToken = null, string nftId = null)
        {
            const string operationName = "Assets";

            const string varLimit = "limit";
            const string varNextToken = "nextToken";
            const string varNftId = "nftId";
            const string varAssetId = "assetId";

            var query = $@"query {operationName}(${varLimit}: String, ${varNextToken}: String, ${varNftId}: String, ${varAssetId}: String) {{ " +
                            $@"Assets(input: {{ {varLimit}: ${varLimit}, {varNextToken}: ${varNextToken}, {varNftId}: ${varNftId}, {varAssetId}: ${varAssetId} }}) {{ " +
                                "items { assetTemplateId cId gameId metadata { name description image animationUrl " +
                                "attributes { traitType value } } nftId walletAddress } limit nextToken } }";

            var variables = new Dictionary<string, object>();
            if (nftId.IsNotNullOrEmpty())
            {
                variables[varNftId] = nftId;
            }
            if (assetId.IsNotNullOrEmpty()) // Added assetId condition
            {
                variables[varAssetId] = assetId;
            }
            if (nextToken.IsNotNullOrEmpty())
            {
                variables[varLimit] = limit;
                variables[varNextToken] = nextToken;
            };

            var userAssetsRequest = new GraphQLRequest
            {
                Query = query,
                OperationName = operationName,
                Variables = variables
            };

            var userAssetsResponse = await api.SendGraphQLRequest<AssetsResponse>(userAssetsRequest, query, variables);
            return userAssetsResponse?.Data?.Assets;
        }

        #endregion

        #region AdSurfaces

        /// <summary>
        /// Retrieves a SINGLE AdSurface based on the provided adSurfaceId.
        /// </summary>
        /// <param name="adSurfaceId">The unique identifier for the AdSurface you want to retrieve.</param>
        /// <param name="assetTemplateId">The ID of the asset template to filter the Ad Surfaces. Required to select a specific Asset Template.</param>
        /// <returns>An AdSurface object if an AdSurface with the given ID exists, or null otherwise.</returns>
        public async Task<AdSurfacesData.AdSurface> AdSurface(string assetTemplateId, string adSurfaceId)
        {
            var adSurfacesData = await AdSurfaces(assetTemplateId, limit: 1, adSurfaceId: adSurfaceId);
            return adSurfacesData.Items.FirstOrDefault();
        }

        /// <summary>
        /// Retrieves a list of ALL Ad Surfaces in the Template based on the provided asset template ID and limit.
        /// </summary>
        /// <param name="assetTemplateId">The ID of the asset template to filter the Ad Surfaces. Required to select a specific Asset Template.</param>
        /// <param name="limit">The maximum number of Ad Surfaces to retrieve. Defaults to 10 if not specified.</param>
        /// <returns>A list of Ad Surfaces that match the provided asset template ID and limit.</returns>

        public async Task<AdSurfacesData> AdSurfaces(string assetTemplateId, string nextToken = null, int limit = 10)
        {
            return await AdSurfaces(assetTemplateId, nextToken, limit, string.Empty);
        }

        private async Task<AdSurfacesData> AdSurfaces(string assetTemplateId, string nextToken = null, int limit = 10, string adSurfaceId = null)
        {
            const string operationName = "AdSurfaces";

            const string varLimit = "limit";
            const string varNextToken = "nextToken";
            const string varAdSurfaceId = "adSurfaceId";
            const string varAssetTemplateId = "assetTemplateId";

            var query = $@"query {operationName}(${varLimit}: String, ${varNextToken}: String, ${varAdSurfaceId}: String, ${varAssetTemplateId}: String) {{ " +
                            $@"AdSurfaces(input: {{ {varLimit}: ${varLimit}, {varNextToken}: ${varNextToken}, {varAdSurfaceId}: ${varAdSurfaceId}, {varAssetTemplateId}: ${varAssetTemplateId} }}) {{ " + // Added assetTemplateId here
                                "items { adSurfaceId adType height interactivity targetingTags videoAspectRatio videoResolution width } limit nextToken } }";

            var variables = new Dictionary<string, object>();
            if (adSurfaceId.IsNotNullOrEmpty())
            {
                variables[varAdSurfaceId] = adSurfaceId;
            }
            if (assetTemplateId.IsNotNullOrEmpty())
            {
                variables[varAssetTemplateId] = assetTemplateId;
            }
            if (nextToken.IsNotNullOrEmpty())
            {
                variables[varLimit] = limit;
                variables[varNextToken] = nextToken;
            };

            var adSurfacesRequest = new GraphQLRequest
            {
                Query = query,
                OperationName = operationName,
                Variables = variables
            };

            var adSurfacesResponse = await api.SendGraphQLRequest<AdSurfacesResponse>(adSurfacesRequest, query, variables);
            return adSurfacesResponse?.Data?.adSurfaceses;
        }

        #endregion

        #region AssetTemplates

        /// <summary>
        /// Retrieves a list of Ownable Asset based on the provided parameters.
        /// </summary>
        /// <param name="limit">The maximum number of Ownable Assets to retrieve. Defaults to 10 if not specified. Optional.</param>
        /// <param name="nextToken">The token for pagination. Use this to retrieve the next set of Ownable Assets in a paginated response. Optional.</param>
        /// <param name="ownableAssetId">The ID of a specific Ownable Assets to retrieve. If specified, only the Ownable Assets with this ID will be retrieved. Optional.</param>
        /// <returns> <see cref="GameRelatedPersistentData"/> that match the provided parameters, including associated Ad Surfaces.</returns>

        public async Task<OwnableAssetsResponse.OwnableAssetsData> OwnableAssets(int limit = 10, string nextToken = null, string ownableAssetId = null)
        {
            const string operationName = "OwnableAssets";


            const string varLimit = "limit";
            const string varNextToken = "nextToken";
            const string varOwnableAssetId = "ownableAssetId";

            var query = $@"query {operationName}(${varLimit}: String, ${varNextToken}: String, ${varOwnableAssetId}: String) {{ " +
                           $@"{operationName}(input: {{ {varLimit}: ${varLimit}, {varNextToken}: ${varNextToken}, {varOwnableAssetId}: ${varOwnableAssetId} }}) {{ " +
                               "items { ownableAssetId name attributes { displayType maxValue traitType values } data { description price supply } " +
                               "files { animations { name url extension } images { name url extension } } gameEngineFiles { name url extension } " +
                               "image { name url extension } metadataTemplates { backgroundColor description name } " +
                               "adSurfaces { items { adSurfaceId adType interactivity targetingTags resolutionIab } limit nextToken } } " + // Added adSurfaces field
                               "limit nextToken } }";


            var variables = new Dictionary<string, object>();
            if (ownableAssetId.IsNotNullOrEmpty())
            {
                variables[varOwnableAssetId] = ownableAssetId;
            }
            else if (nextToken.IsNotNullOrEmpty())
            {
                variables[varLimit] = limit;
                variables[varNextToken] = nextToken;
            };

            var assetTemplatesRequest = new GraphQLRequest
            {
                Query = query,
                OperationName = operationName,
                Variables = variables
            };

            var assetTemplatesResponse = await api.SendGraphQLRequest<OwnableAssetsResponse>(assetTemplatesRequest, query, variables);
            return assetTemplatesResponse?.Data?.OwnableAssets;
        }

        public async Task<BrandedAssetsData> BrandedAssets(int limit = 10, string nextToken = null, string brandedAssetId = null)
        {
            const string operationName = "BrandedAssets";


            const string varLimit = "limit";
            const string varNextToken = "nextToken";
            const string varBrandedAssetId = "brandedAssetId";

            var query = $@"query {operationName}(${varLimit}: String, ${varNextToken}: String, ${varBrandedAssetId}: String) {{ " +
                           $@"{operationName}(input: {{ {varLimit}: ${varLimit}, {varNextToken}: ${varNextToken}, {varBrandedAssetId}: ${varBrandedAssetId} }}) {{ " +
                               "items { brandedAssetId name brand { brandId description image { name url extension } name website } description " +
                               "gameEngineFiles { name url extension } image { name url extension } " +
                               "adSurfaces { items { adSurfaceId adType resolutionIab interactivity targetingTags } limit nextToken } } " + // Added adSurfaces field
                               "limit nextToken } }";



            var variables = new Dictionary<string, object>();
            if (brandedAssetId.IsNotNullOrEmpty())
            {
                variables[varBrandedAssetId] = brandedAssetId;
            }
            else if (nextToken.IsNotNullOrEmpty())
            {
                variables[varLimit] = limit;
                variables[varNextToken] = nextToken;
            };

            var brandedAssetsRequest = new GraphQLRequest
            {
                Query = query,
                OperationName = operationName,
                Variables = variables
            };

            var brandedAssetsResponse = await api.SendGraphQLRequest<BrandedAssetsResponse>(brandedAssetsRequest, query, variables);
            return brandedAssetsResponse?.Data?.BrandedAssets;
        }
        #endregion

        #region AssetMint

        public async Task<bool> AssetMint(string assetTemplateId)
        {
            return await AssetMint(assetTemplateId, null, true);
        }

        public async Task<bool> AssetMint(string assetTemplateId, AssetMetadata assetMetadata)
        {
            return await AssetMint(assetTemplateId, assetMetadata, true);
        }

        public async Task<bool> AssetMint(string assetTemplateId, AssetMetadata assetMetadata, bool isTestNet)
        {
            const string operationName = "MintAsset";

            const string varAssetTemplateId = "assetTemplateId";
            const string varIsTestNet = "isTestNet";
            const string varMetadata = "metadata";
            const string varUserId = "userId";

            var query = $@"mutation {operationName}(${varAssetTemplateId}: String!, ${varIsTestNet}: Boolean!, ${varMetadata}: AssetMetadataInput, ${varUserId}: String) {{ " +
                            $@"MintAsset(input: {{ {varAssetTemplateId}: ${varAssetTemplateId}, {varIsTestNet}: ${varIsTestNet}, {varMetadata}: ${varMetadata}, {varUserId}: ${varUserId}}}) }}";

            var variables = new Dictionary<string, object>();
            variables[varAssetTemplateId] = assetTemplateId;
            variables[varIsTestNet] = isTestNet;
            variables[varUserId] = api.UserId;
            if (assetMetadata != null)
            {
                variables[varMetadata] = assetMetadata;
            }

            var assetMintRequest = new GraphQLRequest
            {
                Query = query,
                OperationName = operationName,
                Variables = variables
            };

            var assetMintResponse = await api.SendGraphQLRequest<MintAssetResponse>(assetMintRequest, query, variables);
            return (bool)(assetMintResponse?.Data?.MintAsset);
        }

        #endregion


        #region ServeAd

        public async Task<ServeAdResponse.ServeAdData> ServeAd(string adSurfaceId, bool GraphQLRequest = true, bool SendGraphQLRequest = true)
        {
            const string operationName = "ServeAd";
            const string varAdSurfaceId = "adSurfaceId";

            // Corrected string interpolation syntax
            var query = $@"query {operationName}(${varAdSurfaceId}: String!) {{ " +
                $"ServeAd({varAdSurfaceId}: ${varAdSurfaceId}) {{ adId adType url }} }}";


            var variables = new Dictionary<string, object> { { varAdSurfaceId, adSurfaceId } };

            if (!GraphQLRequest) return default;
            var serveAdRequest = new GraphQLRequest
            {
                Query = query,
                OperationName = operationName,
                Variables = variables
            };
            if (!SendGraphQLRequest) return default;
            try
            {
                var serveAdResponse = await api.SendGraphQLRequest<ServeAdResponse>(serveAdRequest, query, variables);
                bool isHavingNoErrors = serveAdResponse.Errors != null && serveAdResponse.Errors.Any();
                if (isHavingNoErrors)
                {
                    var firstError = serveAdResponse.Errors.First();
                    API.Logger.Log("Error: " + firstError.Message +
                        $" Check that {adSurfaceId} is a valid {nameof(adSurfaceId)} for {nameof(ServeAd)} method");
                    return default;
                }
                return serveAdResponse?.Data?.ServeAd;
            }
            catch (Newtonsoft.Json.JsonSerializationException ex)
            {
                API.Logger.Log($"No ad received meaning there is no ad campaign running applicable for this adsurface with surface ID {adSurfaceId}\n"
        + nameof(Newtonsoft.Json.JsonSerializationException) + ex.Message);
                return default;
            }
        }




        #endregion

        public async Task<bool> AddAdView(string viewId, bool viewed, string viewedDurationInSeconds)
        {
            const string operationName = "AddAdView";

            const string varViewId = "viewId";
            const string varViewed = "viewed";
            const string varViewedDurationInSeconds = "viewedDurationInSeconds";

            var query = $@"mutation {operationName}(${varViewId}: String!, ${varViewed}: Boolean!, ${varViewedDurationInSeconds}: String!) {{ " +
                            $@"AddAdView(input: {{ {varViewId}: ${varViewId}, {varViewed}: ${varViewed}, {varViewedDurationInSeconds}: ${varViewedDurationInSeconds}}}) }}";

            var variables = new Dictionary<string, object>();
            variables[varViewId] = viewId;
            variables[varViewed] = viewed;
            variables[varViewedDurationInSeconds] = viewedDurationInSeconds;

            var addAdViewRequest = new GraphQLRequest
            {
                Query = query,
                OperationName = operationName,
                Variables = variables
            };

            var addAdViewResponse = await api.SendGraphQLRequest<AddAdViewResponse>(addAdViewRequest, query, variables);
            return (bool)(addAdViewResponse?.Data?.AddAdView);
        }

        public async Task<string> MarkAdInteraction(string adId, string email)
        {
            const string operationName = "MarkAdInteraction";

            const string varAdId = "adId";
            const string varEmail = "email";

            var query = $@"mutation {operationName}(${varAdId}: String!, ${varEmail}: String) {{ " +
                            $@"AddAdView(input: {{ {varAdId}: ${varAdId}, {varEmail}: ${varEmail}}}) }}";

            var variables = new Dictionary<string, object>();
            variables[varAdId] = adId;
            variables[varEmail] = email;

            var markAdInteractionRequest = new GraphQLRequest
            {
                Query = query,
                OperationName = operationName,
                Variables = variables
            };

            var markAdInteractionResponse = await api.SendGraphQLRequest<MarkAdInteractionResponse>(markAdInteractionRequest, query, variables);
            return markAdInteractionResponse?.Data?.MarkAdInteraction;
        }

        public async Task<AssetTransfer> AssetTransfer(string assetId, string userId)
        {
            const string operationName = "TransferAsset";

            const string varNftId = "nftId";
            const string varRecieverUserId = "recieverUserId";

            var query = $@"mutation {operationName}(${varNftId}: String!, ${varRecieverUserId}: String!) {{ " +
                            $@"TransferAsset(input: {{ {varNftId}: ${varNftId}, {varRecieverUserId}: ${varRecieverUserId} }}) {{ " +
                                "nftId recieverUserId } }";

            var variables = new Dictionary<string, object>();
            variables[varNftId] = assetId;
            variables[varRecieverUserId] = userId;

            var assetTransferRequest = new GraphQLRequest
            {
                Query = query,
                OperationName = operationName,
                Variables = variables
            };

            var assetTransferResponse = await api.SendGraphQLRequest<TransferAssetResponse>(assetTransferRequest, query, variables);

            return assetTransferResponse?.Data?.TransferAsset;
        }

        private async Task GameConnectAsyncTask()
        {
            const string operationName = "OnGameConnectAuthUpdateOp";
            const string varUserId = "userId";

            var query = $@"subscription {operationName} ( ${varUserId}: String!) {{ " +
                            $@"OnGameConnectAuthUpdate({varUserId}: ${varUserId}) {{ " +
                            "gameId jwt scopes userId validUntil } }";

            var variables = new Dictionary<string, object>();
            variables[varUserId] = api.UserId;

            var onGameConnectAuthUpdateRequest = new GraphQLRequest
            {
                Query = query,
                OperationName = operationName,
                Variables = variables
            };

            var appSyncClientWebSocket = await api.CreateAppSyncWebsocketConnection(query, variables);

            var appSyncHeader = await api.AppSyncHeader(query, variables);
            await appSyncClientWebSocket.AppSyncSubscribe<DelegatedTokenResponse>(appSyncHeader,
                onGameConnectAuthUpdateRequest, (response) =>
                {
                    if (response?.Data?.OnGameConnectAuthUpdate?.Jwt != null)
                    {
                        api.AuthToken = response.Data.OnGameConnectAuthUpdate.Jwt;
                        Console.WriteLine($"\nReneVerse User {api.UserId} Authorized");
                        return true;
                    }
                    return false;
                });
        }
    }
}