using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ExtensionsMethods;

namespace DICOMParser
{
    /// <summary>
    /// Implements the internal representation of a DICOM file.
    /// Stores all DataElements and makes them accessable via getDataElement(TagName).
    /// Also stores the pixel data & important information for displaying the contained image in
    /// separate variables with special access functions.
    ///
    ///  It is assumed that all DICOM files of a set are little_endian and of the same vr_format.
    ///
    /// @author kif ported to C# by Marco Deneke
    /// </summary>
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
        private string _fileName;

        private readonly Dictionary<uint, DiDataElement> _dataElements;
        private readonly Dictionary<uint, Dictionary<uint, DiDataElement>> _sequences; // contains the sequences

        /// <summary>
        /// Default constructor - creates an empty dicom File.
        /// </summary>
        public DiFile()
        {
            _width = _height = _bitsStored = _bitsAllocated = _imageNumber = 0;
            _dataElements = new Dictionary<uint, DiDataElement>();
            _sequences = new Dictionary<uint, Dictionary<uint, DiDataElement>>();
            _fileName = null;

            _vrFormat = VrUnknown;
            _endianess = EndianUnknown;
        }

        /// <summary>
        /// Default Constructor - creates an empty DiFile.
        /// </summary>
        /// <param name="endianess">endianess of this file</param>
        /// <param name="vrFormat">vrFormat of this file</param>
        public DiFile(int endianess, int vrFormat)
        {
            _width = _height = _bitsStored = _bitsAllocated = _imageNumber = 0;
            _dataElements = new Dictionary<uint, DiDataElement>();
            _sequences = new Dictionary<uint, Dictionary<uint, DiDataElement>>();
            _fileName = null;

            _vrFormat = vrFormat;
            _endianess = endianess;
        }

        /// <summary>
        /// Initializes the DiFile from a file. Might throw an exception (unexpected
        /// end of file, wrong data etc).
        /// This method will be implemented in exercise 1.
        /// </summary>
        /// <param name="fileName">file_name a string containing the name of a valid dicom file</param>
        public void InitFromFile(string fileName)
        {
            _fileName = fileName;
            var file = new DiFileStream(_fileName);

            if (!file.SkipHeader()) return;
            DiDataElement deOld = null, diDataElement;

            var i = 0;

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
                    var seq = new Dictionary<uint, DiDataElement>();
                    var seqDe = new DiDataElement();
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
                    var seq = new Dictionary<uint, DiDataElement>();
                    var seqDe = new DiDataElement();
                    var pixelDataDe = diDataElement;
                    var count = 0;

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


        /// <summary>
        ///  Converts a dicom file into a human readable string info. Might be long. Useful for debugging.
        /// </summary>
        /// <returns>a human readable string representation</returns>
        public override string ToString()
        {
            var str = "";

            str += _fileName + "\n";
            var keys = _dataElements.Keys;
            var l = new List<string>();


            foreach (var key in keys)
            {
                var el = _dataElements[key];
                l.Add(el.ToString());
            }

            l.Sort();

            foreach (var element in l)
            {
                str += element;

            }

            return str;
        }

        /// <summary>
        /// Returns the number of allocated bits per pixel.
        /// </summary>
        /// <returns> the number of allocated bits.</returns>
        public int GetBitsAllocated()
        {
            return _bitsAllocated;
        }

        /// <summary>
        ///  Returns the number of bits per pixel that are actually used for color info.
        /// </summary>
        /// <returns>the number of stored bits.</returns>
        public int GetBitsStored()
        {
            return _bitsStored;
        }

        /// <summary>
        /// Returns the index of the highest pixel intensity bit
        /// </summary>
        /// <returns></returns>
        public int GetHighBit()
        {
            return _highBit;
        }

        /// <summary>
        /// Allows access to the internal data element Dictionary.
        /// </summary>
        /// <returns> a reference to the data element Dictionary</returns>
        public Dictionary<uint, DiDataElement> GetDataElements()
        {
            return _dataElements;
        }

        /// <summary>
        /// Returns the DiDataElement with the given id. Can return null.
        /// </summary>
        /// <param name="id">DiDataElement id</param>
        /// <returns></returns>
        public DiDataElement GetElement(uint id)
        {
            return _dataElements.GetValue(id);
        }

        /// <summary>
        /// Returns the DiDataElement with the given ids. Can return null.
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="elementId"></param>
        /// <returns></returns>
        public DiDataElement GetElement(uint groupId, uint elementId)
        {
            return _dataElements.GetValue(DiDictonary.ToTag(groupId, elementId));
        }

        /// <summary>
        /// Returns the DiDataElement with the given ids and removes it from this diFile. Can return null.
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="elementId"></param>
        /// <returns></returns>
        public DiDataElement RemoveElement(uint groupId, uint elementId)
        {
            DiDataElement element = GetElement(groupId, elementId);
            _dataElements.Remove(DiDictonary.ToTag(groupId, elementId));

            return element;
        }

        /// <summary>
        /// Returns the image width of the contained dicom image.
        /// </summary>
        /// <returns>the image width</returns>
        public int GetImageWidth()
        {
            return _width;
        }

        /// <summary>
        ///  Returns the image height of the contained dicom image.
        /// </summary>
        /// <returns> the image height</returns>
        public int GetImageHeight()
        {
            return _height;
        }

        /// <summary>
        ///  Returns the file name of the current file.
        /// </summary>
        /// <returns> the file name</returns>
        public string GetFileName()
        {
            return _fileName;
        }

        /// <summary>
        /// Returns the image number in the current dicom series.
        /// </summary>
        /// <returns>the image number</returns>
        public int GetImageNumber()
        {
            return _imageNumber;
        }

        public override bool Equals(System.Object o)
        {
            if (this == o) return true;
            if (o == null || this.GetType() != o.GetType()) return false;
            var diFile = (DiFile)o;
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
