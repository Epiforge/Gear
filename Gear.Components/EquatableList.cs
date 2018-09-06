using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Gear.Components
{
    public struct EquatableList<T> : IReadOnlyList<T>, IEquatable<EquatableList<T>>
    {
        public static bool operator ==(EquatableList<T> a, EquatableList<T> b) => a.Equals(b);

        public static bool operator !=(EquatableList<T> a, EquatableList<T> b) => !a.Equals(b);

        public EquatableList(IReadOnlyList<T> elements)
        {
            this.elements = elements;
            hashCode = HashCodes.CombineObjects(this.elements);
        }

        readonly IReadOnlyList<T> elements;
        readonly int hashCode;

        public override bool Equals(object obj) => obj is EquatableList<T> other ? Equals(other) : false;

        public bool Equals(EquatableList<T> other) => other != null && elements.SequenceEqual(other.elements);

        public IEnumerator<T> GetEnumerator() => elements.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => elements.GetEnumerator();

        public override int GetHashCode() => hashCode;

        public T this[int index] => elements[index];

        public int Count => elements.Count;
    }
}
