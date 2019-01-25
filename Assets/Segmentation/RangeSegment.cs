

namespace Segmentation
{
    public class RangeSegment : Segment<RangeSegment.RangeParameter>
    {

        public sealed class RangeParameter
        {

            public int Lower { get; set; }

            public int Upper { get; set; }

            public int Threadcount { get; set; }

            public RangeParameter(int lower, int upper, int threadcount)
            {
                Lower = lower;
                Upper = upper;
                Threadcount = threadcount;
            }
        }

        public RangeSegment(SegmentationColor color) : base(color)
        {
        }

        public sealed override void Fit(int[] data, RangeParameter parameters)
        {
            for (var i = 0; i < Slices; ++i)
            {
                var idxPartId = i * Width * Height;

                for (var y = 0; y < Height; ++y)
                {
                    var idxPart = idxPartId + y;

                    for (var x = 0; x < Width; ++x)
                    {
                        var value = data[idxPart + x * Height];

                        Set(x, y, i, value >= parameters.Lower && value <= parameters.Upper);  
                    }
                }
            }
        }

        public override bool Done()
        {
            return true;
        }
    }
}

