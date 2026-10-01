using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace LogGrokX.Data.IndexTree
{
    public static class LeafExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetValue<T, TLeaf>(this TLeaf leaf, int index)
            where TLeaf : class, ILeaf<T, TLeaf>, ITreeNode<T>, IEnumerable<T>
        {
            var leafIndex = index >= leaf.MinIndex ? index - leaf.MinIndex : 0;
            return leaf[leafIndex];
        }

        public static IEnumerable<T> GetEnumerableFromIndex<T, TLeaf>(this TLeaf leaf, int index)
            where TLeaf : class, ILeaf<T, TLeaf>, ITreeNode<T>, IEnumerable<T>
        {
            var startIndex = index >= leaf.MinIndex ? index - leaf.MinIndex : 0;
            var next = leaf.Next;
            var firstCount = leaf.Count;
            for (var i = startIndex; i < firstCount; i++)
            {
                yield return leaf[i];
            }

            while (next != null)
            {
                var tempNext = next.Next;
                var count = next.Count;
                for (var j = 0; j < count; j++)
                {
                    yield return next[j];
                }

                next = tempNext;
            }
        }
    }
}
