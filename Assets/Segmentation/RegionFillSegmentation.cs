using System;
using System.Collections.Generic;
using System.Threading;
using Threads;
using UnityEngine;

namespace Segmentation
{
    public class RegionFillSegmentation : SegmentationStrategy<RegionFillSegmentation.RegionFillParameter>
    {
        public sealed class RegionFillParameter
        {
            public Vector3Int Seed { get; set; }

            public int Threshold { get; set; }

            public RegionFillParameter(Vector3Int seed, int threshold = 0)
            {
                Seed = seed;
                Threshold = threshold;
            }
        }

        public override ThreadGroupState Fit(Segment segment, int[] data, RegionFillParameter parameters)
        {
            if (segment._currentWorkload.Working > 0)
            {
                return null;
            }

            segment._currentWorkload.Reset();
            segment._currentWorkload.TotalProgress = 1;

            segment._currentWorkload.Register();

            var t = new Thread(() => StartRegionFill(segment, data, parameters))
            {
                IsBackground = true
            };

            t.Start();

            return segment._currentWorkload;
        }

        private void StartRegionFill(Segment segment, IReadOnlyList<int> data,
            RegionFillParameter regionFillParameter)
        {
            var pending = new Queue<Vector3Int>(100);
            var visited = new HashSet<Vector3Int>();

            pending.Enqueue(regionFillParameter.Seed);
            var intensityBase = data[GetIndex(pending.Peek(), segment.Width, segment.Height)];

            var intensityLower = data[intensityBase - regionFillParameter.Threshold];
            var intensityUpper = intensityBase + regionFillParameter.Threshold;

            while (pending.Count > 0)
            {
                var currentVec = pending.Dequeue();
                var currentIdx = GetIndex(currentVec, segment.Width, segment.Height);


                if (!(data[currentIdx] >= intensityLower && data[currentIdx] <= intensityUpper))
                {
                    continue;
                }

                segment.Set(currentVec.x, currentVec.y, currentVec.z);
                visited.Add(currentVec);

                Vector3Int neighbor;

                if (currentVec.x > 0)
                {
                    neighbor = new Vector3Int(currentVec.x - 1, currentVec.y, currentVec.z);

                    if (!visited.Contains(neighbor))
                    {
                        pending.Enqueue(neighbor);
                    }
                }
                if (currentVec.x < segment.Width - 1)
                {
                    neighbor = new Vector3Int(currentVec.x + 1, currentVec.y, currentVec.z);
                    if (!visited.Contains(neighbor))
                    {
                        pending.Enqueue(neighbor);
                    }
                }
                if (currentVec.y > 0)
                {
                    neighbor = new Vector3Int(currentVec.x, currentVec.y - 1, currentVec.z);

                    if (!visited.Contains(neighbor))
                    {
                        pending.Enqueue(neighbor);
                    }
                }
                if (currentVec.y < segment.Height - 1)
                {
                    neighbor = new Vector3Int(currentVec.x, currentVec.y + 1, currentVec.z);
                    if (!visited.Contains(neighbor))
                    {
                        pending.Enqueue(neighbor);
                    }
                }
                if (currentVec.z > 0)
                {
                    neighbor = new Vector3Int(currentVec.x, currentVec.y, currentVec.z - 1);
                    if (!visited.Contains(neighbor))
                    {
                        pending.Enqueue(neighbor);
                    }
                }
                if (currentVec.z < segment.Slices - 1)
                {
                    neighbor = new Vector3Int(currentVec.x, currentVec.y, currentVec.z + 1);
                    if (!visited.Contains(neighbor))
                    {
                        pending.Enqueue(neighbor);
                    }
                }
                
            }
            segment._currentWorkload.IncrementProgress();
            segment._currentWorkload.Done();
        }

        private int GetIndex(Vector3Int coordinates, int width, int height)
        {
            return coordinates.z * width * height
                   + coordinates.x * height
                   + coordinates.y;
        }
    }
}
