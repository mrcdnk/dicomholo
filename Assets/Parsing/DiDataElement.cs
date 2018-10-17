using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Globalization;

namespace DICOMParser
{

    /**
    * Implements the internal representation of a DICOM Data Element.
    *
*/
    public class DiDataElement
    {

        private DiDictonary diDictonary = DiDictonary.Instance;

        private uint groupid;
        private uint elementid;
        private int vl;
        private uint vr;
        private byte[] values;
        private string fileName = null;

        bool exp = true;

        public DiDataElement()
        {
            groupid = 0;
            elementid = 0;
            vl = 0;
            vr = 0;
            values = null;
        }

        public DiDataElement(string fName, bool exp) : this(fName)
        {
            this.exp = exp;
        }

        /**
         * Default constructor; creates an empty element.
         */
        public DiDataElement(string fName) : this()
        {
            fileName = fName;
        }

        /**
         * Reads the next DiDataElement from a (dicom) input stream.
         * Might throwh an IOException, for example unexpected end of file.
         * This method will be implemented in exercise 1.
         *
         * @param is a DiInputStream - must be open and readable
         */
        public void readNext(DiFileStream inputStream)
        {
            //get ids
            groupid = inputStream.ReadShort();
            elementid = inputStream.ReadShort();

            //get vr
            if (groupid <= 2 || exp)
            {
                vr = ((uint)inputStream.ReadByte() << 8) | (uint)inputStream.ReadByte();
            }
            else
            {
                vr = diDictonary.getVR(getTag());
            }

            //get vl
            switch (vr)
            {
                case DiDictonary.OB:
                case DiDictonary.OF:
                case DiDictonary.OW:
                case DiDictonary.SQ:
                case DiDictonary.UT:
                case DiDictonary.UN:
                    //special
                    if (groupid <= 2 || exp)
                    {
                        inputStream.ReadShort();
                    }

                    vl = inputStream.ReadInt();
                    break;
                default:
                    //normal
                    if (groupid <= 2 || exp)
                    {
                        vl = Convert.ToInt32(inputStream.ReadShort());
                    }
                    else
                    {
                        vl = inputStream.ReadInt();
                    }
                    break;
            }

            //get data
            values = new byte[vl];

            inputStream.Read(values, 0, values.Length);
        }

        /**
         * Converts the DiDataElement to a human readable string.
         *
         * @return a human readable string representation
         */
        public string toString()
        {
            string str;

            str = getTagString() + " (" + diDictonary.getTagDescr(getTag()) + ")  ";
            str += "VR: " + getVRString() + "  VL: " + vl + "  Values: " + getValueAsString();

            return str;
        }

        /**
         * Returns the element number (second part of the tag id).
         *
         * @return the element numbber as an integer.
         */
        public uint getElementID()
        {
            return elementid;
        }

        /**
         * Sets the element number.
         *
         * @param element_number the element number.
         */
        public void setElementID(uint element_number)
        {
            this.elementid = element_number;
        }

        /**
         * Returns the group number (first part of the tag id)..
         *
         * @return the group number.
         */
        public uint getGroupID()
        {
            return groupid;
        }


        /**
         * Sets the group number.
         *
         * @param group_number the group_number.
         */
        public void setGroupID(uint group_number)
        {
            this.groupid = group_number;
        }

        /**
         * Returns the value length.
         *
         * @return the value length
         */
        public int getVL()
        {
            return vl;
        }

        /**
         * Sets the value length.
         *
         * @param value_length
         */
        public void setVL(int value_length)
        {
            this.vl = value_length;
        }

        /**
         * Allows access to the byte value array.
         *
         * @return the byte value array containing the element data
         */
        public byte[] getValues()
        {
            return values;
        }

        /**
         * Sets the byte value array.
         *
         * @param values a byte array containing the element values.
         */
        public void setValues(byte[] values)
        {
            this.values = values;
        }

        /**
         * Returns the value as a double value. Does not perform a typecheck before.
         *
         * @return the double value
         */
        public double getValueAsDouble()
        {
            string str = getValueAsString();

            return double.Parse(str.Trim(), CultureInfo.InvariantCulture);
        }

        /**
         * Returns the value as an int value. Does not perform a typecheck before.
         *
         * @return the int value
         */
        public int getValueAsInt()
        {
            string str = getValueAsString();
            return Int32.Parse(str.Trim(), CultureInfo.InvariantCulture);
        }

        /**
         * Returns the value as a string value.
         *
         * @return the string value
         */
        public string getValueAsString()
        {
            string str = "";

            if (vl > 255)
            {
                str = "(too long to be printed)";
            }
            else if (vr == DiDictonary.AE || vr == DiDictonary.AS || vr == DiDictonary.CS || vr == DiDictonary.DA || vr == DiDictonary.DS ||
                  vr == DiDictonary.DT || vr == DiDictonary.IS || vr == DiDictonary.LO || vr == DiDictonary.LT || vr == DiDictonary.OF ||
                  vr == DiDictonary.PN || vr == DiDictonary.SH || vr == DiDictonary.ST || vr == DiDictonary.TM || vr == DiDictonary.UI ||
                  vr == DiDictonary.UN || vr == DiDictonary.UT)
            {
                for (int i = 0; i < vl; i++)
                {
                    if (values[i] > 0)
                    {
                        str += ((char)(values[i]));
                    }
                }
            }
            else if (vr == DiDictonary.FL)
            {
                //int tmp = (values[3] << 24 | values[2] << 16 | values[1] << 8 | values[0]);
                float f = BitConverter.ToSingle(values, 0);
                str = f.ToString("0.0000");
            }
            else if (vr == DiDictonary.FD)
            {
                //Int64 tmp = (values[7] << 56 | values[6] << 48 | values[5] << 40 | values[0] << 32 |
                //        values[3] << 24 | values[2] << 16 | values[1] << 8 | values[0]);
                Double d = BitConverter.ToDouble(values, 0);
                str = d.ToString();
            }
            else if (vr == DiDictonary.SL)
            {
                //int tmp = (values[3] << 24 | values[2] << 16 | values[1] << 8 | values[0]);

                str = BitConverter.ToString(values, 0); ;
            }
            else if (vr == DiDictonary.SQ)
            {
                str = "TODO";
            }
            else if (vr == DiDictonary.SS)
            {
                int tmp = (values[1] << 8 | values[0]);
                str = "" + tmp;
            }
            else if (vr == DiDictonary.UL)
            {
                long tmp = ((values[3] & 0xFF) << 24 | (values[2] & 0xFF) << 16
                        | (values[1] & 0xFF) << 8 | (values[0] & 0xFF));
                str = "" + tmp;
            }
            else if (vr == DiDictonary.US)
            {
                int tmp = ((values[1] & 0xFF) << 8 | (values[0] & 0xFF));
                str = "" + tmp;
            }
            else
            {
                // supports: OB
                for (int i = 0; i < vl; i++)
                {
                    if (i < vl - 1)
                    {
                        str += ((int)(values[i]) + "|");
                    }
                    else
                    {
                        str += ((int)(values[i]));
                    }
                }
            }

            return str;
        }

        /**
         * Returns the vr tag as an integer value (faster for comparing).
         *
         * @return the vr tag as integer - compare with public DiDictonary constants
         * @see DiDictonary
         */
        public uint getVR()
        {
            return vr;
        }

        /**
         * Sets the vr tag.
         *
         * @param vr the vr value - use only public DiDictonary constants here
         * @see DiDictonary
         */
        public void setVR(uint vr)
        {
            this.vr = vr;
        }

        /**
         * Returns the vr tag as an string value (human readable).
         *
         * @return the vr tag as string
         */
        public string getVRString()
        {
            return "" + (char)((vr & 0xff00) >> 8) + "" + (char)(vr & 0x00ff);
        }

        /**
         * Returns the complete tag id (groub number,elementnumber) as an integer
         * (fast comparing).
         *
         * @return the tag id as an int
         */
        public uint getTag()
        {
            return (groupid << 16 | elementid);
        }


        /**
         * Returns the complete tag id (groub number,elementnumber) as a string
         * (human readable).
         *
         * @return the tag id as a string
         */
        public string getTagString()
        {
            return "(" + my_format(groupid) + "," + my_format(elementid) + ")";
        }

        /**
         * Returns the explicit state (from tag 0002,0010)
         *
         * @return the explicit state
         */
        public bool quickscanExp()
        {

            DiFileStream ds = new DiFileStream(fileName);
            ds.SkipHeader();

            DiDataElement de = new DiDataElement();

            do
            {
                de.readNext(ds);
            } while (de.getTag() < 0x00020010);

            ds.Close();

            string uid = de.getValueAsString();

            return uid.StartsWith("1.2.840.10008.1.2.1") || uid.StartsWith("1.2.840.10008.1.2.2");
        }

        /**
        * Formats group and element id
        *
        * @param num an integer
        * @return a string
        */
        private string my_format(uint num)
        {
            string str = num.ToString("X");

            if (num < 4096) str = "0" + str;
            if (num < 256) str = "0" + str;
            if (num < 16) str = "0" + str;

            return str;
        }


    }
}
