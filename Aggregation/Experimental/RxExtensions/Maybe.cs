using System;
using System.Collections.Generic;


namespace Aggregation.Experimental.RxExtensions
{
    public struct Maybe<T> : IEquatable<Maybe<T>>
    {
        #region Public Properties
        public bool HasValue
        {
            get
            {
                return this.hasValue;
            }
        }

        public T Value
        {
            get
            {


                return this.value;
            }
        }
        #endregion

        #region Private / Protected
        internal static readonly Maybe<T> Empty = new Maybe<T>();


        private readonly T value;

        private readonly bool hasValue;
        #endregion

        #region Constructors
        internal Maybe(T value)
        {

            this.value = value;

            this.hasValue = true;
        }
        #endregion

        #region Methods
        public static bool operator ==(Maybe<T> first, Maybe<T> second)
        {
            return first.Equals(second);
        }

        public static bool operator !=(Maybe<T> first, Maybe<T> second)
        {
            return !first.Equals(second);
        }

        public override bool Equals(object obj)
        {
            return obj is Maybe<T>
                   && this.Equals((Maybe<T>)obj);
        }

        public bool Equals(Maybe<T> other)
        {
            return this.hasValue == other.hasValue
                   && (!this.hasValue || EqualityComparer<T>.Default.Equals(this.value, other.value));
        }

        public override int GetHashCode()
        {
            return this.hasValue
                       ? this.value == null ? 0 : this.value.GetHashCode()
                       : -1;
        }

        public override string ToString()
        {
            return this.value == null ? string.Empty : this.value.ToString();
        }
        #endregion
    }

    public static class Maybe
    {

        public static Maybe<T> Empty<T>()
        {
            return Maybe<T>.Empty;
        }

        public static Maybe<T> Return<T>(T value)
        {
            var maybe = new Maybe<T>(value);
            return maybe;
        }
    }
}