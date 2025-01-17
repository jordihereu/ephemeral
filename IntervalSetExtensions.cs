using Optional.Unsafe;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Marsop.Ephemeral
{
    public static class IntervalSetExtensions
    {

        /// <summary>
        /// Checks if the timestamp is included in the interval set
        /// </summary>
        public static bool Covers(this IDisjointIntervalSet set, DateTimeOffset timestamp) =>
            set.Any(x => x.Covers(timestamp));

        public static IDisjointIntervalSet Join(this IDisjointIntervalSet set, IDisjointIntervalSet other)
        {
            var result = set.Consolidate();
            other.ToList().ForEach(x => result.Join(x));
            return result;
        }

        public static IDisjointIntervalSet Join(this IDisjointIntervalSet set, IInterval interval)
        {
            var groups = set.GroupBy(val => val.Intersects(interval)).ToDictionary(g => g.Key, g => g.ToList());

            var nonOverlaps = groups.ContainsKey(false) ? groups[false] : new List<IInterval>();
            var result = new DisjointIntervalSet(nonOverlaps);

            var overlaps = groups.ContainsKey(true) ? groups[true] : new List<IInterval>();
            var newInterval = interval;
            foreach (var overlap in overlaps)
                newInterval = Interval.Join(newInterval, overlap);
            result.Add(newInterval);

            return result;
        }


        public static IDisjointIntervalSet Intersect(this IDisjointIntervalSet set, IInterval interval)
        {
            var intersections = set
                .Select(x => x.Intersect(interval))
                .Where(y => y.HasValue)
                .Select(z => z.ValueOrDefault());
            return new DisjointIntervalSet(intersections);
        }


        /// <summary>
        /// Joins adjacent intervals.
        /// </summary>
        /// <returns> new set with the minimum amount of intervals</returns>
        public static IDisjointIntervalSet Consolidate(this IDisjointIntervalSet set)
        {
            var result = new DisjointIntervalSet();

            var orderedList = set.OrderBy(x => x.Start);

            var cachedItem = orderedList.FirstOrDefault();

            foreach (var item in orderedList.Skip(1))
            {
                if (cachedItem.IsContiguouslyFollowedBy(item))
                {
                    cachedItem = new Interval(cachedItem.Start, item.End, cachedItem.StartIncluded, item.EndIncluded);
                }
                else
                {
                    result.Add(cachedItem);
                    cachedItem = item;
                }
            }

            result.Add(cachedItem);

            return result;
        }
    }
}