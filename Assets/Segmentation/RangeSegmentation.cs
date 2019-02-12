using System.Threading;
using Threads;

namespace Segmentation
{
    public class RangeSegmentation : SegmentationStrategy<RangeSegmentation.RangeParameter>
    {

        public sealed class RangeParameter
        {

            public int Lower { get; }

            public int Upper { get; }

            public int ThreadCount { get; }

            public RangeParameter(int lower, int upper, int threadCount)
            {
                Lower = lower;
                Upper = upper;
                ThreadCount = threadCount;
            }
        }

        /// <summary>
        /// Iterates over the given data, which has to match the allocated size, to check whether data points are inside the segment or not.
        /// </summary>
        /// <param name="segment">The segment this segmentation is going to be applied to.</param>
        /// <param name="data">Base data volume</param>
        /// <param name="parameters">Custom Parameter Object</param>
        /// <returns>The ThreadGroupState to enable progress monitoring and callback on finish. May return null if previous work has not yet been finished.</returns>
        public override ThreadGroupState Fit(Segment segment, int[] data, RangeParameter parameters)
        {
            if (segment._currentWorkload.Working > 0)
            {
                return null;
            }

            segment._currentWorkload.Reset();
            segment._currentWorkload.TotalProgress = segment.Slices;
            StartFittingRange(segment, data, parameters, parameters.ThreadCount);

            return segment._currentWorkload;
        }

        /// <summary>
        /// Starts the worker threads
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="data"></param>
        /// <param name="rangeParameter"></param>
        /// <param name="threadCount"></param>
        private void StartFittingRange(Segment segment, int[] data, RangeParameter rangeParameter, int threadCount)
        {
            int spacing = segment.Slices / threadCount;

            for (var i = 0; i < threadCount; ++i)
            {
                int startIndex = i * spacing;
                int endIndex = startIndex + spacing;

                if (i + 1 == threadCount)
                {
                    endIndex = segment.Slices;
                }

                segment._currentWorkload.Register();
                var t = new Thread(() => FitRangePartially(segment, data, rangeParameter, startIndex, endIndex))
                {
                    IsBackground = true
                };
                t.Start();
            }
        }

        /// <summary>
        /// Possible memory corruption with multiple threads if two threads try to access the same long. Needs array of locks...
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="data"></param>
        /// <param name="rangeParameter"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        private void FitRangePartially(Segment segment, int[] data, RangeParameter rangeParameter, int startIndex, int endIndex)
        {
            for (var i = startIndex; i < endIndex; ++i)
            {
                var idxPartId = i * segment.Width * segment.Height;

                for (var y = 0; y < segment.Height; ++y)
                {
                    var idxPart = idxPartId + y;

                    for (var x = 0; x < segment.Width; ++x)
                    {
                        var value = data[idxPart + x * segment.Height];

                        segment.Set(x, y, i, value >= rangeParameter.Lower && value <= rangeParameter.Upper);
                    }
                }
                segment._currentWorkload.IncrementProgress();
            }
            segment._currentWorkload.Done();
        }
    }
}
