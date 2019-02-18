
using System.IO;
using System;
using System.Text;
using GLTF.Math;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace DICOMParser
{
    public class DiFileStream : FileStream
    {
        public bool MetaGroup, BeforeMetaGroup;

        public int VrFormat { get; set; }
        public int Endianess { get; set; }

        public class QuickInfo
        {
            public string SeriesUid;
            //	    public String _media_stored_sop_class_uid;
            public int ImageNumber;
            public bool Scout;

            public override string ToString()
            {
                return SeriesUid + ", " + ImageNumber + ", " + Scout;
            }
        }

        public DiFileStream(string fname) : base(fname, FileMode.Open, FileAccess.Read, FileShare.Read)
        {
            Endianess = DiFile.EndianUnknown;
            VrFormat = DiFile.VrUnknown;
            BeforeMetaGroup = true;
            MetaGroup = false;
        }

        public DiDataElement Scan_for(uint searchTag)
        {
            DiDataElement de = new DiDataElement();
            bool exceptionOccured = false;
            bool inSequence = false;
            uint searchGroupId = (searchTag >> 16);

            do
            {
                try
                {
                    de.ReadNext(this);
                    if (de.GetGroupId() < searchGroupId && de.GetElementId() == 0)
                    {
                        Seek(de.GetInt(), SeekOrigin.Current);
                    }
                }
                catch (Exception ex) {
                    Debug.Log("DiFileInputStream::get_quick_info failed:" + ex);
                    exceptionOccured = true;
                }

                if (searchTag == de.GetTag())
                {
                    return de;
                }
            } while (!exceptionOccured && (inSequence || de.GetTag() < 0x00200013)) ;

            return null;
        }

        /**
        * Fills a QuickInfo objects and skips all information that is not necessary.
        * 
        * @return
        */
        public QuickInfo Get_quick_info()
        {
            DiDataElement de = new DiDataElement();
            QuickInfo qi = new QuickInfo();

            VrFormat = DiFile.VrExplicit;
            Endianess = DiFile.EndianLittle;

            //        // media stored sop
            //        de = scan_for(0x00020002);
            //        if (de!=null) {
            //            qi._media_stored_sop_class_uid = de.get_value_as_string();
            //        } else {
            //            return null;
            //        }

            // transfer syntax
            de = Scan_for(0x00020010);
            if (de != null)
            {
                String ts_uid = de.GetValueAsString();
                VrFormat = DiDictonary.get_ts_uid_vr_format(ts_uid);
                Endianess = DiDictonary.get_ts_uid_endianess(ts_uid);
                if (VrFormat == DiFile.EndianUnknown)
                {
                    Debug.Log("DiFileInputStream::get_quick_info Warning: Unknown Transfer Syntax UID \"" + ts_uid + "\". Endianess & VR format will be guessed.");
                }
            }

            // image type
            de = Scan_for(0x00080008);
            if (de != null)
            {
                string image_type = de.GetValueAsString();
                string[] split = image_type.Split(new [] {"\\\\"}, StringSplitOptions.None);
                if (split.Length > 2 && split[2].Equals("SCOUT"))
                {
                    qi.Scout = true;
                }
                else
                {
                    qi.Scout = false;
                }
            }

            // series uid
            de = Scan_for(0x0020000e);
            if (de != null)
            {
                qi.SeriesUid = de.GetValueAsString();
            }
            else
            {
                return null;
            }

            // image number
            de = Scan_for(0x00200013);
            if (de != null)
            {
                qi.ImageNumber = de.GetInt();
            }
            else
            {
                // TODO: better error handling
                qi.ImageNumber = 0;
            }

            return qi;
        }

        public uint ReadUShort(int endianess)
        {
            byte[] val = new byte[2];
            Read(val, 0, val.Length);

            if (endianess == DiFile.EndianBig)
            {
                Array.Reverse(val);
            }

            return BitConverter.ToUInt16(val, 0);
        }

        public int ReadShort(int endianess)
        {
            byte[] val = new byte[2];
            Read(val, 0, val.Length);

            if (endianess == DiFile.EndianBig)
            {
                Array.Reverse(val);
            }

            return BitConverter.ToInt16(val, 0);
        }

        public Int32 ReadInt(int endianess)
        {
            byte[] val = new byte[4];
            Read(val, 0, val.Length);

            if (endianess == DiFile.EndianBig)
            {
                Array.Reverse(val);
            }

            return BitConverter.ToInt32(val, 0);
        }

        public bool SkipHeader()
        {
            if (!CanSeek || Length < 128 || Seek(128, SeekOrigin.Begin) <= 0)
            {
                return false;
            }
            byte[] dicm = new byte[4];

            return Read(dicm, 0, dicm.Length) == 4 && ("DICM".Equals(Encoding.ASCII.GetString(dicm)));
        }

    }
}
