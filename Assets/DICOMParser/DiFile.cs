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
     */
    public class DiFile
    {
        private int width;
        private int height;
        private int bits_stored;
        private int bits_allocated;
        private int high_bit;
        private bool exp = true;
        private int image_number;
        string file_name;

        private Dictionary<uint, DiDataElement> data_elements;
        private Dictionary<uint, Dictionary<uint, DiDataElement>> sequences; // contains the sequences

        /**
         * Default Construtor - creates an empty DicomFile.
         */
        public DiFile(bool exp)
        {
            width = height = bits_stored = bits_allocated = image_number = 0;
            data_elements = new Dictionary<uint, DiDataElement>();
            sequences = new Dictionary<uint, Dictionary<uint, DiDataElement>>();
            file_name = null;
            this.exp = exp;
        }

        /**
         * Initializes the DicomFile from a file. Might throw an exception (unexpected
         * end of file, wrong data etc).
         * This method will be implemented in exercise 1.
         *
         * @param file_name a string containing the name of a valid dicom file
         * @throws Exception
         */
        public void InitFromFile(string file_name)
        {
            this.file_name = file_name;
            DiFileStream file = new DiFileStream(this.file_name);

        
            if (file.SkipHeader())
            {
                DiDataElement diDataElement = null, de_old = null;

                while (file.Position+1 < file.Length)
                {
                    diDataElement = new DiDataElement(file_name, exp);
                    diDataElement.readNext(file);
                 
                    if (diDataElement.getTag() == 0xfffee00) // handle sequences ...
                    {
                        Dictionary<uint, DiDataElement> seq = new Dictionary<uint, DiDataElement>();
                        DiDataElement seq_de = new DiDataElement();
                        while (seq_de.getTag() != 0xfffee0dd)
                        {
                            seq_de = new DiDataElement();
                            seq_de.readNext(file);
                            seq[seq_de.getTag()] = seq_de;
                        }
                        sequences[de_old.getTag()] = seq;
                    }
                    else if (diDataElement.getTag() == 0x7fe00010 && diDataElement.GetVl() == 0)
                    {
                        Debug.Log(diDataElement);
                        // encapsulated pixel format
                        Dictionary<uint, DiDataElement> seq = new Dictionary<uint, DiDataElement>();
                        DiDataElement seq_de = new DiDataElement();
                        DiDataElement pixel_data_de = diDataElement;
                        int count = 0;

                        while ((seq_de.getTag() != 0xfffee0dd))
                        {
                            seq_de = new DiDataElement();
                            seq_de.readNext(file);
                            if (seq_de.GetVl() > 4)
                            {
                                pixel_data_de = seq_de;
                            }
                            seq[seq_de.getTag()] = seq_de;
                            count++;
                        }
                        data_elements[diDataElement.getTag()] = pixel_data_de;
                    }
                    else
                    { // handle all but sequences
                        data_elements.Add(diDataElement.getTag(), diDataElement);
                    }

                    // 0028 -> 40
                    if (diDataElement.GetGroupId() == 40)
                    {
                        switch (diDataElement.GetElementId())
                        {
                            // Hoehe
                            case 16:
                                height = diDataElement.GetInt();
                                break;
                            // Breite
                            case 17:
                                width = diDataElement.GetInt();
                                break;
                            // bits allocated
                            case 256:
                                bits_allocated = diDataElement.GetInt();
                                break;
                            // bits stored
                            case 257:
                                bits_stored = diDataElement.GetInt();
                                break;
                            case 258:
                                high_bit = diDataElement.GetInt();
                                break;
                        }

                    }
                    else if (diDataElement.GetGroupId() == 32)
                    {
                        if (diDataElement.GetElementId() == 19)
                        {
                            image_number = diDataElement.GetInt();
                        }
                    }

                    de_old = diDataElement;

                    if (diDataElement.getTag() == 0x7fe0010)
                    {
                        break;
                    }
                }
            }

            file.Close();
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

            str += file_name + "\n";
            var keys = data_elements.Keys;
            List<string> l = new List<string>();


            foreach (var key in keys)
            {
                DiDataElement el = data_elements[key];
                l.Add(el.toString());
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
            return bits_allocated;
        }

        /**
         * Returns the number of bits per pixel that are actually used for color info.
         *
         * @return the number of stored bits.
         */
        public int GetBitsStored()
        {
            return bits_stored;
        }

        public int GetHighBit()
        {
            return high_bit;
        }

        /**
         * Allows access to the internal data element Dictionary.
         *
         * @return a reference to the data element Dictionary
         */
        public Dictionary<uint, DiDataElement> GetDataElements()
        {
            return data_elements;
        }

        /**
         * Returns the DiDataElement with the given id. Can return null.
         *
         * @param id DiDataElement id
         * @return
         */
        public DiDataElement GetElement(uint id)
        {
            return data_elements.GetValue(id);
        }

        public DiDataElement GetElement(uint groupId, uint elementId)
        {
            return data_elements.GetValue(DiDictonary.ToTag(groupId, elementId));
        }

        public DiDataElement RemoveElement(uint groupId, uint elementId)
        {
            DiDataElement element = GetElement(groupId, elementId);
            data_elements.Remove(DiDictonary.ToTag(groupId, elementId));

            return element;
        }

        /**
         * Returns the image width of the contained dicom image.
         *
         * @return the image width
         */
        public int GetImageWidth()
        {
            return width;
        }

        /**
         * Returns the image height of the contained dicom image.
         *
         * @return the image height
         */
        public int GetImageHeight()
        {
            return height;
        }

        /**
         * Returns the file name of the current file.
         *
         * @return the file name
         */
        public string GetFileName()
        {
            return file_name;
        }

        /**
         * Returns the image number in the current dicom series.
         *
         * @return the image number
         */
        public int GetImageNumber()
        {
            return image_number;
        }


        public override bool Equals(System.Object o)
        {
            if (this == o) return true;
            if (o == null || this.GetType() != o.GetType()) return false;
            DiFile diFile = (DiFile)o;
            return width == diFile.width &&
                    height == diFile.height &&
                    bits_stored == diFile.bits_stored &&
                    bits_allocated == diFile.bits_allocated &&
                    image_number == diFile.image_number &&
                    data_elements.Equals(diFile.data_elements) &&
                    file_name.Equals(diFile.file_name);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = width;
                hashCode = (hashCode * 397) ^ height;
                hashCode = (hashCode * 397) ^ bits_stored;
                hashCode = (hashCode * 397) ^ bits_allocated;
                hashCode = (hashCode * 397) ^ exp.GetHashCode();
                hashCode = (hashCode * 397) ^ (data_elements != null ? data_elements.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ image_number;
                hashCode = (hashCode * 397) ^ (file_name != null ? file_name.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

}
