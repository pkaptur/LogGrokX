using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace LogGrokX.Data.IndexTree
{
    public sealed class LongsLeaf
        : LeafOrNode<long, LongsLeaf>,
            ILeaf<long, LongsLeaf>
    {
        private const int Capacity = 64*1024;
        private const int InitialCapacity = 16;
        private readonly long _firstValue;
        private readonly int _firstIndex;
        private readonly List<int> _storage;
        private int _count;
        private LongsLeaf? _next;
        
        public LongsLeaf(long firstValue, int valueIndex)
        {
            _storage = new List<int>(InitialCapacity) {0};
            _firstIndex = valueIndex;
            _firstValue = firstValue;
            Volatile.Write(ref _count, 1);
        }

        public LongsLeaf? Add(long value, int valueIndex)
        {
            if (_storage.Count < Capacity)
            {
                _storage.Add((int)(value - _firstValue));
                Volatile.Write(ref _count, _storage.Count);
                return null;
            }

            var next = new LongsLeaf(value, valueIndex);
            Volatile.Write(ref _next, next);
            return next;
        }

        public long this[int index] => _firstValue +_storage[index];

        public int Count => Volatile.Read(ref _count);
        public LongsLeaf? Next => Volatile.Read(ref _next);
        
        public override long FirstValue => _firstValue;
        public override int MinIndex => _firstIndex;
        
        public override IEnumerable<long> GetEnumerableFromIndex(int index)
        {
            return this.GetEnumerableFromIndex<long, LongsLeaf>(index);
        }

        public override long GetValue(int index)
        {
            return this.GetValue<long, LongsLeaf>(index);
        }

        public override (int index, LongsLeaf leaf) FindByValue(long value)
        {
            var index = _storage.BinarySearch(0, Volatile.Read(ref _count), (int) (value - _firstValue), null);
            return (_firstIndex + (index >= 0 ? index : ~index), this);
        }

        public IEnumerator<long> GetEnumerator()
        {
            foreach (var value in _storage)
            {
                yield return _firstValue + value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
