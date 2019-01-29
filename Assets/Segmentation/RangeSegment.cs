

using System.Threading;
using Threads;

namespace Segmentation
{
    public class RangeSegment : Segment<RangeSegment.RangeParameter>
    {

        private readonly ThreadGroupState _currentWorkload;

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
        }

        public RangeSegment(SegmentationColor segmentColor) : base(segmentColor)
        {
            _currentWorkload = new ThreadGroupState();
        }

        /// <inheritdoc />
        public sealed override ThreadGroupState Fit(int[] data, RangeParameter parameters)
        {
            if (_currentWorkload.Working > 0)
            {
                return null;
            }

            _currentWorkload.Reset();
            _currentWorkload.TotalProgress = Slices;
            StartFitting(_currentWorkload, data, parameters, parameters.ThreadCount);

            return _currentWorkload;
        }

        /// <summary>
        /// Starts the worker threads
        /// </summary>
        /// <param name="state"></param>
        /// <param name="data"></param>
        /// <param name="rangeParameter"></param>
        /// <param name="threadCount"></param>
        private void StartFitting(ThreadGroupState state, int[] data, RangeParameter rangeParameter, int threadCount)
        {
            int spacing = Slices / threadCount;

            for (var i = 0; i < threadCount; ++i)
            {
                int startIndex = i * spacing;
                int endIndex = startIndex + spacing;

                if (i + 1 == threadCount)
                {
                    endIndex = Slices;
                }

                state.Register();
                var t = new Thread(() => FitPartially(state, data, rangeParameter, startIndex, endIndex));
                t.IsBackground = true;
                t.Start();
            }
        }

        private void FitPartially(ThreadGroupState state, int[] data, RangeParameter rangeParameter, int startIndex, int endIndex)
        {
            for (var i = startIndex; i < endIndex; ++i)
            {
                var idxPartId = i * Width * Height;

                for (var y = 0; y < Height; ++y)
                {
                    var idxPart = idxPartId + y;

                    for (var x = 0; x < Width; ++x)
                    {
                        var value = data[idxPart + x * Height];

                        Set(x, y, i, value >= rangeParameter.Lower && value <= rangeParameter.Upper);
                    }
                }
                state.IncrementProgress();
            }
            state.Done();
        }

        public override bool Done()
        {
            return true;
        }
    }
}

