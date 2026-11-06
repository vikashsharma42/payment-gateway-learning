using System;
using System.Collections.Generic;
using System.Linq;

namespace DineBook.Common.Domain
{
    /// <summary>
    /// Base type for value objects. Two value objects are equal when all their components are equal.
    /// </summary>
    public abstract class ValueObject : IEquatable<ValueObject>
    {
        protected abstract IEnumerable<object> GetEqualityComponents();

        public bool Equals(ValueObject other)
        {
            if (other is null || GetType() != other.GetType())
                return false;

            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override bool Equals(object obj) => obj is ValueObject other && Equals(other);

        public override int GetHashCode() =>
            GetEqualityComponents().Aggregate(0, (hash, component) => HashCode.Combine(hash, component));

        public static bool operator ==(ValueObject left, ValueObject right) =>
            left is null ? right is null : left.Equals(right);

        public static bool operator !=(ValueObject left, ValueObject right) => !(left == right);
    }
}
