using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace LogGrokX.Data.Index
{
   public class CountIndex<TIndex> where TIndex : IIndex<int>
    {
        public const int Granularity = 16384;
        private ImmutableList<List<(IndexKeyNum, int)>> _counts = ImmutableList<List<(IndexKeyNum, int)>>.Empty;
        private readonly IDictionary<IndexKeyNum,TIndex> _indices;
        private volatile bool _isFinished;
        private int _lastIndex = -1;
        private LiveSnapshot? _liveSnapshot;

        private sealed class LiveSnapshot(int index, IReadOnlyList<List<(IndexKeyNum, int)>> counts)
        {
            public int Index { get; } = index;
            public IReadOnlyList<List<(IndexKeyNum, int)>> Counts { get; } = counts;
        }

        public IReadOnlyList<List<(IndexKeyNum, int)>> Counts
        {
            get
            {
                if (_isFinished)
                    return _counts;
                var index = Volatile.Read(ref _lastIndex);
                var cached = Volatile.Read(ref _liveSnapshot);
                if (cached != null && cached.Index == index)
                    return cached.Counts;
                var counts = _counts.Add(MakeCountsSnapshot());
                if (_isFinished)
                    return _counts;
                Volatile.Write(ref _liveSnapshot, new LiveSnapshot(index, counts));
                return counts;
            }
        }

        public CountIndex(IDictionary<IndexKeyNum, TIndex> indices)
        {
            _indices = indices;
        }

        public void Add(int currentIndex, IDictionary<IndexKeyNum, TIndex> indices)
        {
            if (currentIndex % Granularity == 0 && currentIndex != 0)
                UpdateCountsSnapshot();
            Volatile.Write(ref _lastIndex, currentIndex);
        }

        public void Finish(IDictionary<IndexKeyNum, TIndex> indices)
        {
            UpdateCountsSnapshot();
            _isFinished = true;
        }

        private void UpdateCountsSnapshot()
        {
            _counts = _counts.Add(MakeCountsSnapshot());
        }

        private List<(IndexKeyNum, int)> MakeCountsSnapshot()
        {
            var snapshotList = new List<(IndexKeyNum, int)>(_indices.Count);
            

#pragma warning disable CS8619
            foreach (var (key, value) in _indices)
#pragma warning restore CS8619
            {
                snapshotList.Add((key, value.Count));
            }

            return snapshotList;
        }
    }
}
