using System.Collections.Generic;
using System.Threading;
using DICOMParser;
using Threads;

namespace Segmentation
{
    public class RangeSegmentation : SegmentationStrategy<RangeSegmentation.RangeParameter>
    {
        /// <summary>
        /// Contains Parameters used for the Range segmentation.
        /// </summary>
        public sealed class RangeParameter
        {
            public int Lower { get; set; }

            public int Upper { get; set; }

            public int ThreadCount { get; set; }

            public RangeParameter(int lower, int upper, int threadCount)
            {
                Lower = lower;
                Upper = upper;
                ThreadCount = threadCount;
            }

            public bool IsValid()
            {
                return Lower <= Upper && ThreadCount >= 1;
            }
        }

        /// <summary>
        /// Iterates over the given data, which has to match the allocated size, to check whether data points are inside the segment or not.
        /// </summary>
        /// <param name="segment">The segment this segmentation is going to be applied to.</param>
        /// <param name="data">Base data volume</param>
        /// <param name="parameters">Range Parameters for the segmentation</param>
        /// <returns>The ThreadGroupState to enable progress monitoring and callback on finish. May return null if previous work has not yet been finished.</returns>
        public override ThreadGroupState Fit(Segment segment, IReadOnlyList<int> data, RangeParameter parameters)
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
        /// <param name="segment">The segment this segmentation is going to be applied to.</param>
        /// <param name="data">Base data volume</param>
        /// <param name="rangeParameter">Range Parameters for the segmentation</param>
        /// <param name="threadCount">Number of threads to use</param>
        private void StartFittingRange(Segment segment, IReadOnlyList<int> data, RangeParameter rangeParameter, int threadCount)
        {
            int spacing = segment.Slices / threadCount;

            for (var i = 0; i < threadCount; ++i)
            {
                var startIndex = i * spacing;
                var endIndex = startIndex + spacing;

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
        /// Work portion for one of the worker threads.
        /// </summary>
        /// <param name="segment">The segment this segmentation is going to be applied to.</param>
        /// <param name="data">Base data volume</param>
        /// <param name="rangeParameter">Range Parameters for the segmentation</param>
        /// <param name="startIndex">Slice Index this thread starts working on</param>
        /// <param name="endIndex">Index after the last slice to be worked on</param>
        private void FitRangePartially(Segment segment, IReadOnlyList<int> data, RangeParameter rangeParameter, int startIndex, int endIndex)
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
