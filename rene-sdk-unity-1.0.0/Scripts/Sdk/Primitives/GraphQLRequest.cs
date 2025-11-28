using System;
using System.Collections.Generic;

namespace Rene.Sdk.Primitives
{
    public class GraphQLRequest : Dictionary<string, object>, IEquatable<GraphQLRequest>
    {
        public const string OPERATION_NAME_KEY = "operationName";
        public const string QUERY_KEY = "query";
        public const string VARIABLES_KEY = "variables";
        public const string EXTENSIONS_KEY = "extensions";

        public string Query
        {
            get => TryGetValue(QUERY_KEY, out object value) ? (string)value : null;
            set => this[QUERY_KEY] = value;
        }

        public string OperationName
        {
            get => TryGetValue(OPERATION_NAME_KEY, out object value) ? (string)value : null;
            set => this[OPERATION_NAME_KEY] = value;
        }

        public object Variables
        {
            get => TryGetValue(VARIABLES_KEY, out object value) ? value : null;
            set => this[VARIABLES_KEY] = value;
        }

        public object Extensions
        {
            get => TryGetValue(EXTENSIONS_KEY, out object value) ? value : null;
            set => this[EXTENSIONS_KEY] = value;
        }

        public GraphQLRequest() { }

        public GraphQLRequest(string query, object variables = null, string operationName = null, object extensions = null)
        {
            Query = query;
            Variables = variables;
            OperationName = operationName;
            Extensions = extensions;
        }

        public GraphQLRequest(GraphQLRequest other) : base(other) { }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((GraphQLRequest)obj);
        }

        public virtual bool Equals(GraphQLRequest other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Count == other.Count;
        }

        public override int GetHashCode() => (Query, OperationName, Variables, Extensions).GetHashCode();

        public static bool operator ==(GraphQLRequest left, GraphQLRequest right) => EqualityComparer<GraphQLRequest>.Default.Equals(left, right);

        public static bool operator !=(GraphQLRequest left, GraphQLRequest right) => !(left == right);
    }
}

