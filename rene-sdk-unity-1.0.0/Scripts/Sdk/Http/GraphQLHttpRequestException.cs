using System;
using System.Net;
using System.Net.Http.Headers;

namespace Rene.Sdk.Http
{
    public class GraphQLHttpRequestException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public HttpResponseHeaders ResponseHeaders { get; }

        public string Content { get; }

        public GraphQLHttpRequestException(HttpStatusCode statusCode, HttpResponseHeaders responseHeaders, string content) : base($"The HTTP request failed with status code {statusCode}")
        {
            StatusCode = statusCode;
            ResponseHeaders = responseHeaders;
            Content = content;
        }
    }
}
