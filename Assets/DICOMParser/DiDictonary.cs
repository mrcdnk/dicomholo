using System;
using System.Collections;
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
        private static Dictionary<uint, TagMetaData> _dataElementMap = new Dictionary<uint, TagMetaData>();
        private static Dictionary<string, string> _mediaStorageMap = new Dictionary<string, string>();
        private static Dictionary<string, TSUIDMetaData> _tuidStorageMap = new Dictionary<string, TSUIDMetaData>();

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


        /**
         * Private class for storing the tag meta data in the DiDi HashTable.
         * It contains only the value range and its description of a given
         * tag, the tag id itself is stored in the global DiDi HashTable.
         * @author kif
         *
         */
        private class TagMetaData
        {
            public VRType vr;
            public string descr;

            /**
             * The one and only constructor for this class.
             * 
             * @param vr    the VR identifier
             * @param descr the VR description
             */
            public TagMetaData(VRType vr, string descr)
            {
                this.vr = vr;
                this.descr = descr;
            }
        }

        private void tsuid_register(TSUIDMetaData ts_uid_meta)
        {
            _tuidStorageMap[ts_uid_meta.Uid] =  ts_uid_meta;
        }

        /**
     * Returns the endianess (little or big) based on the transfer syntax UID
     *  
     * @param ts_uid transfer syntax UID
     * @return DiFile.EndianLittle, DiFile.EndianBig or DiFile.DiFile.ENDIAN_UNKNOWN
     */
        public static int get_ts_uid_endianess(string ts_uid)
        {
            int result = DiFile.EndianUnknown;

            TSUIDMetaData tsUidMeta = _tuidStorageMap[ts_uid];
            if (tsUidMeta != null)
            {
                result = tsUidMeta.Endianess;
            }

            return result;
        }

        /**
         * Returns the VR format (explicit or implicit) based on the transfer syntax UID
         * 
         * @param ts_uid transfer syntax UID
         * @return DiFile.VrExplicit, DiFile.VR_IMPLICIT or DiFile.VR_UNKOWN
         */
        public static int get_ts_uid_vr_format(string ts_uid)
        {
            int result = DiFile.VrUnknown;

            TSUIDMetaData tsUidMeta = _tuidStorageMap[ts_uid];
            if (tsUidMeta != null)
            {
                result = tsUidMeta.VrFormat;
            }

            return result;
        }

        /**
         * Returns a textual description of the given transfer syntax UID as defined by DICOM
         * 
         * @param ts_uid transfer syntax UID
         * @return textual description as defined by DICOM
         */
        public static string get_ts_uid_descr(string ts_uid)
        {
            string result = "unknown";

            TSUIDMetaData tsUidMeta = _tuidStorageMap[ts_uid];
            if (tsUidMeta != null)
            {
                result = tsUidMeta.Descr;
            }

            return result;
        }

        /**
         * Returns the additional info of the given transfer syntax UID as defined by DICOM
         * @param ts_uid transfer syntax UID
         * @return additional info as written as defined by DICOM
         */
        public static string get_ts_uid_additional_info(string ts_uid)
        {
            string result = "unknown";

            TSUIDMetaData tsUidMeta = _tuidStorageMap[ts_uid];
            if (tsUidMeta != null)
            {
                result = tsUidMeta.AdditionalInfo;
            }

            return result;
        }

        /**
         * Returns true if the TS UID is not supported by the latest DICOM version anymore
         * 
         * @param ts_uid transfer syntax UID
         * @return true if the transfer syntax UID is retired, false if not
         */
        public static bool get_ts_uid_retired(string ts_uid)
        {
            bool result = false;

            TSUIDMetaData tsUidMeta = _tuidStorageMap[ts_uid];
            if (tsUidMeta != null)
            {
                result = tsUidMeta.Retired;
            }

            return result;
        }

        /**
         * Returns true if the transfer syntax UID is part of DICOM
         * @param ts_uid transfer syntax UID
         * @return true if it is a known UID
         */
        public static bool is_ts_uid_known(string ts_uid)
        {
            return _tuidStorageMap.ContainsKey(ts_uid);
        }

        /**
         * The default constructor is private (singleton).
         *
         */
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

            _mediaStorageMap.Add("1.2.840.10008.1.3.10", "Media Storage Directory Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.1", "ComAdded Radiography Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.1.1", "Digital X-Ray Image Storage For Presentation");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.1.1.1", "Digital X-Ray Image Storage For Processing");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.1.2", "Digital Mammography Image Storage For Presentation");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.1.2.1", "Digital Mammography Image Storage For Processing");

            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.1.3", "Digital Intra-oral X-Ray Image Storage For Presentation");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.1.3.1", "Digital Intra-oral X-Ray Image Storage For Processing");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.2", "CT Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.2.1", "Enhanced CT Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.3.1", "Ultrasound Multi-frame Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.4", "MR Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.4.1", "Enhanced MR Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.4.2", "MR Spectroscopy Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.6.1", "Ultrasound Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.7", "Secondary Capture Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.7.1", "Multi-frame Single Bit Secondary Capture Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.7.2", "Secondary Capture Image Multi-frame Grayscale Byte Secondary Capture Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.7.3", "Multi-frame Grayscale Word Secondary Capture Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.7.4", "Multi-frame True Color Secondary Capture Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.9.1.1", "12-lead ECG Waveform Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.9.1.2", "General ECG Waveform Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.9.1.3", "Ambulatory ECG Waveform Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.9.2.1", "Hemodynamic Waveform Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.9.3.1", "Cardiac Electrophysiology Waveform Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.9.4.1", "Basic Voice Audio Waveform Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.11.1", "Grayscale Softcopy Presentation State Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.11.2", "Color Softcopy Presentation State Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.11.3", "Presentation State Storage");

            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.11.4", "Blending Softcopy Presentation State Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.12.1", "X-Ray Angiographic Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.12.1.1", "Enhanced XA Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.12.2", "X-Ray Radiofluoroscopic Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.12.2.1", "Enhanced XRF Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.13.1.1", "X-Ray 3D Angiographic Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.13.1.2", "X-Ray 3D Craniofacial Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.20", "Nuclear Medicine Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.66", "Raw Data Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.66.1", "Spatial Registration Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.66.2", "Spatial Fiducials Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.66.3", "Deformable Spatial Registration Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.66.4", "Segmentation Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.67", "Real World Value Mapping Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.1", "VL Endoscopic Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.1.1", "Video Endoscopic Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.2", "VL Microscopic Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.2.1", "Video Microscopic Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.3", "VL Slide-Coordinates Microscopic Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.4", "VL Photographic Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.4.1", "Video Photographic Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.5.1", "Ophthalmic Photography 8 Bit Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.5.2", "Ophthalmic Photography 16 Bit Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.5.3", "Stereometric Relationship Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.77.1.5.4", "Ophthalmic Tomography Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.88.11", "Basic Text SR Enhanced SR");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.88.22", "Basic Text SR");

            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.88.33", "Comprehensive SR");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.88.40", "Procedure Log");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.88.50", "Mammography CAD SR");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.88.59", "Key Object Selection Document");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.88.65", "Chest CAD SR");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.88.67", "X-Ray Radiation Dose SR");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.104.1", "Encapsulated PDF Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.128", "Positron Emission Tomography Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.481.1", "RT Image Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.481.2", "RT Dose Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.481.3", "RT Structure Set Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.481.4", "RT Beams Treatment Record Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.481.5", "RT Plan Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.481.6", "RT Brachy Treatment Record Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.481.7", "RT Treatment Summary Record Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.481.8", "RT Ion Plan Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.1.1.481.9", "RT Ion Beams Treatment Record Storage");
            _mediaStorageMap.Add("1.2.840.10008.5.1.4.38.1", "Hanging Protocol Storage");
            
            _dataElementMap.Add(0x00000000, new TagMetaData(VRType.UL, "Group 0000 Length"));
            _dataElementMap.Add(0x00000001, new TagMetaData(VRType.UL, "Group 0000 Length to End (RET)"));
            _dataElementMap.Add(0x00000002, new TagMetaData(VRType.UI, "Affected SOP Class UID"));
            _dataElementMap.Add(0x00000003, new TagMetaData(VRType.UI, "Requested SOP Class UID"));
            _dataElementMap.Add(0x00000010, new TagMetaData(VRType.SH, "Recognition Code (RET)"));
            _dataElementMap.Add(0x00000100, new TagMetaData(VRType.US, "Command Field"));
            _dataElementMap.Add(0x00000110, new TagMetaData(VRType.US, "Message ID"));
            _dataElementMap.Add(0x00000120, new TagMetaData(VRType.US, "Message Id being Responded to"));
            _dataElementMap.Add(0x00000200, new TagMetaData(VRType.AE, "Initiator (RET)"));
            _dataElementMap.Add(0x00000300, new TagMetaData(VRType.AE, "Receiver (RET)"));
            _dataElementMap.Add(0x00000400, new TagMetaData(VRType.AE, "Find Location (RET)"));
            _dataElementMap.Add(0x00000600, new TagMetaData(VRType.AE, "Move Destination"));
            _dataElementMap.Add(0x00000700, new TagMetaData(VRType.US, "Priority"));
            _dataElementMap.Add(0x00000800, new TagMetaData(VRType.US, "Data Set Type"));
            _dataElementMap.Add(0x00000850, new TagMetaData(VRType.US, "Number of Matches (RET)"));
            _dataElementMap.Add(0x00000860, new TagMetaData(VRType.US, "Response Sequence Number (RET)"));
            _dataElementMap.Add(0x00000900, new TagMetaData(VRType.US, "Status"));
            _dataElementMap.Add(0x00000901, new TagMetaData(VRType.AT, "Offending Element"));
            _dataElementMap.Add(0x00000902, new TagMetaData(VRType.LO, "Error Comment"));
            _dataElementMap.Add(0x00000903, new TagMetaData(VRType.US, "Error ID"));
            _dataElementMap.Add(0x00001000, new TagMetaData(VRType.UI, "Affected SOP Instance UID"));
            _dataElementMap.Add(0x00001001, new TagMetaData(VRType.UI, "Requested SOP Instance UID"));
            _dataElementMap.Add(0x00001002, new TagMetaData(VRType.US, "Event Type ID"));
            _dataElementMap.Add(0x00001005, new TagMetaData(VRType.AT, "Attribute Identifier List"));
            _dataElementMap.Add(0x00001008, new TagMetaData(VRType.US, "Action Type ID"));
            _dataElementMap.Add(0x00001012, new TagMetaData(VRType.UI, "Requested SOP Instance UID List"));
            _dataElementMap.Add(0x00001020, new TagMetaData(VRType.US, "Number of Remaining Sub-operations"));
            _dataElementMap.Add(0x00001021, new TagMetaData(VRType.US, "Number of Completed Sub-operations"));
            _dataElementMap.Add(0x00001022, new TagMetaData(VRType.US, "Number of Failed Sub-operations"));
            _dataElementMap.Add(0x00001023, new TagMetaData(VRType.US, "Number of Warning Sub-operations"));
            _dataElementMap.Add(0x00001030, new TagMetaData(VRType.AE, "Move Originator Application Entity Title"));
            _dataElementMap.Add(0x00001031, new TagMetaData(VRType.US, "Move Originator Message ID"));
            _dataElementMap.Add(0x00005010, new TagMetaData(VRType.LO, "Message Set ID (RET)"));
            _dataElementMap.Add(0x00005020, new TagMetaData(VRType.LO, "End Message Set ID (RET)"));
            _dataElementMap.Add(0x00020000, new TagMetaData(VRType.UL, "Group 0002 Length"));
            _dataElementMap.Add(0x00020001, new TagMetaData(VRType.OB, "File Meta Information Version"));
            _dataElementMap.Add(0x00020002, new TagMetaData(VRType.UI, "Media Stored SOP Class UID"));
            _dataElementMap.Add(0x00020003, new TagMetaData(VRType.UI, "Media Stored SOP Instance UID"));
            _dataElementMap.Add(0x00020010, new TagMetaData(VRType.UI, "Transfer Syntax UID"));
            _dataElementMap.Add(0x00020012, new TagMetaData(VRType.UI, "Implementation Class UID"));
            _dataElementMap.Add(0x00020013, new TagMetaData(VRType.SH, "Implementation Version Name"));
            _dataElementMap.Add(0x00020016, new TagMetaData(VRType.AE, "Source Application Entity Title"));
            _dataElementMap.Add(0x00020100, new TagMetaData(VRType.UI, "Private Information Creator UID"));
            _dataElementMap.Add(0x00020102, new TagMetaData(VRType.OB, "Private Information"));
            _dataElementMap.Add(0x00040000, new TagMetaData(VRType.UL, "Group 0004 Length"));
            _dataElementMap.Add(0x00041130, new TagMetaData(VRType.CS, "File-set ID"));
            _dataElementMap.Add(0x00041141, new TagMetaData(VRType.CS, "File-set Descriptor File File ID"));
            _dataElementMap.Add(0x00041142, new TagMetaData(VRType.CS, "File-set Descriptor File Format"));
            _dataElementMap.Add(0x00041200, new TagMetaData(VRType.UL, "Root Directory Entity's First Directory Record Offset"));
            _dataElementMap.Add(0x00041202, new TagMetaData(VRType.UL, "Root Directory Entity's Last Directory Record Offset"));
            _dataElementMap.Add(0x00041212, new TagMetaData(VRType.US, "File-set Consistence Flag"));
            _dataElementMap.Add(0x00041220, new TagMetaData(VRType.SQ, "Directory Record Sequence"));
            _dataElementMap.Add(0x00041400, new TagMetaData(VRType.UL, "Next Directory Record Offset"));
            _dataElementMap.Add(0x00041410, new TagMetaData(VRType.US, "Record In-use Flag"));
            _dataElementMap.Add(0x00041420, new TagMetaData(VRType.UL, "Referenced Lower-level Directory Entity Offset"));
            _dataElementMap.Add(0x00041430, new TagMetaData(VRType.CS, "Directory Record Type"));
            _dataElementMap.Add(0x00041432, new TagMetaData(VRType.UI, "Private Record UID"));
            _dataElementMap.Add(0x00041500, new TagMetaData(VRType.CS, "Referenced File ID"));
            _dataElementMap.Add(0x00041510, new TagMetaData(VRType.UI, "Referenced SOP Class UID in File"));
            _dataElementMap.Add(0x00041511, new TagMetaData(VRType.UI, "Referenced SOP Instance UID in File"));
            _dataElementMap.Add(0x00041600, new TagMetaData(VRType.UL, "Number of References"));
            _dataElementMap.Add(0x00080000, new TagMetaData(VRType.UL, "Group 0008 Length"));
            _dataElementMap.Add(0x00080001, new TagMetaData(VRType.UL, "Group 0008 Length to End (RET)"));
            _dataElementMap.Add(0x00080005, new TagMetaData(VRType.CS, "Specific Character Set"));
            _dataElementMap.Add(0x00080008, new TagMetaData(VRType.CS, "Image Type"));
            _dataElementMap.Add(0x00080010, new TagMetaData(VRType.SH, "Recognition Code (RET)"));
            _dataElementMap.Add(0x00080012, new TagMetaData(VRType.DA, "Instance Creation Date"));
            _dataElementMap.Add(0x00080013, new TagMetaData(VRType.TM, "Instance Creation Time"));
            _dataElementMap.Add(0x00080014, new TagMetaData(VRType.UI, "Instance Creator UID"));
            _dataElementMap.Add(0x00080016, new TagMetaData(VRType.UI, "SOP Class UID"));
            _dataElementMap.Add(0x00080018, new TagMetaData(VRType.UI, "SOP Instance UID"));
            _dataElementMap.Add(0x00080020, new TagMetaData(VRType.DA, "Study Date"));
            _dataElementMap.Add(0x00080021, new TagMetaData(VRType.DA, "Series Date"));
            _dataElementMap.Add(0x00080022, new TagMetaData(VRType.DA, "Acquisition Date"));
            _dataElementMap.Add(0x00080023, new TagMetaData(VRType.DA, "Image Date"));
            _dataElementMap.Add(0x00080024, new TagMetaData(VRType.DA, "Overlay Date"));
            _dataElementMap.Add(0x00080025, new TagMetaData(VRType.DA, "Curve Date"));
            _dataElementMap.Add(0x00080030, new TagMetaData(VRType.TM, "Study Time"));
            _dataElementMap.Add(0x00080031, new TagMetaData(VRType.TM, "Series Time"));
            _dataElementMap.Add(0x00080032, new TagMetaData(VRType.TM, "Acquisition Time"));
            _dataElementMap.Add(0x00080033, new TagMetaData(VRType.TM, "Image Time"));
            _dataElementMap.Add(0x00080034, new TagMetaData(VRType.TM, "Overlay Time"));
            _dataElementMap.Add(0x00080035, new TagMetaData(VRType.TM, "Curve Time"));
            _dataElementMap.Add(0x00080040, new TagMetaData(VRType.US, "Data Set Type (RET)"));
            _dataElementMap.Add(0x00080041, new TagMetaData(VRType.SH, "Data Set Subtype (RET)"));
            _dataElementMap.Add(0x00080042, new TagMetaData(VRType.CS, "Nuclear Medicine Series Type"));
            _dataElementMap.Add(0x00080050, new TagMetaData(VRType.SH, "Accession Number"));
            _dataElementMap.Add(0x00080052, new TagMetaData(VRType.CS, "Query/Retrieve Level"));
            _dataElementMap.Add(0x00080054, new TagMetaData(VRType.AE, "Retrieve AE Title"));
            _dataElementMap.Add(0x00080058, new TagMetaData(VRType.AE, "Failed SOP Instance UID List"));
            _dataElementMap.Add(0x00080060, new TagMetaData(VRType.CS, "Modality"));
            _dataElementMap.Add(0x00080064, new TagMetaData(VRType.CS, "Conversion Type"));
            _dataElementMap.Add(0x00080070, new TagMetaData(VRType.LO, "Manufacturer"));
            _dataElementMap.Add(0x00080080, new TagMetaData(VRType.LO, "Institution Name"));
            _dataElementMap.Add(0x00080081, new TagMetaData(VRType.ST, "Institution Address"));
            _dataElementMap.Add(0x00080082, new TagMetaData(VRType.SQ, "Institution Code Sequence"));
            _dataElementMap.Add(0x00080090, new TagMetaData(VRType.PN, "Referring Physician's Name"));
            _dataElementMap.Add(0x00080092, new TagMetaData(VRType.ST, "Referring Physician's Address"));
            _dataElementMap.Add(0x00080094, new TagMetaData(VRType.SH, "Referring Physician's Telephone Numbers"));
            _dataElementMap.Add(0x00080100, new TagMetaData(VRType.SH, "Code Value"));
            _dataElementMap.Add(0x00080102, new TagMetaData(VRType.SH, "Coding Scheme Designator"));
            _dataElementMap.Add(0x00080104, new TagMetaData(VRType.LO, "Code Meaning"));
            _dataElementMap.Add(0x00081000, new TagMetaData(VRType.SH, "Network ID (RET)"));
            _dataElementMap.Add(0x00081010, new TagMetaData(VRType.SH, "Station Name"));
            _dataElementMap.Add(0x00081030, new TagMetaData(VRType.LO, "Study Description"));
            _dataElementMap.Add(0x00081032, new TagMetaData(VRType.SQ, "Procedure Code Sequence"));
            _dataElementMap.Add(0x0008103E, new TagMetaData(VRType.LO, "Series Description"));
            _dataElementMap.Add(0x00081040, new TagMetaData(VRType.LO, "Institutional Department Name"));
            _dataElementMap.Add(0x00081050, new TagMetaData(VRType.PN, "Attending Physician's Name"));
            _dataElementMap.Add(0x00081060, new TagMetaData(VRType.PN, "Name of Physician(s) Reading Study"));
            _dataElementMap.Add(0x00081070, new TagMetaData(VRType.PN, "Operator's Name"));
            _dataElementMap.Add(0x00081080, new TagMetaData(VRType.LO, "Admitting Diagnoses Description"));
            _dataElementMap.Add(0x00081084, new TagMetaData(VRType.SQ, "Admitting Diagnosis Code Sequence"));
            _dataElementMap.Add(0x00081090, new TagMetaData(VRType.LO, "Manufacturer's Model Name"));
            _dataElementMap.Add(0x00081100, new TagMetaData(VRType.SQ, "Referenced Results Sequence"));
            _dataElementMap.Add(0x00081110, new TagMetaData(VRType.SQ, "Referenced Study Sequence"));
            _dataElementMap.Add(0x00081111, new TagMetaData(VRType.SQ, "Referenced Study Component Sequence"));
            _dataElementMap.Add(0x00081115, new TagMetaData(VRType.SQ, "Referenced Series Sequence"));
            _dataElementMap.Add(0x00081120, new TagMetaData(VRType.SQ, "Referenced Patient Sequence"));
            _dataElementMap.Add(0x00081125, new TagMetaData(VRType.SQ, "Referenced Visit Sequence"));
            _dataElementMap.Add(0x00081130, new TagMetaData(VRType.SQ, "Referenced Overlay Sequence"));
            _dataElementMap.Add(0x00081140, new TagMetaData(VRType.SQ, "Referenced Image Sequence"));
            _dataElementMap.Add(0x00081145, new TagMetaData(VRType.SQ, "Referenced Curve Sequence"));
            _dataElementMap.Add(0x00081150, new TagMetaData(VRType.UI, "Referenced SOP Class UID"));
            _dataElementMap.Add(0x00081155, new TagMetaData(VRType.UI, "Referenced SOP Instance UID"));
            _dataElementMap.Add(0x00082111, new TagMetaData(VRType.ST, "Derivation Description"));
            _dataElementMap.Add(0x00082112, new TagMetaData(VRType.SQ, "Source Image Sequence"));
            _dataElementMap.Add(0x00082120, new TagMetaData(VRType.SH, "Stage Name"));
            _dataElementMap.Add(0x00082122, new TagMetaData(VRType.IS, "Stage Number"));
            _dataElementMap.Add(0x00082124, new TagMetaData(VRType.IS, "Number of Stages"));
            _dataElementMap.Add(0x00082129, new TagMetaData(VRType.IS, "Number of Event Timers"));
            _dataElementMap.Add(0x00082128, new TagMetaData(VRType.IS, "View Number"));
            _dataElementMap.Add(0x0008212A, new TagMetaData(VRType.IS, "Number of Views in Stage"));
            _dataElementMap.Add(0x00082130, new TagMetaData(VRType.DS, "Event Elapsed Time(s)"));
            _dataElementMap.Add(0x00082132, new TagMetaData(VRType.LO, "Event Timer Name(s)"));
            _dataElementMap.Add(0x00082142, new TagMetaData(VRType.IS, "Start Trim"));
            _dataElementMap.Add(0x00082143, new TagMetaData(VRType.IS, "Stop Trim"));
            _dataElementMap.Add(0x00082144, new TagMetaData(VRType.IS, "Recommended Display Frame Rate"));
            _dataElementMap.Add(0x00082200, new TagMetaData(VRType.CS, "Transducer Position"));
            _dataElementMap.Add(0x00082204, new TagMetaData(VRType.CS, "Transducer Orientation"));
            _dataElementMap.Add(0x00082208, new TagMetaData(VRType.CS, "Anatomic Structure"));
            _dataElementMap.Add(0x00084000, new TagMetaData(VRType.SH, "Group 0008 Comments (RET)"));
            _dataElementMap.Add(0x00089215, new TagMetaData(VRType.SQ, "Derivation Code Sequence"));
            _dataElementMap.Add(0x00090010, new TagMetaData(VRType.LO, "unknown"));
            _dataElementMap.Add(0x00100000, new TagMetaData(VRType.UL, "Group 0010 Length"));
            _dataElementMap.Add(0x00100010, new TagMetaData(VRType.PN, "Patient's Name"));
            _dataElementMap.Add(0x00100020, new TagMetaData(VRType.LO, "Patient ID"));
            _dataElementMap.Add(0x00100021, new TagMetaData(VRType.LO, "Issuer of Patient ID"));
            _dataElementMap.Add(0x00100030, new TagMetaData(VRType.DA, "Patient's Birth Date"));
            _dataElementMap.Add(0x00100032, new TagMetaData(VRType.TM, "Patient's Birth Time"));
            _dataElementMap.Add(0x00100040, new TagMetaData(VRType.CS, "Patient's Sex"));
            _dataElementMap.Add(0x00100042, new TagMetaData(VRType.SH, "Patient's Social Security Number"));
            _dataElementMap.Add(0x00100050, new TagMetaData(VRType.SQ, "Patient's Insurance Plan Code Sequence"));
            _dataElementMap.Add(0x00101000, new TagMetaData(VRType.LO, "Other Patient IDs"));
            _dataElementMap.Add(0x00101001, new TagMetaData(VRType.PN, "Other Patient Names"));
            _dataElementMap.Add(0x00101005, new TagMetaData(VRType.PN, "Patient's Maiden Name"));
            _dataElementMap.Add(0x00101010, new TagMetaData(VRType.AS, "Patient's Age"));
            _dataElementMap.Add(0x00101020, new TagMetaData(VRType.DS, "Patient's Size"));
            _dataElementMap.Add(0x00101030, new TagMetaData(VRType.DS, "Patient's Weight"));
            _dataElementMap.Add(0x00101040, new TagMetaData(VRType.LO, "Patient's Address"));
            _dataElementMap.Add(0x00101050, new TagMetaData(VRType.SH, "Insurance Plan Identification (RET)"));
            _dataElementMap.Add(0x00101060, new TagMetaData(VRType.PN, "Patient's Mother's Maiden Name"));
            _dataElementMap.Add(0x00101080, new TagMetaData(VRType.LO, "Military Rank"));
            _dataElementMap.Add(0x00101081, new TagMetaData(VRType.LO, "Branch of Service"));
            _dataElementMap.Add(0x00101090, new TagMetaData(VRType.LO, "Medical Record Locator"));
            _dataElementMap.Add(0x00102000, new TagMetaData(VRType.LO, "Medical Alerts"));
            _dataElementMap.Add(0x00102110, new TagMetaData(VRType.LO, "Contrast Allergies"));
            _dataElementMap.Add(0x00102150, new TagMetaData(VRType.LO, "Country of Residence"));
            _dataElementMap.Add(0x00102152, new TagMetaData(VRType.LO, "Region of Residence"));
            _dataElementMap.Add(0x00102154, new TagMetaData(VRType.SH, "Patient's Telephone Numbers"));
            _dataElementMap.Add(0x00102160, new TagMetaData(VRType.SH, "Ethnic Group"));
            _dataElementMap.Add(0x00102180, new TagMetaData(VRType.SH, "Occupation"));
            _dataElementMap.Add(0x001021A0, new TagMetaData(VRType.CS, "Smoking Status"));
            _dataElementMap.Add(0x001021B0, new TagMetaData(VRType.LT, "Additional Patient History"));
            _dataElementMap.Add(0x001021C0, new TagMetaData(VRType.US, "Pregnancy Status"));
            _dataElementMap.Add(0x001021D0, new TagMetaData(VRType.DA, "Last Menstrual Date"));
            _dataElementMap.Add(0x001021F0, new TagMetaData(VRType.LO, "Patient's Religious Preference"));
            _dataElementMap.Add(0x00104000, new TagMetaData(VRType.LT, "Patient Comments"));
            _dataElementMap.Add(0x00180000, new TagMetaData(VRType.UL, "Group 0018 Length"));
            _dataElementMap.Add(0x00180010, new TagMetaData(VRType.LO, "Contrast/Bolus Agent"));
            _dataElementMap.Add(0x00180015, new TagMetaData(VRType.CS, "Body Part Examined"));
            _dataElementMap.Add(0x00180020, new TagMetaData(VRType.CS, "Scanning Sequence"));
            _dataElementMap.Add(0x00180021, new TagMetaData(VRType.CS, "Sequence Variant"));
            _dataElementMap.Add(0x00180022, new TagMetaData(VRType.CS, "Scan Options"));
            _dataElementMap.Add(0x00180023, new TagMetaData(VRType.CS, "MR Acquisition Type"));
            _dataElementMap.Add(0x00180024, new TagMetaData(VRType.SH, "Sequence Name"));
            _dataElementMap.Add(0x00180025, new TagMetaData(VRType.CS, "Angio Flag"));
            _dataElementMap.Add(0x00180030, new TagMetaData(VRType.LO, "Radionuclide"));
            _dataElementMap.Add(0x00180031, new TagMetaData(VRType.LO, "Radiopharmaceutical"));
            _dataElementMap.Add(0x00180032, new TagMetaData(VRType.DS, "Energy Window Centerline"));
            _dataElementMap.Add(0x00180033, new TagMetaData(VRType.DS, "Energy Window Total Width"));
            _dataElementMap.Add(0x00180034, new TagMetaData(VRType.LO, "Intervention Drug Name"));
            _dataElementMap.Add(0x00180035, new TagMetaData(VRType.TM, "Intervention Drug Start Time"));
            _dataElementMap.Add(0x00180040, new TagMetaData(VRType.IS, "Cine Rate"));
            _dataElementMap.Add(0x00180050, new TagMetaData(VRType.DS, "Slice Thickness"));
            _dataElementMap.Add(0x00180060, new TagMetaData(VRType.DS, "KVP"));
            _dataElementMap.Add(0x00180070, new TagMetaData(VRType.IS, "Counts Accumulated"));
            _dataElementMap.Add(0x00180071, new TagMetaData(VRType.CS, "Acquisition Termination Condition"));
            _dataElementMap.Add(0x00180072, new TagMetaData(VRType.DS, "Effective Series Duration"));
            _dataElementMap.Add(0x00180080, new TagMetaData(VRType.DS, "Repetition Time"));
            _dataElementMap.Add(0x00180081, new TagMetaData(VRType.DS, "Echo Time"));
            _dataElementMap.Add(0x00180082, new TagMetaData(VRType.DS, "Inversion Time"));
            _dataElementMap.Add(0x00180083, new TagMetaData(VRType.DS, "Number of Averages"));
            _dataElementMap.Add(0x00180084, new TagMetaData(VRType.DS, "Imaging Frequency"));
            _dataElementMap.Add(0x00180085, new TagMetaData(VRType.SH, "Imaged Nucleus"));
            _dataElementMap.Add(0x00180086, new TagMetaData(VRType.IS, "Echo Numbers(s)"));
            _dataElementMap.Add(0x00180087, new TagMetaData(VRType.DS, "Magnetic Field Strength"));
            _dataElementMap.Add(0x00180088, new TagMetaData(VRType.DS, "Spacing Between Slices"));
            _dataElementMap.Add(0x00180089, new TagMetaData(VRType.IS, "Number of Phase Encoding Steps"));
            _dataElementMap.Add(0x00180090, new TagMetaData(VRType.DS, "Data Collection Diameter"));
            _dataElementMap.Add(0x00180091, new TagMetaData(VRType.IS, "Echo Train Length"));
            _dataElementMap.Add(0x00180093, new TagMetaData(VRType.DS, "Percent Sampling"));
            _dataElementMap.Add(0x00180094, new TagMetaData(VRType.DS, "Percent Phase Field of View"));
            _dataElementMap.Add(0x00180095, new TagMetaData(VRType.DS, "Pixel Bandwidth"));
            _dataElementMap.Add(0x00181000, new TagMetaData(VRType.LO, "Device Serial Number"));
            _dataElementMap.Add(0x00181004, new TagMetaData(VRType.LO, "Plate ID"));
            _dataElementMap.Add(0x00181010, new TagMetaData(VRType.LO, "Secondary Capture Device ID"));
            _dataElementMap.Add(0x00181012, new TagMetaData(VRType.DA, "Date of Secondary Capture"));
            _dataElementMap.Add(0x00181014, new TagMetaData(VRType.TM, "Time of Secondary Capture"));
            _dataElementMap.Add(0x00181016, new TagMetaData(VRType.LO, "Secondary Capture Device Manufacturer"));
            _dataElementMap.Add(0x00181018, new TagMetaData(VRType.LO, "Secondary Capture Device Manufacturer's Model Name"));
            _dataElementMap.Add(0x00181019, new TagMetaData(VRType.LO, "Secondary Capture Device Software Version(s)"));
            _dataElementMap.Add(0x00181020, new TagMetaData(VRType.LO, "Software Versions(s)"));
            _dataElementMap.Add(0x00181022, new TagMetaData(VRType.SH, "Video Image Format Acquired"));
            _dataElementMap.Add(0x00181023, new TagMetaData(VRType.LO, "Digital Image Format Acquired"));
            _dataElementMap.Add(0x00181030, new TagMetaData(VRType.LO, "Protocol Name"));
            _dataElementMap.Add(0x00181040, new TagMetaData(VRType.LO, "Contrast/Bolus Route"));
            _dataElementMap.Add(0x00181041, new TagMetaData(VRType.DS, "Contrast/Bolus Volume"));
            _dataElementMap.Add(0x00181042, new TagMetaData(VRType.TM, "Contrast/Bolus Start Time"));
            _dataElementMap.Add(0x00181043, new TagMetaData(VRType.TM, "Contrast/Bolus Stop Time"));
            _dataElementMap.Add(0x00181044, new TagMetaData(VRType.DS, "Contrast/Bolus Total Dose"));
            _dataElementMap.Add(0x00181045, new TagMetaData(VRType.IS, "Syringe Counts"));
            _dataElementMap.Add(0x00181050, new TagMetaData(VRType.DS, "Spatial Resolution"));
            _dataElementMap.Add(0x00181060, new TagMetaData(VRType.DS, "Trigger Time"));
            _dataElementMap.Add(0x00181061, new TagMetaData(VRType.LO, "Trigger Source or Type"));
            _dataElementMap.Add(0x00181062, new TagMetaData(VRType.IS, "Nominal Interval"));
            _dataElementMap.Add(0x00181063, new TagMetaData(VRType.DS, "Frame Time"));
            _dataElementMap.Add(0x00181064, new TagMetaData(VRType.LO, "Framing Type"));
            _dataElementMap.Add(0x00181065, new TagMetaData(VRType.DS, "Frame Time Vector"));
            _dataElementMap.Add(0x00181066, new TagMetaData(VRType.DS, "Frame Delay"));
            _dataElementMap.Add(0x00181070, new TagMetaData(VRType.LO, "Radionuclide Route"));
            _dataElementMap.Add(0x00181071, new TagMetaData(VRType.DS, "Radionuclide Volume"));
            _dataElementMap.Add(0x00181072, new TagMetaData(VRType.TM, "Radionuclide Start Time"));
            _dataElementMap.Add(0x00181073, new TagMetaData(VRType.TM, "Radionuclide Stop Time"));
            _dataElementMap.Add(0x00181074, new TagMetaData(VRType.DS, "Radionuclide Total Dose"));
            _dataElementMap.Add(0x00181080, new TagMetaData(VRType.CS, "Beat Rejection Flag"));
            _dataElementMap.Add(0x00181081, new TagMetaData(VRType.IS, "Low R-R Value"));
            _dataElementMap.Add(0x00181082, new TagMetaData(VRType.IS, "High R-R Value"));
            _dataElementMap.Add(0x00181083, new TagMetaData(VRType.IS, "Intervals Acquired"));
            _dataElementMap.Add(0x00181084, new TagMetaData(VRType.IS, "Intervals Rejected"));
            _dataElementMap.Add(0x00181085, new TagMetaData(VRType.LO, "PVC Rejection"));
            _dataElementMap.Add(0x00181086, new TagMetaData(VRType.IS, "Skip Beats"));
            _dataElementMap.Add(0x00181088, new TagMetaData(VRType.IS, "Heart Rate"));
            _dataElementMap.Add(0x00181090, new TagMetaData(VRType.IS, "Cardiac Number of Images"));
            _dataElementMap.Add(0x00181094, new TagMetaData(VRType.IS, "Trigger Window"));
            _dataElementMap.Add(0x00181100, new TagMetaData(VRType.DS, "Reconstruction Diameter"));
            _dataElementMap.Add(0x00181110, new TagMetaData(VRType.DS, "Distance Source to Detector"));
            _dataElementMap.Add(0x00181111, new TagMetaData(VRType.DS, "Distance Source to Patient"));
            _dataElementMap.Add(0x00181120, new TagMetaData(VRType.DS, "Gantry/Detector Tilt"));
            _dataElementMap.Add(0x00181130, new TagMetaData(VRType.DS, "Table Height"));
            _dataElementMap.Add(0x00181131, new TagMetaData(VRType.DS, "Table Traverse"));
            _dataElementMap.Add(0x00181140, new TagMetaData(VRType.CS, "Rotation Direction"));
            _dataElementMap.Add(0x00181141, new TagMetaData(VRType.DS, "Angular Position"));
            _dataElementMap.Add(0x00181142, new TagMetaData(VRType.DS, "Radial Position"));
            _dataElementMap.Add(0x00181143, new TagMetaData(VRType.DS, "Scan Arc"));
            _dataElementMap.Add(0x00181144, new TagMetaData(VRType.DS, "Angular Step"));
            _dataElementMap.Add(0x00181145, new TagMetaData(VRType.DS, "Center of Rotation Offset"));
            _dataElementMap.Add(0x00181146, new TagMetaData(VRType.DS, "Rotation Offset"));
            _dataElementMap.Add(0x00181147, new TagMetaData(VRType.CS, "Field of View Shape"));
            _dataElementMap.Add(0x00181149, new TagMetaData(VRType.IS, "Field of View Dimensions(s)"));
            _dataElementMap.Add(0x00181150, new TagMetaData(VRType.IS, "Exposure Time"));
            _dataElementMap.Add(0x00181151, new TagMetaData(VRType.IS, "X-ray Tube Current"));
            _dataElementMap.Add(0x00181152, new TagMetaData(VRType.IS, "Exposure"));
            _dataElementMap.Add(0x00181160, new TagMetaData(VRType.SH, "Filter Type"));
            _dataElementMap.Add(0x00181170, new TagMetaData(VRType.IS, "Generator Power"));
            _dataElementMap.Add(0x00181180, new TagMetaData(VRType.SH, "Collimator/grid Name"));
            _dataElementMap.Add(0x00181181, new TagMetaData(VRType.CS, "Collimator Type"));
            _dataElementMap.Add(0x00181182, new TagMetaData(VRType.IS, "Focal Distance"));
            _dataElementMap.Add(0x00181183, new TagMetaData(VRType.DS, "X Focus Center"));
            _dataElementMap.Add(0x00181184, new TagMetaData(VRType.DS, "Y Focus Center"));
            _dataElementMap.Add(0x00181190, new TagMetaData(VRType.DS, "Focal Spot(s)"));
            _dataElementMap.Add(0x00181200, new TagMetaData(VRType.DA, "Date of Last Calibration"));
            _dataElementMap.Add(0x00181201, new TagMetaData(VRType.TM, "Time of Last Calibration"));
            _dataElementMap.Add(0x00181210, new TagMetaData(VRType.SH, "Convolution Kernel"));
            _dataElementMap.Add(0x00181240, new TagMetaData(VRType.DS, "Upper/Lower Pixel Values (RET)"));
            _dataElementMap.Add(0x00181242, new TagMetaData(VRType.IS, "Actual Frame Duration"));
            _dataElementMap.Add(0x00181243, new TagMetaData(VRType.IS, "Count Rate"));
            _dataElementMap.Add(0x00181250, new TagMetaData(VRType.SH, "Receiving Coil"));
            _dataElementMap.Add(0x00181251, new TagMetaData(VRType.SH, "Transmitting Coil"));
            _dataElementMap.Add(0x00181260, new TagMetaData(VRType.SH, "Screen Type"));
            _dataElementMap.Add(0x00181261, new TagMetaData(VRType.LO, "Phosphor Type"));
            _dataElementMap.Add(0x00181300, new TagMetaData(VRType.IS, "Scan Velocity"));
            _dataElementMap.Add(0x00181301, new TagMetaData(VRType.CS, "Whole Body Technique"));
            _dataElementMap.Add(0x00181302, new TagMetaData(VRType.IS, "Scan Length"));
            _dataElementMap.Add(0x00181310, new TagMetaData(VRType.US, "Acquisition Matrix"));
            _dataElementMap.Add(0x00181312, new TagMetaData(VRType.CS, "Phase Encoding Direction"));
            _dataElementMap.Add(0x00181314, new TagMetaData(VRType.DS, "Flip Angle"));
            _dataElementMap.Add(0x00181315, new TagMetaData(VRType.CS, "Variable Flip Angle Flag"));
            _dataElementMap.Add(0x00181316, new TagMetaData(VRType.DS, "SAR"));
            _dataElementMap.Add(0x00181318, new TagMetaData(VRType.DS, "dB/dt"));
            _dataElementMap.Add(0x00181400, new TagMetaData(VRType.LO, "Acquisition Device Processing Description"));
            _dataElementMap.Add(0x00181401, new TagMetaData(VRType.LO, "Acquisition Device Processing Code"));
            _dataElementMap.Add(0x00181402, new TagMetaData(VRType.CS, "Cassette Orientation"));
            _dataElementMap.Add(0x00181403, new TagMetaData(VRType.CS, "Cassette Size"));
            _dataElementMap.Add(0x00181404, new TagMetaData(VRType.US, "Exposures on Plate"));
            _dataElementMap.Add(0x00181405, new TagMetaData(VRType.IS, "Relative X-ray Exposure"));
            _dataElementMap.Add(0x00184000, new TagMetaData(VRType.SH, "Group 0018 Comments (RET)"));
            _dataElementMap.Add(0x00185000, new TagMetaData(VRType.SH, "Output Power"));
            _dataElementMap.Add(0x00185010, new TagMetaData(VRType.LO, "Transducer Data"));
            _dataElementMap.Add(0x00185012, new TagMetaData(VRType.DS, "Focus Depth"));
            _dataElementMap.Add(0x00185020, new TagMetaData(VRType.LO, "Preprocessing Function"));
            _dataElementMap.Add(0x00185021, new TagMetaData(VRType.LO, "Postprocessing Function"));
            _dataElementMap.Add(0x00185022, new TagMetaData(VRType.DS, "Mechanical Index"));
            _dataElementMap.Add(0x00185024, new TagMetaData(VRType.DS, "Thermal Index"));
            _dataElementMap.Add(0x00185026, new TagMetaData(VRType.DS, "Cranial Thermal Index"));
            _dataElementMap.Add(0x00185027, new TagMetaData(VRType.DS, "Soft Tissue Thermal Index"));
            _dataElementMap.Add(0x00185028, new TagMetaData(VRType.DS, "Soft Tissue-focus Thermal Index"));
            _dataElementMap.Add(0x00185029, new TagMetaData(VRType.DS, "Soft Tissue-surface Thermal Index"));
            _dataElementMap.Add(0x00185030, new TagMetaData(VRType.IS, "Dynamic Range (RET)"));
            _dataElementMap.Add(0x00185040, new TagMetaData(VRType.IS, "Total Gain (RET)"));
            _dataElementMap.Add(0x00185050, new TagMetaData(VRType.IS, "Depth of Scan Field"));
            _dataElementMap.Add(0x00185100, new TagMetaData(VRType.CS, "Patient Position"));
            _dataElementMap.Add(0x00185101, new TagMetaData(VRType.CS, "View Position"));
            _dataElementMap.Add(0x00185210, new TagMetaData(VRType.DS, "Image Transformation Matrix"));
            _dataElementMap.Add(0x00185212, new TagMetaData(VRType.DS, "Image Translation Vector"));
            _dataElementMap.Add(0x00186000, new TagMetaData(VRType.DS, "Sensitivity"));
            _dataElementMap.Add(0x00186011, new TagMetaData(VRType.SQ, "Sequence of Ultrasound Regions"));
            _dataElementMap.Add(0x00186012, new TagMetaData(VRType.US, "Region Spatial Format"));
            _dataElementMap.Add(0x00186014, new TagMetaData(VRType.US, "Region Data Type"));
            _dataElementMap.Add(0x00186016, new TagMetaData(VRType.UL, "Region Flags"));
            _dataElementMap.Add(0x00186018, new TagMetaData(VRType.UL, "Region Location Min X0"));
            _dataElementMap.Add(0x0018601A, new TagMetaData(VRType.UL, "Region Location Min Y0"));
            _dataElementMap.Add(0x0018601C, new TagMetaData(VRType.UL, "Region Location Max X1"));
            _dataElementMap.Add(0x0018601E, new TagMetaData(VRType.UL, "Region Location Max Y1"));
            _dataElementMap.Add(0x00186020, new TagMetaData(VRType.SL, "Reference Pixel X0"));
            _dataElementMap.Add(0x00186022, new TagMetaData(VRType.SL, "Reference Pixel Y0"));
            _dataElementMap.Add(0x00186024, new TagMetaData(VRType.US, "Physical Units X Direction"));
            _dataElementMap.Add(0x00186026, new TagMetaData(VRType.US, "Physical Units Y Direction"));
            _dataElementMap.Add(0x00181628, new TagMetaData(VRType.FD, "Reference Pixel Physical Value X"));
            _dataElementMap.Add(0x0018602A, new TagMetaData(VRType.FD, "Reference Pixel Physical Value Y"));
            _dataElementMap.Add(0x0018602C, new TagMetaData(VRType.FD, "Physical Delta X"));
            _dataElementMap.Add(0x0018602E, new TagMetaData(VRType.FD, "Physical Delta Y"));
            _dataElementMap.Add(0x00186030, new TagMetaData(VRType.UL, "Transducer Frequency"));
            _dataElementMap.Add(0x00186031, new TagMetaData(VRType.CS, "Transducer Type"));
            _dataElementMap.Add(0x00186032, new TagMetaData(VRType.UL, "Pulse Repetition Frequency"));
            _dataElementMap.Add(0x00186034, new TagMetaData(VRType.FD, "Doppler Correction Angle"));
            _dataElementMap.Add(0x00186036, new TagMetaData(VRType.FD, "Sterring Angle"));
            _dataElementMap.Add(0x00186038, new TagMetaData(VRType.UL, "Doppler Sample Volume X Position"));
            _dataElementMap.Add(0x0018603A, new TagMetaData(VRType.UL, "Doppler Sample Volume Y Position"));
            _dataElementMap.Add(0x0018603C, new TagMetaData(VRType.UL, "TM-Line Position X0"));
            _dataElementMap.Add(0x0018603E, new TagMetaData(VRType.UL, "TM-Line Position Y0"));
            _dataElementMap.Add(0x00186040, new TagMetaData(VRType.UL, "TM-Line Position X1"));
            _dataElementMap.Add(0x00186042, new TagMetaData(VRType.UL, "TM-Line Position Y1"));
            _dataElementMap.Add(0x00186044, new TagMetaData(VRType.US, "Pixel Component Organization"));
            _dataElementMap.Add(0x00186046, new TagMetaData(VRType.UL, "Pixel Component Organization"));
            _dataElementMap.Add(0x00186048, new TagMetaData(VRType.UL, "Pixel Component Range Start"));
            _dataElementMap.Add(0x0018604A, new TagMetaData(VRType.UL, "Pixel Component Range Stop"));
            _dataElementMap.Add(0x0018604C, new TagMetaData(VRType.US, "Pixel Component Physical Units"));
            _dataElementMap.Add(0x0018604E, new TagMetaData(VRType.US, "Pixel Component Data Type"));
            _dataElementMap.Add(0x00186050, new TagMetaData(VRType.UL, "Number of Table Break Points"));
            _dataElementMap.Add(0x00186052, new TagMetaData(VRType.UL, "Table of X Break Points"));
            _dataElementMap.Add(0x00186054, new TagMetaData(VRType.FD, "Table of Y Break Points"));
            _dataElementMap.Add(0x00200000, new TagMetaData(VRType.UL, "Group 0020 Length"));
            _dataElementMap.Add(0x0020000D, new TagMetaData(VRType.UI, "Study Instance UID"));
            _dataElementMap.Add(0x0020000E, new TagMetaData(VRType.UI, "Series Instance UID"));
            _dataElementMap.Add(0x00200010, new TagMetaData(VRType.SH, "Study ID"));
            _dataElementMap.Add(0x00200011, new TagMetaData(VRType.IS, "Series Number"));
            _dataElementMap.Add(0x00200012, new TagMetaData(VRType.IS, "Scquisition Number"));
            _dataElementMap.Add(0x00200013, new TagMetaData(VRType.IS, "Image Number"));
            _dataElementMap.Add(0x00200014, new TagMetaData(VRType.IS, "Isotope Number"));
            _dataElementMap.Add(0x00200015, new TagMetaData(VRType.IS, "Phase Number"));
            _dataElementMap.Add(0x00200016, new TagMetaData(VRType.IS, "Interval Number"));
            _dataElementMap.Add(0x00200017, new TagMetaData(VRType.IS, "Time Slot Number"));
            _dataElementMap.Add(0x00200018, new TagMetaData(VRType.IS, "Angle Number"));
            _dataElementMap.Add(0x00200020, new TagMetaData(VRType.CS, "Patient Orientation"));
            _dataElementMap.Add(0x00200022, new TagMetaData(VRType.US, "Overlay Number"));
            _dataElementMap.Add(0x00200024, new TagMetaData(VRType.US, "Curve Number"));
            _dataElementMap.Add(0x00200030, new TagMetaData(VRType.DS, "Image Position (RET)"));
            _dataElementMap.Add(0x00200032, new TagMetaData(VRType.DS, "Image Position (Patient)"));
            _dataElementMap.Add(0x00200035, new TagMetaData(VRType.DS, "Image Orientation (RET)"));
            _dataElementMap.Add(0x00200037, new TagMetaData(VRType.DS, "Image Orientation (Patient)"));
            _dataElementMap.Add(0x00200050, new TagMetaData(VRType.DS, "Location (RET)"));
            _dataElementMap.Add(0x00200052, new TagMetaData(VRType.UI, "Frame of Reference UID"));
            _dataElementMap.Add(0x00200060, new TagMetaData(VRType.CS, "Laterality"));
            _dataElementMap.Add(0x00200070, new TagMetaData(VRType.SH, "Image Geometry Type (RET)"));
            _dataElementMap.Add(0x00200080, new TagMetaData(VRType.UI, "Masking Image UID"));
            _dataElementMap.Add(0x00200100, new TagMetaData(VRType.IS, "Temporal Position Identifier"));
            _dataElementMap.Add(0x00200105, new TagMetaData(VRType.IS, "Number of Temporal Positions"));
            _dataElementMap.Add(0x00200110, new TagMetaData(VRType.DS, "Temporal Resolution"));
            _dataElementMap.Add(0x00201000, new TagMetaData(VRType.IS, "Series in Study"));
            _dataElementMap.Add(0x00201001, new TagMetaData(VRType.IS, "Acquisitions in Series (RET)"));
            _dataElementMap.Add(0x00201002, new TagMetaData(VRType.IS, "Images in Acquisition"));
            _dataElementMap.Add(0x00201004, new TagMetaData(VRType.IS, "Acquisition in Study"));
            _dataElementMap.Add(0x00201020, new TagMetaData(VRType.SH, "Reference (RET)"));
            _dataElementMap.Add(0x00201040, new TagMetaData(VRType.LO, "Position Reference Indicator"));
            _dataElementMap.Add(0x00201041, new TagMetaData(VRType.DS, "Slice Location"));
            _dataElementMap.Add(0x00201070, new TagMetaData(VRType.IS, "Other Study Numbers"));
            _dataElementMap.Add(0x00201200, new TagMetaData(VRType.IS, "Number of Patient Related Studies"));
            _dataElementMap.Add(0x00201202, new TagMetaData(VRType.IS, "Number of Patient Related Series"));
            _dataElementMap.Add(0x00201204, new TagMetaData(VRType.IS, "Number of Patient Related Images"));
            _dataElementMap.Add(0x00201206, new TagMetaData(VRType.IS, "Number of Study Related Series"));
            _dataElementMap.Add(0x00201208, new TagMetaData(VRType.IS, "Number of Study Related Images"));
            _dataElementMap.Add(0x00203100, new TagMetaData(VRType.SH, "Source Image ID (RET)s"));
            _dataElementMap.Add(0x00203401, new TagMetaData(VRType.SH, "Modifying Device ID (RET)"));
            _dataElementMap.Add(0x00203402, new TagMetaData(VRType.SH, "Modified Image ID (RET)"));
            _dataElementMap.Add(0x00203403, new TagMetaData(VRType.SH, "Modified Image Date (RET)"));
            _dataElementMap.Add(0x00203404, new TagMetaData(VRType.SH, "Modifying Device Manufacturer (RET)"));
            _dataElementMap.Add(0x00203405, new TagMetaData(VRType.SH, "Modified Image Time (RET)"));
            _dataElementMap.Add(0x00203406, new TagMetaData(VRType.SH, "Modified Image Description (RET)"));
            _dataElementMap.Add(0x00204000, new TagMetaData(VRType.LT, "Image Comments"));
            _dataElementMap.Add(0x00205000, new TagMetaData(VRType.US, "Original Image Identification (RET)"));
            _dataElementMap.Add(0x00205002, new TagMetaData(VRType.SH, "Original Image Identification Nomenclature (RET)"));
            _dataElementMap.Add(0x00280000, new TagMetaData(VRType.UL, "Group 0028 Length"));
            _dataElementMap.Add(0x00280002, new TagMetaData(VRType.US, "Samples per Pixel"));
            _dataElementMap.Add(0x00280004, new TagMetaData(VRType.CS, "Photometric Interpretation"));
            _dataElementMap.Add(0x00280005, new TagMetaData(VRType.US, "Image Dimensions (RET)"));
            _dataElementMap.Add(0x00280006, new TagMetaData(VRType.US, "Planar Configuration"));
            _dataElementMap.Add(0x00280008, new TagMetaData(VRType.IS, "Number of Frames"));
            _dataElementMap.Add(0x00280009, new TagMetaData(VRType.AT, "Frame Increment Pointer"));
            _dataElementMap.Add(0x00280010, new TagMetaData(VRType.US, "Rows"));
            _dataElementMap.Add(0x00280011, new TagMetaData(VRType.US, "Columns"));
            _dataElementMap.Add(0x00280030, new TagMetaData(VRType.DS, "Pixel Spacing"));
            _dataElementMap.Add(0x00280031, new TagMetaData(VRType.DS, "Zoom Factor"));
            _dataElementMap.Add(0x00280032, new TagMetaData(VRType.DS, "Zoom Center"));
            _dataElementMap.Add(0x00280034, new TagMetaData(VRType.IS, "Pixel Aspect Ratio"));
            _dataElementMap.Add(0x00280040, new TagMetaData(VRType.SH, "Image Format (RET)"));
            _dataElementMap.Add(0x00280050, new TagMetaData(VRType.SH, "Manipulated Image (RET)"));
            _dataElementMap.Add(0x00280051, new TagMetaData(VRType.CS, "Corrected Image"));
            _dataElementMap.Add(0x00280060, new TagMetaData(VRType.SH, "Compression Code (RET)"));
            _dataElementMap.Add(0x00280100, new TagMetaData(VRType.US, "Bits Allocated"));
            _dataElementMap.Add(0x00280101, new TagMetaData(VRType.US, "Bits Stored"));
            _dataElementMap.Add(0x00280102, new TagMetaData(VRType.US, "High Bit"));
            _dataElementMap.Add(0x00280103, new TagMetaData(VRType.US, "Pixel Representation"));
            _dataElementMap.Add(0x00280104, new TagMetaData(VRType.US, "Smallest Valid Pixel Value (RET)"));
            _dataElementMap.Add(0x00280105, new TagMetaData(VRType.US, "Largest Valid Pixel Value (RET)"));
            _dataElementMap.Add(0x00280106, new TagMetaData(VRType.US, "Smallest Image Pixel Value"));
            _dataElementMap.Add(0x00280107, new TagMetaData(VRType.US, "Largest Image Pixel Value"));
            _dataElementMap.Add(0x00280108, new TagMetaData(VRType.US, "Smallest Pixel Value in Series"));
            _dataElementMap.Add(0x00280109, new TagMetaData(VRType.US, "Largest Pixel Value in Series"));
            _dataElementMap.Add(0x00280120, new TagMetaData(VRType.US, "Pixel Padding Value"));
            _dataElementMap.Add(0x00280200, new TagMetaData(VRType.US, "Image Location (RET)"));
            _dataElementMap.Add(0x00281050, new TagMetaData(VRType.DS, "Window Center"));
            _dataElementMap.Add(0x00281051, new TagMetaData(VRType.DS, "Window Width"));
            _dataElementMap.Add(0x00281052, new TagMetaData(VRType.DS, "Rescale Intercept"));
            _dataElementMap.Add(0x00281053, new TagMetaData(VRType.DS, "Rescale Slope"));
            _dataElementMap.Add(0x00281054, new TagMetaData(VRType.LO, "Rescale Type"));
            _dataElementMap.Add(0x00281055, new TagMetaData(VRType.LO, "Window Center & Width Explanation"));
            _dataElementMap.Add(0x00281080, new TagMetaData(VRType.SH, "Gray Scale (RET)"));
            _dataElementMap.Add(0x00281100, new TagMetaData(VRType.US, "Gray Lookup Table Descriptor (RET)"));
            _dataElementMap.Add(0x00281101, new TagMetaData(VRType.US, "Red Palette Color Lookup Table Descriptor"));
            _dataElementMap.Add(0x00281102, new TagMetaData(VRType.US, "Green Palette Color Lookup Table Descriptor"));
            _dataElementMap.Add(0x00281103, new TagMetaData(VRType.US, "Blue Palette Color Lookup Table Descriptor"));
            _dataElementMap.Add(0x00281200, new TagMetaData(VRType.US, "Gray Lookup Table Data (RET)"));
            _dataElementMap.Add(0x00281201, new TagMetaData(VRType.US, "Red Palette Color Lookup Table Data"));
            _dataElementMap.Add(0x00281202, new TagMetaData(VRType.US, "Green Palette Color Lookup Table Data"));
            _dataElementMap.Add(0x00281203, new TagMetaData(VRType.US, "Blue Palette Color Lookup Table Data"));
            _dataElementMap.Add(0x00283000, new TagMetaData(VRType.SQ, "Modality LUT Sequence"));
            _dataElementMap.Add(0x00283002, new TagMetaData(VRType.US, "LUT Descriptor"));
            _dataElementMap.Add(0x00283003, new TagMetaData(VRType.LO, "LUT Explanation"));
            _dataElementMap.Add(0x00283004, new TagMetaData(VRType.LO, "Madality LUT Type"));
            _dataElementMap.Add(0x00283006, new TagMetaData(VRType.US, "LUT Data"));
            _dataElementMap.Add(0x00283010, new TagMetaData(VRType.SQ, "VOI LUT Sequence"));
            _dataElementMap.Add(0x00284000, new TagMetaData(VRType.SH, "Group 0028 Comments (RET)"));
            _dataElementMap.Add(0x00320000, new TagMetaData(VRType.UL, "Group 0032 Length"));
            _dataElementMap.Add(0x0032000A, new TagMetaData(VRType.CS, "Study Status ID"));
            _dataElementMap.Add(0x0032000C, new TagMetaData(VRType.CS, "Study Priority ID"));
            _dataElementMap.Add(0x00320012, new TagMetaData(VRType.LO, "Study ID Issuer"));
            _dataElementMap.Add(0x00320032, new TagMetaData(VRType.DA, "Study Verified Date"));
            _dataElementMap.Add(0x00320033, new TagMetaData(VRType.TM, "Study Verified Time"));
            _dataElementMap.Add(0x00320034, new TagMetaData(VRType.DA, "Study Read Date"));
            _dataElementMap.Add(0x00320035, new TagMetaData(VRType.TM, "Study Read Time"));
            _dataElementMap.Add(0x00321000, new TagMetaData(VRType.DA, "Scheduled Study Start Date"));
            _dataElementMap.Add(0x00321001, new TagMetaData(VRType.TM, "Scheduled Study Start Time"));
            _dataElementMap.Add(0x00321010, new TagMetaData(VRType.DA, "Scheduled Study Stop Date"));
            _dataElementMap.Add(0x00321011, new TagMetaData(VRType.TM, "Scheduled Study Stop Time"));
            _dataElementMap.Add(0x00321020, new TagMetaData(VRType.LO, "Scheduled Study Location"));
            _dataElementMap.Add(0x00321021, new TagMetaData(VRType.AE, "Scheduled Study Location AE Title(s)"));
            _dataElementMap.Add(0x00321030, new TagMetaData(VRType.LO, "Reason  for Study"));
            _dataElementMap.Add(0x00321032, new TagMetaData(VRType.PN, "Requesting Physician"));
            _dataElementMap.Add(0x00321033, new TagMetaData(VRType.LO, "Requesting Service"));
            _dataElementMap.Add(0x00321040, new TagMetaData(VRType.DA, "Study Arrival Date"));
            _dataElementMap.Add(0x00321041, new TagMetaData(VRType.TM, "Study Arrival Time"));
            _dataElementMap.Add(0x00321050, new TagMetaData(VRType.DA, "Study Completion Date"));
            _dataElementMap.Add(0x00321051, new TagMetaData(VRType.TM, "Study Completion Time"));
            _dataElementMap.Add(0x00321055, new TagMetaData(VRType.CS, "Study Component Status ID"));
            _dataElementMap.Add(0x00321060, new TagMetaData(VRType.LO, "Requested Procedure Description"));
            _dataElementMap.Add(0x00321064, new TagMetaData(VRType.SQ, "Requested Procedure Code Sequence"));
            _dataElementMap.Add(0x00321070, new TagMetaData(VRType.LO, "Requested Contrast Agent"));
            _dataElementMap.Add(0x00324000, new TagMetaData(VRType.LT, "Study Comments"));
            _dataElementMap.Add(0x00380000, new TagMetaData(VRType.UL, "Group 0038 Length"));
            _dataElementMap.Add(0x00380004, new TagMetaData(VRType.SQ, "Referenced Patient Alias Sequence"));
            _dataElementMap.Add(0x00380008, new TagMetaData(VRType.CS, "Visit Status ID"));
            _dataElementMap.Add(0x00380010, new TagMetaData(VRType.LO, "Admissin ID"));
            _dataElementMap.Add(0x00380011, new TagMetaData(VRType.LO, "Issuer of Admission ID"));
            _dataElementMap.Add(0x00380016, new TagMetaData(VRType.LO, "Route of Admissions"));
            _dataElementMap.Add(0x0038001A, new TagMetaData(VRType.DA, "Scheduled Admissin Date"));
            _dataElementMap.Add(0x0038001B, new TagMetaData(VRType.TM, "Scheduled Adission Time"));
            _dataElementMap.Add(0x0038001C, new TagMetaData(VRType.DA, "Scheduled Discharge Date"));
            _dataElementMap.Add(0x0038001D, new TagMetaData(VRType.TM, "Scheduled Discharge Time"));
            _dataElementMap.Add(0x0038001E, new TagMetaData(VRType.LO, "Scheduled Patient Institution Residence"));
            _dataElementMap.Add(0x00380020, new TagMetaData(VRType.DA, "Admitting Date"));
            _dataElementMap.Add(0x00380021, new TagMetaData(VRType.TM, "Admitting Time"));
            _dataElementMap.Add(0x00380030, new TagMetaData(VRType.DA, "Discharge Date"));
            _dataElementMap.Add(0x00380032, new TagMetaData(VRType.TM, "Discharge Time"));
            _dataElementMap.Add(0x00380040, new TagMetaData(VRType.LO, "Discharge Diagnosis Description"));
            _dataElementMap.Add(0x00380044, new TagMetaData(VRType.SQ, "Discharge Diagnosis Code Sequence"));
            _dataElementMap.Add(0x00380050, new TagMetaData(VRType.LO, "Special Needs"));
            _dataElementMap.Add(0x00380300, new TagMetaData(VRType.LO, "Current Patient Location"));
            _dataElementMap.Add(0x00380400, new TagMetaData(VRType.LO, "Patient's Institution Residence"));
            _dataElementMap.Add(0x00380500, new TagMetaData(VRType.LO, "Patient State"));
            _dataElementMap.Add(0x00384000, new TagMetaData(VRType.LT, "Visit Comments"));
            _dataElementMap.Add(0x00880000, new TagMetaData(VRType.UL, "Group 0088 Length"));
            _dataElementMap.Add(0x00880130, new TagMetaData(VRType.SH, "Storage Media File-set ID"));
            _dataElementMap.Add(0x00880140, new TagMetaData(VRType.UI, "Storage Media File-set UID"));
            _dataElementMap.Add(0x20000000, new TagMetaData(VRType.UL, "Group 2000 Length"));
            _dataElementMap.Add(0x20000010, new TagMetaData(VRType.IS, "Number of Copies"));
            _dataElementMap.Add(0x20000020, new TagMetaData(VRType.CS, "Print Priority"));
            _dataElementMap.Add(0x20000030, new TagMetaData(VRType.CS, "Medium Type"));
            _dataElementMap.Add(0x20000040, new TagMetaData(VRType.CS, "Film Destination"));
            _dataElementMap.Add(0x20000050, new TagMetaData(VRType.LO, "Film Session Label"));
            _dataElementMap.Add(0x20000060, new TagMetaData(VRType.IS, "Memory Allocation"));
            _dataElementMap.Add(0x20000500, new TagMetaData(VRType.SQ, "Referenced Film Box Sequence"));
            _dataElementMap.Add(0x20100000, new TagMetaData(VRType.UL, "Group 2010 Length"));
            _dataElementMap.Add(0x20100010, new TagMetaData(VRType.ST, "Smage Display Format"));
            _dataElementMap.Add(0x20100030, new TagMetaData(VRType.CS, "Annotation Display Format ID"));
            _dataElementMap.Add(0x20100040, new TagMetaData(VRType.CS, "Film Orientation"));
            _dataElementMap.Add(0x20100050, new TagMetaData(VRType.CS, "Film Size ID"));
            _dataElementMap.Add(0x20100060, new TagMetaData(VRType.CS, "Magnification Type"));
            _dataElementMap.Add(0x20100080, new TagMetaData(VRType.CS, "Smoothing Type"));
            _dataElementMap.Add(0x20100100, new TagMetaData(VRType.CS, "Border Density"));
            _dataElementMap.Add(0x20100110, new TagMetaData(VRType.CS, "Empty Image Density"));
            _dataElementMap.Add(0x20100120, new TagMetaData(VRType.US, "Min Density"));
            _dataElementMap.Add(0x20100130, new TagMetaData(VRType.US, "Max Density"));
            _dataElementMap.Add(0x20100140, new TagMetaData(VRType.CS, "Trim"));
            _dataElementMap.Add(0x20100150, new TagMetaData(VRType.ST, "Configuration Information"));
            _dataElementMap.Add(0x20100500, new TagMetaData(VRType.SQ, "Referenced Film Session Sequence"));
            _dataElementMap.Add(0x20100510, new TagMetaData(VRType.SQ, "Referenced Basic Image Box Sequence"));
            _dataElementMap.Add(0x20100520, new TagMetaData(VRType.SQ, "Referenced Basic Annotation Box Sequence"));
            _dataElementMap.Add(0x20200000, new TagMetaData(VRType.UL, "Group 2020 Length"));
            _dataElementMap.Add(0x20200010, new TagMetaData(VRType.US, "Image Position"));
            _dataElementMap.Add(0x20200020, new TagMetaData(VRType.CS, "Polarity"));
            _dataElementMap.Add(0x20200030, new TagMetaData(VRType.DS, "Requested Image Size"));
            _dataElementMap.Add(0x20200110, new TagMetaData(VRType.SQ, "Preformatted Greyscale Image Sequence"));
            _dataElementMap.Add(0x20200111, new TagMetaData(VRType.SQ, "Preformatted Color Image Sequence"));
            _dataElementMap.Add(0x20200130, new TagMetaData(VRType.SQ, "Referenced Image Overlay Box Sequence"));
            _dataElementMap.Add(0x20200140, new TagMetaData(VRType.SQ, "Referenced VOI LUT Sequence"));
            _dataElementMap.Add(0x20300000, new TagMetaData(VRType.UL, "Group 2030 Length"));
            _dataElementMap.Add(0x20300010, new TagMetaData(VRType.US, "Annotation Position"));
            _dataElementMap.Add(0x20300020, new TagMetaData(VRType.LO, "Text Object"));
            _dataElementMap.Add(0x20400000, new TagMetaData(VRType.UL, "Group 2040 Length"));
            _dataElementMap.Add(0x20400010, new TagMetaData(VRType.SQ, "Referenced Overlay Plane Sequence"));
            _dataElementMap.Add(0x20400011, new TagMetaData(VRType.US, "Refenced Overlay Plane Groups"));
            _dataElementMap.Add(0x20400060, new TagMetaData(VRType.CS, "Overlay Magnification Type"));
            _dataElementMap.Add(0x20400070, new TagMetaData(VRType.CS, "Overlay Smoothing Type"));
            _dataElementMap.Add(0x20400080, new TagMetaData(VRType.CS, "Overlay Foreground Density"));
            _dataElementMap.Add(0x20400090, new TagMetaData(VRType.CS, "overlay Mode"));
            _dataElementMap.Add(0x20400100, new TagMetaData(VRType.CS, "Threshold Density"));
            _dataElementMap.Add(0x20400500, new TagMetaData(VRType.SQ, "Referenced Image Box Sequence"));
            _dataElementMap.Add(0x21000000, new TagMetaData(VRType.UL, "Group 2100 Length"));
            _dataElementMap.Add(0x21000020, new TagMetaData(VRType.CS, "Execution Status"));
            _dataElementMap.Add(0x21000030, new TagMetaData(VRType.CS, "Execution Status Info"));
            _dataElementMap.Add(0x21000040, new TagMetaData(VRType.DA, "Creation Date"));
            _dataElementMap.Add(0x21000050, new TagMetaData(VRType.TM, "Creation Time"));
            _dataElementMap.Add(0x21000070, new TagMetaData(VRType.AE, "Originator"));
            _dataElementMap.Add(0x21000500, new TagMetaData(VRType.SQ, "Referenced Print Job Sequence"));
            _dataElementMap.Add(0x21100000, new TagMetaData(VRType.UL, "Group 2110 Length"));
            _dataElementMap.Add(0x21100010, new TagMetaData(VRType.CS, "Printer Status"));
            _dataElementMap.Add(0x21100020, new TagMetaData(VRType.CS, "Printer Status Info"));
            _dataElementMap.Add(0x21100030, new TagMetaData(VRType.ST, "Printer Name"));
            _dataElementMap.Add(0x40000000, new TagMetaData(VRType.UL, "Group 4000 Length (RET)"));
            _dataElementMap.Add(0x40000010, new TagMetaData(VRType.SH, "Arbitray (RET)"));
            _dataElementMap.Add(0x40004000, new TagMetaData(VRType.LT, "Group 4000 Comments (RET)"));
            _dataElementMap.Add(0x40080000, new TagMetaData(VRType.UL, "Group 4008 Length"));
            _dataElementMap.Add(0x40080040, new TagMetaData(VRType.SH, "Results ID"));
            _dataElementMap.Add(0x40080042, new TagMetaData(VRType.LO, "Results ID Issuer"));
            _dataElementMap.Add(0x40080050, new TagMetaData(VRType.SQ, "Referenced Interpretation Sequence"));
            _dataElementMap.Add(0x40080100, new TagMetaData(VRType.DA, "Interpretation Recorded Date"));
            _dataElementMap.Add(0x40080101, new TagMetaData(VRType.TM, "Interpretation Recorded Time"));
            _dataElementMap.Add(0x40080102, new TagMetaData(VRType.PN, "Interpretation Recorder"));
            _dataElementMap.Add(0x40080103, new TagMetaData(VRType.LO, "Reference to Recorded Sound"));
            _dataElementMap.Add(0x40080108, new TagMetaData(VRType.DA, "Interpretation Transcription Time"));
            _dataElementMap.Add(0x40080109, new TagMetaData(VRType.TM, "Interpretation Transcription Time"));
            _dataElementMap.Add(0x4008010A, new TagMetaData(VRType.PN, "Interpretation Transcriber"));
            _dataElementMap.Add(0x4008010B, new TagMetaData(VRType.ST, "Interpretation Text"));
            _dataElementMap.Add(0x4008010C, new TagMetaData(VRType.PN, "Interpretation Author"));
            _dataElementMap.Add(0x40080111, new TagMetaData(VRType.SQ, "Interpretation Approver Sequence"));
            _dataElementMap.Add(0x40080112, new TagMetaData(VRType.DA, "Interpretation Approval Date"));
            _dataElementMap.Add(0x40080113, new TagMetaData(VRType.TM, "Interpretation Approval Time"));
            _dataElementMap.Add(0x40080114, new TagMetaData(VRType.PN, "Physician Approving Interpretation"));
            _dataElementMap.Add(0x40080115, new TagMetaData(VRType.LT, "Interpretation Diagnosis Description"));
            _dataElementMap.Add(0x40080117, new TagMetaData(VRType.SQ, "Diagnosis Code Sequence"));
            _dataElementMap.Add(0x40080118, new TagMetaData(VRType.SQ, "Results Distribution List Sequence"));
            _dataElementMap.Add(0x40080119, new TagMetaData(VRType.PN, "Distribution Name"));
            _dataElementMap.Add(0x4008011A, new TagMetaData(VRType.LO, "Distribution Address"));
            _dataElementMap.Add(0x40080200, new TagMetaData(VRType.SH, "Interpretation ID"));
            _dataElementMap.Add(0x40080202, new TagMetaData(VRType.LO, "Interpretation ID Issuer"));
            _dataElementMap.Add(0x40080210, new TagMetaData(VRType.CS, "Interpretation Type ID"));
            _dataElementMap.Add(0x40080212, new TagMetaData(VRType.CS, "Interpretation Status ID"));
            _dataElementMap.Add(0x40080300, new TagMetaData(VRType.ST, "Impression"));
            _dataElementMap.Add(0x40084000, new TagMetaData(VRType.SH, "Group 4008 Comments"));
            _dataElementMap.Add(0x50000000, new TagMetaData(VRType.UL, "Group 5000 Length"));
            _dataElementMap.Add(0x50000005, new TagMetaData(VRType.US, "Curve Dimensions"));
            _dataElementMap.Add(0x50000010, new TagMetaData(VRType.US, "Number of Points"));
            _dataElementMap.Add(0x50000020, new TagMetaData(VRType.CS, "Type of Data"));
            _dataElementMap.Add(0x50000022, new TagMetaData(VRType.LO, "Curve Description"));
            _dataElementMap.Add(0x50000030, new TagMetaData(VRType.SH, "Axis Units"));
            _dataElementMap.Add(0x50000040, new TagMetaData(VRType.SH, "Axis Labels"));
            _dataElementMap.Add(0x50000103, new TagMetaData(VRType.US, "Data Value Representation"));
            _dataElementMap.Add(0x50000104, new TagMetaData(VRType.US, "Minimum Coordinate Value"));
            _dataElementMap.Add(0x50000105, new TagMetaData(VRType.US, "Maximum Coordinate Value"));
            _dataElementMap.Add(0x50000106, new TagMetaData(VRType.SH, "Curve Range"));
            _dataElementMap.Add(0x50000110, new TagMetaData(VRType.US, "Curve Data Descriptor"));
            _dataElementMap.Add(0x50000112, new TagMetaData(VRType.US, "Coordinate Start Value"));
            _dataElementMap.Add(0x50000114, new TagMetaData(VRType.US, "Coordinate Step Value"));
            _dataElementMap.Add(0x50002000, new TagMetaData(VRType.US, "Audio Type"));
            _dataElementMap.Add(0x50002002, new TagMetaData(VRType.US, "Audio Sample Format"));
            _dataElementMap.Add(0x50002004, new TagMetaData(VRType.US, "Number of Channels"));
            _dataElementMap.Add(0x50002006, new TagMetaData(VRType.UL, "Number of Samples"));
            _dataElementMap.Add(0x50002008, new TagMetaData(VRType.UL, "Sample Rate"));
            _dataElementMap.Add(0x5000200A, new TagMetaData(VRType.UL, "Total Time"));
            _dataElementMap.Add(0x5000200C, new TagMetaData(VRType.OX, "Audio Sample Data"));
            _dataElementMap.Add(0x5000200E, new TagMetaData(VRType.LT, "Audio Comments"));
            _dataElementMap.Add(0x50003000, new TagMetaData(VRType.OX, "Curve Data"));
            _dataElementMap.Add(0x60000000, new TagMetaData(VRType.UL, "Group 6000 Length"));
            _dataElementMap.Add(0x60000010, new TagMetaData(VRType.US, "Rows"));
            _dataElementMap.Add(0x60000011, new TagMetaData(VRType.US, "Columns"));
            _dataElementMap.Add(0x60000015, new TagMetaData(VRType.IS, "Number of Frames in Overlay"));
            _dataElementMap.Add(0x60000040, new TagMetaData(VRType.CS, "Overlay Type"));
            _dataElementMap.Add(0x60000050, new TagMetaData(VRType.SS, "Origin"));
            _dataElementMap.Add(0x60000060, new TagMetaData(VRType.SH, "Compression Code (RET)"));
            _dataElementMap.Add(0x60000100, new TagMetaData(VRType.US, "Bits Allocated"));
            _dataElementMap.Add(0x60000102, new TagMetaData(VRType.US, "Bit Position"));
            _dataElementMap.Add(0x60000110, new TagMetaData(VRType.SH, "Overlay Format (RET)"));
            _dataElementMap.Add(0x60000200, new TagMetaData(VRType.US, "Overlay Location (RET)"));
            _dataElementMap.Add(0x60001100, new TagMetaData(VRType.US, "Overlay Descriptor - Gray"));
            _dataElementMap.Add(0x60001101, new TagMetaData(VRType.US, "Overlay Descriptor - Red"));
            _dataElementMap.Add(0x60001102, new TagMetaData(VRType.US, "Overlay Descriptor - Green"));
            _dataElementMap.Add(0x60001103, new TagMetaData(VRType.US, "Overlay Descriptor - Blue"));
            _dataElementMap.Add(0x60001200, new TagMetaData(VRType.US, "Overlays - Gray"));
            _dataElementMap.Add(0x60001201, new TagMetaData(VRType.US, "Overlays - Red"));
            _dataElementMap.Add(0x60001202, new TagMetaData(VRType.US, "Overlays - Green"));
            _dataElementMap.Add(0x60001203, new TagMetaData(VRType.US, "Overlays - Blue"));
            _dataElementMap.Add(0x60001301, new TagMetaData(VRType.IS, "ROI Area"));
            _dataElementMap.Add(0x60001302, new TagMetaData(VRType.DS, "ROI Mean"));
            _dataElementMap.Add(0x60001303, new TagMetaData(VRType.DS, "ROI Standard Deviation"));
            _dataElementMap.Add(0x60003000, new TagMetaData(VRType.OW, "Overlay Data"));
            _dataElementMap.Add(0x60004000, new TagMetaData(VRType.SH, "Group 6000 Comments (RET)"));
            _dataElementMap.Add(0x7FE00000, new TagMetaData(VRType.UL, "Group 7FE0 Length"));
            _dataElementMap.Add(0x7FE00010, new TagMetaData(VRType.OX, "Pixel Data"));
            _dataElementMap.Add(0xFFFEE000, new TagMetaData(VRType.DL, "Item"));
            _dataElementMap.Add(0xFFFEE00D, new TagMetaData(VRType.DL, "Item Delimitation Item"));
            _dataElementMap.Add(0xFFFEE0DD, new TagMetaData(VRType.DL, "Sequence Delimitation Item"));
        }

        /**
         * Returns the value range (VR) of a given tag.
         * @param tag  the tag id (group number + element number)
         * @return     the value range
         */
        public VRType getVR(uint tag)
        {
            TagMetaData meta_data;
            try
            {
                meta_data = _dataElementMap[tag];
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

        /**
         * Returns a string representation of the value range (VR) of a given tag.
         * @param tag  the tag id (group number + element number)
         * @return     a string representing the value range
         */
        public string getVRstr(uint tag)
        {
            TagMetaData meta_data = _dataElementMap[tag];
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


        /**
         * Returns a description of the given tag (window width, patient birth, etc.).
         * @param tag  the tag id (group number + element number)
         * @return     a string containing a description of the tag
         */
        public string GetTagDescription(uint tag)
        {
            TagMetaData meta_data = _dataElementMap[tag];
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
            return _mediaStorageMap[id];
        }

        public static uint ToTag(uint groupId, uint elementId)
        {
            return (groupId << 16) | elementId;
        }
    }
}

