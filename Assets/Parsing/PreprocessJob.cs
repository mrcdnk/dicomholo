
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using DICOMParser;
using System.Collections.Generic;
using System.IO;

namespace dicomholo.Jobs
{
    public struct PreprocessJob : IJobParallelFor
    {
        [ReadOnly]
        public int height;
        [ReadOnly]
        public int width;
        [ReadOnly]
        public Dictionary<int, DiFile> files;

        public NativeArray<int> data;

        public void Execute(int index)
        {
            DiFile currenDiFile;
            byte[] storedBytes = new byte[4];

           
            currenDiFile = files[index];
            DiDataElement pixelData = currenDiFile.removeElement(0x7FE0, 0x0010);
            DiDataElement highBitElement = currenDiFile.getElement(0x0028, 0x0102);
            int mask = ~((~0) << highBitElement.getValueAsInt() + 1);
            int allocated = currenDiFile.getBitsAllocated() / 8;

            using (MemoryStream pixels = new MemoryStream(pixelData.GetValues()))
            {
                int currentPix;

                for (int y = 0; y < height; ++y)
                {
                    for (int x = 0; x < width; ++x)
                    {
                        //get current Int value
                        pixels.Read(storedBytes, 0, allocated);

                        int value = System.BitConverter.ToInt32(storedBytes, 0);

                        currentPix = getPixelIntensity(value & mask, currenDiFile);

                        data[index * width * height + x * height + y] = currentPix;
                    }
                }
            }
        }

        private int getPixelIntensity(int pixelIntensity, DiFile file)
        {
            DiDataElement interceptElement = file.getElement(0x0028, 0x1052);
            DiDataElement slopeElement = file.getElement(0x0028, 0x1053);

            double intercept = 0;
            double slope = 1;

            if (interceptElement != null)
            {
                intercept = interceptElement.getValueAsDouble();
            }

            if (slopeElement != null)
            {
                slope = slopeElement.getValueAsDouble();
            }

            double intensity = (pixelIntensity * slope) + intercept;

            return (int)intensity;
        }
    }
}
