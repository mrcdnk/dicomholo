
using System.IO;
using System;
using System.Text;

namespace DICOMParser
{
    public class DiFileStream : FileStream
    {
        //General assumption: little_endian!!
        private bool little_endian = true;

        public DiFileStream(string fname) : base(fname, FileMode.Open, FileAccess.Read, FileShare.Read){}

        public uint ReadShort()
        {
            byte[] val = new byte[2];
            Read(val, 0, val.Length);

            if (!little_endian)
            {
                Array.Reverse(val);
            }

            return BitConverter.ToUInt16(val, 0);
        }

        public Int32 ReadInt()
        {
            byte[] val = new byte[4];
            Read(val, 0, val.Length);

            if (!little_endian)
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
