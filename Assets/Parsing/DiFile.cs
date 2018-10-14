using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        data_elements = new Hashtable<>();
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
        file_name = file_name;
        DiFileStream file = new DiFileStream(file_name);

        if (file.skipHeader()) {
            DiDataElement diDataElement;

            while (file.available() > 0) {
                diDataElement = new DiDataElement(file_name, exp);
                diDataElement.readNext(file);

                data_elements.put(diDataElement.getTag(), diDataElement);
                // 0028 -> 40
                if (diDataElement.getGroupID() == 40) {
                    switch (diDataElement.getElementID()) {
                        // Hoehe
                        case 16:
                            h = diDataElement.getValueAsInt();
                            break;
                        // Breite
                        case 17:
                            w = diDataElement.getValueAsInt();
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

                } else if (diDataElement.getGroupID() == 32) {
                    if (diDataElement.getElementID() == 19) {
                        image_number = diDataElement.getValueAsInt();
                    }
                } else if (diDataElement.getGroupID() == 2) {
                    if (diDataElement.getElementID() == 16) {
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
        String str = "";

        str += _file_name + "\n";
        var keys = data_elements.Keys;
        List<string> l = new List<>();


        foreach (var key in keys)
        {
            DiDataElement el = data_elements[key];
            l.add(el.toString());
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
        return data_elements.Remove(DiDictonary.toTag(groupId, elementId));
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
        if (o == null || !this.GetType().Equals(obj.GetType())) return false;
        DiFile diFile = (DiFile)o;
        return width == diFile.width &&
                height == diFile.height &&
                bits_stored == diFile.bits_stored &&
                bits_allocated == diFile.bits_allocated &&
                image_number == diFile.image_number &&
                Objects.Equals(data_elements, diFile.data_elements) &&
                Objects.Equals(file_name, diFile.file_name);
    }

    
    public override int GetHashCode()
    {
        return Tuple.create(_w, _h, _bits_stored, _bits_allocated, _data_elements, _image_number, _file_name).GetHashCode();
    }
}
