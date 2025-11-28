using System.Collections.Generic;
using static Rene.Sdk.Api.User.Data.UserResponse;

namespace Rene.Sdk.Api.User.Data
{
    public class LoginUserResponse
    {
        public DelegatedTokenData LoginUser { get; set; }

        public LoginUserResponse() { }

        public class DelegatedTokenData
        {
            public string GameId { get; set; }
            public string Jwt { get; set; }
            public List<Scope> Scopes { get; set; }
            public string UserId { get; set; }
            public string ValidUntil { get; set; }

            public DelegatedTokenData() { }

            public enum Scope
            {
                jwt_get_token, jwt_refresh_token, org_all, user_all, user_verify_email
            }
        }
    }

    public class UserResponse
    {
        public UserData User { get; set; }

        public UserResponse() { }

        public class UserData
        {
            public string UserId { get; set; }
            public string Email { get; set; }
            public string OrgId { get; set; }
            public bool IsActive { get; set; }
            public string WalletAddress { get; set; }
            public UserDetailsData Data { get; set; }
            public UserStats Stats { get; set; }
            public UserVerified Verified { get; set; }
            public List<Scope> Scopes { get; set; }
            public UserRoles Role { get; set; }
            public S3File Image { get; set; }
            public UserExternalAccounts ExternalAccounts { get; set; }

            public UserData() { }

            public class UserDetailsData
            {
                public string FirstName { get; set; }
                public string LastName { get; set; }

                public UserDetailsData() { }
            }

            public class UserStats
            {
                public string Assets { get; set; }
                public string Games { get; set; }
                public string Value { get; set; }

                public UserStats() { }
            }

            public class UserVerified
            {
                public bool IsEmail { get; set; }

                public UserVerified() { }
            }

            public enum UserRoles
            {
                GAMER, GAMER_DEV
            }

            public class S3File
            {
                public string Extension { get; set; }
                public string FileId { get; set; }
                public string Name { get; set; }
                public string Url { get; set; }
                public string UploadUrl { get; set; }
                public bool IsIpfs { get; set; }

                public S3File() { }
            }

            public class UserExternalAccounts
            {
                public string DiscordId { get; set; }
                public string SteamId { get; set; }
                public string TwitterId { get; set; }

                public UserExternalAccounts() { }
            }

            public enum Scope
            {
                jwt_get_token, jwt_refresh_token, org_all, user_all, user_verify_email
            }

        }
    }

    public class UsersResponse
    {
        public UsersData UserSearch { get; set; }

        public UsersResponse() { }

        public class UsersData
        {
            public List<UserData> Items { get; set; }
            public int? Limit { get; set; }
            public string NextToken { get; set; }

            public UsersData() { }
        }
    }
}