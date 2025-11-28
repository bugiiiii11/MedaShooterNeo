using System;
using System.Collections.Generic;

namespace Rene.Sdk.Primitives
{
    public sealed class GraphQLLocation : IEquatable<GraphQLLocation>
    {
        public uint Column { get; set; }

        public uint Line { get; set; }

        public override bool Equals(object obj) => Equals(obj as GraphQLLocation);

        public bool Equals(GraphQLLocation other)
        {
            if (other == null)
            { return false; }
            if (ReferenceEquals(this, other))
            { return true; }
            return EqualityComparer<uint>.Default.Equals(Column, other.Column) &&
                EqualityComparer<uint>.Default.Equals(Line, other.Line);
        }

        public override int GetHashCode() =>
            Column.GetHashCode() ^ Line.GetHashCode();

        public static bool operator ==(GraphQLLocation left, GraphQLLocation right) =>
            EqualityComparer<GraphQLLocation>.Default.Equals(left, right);

        public static bool operator !=(GraphQLLocation left, GraphQLLocation right) =>
            !EqualityComparer<GraphQLLocation>.Default.Equals(left, right);
    }
}
