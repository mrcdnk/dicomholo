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

        private DiDictonary diDictionary = DiDictonary.Instance;

        private uint groupid;
        private uint elementid;
        private int vl;
        private VRType vr;
        private byte[] values;
        private string fileName = null;

        private int rawInt;
        private double rawDouble;

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
         *
         * @param is a DiInputStream - must be open and readable
         */
        public void readNext(DiFileStream inputStream)
        {
            bool exp = this.exp;
            //get ids
            groupid = inputStream.ReadShort();
            elementid = inputStream.ReadShort();

            //get vr
            if (groupid <= 2 || exp)
            {
                vr = (VRType)(((uint) inputStream.ReadByte() << 8) | (uint) inputStream.ReadByte());
            }
            else
            {
                vr = diDictionary.getVR(getTag());
            }

            // There are three special SQ related Data Elements that are not ruled by the VR encoding rules
            // conveyed by the Transfer Syntax. They shall be encoded as Implicit VR. These special Data Elements are
            // Item (FFFE,E000), Item Delimitation Item (FFFE,E00D), and Sequence Delimitation Item (FFFE,E0DD).
            // However, the Data Set within the Value Field of the Data Element Item (FFFE,E000) shall be encoded
            // according to the rules conveyed by the Transfer Syntax.
            if (groupid == 0xfffe && (elementid == 0xe000 || elementid == 0xe00d || elementid == 0xe0dd))
            {
                exp = false;
            }

            //get vl
            switch (vr)
            {
                case VRType.OB:
                case VRType.OF:
                case VRType.OW:
                case VRType.SQ:
                case VRType.UT:
                case VRType.UN:
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

            vl = Math.Max(vl, 0);

            //get data
            values = new byte[vl];

            inputStream.Read(values, 0, vl);

            try
            {
                rawInt = getValueAsInt();
            }
            catch (Exception)
            {
                //nothing to worry about, some elements aren't supposed to be used with Int
            }

            try
            {
                rawDouble = GetValueAsDouble();
            }
            catch (Exception)
            {
                //nothing to worry about, some elements aren't supposed to be used with Double
            }
        }

        /**
         * Converts the DiDataElement to a human readable string.
         *
         * @return a human readable string representation
         */
        public string toString()
        {
            string str;

            str = getTagString() + " (" + diDictionary.GetTagDescription(getTag()) + ")  ";
            str += "VR: " + getVRString() + "  VL: " + vl + "  Values: " + GetValueAsString();

            return str;
        }

        /**
         * Returns the element number (second part of the tag id).
         *
         * @return the element numbber as an integer.
         */
        public uint GetElementId()
        {
            return elementid;
        }

        /**
         * Sets the element number.
         *
         * @param element_number the element number.
         */
        public void SetElementId(uint element_number)
        {
            this.elementid = element_number;
        }

        /**
         * Returns the group number (first part of the tag id)..
         *
         * @return the group number.
         */
        public uint GetGroupId()
        {
            return groupid;
        }


        /**
         * Sets the group number.
         *
         * @param group_number the group_number.
         */
        public void SetGroupId(uint group_number)
        {
            this.groupid = group_number;
        }

        /**
         * Returns the value length.
         *
         * @return the value length
         */
        public int GetVl()
        {
            return vl;
        }

        /**
         * Sets the value length.
         *
         * @param value_length
         */
        public void SetVl(int value_length)
        {
            this.vl = value_length;
        }

        /**
         * Allows access to the byte value array.
         *
         * @return the byte value array containing the element data
         */
        public byte[] GetValues()
        {
            return values;
        }

        /**
         * Sets the byte value array.
         *
         * @param values a byte array containing the element values.
         */
        public void SetValues(byte[] values)
        {
            this.values = values;
        }

        /**
         * Returns the value as a double value. Does not perform a typecheck before.
         *
         * @return the double value
         */
        private double GetValueAsDouble()
        {
            string str = GetValueAsString();

            return double.Parse(str.Trim(), CultureInfo.InvariantCulture);
        }

        /**
         * Returns the double computed at parsing time. Faster for performance.
         */
        public double GetDouble()
        {
            return rawDouble;
        }

        /**
         * Returns the value as an int value. Does not perform a typecheck before.
         *
         * @return the int value
         */
        private int getValueAsInt()
        {
            string str = GetValueAsString();
            return Int32.Parse(str.Trim(), CultureInfo.InvariantCulture);
        }

        /*
         * Returns the int value computed at parsing time. Faster for performance.
         */
        public int GetInt()
        {
            return rawInt;
        }

        /**
         * Returns the value as a string value.
         *
         * @return the string value
         */
        public string GetValueAsString()
        {
            string str = "";

            if (vl > 255)
            {
                str = "(too long to be printed)";
            }
            else switch (vr)
            {
                case VRType.AE:
                case VRType.AS:
                case VRType.CS:
                case VRType.DA:
                case VRType.DS:
                case VRType.DT:
                case VRType.IS:
                case VRType.LO:
                case VRType.LT:
                case VRType.OF:
                case VRType.PN:
                case VRType.SH:
                case VRType.ST:
                case VRType.TM:
                case VRType.UI:
                case VRType.UN:
                case VRType.UT:
                {
                    for (int i = 0; i < vl; i++)
                    {
                        if (values[i] > 0)
                        {
                            str += (char)(values[i]);
                        }
                    }

                    break;
                }
                case VRType.FL:
                {
                    //int tmp = (values[3] << 24 | values[2] << 16 | values[1] << 8 | values[0]);
                    float f = BitConverter.ToSingle(values, 0);
                    str = f.ToString("0.0000");
                    break;
                }
                case VRType.FD:
                {
                    //Int64 tmp = (values[7] << 56 | values[6] << 48 | values[5] << 40 | values[0] << 32 |
                    //        values[3] << 24 | values[2] << 16 | values[1] << 8 | values[0]);
                    Double d = BitConverter.ToDouble(values, 0);
                    str = d.ToString();
                    break;
                }
                case VRType.SL:
                    //int tmp = (values[3] << 24 | values[2] << 16 | values[1] << 8 | values[0]);

                    str = BitConverter.ToString(values, 0); ;
                    break;
                case VRType.SQ:
                    str = "TODO";
                    break;
                case VRType.SS:
                {
                    int tmp = (values[1] << 8 | values[0]);
                    str = "" + tmp;
                    break;
                }
                case VRType.UL:
                {
                    long tmp = ((values[3] & 0xFF) << 24 | (values[2] & 0xFF) << 16
                                                         | (values[1] & 0xFF) << 8 | (values[0] & 0xFF));
                    str = "" + tmp;
                    break;
                }
                case VRType.US:
                {
                    int tmp = ((values[1] & 0xFF) << 8 | (values[0] & 0xFF));
                    str = "" + tmp;
                    break;
                }
                default:
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

                    break;
                }
            }

            return str;
        }

        /**
         * Returns the vr tag as an integer value (faster for comparing).
         *
         * @return the vr tag as integer - compare with public VRType constants
         * @see DiDictonary
         */
        public VRType GetVr()
        {
            return vr;
        }

        /**
         * Sets the vr tag.
         *
         * @param vr the vr value - use only public DiDictonary constants here
         * @see DiDictonary
         */
        public void setVR(VRType vr)
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
            return "" + (char)(((uint)vr & 0xff00) >> 8) + "" + (char)((uint)vr & 0x00ff);
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

            string uid = de.GetValueAsString();

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
