using System;
using System.Collections.Generic;
using UnityEngine;

namespace DICOMParser
{
    public enum VRType : uint
    {
        // only use THESE constants as VR types. DO NOT HARDCODE VR identifiers.
        AE = 0x4145, // AE (Application Entity)
        AS = 0x4153, // AS (Age String)
        AT = 0x4154, // AT (Attribute Tag)
        CS = 0x4353, // CS (Code String)
        DA = 0x4441, // DA (Date)
        DS = 0x4453, // DS (Decimal String)
        DT = 0x4454, // DT (Date Time)
        FD = 0x4644, // FD (Floating Point Double)
        FL = 0x464C, // FL (Floating Point Single)
        IS = 0x4953, // IS (Integer String)
        LO = 0x4C4F, // LO (Long String)
        LT = 0x4C54, // LT (Long Text)
        PN = 0x504E, // PN (Patient Name)
        SH = 0x5348, // SH (Short String)
        SL = 0x534C, // SL (Signed Long)
        SS = 0x5353, // SS (Signed Short)
        ST = 0x5354, // ST (Short Text)
        TM = 0x544D, // TM (Time)
        UC = 0x5543, // UC (?)
        UI = 0x5549, // UI (Unique Identifier)
        UL = 0x554C, // UL (Unsigned Long)
        UR = 0x5552, // UR (?)
        US = 0x5553, // US (Unsigned Short)
        UT = 0x5554, // UT (Unlimited Text)
        OB = 0x4F42, // OB (Other Byte)
        OF = 0x4F46, // OF (Other Float)
        OW = 0x4F57, // OW (Other Word)
        SQ = 0x5351, // SQ (Sequence)

        // special types
        UN = 0x554E, // 
        QQ = 0x3F3F, // 
        OX = 0x4F58, //
        DL = 0x444C, //
        XX = 0x0000 //
    };

    public sealed class DiDictonary
    {
        private static readonly Dictionary<uint, TagMetaData> DataElementMap = new Dictionary<uint, TagMetaData>();
        private static readonly Dictionary<string, string> MediaStorageMap = new Dictionary<string, string>();
        private static readonly Dictionary<string, TSUIDMetaData> TuidStorageMap = new Dictionary<string, TSUIDMetaData>();

        public static DiDictonary Instance = new DiDictonary();

        private class TSUIDMetaData
        {
            public string Uid;
            public string Descr;
            public string AdditionalInfo;
            public int VrFormat;
            public int Endianess;
            public bool Retired;

            public TSUIDMetaData(string uid, string descr, string additional_info, int vr_format, int endianess, bool retired)
            {
                Uid = uid;
                Descr = descr;
                AdditionalInfo = additional_info;
                VrFormat = vr_format;
                Endianess = endianess;
                Retired = retired;
            }
        }


        /// <summary>
        /// Private class for storing the tag meta data in the DiDi HashTable.
        /// It contains only the value range and its description of a given
        /// tag, the tag id itself is stored in the global DiDi HashTable.
        /// @author kif
        /// </summary>
        private class TagMetaData
        {
            public VRType vr;
            public string descr;

            /// <summary>
            /// The one and only constructor for this class.
            /// </summary>
            /// <param name="vr"> the VR identifier</param>
            /// <param name="descr">the VR description</param>
            public TagMetaData(VRType vr, string descr)
            {
                this.vr = vr;
                this.descr = descr;
            }
        }

        private void tsuid_register(TSUIDMetaData ts_uid_meta)
        {
            TuidStorageMap[ts_uid_meta.Uid] =  ts_uid_meta;
        }

        /// <summary>
        /// Returns the endianess (little or big) based on the transfer syntax UID
        /// </summary>
        /// <param name="ts_uid">transfer syntax UID</param>
        /// <returns>DiFile.EndianLittle, DiFile.EndianBig or DiFile.DiFile.ENDIAN_UNKNOWN</returns>
        public static int get_ts_uid_endianess(string ts_uid)
        {
            int result = DiFile.EndianUnknown;

            TSUIDMetaData tsUidMeta = TuidStorageMap[ts_uid];
            if (tsUidMeta != null)
            {
                result = tsUidMeta.Endianess;
            }

            return result;
        }

        /// <summary>
        /// Returns the VR format (explicit or implicit) based on the transfer syntax UID
        /// </summary>
        /// <param name="ts_uid">transfer syntax UID</param>
        /// <returns>DiFile.VrExplicit, DiFile.VR_IMPLICIT or DiFile.VR_UNKOWN</returns>
        public static int get_ts_uid_vr_format(string ts_uid)
        {
            int result = DiFile.VrUnknown;

            TSUIDMetaData tsUidMeta = TuidStorageMap[ts_uid];
            if (tsUidMeta != null)
            {
                result = tsUidMeta.VrFormat;
            }

            return result;
        }

        /// <summary>
        /// Returns a textual description of the given transfer syntax UID as defined by DICOM
        /// </summary>
        /// <param name="ts_uid">transfer syntax UID</param>
        /// <returns>textual description as defined by DICOM</returns>
        public static string get_ts_uid_descr(string ts_uid)
        {
            string result = "unknown";

            TSUIDMetaData tsUidMeta = TuidStorageMap[ts_uid];
            if (tsUidMeta != null)
            {
                result = tsUidMeta.Descr;
            }

            return result;
        }

        /// <summary>
        /// Returns the additional info of the given transfer syntax UID as defined by DICOM
        /// </summary>
        /// <param name="ts_uid">transfer syntax UID</param>
        /// <returns>additional info as written as defined by DICOM</returns>
        public static string get_ts_uid_additional_info(string ts_uid)
        {
            string result = "unknown";

            TSUIDMetaData tsUidMeta = TuidStorageMap[ts_uid];
            if (tsUidMeta != null)
            {
                result = tsUidMeta.AdditionalInfo;
            }

            return result;
        }

        /// <summary>
        /// Returns true if the TS UID is not supported by the latest DICOM version anymore
        /// </summary>
        /// <param name="ts_uid">transfer syntax UID</param>
        /// <returns>true if the transfer syntax UID is retired, false if not</returns>
        public static bool get_ts_uid_retired(string ts_uid)
        {
            bool result = false;

            TSUIDMetaData tsUidMeta = TuidStorageMap[ts_uid];
            if (tsUidMeta != null)
            {
                result = tsUidMeta.Retired;
            }

            return result;
        }

        /// <summary>
        /// Returns true if the transfer syntax UID is part of DICOM
        /// </summary>
        /// <param name="ts_uid">transfer syntax UID</param>
        /// <returns>true if it is a known UID</returns>
        public static bool is_ts_uid_known(string ts_uid)
        {
            return TuidStorageMap.ContainsKey(ts_uid);
        }

        /// <summary>
        /// The default constructor is private (singleton).
        /// </summary>
        private DiDictonary()
        {
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2", "Implicit VR Endian",
               "Default Transfer Syntax for DICOM", DiFile.VrImplicit, DiFile.EndianLittle, false));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.1", "Explicit VR Little Endian", "", DiFile.VrExplicit, DiFile.EndianLittle, false));

            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.1.99", "Deflated Explicit VR Big Endian",
                    "", DiFile.VrExplicit, DiFile.EndianBig, false));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.2", "Explicit VR Big Endian", "",
                    DiFile.VrExplicit, DiFile.EndianBig, false));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.50", "JPEG Baseline (Process 1)",
                    "Default Transfer Syntax for Lossy JPEG 8-bit Image Compression", DiFile.EndianLittle, DiFile.VrExplicit, false));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.51", "JPEG Baseline (Processes 2 & 4)",
                    "Default Transfer Syntax for Lossy JPEG 12-bit Image Compression (Process 4 only)", DiFile.EndianLittle, DiFile.VrExplicit, false));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.52", "JPEG Extended (Processes 3 & 5)",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, true));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.53", "JPEG Spectral Selection, Nonhierarchical (Processes 6 & 8)",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, true));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.54", "JPEG Spectral Selection, Nonhierarchical (Processes 7 & 9)",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, true));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.55", "JPEG Full Progression, Nonhierarchical (Processes 10 & 12)",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, true));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.56", "JPEG Full Progression, Nonhierarchical (Processes 11 & 13)",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, true));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.57", "JPEG Lossless, Nonhierarchical (Processes 14)",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, false));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.58", "JPEG Lossless, Nonhierarchical (Processes 15)",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, true));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.59", "JPEG Extended, Hierarchical (Processes 16 & 18)",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, true));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.60", "JPEG Extended, Hierarchical (Processes 17 & 19)",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, true));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.61", "JPEG Spectral Selection, Hierarchical (Processes 20 & 22)",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, true));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.62", "JPEG Spectral Selection, Hierarchical (Processes 21 & 23)",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, true));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.63", "JPEG Full Progression, Hierarchical (Processes 24 & 26)",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, true));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.64", "JPEG Full Progression, Hierarchical (Processes 25 & 27)",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, true));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.65", "JPEG Lossless, Nonhierarchical (Process 28)",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, true));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.66", "JPEG Lossless, Nonhierarchical (Process 29)",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, true));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.70", "JPEG Lossless, Nonhierarchical, First-Order Prediction (Processes 14 [Selection Value 1])",
                    "Default Transfer Syntax for Lossless JPEG Image Compression", DiFile.EndianLittle, DiFile.VrExplicit, false));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.80", "JPEG-LS Lossless Image Compression",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, false));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.81", "JPEG-LS Lossy (Near-Lossless) Image Compression",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, false));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.90", "JPEG 2000 Image Compression (Lossless Only)",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, false));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.91", "JPEG 2000 Image Compression",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, false));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.92", "JPEG 2000 Part 2 Multicomponent Image Compression (Lossless Only)",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, false));
            tsuid_register(new TSUIDMetaData("1.2.840.10008.1.2.4.93", "JPEG 2000 Part 2 Multicomponent Image Compression",
                    "", DiFile.EndianLittle, DiFile.VrExplicit, false));

            MediaStorageMap.Add("1.2.840.10008.1.3.10", "Media Storage Directory Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.1", "ComAdded Radiography Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.1.1", "Digital X-Ray Image Storage For Presentation");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.1.1.1", "Digital X-Ray Image Storage For Processing");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.1.2", "Digital Mammography Image Storage For Presentation");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.1.2.1", "Digital Mammography Image Storage For Processing");

            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.1.3", "Digital Intra-oral X-Ray Image Storage For Presentation");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.1.3.1", "Digital Intra-oral X-Ray Image Storage For Processing");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.2", "CT Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.2.1", "Enhanced CT Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.3.1", "Ultrasound Multi-frame Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.4", "MR Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.4.1", "Enhanced MR Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.4.2", "MR Spectroscopy Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.6.1", "Ultrasound Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.7", "Secondary Capture Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.7.1", "Multi-frame Single Bit Secondary Capture Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.7.2", "Secondary Capture Image Multi-frame Grayscale Byte Secondary Capture Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.7.3", "Multi-frame Grayscale Word Secondary Capture Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.7.4", "Multi-frame True Color Secondary Capture Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.9.1.1", "12-lead ECG Waveform Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.9.1.2", "General ECG Waveform Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.9.1.3", "Ambulatory ECG Waveform Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.9.2.1", "Hemodynamic Waveform Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.9.3.1", "Cardiac Electrophysiology Waveform Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.9.4.1", "Basic Voice Audio Waveform Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.11.1", "Grayscale Softcopy Presentation State Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.11.2", "Color Softcopy Presentation State Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.11.3", "Presentation State Storage");

            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.11.4", "Blending Softcopy Presentation State Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.12.1", "X-Ray Angiographic Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.12.1.1", "Enhanced XA Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.12.2", "X-Ray Radiofluoroscopic Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.12.2.1", "Enhanced XRF Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.13.1.1", "X-Ray 3D Angiographic Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.13.1.2", "X-Ray 3D Craniofacial Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.20", "Nuclear Medicine Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.66", "Raw Data Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.66.1", "Spatial Registration Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.66.2", "Spatial Fiducials Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.66.3", "Deformable Spatial Registration Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.66.4", "Segmentation Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.67", "Real World Value Mapping Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.1", "VL Endoscopic Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.1.1", "Video Endoscopic Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.2", "VL Microscopic Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.2.1", "Video Microscopic Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.3", "VL Slide-Coordinates Microscopic Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.4", "VL Photographic Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.4.1", "Video Photographic Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.5.1", "Ophthalmic Photography 8 Bit Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.5.2", "Ophthalmic Photography 16 Bit Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.5.3", "Stereometric Relationship Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.5.4", "Ophthalmic Tomography Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.88.11", "Basic Text SR Enhanced SR");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.88.22", "Basic Text SR");

            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.88.33", "Comprehensive SR");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.88.40", "Procedure Log");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.88.50", "Mammography CAD SR");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.88.59", "Key Object Selection Document");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.88.65", "Chest CAD SR");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.88.67", "X-Ray Radiation Dose SR");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.104.1", "Encapsulated PDF Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.128", "Positron Emission Tomography Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.481.1", "RT Image Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.481.2", "RT Dose Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.481.3", "RT Structure Set Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.481.4", "RT Beams Treatment Record Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.481.5", "RT Plan Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.481.6", "RT Brachy Treatment Record Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.481.7", "RT Treatment Summary Record Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.481.8", "RT Ion Plan Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.481.9", "RT Ion Beams Treatment Record Storage");
            MediaStorageMap.Add("1.2.840.10008.5.1.4.38.1", "Hanging Protocol Storage");
            
            DataElementMap.Add(0x00000000, new TagMetaData(VRType.UL, "Group 0000 Length"));
            DataElementMap.Add(0x00000001, new TagMetaData(VRType.UL, "Group 0000 Length to End (RET)"));
            DataElementMap.Add(0x00000002, new TagMetaData(VRType.UI, "Affected SOP Class UID"));
            DataElementMap.Add(0x00000003, new TagMetaData(VRType.UI, "Requested SOP Class UID"));
            DataElementMap.Add(0x00000010, new TagMetaData(VRType.SH, "Recognition Code (RET)"));
            DataElementMap.Add(0x00000100, new TagMetaData(VRType.US, "Command Field"));
            DataElementMap.Add(0x00000110, new TagMetaData(VRType.US, "Message ID"));
            DataElementMap.Add(0x00000120, new TagMetaData(VRType.US, "Message Id being Responded to"));
            DataElementMap.Add(0x00000200, new TagMetaData(VRType.AE, "Initiator (RET)"));
            DataElementMap.Add(0x00000300, new TagMetaData(VRType.AE, "Receiver (RET)"));
            DataElementMap.Add(0x00000400, new TagMetaData(VRType.AE, "Find Location (RET)"));
            DataElementMap.Add(0x00000600, new TagMetaData(VRType.AE, "Move Destination"));
            DataElementMap.Add(0x00000700, new TagMetaData(VRType.US, "Priority"));
            DataElementMap.Add(0x00000800, new TagMetaData(VRType.US, "Data Set Type"));
            DataElementMap.Add(0x00000850, new TagMetaData(VRType.US, "Number of Matches (RET)"));
            DataElementMap.Add(0x00000860, new TagMetaData(VRType.US, "Response Sequence Number (RET)"));
            DataElementMap.Add(0x00000900, new TagMetaData(VRType.US, "Status"));
            DataElementMap.Add(0x00000901, new TagMetaData(VRType.AT, "Offending Element"));
            DataElementMap.Add(0x00000902, new TagMetaData(VRType.LO, "Error Comment"));
            DataElementMap.Add(0x00000903, new TagMetaData(VRType.US, "Error ID"));
            DataElementMap.Add(0x00001000, new TagMetaData(VRType.UI, "Affected SOP Instance UID"));
            DataElementMap.Add(0x00001001, new TagMetaData(VRType.UI, "Requested SOP Instance UID"));
            DataElementMap.Add(0x00001002, new TagMetaData(VRType.US, "Event Type ID"));
            DataElementMap.Add(0x00001005, new TagMetaData(VRType.AT, "Attribute Identifier List"));
            DataElementMap.Add(0x00001008, new TagMetaData(VRType.US, "Action Type ID"));
            DataElementMap.Add(0x00001012, new TagMetaData(VRType.UI, "Requested SOP Instance UID List"));
            DataElementMap.Add(0x00001020, new TagMetaData(VRType.US, "Number of Remaining Sub-operations"));
            DataElementMap.Add(0x00001021, new TagMetaData(VRType.US, "Number of Completed Sub-operations"));
            DataElementMap.Add(0x00001022, new TagMetaData(VRType.US, "Number of Failed Sub-operations"));
            DataElementMap.Add(0x00001023, new TagMetaData(VRType.US, "Number of Warning Sub-operations"));
            DataElementMap.Add(0x00001030, new TagMetaData(VRType.AE, "Move Originator Application Entity Title"));
            DataElementMap.Add(0x00001031, new TagMetaData(VRType.US, "Move Originator Message ID"));
            DataElementMap.Add(0x00005010, new TagMetaData(VRType.LO, "Message Set ID (RET)"));
            DataElementMap.Add(0x00005020, new TagMetaData(VRType.LO, "End Message Set ID (RET)"));
            DataElementMap.Add(0x00020000, new TagMetaData(VRType.UL, "Group 0002 Length"));
            DataElementMap.Add(0x00020001, new TagMetaData(VRType.OB, "File Meta Information Version"));
            DataElementMap.Add(0x00020002, new TagMetaData(VRType.UI, "Media Stored SOP Class UID"));
            DataElementMap.Add(0x00020003, new TagMetaData(VRType.UI, "Media Stored SOP Instance UID"));
            DataElementMap.Add(0x00020010, new TagMetaData(VRType.UI, "Transfer Syntax UID"));
            DataElementMap.Add(0x00020012, new TagMetaData(VRType.UI, "Implementation Class UID"));
            DataElementMap.Add(0x00020013, new TagMetaData(VRType.SH, "Implementation Version Name"));
            DataElementMap.Add(0x00020016, new TagMetaData(VRType.AE, "Source Application Entity Title"));
            DataElementMap.Add(0x00020100, new TagMetaData(VRType.UI, "Private Information Creator UID"));
            DataElementMap.Add(0x00020102, new TagMetaData(VRType.OB, "Private Information"));
            DataElementMap.Add(0x00040000, new TagMetaData(VRType.UL, "Group 0004 Length"));
            DataElementMap.Add(0x00041130, new TagMetaData(VRType.CS, "File-set ID"));
            DataElementMap.Add(0x00041141, new TagMetaData(VRType.CS, "File-set Descriptor File File ID"));
            DataElementMap.Add(0x00041142, new TagMetaData(VRType.CS, "File-set Descriptor File Format"));
            DataElementMap.Add(0x00041200, new TagMetaData(VRType.UL, "Root Directory Entity's First Directory Record Offset"));
            DataElementMap.Add(0x00041202, new TagMetaData(VRType.UL, "Root Directory Entity's Last Directory Record Offset"));
            DataElementMap.Add(0x00041212, new TagMetaData(VRType.US, "File-set Consistence Flag"));
            DataElementMap.Add(0x00041220, new TagMetaData(VRType.SQ, "Directory Record Sequence"));
            DataElementMap.Add(0x00041400, new TagMetaData(VRType.UL, "Next Directory Record Offset"));
            DataElementMap.Add(0x00041410, new TagMetaData(VRType.US, "Record In-use Flag"));
            DataElementMap.Add(0x00041420, new TagMetaData(VRType.UL, "Referenced Lower-level Directory Entity Offset"));
            DataElementMap.Add(0x00041430, new TagMetaData(VRType.CS, "Directory Record Type"));
            DataElementMap.Add(0x00041432, new TagMetaData(VRType.UI, "Private Record UID"));
            DataElementMap.Add(0x00041500, new TagMetaData(VRType.CS, "Referenced File ID"));
            DataElementMap.Add(0x00041510, new TagMetaData(VRType.UI, "Referenced SOP Class UID in File"));
            DataElementMap.Add(0x00041511, new TagMetaData(VRType.UI, "Referenced SOP Instance UID in File"));
            DataElementMap.Add(0x00041600, new TagMetaData(VRType.UL, "Number of References"));
            DataElementMap.Add(0x00080000, new TagMetaData(VRType.UL, "Group 0008 Length"));
            DataElementMap.Add(0x00080001, new TagMetaData(VRType.UL, "Group 0008 Length to End (RET)"));
            DataElementMap.Add(0x00080005, new TagMetaData(VRType.CS, "Specific Character Set"));
            DataElementMap.Add(0x00080008, new TagMetaData(VRType.CS, "Image Type"));
            DataElementMap.Add(0x00080010, new TagMetaData(VRType.SH, "Recognition Code (RET)"));
            DataElementMap.Add(0x00080012, new TagMetaData(VRType.DA, "Instance Creation Date"));
            DataElementMap.Add(0x00080013, new TagMetaData(VRType.TM, "Instance Creation Time"));
            DataElementMap.Add(0x00080014, new TagMetaData(VRType.UI, "Instance Creator UID"));
            DataElementMap.Add(0x00080016, new TagMetaData(VRType.UI, "SOP Class UID"));
            DataElementMap.Add(0x00080018, new TagMetaData(VRType.UI, "SOP Instance UID"));
            DataElementMap.Add(0x00080020, new TagMetaData(VRType.DA, "Study Date"));
            DataElementMap.Add(0x00080021, new TagMetaData(VRType.DA, "Series Date"));
            DataElementMap.Add(0x00080022, new TagMetaData(VRType.DA, "Acquisition Date"));
            DataElementMap.Add(0x00080023, new TagMetaData(VRType.DA, "Image Date"));
            DataElementMap.Add(0x00080024, new TagMetaData(VRType.DA, "Overlay Date"));
            DataElementMap.Add(0x00080025, new TagMetaData(VRType.DA, "Curve Date"));
            DataElementMap.Add(0x00080030, new TagMetaData(VRType.TM, "Study Time"));
            DataElementMap.Add(0x00080031, new TagMetaData(VRType.TM, "Series Time"));
            DataElementMap.Add(0x00080032, new TagMetaData(VRType.TM, "Acquisition Time"));
            DataElementMap.Add(0x00080033, new TagMetaData(VRType.TM, "Image Time"));
            DataElementMap.Add(0x00080034, new TagMetaData(VRType.TM, "Overlay Time"));
            DataElementMap.Add(0x00080035, new TagMetaData(VRType.TM, "Curve Time"));
            DataElementMap.Add(0x00080040, new TagMetaData(VRType.US, "Data Set Type (RET)"));
            DataElementMap.Add(0x00080041, new TagMetaData(VRType.SH, "Data Set Subtype (RET)"));
            DataElementMap.Add(0x00080042, new TagMetaData(VRType.CS, "Nuclear Medicine Series Type"));
            DataElementMap.Add(0x00080050, new TagMetaData(VRType.SH, "Accession Number"));
            DataElementMap.Add(0x00080052, new TagMetaData(VRType.CS, "Query/Retrieve Level"));
            DataElementMap.Add(0x00080054, new TagMetaData(VRType.AE, "Retrieve AE Title"));
            DataElementMap.Add(0x00080058, new TagMetaData(VRType.AE, "Failed SOP Instance UID List"));
            DataElementMap.Add(0x00080060, new TagMetaData(VRType.CS, "Modality"));
            DataElementMap.Add(0x00080064, new TagMetaData(VRType.CS, "Conversion Type"));
            DataElementMap.Add(0x00080070, new TagMetaData(VRType.LO, "Manufacturer"));
            DataElementMap.Add(0x00080080, new TagMetaData(VRType.LO, "Institution Name"));
            DataElementMap.Add(0x00080081, new TagMetaData(VRType.ST, "Institution Address"));
            DataElementMap.Add(0x00080082, new TagMetaData(VRType.SQ, "Institution Code Sequence"));
            DataElementMap.Add(0x00080090, new TagMetaData(VRType.PN, "Referring Physician's Name"));
            DataElementMap.Add(0x00080092, new TagMetaData(VRType.ST, "Referring Physician's Address"));
            DataElementMap.Add(0x00080094, new TagMetaData(VRType.SH, "Referring Physician's Telephone Numbers"));
            DataElementMap.Add(0x00080100, new TagMetaData(VRType.SH, "Code Value"));
            DataElementMap.Add(0x00080102, new TagMetaData(VRType.SH, "Coding Scheme Designator"));
            DataElementMap.Add(0x00080104, new TagMetaData(VRType.LO, "Code Meaning"));
            DataElementMap.Add(0x00081000, new TagMetaData(VRType.SH, "Network ID (RET)"));
            DataElementMap.Add(0x00081010, new TagMetaData(VRType.SH, "Station Name"));
            DataElementMap.Add(0x00081030, new TagMetaData(VRType.LO, "Study Description"));
            DataElementMap.Add(0x00081032, new TagMetaData(VRType.SQ, "Procedure Code Sequence"));
            DataElementMap.Add(0x0008103E, new TagMetaData(VRType.LO, "Series Description"));
            DataElementMap.Add(0x00081040, new TagMetaData(VRType.LO, "Institutional Department Name"));
            DataElementMap.Add(0x00081050, new TagMetaData(VRType.PN, "Attending Physician's Name"));
            DataElementMap.Add(0x00081060, new TagMetaData(VRType.PN, "Name of Physician(s) Reading Study"));
            DataElementMap.Add(0x00081070, new TagMetaData(VRType.PN, "Operator's Name"));
            DataElementMap.Add(0x00081080, new TagMetaData(VRType.LO, "Admitting Diagnoses Description"));
            DataElementMap.Add(0x00081084, new TagMetaData(VRType.SQ, "Admitting Diagnosis Code Sequence"));
            DataElementMap.Add(0x00081090, new TagMetaData(VRType.LO, "Manufacturer's Model Name"));
            DataElementMap.Add(0x00081100, new TagMetaData(VRType.SQ, "Referenced Results Sequence"));
            DataElementMap.Add(0x00081110, new TagMetaData(VRType.SQ, "Referenced Study Sequence"));
            DataElementMap.Add(0x00081111, new TagMetaData(VRType.SQ, "Referenced Study Component Sequence"));
            DataElementMap.Add(0x00081115, new TagMetaData(VRType.SQ, "Referenced Series Sequence"));
            DataElementMap.Add(0x00081120, new TagMetaData(VRType.SQ, "Referenced Patient Sequence"));
            DataElementMap.Add(0x00081125, new TagMetaData(VRType.SQ, "Referenced Visit Sequence"));
            DataElementMap.Add(0x00081130, new TagMetaData(VRType.SQ, "Referenced Overlay Sequence"));
            DataElementMap.Add(0x00081140, new TagMetaData(VRType.SQ, "Referenced Image Sequence"));
            DataElementMap.Add(0x00081145, new TagMetaData(VRType.SQ, "Referenced Curve Sequence"));
            DataElementMap.Add(0x00081150, new TagMetaData(VRType.UI, "Referenced SOP Class UID"));
            DataElementMap.Add(0x00081155, new TagMetaData(VRType.UI, "Referenced SOP Instance UID"));
            DataElementMap.Add(0x00082111, new TagMetaData(VRType.ST, "Derivation Description"));
            DataElementMap.Add(0x00082112, new TagMetaData(VRType.SQ, "Source Image Sequence"));
            DataElementMap.Add(0x00082120, new TagMetaData(VRType.SH, "Stage Name"));
            DataElementMap.Add(0x00082122, new TagMetaData(VRType.IS, "Stage Number"));
            DataElementMap.Add(0x00082124, new TagMetaData(VRType.IS, "Number of Stages"));
            DataElementMap.Add(0x00082129, new TagMetaData(VRType.IS, "Number of Event Timers"));
            DataElementMap.Add(0x00082128, new TagMetaData(VRType.IS, "View Number"));
            DataElementMap.Add(0x0008212A, new TagMetaData(VRType.IS, "Number of Views in Stage"));
            DataElementMap.Add(0x00082130, new TagMetaData(VRType.DS, "Event Elapsed Time(s)"));
            DataElementMap.Add(0x00082132, new TagMetaData(VRType.LO, "Event Timer Name(s)"));
            DataElementMap.Add(0x00082142, new TagMetaData(VRType.IS, "Start Trim"));
            DataElementMap.Add(0x00082143, new TagMetaData(VRType.IS, "Stop Trim"));
            DataElementMap.Add(0x00082144, new TagMetaData(VRType.IS, "Recommended Display Frame Rate"));
            DataElementMap.Add(0x00082200, new TagMetaData(VRType.CS, "Transducer Position"));
            DataElementMap.Add(0x00082204, new TagMetaData(VRType.CS, "Transducer Orientation"));
            DataElementMap.Add(0x00082208, new TagMetaData(VRType.CS, "Anatomic Structure"));
            DataElementMap.Add(0x00084000, new TagMetaData(VRType.SH, "Group 0008 Comments (RET)"));
            DataElementMap.Add(0x00089215, new TagMetaData(VRType.SQ, "Derivation Code Sequence"));
            DataElementMap.Add(0x00090010, new TagMetaData(VRType.LO, "unknown"));
            DataElementMap.Add(0x00100000, new TagMetaData(VRType.UL, "Group 0010 Length"));
            DataElementMap.Add(0x00100010, new TagMetaData(VRType.PN, "Patient's Name"));
            DataElementMap.Add(0x00100020, new TagMetaData(VRType.LO, "Patient ID"));
            DataElementMap.Add(0x00100021, new TagMetaData(VRType.LO, "Issuer of Patient ID"));
            DataElementMap.Add(0x00100030, new TagMetaData(VRType.DA, "Patient's Birth Date"));
            DataElementMap.Add(0x00100032, new TagMetaData(VRType.TM, "Patient's Birth Time"));
            DataElementMap.Add(0x00100040, new TagMetaData(VRType.CS, "Patient's Sex"));
            DataElementMap.Add(0x00100042, new TagMetaData(VRType.SH, "Patient's Social Security Number"));
            DataElementMap.Add(0x00100050, new TagMetaData(VRType.SQ, "Patient's Insurance Plan Code Sequence"));
            DataElementMap.Add(0x00101000, new TagMetaData(VRType.LO, "Other Patient IDs"));
            DataElementMap.Add(0x00101001, new TagMetaData(VRType.PN, "Other Patient Names"));
            DataElementMap.Add(0x00101005, new TagMetaData(VRType.PN, "Patient's Maiden Name"));
            DataElementMap.Add(0x00101010, new TagMetaData(VRType.AS, "Patient's Age"));
            DataElementMap.Add(0x00101020, new TagMetaData(VRType.DS, "Patient's Size"));
            DataElementMap.Add(0x00101030, new TagMetaData(VRType.DS, "Patient's Weight"));
            DataElementMap.Add(0x00101040, new TagMetaData(VRType.LO, "Patient's Address"));
            DataElementMap.Add(0x00101050, new TagMetaData(VRType.SH, "Insurance Plan Identification (RET)"));
            DataElementMap.Add(0x00101060, new TagMetaData(VRType.PN, "Patient's Mother's Maiden Name"));
            DataElementMap.Add(0x00101080, new TagMetaData(VRType.LO, "Military Rank"));
            DataElementMap.Add(0x00101081, new TagMetaData(VRType.LO, "Branch of Service"));
            DataElementMap.Add(0x00101090, new TagMetaData(VRType.LO, "Medical Record Locator"));
            DataElementMap.Add(0x00102000, new TagMetaData(VRType.LO, "Medical Alerts"));
            DataElementMap.Add(0x00102110, new TagMetaData(VRType.LO, "Contrast Allergies"));
            DataElementMap.Add(0x00102150, new TagMetaData(VRType.LO, "Country of Residence"));
            DataElementMap.Add(0x00102152, new TagMetaData(VRType.LO, "Region of Residence"));
            DataElementMap.Add(0x00102154, new TagMetaData(VRType.SH, "Patient's Telephone Numbers"));
            DataElementMap.Add(0x00102160, new TagMetaData(VRType.SH, "Ethnic Group"));
            DataElementMap.Add(0x00102180, new TagMetaData(VRType.SH, "Occupation"));
            DataElementMap.Add(0x001021A0, new TagMetaData(VRType.CS, "Smoking Status"));
            DataElementMap.Add(0x001021B0, new TagMetaData(VRType.LT, "Additional Patient History"));
            DataElementMap.Add(0x001021C0, new TagMetaData(VRType.US, "Pregnancy Status"));
            DataElementMap.Add(0x001021D0, new TagMetaData(VRType.DA, "Last Menstrual Date"));
            DataElementMap.Add(0x001021F0, new TagMetaData(VRType.LO, "Patient's Religious Preference"));
            DataElementMap.Add(0x00104000, new TagMetaData(VRType.LT, "Patient Comments"));
            DataElementMap.Add(0x00180000, new TagMetaData(VRType.UL, "Group 0018 Length"));
            DataElementMap.Add(0x00180010, new TagMetaData(VRType.LO, "Contrast/Bolus Agent"));
            DataElementMap.Add(0x00180015, new TagMetaData(VRType.CS, "Body Part Examined"));
            DataElementMap.Add(0x00180020, new TagMetaData(VRType.CS, "Scanning Sequence"));
            DataElementMap.Add(0x00180021, new TagMetaData(VRType.CS, "Sequence Variant"));
            DataElementMap.Add(0x00180022, new TagMetaData(VRType.CS, "Scan Options"));
            DataElementMap.Add(0x00180023, new TagMetaData(VRType.CS, "MR Acquisition Type"));
            DataElementMap.Add(0x00180024, new TagMetaData(VRType.SH, "Sequence Name"));
            DataElementMap.Add(0x00180025, new TagMetaData(VRType.CS, "Angio Flag"));
            DataElementMap.Add(0x00180030, new TagMetaData(VRType.LO, "Radionuclide"));
            DataElementMap.Add(0x00180031, new TagMetaData(VRType.LO, "Radiopharmaceutical"));
            DataElementMap.Add(0x00180032, new TagMetaData(VRType.DS, "Energy Window Centerline"));
            DataElementMap.Add(0x00180033, new TagMetaData(VRType.DS, "Energy Window Total Width"));
            DataElementMap.Add(0x00180034, new TagMetaData(VRType.LO, "Intervention Drug Name"));
            DataElementMap.Add(0x00180035, new TagMetaData(VRType.TM, "Intervention Drug Start Time"));
            DataElementMap.Add(0x00180040, new TagMetaData(VRType.IS, "Cine Rate"));
            DataElementMap.Add(0x00180050, new TagMetaData(VRType.DS, "Slice Thickness"));
            DataElementMap.Add(0x00180060, new TagMetaData(VRType.DS, "KVP"));
            DataElementMap.Add(0x00180070, new TagMetaData(VRType.IS, "Counts Accumulated"));
            DataElementMap.Add(0x00180071, new TagMetaData(VRType.CS, "Acquisition Termination Condition"));
            DataElementMap.Add(0x00180072, new TagMetaData(VRType.DS, "Effective Series Duration"));
            DataElementMap.Add(0x00180080, new TagMetaData(VRType.DS, "Repetition Time"));
            DataElementMap.Add(0x00180081, new TagMetaData(VRType.DS, "Echo Time"));
            DataElementMap.Add(0x00180082, new TagMetaData(VRType.DS, "Inversion Time"));
            DataElementMap.Add(0x00180083, new TagMetaData(VRType.DS, "Number of Averages"));
            DataElementMap.Add(0x00180084, new TagMetaData(VRType.DS, "Imaging Frequency"));
            DataElementMap.Add(0x00180085, new TagMetaData(VRType.SH, "Imaged Nucleus"));
            DataElementMap.Add(0x00180086, new TagMetaData(VRType.IS, "Echo Numbers(s)"));
            DataElementMap.Add(0x00180087, new TagMetaData(VRType.DS, "Magnetic Field Strength"));
            DataElementMap.Add(0x00180088, new TagMetaData(VRType.DS, "Spacing Between Slices"));
            DataElementMap.Add(0x00180089, new TagMetaData(VRType.IS, "Number of Phase Encoding Steps"));
            DataElementMap.Add(0x00180090, new TagMetaData(VRType.DS, "Data Collection Diameter"));
            DataElementMap.Add(0x00180091, new TagMetaData(VRType.IS, "Echo Train Length"));
            DataElementMap.Add(0x00180093, new TagMetaData(VRType.DS, "Percent Sampling"));
            DataElementMap.Add(0x00180094, new TagMetaData(VRType.DS, "Percent Phase Field of View"));
            DataElementMap.Add(0x00180095, new TagMetaData(VRType.DS, "Pixel Bandwidth"));
            DataElementMap.Add(0x00181000, new TagMetaData(VRType.LO, "Device Serial Number"));
            DataElementMap.Add(0x00181004, new TagMetaData(VRType.LO, "Plate ID"));
            DataElementMap.Add(0x00181010, new TagMetaData(VRType.LO, "Secondary Capture Device ID"));
            DataElementMap.Add(0x00181012, new TagMetaData(VRType.DA, "Date of Secondary Capture"));
            DataElementMap.Add(0x00181014, new TagMetaData(VRType.TM, "Time of Secondary Capture"));
            DataElementMap.Add(0x00181016, new TagMetaData(VRType.LO, "Secondary Capture Device Manufacturer"));
            DataElementMap.Add(0x00181018, new TagMetaData(VRType.LO, "Secondary Capture Device Manufacturer's Model Name"));
            DataElementMap.Add(0x00181019, new TagMetaData(VRType.LO, "Secondary Capture Device Software Version(s)"));
            DataElementMap.Add(0x00181020, new TagMetaData(VRType.LO, "Software Versions(s)"));
            DataElementMap.Add(0x00181022, new TagMetaData(VRType.SH, "Video Image Format Acquired"));
            DataElementMap.Add(0x00181023, new TagMetaData(VRType.LO, "Digital Image Format Acquired"));
            DataElementMap.Add(0x00181030, new TagMetaData(VRType.LO, "Protocol Name"));
            DataElementMap.Add(0x00181040, new TagMetaData(VRType.LO, "Contrast/Bolus Route"));
            DataElementMap.Add(0x00181041, new TagMetaData(VRType.DS, "Contrast/Bolus Volume"));
            DataElementMap.Add(0x00181042, new TagMetaData(VRType.TM, "Contrast/Bolus Start Time"));
            DataElementMap.Add(0x00181043, new TagMetaData(VRType.TM, "Contrast/Bolus Stop Time"));
            DataElementMap.Add(0x00181044, new TagMetaData(VRType.DS, "Contrast/Bolus Total Dose"));
            DataElementMap.Add(0x00181045, new TagMetaData(VRType.IS, "Syringe Counts"));
            DataElementMap.Add(0x00181050, new TagMetaData(VRType.DS, "Spatial Resolution"));
            DataElementMap.Add(0x00181060, new TagMetaData(VRType.DS, "Trigger Time"));
            DataElementMap.Add(0x00181061, new TagMetaData(VRType.LO, "Trigger Source or Type"));
            DataElementMap.Add(0x00181062, new TagMetaData(VRType.IS, "Nominal Interval"));
            DataElementMap.Add(0x00181063, new TagMetaData(VRType.DS, "Frame Time"));
            DataElementMap.Add(0x00181064, new TagMetaData(VRType.LO, "Framing Type"));
            DataElementMap.Add(0x00181065, new TagMetaData(VRType.DS, "Frame Time Vector"));
            DataElementMap.Add(0x00181066, new TagMetaData(VRType.DS, "Frame Delay"));
            DataElementMap.Add(0x00181070, new TagMetaData(VRType.LO, "Radionuclide Route"));
            DataElementMap.Add(0x00181071, new TagMetaData(VRType.DS, "Radionuclide Volume"));
            DataElementMap.Add(0x00181072, new TagMetaData(VRType.TM, "Radionuclide Start Time"));
            DataElementMap.Add(0x00181073, new TagMetaData(VRType.TM, "Radionuclide Stop Time"));
            DataElementMap.Add(0x00181074, new TagMetaData(VRType.DS, "Radionuclide Total Dose"));
            DataElementMap.Add(0x00181080, new TagMetaData(VRType.CS, "Beat Rejection Flag"));
            DataElementMap.Add(0x00181081, new TagMetaData(VRType.IS, "Low R-R Value"));
            DataElementMap.Add(0x00181082, new TagMetaData(VRType.IS, "High R-R Value"));
            DataElementMap.Add(0x00181083, new TagMetaData(VRType.IS, "Intervals Acquired"));
            DataElementMap.Add(0x00181084, new TagMetaData(VRType.IS, "Intervals Rejected"));
            DataElementMap.Add(0x00181085, new TagMetaData(VRType.LO, "PVC Rejection"));
            DataElementMap.Add(0x00181086, new TagMetaData(VRType.IS, "Skip Beats"));
            DataElementMap.Add(0x00181088, new TagMetaData(VRType.IS, "Heart Rate"));
            DataElementMap.Add(0x00181090, new TagMetaData(VRType.IS, "Cardiac Number of Images"));
            DataElementMap.Add(0x00181094, new TagMetaData(VRType.IS, "Trigger Window"));
            DataElementMap.Add(0x00181100, new TagMetaData(VRType.DS, "Reconstruction Diameter"));
            DataElementMap.Add(0x00181110, new TagMetaData(VRType.DS, "Distance Source to Detector"));
            DataElementMap.Add(0x00181111, new TagMetaData(VRType.DS, "Distance Source to Patient"));
            DataElementMap.Add(0x00181120, new TagMetaData(VRType.DS, "Gantry/Detector Tilt"));
            DataElementMap.Add(0x00181130, new TagMetaData(VRType.DS, "Table Height"));
            DataElementMap.Add(0x00181131, new TagMetaData(VRType.DS, "Table Traverse"));
            DataElementMap.Add(0x00181140, new TagMetaData(VRType.CS, "Rotation Direction"));
            DataElementMap.Add(0x00181141, new TagMetaData(VRType.DS, "Angular Position"));
            DataElementMap.Add(0x00181142, new TagMetaData(VRType.DS, "Radial Position"));
            DataElementMap.Add(0x00181143, new TagMetaData(VRType.DS, "Scan Arc"));
            DataElementMap.Add(0x00181144, new TagMetaData(VRType.DS, "Angular Step"));
            DataElementMap.Add(0x00181145, new TagMetaData(VRType.DS, "Center of Rotation Offset"));
            DataElementMap.Add(0x00181146, new TagMetaData(VRType.DS, "Rotation Offset"));
            DataElementMap.Add(0x00181147, new TagMetaData(VRType.CS, "Field of View Shape"));
            DataElementMap.Add(0x00181149, new TagMetaData(VRType.IS, "Field of View Dimensions(s)"));
            DataElementMap.Add(0x00181150, new TagMetaData(VRType.IS, "Exposure Time"));
            DataElementMap.Add(0x00181151, new TagMetaData(VRType.IS, "X-ray Tube Current"));
            DataElementMap.Add(0x00181152, new TagMetaData(VRType.IS, "Exposure"));
            DataElementMap.Add(0x00181160, new TagMetaData(VRType.SH, "Filter Type"));
            DataElementMap.Add(0x00181170, new TagMetaData(VRType.IS, "Generator Power"));
            DataElementMap.Add(0x00181180, new TagMetaData(VRType.SH, "Collimator/grid Name"));
            DataElementMap.Add(0x00181181, new TagMetaData(VRType.CS, "Collimator Type"));
            DataElementMap.Add(0x00181182, new TagMetaData(VRType.IS, "Focal Distance"));
            DataElementMap.Add(0x00181183, new TagMetaData(VRType.DS, "X Focus Center"));
            DataElementMap.Add(0x00181184, new TagMetaData(VRType.DS, "Y Focus Center"));
            DataElementMap.Add(0x00181190, new TagMetaData(VRType.DS, "Focal Spot(s)"));
            DataElementMap.Add(0x00181200, new TagMetaData(VRType.DA, "Date of Last Calibration"));
            DataElementMap.Add(0x00181201, new TagMetaData(VRType.TM, "Time of Last Calibration"));
            DataElementMap.Add(0x00181210, new TagMetaData(VRType.SH, "Convolution Kernel"));
            DataElementMap.Add(0x00181240, new TagMetaData(VRType.DS, "Upper/Lower Pixel Values (RET)"));
            DataElementMap.Add(0x00181242, new TagMetaData(VRType.IS, "Actual Frame Duration"));
            DataElementMap.Add(0x00181243, new TagMetaData(VRType.IS, "Count Rate"));
            DataElementMap.Add(0x00181250, new TagMetaData(VRType.SH, "Receiving Coil"));
            DataElementMap.Add(0x00181251, new TagMetaData(VRType.SH, "Transmitting Coil"));
            DataElementMap.Add(0x00181260, new TagMetaData(VRType.SH, "Screen Type"));
            DataElementMap.Add(0x00181261, new TagMetaData(VRType.LO, "Phosphor Type"));
            DataElementMap.Add(0x00181300, new TagMetaData(VRType.IS, "Scan Velocity"));
            DataElementMap.Add(0x00181301, new TagMetaData(VRType.CS, "Whole Body Technique"));
            DataElementMap.Add(0x00181302, new TagMetaData(VRType.IS, "Scan Length"));
            DataElementMap.Add(0x00181310, new TagMetaData(VRType.US, "Acquisition Matrix"));
            DataElementMap.Add(0x00181312, new TagMetaData(VRType.CS, "Phase Encoding Direction"));
            DataElementMap.Add(0x00181314, new TagMetaData(VRType.DS, "Flip Angle"));
            DataElementMap.Add(0x00181315, new TagMetaData(VRType.CS, "Variable Flip Angle Flag"));
            DataElementMap.Add(0x00181316, new TagMetaData(VRType.DS, "SAR"));
            DataElementMap.Add(0x00181318, new TagMetaData(VRType.DS, "dB/dt"));
            DataElementMap.Add(0x00181400, new TagMetaData(VRType.LO, "Acquisition Device Processing Description"));
            DataElementMap.Add(0x00181401, new TagMetaData(VRType.LO, "Acquisition Device Processing Code"));
            DataElementMap.Add(0x00181402, new TagMetaData(VRType.CS, "Cassette Orientation"));
            DataElementMap.Add(0x00181403, new TagMetaData(VRType.CS, "Cassette Size"));
            DataElementMap.Add(0x00181404, new TagMetaData(VRType.US, "Exposures on Plate"));
            DataElementMap.Add(0x00181405, new TagMetaData(VRType.IS, "Relative X-ray Exposure"));
            DataElementMap.Add(0x00184000, new TagMetaData(VRType.SH, "Group 0018 Comments (RET)"));
            DataElementMap.Add(0x00185000, new TagMetaData(VRType.SH, "Output Power"));
            DataElementMap.Add(0x00185010, new TagMetaData(VRType.LO, "Transducer Data"));
            DataElementMap.Add(0x00185012, new TagMetaData(VRType.DS, "Focus Depth"));
            DataElementMap.Add(0x00185020, new TagMetaData(VRType.LO, "Preprocessing Function"));
            DataElementMap.Add(0x00185021, new TagMetaData(VRType.LO, "Postprocessing Function"));
            DataElementMap.Add(0x00185022, new TagMetaData(VRType.DS, "Mechanical Index"));
            DataElementMap.Add(0x00185024, new TagMetaData(VRType.DS, "Thermal Index"));
            DataElementMap.Add(0x00185026, new TagMetaData(VRType.DS, "Cranial Thermal Index"));
            DataElementMap.Add(0x00185027, new TagMetaData(VRType.DS, "Soft Tissue Thermal Index"));
            DataElementMap.Add(0x00185028, new TagMetaData(VRType.DS, "Soft Tissue-focus Thermal Index"));
            DataElementMap.Add(0x00185029, new TagMetaData(VRType.DS, "Soft Tissue-surface Thermal Index"));
            DataElementMap.Add(0x00185030, new TagMetaData(VRType.IS, "Dynamic Range (RET)"));
            DataElementMap.Add(0x00185040, new TagMetaData(VRType.IS, "Total Gain (RET)"));
            DataElementMap.Add(0x00185050, new TagMetaData(VRType.IS, "Depth of Scan Field"));
            DataElementMap.Add(0x00185100, new TagMetaData(VRType.CS, "Patient Position"));
            DataElementMap.Add(0x00185101, new TagMetaData(VRType.CS, "View Position"));
            DataElementMap.Add(0x00185210, new TagMetaData(VRType.DS, "Image Transformation Matrix"));
            DataElementMap.Add(0x00185212, new TagMetaData(VRType.DS, "Image Translation Vector"));
            DataElementMap.Add(0x00186000, new TagMetaData(VRType.DS, "Sensitivity"));
            DataElementMap.Add(0x00186011, new TagMetaData(VRType.SQ, "Sequence of Ultrasound Regions"));
            DataElementMap.Add(0x00186012, new TagMetaData(VRType.US, "Region Spatial Format"));
            DataElementMap.Add(0x00186014, new TagMetaData(VRType.US, "Region Data Type"));
            DataElementMap.Add(0x00186016, new TagMetaData(VRType.UL, "Region Flags"));
            DataElementMap.Add(0x00186018, new TagMetaData(VRType.UL, "Region Location Min X0"));
            DataElementMap.Add(0x0018601A, new TagMetaData(VRType.UL, "Region Location Min Y0"));
            DataElementMap.Add(0x0018601C, new TagMetaData(VRType.UL, "Region Location Max X1"));
            DataElementMap.Add(0x0018601E, new TagMetaData(VRType.UL, "Region Location Max Y1"));
            DataElementMap.Add(0x00186020, new TagMetaData(VRType.SL, "Reference Pixel X0"));
            DataElementMap.Add(0x00186022, new TagMetaData(VRType.SL, "Reference Pixel Y0"));
            DataElementMap.Add(0x00186024, new TagMetaData(VRType.US, "Physical Units X Direction"));
            DataElementMap.Add(0x00186026, new TagMetaData(VRType.US, "Physical Units Y Direction"));
            DataElementMap.Add(0x00181628, new TagMetaData(VRType.FD, "Reference Pixel Physical Value X"));
            DataElementMap.Add(0x0018602A, new TagMetaData(VRType.FD, "Reference Pixel Physical Value Y"));
            DataElementMap.Add(0x0018602C, new TagMetaData(VRType.FD, "Physical Delta X"));
            DataElementMap.Add(0x0018602E, new TagMetaData(VRType.FD, "Physical Delta Y"));
            DataElementMap.Add(0x00186030, new TagMetaData(VRType.UL, "Transducer Frequency"));
            DataElementMap.Add(0x00186031, new TagMetaData(VRType.CS, "Transducer Type"));
            DataElementMap.Add(0x00186032, new TagMetaData(VRType.UL, "Pulse Repetition Frequency"));
            DataElementMap.Add(0x00186034, new TagMetaData(VRType.FD, "Doppler Correction Angle"));
            DataElementMap.Add(0x00186036, new TagMetaData(VRType.FD, "Sterring Angle"));
            DataElementMap.Add(0x00186038, new TagMetaData(VRType.UL, "Doppler Sample Volume X Position"));
            DataElementMap.Add(0x0018603A, new TagMetaData(VRType.UL, "Doppler Sample Volume Y Position"));
            DataElementMap.Add(0x0018603C, new TagMetaData(VRType.UL, "TM-Line Position X0"));
            DataElementMap.Add(0x0018603E, new TagMetaData(VRType.UL, "TM-Line Position Y0"));
            DataElementMap.Add(0x00186040, new TagMetaData(VRType.UL, "TM-Line Position X1"));
            DataElementMap.Add(0x00186042, new TagMetaData(VRType.UL, "TM-Line Position Y1"));
            DataElementMap.Add(0x00186044, new TagMetaData(VRType.US, "Pixel Component Organization"));
            DataElementMap.Add(0x00186046, new TagMetaData(VRType.UL, "Pixel Component Organization"));
            DataElementMap.Add(0x00186048, new TagMetaData(VRType.UL, "Pixel Component Range Start"));
            DataElementMap.Add(0x0018604A, new TagMetaData(VRType.UL, "Pixel Component Range Stop"));
            DataElementMap.Add(0x0018604C, new TagMetaData(VRType.US, "Pixel Component Physical Units"));
            DataElementMap.Add(0x0018604E, new TagMetaData(VRType.US, "Pixel Component Data Type"));
            DataElementMap.Add(0x00186050, new TagMetaData(VRType.UL, "Number of Table Break Points"));
            DataElementMap.Add(0x00186052, new TagMetaData(VRType.UL, "Table of X Break Points"));
            DataElementMap.Add(0x00186054, new TagMetaData(VRType.FD, "Table of Y Break Points"));
            DataElementMap.Add(0x00200000, new TagMetaData(VRType.UL, "Group 0020 Length"));
            DataElementMap.Add(0x0020000D, new TagMetaData(VRType.UI, "Study Instance UID"));
            DataElementMap.Add(0x0020000E, new TagMetaData(VRType.UI, "Series Instance UID"));
            DataElementMap.Add(0x00200010, new TagMetaData(VRType.SH, "Study ID"));
            DataElementMap.Add(0x00200011, new TagMetaData(VRType.IS, "Series Number"));
            DataElementMap.Add(0x00200012, new TagMetaData(VRType.IS, "Scquisition Number"));
            DataElementMap.Add(0x00200013, new TagMetaData(VRType.IS, "Image Number"));
            DataElementMap.Add(0x00200014, new TagMetaData(VRType.IS, "Isotope Number"));
            DataElementMap.Add(0x00200015, new TagMetaData(VRType.IS, "Phase Number"));
            DataElementMap.Add(0x00200016, new TagMetaData(VRType.IS, "Interval Number"));
            DataElementMap.Add(0x00200017, new TagMetaData(VRType.IS, "Time Slot Number"));
            DataElementMap.Add(0x00200018, new TagMetaData(VRType.IS, "Angle Number"));
            DataElementMap.Add(0x00200020, new TagMetaData(VRType.CS, "Patient Orientation"));
            DataElementMap.Add(0x00200022, new TagMetaData(VRType.US, "Overlay Number"));
            DataElementMap.Add(0x00200024, new TagMetaData(VRType.US, "Curve Number"));
            DataElementMap.Add(0x00200030, new TagMetaData(VRType.DS, "Image Position (RET)"));
            DataElementMap.Add(0x00200032, new TagMetaData(VRType.DS, "Image Position (Patient)"));
            DataElementMap.Add(0x00200035, new TagMetaData(VRType.DS, "Image Orientation (RET)"));
            DataElementMap.Add(0x00200037, new TagMetaData(VRType.DS, "Image Orientation (Patient)"));
            DataElementMap.Add(0x00200050, new TagMetaData(VRType.DS, "Location (RET)"));
            DataElementMap.Add(0x00200052, new TagMetaData(VRType.UI, "Frame of Reference UID"));
            DataElementMap.Add(0x00200060, new TagMetaData(VRType.CS, "Laterality"));
            DataElementMap.Add(0x00200070, new TagMetaData(VRType.SH, "Image Geometry Type (RET)"));
            DataElementMap.Add(0x00200080, new TagMetaData(VRType.UI, "Masking Image UID"));
            DataElementMap.Add(0x00200100, new TagMetaData(VRType.IS, "Temporal Position Identifier"));
            DataElementMap.Add(0x00200105, new TagMetaData(VRType.IS, "Number of Temporal Positions"));
            DataElementMap.Add(0x00200110, new TagMetaData(VRType.DS, "Temporal Resolution"));
            DataElementMap.Add(0x00201000, new TagMetaData(VRType.IS, "Series in Study"));
            DataElementMap.Add(0x00201001, new TagMetaData(VRType.IS, "Acquisitions in Series (RET)"));
            DataElementMap.Add(0x00201002, new TagMetaData(VRType.IS, "Images in Acquisition"));
            DataElementMap.Add(0x00201004, new TagMetaData(VRType.IS, "Acquisition in Study"));
            DataElementMap.Add(0x00201020, new TagMetaData(VRType.SH, "Reference (RET)"));
            DataElementMap.Add(0x00201040, new TagMetaData(VRType.LO, "Position Reference Indicator"));
            DataElementMap.Add(0x00201041, new TagMetaData(VRType.DS, "Slice Location"));
            DataElementMap.Add(0x00201070, new TagMetaData(VRType.IS, "Other Study Numbers"));
            DataElementMap.Add(0x00201200, new TagMetaData(VRType.IS, "Number of Patient Related Studies"));
            DataElementMap.Add(0x00201202, new TagMetaData(VRType.IS, "Number of Patient Related Series"));
            DataElementMap.Add(0x00201204, new TagMetaData(VRType.IS, "Number of Patient Related Images"));
            DataElementMap.Add(0x00201206, new TagMetaData(VRType.IS, "Number of Study Related Series"));
            DataElementMap.Add(0x00201208, new TagMetaData(VRType.IS, "Number of Study Related Images"));
            DataElementMap.Add(0x00203100, new TagMetaData(VRType.SH, "Source Image ID (RET)s"));
            DataElementMap.Add(0x00203401, new TagMetaData(VRType.SH, "Modifying Device ID (RET)"));
            DataElementMap.Add(0x00203402, new TagMetaData(VRType.SH, "Modified Image ID (RET)"));
            DataElementMap.Add(0x00203403, new TagMetaData(VRType.SH, "Modified Image Date (RET)"));
            DataElementMap.Add(0x00203404, new TagMetaData(VRType.SH, "Modifying Device Manufacturer (RET)"));
            DataElementMap.Add(0x00203405, new TagMetaData(VRType.SH, "Modified Image Time (RET)"));
            DataElementMap.Add(0x00203406, new TagMetaData(VRType.SH, "Modified Image Description (RET)"));
            DataElementMap.Add(0x00204000, new TagMetaData(VRType.LT, "Image Comments"));
            DataElementMap.Add(0x00205000, new TagMetaData(VRType.US, "Original Image Identification (RET)"));
            DataElementMap.Add(0x00205002, new TagMetaData(VRType.SH, "Original Image Identification Nomenclature (RET)"));
            DataElementMap.Add(0x00280000, new TagMetaData(VRType.UL, "Group 0028 Length"));
            DataElementMap.Add(0x00280002, new TagMetaData(VRType.US, "Samples per Pixel"));
            DataElementMap.Add(0x00280004, new TagMetaData(VRType.CS, "Photometric Interpretation"));
            DataElementMap.Add(0x00280005, new TagMetaData(VRType.US, "Image Dimensions (RET)"));
            DataElementMap.Add(0x00280006, new TagMetaData(VRType.US, "Planar Configuration"));
            DataElementMap.Add(0x00280008, new TagMetaData(VRType.IS, "Number of Frames"));
            DataElementMap.Add(0x00280009, new TagMetaData(VRType.AT, "Frame Increment Pointer"));
            DataElementMap.Add(0x00280010, new TagMetaData(VRType.US, "Rows"));
            DataElementMap.Add(0x00280011, new TagMetaData(VRType.US, "Columns"));
            DataElementMap.Add(0x00280030, new TagMetaData(VRType.DS, "Pixel Spacing"));
            DataElementMap.Add(0x00280031, new TagMetaData(VRType.DS, "Zoom Factor"));
            DataElementMap.Add(0x00280032, new TagMetaData(VRType.DS, "Zoom Center"));
            DataElementMap.Add(0x00280034, new TagMetaData(VRType.IS, "Pixel Aspect Ratio"));
            DataElementMap.Add(0x00280040, new TagMetaData(VRType.SH, "Image Format (RET)"));
            DataElementMap.Add(0x00280050, new TagMetaData(VRType.SH, "Manipulated Image (RET)"));
            DataElementMap.Add(0x00280051, new TagMetaData(VRType.CS, "Corrected Image"));
            DataElementMap.Add(0x00280060, new TagMetaData(VRType.SH, "Compression Code (RET)"));
            DataElementMap.Add(0x00280100, new TagMetaData(VRType.US, "Bits Allocated"));
            DataElementMap.Add(0x00280101, new TagMetaData(VRType.US, "Bits Stored"));
            DataElementMap.Add(0x00280102, new TagMetaData(VRType.US, "High Bit"));
            DataElementMap.Add(0x00280103, new TagMetaData(VRType.US, "Pixel Representation"));
            DataElementMap.Add(0x00280104, new TagMetaData(VRType.US, "Smallest Valid Pixel Value (RET)"));
            DataElementMap.Add(0x00280105, new TagMetaData(VRType.US, "Largest Valid Pixel Value (RET)"));
            DataElementMap.Add(0x00280106, new TagMetaData(VRType.US, "Smallest Image Pixel Value"));
            DataElementMap.Add(0x00280107, new TagMetaData(VRType.US, "Largest Image Pixel Value"));
            DataElementMap.Add(0x00280108, new TagMetaData(VRType.US, "Smallest Pixel Value in Series"));
            DataElementMap.Add(0x00280109, new TagMetaData(VRType.US, "Largest Pixel Value in Series"));
            DataElementMap.Add(0x00280120, new TagMetaData(VRType.US, "Pixel Padding Value"));
            DataElementMap.Add(0x00280200, new TagMetaData(VRType.US, "Image Location (RET)"));
            DataElementMap.Add(0x00281050, new TagMetaData(VRType.DS, "Window Center"));
            DataElementMap.Add(0x00281051, new TagMetaData(VRType.DS, "Window Width"));
            DataElementMap.Add(0x00281052, new TagMetaData(VRType.DS, "Rescale Intercept"));
            DataElementMap.Add(0x00281053, new TagMetaData(VRType.DS, "Rescale Slope"));
            DataElementMap.Add(0x00281054, new TagMetaData(VRType.LO, "Rescale Type"));
            DataElementMap.Add(0x00281055, new TagMetaData(VRType.LO, "Window Center & Width Explanation"));
            DataElementMap.Add(0x00281080, new TagMetaData(VRType.SH, "Gray Scale (RET)"));
            DataElementMap.Add(0x00281100, new TagMetaData(VRType.US, "Gray Lookup Table Descriptor (RET)"));
            DataElementMap.Add(0x00281101, new TagMetaData(VRType.US, "Red Palette Color Lookup Table Descriptor"));
            DataElementMap.Add(0x00281102, new TagMetaData(VRType.US, "Green Palette Color Lookup Table Descriptor"));
            DataElementMap.Add(0x00281103, new TagMetaData(VRType.US, "Blue Palette Color Lookup Table Descriptor"));
            DataElementMap.Add(0x00281200, new TagMetaData(VRType.US, "Gray Lookup Table Data (RET)"));
            DataElementMap.Add(0x00281201, new TagMetaData(VRType.US, "Red Palette Color Lookup Table Data"));
            DataElementMap.Add(0x00281202, new TagMetaData(VRType.US, "Green Palette Color Lookup Table Data"));
            DataElementMap.Add(0x00281203, new TagMetaData(VRType.US, "Blue Palette Color Lookup Table Data"));
            DataElementMap.Add(0x00283000, new TagMetaData(VRType.SQ, "Modality LUT Sequence"));
            DataElementMap.Add(0x00283002, new TagMetaData(VRType.US, "LUT Descriptor"));
            DataElementMap.Add(0x00283003, new TagMetaData(VRType.LO, "LUT Explanation"));
            DataElementMap.Add(0x00283004, new TagMetaData(VRType.LO, "Madality LUT Type"));
            DataElementMap.Add(0x00283006, new TagMetaData(VRType.US, "LUT Data"));
            DataElementMap.Add(0x00283010, new TagMetaData(VRType.SQ, "VOI LUT Sequence"));
            DataElementMap.Add(0x00284000, new TagMetaData(VRType.SH, "Group 0028 Comments (RET)"));
            DataElementMap.Add(0x00320000, new TagMetaData(VRType.UL, "Group 0032 Length"));
            DataElementMap.Add(0x0032000A, new TagMetaData(VRType.CS, "Study Status ID"));
            DataElementMap.Add(0x0032000C, new TagMetaData(VRType.CS, "Study Priority ID"));
            DataElementMap.Add(0x00320012, new TagMetaData(VRType.LO, "Study ID Issuer"));
            DataElementMap.Add(0x00320032, new TagMetaData(VRType.DA, "Study Verified Date"));
            DataElementMap.Add(0x00320033, new TagMetaData(VRType.TM, "Study Verified Time"));
            DataElementMap.Add(0x00320034, new TagMetaData(VRType.DA, "Study Read Date"));
            DataElementMap.Add(0x00320035, new TagMetaData(VRType.TM, "Study Read Time"));
            DataElementMap.Add(0x00321000, new TagMetaData(VRType.DA, "Scheduled Study Start Date"));
            DataElementMap.Add(0x00321001, new TagMetaData(VRType.TM, "Scheduled Study Start Time"));
            DataElementMap.Add(0x00321010, new TagMetaData(VRType.DA, "Scheduled Study Stop Date"));
            DataElementMap.Add(0x00321011, new TagMetaData(VRType.TM, "Scheduled Study Stop Time"));
            DataElementMap.Add(0x00321020, new TagMetaData(VRType.LO, "Scheduled Study Location"));
            DataElementMap.Add(0x00321021, new TagMetaData(VRType.AE, "Scheduled Study Location AE Title(s)"));
            DataElementMap.Add(0x00321030, new TagMetaData(VRType.LO, "Reason  for Study"));
            DataElementMap.Add(0x00321032, new TagMetaData(VRType.PN, "Requesting Physician"));
            DataElementMap.Add(0x00321033, new TagMetaData(VRType.LO, "Requesting Service"));
            DataElementMap.Add(0x00321040, new TagMetaData(VRType.DA, "Study Arrival Date"));
            DataElementMap.Add(0x00321041, new TagMetaData(VRType.TM, "Study Arrival Time"));
            DataElementMap.Add(0x00321050, new TagMetaData(VRType.DA, "Study Completion Date"));
            DataElementMap.Add(0x00321051, new TagMetaData(VRType.TM, "Study Completion Time"));
            DataElementMap.Add(0x00321055, new TagMetaData(VRType.CS, "Study Component Status ID"));
            DataElementMap.Add(0x00321060, new TagMetaData(VRType.LO, "Requested Procedure Description"));
            DataElementMap.Add(0x00321064, new TagMetaData(VRType.SQ, "Requested Procedure Code Sequence"));
            DataElementMap.Add(0x00321070, new TagMetaData(VRType.LO, "Requested Contrast Agent"));
            DataElementMap.Add(0x00324000, new TagMetaData(VRType.LT, "Study Comments"));
            DataElementMap.Add(0x00380000, new TagMetaData(VRType.UL, "Group 0038 Length"));
            DataElementMap.Add(0x00380004, new TagMetaData(VRType.SQ, "Referenced Patient Alias Sequence"));
            DataElementMap.Add(0x00380008, new TagMetaData(VRType.CS, "Visit Status ID"));
            DataElementMap.Add(0x00380010, new TagMetaData(VRType.LO, "Admissin ID"));
            DataElementMap.Add(0x00380011, new TagMetaData(VRType.LO, "Issuer of Admission ID"));
            DataElementMap.Add(0x00380016, new TagMetaData(VRType.LO, "Route of Admissions"));
            DataElementMap.Add(0x0038001A, new TagMetaData(VRType.DA, "Scheduled Admissin Date"));
            DataElementMap.Add(0x0038001B, new TagMetaData(VRType.TM, "Scheduled Adission Time"));
            DataElementMap.Add(0x0038001C, new TagMetaData(VRType.DA, "Scheduled Discharge Date"));
            DataElementMap.Add(0x0038001D, new TagMetaData(VRType.TM, "Scheduled Discharge Time"));
            DataElementMap.Add(0x0038001E, new TagMetaData(VRType.LO, "Scheduled Patient Institution Residence"));
            DataElementMap.Add(0x00380020, new TagMetaData(VRType.DA, "Admitting Date"));
            DataElementMap.Add(0x00380021, new TagMetaData(VRType.TM, "Admitting Time"));
            DataElementMap.Add(0x00380030, new TagMetaData(VRType.DA, "Discharge Date"));
            DataElementMap.Add(0x00380032, new TagMetaData(VRType.TM, "Discharge Time"));
            DataElementMap.Add(0x00380040, new TagMetaData(VRType.LO, "Discharge Diagnosis Description"));
            DataElementMap.Add(0x00380044, new TagMetaData(VRType.SQ, "Discharge Diagnosis Code Sequence"));
            DataElementMap.Add(0x00380050, new TagMetaData(VRType.LO, "Special Needs"));
            DataElementMap.Add(0x00380300, new TagMetaData(VRType.LO, "Current Patient Location"));
            DataElementMap.Add(0x00380400, new TagMetaData(VRType.LO, "Patient's Institution Residence"));
            DataElementMap.Add(0x00380500, new TagMetaData(VRType.LO, "Patient State"));
            DataElementMap.Add(0x00384000, new TagMetaData(VRType.LT, "Visit Comments"));
            DataElementMap.Add(0x00880000, new TagMetaData(VRType.UL, "Group 0088 Length"));
            DataElementMap.Add(0x00880130, new TagMetaData(VRType.SH, "Storage Media File-set ID"));
            DataElementMap.Add(0x00880140, new TagMetaData(VRType.UI, "Storage Media File-set UID"));
            DataElementMap.Add(0x20000000, new TagMetaData(VRType.UL, "Group 2000 Length"));
            DataElementMap.Add(0x20000010, new TagMetaData(VRType.IS, "Number of Copies"));
            DataElementMap.Add(0x20000020, new TagMetaData(VRType.CS, "Print Priority"));
            DataElementMap.Add(0x20000030, new TagMetaData(VRType.CS, "Medium Type"));
            DataElementMap.Add(0x20000040, new TagMetaData(VRType.CS, "Film Destination"));
            DataElementMap.Add(0x20000050, new TagMetaData(VRType.LO, "Film Session Label"));
            DataElementMap.Add(0x20000060, new TagMetaData(VRType.IS, "Memory Allocation"));
            DataElementMap.Add(0x20000500, new TagMetaData(VRType.SQ, "Referenced Film Box Sequence"));
            DataElementMap.Add(0x20100000, new TagMetaData(VRType.UL, "Group 2010 Length"));
            DataElementMap.Add(0x20100010, new TagMetaData(VRType.ST, "Smage Display Format"));
            DataElementMap.Add(0x20100030, new TagMetaData(VRType.CS, "Annotation Display Format ID"));
            DataElementMap.Add(0x20100040, new TagMetaData(VRType.CS, "Film Orientation"));
            DataElementMap.Add(0x20100050, new TagMetaData(VRType.CS, "Film Size ID"));
            DataElementMap.Add(0x20100060, new TagMetaData(VRType.CS, "Magnification Type"));
            DataElementMap.Add(0x20100080, new TagMetaData(VRType.CS, "Smoothing Type"));
            DataElementMap.Add(0x20100100, new TagMetaData(VRType.CS, "Border Density"));
            DataElementMap.Add(0x20100110, new TagMetaData(VRType.CS, "Empty Image Density"));
            DataElementMap.Add(0x20100120, new TagMetaData(VRType.US, "Min Density"));
            DataElementMap.Add(0x20100130, new TagMetaData(VRType.US, "Max Density"));
            DataElementMap.Add(0x20100140, new TagMetaData(VRType.CS, "Trim"));
            DataElementMap.Add(0x20100150, new TagMetaData(VRType.ST, "Configuration Information"));
            DataElementMap.Add(0x20100500, new TagMetaData(VRType.SQ, "Referenced Film Session Sequence"));
            DataElementMap.Add(0x20100510, new TagMetaData(VRType.SQ, "Referenced Basic Image Box Sequence"));
            DataElementMap.Add(0x20100520, new TagMetaData(VRType.SQ, "Referenced Basic Annotation Box Sequence"));
            DataElementMap.Add(0x20200000, new TagMetaData(VRType.UL, "Group 2020 Length"));
            DataElementMap.Add(0x20200010, new TagMetaData(VRType.US, "Image Position"));
            DataElementMap.Add(0x20200020, new TagMetaData(VRType.CS, "Polarity"));
            DataElementMap.Add(0x20200030, new TagMetaData(VRType.DS, "Requested Image Size"));
            DataElementMap.Add(0x20200110, new TagMetaData(VRType.SQ, "Preformatted Greyscale Image Sequence"));
            DataElementMap.Add(0x20200111, new TagMetaData(VRType.SQ, "Preformatted Color Image Sequence"));
            DataElementMap.Add(0x20200130, new TagMetaData(VRType.SQ, "Referenced Image Overlay Box Sequence"));
            DataElementMap.Add(0x20200140, new TagMetaData(VRType.SQ, "Referenced VOI LUT Sequence"));
            DataElementMap.Add(0x20300000, new TagMetaData(VRType.UL, "Group 2030 Length"));
            DataElementMap.Add(0x20300010, new TagMetaData(VRType.US, "Annotation Position"));
            DataElementMap.Add(0x20300020, new TagMetaData(VRType.LO, "Text Object"));
            DataElementMap.Add(0x20400000, new TagMetaData(VRType.UL, "Group 2040 Length"));
            DataElementMap.Add(0x20400010, new TagMetaData(VRType.SQ, "Referenced Overlay Plane Sequence"));
            DataElementMap.Add(0x20400011, new TagMetaData(VRType.US, "Refenced Overlay Plane Groups"));
            DataElementMap.Add(0x20400060, new TagMetaData(VRType.CS, "Overlay Magnification Type"));
            DataElementMap.Add(0x20400070, new TagMetaData(VRType.CS, "Overlay Smoothing Type"));
            DataElementMap.Add(0x20400080, new TagMetaData(VRType.CS, "Overlay Foreground Density"));
            DataElementMap.Add(0x20400090, new TagMetaData(VRType.CS, "overlay Mode"));
            DataElementMap.Add(0x20400100, new TagMetaData(VRType.CS, "Threshold Density"));
            DataElementMap.Add(0x20400500, new TagMetaData(VRType.SQ, "Referenced Image Box Sequence"));
            DataElementMap.Add(0x21000000, new TagMetaData(VRType.UL, "Group 2100 Length"));
            DataElementMap.Add(0x21000020, new TagMetaData(VRType.CS, "Execution Status"));
            DataElementMap.Add(0x21000030, new TagMetaData(VRType.CS, "Execution Status Info"));
            DataElementMap.Add(0x21000040, new TagMetaData(VRType.DA, "Creation Date"));
            DataElementMap.Add(0x21000050, new TagMetaData(VRType.TM, "Creation Time"));
            DataElementMap.Add(0x21000070, new TagMetaData(VRType.AE, "Originator"));
            DataElementMap.Add(0x21000500, new TagMetaData(VRType.SQ, "Referenced Print Job Sequence"));
            DataElementMap.Add(0x21100000, new TagMetaData(VRType.UL, "Group 2110 Length"));
            DataElementMap.Add(0x21100010, new TagMetaData(VRType.CS, "Printer Status"));
            DataElementMap.Add(0x21100020, new TagMetaData(VRType.CS, "Printer Status Info"));
            DataElementMap.Add(0x21100030, new TagMetaData(VRType.ST, "Printer Name"));
            DataElementMap.Add(0x40000000, new TagMetaData(VRType.UL, "Group 4000 Length (RET)"));
            DataElementMap.Add(0x40000010, new TagMetaData(VRType.SH, "Arbitray (RET)"));
            DataElementMap.Add(0x40004000, new TagMetaData(VRType.LT, "Group 4000 Comments (RET)"));
            DataElementMap.Add(0x40080000, new TagMetaData(VRType.UL, "Group 4008 Length"));
            DataElementMap.Add(0x40080040, new TagMetaData(VRType.SH, "Results ID"));
            DataElementMap.Add(0x40080042, new TagMetaData(VRType.LO, "Results ID Issuer"));
            DataElementMap.Add(0x40080050, new TagMetaData(VRType.SQ, "Referenced Interpretation Sequence"));
            DataElementMap.Add(0x40080100, new TagMetaData(VRType.DA, "Interpretation Recorded Date"));
            DataElementMap.Add(0x40080101, new TagMetaData(VRType.TM, "Interpretation Recorded Time"));
            DataElementMap.Add(0x40080102, new TagMetaData(VRType.PN, "Interpretation Recorder"));
            DataElementMap.Add(0x40080103, new TagMetaData(VRType.LO, "Reference to Recorded Sound"));
            DataElementMap.Add(0x40080108, new TagMetaData(VRType.DA, "Interpretation Transcription Time"));
            DataElementMap.Add(0x40080109, new TagMetaData(VRType.TM, "Interpretation Transcription Time"));
            DataElementMap.Add(0x4008010A, new TagMetaData(VRType.PN, "Interpretation Transcriber"));
            DataElementMap.Add(0x4008010B, new TagMetaData(VRType.ST, "Interpretation Text"));
            DataElementMap.Add(0x4008010C, new TagMetaData(VRType.PN, "Interpretation Author"));
            DataElementMap.Add(0x40080111, new TagMetaData(VRType.SQ, "Interpretation Approver Sequence"));
            DataElementMap.Add(0x40080112, new TagMetaData(VRType.DA, "Interpretation Approval Date"));
            DataElementMap.Add(0x40080113, new TagMetaData(VRType.TM, "Interpretation Approval Time"));
            DataElementMap.Add(0x40080114, new TagMetaData(VRType.PN, "Physician Approving Interpretation"));
            DataElementMap.Add(0x40080115, new TagMetaData(VRType.LT, "Interpretation Diagnosis Description"));
            DataElementMap.Add(0x40080117, new TagMetaData(VRType.SQ, "Diagnosis Code Sequence"));
            DataElementMap.Add(0x40080118, new TagMetaData(VRType.SQ, "Results Distribution List Sequence"));
            DataElementMap.Add(0x40080119, new TagMetaData(VRType.PN, "Distribution Name"));
            DataElementMap.Add(0x4008011A, new TagMetaData(VRType.LO, "Distribution Address"));
            DataElementMap.Add(0x40080200, new TagMetaData(VRType.SH, "Interpretation ID"));
            DataElementMap.Add(0x40080202, new TagMetaData(VRType.LO, "Interpretation ID Issuer"));
            DataElementMap.Add(0x40080210, new TagMetaData(VRType.CS, "Interpretation Type ID"));
            DataElementMap.Add(0x40080212, new TagMetaData(VRType.CS, "Interpretation Status ID"));
            DataElementMap.Add(0x40080300, new TagMetaData(VRType.ST, "Impression"));
            DataElementMap.Add(0x40084000, new TagMetaData(VRType.SH, "Group 4008 Comments"));
            DataElementMap.Add(0x50000000, new TagMetaData(VRType.UL, "Group 5000 Length"));
            DataElementMap.Add(0x50000005, new TagMetaData(VRType.US, "Curve Dimensions"));
            DataElementMap.Add(0x50000010, new TagMetaData(VRType.US, "Number of Points"));
            DataElementMap.Add(0x50000020, new TagMetaData(VRType.CS, "Type of Data"));
            DataElementMap.Add(0x50000022, new TagMetaData(VRType.LO, "Curve Description"));
            DataElementMap.Add(0x50000030, new TagMetaData(VRType.SH, "Axis Units"));
            DataElementMap.Add(0x50000040, new TagMetaData(VRType.SH, "Axis Labels"));
            DataElementMap.Add(0x50000103, new TagMetaData(VRType.US, "Data Value Representation"));
            DataElementMap.Add(0x50000104, new TagMetaData(VRType.US, "Minimum Coordinate Value"));
            DataElementMap.Add(0x50000105, new TagMetaData(VRType.US, "Maximum Coordinate Value"));
            DataElementMap.Add(0x50000106, new TagMetaData(VRType.SH, "Curve Range"));
            DataElementMap.Add(0x50000110, new TagMetaData(VRType.US, "Curve Data Descriptor"));
            DataElementMap.Add(0x50000112, new TagMetaData(VRType.US, "Coordinate Start Value"));
            DataElementMap.Add(0x50000114, new TagMetaData(VRType.US, "Coordinate Step Value"));
            DataElementMap.Add(0x50002000, new TagMetaData(VRType.US, "Audio Type"));
            DataElementMap.Add(0x50002002, new TagMetaData(VRType.US, "Audio Sample Format"));
            DataElementMap.Add(0x50002004, new TagMetaData(VRType.US, "Number of Channels"));
            DataElementMap.Add(0x50002006, new TagMetaData(VRType.UL, "Number of Samples"));
            DataElementMap.Add(0x50002008, new TagMetaData(VRType.UL, "Sample Rate"));
            DataElementMap.Add(0x5000200A, new TagMetaData(VRType.UL, "Total Time"));
            DataElementMap.Add(0x5000200C, new TagMetaData(VRType.OX, "Audio Sample Data"));
            DataElementMap.Add(0x5000200E, new TagMetaData(VRType.LT, "Audio Comments"));
            DataElementMap.Add(0x50003000, new TagMetaData(VRType.OX, "Curve Data"));
            DataElementMap.Add(0x60000000, new TagMetaData(VRType.UL, "Group 6000 Length"));
            DataElementMap.Add(0x60000010, new TagMetaData(VRType.US, "Rows"));
            DataElementMap.Add(0x60000011, new TagMetaData(VRType.US, "Columns"));
            DataElementMap.Add(0x60000015, new TagMetaData(VRType.IS, "Number of Frames in Overlay"));
            DataElementMap.Add(0x60000040, new TagMetaData(VRType.CS, "Overlay Type"));
            DataElementMap.Add(0x60000050, new TagMetaData(VRType.SS, "Origin"));
            DataElementMap.Add(0x60000060, new TagMetaData(VRType.SH, "Compression Code (RET)"));
            DataElementMap.Add(0x60000100, new TagMetaData(VRType.US, "Bits Allocated"));
            DataElementMap.Add(0x60000102, new TagMetaData(VRType.US, "Bit Position"));
            DataElementMap.Add(0x60000110, new TagMetaData(VRType.SH, "Overlay Format (RET)"));
            DataElementMap.Add(0x60000200, new TagMetaData(VRType.US, "Overlay Location (RET)"));
            DataElementMap.Add(0x60001100, new TagMetaData(VRType.US, "Overlay Descriptor - Gray"));
            DataElementMap.Add(0x60001101, new TagMetaData(VRType.US, "Overlay Descriptor - Red"));
            DataElementMap.Add(0x60001102, new TagMetaData(VRType.US, "Overlay Descriptor - Green"));
            DataElementMap.Add(0x60001103, new TagMetaData(VRType.US, "Overlay Descriptor - Blue"));
            DataElementMap.Add(0x60001200, new TagMetaData(VRType.US, "Overlays - Gray"));
            DataElementMap.Add(0x60001201, new TagMetaData(VRType.US, "Overlays - Red"));
            DataElementMap.Add(0x60001202, new TagMetaData(VRType.US, "Overlays - Green"));
            DataElementMap.Add(0x60001203, new TagMetaData(VRType.US, "Overlays - Blue"));
            DataElementMap.Add(0x60001301, new TagMetaData(VRType.IS, "ROI Area"));
            DataElementMap.Add(0x60001302, new TagMetaData(VRType.DS, "ROI Mean"));
            DataElementMap.Add(0x60001303, new TagMetaData(VRType.DS, "ROI Standard Deviation"));
            DataElementMap.Add(0x60003000, new TagMetaData(VRType.OW, "Overlay Data"));
            DataElementMap.Add(0x60004000, new TagMetaData(VRType.SH, "Group 6000 Comments (RET)"));
            DataElementMap.Add(0x7FE00000, new TagMetaData(VRType.UL, "Group 7FE0 Length"));
            DataElementMap.Add(0x7FE00010, new TagMetaData(VRType.OX, "Pixel Data"));
            DataElementMap.Add(0xFFFEE000, new TagMetaData(VRType.DL, "Item"));
            DataElementMap.Add(0xFFFEE00D, new TagMetaData(VRType.DL, "Item Delimitation Item"));
            DataElementMap.Add(0xFFFEE0DD, new TagMetaData(VRType.DL, "Sequence Delimitation Item"));
        }

        /// <summary>
        /// Returns the value range (VR) of a given tag.
        /// </summary>
        /// <param name="tag">the tag id (group number + element number)</param>
        /// <returns> the value range</returns>
        public VRType getVR(uint tag)
        {
            TagMetaData meta_data;
            try
            {
                meta_data = DataElementMap[tag];
            }
            catch (KeyNotFoundException e)
            {
                Debug.Log(Convert.ToString(tag, 16));
                return VRType.XX;
            }

            VRType result;

            if (meta_data != null)
            {
                result = meta_data.vr;
            }
            else
            {
                result = VRType.XX;
            }

            return result;
        }

        /// <summary>
        /// Returns a string representation of the value range (VR) of a given tag.
        /// </summary>
        /// <param name="tag">the tag id (group number + element number)</param>
        /// <returns>a string representing the value range</returns>
        public string getVRstr(uint tag)
        {
            TagMetaData meta_data = DataElementMap[tag];
            string result;

            if (meta_data != null)
            {
                result = "" + meta_data.vr;
            }
            else
            {
                result = "--";
            }

            return result;
        }

        /// <summary>
        /// Returns a description of the given tag (window width, patient birth, etc.).
        /// </summary>
        /// <param name="tag">the tag id (group number + element number)</param>
        /// <returns> a string containing a description of the tag</returns>
        public string GetTagDescription(uint tag)
        {
            TagMetaData meta_data = DataElementMap[tag];
            string result;

            if (meta_data != null)
            {
                result = "" + meta_data.descr;
            }
            else
            {
                result = "unknown";
            }

            return result;
        }

        public string GetSopMediaDescr(string id)
        {
            return MediaStorageMap[id];
        }

        /// <summary>
        /// Returns the Tag representing the given group and element id combination
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="elementId"></param>
        /// <returns></returns>
        public static uint ToTag(uint groupId, uint elementId)
        {
            return (groupId << 16) | elementId;
        }
    }
}

