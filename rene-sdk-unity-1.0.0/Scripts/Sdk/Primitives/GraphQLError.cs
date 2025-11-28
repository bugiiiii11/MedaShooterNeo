using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace Rene.Sdk.Primitives
{
    public class GraphQLError : IEquatable<GraphQLError>
    {
        [DataMember(Name = "locations")]
        public GraphQLLocation[] Locations { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "path")]
        public List<object> Path { get; set; }

        public override bool Equals(object obj) => Equals(obj as GraphQLError);

        public bool Equals(GraphQLError other)
        {
            if (other == null)
            { return false; }
            if (ReferenceEquals(this, other))
            { return true; }
            {
                if (Locations != null && other.Locations != null)
                {
                    if (!Locations.SequenceEqual(other.Locations))
                    { return false; }
                }
                else if (Locations != null && other.Locations == null)
                { return false; }
                else if (Locations == null && other.Locations != null)
                { return false; }
            }
            if (!EqualityComparer<string>.Default.Equals(Message, other.Message))
            { return false; }
            {
                if (Path != null && other.Path != null)
                {
                    if (!Path.SequenceEqual(other.Path))
                    { return false; }
                }
                else if (Path != null && other.Path == null)
                { return false; }
                else if (Path == null && other.Path != null)
                { return false; }
            }
            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = 0;
            if (Locations != null)
            {
                hashCode ^= EqualityComparer<GraphQLLocation[]>.Default.GetHashCode(Locations);
            }
            hashCode ^= EqualityComparer<string>.Default.GetHashCode(Message);
            if (Path != null)
            {
                hashCode ^= EqualityComparer<dynamic>.Default.GetHashCode(Path);
            }
            return hashCode;
        }

        public static bool operator ==(GraphQLError left, GraphQLError right) =>
            EqualityComparer<GraphQLError>.Default.Equals(left, right);

        public static bool operator !=(GraphQLError left, GraphQLError right) =>
            !EqualityComparer<GraphQLError>.Default.Equals(left, right);
    }
}
