using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Globalization;
using Debug = UnityEngine.Debug;

namespace DICOMParser
{
    /**
    * Implements the internal representation of a DICOM Data Element.
    *
*/
    public class DiDataElement
    {
        private DiDictonary diDictionary = DiDictonary.Instance;

        private uint _groupid;
        private uint _elementid;
        private int _vl;
        private VRType _vr;
        private byte[] _values;
        private readonly string _fileName = null;
        protected int Endianess;

        private int _rawInt;
        private double _rawDouble;

        public DiDataElement()
        {
            _groupid = 0;
            _elementid = 0;
            _vl = 0;
            _vr = 0;
            _values = null;
        }

        /**
         * Default constructor; creates an empty element.
         */
        public DiDataElement(string fName) : this()
        {
            _fileName = fName;
        }

        /**
         * Reads the next DiDataElement from a (dicom) input stream.
         * Might throwh an IOException, for example unexpected end of file.
         *
         * @param is a DiInputStream - must be open and readable
         */
        public void ReadNext(DiFileStream inputStream)
        {
            bool exp;
            int vrFormat;

            uint b0 = (uint) inputStream.ReadByte();
            uint b1 = (uint) inputStream.ReadByte();

            _groupid = (b1 << 8) | b0;

            // --- meta group part start ----------------------------

            if (inputStream.BeforeMetaGroup && _groupid == 0x0002)
            {
                // we just entered the meta group
                inputStream.BeforeMetaGroup = false;
                inputStream.MetaGroup = true;
            }

            if (inputStream.MetaGroup && _groupid != 0x0002)
            {
                // we just left the meta group
                inputStream.MetaGroup = false;
            }

            if (inputStream.BeforeMetaGroup || inputStream.MetaGroup)
            {
                // are we still before or inside meta group?
                vrFormat = DiFile.VrExplicit;
                Endianess = DiFile.EndianLittle;
            }
            else
            {
                vrFormat = inputStream.VrFormat;
                Endianess = inputStream.Endianess;
            }

            if (Endianess == DiFile.EndianBig)
            {
                _groupid = (b0 << 8) | b1;
            }

            // --- meta group part end ------------------------------

            _elementid = inputStream.ReadUShort(Endianess);
            //Debug.Log("group: " +_groupid + "\n");
            //Debug.Log("element: " + _elementid + "\n");
            b0 = (uint) inputStream.ReadByte();
            b1 = (uint) inputStream.ReadByte();

            _vr = (VRType)((b0 << 8) | b1);

            // check if we are explicit or implicit:
            // b0 and b1 could a) be an explicit VR or b) be the first part of the VL
            exp = (vrFormat == DiFile.VrExplicit) || (vrFormat == DiFile.VrUnknown &&
                                                      (_vr == VRType.AE || _vr == VRType.AS || _vr == VRType.AT ||
                                                       _vr == VRType.CS || _vr == VRType.DA || _vr == VRType.DS ||
                                                       _vr == VRType.DT || _vr == VRType.FD || _vr == VRType.FL ||
                                                       _vr == VRType.IS || _vr == VRType.LO || _vr == VRType.LT ||
                                                       _vr == VRType.PN || _vr == VRType.SH || _vr == VRType.SL ||
                                                       _vr == VRType.SS || _vr == VRType.ST || _vr == VRType.TM ||
                                                       _vr == VRType.UI || _vr == VRType.UL || _vr == VRType.US ||
                                                       _vr == VRType.UT || _vr == VRType.OB || _vr == VRType.OW ||
                                                       _vr == VRType.SQ || _vr == VRType.UN || _vr == VRType.QQ));

            // There are three special SQ related Data Elements that are not ruled by the VR encoding rules
            // conveyed by the Transfer Syntax. They shall be encoded as Implicit VR. These special Data Elements are
            // Item (FFFE,E000), Item Delimitation Item (FFFE,E00D), and Sequence Delimitation Item (FFFE,E0DD).
            // However, the Data Set within the Value Field of the Data Element Item (FFFE,E000) shall be encoded
            // according to the rules conveyed by the Transfer Syntax.
            if (_groupid == 0xfffe && (_elementid == 0xe000 || _elementid == 0xe00d || _elementid == 0xe0dd))
            {
                exp = false;
            }

            if (exp)
            {
                // explicit VR -> get the VR first
                // VL can have 2 or 4 byte ...
                if (_vr == VRType.OB || _vr == VRType.OW || _vr == VRType.SQ || _vr == VRType.UT || _vr == VRType.UN)
                {
                    inputStream.ReadByte(); // skip 2 bytes ...
                    inputStream.ReadByte();
                    _vl = inputStream.ReadInt(Endianess);
                }
                else
                {
                    _vl = inputStream.ReadShort(Endianess);
                }
            }
            else
            {
                // implicit VR -> lookup VR in the DicomDictionary
                _vr = diDictionary.getVR(GetTag());

                uint b2 = (uint)inputStream.ReadByte(), b3 = (uint)inputStream.ReadByte();

                if (Endianess == DiFile.EndianLittle)
                {
                    _vl = (int)((b3 << 24) + (b2 << 16) + (b1 << 8) + b0);
                }
                else
                {
                    _vl = (int)((b0 << 24) + (b1 << 16) + (b2 << 8) + b3);
                }
            }

            if (_vl == -1) _vl = 0; // _vl can be -1 if VR == SQ

            _values = new byte[_vl];
            inputStream.Read(_values, 0, _values.Length);

            if (Endianess == DiFile.EndianBig)
            {
                // VR's affected by endianess:
                //   2-byte US, SS, OW and each component of AT
                //   4-byte UL, SL, and FL
                //   8 byte FD
                if (_vr == VRType.US || _vr == VRType.SS || _vr == VRType.OW || _vr == VRType.UL || _vr == VRType.SL ||
                    _vr == VRType.FL || _vr == VRType.FD)
                {
                    for (int i = 0; i < _values.Length / 2; i++)
                    {
                        byte tmp = _values[i];
                        _values[i] = _values[_values.Length - 1 - i];
                        _values[_values.Length - 1 - i] = tmp;
                    }
                }
            }

            if (DiDictonary.ToTag(_groupid, _elementid) == 0x00020010)
            {
                // check endianess and VR format
                String ts_uid = GetValueAsString();
                inputStream.VrFormat = DiDictonary.get_ts_uid_vr_format(ts_uid);
                inputStream.Endianess = DiDictonary.get_ts_uid_endianess(ts_uid);
                if (inputStream.VrFormat == DiFile.EndianUnknown)
                {
                    Debug.Log("DiDataElement Unknown Transfer Syntax UID \"" + ts_uid +
                              "\". Endianess & VR format will be guessed.");
                }
            }

            try
            {
                if (_groupid == 0x0028 && (_elementid == 0x1050 || _elementid == 0x1051))
                {
                    _rawInt = Int32.Parse(GetValueAsString().Split('\\')[0]);
                }
                else
                {
                    _rawInt = GetValueAsInt();
                }
            }
            catch (Exception)
            {
                //nothing to worry about, some elements aren't supposed to be used with Int
            }

            try
            {
                _rawDouble = GetValueAsDouble();
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
        public override string ToString()
        {
            string str;

            str = GetTagString() + " (" + diDictionary.GetTagDescription(GetTag()) + ")  ";
            str += "VR: " + GetVrString() + "  VL: " + _vl + "  Values: " + GetValueAsString();

            return str;
        }

        /**
         * Returns the element number (second part of the tag id).
         *
         * @return the element numbber as an integer.
         */
        public uint GetElementId()
        {
            return _elementid;
        }

        /**
         * Sets the element number.
         *
         * @param element_number the element number.
         */
        public void SetElementId(uint elementNumber)
        {
            this._elementid = elementNumber;
        }

        /**
         * Returns the group number (first part of the tag id)..
         *
         * @return the group number.
         */
        public uint GetGroupId()
        {
            return _groupid;
        }


        /**
         * Sets the group number.
         *
         * @param group_number the group_number.
         */
        public void SetGroupId(uint groupNumber)
        {
            this._groupid = groupNumber;
        }

        /**
         * Returns the value length.
         *
         * @return the value length
         */
        public int GetVl()
        {
            return _vl;
        }

        /**
         * Sets the value length.
         *
         * @param value_length
         */
        public void SetVl(int valueLength)
        {
            this._vl = valueLength;
        }

        /**
         * Allows access to the byte value array.
         *
         * @return the byte value array containing the element data
         */
        public byte[] GetValues()
        {
            return _values;
        }

        /**
         * Sets the byte value array.
         *
         * @param values a byte array containing the element values.
         */
        public void SetValues(byte[] values)
        {
            this._values = values;
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
            return _rawDouble;
        }

        /**
         * Returns the value as an int value. Does not perform a typecheck before.
         *
         * @return the int value
         */
        private int GetValueAsInt()
        {
            string str = GetValueAsString();
            return Int32.Parse(str.Trim(), CultureInfo.InvariantCulture);
        }

        /*
         * Returns the int value computed at parsing time. Faster for performance.
         */
        public int GetInt()
        {
            return _rawInt;
        }

        /**
         * Returns the value as a string value.
         *
         * @return the string value
         */
        public string GetValueAsString()
        {
            string str = "";

            if (_vl > 255)
            {
                str = "(too long to be printed)";
            }
            else
                switch (_vr)
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
                        for (int i = 0; i < _vl; i++)
                        {
                            if (_values[i] > 0)
                            {
                                str += (char) (_values[i]);
                            }
                        }

                        break;
                    }
                    case VRType.FL:
                    {
                        //int tmp = (values[3] << 24 | values[2] << 16 | values[1] << 8 | values[0]);
                        float f = BitConverter.ToSingle(_values, 0);
                        str = f.ToString("0.0000");
                        break;
                    }
                    case VRType.FD:
                    {
                        //Int64 tmp = (values[7] << 56 | values[6] << 48 | values[5] << 40 | values[0] << 32 |
                        //        values[3] << 24 | values[2] << 16 | values[1] << 8 | values[0]);
                        Double d = BitConverter.ToDouble(_values, 0);
                        str = d.ToString(CultureInfo.InvariantCulture);
                        break;
                    }
                    case VRType.SL:
                        //int tmp = (values[3] << 24 | values[2] << 16 | values[1] << 8 | values[0]);

                        str = BitConverter.ToString(_values, 0);
                        ;
                        break;
                    case VRType.SQ:
                        str = "TODO";
                        break;
                    case VRType.SS:
                    {
                        int tmp = (_values[1] << 8 | _values[0]);
                        str = "" + tmp;
                        break;
                    }
                    case VRType.UL:
                    {
                        long tmp = ((_values[3] & 0xFF) << 24 | (_values[2] & 0xFF) << 16
                                                              | (_values[1] & 0xFF) << 8 | (_values[0] & 0xFF));
                        str = "" + tmp;
                        break;
                    }
                    case VRType.US:
                    {
                        int tmp = ((_values[1] & 0xFF) << 8 | (_values[0] & 0xFF));
                        str = "" + tmp;
                        break;
                    }
                    default:
                    {
                        // supports: OB
                        for (int i = 0; i < _vl; i++)
                        {
                            if (i < _vl - 1)
                            {
                                str += ((int) (_values[i]) + "|");
                            }
                            else
                            {
                                str += ((int) (_values[i]));
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
            return _vr;
        }

        /**
         * Sets the vr tag.
         *
         * @param vr the vr value - use only public DiDictonary constants here
         * @see DiDictonary
         */
        public void SetVr(VRType vr)
        {
            this._vr = vr;
        }

        /**
         * Returns the vr tag as an string value (human readable).
         *
         * @return the vr tag as string
         */
        public string GetVrString()
        {
            return "" + (char) (((uint) _vr & 0xff00) >> 8) + "" + (char) ((uint) _vr & 0x00ff);
        }

        /**
         * Returns the complete tag id (groub number,elementnumber) as an integer
         * (fast comparing).
         *
         * @return the tag id as an int
         */
        public uint GetTag()
        {
            return (_groupid << 16 | _elementid);
        }


        /**
         * Returns the complete tag id (groub number,elementnumber) as a string
         * (human readable).
         *
         * @return the tag id as a string
         */
        public string GetTagString()
        {
            return "(" + my_format(_groupid) + "," + my_format(_elementid) + ")";
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