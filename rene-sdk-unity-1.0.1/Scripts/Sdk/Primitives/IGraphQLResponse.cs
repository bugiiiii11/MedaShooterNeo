using System;
using System.Collections.Generic;

namespace Rene.Sdk.Primitives
{
    public interface IGraphQLResponse
    {
        object Data { get; }

        GraphQLError[] Errors { get; set; }
    }
}