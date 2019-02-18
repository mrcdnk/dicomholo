using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ExtensionsMethods;

namespace DICOMParser
{
    /**
     * Implements the internal representation of a DICOM file.
     * Stores all DataElements and makes them accessable via getDataElement(TagName).
     * Also stores the pixel data & important information for displaying the contained image in
     * separate variables with special access functions.
     *
     * It is assumed that all DICOM files of a set are little_endian and of the same vr_format.
     *
     * @author kif ported to C# by Marco Deneke
     */
    public class DiFile
    {
        public const int VrUnknown = 0;
        public const int VrExplicit = 1;
        public const int VrImplicit = 2;

        public const int EndianUnknown = 0;
        public const int EndianLittle = 1;
        public const int EndianBig = 2;

        private int _vrFormat;
        private int _endianess;

        private int _width;
        private int _height;

        private int _bitsStored;
        private int _bitsAllocated;
        private int _highBit;
        private int _intercept;
        private int _slope;
        private int _imageNumber;
        string _fileName;

        private readonly Dictionary<uint, DiDataElement> _dataElements;
        private readonly Dictionary<uint, Dictionary<uint, DiDataElement>> _sequences; // contains the sequences

        /**
         * Default Construtor - creates an empty DicomFile.
         */
        public DiFile()
        {
            _width = _height = _bitsStored = _bitsAllocated = _imageNumber = 0;
            _dataElements = new Dictionary<uint, DiDataElement>();
            _sequences = new Dictionary<uint, Dictionary<uint, DiDataElement>>();
            _fileName = null;

            _vrFormat = VrUnknown;
            _endianess = EndianUnknown;
        }

        /**
	 * Default Constructor - creates an empty DiFile.
	 */
        public DiFile(int endianess, int vrFormat)
        {
            _width = _height = _bitsStored = _bitsAllocated = _imageNumber = 0;
            _dataElements = new Dictionary<uint, DiDataElement>();
            _sequences = new Dictionary<uint, Dictionary<uint, DiDataElement>>();
            _fileName = null;

            _vrFormat = vrFormat;
            _endianess = endianess;
        }

        /**
         * Initializes the DicomFile from a file. Might throw an exception (unexpected
         * end of file, wrong data etc).
         * This method will be implemented in exercise 1.
         *
         * @param file_name a string containing the name of a valid dicom file
         * @throws Exception
         */
        public void InitFromFile(string fileName)
        {
            _fileName = fileName;
            var file = new DiFileStream(_fileName);

            if (!file.SkipHeader()) return;
            DiDataElement deOld = null, diDataElement;

            int i = 0;

            // read rest
            do
            {
                diDataElement = new DiDataElement();
                diDataElement.ReadNext(file);
                // System.out.println(de);
                // byte_sum += de._vl;

                if (diDataElement.GetTag() == 0xfffee000)
                {
                    // handle sequences ...
                    Dictionary<uint, DiDataElement> seq = new Dictionary<uint, DiDataElement>();
                    DiDataElement seqDe = new DiDataElement();
                    while (seqDe.GetTag() != 0xfffee0dd)
                    {
                        seqDe = new DiDataElement();
                        seqDe.ReadNext(file);
                        seq[seqDe.GetTag()] = seqDe;
                    }

                    _sequences[deOld.GetTag()] = seq;
                }
                else if (diDataElement.GetTag() == 0x7fe00010 && diDataElement.GetVl() == 0)
                {
                    // encapsulated pixel format
                    Dictionary<uint, DiDataElement> seq = new Dictionary<uint, DiDataElement>();
                    DiDataElement seqDe = new DiDataElement();
                    DiDataElement pixelDataDe = diDataElement;
                    int count = 0;

                    while (seqDe.GetTag() != 0xfffee0dd)
                    {
                        Debug.Log(count + " -> " + seqDe);
                        seqDe = new DiDataElement();
                        seqDe.ReadNext(file);
                        if (seqDe.GetVl() > 4)
                        {
                            pixelDataDe = seqDe;
                        }

                        seq[seqDe.GetTag()] = seqDe;
                        count++;
                    }

                    _dataElements[diDataElement.GetTag()] = pixelDataDe;
                }
                else
                {
                    // handle all but sequences
                    _dataElements[diDataElement.GetTag()] = diDataElement;
                }

                if (i == 150)
                {
                    break;
                }

                i++;
                deOld = diDataElement;
            } while (diDataElement.GetTag() != 0x07fe0010 && file.CanRead && file.Position < file.Length);


            file.Close();

            // image number
            if (_dataElements.TryGetValue(0x00200013, out diDataElement))
            {
                _imageNumber = diDataElement.GetInt();
            }

            // image height
            if (_dataElements.TryGetValue(0x00280010, out diDataElement))
            {
                _height = diDataElement.GetInt();
            }

            // image width
            if (_dataElements.TryGetValue(0x00280011, out diDataElement))
            {
                _width = diDataElement.GetInt();
            }

            // Bits/Pixel allocated
            if (_dataElements.TryGetValue(0x00280100, out diDataElement))
            {
                _bitsAllocated = diDataElement.GetInt();
            }

            // Bits/Pixel stored
            if (_dataElements.TryGetValue(0x00280101, out diDataElement))
            {
                _bitsStored = diDataElement.GetInt();
            }

            // high bit
            if (_dataElements.TryGetValue(0x00280102, out diDataElement))
            {
                _highBit = diDataElement.GetInt();
            }
        }


        /**
        * Converts a dicom file into a human readable string info. Might be long.
        * Useful for debugging.
        *
        * @return a human readable string representation
        */
        public override string ToString()
        {
            string str = "";

            str += _fileName + "\n";
            var keys = _dataElements.Keys;
            List<string> l = new List<string>();


            foreach (var key in keys)
            {
                DiDataElement el = _dataElements[key];
                l.Add(el.ToString());
            }

            l.Sort();

            foreach (var element in l)
            {
                str += element;

            }

            return str;
        }

        /**
         * Returns the number of allocated bits per pixel.
         *
         * @return the number of allocated bits.
         */
        public int GetBitsAllocated()
        {
            return _bitsAllocated;
        }

        /**
         * Returns the number of bits per pixel that are actually used for color info.
         *
         * @return the number of stored bits.
         */
        public int GetBitsStored()
        {
            return _bitsStored;
        }

        public int GetHighBit()
        {
            return _highBit;
        }

        /**
         * Allows access to the internal data element Dictionary.
         *
         * @return a reference to the data element Dictionary
         */
        public Dictionary<uint, DiDataElement> GetDataElements()
        {
            return _dataElements;
        }

        /**
         * Returns the DiDataElement with the given id. Can return null.
         *
         * @param id DiDataElement id
         * @return
         */
        public DiDataElement GetElement(uint id)
        {
            return _dataElements.GetValue(id);
        }

        public DiDataElement GetElement(uint groupId, uint elementId)
        {
            return _dataElements.GetValue(DiDictonary.ToTag(groupId, elementId));
        }

        public DiDataElement RemoveElement(uint groupId, uint elementId)
        {
            DiDataElement element = GetElement(groupId, elementId);
            _dataElements.Remove(DiDictonary.ToTag(groupId, elementId));

            return element;
        }

        /**
         * Returns the image width of the contained dicom image.
         *
         * @return the image width
         */
        public int GetImageWidth()
        {
            return _width;
        }

        /**
         * Returns the image height of the contained dicom image.
         *
         * @return the image height
         */
        public int GetImageHeight()
        {
            return _height;
        }

        /**
         * Returns the file name of the current file.
         *
         * @return the file name
         */
        public string GetFileName()
        {
            return _fileName;
        }

        /**
         * Returns the image number in the current dicom series.
         *
         * @return the image number
         */
        public int GetImageNumber()
        {
            return _imageNumber;
        }


        public override bool Equals(System.Object o)
        {
            if (this == o) return true;
            if (o == null || this.GetType() != o.GetType()) return false;
            DiFile diFile = (DiFile)o;
            return _width == diFile._width &&
                    _height == diFile._height &&
                    _bitsStored == diFile._bitsStored &&
                    _bitsAllocated == diFile._bitsAllocated &&
                    _imageNumber == diFile._imageNumber &&
                    _dataElements.Equals(diFile._dataElements) &&
                    _fileName.Equals(diFile._fileName);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _width;
                hashCode = (hashCode * 397) ^ _height;
                hashCode = (hashCode * 397) ^ _bitsStored;
                hashCode = (hashCode * 397) ^ _bitsAllocated;
                hashCode = (hashCode * 397) ^ (_dataElements != null ? _dataElements.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ _imageNumber;
                hashCode = (hashCode * 397) ^ (_fileName != null ? _fileName.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

}
