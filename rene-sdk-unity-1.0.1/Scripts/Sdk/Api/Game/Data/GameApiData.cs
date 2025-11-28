using System;
using System.Collections.Generic;
using ReneVerse;
using UnityEngine;
using static Rene.Sdk.Api.Game.Data.AdSurfacesResponse;

namespace Rene.Sdk.Api.Game.Data
{
    public class GameConnectData
    {
        public string UserId;
        public string Name;
        public string GameId;
        public GameConnectStatus Status;

        public GameConnectData()
        {
        }
    }

    public class GameConnectResponse
    {
        public GameConnectData GameConnect;

        public GameConnectResponse()
        {
        }
    }

    public class OnGameConnectUpdateResponse
    {
        public GameConnectData GameConnectSubscription;

        public OnGameConnectUpdateResponse()
        {
        }
    }

    public class DelegatedTokenResponse
    {
        public DelegatedTokenData OnGameConnectAuthUpdate;

        public DelegatedTokenResponse()
        {
        }

        public class DelegatedTokenData
        {
            public string GameId;
            public string Jwt;
            public List<Scope> Scopes;
            public string UserId;
            public string ValidUntil;

            public DelegatedTokenData()
            {
            }
        }
    }

    public class GameResponse
    {
        public GameData Game;

        public GameResponse()
        {
        }
        [Serializable]
        public class GameData
        {
            public string GameId;
            public string Name;
            public GameDataa Data;
            public List<string> Genres;
            public S3File Image;
            public GameStats Stats;
            public List<GameUrl> Urls;

            public AdSurfacesData AdSurfaces;
            public CollectionsData Collections;

            public class GameDataa
            {
                public string Description;
            }

            public class GameStats
            {
                public string MintedAssets;
                public string WalletAssets;
                public string Players;
                public string Value;
            }

            public class GameUrl
            {
                public string Type;
                public string Url;
            }
[Serializable]
            public class CollectionsData
            {
                public List<CollectionData> Items;
                public int? Limit;
                public string NextToken;

                public CollectionsData()
                {
                }
[Serializable]
                public class CollectionData
                {
                    public string Name;
                    public string CollectionId;
                    public string Description;
                    public S3File Image;

                    public AssetType Type;
                    public CrossGameState CrossGameState;

                    public CollectionStats Stats;

                    public BrandedAssetsResponse.BrandedAssetsData BrandedAssets;
                    public OwnableAssetsResponse.OwnableAssetsData OwnableAssets;

                    public string UpdatedAt;

                    public class CollectionStats
                    {
                        public string AdCampaigns;
                        public string Assets;
                        public string Games;
                        public string Impressions;
                        public string Interactions;
                        public string Value;
                    }
                }
            }
        }
    }

    public class AssetsResponse
    {
        public AssetsData Assets;

        public AssetsResponse()
        {
        }

        public class AssetsData
        {
            public List<Asset> Items;
            public string Limit;
            public string NextToken;

            public AssetsData()
            {
            }

            public class Asset
            {
                public string AssetTemplateId;
                public string CId;
                public string GameId;
                public string NftId;
                public string WalletAddress;

                public AssetMetadata Metadata;

                public Asset()
                {
                }

                public class AssetMetadata
                {
                    public string Name;
                    public string Description;
                    public string Image;
                    public string AnimationUrl;
                    public List<AssetAttribute> Attributes;

                    public AssetMetadata()
                    {
                    }

                    public class AssetAttribute
                    {
                        public DisplayTypes DisplayType;
                        public string TraitType;
                        public string Value;
                        public string MaxValue;

                        public AssetAttribute()
                        {
                        }
                    }
                }
            }
        }
    }

    public class AssetMetadata
    {
        public string name;
        public string description;
        public string imageFilename;
        public string animationFilename;
        public List<AssetAttribute> attributes;

        public AssetMetadata()
        {
        }

        public class AssetAttribute
        {
            public string displayType;
            public string traitType;
            public string value;

            public AssetAttribute()
            {
            }
        }
    }

    [Serializable]
    public class S3File
    {
        public string Extension;
        public string FileId;
        public bool IsIpfs;
        public string Name;
        public string UploadUrl;
        public string Url;

        public S3File()
        {
        }
    }
    
    public interface IAsset
    {
        string GetOwnableAssetId { get; }
        string GetName { get; }
        string GetDescription { get; }
        AdSurfacesData GetadSurfaces { get; }
        S3File GetImage { get; }
#if UNITY_EDITOR
        Texture2D GetTexture();
#endif
    }

    [Serializable]
    public class OwnableAssetsResponse
    {
        public OwnableAssetsData OwnableAssets;

        public OwnableAssetsResponse()
        {
        }

        [Serializable]
        public class OwnableAssetsData
        {
            public List<OwnableAsset> Items;
            public string Limit;
            public string NextToken;

            public OwnableAssetsData()
            {
            }



            [Serializable]
            public class OwnableAsset : IAsset
            {
                public string OwnableAssetId;
                public string GetOwnableAssetId => OwnableAssetId;
                public string Name;
                public string GetName => Name;
                public string Description;
                public string GetDescription => Description;
                public List<Attribute> Attributes;
                public AdSurfacesData adSurfaces;
                public AdSurfacesData GetadSurfaces => adSurfaces;
                
                
                public AssetTemplateData Data;
                    
                public S3File Image;
                public S3File GetImage => Image;
                public AssetTemplateFiles Files;
                public List<S3File> GameEngineFiles;
                public bool IsIpfs;
                public AssetTemplateMetadataTemplate MetadataTemplates;

#if UNITY_EDITOR
                public string MainImageTextureString;

                private Texture2D _cachedEditorTexture;

                public Texture2D GetTexture()
                {
                    if (_cachedEditorTexture == null)
                    {
                        // Load the texture and cache it
                        _cachedEditorTexture = Helper.GetTextureFromString(MainImageTextureString);
                    }

                    return _cachedEditorTexture;
                }
#endif

                public OwnableAsset()
                {
                }

                [Serializable]
                public class Attribute
                {
                    public DisplayTypes DisplayType;
                    public string MaxValue;
                    public string TraitType;
                    public List<string> Values;

                    public Attribute()
                    {
                    }
                }

                [Serializable]
                public class AssetTemplateData
                {
                    public string Description;
                    public double? Price;
                    public int? Supply;

                    public AssetTemplateData()
                    {
                    }
                }

                [Serializable]
                public class AssetTemplateFiles
                {
                    public List<S3File> Images;
                    public List<S3File> Animations;

                    public AssetTemplateFiles()
                    {
                    }
                }

                [Serializable]
                public class AssetTemplateMetadataTemplate
                {
                    public string BackgroundColor;
                    public string Description;
                    public string Name;

                    public AssetTemplateMetadataTemplate()
                    {
                    }
                }
            }
        }
    }

    public class BrandedAssetsResponse
    {
        public BrandedAssetsData BrandedAssets;

        public BrandedAssetsResponse()
        {
        }
[Serializable]
        public class BrandedAssetsData
        {
            public List<BrandedAsset> Items;
            public string Limit;
            public string NextToken;

            public BrandedAssetsData()
            {
            }

            [Serializable]
            public class BrandedAsset: IAsset
            {

                public string GetOwnableAssetId => BrandedAssetId;
                public string GetName => Name;
                public string GetDescription => Description;
                public AdSurfacesData GetadSurfaces => adSurfaces;
                public S3File GetImage => Image;
                
                public string BrandedAssetId;
                public BrandData Brand;
                public string Name;
                public string Description;
                public S3File Image;
                public AdSurfacesData adSurfaces;
                public List<S3File> GameEngineFiles;
#if UNITY_EDITOR
                public string MainImageTextureString;

                private Texture2D _cachedEditorTexture;

                public Texture2D GetTexture()
                {
                    if (_cachedEditorTexture == null)
                    {
                        // Load the texture and cache it
                        _cachedEditorTexture = Helper.GetTextureFromString(MainImageTextureString);
                    }

                    return _cachedEditorTexture;
                }
#endif
                public BrandedAsset()
                {
                }

                [Serializable]
                public class BrandData
                {
                    public string BrandId;
                    public string Description;
                    public S3File Image;
                    public string Name;
                    public string UpdatedAt;
                    public string Website;
                }


            }
        }
    }

    [Serializable]
    public class AdSurfacesResponse
    {
        public AdSurfacesData adSurfaceses;

        public AdSurfacesResponse()
        {
        }

        [Serializable]
        public class AdSurfacesData
        {
            public List<AdSurface> Items;
            public int? Limit;
            public string NextToken;

            public AdSurfacesData()
            {
            }

            [Serializable]
            public class AdSurface
            {
                public string AdSurfaceId;
                public AdType AdType;
                public AdInteractivity Interactivity;
                public List<string> TargetingTags;
                public string ResolutionIab;

                public AdSurface()
                {
                }
            }
        }
    }

    public class ServeAdResponse
    {
        public ServeAdData ServeAd;

        public ServeAdResponse()
        {
        }

        public class ServeAdData
        {
            public ServeAdData()
            {
            }

            public string AdId;
            public AdType AdType;
            public string Url;
        }
    }

    public class MintAssetResponse
    {
        public bool MintAsset;

        public MintAssetResponse()
        {
        }
    }

    public class TransferAssetResponse
    {
        public AssetTransfer TransferAsset;

        public TransferAssetResponse()
        {
        }

        public class AssetTransfer
        {
            public string NftId;
            public string RecieverUserId;

            public AssetTransfer()
            {
            }
        }
    }

    public class AddAdViewResponse
    {
        public bool AddAdView;

        public AddAdViewResponse()
        {
        }
    }

    public class MarkAdInteractionResponse
    {
        public string MarkAdInteraction;

        public MarkAdInteractionResponse()
        {
        }
    }

    #region enums

    public enum GameConnectStatus
    {
        REGISTRATION_REQUESTED,
        AUTHORIZATION_REQUESTED,
        CONNECT_UNCONFIRMED,
        CONNECT_CONFIRMED,
        DISCONNECTED
    }

    public enum Scope
    {
        jwt_get_token,
        jwt_refresh_token,
        org_all,
        user_all,
        user_verify_email
    }

    public enum DisplayTypes
    {
        boost_number,
        boost_percentage,
        date,
        number,
        STRING
    }

    [Serializable]
    public enum AdType
    {
        BANNER,
        VIDEO
    }

    [Serializable]
    public enum AdInteractivity
    {
        INTERACTIVE,
        STATIC
    }

    public enum AdVideoAspectRatio
    {
        FOUR_BY_THREE,
        SIXTEEN_BY_NINE
    }

    public enum AdVideoResolution
    {
        EIGHT_K,
        FOUR_EIGHTY,
        FOUR_K,
        FULL_HD,
        HD,
        THREE_SIXTY,
        TWO_FORTY,
        TWO_K
    }


    public enum AssetType
    {
        BRANDED,
        OWNABLE
    }

    public enum CrossGameState
    {
        CLOSED,
        OPEN
    }

    #endregion
}