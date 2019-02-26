
using System.IO;
using System;
using System.Text;
using GLTF.Math;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace DICOMParser
{
    /// <summary>
    /// Stream for parsing a DiFile
    /// </summary>
    public class DiFileStream : FileStream
    {
        public bool MetaGroup, BeforeMetaGroup;

        public int VrFormat { get; set; }
        public int Endianess { get; set; }

        public class QuickInfo
        {
            public string SeriesUid;
            //public String _media_stored_sop_class_uid;
            public int ImageNumber;
            public bool Scout;

            public override string ToString()
            {
                return SeriesUid + ", " + ImageNumber + ", " + Scout;
            }
        }

        /// <summary>
        /// Creates a file stream for the given file
        /// </summary>
        /// <param name="fName"></param>
        public DiFileStream(string fName) : base(fName, FileMode.Open, FileAccess.Read, FileShare.Read)
        {
            Endianess = DiFile.EndianUnknown;
            VrFormat = DiFile.VrUnknown;
            BeforeMetaGroup = true;
            MetaGroup = false;
        }

        /// <summary>
        /// Scans for the given tag in the di file
        /// </summary>
        /// <param name="searchTag">tag id to look for</param>
        /// <returns></returns>
        public DiDataElement Scan_for(uint searchTag)
        {
            var de = new DiDataElement();
            var exceptionOccured = false;
            var inSequence = false;
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

        /// <summary>
        /// Fills a QuickInfo objects and skips all information that is not necessary.
        /// </summary>
        /// <returns></returns>
        public QuickInfo Get_quick_info()
        {
            var de = new DiDataElement();
            var qi = new QuickInfo();

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
                var tsUid = de.GetValueAsString();
                VrFormat = DiDictonary.get_ts_uid_vr_format(tsUid);
                Endianess = DiDictonary.get_ts_uid_endianess(tsUid);
                if (VrFormat == DiFile.EndianUnknown)
                {
                    Debug.Log("DiFileInputStream::get_quick_info Warning: Unknown Transfer Syntax UID \"" + tsUid + "\". Endianess & VR format will be guessed.");
                }
            }

            // image type
            de = Scan_for(0x00080008);
            if (de != null)
            {
                var imageType = de.GetValueAsString();
                var split = imageType.Split(new [] {"\\\\"}, StringSplitOptions.None);
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
            qi.ImageNumber = de?.GetInt() ?? 0;

            return qi;
        }

        /// <summary>
        /// Reads an unsigned short from the stream
        /// </summary>
        /// <param name="endianess"></param>
        /// <returns></returns>
        public uint ReadUShort(int endianess)
        {
            var val = new byte[2];
            Read(val, 0, val.Length);

            if (endianess == DiFile.EndianBig)
            {
                Array.Reverse(val);
            }

            return BitConverter.ToUInt16(val, 0);
        }

        /// <summary>
        /// Reads a signed short from the stream
        /// </summary>
        /// <param name="endianess"></param>
        /// <returns></returns>
        public int ReadShort(int endianess)
        {
            var val = new byte[2];
            Read(val, 0, val.Length);

            if (endianess == DiFile.EndianBig)
            {
                Array.Reverse(val);
            }

            return BitConverter.ToInt16(val, 0);
        }

        /// <summary>
        /// Reads a signed integer from the stream
        /// </summary>
        /// <param name="endianess"></param>
        /// <returns></returns>
        public int ReadInt(int endianess)
        {
            var val = new byte[4];
            Read(val, 0, val.Length);

            if (endianess == DiFile.EndianBig)
            {
                Array.Reverse(val);
            }

            return BitConverter.ToInt32(val, 0);
        }

        /// <summary>
        /// Checks if the current file is a dicom file and skips the header.
        /// </summary>
        /// <returns></returns>
        public bool SkipHeader()
        {
            if (!CanSeek || Length < 128 || Seek(128, SeekOrigin.Begin) <= 0)
            {
                return false;
            }
            var dicm = new byte[4];

            return Read(dicm, 0, dicm.Length) == 4 && ("DICM".Equals(Encoding.ASCII.GetString(dicm)));
        }

    }
}
