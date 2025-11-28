using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rene.Sdk.Api.User.Data;
using Rene.Sdk.Primitives;
using static Rene.Sdk.Api.User.Data.UserResponse;
using static Rene.Sdk.Api.User.Data.UsersResponse;

namespace Rene.Sdk.Api.User
{
    public class UserAPI
    {
        private API api;

        public UserAPI(API api)
        {
            this.api = api;
        }

        public async Task<bool> Login(string email, string password)
        {
            const string operationName = "LoginUser";

            const string varEmailTerm = "email";
            const string varPasswordTerm = "password";

            var query = $@"mutation {operationName}(${varEmailTerm}: String!, ${varPasswordTerm}: String!) {{ " +
                            $@"{operationName}(input: {{ {varEmailTerm}: ${varEmailTerm},  {varPasswordTerm}: ${varPasswordTerm} }}) {{ " +
                                "gameId jwt scopes userId validUntil } }";

            var variables = new Dictionary<string, object>();
            variables[varEmailTerm] = email;
            variables[varPasswordTerm] = password;

            var loginUserRequest = new GraphQLRequest
            {
                Query = query,
                OperationName = operationName,
                Variables = variables
            };

            var loginUserResponse = await api.SendGraphQLRequest<LoginUserResponse>(loginUserRequest, query, variables);
            if (loginUserResponse?.Data?.LoginUser?.Jwt != null)
            {
                api.AuthToken = loginUserResponse.Data.LoginUser.Jwt;
                Console.WriteLine($"\nReneVerse User {api.UserId} Authorized");
                return true;
            }
            return false;
        }

        public async Task<UserData> Profile()
        {
            const string operationName = "User";

            var query = $@"query {operationName} {{ " +
                            "User { userId email data { firstName lastName } externalAccounts { discordId steamId twitterId } image { url } stats { assets games value }  } }";

            var userRequest = new GraphQLRequest
            {
                Query = query,
                OperationName = operationName
            };

            var userResponse = await api.SendGraphQLRequest<UserResponse>(userRequest, query);

            return userResponse?.Data?.User;
        }

        public async Task<UsersData> Search(string searchTerm)
        {
            const string operationName = "UserSearch";

            const string varUserSearchTerm = "userSearchTerm";

            var query = $@"query {operationName}(${varUserSearchTerm}: String!) {{ " +
                            $@"UserSearch(input: {{ {varUserSearchTerm}: ${varUserSearchTerm} }}) {{ " +
                                "items { userId email data { firstName lastName } externalAccounts { discordId steamId twitterId } image { url } stats { assets games value } } limit nextToken } }";

            var variables = new Dictionary<string, object>();
            variables[varUserSearchTerm] = searchTerm;
            
            var usersSearchRequest = new GraphQLRequest
            {
                Query = query,
                OperationName = operationName,
                Variables = variables
            };

            var usersResponse = await api.SendGraphQLRequest<UsersResponse>(usersSearchRequest, query, variables);
            return usersResponse?.Data?.UserSearch;
        }

    }
}