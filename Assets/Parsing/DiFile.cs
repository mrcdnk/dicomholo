using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DICOMParser
{
    /**
     * Implements the internal representation of a DICOM file.
     * Stores all DataElements and makes them accessable via getDataElement(TagName).
     * Also stores the pixel data & important information for displaying the contained image in
     * separate variables with special access functions.
     */
    public class DiFile
    {
        private int width;
        private int height;
        private int bits_stored;
        private int bits_allocated;
        private bool exp = true;
        private Dictionary<uint, DiDataElement> data_elements;
        private int image_number;
        string file_name;

        /**
         * Default Construtor - creates an empty DicomFile.
         */
        public DiFile(bool exp)
        {
            width = height = bits_stored = bits_allocated = image_number = 0;
            data_elements = new Dictionary<uint, DiDataElement>();
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
        public void initFromFile(string file_name)
        {
            this.file_name = file_name;
            DiFileStream file = new DiFileStream(this.file_name);

            if (file.SkipHeader())
            {
                DiDataElement diDataElement;

                while (file.Position+1 < file.Length)
                {
                    diDataElement = new DiDataElement(file_name, exp);
                    diDataElement.readNext(file);

                    data_elements.Add(diDataElement.getTag(), diDataElement);
                    // 0028 -> 40
                    if (diDataElement.getGroupID() == 40)
                    {
                        switch (diDataElement.getElementID())
                        {
                            // Hoehe
                            case 16:
                                height = diDataElement.getValueAsInt();
                                break;
                            // Breite
                            case 17:
                                width = diDataElement.getValueAsInt();
                                break;
                            // bits allocated
                            case 256:
                                bits_allocated = diDataElement.getValueAsInt();
                                break;
                            // bits stored
                            case 257:
                                bits_stored = diDataElement.getValueAsInt();
                                break;

                        }

                    }
                    else if (diDataElement.getGroupID() == 32)
                    {
                        if (diDataElement.getElementID() == 19)
                        {
                            image_number = diDataElement.getValueAsInt();
                        }
                    }
                    else if (diDataElement.getGroupID() == 2)
                    {
                        if (diDataElement.getElementID() == 16)
                        {
                            string uid = diDataElement.getValueAsString();
                        }
                    }
                }
            }
        }

        /**
        * Converts a dicom file into a human readable string info. Might be long.
        * Useful for debugging.
        *
        * @return a human readable string representation
        */
        public string toString()
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
        public int getBitsAllocated()
        {
            return bits_allocated;
        }

        /**
         * Returns the number of bits per pixel that are actually used for color info.
         *
         * @return the number of stored bits.
         */
        public int getBitsStored()
        {
            return bits_stored;
        }

        /**
         * Allows access to the internal data element Dictionary.
         *
         * @return a reference to the data element Dictionary
         */
        public Dictionary<uint, DiDataElement> getDataElements()
        {
            return data_elements;
        }

        /**
         * Returns the DiDataElement with the given id. Can return null.
         *
         * @param id DiDataElement id
         * @return
         */
        public DiDataElement getElement(uint id)
        {
            return data_elements[id];
        }

        public DiDataElement getElement(uint groupId, uint elementId)
        {
            return data_elements[DiDictonary.toTag(groupId, elementId)];
        }

        public DiDataElement removeElement(uint groupId, uint elementId)
        {
            DiDataElement element = getElement(groupId, elementId);
            data_elements.Remove(DiDictonary.toTag(groupId, elementId));

            return element;
        }

        /**
         * Returns the image width of the contained dicom image.
         *
         * @return the image width
         */
        public int getImageWidth()
        {
            return width;
        }

        /**
         * Returns the image height of the contained dicom image.
         *
         * @return the image height
         */
        public int getImageHeight()
        {
            return height;
        }

        /**
         * Returns the file name of the current file.
         *
         * @return the file name
         */
        public string getFileName()
        {
            return file_name;
        }

        /**
         * Returns the image number in the current dicom series.
         *
         * @return the image number
         */
        public int getImageNumber()
        {
            return image_number;
        }


        public override bool Equals(System.Object o)
        {
            if (this == o) return true;
            if (o == null || !this.GetType().Equals(o.GetType())) return false;
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
            return image_number;
        }
    }
}
