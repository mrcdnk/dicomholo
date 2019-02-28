using System;
using System.Globalization;
using UnityEngine;

namespace DICOMParser
{
    /// <summary>
    /// Implements the internal representation of a DICOM Data Element.
    /// </summary>
    public class DiDataElement
    {
        protected int Endianess;

        private uint _groupId;
        private uint _elementId;
        private int _vl;
        private VRType _vr;
        private byte[] _values;
        private int _rawInt;
        private double[] _rawDoubles = new double[1];

        private readonly DiDictonary _diDictionary = DiDictonary.Instance;
        private readonly string _fileName = "";

        public DiDataElement()
        {
            _groupId = 0;
            _elementId = 0;
            _vl = 0;
            _vr = 0;
            _values = null;
        }

        /// <summary>
        /// Default constructor; creates an empty element.
        /// </summary>
        /// <param name="fName">name of file with this element</param>
        public DiDataElement(string fName) : this()
        {
            _fileName = fName;
        }

        /// <summary>
        /// Reads the next DiDataElement from a (dicom) input stream.
        /// </summary>
        /// <param name="inputStream">is a DiInputStream - must be open and readable</param>
        public void ReadNext(DiFileStream inputStream)
        {
            bool exp;
            int vrFormat;

            uint b0 = (uint) inputStream.ReadByte();
            uint b1 = (uint) inputStream.ReadByte();

            _groupId = (b1 << 8) | b0;

            // --- meta group part start ----------------------------

            if (inputStream.BeforeMetaGroup && _groupId == 0x0002)
            {
                // we just entered the meta group
                inputStream.BeforeMetaGroup = false;
                inputStream.MetaGroup = true;
            }

            if (inputStream.MetaGroup && _groupId != 0x0002)
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
                _groupId = (b0 << 8) | b1;
            }

            // --- meta group part end ------------------------------

            _elementId = inputStream.ReadUShort(Endianess);
        
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
            if (_groupId == 0xfffe && (_elementId == 0xe000 || _elementId == 0xe00d || _elementId == 0xe0dd))
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
                _vr = _diDictionary.getVR(GetTag());

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
                    for (var i = 0; i < _values.Length / 2; i++)
                    {
                        var tmp = _values[i];
                        _values[i] = _values[_values.Length - 1 - i];
                        _values[_values.Length - 1 - i] = tmp;
                    }
                }
            }

            if (DiDictonary.ToTag(_groupId, _elementId) == 0x00020010)
            {
                // check endianess and VR format
                var tsUid = GetValueAsString();
                inputStream.VrFormat = DiDictonary.get_ts_uid_vr_format(tsUid);
                inputStream.Endianess = DiDictonary.get_ts_uid_endianess(tsUid);
                if (inputStream.VrFormat == DiFile.EndianUnknown)
                {
                    Debug.Log("DiDataElement Unknown Transfer Syntax UID \"" + tsUid +
                              "\". Endianess & VR format will be guessed.");
                }
            }

            try
            {
                _rawInt = GetValueAsInt();
            }
            catch (Exception)
            {
                //nothing to worry about, some elements aren't supposed to be used with Int
            }

            try
            {
                if (_groupId == 0x0028 && (_elementId == 0x1050 || _elementId == 0x1051))
                {
                    var numbers = GetValueAsString().Split('\\');
                    _rawDoubles = new double[numbers.Length];

                    for (var index = 0; index < numbers.Length; index++)
                    {
                        _rawDoubles[index] = double.Parse(numbers[index]);
                    }
                }
                else
                {
                    _rawDoubles[0] = GetValueAsDouble();
                }
            }
            catch (Exception)
            {
                //nothing to worry about, some elements aren't supposed to be used with Double
            }
        }

        /// <summary>
        /// Converts the DiDataElement to a human readable string.
        /// </summary>
        /// <returns>a human readable string representation</returns>
        public override string ToString()
        {
            var str = GetTagString() + " (" + _diDictionary.GetTagDescription(GetTag()) + ")  ";
            str += "VR: " + GetVrString() + "  VL: " + _vl + "  Values: " + GetValueAsString();

            return str;
        }

        /// <summary>
        /// Returns the element number (second part of the tag id).
        /// </summary>
        /// <returns> the element numbber as an integer.</returns>
        public uint GetElementId()
        {
            return _elementId;
        }

        /// <summary>
        /// Returns the group number (first part of the tag id).
        /// </summary>
        /// <returns>the group number.</returns>
        public uint GetGroupId()
        {
            return _groupId;
        }


        /// <summary>
        /// Returns the value length.
        /// </summary>
        /// <returns>the value length</returns>
        public int GetVl()
        {
            return _vl;
        }

        /// <summary>
        /// Allows access to the byte value array.
        /// </summary>
        /// <returns>the byte value array containing the element data</returns>
        public byte[] GetValues()
        {
            return _values;
        }

        /// <summary>
        /// Returns the value as a double value. Does not perform a typecheck before.
        /// </summary>
        /// <returns> the double value</returns>
        private double GetValueAsDouble()
        {
            var str = GetValueAsString();

            return double.Parse(str.Trim(), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns the double computed at parsing time. Faster for performance.
        /// </summary>
        /// <returns></returns>
        public double GetDouble()
        {
            return _rawDoubles[0];
        }

        /// <summary>
        /// Returns the value as an int value. Does not perform a typecheck before.
        /// </summary>
        /// <returns></returns>
        private int GetValueAsInt()
        {
            var str = GetValueAsString();
            return int.Parse(str.Trim(), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns the int value computed at parsing time. Faster for performance.
        /// </summary>
        /// <returns>the contained int value</returns>
        public int GetInt()
        {
            return _rawInt;
        }

        /// <summary>
        /// Returns an array of previously slash separated doubles.
        /// </summary>
        /// <returns>array of doubles contained in this element</returns>
        public double[] GetDoubles()
        {
            return _rawDoubles;
        }

        /// <summary>
        /// Returns the value as a string value.
        /// </summary>
        /// <returns>the string value</returns>
        public string GetValueAsString()
        {
            var str = "";

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

        /// <summary>
        /// Returns the vr tag (faster for comparing).
        /// </summary>
        /// <returns>the vr tag - compare with public VRType constants</returns>
        public VRType GetVr()
        {
            return _vr;
        }

        /// <summary>
        /// Returns the vr tag as an string value (human readable).
        /// </summary>
        /// <returns>the vr tag as string</returns>
        public string GetVrString()
        {
            return "" + (char) (((uint) _vr & 0xff00) >> 8) + "" + (char) ((uint) _vr & 0x00ff);
        }

        /// <summary>
        ///  Returns the complete tag id (groub number,elementnumber) as an integer (fast comparing).
        /// </summary>
        /// <returns>the tag id as an uint</returns>
        public uint GetTag()
        {
            return (_groupId << 16 | _elementId);
        }


        /// <summary>
        /// Returns the complete tag id (groub number,elementnumber) as a string (human readable).
        /// </summary>
        /// <returns>the tag id as a string</returns>
        public string GetTagString()
        {
            return "(" + My_format(_groupId) + "," + My_format(_elementId) + ")";
        }

        /// <summary>
        /// Formats group and element id
        /// </summary>
        /// <param name="num">num an integer</param>
        /// <returns>a string</returns>
        private static string My_format(uint num)
        {
            var str = num.ToString("X");

            if (num < 4096) str = "0" + str;
            if (num < 256) str = "0" + str;
            if (num < 16) str = "0" + str;

            return str;
        }
    }
}