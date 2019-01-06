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
        private static Dictionary<uint, TagMetaData> data_element_map = new Dictionary<uint, TagMetaData>();
        private static Dictionary<string, string> media_storage_map = new Dictionary<string, string>();

        public static DiDictonary Instance = new DiDictonary();


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

        /**
         * The default constructor is private (singleton).
         *
         */
        private DiDictonary()
        {
            media_storage_map.Add("1.2.840.10008.1.3.10", "Media Storage Directory Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.1", "ComAdded Radiography Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.1.1", "Digital X-Ray Image Storage For Presentation");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.1.1.1", "Digital X-Ray Image Storage For Processing");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.1.2", "Digital Mammography Image Storage For Presentation");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.1.2.1", "Digital Mammography Image Storage For Processing");

            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.1.3", "Digital Intra-oral X-Ray Image Storage For Presentation");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.1.3.1", "Digital Intra-oral X-Ray Image Storage For Processing");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.2", "CT Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.2.1", "Enhanced CT Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.3.1", "Ultrasound Multi-frame Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.4", "MR Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.4.1", "Enhanced MR Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.4.2", "MR Spectroscopy Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.6.1", "Ultrasound Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.7", "Secondary Capture Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.7.1", "Multi-frame Single Bit Secondary Capture Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.7.2", "Secondary Capture Image Multi-frame Grayscale Byte Secondary Capture Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.7.3", "Multi-frame Grayscale Word Secondary Capture Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.7.4", "Multi-frame True Color Secondary Capture Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.9.1.1", "12-lead ECG Waveform Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.9.1.2", "General ECG Waveform Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.9.1.3", "Ambulatory ECG Waveform Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.9.2.1", "Hemodynamic Waveform Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.9.3.1", "Cardiac Electrophysiology Waveform Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.9.4.1", "Basic Voice Audio Waveform Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.11.1", "Grayscale Softcopy Presentation State Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.11.2", "Color Softcopy Presentation State Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.11.3", "Presentation State Storage");

            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.11.4", "Blending Softcopy Presentation State Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.12.1", "X-Ray Angiographic Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.12.1.1", "Enhanced XA Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.12.2", "X-Ray Radiofluoroscopic Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.12.2.1", "Enhanced XRF Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.13.1.1", "X-Ray 3D Angiographic Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.13.1.2", "X-Ray 3D Craniofacial Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.20", "Nuclear Medicine Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.66", "Raw Data Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.66.1", "Spatial Registration Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.66.2", "Spatial Fiducials Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.66.3", "Deformable Spatial Registration Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.66.4", "Segmentation Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.67", "Real World Value Mapping Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.77.1.1", "VL Endoscopic Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.77.1.1.1", "Video Endoscopic Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.77.1.2", "VL Microscopic Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.77.1.2.1", "Video Microscopic Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.77.1.3", "VL Slide-Coordinates Microscopic Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.77.1.4", "VL Photographic Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.77.1.4.1", "Video Photographic Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.77.1.5.1", "Ophthalmic Photography 8 Bit Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.77.1.5.2", "Ophthalmic Photography 16 Bit Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.77.1.5.3", "Stereometric Relationship Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.77.1.5.4", "Ophthalmic Tomography Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.88.11", "Basic Text SR Enhanced SR");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.88.22", "Basic Text SR");

            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.88.33", "Comprehensive SR");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.88.40", "Procedure Log");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.88.50", "Mammography CAD SR");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.88.59", "Key Object Selection Document");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.88.65", "Chest CAD SR");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.88.67", "X-Ray Radiation Dose SR");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.104.1", "Encapsulated PDF Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.128", "Positron Emission Tomography Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.481.1", "RT Image Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.481.2", "RT Dose Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.481.3", "RT Structure Set Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.481.4", "RT Beams Treatment Record Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.481.5", "RT Plan Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.481.6", "RT Brachy Treatment Record Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.481.7", "RT Treatment Summary Record Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.481.8", "RT Ion Plan Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.1.1.481.9", "RT Ion Beams Treatment Record Storage");
            media_storage_map.Add("1.2.840.10008.5.1.4.38.1", "Hanging Protocol Storage");
            
            data_element_map.Add(0x00000000, new TagMetaData(VRType.UL, "Group 0000 Length"));
            data_element_map.Add(0x00000001, new TagMetaData(VRType.UL, "Group 0000 Length to End (RET)"));
            data_element_map.Add(0x00000002, new TagMetaData(VRType.UI, "Affected SOP Class UID"));
            data_element_map.Add(0x00000003, new TagMetaData(VRType.UI, "Requested SOP Class UID"));
            data_element_map.Add(0x00000010, new TagMetaData(VRType.SH, "Recognition Code (RET)"));
            data_element_map.Add(0x00000100, new TagMetaData(VRType.US, "Command Field"));
            data_element_map.Add(0x00000110, new TagMetaData(VRType.US, "Message ID"));
            data_element_map.Add(0x00000120, new TagMetaData(VRType.US, "Message Id being Responded to"));
            data_element_map.Add(0x00000200, new TagMetaData(VRType.AE, "Initiator (RET)"));
            data_element_map.Add(0x00000300, new TagMetaData(VRType.AE, "Receiver (RET)"));
            data_element_map.Add(0x00000400, new TagMetaData(VRType.AE, "Find Location (RET)"));
            data_element_map.Add(0x00000600, new TagMetaData(VRType.AE, "Move Destination"));
            data_element_map.Add(0x00000700, new TagMetaData(VRType.US, "Priority"));
            data_element_map.Add(0x00000800, new TagMetaData(VRType.US, "Data Set Type"));
            data_element_map.Add(0x00000850, new TagMetaData(VRType.US, "Number of Matches (RET)"));
            data_element_map.Add(0x00000860, new TagMetaData(VRType.US, "Response Sequence Number (RET)"));
            data_element_map.Add(0x00000900, new TagMetaData(VRType.US, "Status"));
            data_element_map.Add(0x00000901, new TagMetaData(VRType.AT, "Offending Element"));
            data_element_map.Add(0x00000902, new TagMetaData(VRType.LO, "Error Comment"));
            data_element_map.Add(0x00000903, new TagMetaData(VRType.US, "Error ID"));
            data_element_map.Add(0x00001000, new TagMetaData(VRType.UI, "Affected SOP Instance UID"));
            data_element_map.Add(0x00001001, new TagMetaData(VRType.UI, "Requested SOP Instance UID"));
            data_element_map.Add(0x00001002, new TagMetaData(VRType.US, "Event Type ID"));
            data_element_map.Add(0x00001005, new TagMetaData(VRType.AT, "Attribute Identifier List"));
            data_element_map.Add(0x00001008, new TagMetaData(VRType.US, "Action Type ID"));
            data_element_map.Add(0x00001012, new TagMetaData(VRType.UI, "Requested SOP Instance UID List"));
            data_element_map.Add(0x00001020, new TagMetaData(VRType.US, "Number of Remaining Sub-operations"));
            data_element_map.Add(0x00001021, new TagMetaData(VRType.US, "Number of Completed Sub-operations"));
            data_element_map.Add(0x00001022, new TagMetaData(VRType.US, "Number of Failed Sub-operations"));
            data_element_map.Add(0x00001023, new TagMetaData(VRType.US, "Number of Warning Sub-operations"));
            data_element_map.Add(0x00001030, new TagMetaData(VRType.AE, "Move Originator Application Entity Title"));
            data_element_map.Add(0x00001031, new TagMetaData(VRType.US, "Move Originator Message ID"));
            data_element_map.Add(0x00005010, new TagMetaData(VRType.LO, "Message Set ID (RET)"));
            data_element_map.Add(0x00005020, new TagMetaData(VRType.LO, "End Message Set ID (RET)"));
            data_element_map.Add(0x00020000, new TagMetaData(VRType.UL, "Group 0002 Length"));
            data_element_map.Add(0x00020001, new TagMetaData(VRType.OB, "File Meta Information Version"));
            data_element_map.Add(0x00020002, new TagMetaData(VRType.UI, "Media Stored SOP Class UID"));
            data_element_map.Add(0x00020003, new TagMetaData(VRType.UI, "Media Stored SOP Instance UID"));
            data_element_map.Add(0x00020010, new TagMetaData(VRType.UI, "Transfer Syntax UID"));
            data_element_map.Add(0x00020012, new TagMetaData(VRType.UI, "Implementation Class UID"));
            data_element_map.Add(0x00020013, new TagMetaData(VRType.SH, "Implementation Version Name"));
            data_element_map.Add(0x00020016, new TagMetaData(VRType.AE, "Source Application Entity Title"));
            data_element_map.Add(0x00020100, new TagMetaData(VRType.UI, "Private Information Creator UID"));
            data_element_map.Add(0x00020102, new TagMetaData(VRType.OB, "Private Information"));
            data_element_map.Add(0x00040000, new TagMetaData(VRType.UL, "Group 0004 Length"));
            data_element_map.Add(0x00041130, new TagMetaData(VRType.CS, "File-set ID"));
            data_element_map.Add(0x00041141, new TagMetaData(VRType.CS, "File-set Descriptor File File ID"));
            data_element_map.Add(0x00041142, new TagMetaData(VRType.CS, "File-set Descriptor File Format"));
            data_element_map.Add(0x00041200, new TagMetaData(VRType.UL, "Root Directory Entity's First Directory Record Offset"));
            data_element_map.Add(0x00041202, new TagMetaData(VRType.UL, "Root Directory Entity's Last Directory Record Offset"));
            data_element_map.Add(0x00041212, new TagMetaData(VRType.US, "File-set Consistence Flag"));
            data_element_map.Add(0x00041220, new TagMetaData(VRType.SQ, "Directory Record Sequence"));
            data_element_map.Add(0x00041400, new TagMetaData(VRType.UL, "Next Directory Record Offset"));
            data_element_map.Add(0x00041410, new TagMetaData(VRType.US, "Record In-use Flag"));
            data_element_map.Add(0x00041420, new TagMetaData(VRType.UL, "Referenced Lower-level Directory Entity Offset"));
            data_element_map.Add(0x00041430, new TagMetaData(VRType.CS, "Directory Record Type"));
            data_element_map.Add(0x00041432, new TagMetaData(VRType.UI, "Private Record UID"));
            data_element_map.Add(0x00041500, new TagMetaData(VRType.CS, "Referenced File ID"));
            data_element_map.Add(0x00041510, new TagMetaData(VRType.UI, "Referenced SOP Class UID in File"));
            data_element_map.Add(0x00041511, new TagMetaData(VRType.UI, "Referenced SOP Instance UID in File"));
            data_element_map.Add(0x00041600, new TagMetaData(VRType.UL, "Number of References"));
            data_element_map.Add(0x00080000, new TagMetaData(VRType.UL, "Group 0008 Length"));
            data_element_map.Add(0x00080001, new TagMetaData(VRType.UL, "Group 0008 Length to End (RET)"));
            data_element_map.Add(0x00080005, new TagMetaData(VRType.CS, "Specific Character Set"));
            data_element_map.Add(0x00080008, new TagMetaData(VRType.CS, "Image Type"));
            data_element_map.Add(0x00080010, new TagMetaData(VRType.SH, "Recognition Code (RET)"));
            data_element_map.Add(0x00080012, new TagMetaData(VRType.DA, "Instance Creation Date"));
            data_element_map.Add(0x00080013, new TagMetaData(VRType.TM, "Instance Creation Time"));
            data_element_map.Add(0x00080014, new TagMetaData(VRType.UI, "Instance Creator UID"));
            data_element_map.Add(0x00080016, new TagMetaData(VRType.UI, "SOP Class UID"));
            data_element_map.Add(0x00080018, new TagMetaData(VRType.UI, "SOP Instance UID"));
            data_element_map.Add(0x00080020, new TagMetaData(VRType.DA, "Study Date"));
            data_element_map.Add(0x00080021, new TagMetaData(VRType.DA, "Series Date"));
            data_element_map.Add(0x00080022, new TagMetaData(VRType.DA, "Acquisition Date"));
            data_element_map.Add(0x00080023, new TagMetaData(VRType.DA, "Image Date"));
            data_element_map.Add(0x00080024, new TagMetaData(VRType.DA, "Overlay Date"));
            data_element_map.Add(0x00080025, new TagMetaData(VRType.DA, "Curve Date"));
            data_element_map.Add(0x00080030, new TagMetaData(VRType.TM, "Study Time"));
            data_element_map.Add(0x00080031, new TagMetaData(VRType.TM, "Series Time"));
            data_element_map.Add(0x00080032, new TagMetaData(VRType.TM, "Acquisition Time"));
            data_element_map.Add(0x00080033, new TagMetaData(VRType.TM, "Image Time"));
            data_element_map.Add(0x00080034, new TagMetaData(VRType.TM, "Overlay Time"));
            data_element_map.Add(0x00080035, new TagMetaData(VRType.TM, "Curve Time"));
            data_element_map.Add(0x00080040, new TagMetaData(VRType.US, "Data Set Type (RET)"));
            data_element_map.Add(0x00080041, new TagMetaData(VRType.SH, "Data Set Subtype (RET)"));
            data_element_map.Add(0x00080042, new TagMetaData(VRType.CS, "Nuclear Medicine Series Type"));
            data_element_map.Add(0x00080050, new TagMetaData(VRType.SH, "Accession Number"));
            data_element_map.Add(0x00080052, new TagMetaData(VRType.CS, "Query/Retrieve Level"));
            data_element_map.Add(0x00080054, new TagMetaData(VRType.AE, "Retrieve AE Title"));
            data_element_map.Add(0x00080058, new TagMetaData(VRType.AE, "Failed SOP Instance UID List"));
            data_element_map.Add(0x00080060, new TagMetaData(VRType.CS, "Modality"));
            data_element_map.Add(0x00080064, new TagMetaData(VRType.CS, "Conversion Type"));
            data_element_map.Add(0x00080070, new TagMetaData(VRType.LO, "Manufacturer"));
            data_element_map.Add(0x00080080, new TagMetaData(VRType.LO, "Institution Name"));
            data_element_map.Add(0x00080081, new TagMetaData(VRType.ST, "Institution Address"));
            data_element_map.Add(0x00080082, new TagMetaData(VRType.SQ, "Institution Code Sequence"));
            data_element_map.Add(0x00080090, new TagMetaData(VRType.PN, "Referring Physician's Name"));
            data_element_map.Add(0x00080092, new TagMetaData(VRType.ST, "Referring Physician's Address"));
            data_element_map.Add(0x00080094, new TagMetaData(VRType.SH, "Referring Physician's Telephone Numbers"));
            data_element_map.Add(0x00080100, new TagMetaData(VRType.SH, "Code Value"));
            data_element_map.Add(0x00080102, new TagMetaData(VRType.SH, "Coding Scheme Designator"));
            data_element_map.Add(0x00080104, new TagMetaData(VRType.LO, "Code Meaning"));
            data_element_map.Add(0x00081000, new TagMetaData(VRType.SH, "Network ID (RET)"));
            data_element_map.Add(0x00081010, new TagMetaData(VRType.SH, "Station Name"));
            data_element_map.Add(0x00081030, new TagMetaData(VRType.LO, "Study Description"));
            data_element_map.Add(0x00081032, new TagMetaData(VRType.SQ, "Procedure Code Sequence"));
            data_element_map.Add(0x0008103E, new TagMetaData(VRType.LO, "Series Description"));
            data_element_map.Add(0x00081040, new TagMetaData(VRType.LO, "Institutional Department Name"));
            data_element_map.Add(0x00081050, new TagMetaData(VRType.PN, "Attending Physician's Name"));
            data_element_map.Add(0x00081060, new TagMetaData(VRType.PN, "Name of Physician(s) Reading Study"));
            data_element_map.Add(0x00081070, new TagMetaData(VRType.PN, "Operator's Name"));
            data_element_map.Add(0x00081080, new TagMetaData(VRType.LO, "Admitting Diagnoses Description"));
            data_element_map.Add(0x00081084, new TagMetaData(VRType.SQ, "Admitting Diagnosis Code Sequence"));
            data_element_map.Add(0x00081090, new TagMetaData(VRType.LO, "Manufacturer's Model Name"));
            data_element_map.Add(0x00081100, new TagMetaData(VRType.SQ, "Referenced Results Sequence"));
            data_element_map.Add(0x00081110, new TagMetaData(VRType.SQ, "Referenced Study Sequence"));
            data_element_map.Add(0x00081111, new TagMetaData(VRType.SQ, "Referenced Study Component Sequence"));
            data_element_map.Add(0x00081115, new TagMetaData(VRType.SQ, "Referenced Series Sequence"));
            data_element_map.Add(0x00081120, new TagMetaData(VRType.SQ, "Referenced Patient Sequence"));
            data_element_map.Add(0x00081125, new TagMetaData(VRType.SQ, "Referenced Visit Sequence"));
            data_element_map.Add(0x00081130, new TagMetaData(VRType.SQ, "Referenced Overlay Sequence"));
            data_element_map.Add(0x00081140, new TagMetaData(VRType.SQ, "Referenced Image Sequence"));
            data_element_map.Add(0x00081145, new TagMetaData(VRType.SQ, "Referenced Curve Sequence"));
            data_element_map.Add(0x00081150, new TagMetaData(VRType.UI, "Referenced SOP Class UID"));
            data_element_map.Add(0x00081155, new TagMetaData(VRType.UI, "Referenced SOP Instance UID"));
            data_element_map.Add(0x00082111, new TagMetaData(VRType.ST, "Derivation Description"));
            data_element_map.Add(0x00082112, new TagMetaData(VRType.SQ, "Source Image Sequence"));
            data_element_map.Add(0x00082120, new TagMetaData(VRType.SH, "Stage Name"));
            data_element_map.Add(0x00082122, new TagMetaData(VRType.IS, "Stage Number"));
            data_element_map.Add(0x00082124, new TagMetaData(VRType.IS, "Number of Stages"));
            data_element_map.Add(0x00082129, new TagMetaData(VRType.IS, "Number of Event Timers"));
            data_element_map.Add(0x00082128, new TagMetaData(VRType.IS, "View Number"));
            data_element_map.Add(0x0008212A, new TagMetaData(VRType.IS, "Number of Views in Stage"));
            data_element_map.Add(0x00082130, new TagMetaData(VRType.DS, "Event Elapsed Time(s)"));
            data_element_map.Add(0x00082132, new TagMetaData(VRType.LO, "Event Timer Name(s)"));
            data_element_map.Add(0x00082142, new TagMetaData(VRType.IS, "Start Trim"));
            data_element_map.Add(0x00082143, new TagMetaData(VRType.IS, "Stop Trim"));
            data_element_map.Add(0x00082144, new TagMetaData(VRType.IS, "Recommended Display Frame Rate"));
            data_element_map.Add(0x00082200, new TagMetaData(VRType.CS, "Transducer Position"));
            data_element_map.Add(0x00082204, new TagMetaData(VRType.CS, "Transducer Orientation"));
            data_element_map.Add(0x00082208, new TagMetaData(VRType.CS, "Anatomic Structure"));
            data_element_map.Add(0x00084000, new TagMetaData(VRType.SH, "Group 0008 Comments (RET)"));
            data_element_map.Add(0x00089215, new TagMetaData(VRType.SQ, "Derivation Code Sequence"));
            data_element_map.Add(0x00090010, new TagMetaData(VRType.LO, "unknown"));
            data_element_map.Add(0x00100000, new TagMetaData(VRType.UL, "Group 0010 Length"));
            data_element_map.Add(0x00100010, new TagMetaData(VRType.PN, "Patient's Name"));
            data_element_map.Add(0x00100020, new TagMetaData(VRType.LO, "Patient ID"));
            data_element_map.Add(0x00100021, new TagMetaData(VRType.LO, "Issuer of Patient ID"));
            data_element_map.Add(0x00100030, new TagMetaData(VRType.DA, "Patient's Birth Date"));
            data_element_map.Add(0x00100032, new TagMetaData(VRType.TM, "Patient's Birth Time"));
            data_element_map.Add(0x00100040, new TagMetaData(VRType.CS, "Patient's Sex"));
            data_element_map.Add(0x00100042, new TagMetaData(VRType.SH, "Patient's Social Security Number"));
            data_element_map.Add(0x00100050, new TagMetaData(VRType.SQ, "Patient's Insurance Plan Code Sequence"));
            data_element_map.Add(0x00101000, new TagMetaData(VRType.LO, "Other Patient IDs"));
            data_element_map.Add(0x00101001, new TagMetaData(VRType.PN, "Other Patient Names"));
            data_element_map.Add(0x00101005, new TagMetaData(VRType.PN, "Patient's Maiden Name"));
            data_element_map.Add(0x00101010, new TagMetaData(VRType.AS, "Patient's Age"));
            data_element_map.Add(0x00101020, new TagMetaData(VRType.DS, "Patient's Size"));
            data_element_map.Add(0x00101030, new TagMetaData(VRType.DS, "Patient's Weight"));
            data_element_map.Add(0x00101040, new TagMetaData(VRType.LO, "Patient's Address"));
            data_element_map.Add(0x00101050, new TagMetaData(VRType.SH, "Insurance Plan Identification (RET)"));
            data_element_map.Add(0x00101060, new TagMetaData(VRType.PN, "Patient's Mother's Maiden Name"));
            data_element_map.Add(0x00101080, new TagMetaData(VRType.LO, "Military Rank"));
            data_element_map.Add(0x00101081, new TagMetaData(VRType.LO, "Branch of Service"));
            data_element_map.Add(0x00101090, new TagMetaData(VRType.LO, "Medical Record Locator"));
            data_element_map.Add(0x00102000, new TagMetaData(VRType.LO, "Medical Alerts"));
            data_element_map.Add(0x00102110, new TagMetaData(VRType.LO, "Contrast Allergies"));
            data_element_map.Add(0x00102150, new TagMetaData(VRType.LO, "Country of Residence"));
            data_element_map.Add(0x00102152, new TagMetaData(VRType.LO, "Region of Residence"));
            data_element_map.Add(0x00102154, new TagMetaData(VRType.SH, "Patient's Telephone Numbers"));
            data_element_map.Add(0x00102160, new TagMetaData(VRType.SH, "Ethnic Group"));
            data_element_map.Add(0x00102180, new TagMetaData(VRType.SH, "Occupation"));
            data_element_map.Add(0x001021A0, new TagMetaData(VRType.CS, "Smoking Status"));
            data_element_map.Add(0x001021B0, new TagMetaData(VRType.LT, "Additional Patient History"));
            data_element_map.Add(0x001021C0, new TagMetaData(VRType.US, "Pregnancy Status"));
            data_element_map.Add(0x001021D0, new TagMetaData(VRType.DA, "Last Menstrual Date"));
            data_element_map.Add(0x001021F0, new TagMetaData(VRType.LO, "Patient's Religious Preference"));
            data_element_map.Add(0x00104000, new TagMetaData(VRType.LT, "Patient Comments"));
            data_element_map.Add(0x00180000, new TagMetaData(VRType.UL, "Group 0018 Length"));
            data_element_map.Add(0x00180010, new TagMetaData(VRType.LO, "Contrast/Bolus Agent"));
            data_element_map.Add(0x00180015, new TagMetaData(VRType.CS, "Body Part Examined"));
            data_element_map.Add(0x00180020, new TagMetaData(VRType.CS, "Scanning Sequence"));
            data_element_map.Add(0x00180021, new TagMetaData(VRType.CS, "Sequence Variant"));
            data_element_map.Add(0x00180022, new TagMetaData(VRType.CS, "Scan Options"));
            data_element_map.Add(0x00180023, new TagMetaData(VRType.CS, "MR Acquisition Type"));
            data_element_map.Add(0x00180024, new TagMetaData(VRType.SH, "Sequence Name"));
            data_element_map.Add(0x00180025, new TagMetaData(VRType.CS, "Angio Flag"));
            data_element_map.Add(0x00180030, new TagMetaData(VRType.LO, "Radionuclide"));
            data_element_map.Add(0x00180031, new TagMetaData(VRType.LO, "Radiopharmaceutical"));
            data_element_map.Add(0x00180032, new TagMetaData(VRType.DS, "Energy Window Centerline"));
            data_element_map.Add(0x00180033, new TagMetaData(VRType.DS, "Energy Window Total Width"));
            data_element_map.Add(0x00180034, new TagMetaData(VRType.LO, "Intervention Drug Name"));
            data_element_map.Add(0x00180035, new TagMetaData(VRType.TM, "Intervention Drug Start Time"));
            data_element_map.Add(0x00180040, new TagMetaData(VRType.IS, "Cine Rate"));
            data_element_map.Add(0x00180050, new TagMetaData(VRType.DS, "Slice Thickness"));
            data_element_map.Add(0x00180060, new TagMetaData(VRType.DS, "KVP"));
            data_element_map.Add(0x00180070, new TagMetaData(VRType.IS, "Counts Accumulated"));
            data_element_map.Add(0x00180071, new TagMetaData(VRType.CS, "Acquisition Termination Condition"));
            data_element_map.Add(0x00180072, new TagMetaData(VRType.DS, "Effective Series Duration"));
            data_element_map.Add(0x00180080, new TagMetaData(VRType.DS, "Repetition Time"));
            data_element_map.Add(0x00180081, new TagMetaData(VRType.DS, "Echo Time"));
            data_element_map.Add(0x00180082, new TagMetaData(VRType.DS, "Inversion Time"));
            data_element_map.Add(0x00180083, new TagMetaData(VRType.DS, "Number of Averages"));
            data_element_map.Add(0x00180084, new TagMetaData(VRType.DS, "Imaging Frequency"));
            data_element_map.Add(0x00180085, new TagMetaData(VRType.SH, "Imaged Nucleus"));
            data_element_map.Add(0x00180086, new TagMetaData(VRType.IS, "Echo Numbers(s)"));
            data_element_map.Add(0x00180087, new TagMetaData(VRType.DS, "Magnetic Field Strength"));
            data_element_map.Add(0x00180088, new TagMetaData(VRType.DS, "Spacing Between Slices"));
            data_element_map.Add(0x00180089, new TagMetaData(VRType.IS, "Number of Phase Encoding Steps"));
            data_element_map.Add(0x00180090, new TagMetaData(VRType.DS, "Data Collection Diameter"));
            data_element_map.Add(0x00180091, new TagMetaData(VRType.IS, "Echo Train Length"));
            data_element_map.Add(0x00180093, new TagMetaData(VRType.DS, "Percent Sampling"));
            data_element_map.Add(0x00180094, new TagMetaData(VRType.DS, "Percent Phase Field of View"));
            data_element_map.Add(0x00180095, new TagMetaData(VRType.DS, "Pixel Bandwidth"));
            data_element_map.Add(0x00181000, new TagMetaData(VRType.LO, "Device Serial Number"));
            data_element_map.Add(0x00181004, new TagMetaData(VRType.LO, "Plate ID"));
            data_element_map.Add(0x00181010, new TagMetaData(VRType.LO, "Secondary Capture Device ID"));
            data_element_map.Add(0x00181012, new TagMetaData(VRType.DA, "Date of Secondary Capture"));
            data_element_map.Add(0x00181014, new TagMetaData(VRType.TM, "Time of Secondary Capture"));
            data_element_map.Add(0x00181016, new TagMetaData(VRType.LO, "Secondary Capture Device Manufacturer"));
            data_element_map.Add(0x00181018, new TagMetaData(VRType.LO, "Secondary Capture Device Manufacturer's Model Name"));
            data_element_map.Add(0x00181019, new TagMetaData(VRType.LO, "Secondary Capture Device Software Version(s)"));
            data_element_map.Add(0x00181020, new TagMetaData(VRType.LO, "Software Versions(s)"));
            data_element_map.Add(0x00181022, new TagMetaData(VRType.SH, "Video Image Format Acquired"));
            data_element_map.Add(0x00181023, new TagMetaData(VRType.LO, "Digital Image Format Acquired"));
            data_element_map.Add(0x00181030, new TagMetaData(VRType.LO, "Protocol Name"));
            data_element_map.Add(0x00181040, new TagMetaData(VRType.LO, "Contrast/Bolus Route"));
            data_element_map.Add(0x00181041, new TagMetaData(VRType.DS, "Contrast/Bolus Volume"));
            data_element_map.Add(0x00181042, new TagMetaData(VRType.TM, "Contrast/Bolus Start Time"));
            data_element_map.Add(0x00181043, new TagMetaData(VRType.TM, "Contrast/Bolus Stop Time"));
            data_element_map.Add(0x00181044, new TagMetaData(VRType.DS, "Contrast/Bolus Total Dose"));
            data_element_map.Add(0x00181045, new TagMetaData(VRType.IS, "Syringe Counts"));
            data_element_map.Add(0x00181050, new TagMetaData(VRType.DS, "Spatial Resolution"));
            data_element_map.Add(0x00181060, new TagMetaData(VRType.DS, "Trigger Time"));
            data_element_map.Add(0x00181061, new TagMetaData(VRType.LO, "Trigger Source or Type"));
            data_element_map.Add(0x00181062, new TagMetaData(VRType.IS, "Nominal Interval"));
            data_element_map.Add(0x00181063, new TagMetaData(VRType.DS, "Frame Time"));
            data_element_map.Add(0x00181064, new TagMetaData(VRType.LO, "Framing Type"));
            data_element_map.Add(0x00181065, new TagMetaData(VRType.DS, "Frame Time Vector"));
            data_element_map.Add(0x00181066, new TagMetaData(VRType.DS, "Frame Delay"));
            data_element_map.Add(0x00181070, new TagMetaData(VRType.LO, "Radionuclide Route"));
            data_element_map.Add(0x00181071, new TagMetaData(VRType.DS, "Radionuclide Volume"));
            data_element_map.Add(0x00181072, new TagMetaData(VRType.TM, "Radionuclide Start Time"));
            data_element_map.Add(0x00181073, new TagMetaData(VRType.TM, "Radionuclide Stop Time"));
            data_element_map.Add(0x00181074, new TagMetaData(VRType.DS, "Radionuclide Total Dose"));
            data_element_map.Add(0x00181080, new TagMetaData(VRType.CS, "Beat Rejection Flag"));
            data_element_map.Add(0x00181081, new TagMetaData(VRType.IS, "Low R-R Value"));
            data_element_map.Add(0x00181082, new TagMetaData(VRType.IS, "High R-R Value"));
            data_element_map.Add(0x00181083, new TagMetaData(VRType.IS, "Intervals Acquired"));
            data_element_map.Add(0x00181084, new TagMetaData(VRType.IS, "Intervals Rejected"));
            data_element_map.Add(0x00181085, new TagMetaData(VRType.LO, "PVC Rejection"));
            data_element_map.Add(0x00181086, new TagMetaData(VRType.IS, "Skip Beats"));
            data_element_map.Add(0x00181088, new TagMetaData(VRType.IS, "Heart Rate"));
            data_element_map.Add(0x00181090, new TagMetaData(VRType.IS, "Cardiac Number of Images"));
            data_element_map.Add(0x00181094, new TagMetaData(VRType.IS, "Trigger Window"));
            data_element_map.Add(0x00181100, new TagMetaData(VRType.DS, "Reconstruction Diameter"));
            data_element_map.Add(0x00181110, new TagMetaData(VRType.DS, "Distance Source to Detector"));
            data_element_map.Add(0x00181111, new TagMetaData(VRType.DS, "Distance Source to Patient"));
            data_element_map.Add(0x00181120, new TagMetaData(VRType.DS, "Gantry/Detector Tilt"));
            data_element_map.Add(0x00181130, new TagMetaData(VRType.DS, "Table Height"));
            data_element_map.Add(0x00181131, new TagMetaData(VRType.DS, "Table Traverse"));
            data_element_map.Add(0x00181140, new TagMetaData(VRType.CS, "Rotation Direction"));
            data_element_map.Add(0x00181141, new TagMetaData(VRType.DS, "Angular Position"));
            data_element_map.Add(0x00181142, new TagMetaData(VRType.DS, "Radial Position"));
            data_element_map.Add(0x00181143, new TagMetaData(VRType.DS, "Scan Arc"));
            data_element_map.Add(0x00181144, new TagMetaData(VRType.DS, "Angular Step"));
            data_element_map.Add(0x00181145, new TagMetaData(VRType.DS, "Center of Rotation Offset"));
            data_element_map.Add(0x00181146, new TagMetaData(VRType.DS, "Rotation Offset"));
            data_element_map.Add(0x00181147, new TagMetaData(VRType.CS, "Field of View Shape"));
            data_element_map.Add(0x00181149, new TagMetaData(VRType.IS, "Field of View Dimensions(s)"));
            data_element_map.Add(0x00181150, new TagMetaData(VRType.IS, "Exposure Time"));
            data_element_map.Add(0x00181151, new TagMetaData(VRType.IS, "X-ray Tube Current"));
            data_element_map.Add(0x00181152, new TagMetaData(VRType.IS, "Exposure"));
            data_element_map.Add(0x00181160, new TagMetaData(VRType.SH, "Filter Type"));
            data_element_map.Add(0x00181170, new TagMetaData(VRType.IS, "Generator Power"));
            data_element_map.Add(0x00181180, new TagMetaData(VRType.SH, "Collimator/grid Name"));
            data_element_map.Add(0x00181181, new TagMetaData(VRType.CS, "Collimator Type"));
            data_element_map.Add(0x00181182, new TagMetaData(VRType.IS, "Focal Distance"));
            data_element_map.Add(0x00181183, new TagMetaData(VRType.DS, "X Focus Center"));
            data_element_map.Add(0x00181184, new TagMetaData(VRType.DS, "Y Focus Center"));
            data_element_map.Add(0x00181190, new TagMetaData(VRType.DS, "Focal Spot(s)"));
            data_element_map.Add(0x00181200, new TagMetaData(VRType.DA, "Date of Last Calibration"));
            data_element_map.Add(0x00181201, new TagMetaData(VRType.TM, "Time of Last Calibration"));
            data_element_map.Add(0x00181210, new TagMetaData(VRType.SH, "Convolution Kernel"));
            data_element_map.Add(0x00181240, new TagMetaData(VRType.DS, "Upper/Lower Pixel Values (RET)"));
            data_element_map.Add(0x00181242, new TagMetaData(VRType.IS, "Actual Frame Duration"));
            data_element_map.Add(0x00181243, new TagMetaData(VRType.IS, "Count Rate"));
            data_element_map.Add(0x00181250, new TagMetaData(VRType.SH, "Receiving Coil"));
            data_element_map.Add(0x00181251, new TagMetaData(VRType.SH, "Transmitting Coil"));
            data_element_map.Add(0x00181260, new TagMetaData(VRType.SH, "Screen Type"));
            data_element_map.Add(0x00181261, new TagMetaData(VRType.LO, "Phosphor Type"));
            data_element_map.Add(0x00181300, new TagMetaData(VRType.IS, "Scan Velocity"));
            data_element_map.Add(0x00181301, new TagMetaData(VRType.CS, "Whole Body Technique"));
            data_element_map.Add(0x00181302, new TagMetaData(VRType.IS, "Scan Length"));
            data_element_map.Add(0x00181310, new TagMetaData(VRType.US, "Acquisition Matrix"));
            data_element_map.Add(0x00181312, new TagMetaData(VRType.CS, "Phase Encoding Direction"));
            data_element_map.Add(0x00181314, new TagMetaData(VRType.DS, "Flip Angle"));
            data_element_map.Add(0x00181315, new TagMetaData(VRType.CS, "Variable Flip Angle Flag"));
            data_element_map.Add(0x00181316, new TagMetaData(VRType.DS, "SAR"));
            data_element_map.Add(0x00181318, new TagMetaData(VRType.DS, "dB/dt"));
            data_element_map.Add(0x00181400, new TagMetaData(VRType.LO, "Acquisition Device Processing Description"));
            data_element_map.Add(0x00181401, new TagMetaData(VRType.LO, "Acquisition Device Processing Code"));
            data_element_map.Add(0x00181402, new TagMetaData(VRType.CS, "Cassette Orientation"));
            data_element_map.Add(0x00181403, new TagMetaData(VRType.CS, "Cassette Size"));
            data_element_map.Add(0x00181404, new TagMetaData(VRType.US, "Exposures on Plate"));
            data_element_map.Add(0x00181405, new TagMetaData(VRType.IS, "Relative X-ray Exposure"));
            data_element_map.Add(0x00184000, new TagMetaData(VRType.SH, "Group 0018 Comments (RET)"));
            data_element_map.Add(0x00185000, new TagMetaData(VRType.SH, "Output Power"));
            data_element_map.Add(0x00185010, new TagMetaData(VRType.LO, "Transducer Data"));
            data_element_map.Add(0x00185012, new TagMetaData(VRType.DS, "Focus Depth"));
            data_element_map.Add(0x00185020, new TagMetaData(VRType.LO, "Preprocessing Function"));
            data_element_map.Add(0x00185021, new TagMetaData(VRType.LO, "Postprocessing Function"));
            data_element_map.Add(0x00185022, new TagMetaData(VRType.DS, "Mechanical Index"));
            data_element_map.Add(0x00185024, new TagMetaData(VRType.DS, "Thermal Index"));
            data_element_map.Add(0x00185026, new TagMetaData(VRType.DS, "Cranial Thermal Index"));
            data_element_map.Add(0x00185027, new TagMetaData(VRType.DS, "Soft Tissue Thermal Index"));
            data_element_map.Add(0x00185028, new TagMetaData(VRType.DS, "Soft Tissue-focus Thermal Index"));
            data_element_map.Add(0x00185029, new TagMetaData(VRType.DS, "Soft Tissue-surface Thermal Index"));
            data_element_map.Add(0x00185030, new TagMetaData(VRType.IS, "Dynamic Range (RET)"));
            data_element_map.Add(0x00185040, new TagMetaData(VRType.IS, "Total Gain (RET)"));
            data_element_map.Add(0x00185050, new TagMetaData(VRType.IS, "Depth of Scan Field"));
            data_element_map.Add(0x00185100, new TagMetaData(VRType.CS, "Patient Position"));
            data_element_map.Add(0x00185101, new TagMetaData(VRType.CS, "View Position"));
            data_element_map.Add(0x00185210, new TagMetaData(VRType.DS, "Image Transformation Matrix"));
            data_element_map.Add(0x00185212, new TagMetaData(VRType.DS, "Image Translation Vector"));
            data_element_map.Add(0x00186000, new TagMetaData(VRType.DS, "Sensitivity"));
            data_element_map.Add(0x00186011, new TagMetaData(VRType.SQ, "Sequence of Ultrasound Regions"));
            data_element_map.Add(0x00186012, new TagMetaData(VRType.US, "Region Spatial Format"));
            data_element_map.Add(0x00186014, new TagMetaData(VRType.US, "Region Data Type"));
            data_element_map.Add(0x00186016, new TagMetaData(VRType.UL, "Region Flags"));
            data_element_map.Add(0x00186018, new TagMetaData(VRType.UL, "Region Location Min X0"));
            data_element_map.Add(0x0018601A, new TagMetaData(VRType.UL, "Region Location Min Y0"));
            data_element_map.Add(0x0018601C, new TagMetaData(VRType.UL, "Region Location Max X1"));
            data_element_map.Add(0x0018601E, new TagMetaData(VRType.UL, "Region Location Max Y1"));
            data_element_map.Add(0x00186020, new TagMetaData(VRType.SL, "Reference Pixel X0"));
            data_element_map.Add(0x00186022, new TagMetaData(VRType.SL, "Reference Pixel Y0"));
            data_element_map.Add(0x00186024, new TagMetaData(VRType.US, "Physical Units X Direction"));
            data_element_map.Add(0x00186026, new TagMetaData(VRType.US, "Physical Units Y Direction"));
            data_element_map.Add(0x00181628, new TagMetaData(VRType.FD, "Reference Pixel Physical Value X"));
            data_element_map.Add(0x0018602A, new TagMetaData(VRType.FD, "Reference Pixel Physical Value Y"));
            data_element_map.Add(0x0018602C, new TagMetaData(VRType.FD, "Physical Delta X"));
            data_element_map.Add(0x0018602E, new TagMetaData(VRType.FD, "Physical Delta Y"));
            data_element_map.Add(0x00186030, new TagMetaData(VRType.UL, "Transducer Frequency"));
            data_element_map.Add(0x00186031, new TagMetaData(VRType.CS, "Transducer Type"));
            data_element_map.Add(0x00186032, new TagMetaData(VRType.UL, "Pulse Repetition Frequency"));
            data_element_map.Add(0x00186034, new TagMetaData(VRType.FD, "Doppler Correction Angle"));
            data_element_map.Add(0x00186036, new TagMetaData(VRType.FD, "Sterring Angle"));
            data_element_map.Add(0x00186038, new TagMetaData(VRType.UL, "Doppler Sample Volume X Position"));
            data_element_map.Add(0x0018603A, new TagMetaData(VRType.UL, "Doppler Sample Volume Y Position"));
            data_element_map.Add(0x0018603C, new TagMetaData(VRType.UL, "TM-Line Position X0"));
            data_element_map.Add(0x0018603E, new TagMetaData(VRType.UL, "TM-Line Position Y0"));
            data_element_map.Add(0x00186040, new TagMetaData(VRType.UL, "TM-Line Position X1"));
            data_element_map.Add(0x00186042, new TagMetaData(VRType.UL, "TM-Line Position Y1"));
            data_element_map.Add(0x00186044, new TagMetaData(VRType.US, "Pixel Component Organization"));
            data_element_map.Add(0x00186046, new TagMetaData(VRType.UL, "Pixel Component Organization"));
            data_element_map.Add(0x00186048, new TagMetaData(VRType.UL, "Pixel Component Range Start"));
            data_element_map.Add(0x0018604A, new TagMetaData(VRType.UL, "Pixel Component Range Stop"));
            data_element_map.Add(0x0018604C, new TagMetaData(VRType.US, "Pixel Component Physical Units"));
            data_element_map.Add(0x0018604E, new TagMetaData(VRType.US, "Pixel Component Data Type"));
            data_element_map.Add(0x00186050, new TagMetaData(VRType.UL, "Number of Table Break Points"));
            data_element_map.Add(0x00186052, new TagMetaData(VRType.UL, "Table of X Break Points"));
            data_element_map.Add(0x00186054, new TagMetaData(VRType.FD, "Table of Y Break Points"));
            data_element_map.Add(0x00200000, new TagMetaData(VRType.UL, "Group 0020 Length"));
            data_element_map.Add(0x0020000D, new TagMetaData(VRType.UI, "Study Instance UID"));
            data_element_map.Add(0x0020000E, new TagMetaData(VRType.UI, "Series Instance UID"));
            data_element_map.Add(0x00200010, new TagMetaData(VRType.SH, "Study ID"));
            data_element_map.Add(0x00200011, new TagMetaData(VRType.IS, "Series Number"));
            data_element_map.Add(0x00200012, new TagMetaData(VRType.IS, "Scquisition Number"));
            data_element_map.Add(0x00200013, new TagMetaData(VRType.IS, "Image Number"));
            data_element_map.Add(0x00200014, new TagMetaData(VRType.IS, "Isotope Number"));
            data_element_map.Add(0x00200015, new TagMetaData(VRType.IS, "Phase Number"));
            data_element_map.Add(0x00200016, new TagMetaData(VRType.IS, "Interval Number"));
            data_element_map.Add(0x00200017, new TagMetaData(VRType.IS, "Time Slot Number"));
            data_element_map.Add(0x00200018, new TagMetaData(VRType.IS, "Angle Number"));
            data_element_map.Add(0x00200020, new TagMetaData(VRType.CS, "Patient Orientation"));
            data_element_map.Add(0x00200022, new TagMetaData(VRType.US, "Overlay Number"));
            data_element_map.Add(0x00200024, new TagMetaData(VRType.US, "Curve Number"));
            data_element_map.Add(0x00200030, new TagMetaData(VRType.DS, "Image Position (RET)"));
            data_element_map.Add(0x00200032, new TagMetaData(VRType.DS, "Image Position (Patient)"));
            data_element_map.Add(0x00200035, new TagMetaData(VRType.DS, "Image Orientation (RET)"));
            data_element_map.Add(0x00200037, new TagMetaData(VRType.DS, "Image Orientation (Patient)"));
            data_element_map.Add(0x00200050, new TagMetaData(VRType.DS, "Location (RET)"));
            data_element_map.Add(0x00200052, new TagMetaData(VRType.UI, "Frame of Reference UID"));
            data_element_map.Add(0x00200060, new TagMetaData(VRType.CS, "Laterality"));
            data_element_map.Add(0x00200070, new TagMetaData(VRType.SH, "Image Geometry Type (RET)"));
            data_element_map.Add(0x00200080, new TagMetaData(VRType.UI, "Masking Image UID"));
            data_element_map.Add(0x00200100, new TagMetaData(VRType.IS, "Temporal Position Identifier"));
            data_element_map.Add(0x00200105, new TagMetaData(VRType.IS, "Number of Temporal Positions"));
            data_element_map.Add(0x00200110, new TagMetaData(VRType.DS, "Temporal Resolution"));
            data_element_map.Add(0x00201000, new TagMetaData(VRType.IS, "Series in Study"));
            data_element_map.Add(0x00201001, new TagMetaData(VRType.IS, "Acquisitions in Series (RET)"));
            data_element_map.Add(0x00201002, new TagMetaData(VRType.IS, "Images in Acquisition"));
            data_element_map.Add(0x00201004, new TagMetaData(VRType.IS, "Acquisition in Study"));
            data_element_map.Add(0x00201020, new TagMetaData(VRType.SH, "Reference (RET)"));
            data_element_map.Add(0x00201040, new TagMetaData(VRType.LO, "Position Reference Indicator"));
            data_element_map.Add(0x00201041, new TagMetaData(VRType.DS, "Slice Location"));
            data_element_map.Add(0x00201070, new TagMetaData(VRType.IS, "Other Study Numbers"));
            data_element_map.Add(0x00201200, new TagMetaData(VRType.IS, "Number of Patient Related Studies"));
            data_element_map.Add(0x00201202, new TagMetaData(VRType.IS, "Number of Patient Related Series"));
            data_element_map.Add(0x00201204, new TagMetaData(VRType.IS, "Number of Patient Related Images"));
            data_element_map.Add(0x00201206, new TagMetaData(VRType.IS, "Number of Study Related Series"));
            data_element_map.Add(0x00201208, new TagMetaData(VRType.IS, "Number of Study Related Images"));
            data_element_map.Add(0x00203100, new TagMetaData(VRType.SH, "Source Image ID (RET)s"));
            data_element_map.Add(0x00203401, new TagMetaData(VRType.SH, "Modifying Device ID (RET)"));
            data_element_map.Add(0x00203402, new TagMetaData(VRType.SH, "Modified Image ID (RET)"));
            data_element_map.Add(0x00203403, new TagMetaData(VRType.SH, "Modified Image Date (RET)"));
            data_element_map.Add(0x00203404, new TagMetaData(VRType.SH, "Modifying Device Manufacturer (RET)"));
            data_element_map.Add(0x00203405, new TagMetaData(VRType.SH, "Modified Image Time (RET)"));
            data_element_map.Add(0x00203406, new TagMetaData(VRType.SH, "Modified Image Description (RET)"));
            data_element_map.Add(0x00204000, new TagMetaData(VRType.LT, "Image Comments"));
            data_element_map.Add(0x00205000, new TagMetaData(VRType.US, "Original Image Identification (RET)"));
            data_element_map.Add(0x00205002, new TagMetaData(VRType.SH, "Original Image Identification Nomenclature (RET)"));
            data_element_map.Add(0x00280000, new TagMetaData(VRType.UL, "Group 0028 Length"));
            data_element_map.Add(0x00280002, new TagMetaData(VRType.US, "Samples per Pixel"));
            data_element_map.Add(0x00280004, new TagMetaData(VRType.CS, "Photometric Interpretation"));
            data_element_map.Add(0x00280005, new TagMetaData(VRType.US, "Image Dimensions (RET)"));
            data_element_map.Add(0x00280006, new TagMetaData(VRType.US, "Planar Configuration"));
            data_element_map.Add(0x00280008, new TagMetaData(VRType.IS, "Number of Frames"));
            data_element_map.Add(0x00280009, new TagMetaData(VRType.AT, "Frame Increment Pointer"));
            data_element_map.Add(0x00280010, new TagMetaData(VRType.US, "Rows"));
            data_element_map.Add(0x00280011, new TagMetaData(VRType.US, "Columns"));
            data_element_map.Add(0x00280030, new TagMetaData(VRType.DS, "Pixel Spacing"));
            data_element_map.Add(0x00280031, new TagMetaData(VRType.DS, "Zoom Factor"));
            data_element_map.Add(0x00280032, new TagMetaData(VRType.DS, "Zoom Center"));
            data_element_map.Add(0x00280034, new TagMetaData(VRType.IS, "Pixel Aspect Ratio"));
            data_element_map.Add(0x00280040, new TagMetaData(VRType.SH, "Image Format (RET)"));
            data_element_map.Add(0x00280050, new TagMetaData(VRType.SH, "Manipulated Image (RET)"));
            data_element_map.Add(0x00280051, new TagMetaData(VRType.CS, "Corrected Image"));
            data_element_map.Add(0x00280060, new TagMetaData(VRType.SH, "Compression Code (RET)"));
            data_element_map.Add(0x00280100, new TagMetaData(VRType.US, "Bits Allocated"));
            data_element_map.Add(0x00280101, new TagMetaData(VRType.US, "Bits Stored"));
            data_element_map.Add(0x00280102, new TagMetaData(VRType.US, "High Bit"));
            data_element_map.Add(0x00280103, new TagMetaData(VRType.US, "Pixel Representation"));
            data_element_map.Add(0x00280104, new TagMetaData(VRType.US, "Smallest Valid Pixel Value (RET)"));
            data_element_map.Add(0x00280105, new TagMetaData(VRType.US, "Largest Valid Pixel Value (RET)"));
            data_element_map.Add(0x00280106, new TagMetaData(VRType.US, "Smallest Image Pixel Value"));
            data_element_map.Add(0x00280107, new TagMetaData(VRType.US, "Largest Image Pixel Value"));
            data_element_map.Add(0x00280108, new TagMetaData(VRType.US, "Smallest Pixel Value in Series"));
            data_element_map.Add(0x00280109, new TagMetaData(VRType.US, "Largest Pixel Value in Series"));
            data_element_map.Add(0x00280120, new TagMetaData(VRType.US, "Pixel Padding Value"));
            data_element_map.Add(0x00280200, new TagMetaData(VRType.US, "Image Location (RET)"));
            data_element_map.Add(0x00281050, new TagMetaData(VRType.DS, "Window Center"));
            data_element_map.Add(0x00281051, new TagMetaData(VRType.DS, "Window Width"));
            data_element_map.Add(0x00281052, new TagMetaData(VRType.DS, "Rescale Intercept"));
            data_element_map.Add(0x00281053, new TagMetaData(VRType.DS, "Rescale Slope"));
            data_element_map.Add(0x00281054, new TagMetaData(VRType.LO, "Rescale Type"));
            data_element_map.Add(0x00281055, new TagMetaData(VRType.LO, "Window Center & Width Explanation"));
            data_element_map.Add(0x00281080, new TagMetaData(VRType.SH, "Gray Scale (RET)"));
            data_element_map.Add(0x00281100, new TagMetaData(VRType.US, "Gray Lookup Table Descriptor (RET)"));
            data_element_map.Add(0x00281101, new TagMetaData(VRType.US, "Red Palette Color Lookup Table Descriptor"));
            data_element_map.Add(0x00281102, new TagMetaData(VRType.US, "Green Palette Color Lookup Table Descriptor"));
            data_element_map.Add(0x00281103, new TagMetaData(VRType.US, "Blue Palette Color Lookup Table Descriptor"));
            data_element_map.Add(0x00281200, new TagMetaData(VRType.US, "Gray Lookup Table Data (RET)"));
            data_element_map.Add(0x00281201, new TagMetaData(VRType.US, "Red Palette Color Lookup Table Data"));
            data_element_map.Add(0x00281202, new TagMetaData(VRType.US, "Green Palette Color Lookup Table Data"));
            data_element_map.Add(0x00281203, new TagMetaData(VRType.US, "Blue Palette Color Lookup Table Data"));
            data_element_map.Add(0x00283000, new TagMetaData(VRType.SQ, "Modality LUT Sequence"));
            data_element_map.Add(0x00283002, new TagMetaData(VRType.US, "LUT Descriptor"));
            data_element_map.Add(0x00283003, new TagMetaData(VRType.LO, "LUT Explanation"));
            data_element_map.Add(0x00283004, new TagMetaData(VRType.LO, "Madality LUT Type"));
            data_element_map.Add(0x00283006, new TagMetaData(VRType.US, "LUT Data"));
            data_element_map.Add(0x00283010, new TagMetaData(VRType.SQ, "VOI LUT Sequence"));
            data_element_map.Add(0x00284000, new TagMetaData(VRType.SH, "Group 0028 Comments (RET)"));
            data_element_map.Add(0x00320000, new TagMetaData(VRType.UL, "Group 0032 Length"));
            data_element_map.Add(0x0032000A, new TagMetaData(VRType.CS, "Study Status ID"));
            data_element_map.Add(0x0032000C, new TagMetaData(VRType.CS, "Study Priority ID"));
            data_element_map.Add(0x00320012, new TagMetaData(VRType.LO, "Study ID Issuer"));
            data_element_map.Add(0x00320032, new TagMetaData(VRType.DA, "Study Verified Date"));
            data_element_map.Add(0x00320033, new TagMetaData(VRType.TM, "Study Verified Time"));
            data_element_map.Add(0x00320034, new TagMetaData(VRType.DA, "Study Read Date"));
            data_element_map.Add(0x00320035, new TagMetaData(VRType.TM, "Study Read Time"));
            data_element_map.Add(0x00321000, new TagMetaData(VRType.DA, "Scheduled Study Start Date"));
            data_element_map.Add(0x00321001, new TagMetaData(VRType.TM, "Scheduled Study Start Time"));
            data_element_map.Add(0x00321010, new TagMetaData(VRType.DA, "Scheduled Study Stop Date"));
            data_element_map.Add(0x00321011, new TagMetaData(VRType.TM, "Scheduled Study Stop Time"));
            data_element_map.Add(0x00321020, new TagMetaData(VRType.LO, "Scheduled Study Location"));
            data_element_map.Add(0x00321021, new TagMetaData(VRType.AE, "Scheduled Study Location AE Title(s)"));
            data_element_map.Add(0x00321030, new TagMetaData(VRType.LO, "Reason  for Study"));
            data_element_map.Add(0x00321032, new TagMetaData(VRType.PN, "Requesting Physician"));
            data_element_map.Add(0x00321033, new TagMetaData(VRType.LO, "Requesting Service"));
            data_element_map.Add(0x00321040, new TagMetaData(VRType.DA, "Study Arrival Date"));
            data_element_map.Add(0x00321041, new TagMetaData(VRType.TM, "Study Arrival Time"));
            data_element_map.Add(0x00321050, new TagMetaData(VRType.DA, "Study Completion Date"));
            data_element_map.Add(0x00321051, new TagMetaData(VRType.TM, "Study Completion Time"));
            data_element_map.Add(0x00321055, new TagMetaData(VRType.CS, "Study Component Status ID"));
            data_element_map.Add(0x00321060, new TagMetaData(VRType.LO, "Requested Procedure Description"));
            data_element_map.Add(0x00321064, new TagMetaData(VRType.SQ, "Requested Procedure Code Sequence"));
            data_element_map.Add(0x00321070, new TagMetaData(VRType.LO, "Requested Contrast Agent"));
            data_element_map.Add(0x00324000, new TagMetaData(VRType.LT, "Study Comments"));
            data_element_map.Add(0x00380000, new TagMetaData(VRType.UL, "Group 0038 Length"));
            data_element_map.Add(0x00380004, new TagMetaData(VRType.SQ, "Referenced Patient Alias Sequence"));
            data_element_map.Add(0x00380008, new TagMetaData(VRType.CS, "Visit Status ID"));
            data_element_map.Add(0x00380010, new TagMetaData(VRType.LO, "Admissin ID"));
            data_element_map.Add(0x00380011, new TagMetaData(VRType.LO, "Issuer of Admission ID"));
            data_element_map.Add(0x00380016, new TagMetaData(VRType.LO, "Route of Admissions"));
            data_element_map.Add(0x0038001A, new TagMetaData(VRType.DA, "Scheduled Admissin Date"));
            data_element_map.Add(0x0038001B, new TagMetaData(VRType.TM, "Scheduled Adission Time"));
            data_element_map.Add(0x0038001C, new TagMetaData(VRType.DA, "Scheduled Discharge Date"));
            data_element_map.Add(0x0038001D, new TagMetaData(VRType.TM, "Scheduled Discharge Time"));
            data_element_map.Add(0x0038001E, new TagMetaData(VRType.LO, "Scheduled Patient Institution Residence"));
            data_element_map.Add(0x00380020, new TagMetaData(VRType.DA, "Admitting Date"));
            data_element_map.Add(0x00380021, new TagMetaData(VRType.TM, "Admitting Time"));
            data_element_map.Add(0x00380030, new TagMetaData(VRType.DA, "Discharge Date"));
            data_element_map.Add(0x00380032, new TagMetaData(VRType.TM, "Discharge Time"));
            data_element_map.Add(0x00380040, new TagMetaData(VRType.LO, "Discharge Diagnosis Description"));
            data_element_map.Add(0x00380044, new TagMetaData(VRType.SQ, "Discharge Diagnosis Code Sequence"));
            data_element_map.Add(0x00380050, new TagMetaData(VRType.LO, "Special Needs"));
            data_element_map.Add(0x00380300, new TagMetaData(VRType.LO, "Current Patient Location"));
            data_element_map.Add(0x00380400, new TagMetaData(VRType.LO, "Patient's Institution Residence"));
            data_element_map.Add(0x00380500, new TagMetaData(VRType.LO, "Patient State"));
            data_element_map.Add(0x00384000, new TagMetaData(VRType.LT, "Visit Comments"));
            data_element_map.Add(0x00880000, new TagMetaData(VRType.UL, "Group 0088 Length"));
            data_element_map.Add(0x00880130, new TagMetaData(VRType.SH, "Storage Media File-set ID"));
            data_element_map.Add(0x00880140, new TagMetaData(VRType.UI, "Storage Media File-set UID"));
            data_element_map.Add(0x20000000, new TagMetaData(VRType.UL, "Group 2000 Length"));
            data_element_map.Add(0x20000010, new TagMetaData(VRType.IS, "Number of Copies"));
            data_element_map.Add(0x20000020, new TagMetaData(VRType.CS, "Print Priority"));
            data_element_map.Add(0x20000030, new TagMetaData(VRType.CS, "Medium Type"));
            data_element_map.Add(0x20000040, new TagMetaData(VRType.CS, "Film Destination"));
            data_element_map.Add(0x20000050, new TagMetaData(VRType.LO, "Film Session Label"));
            data_element_map.Add(0x20000060, new TagMetaData(VRType.IS, "Memory Allocation"));
            data_element_map.Add(0x20000500, new TagMetaData(VRType.SQ, "Referenced Film Box Sequence"));
            data_element_map.Add(0x20100000, new TagMetaData(VRType.UL, "Group 2010 Length"));
            data_element_map.Add(0x20100010, new TagMetaData(VRType.ST, "Smage Display Format"));
            data_element_map.Add(0x20100030, new TagMetaData(VRType.CS, "Annotation Display Format ID"));
            data_element_map.Add(0x20100040, new TagMetaData(VRType.CS, "Film Orientation"));
            data_element_map.Add(0x20100050, new TagMetaData(VRType.CS, "Film Size ID"));
            data_element_map.Add(0x20100060, new TagMetaData(VRType.CS, "Magnification Type"));
            data_element_map.Add(0x20100080, new TagMetaData(VRType.CS, "Smoothing Type"));
            data_element_map.Add(0x20100100, new TagMetaData(VRType.CS, "Border Density"));
            data_element_map.Add(0x20100110, new TagMetaData(VRType.CS, "Empty Image Density"));
            data_element_map.Add(0x20100120, new TagMetaData(VRType.US, "Min Density"));
            data_element_map.Add(0x20100130, new TagMetaData(VRType.US, "Max Density"));
            data_element_map.Add(0x20100140, new TagMetaData(VRType.CS, "Trim"));
            data_element_map.Add(0x20100150, new TagMetaData(VRType.ST, "Configuration Information"));
            data_element_map.Add(0x20100500, new TagMetaData(VRType.SQ, "Referenced Film Session Sequence"));
            data_element_map.Add(0x20100510, new TagMetaData(VRType.SQ, "Referenced Basic Image Box Sequence"));
            data_element_map.Add(0x20100520, new TagMetaData(VRType.SQ, "Referenced Basic Annotation Box Sequence"));
            data_element_map.Add(0x20200000, new TagMetaData(VRType.UL, "Group 2020 Length"));
            data_element_map.Add(0x20200010, new TagMetaData(VRType.US, "Image Position"));
            data_element_map.Add(0x20200020, new TagMetaData(VRType.CS, "Polarity"));
            data_element_map.Add(0x20200030, new TagMetaData(VRType.DS, "Requested Image Size"));
            data_element_map.Add(0x20200110, new TagMetaData(VRType.SQ, "Preformatted Greyscale Image Sequence"));
            data_element_map.Add(0x20200111, new TagMetaData(VRType.SQ, "Preformatted Color Image Sequence"));
            data_element_map.Add(0x20200130, new TagMetaData(VRType.SQ, "Referenced Image Overlay Box Sequence"));
            data_element_map.Add(0x20200140, new TagMetaData(VRType.SQ, "Referenced VOI LUT Sequence"));
            data_element_map.Add(0x20300000, new TagMetaData(VRType.UL, "Group 2030 Length"));
            data_element_map.Add(0x20300010, new TagMetaData(VRType.US, "Annotation Position"));
            data_element_map.Add(0x20300020, new TagMetaData(VRType.LO, "Text Object"));
            data_element_map.Add(0x20400000, new TagMetaData(VRType.UL, "Group 2040 Length"));
            data_element_map.Add(0x20400010, new TagMetaData(VRType.SQ, "Referenced Overlay Plane Sequence"));
            data_element_map.Add(0x20400011, new TagMetaData(VRType.US, "Refenced Overlay Plane Groups"));
            data_element_map.Add(0x20400060, new TagMetaData(VRType.CS, "Overlay Magnification Type"));
            data_element_map.Add(0x20400070, new TagMetaData(VRType.CS, "Overlay Smoothing Type"));
            data_element_map.Add(0x20400080, new TagMetaData(VRType.CS, "Overlay Foreground Density"));
            data_element_map.Add(0x20400090, new TagMetaData(VRType.CS, "overlay Mode"));
            data_element_map.Add(0x20400100, new TagMetaData(VRType.CS, "Threshold Density"));
            data_element_map.Add(0x20400500, new TagMetaData(VRType.SQ, "Referenced Image Box Sequence"));
            data_element_map.Add(0x21000000, new TagMetaData(VRType.UL, "Group 2100 Length"));
            data_element_map.Add(0x21000020, new TagMetaData(VRType.CS, "Execution Status"));
            data_element_map.Add(0x21000030, new TagMetaData(VRType.CS, "Execution Status Info"));
            data_element_map.Add(0x21000040, new TagMetaData(VRType.DA, "Creation Date"));
            data_element_map.Add(0x21000050, new TagMetaData(VRType.TM, "Creation Time"));
            data_element_map.Add(0x21000070, new TagMetaData(VRType.AE, "Originator"));
            data_element_map.Add(0x21000500, new TagMetaData(VRType.SQ, "Referenced Print Job Sequence"));
            data_element_map.Add(0x21100000, new TagMetaData(VRType.UL, "Group 2110 Length"));
            data_element_map.Add(0x21100010, new TagMetaData(VRType.CS, "Printer Status"));
            data_element_map.Add(0x21100020, new TagMetaData(VRType.CS, "Printer Status Info"));
            data_element_map.Add(0x21100030, new TagMetaData(VRType.ST, "Printer Name"));
            data_element_map.Add(0x40000000, new TagMetaData(VRType.UL, "Group 4000 Length (RET)"));
            data_element_map.Add(0x40000010, new TagMetaData(VRType.SH, "Arbitray (RET)"));
            data_element_map.Add(0x40004000, new TagMetaData(VRType.LT, "Group 4000 Comments (RET)"));
            data_element_map.Add(0x40080000, new TagMetaData(VRType.UL, "Group 4008 Length"));
            data_element_map.Add(0x40080040, new TagMetaData(VRType.SH, "Results ID"));
            data_element_map.Add(0x40080042, new TagMetaData(VRType.LO, "Results ID Issuer"));
            data_element_map.Add(0x40080050, new TagMetaData(VRType.SQ, "Referenced Interpretation Sequence"));
            data_element_map.Add(0x40080100, new TagMetaData(VRType.DA, "Interpretation Recorded Date"));
            data_element_map.Add(0x40080101, new TagMetaData(VRType.TM, "Interpretation Recorded Time"));
            data_element_map.Add(0x40080102, new TagMetaData(VRType.PN, "Interpretation Recorder"));
            data_element_map.Add(0x40080103, new TagMetaData(VRType.LO, "Reference to Recorded Sound"));
            data_element_map.Add(0x40080108, new TagMetaData(VRType.DA, "Interpretation Transcription Time"));
            data_element_map.Add(0x40080109, new TagMetaData(VRType.TM, "Interpretation Transcription Time"));
            data_element_map.Add(0x4008010A, new TagMetaData(VRType.PN, "Interpretation Transcriber"));
            data_element_map.Add(0x4008010B, new TagMetaData(VRType.ST, "Interpretation Text"));
            data_element_map.Add(0x4008010C, new TagMetaData(VRType.PN, "Interpretation Author"));
            data_element_map.Add(0x40080111, new TagMetaData(VRType.SQ, "Interpretation Approver Sequence"));
            data_element_map.Add(0x40080112, new TagMetaData(VRType.DA, "Interpretation Approval Date"));
            data_element_map.Add(0x40080113, new TagMetaData(VRType.TM, "Interpretation Approval Time"));
            data_element_map.Add(0x40080114, new TagMetaData(VRType.PN, "Physician Approving Interpretation"));
            data_element_map.Add(0x40080115, new TagMetaData(VRType.LT, "Interpretation Diagnosis Description"));
            data_element_map.Add(0x40080117, new TagMetaData(VRType.SQ, "Diagnosis Code Sequence"));
            data_element_map.Add(0x40080118, new TagMetaData(VRType.SQ, "Results Distribution List Sequence"));
            data_element_map.Add(0x40080119, new TagMetaData(VRType.PN, "Distribution Name"));
            data_element_map.Add(0x4008011A, new TagMetaData(VRType.LO, "Distribution Address"));
            data_element_map.Add(0x40080200, new TagMetaData(VRType.SH, "Interpretation ID"));
            data_element_map.Add(0x40080202, new TagMetaData(VRType.LO, "Interpretation ID Issuer"));
            data_element_map.Add(0x40080210, new TagMetaData(VRType.CS, "Interpretation Type ID"));
            data_element_map.Add(0x40080212, new TagMetaData(VRType.CS, "Interpretation Status ID"));
            data_element_map.Add(0x40080300, new TagMetaData(VRType.ST, "Impression"));
            data_element_map.Add(0x40084000, new TagMetaData(VRType.SH, "Group 4008 Comments"));
            data_element_map.Add(0x50000000, new TagMetaData(VRType.UL, "Group 5000 Length"));
            data_element_map.Add(0x50000005, new TagMetaData(VRType.US, "Curve Dimensions"));
            data_element_map.Add(0x50000010, new TagMetaData(VRType.US, "Number of Points"));
            data_element_map.Add(0x50000020, new TagMetaData(VRType.CS, "Type of Data"));
            data_element_map.Add(0x50000022, new TagMetaData(VRType.LO, "Curve Description"));
            data_element_map.Add(0x50000030, new TagMetaData(VRType.SH, "Axis Units"));
            data_element_map.Add(0x50000040, new TagMetaData(VRType.SH, "Axis Labels"));
            data_element_map.Add(0x50000103, new TagMetaData(VRType.US, "Data Value Representation"));
            data_element_map.Add(0x50000104, new TagMetaData(VRType.US, "Minimum Coordinate Value"));
            data_element_map.Add(0x50000105, new TagMetaData(VRType.US, "Maximum Coordinate Value"));
            data_element_map.Add(0x50000106, new TagMetaData(VRType.SH, "Curve Range"));
            data_element_map.Add(0x50000110, new TagMetaData(VRType.US, "Curve Data Descriptor"));
            data_element_map.Add(0x50000112, new TagMetaData(VRType.US, "Coordinate Start Value"));
            data_element_map.Add(0x50000114, new TagMetaData(VRType.US, "Coordinate Step Value"));
            data_element_map.Add(0x50002000, new TagMetaData(VRType.US, "Audio Type"));
            data_element_map.Add(0x50002002, new TagMetaData(VRType.US, "Audio Sample Format"));
            data_element_map.Add(0x50002004, new TagMetaData(VRType.US, "Number of Channels"));
            data_element_map.Add(0x50002006, new TagMetaData(VRType.UL, "Number of Samples"));
            data_element_map.Add(0x50002008, new TagMetaData(VRType.UL, "Sample Rate"));
            data_element_map.Add(0x5000200A, new TagMetaData(VRType.UL, "Total Time"));
            data_element_map.Add(0x5000200C, new TagMetaData(VRType.OX, "Audio Sample Data"));
            data_element_map.Add(0x5000200E, new TagMetaData(VRType.LT, "Audio Comments"));
            data_element_map.Add(0x50003000, new TagMetaData(VRType.OX, "Curve Data"));
            data_element_map.Add(0x60000000, new TagMetaData(VRType.UL, "Group 6000 Length"));
            data_element_map.Add(0x60000010, new TagMetaData(VRType.US, "Rows"));
            data_element_map.Add(0x60000011, new TagMetaData(VRType.US, "Columns"));
            data_element_map.Add(0x60000015, new TagMetaData(VRType.IS, "Number of Frames in Overlay"));
            data_element_map.Add(0x60000040, new TagMetaData(VRType.CS, "Overlay Type"));
            data_element_map.Add(0x60000050, new TagMetaData(VRType.SS, "Origin"));
            data_element_map.Add(0x60000060, new TagMetaData(VRType.SH, "Compression Code (RET)"));
            data_element_map.Add(0x60000100, new TagMetaData(VRType.US, "Bits Allocated"));
            data_element_map.Add(0x60000102, new TagMetaData(VRType.US, "Bit Position"));
            data_element_map.Add(0x60000110, new TagMetaData(VRType.SH, "Overlay Format (RET)"));
            data_element_map.Add(0x60000200, new TagMetaData(VRType.US, "Overlay Location (RET)"));
            data_element_map.Add(0x60001100, new TagMetaData(VRType.US, "Overlay Descriptor - Gray"));
            data_element_map.Add(0x60001101, new TagMetaData(VRType.US, "Overlay Descriptor - Red"));
            data_element_map.Add(0x60001102, new TagMetaData(VRType.US, "Overlay Descriptor - Green"));
            data_element_map.Add(0x60001103, new TagMetaData(VRType.US, "Overlay Descriptor - Blue"));
            data_element_map.Add(0x60001200, new TagMetaData(VRType.US, "Overlays - Gray"));
            data_element_map.Add(0x60001201, new TagMetaData(VRType.US, "Overlays - Red"));
            data_element_map.Add(0x60001202, new TagMetaData(VRType.US, "Overlays - Green"));
            data_element_map.Add(0x60001203, new TagMetaData(VRType.US, "Overlays - Blue"));
            data_element_map.Add(0x60001301, new TagMetaData(VRType.IS, "ROI Area"));
            data_element_map.Add(0x60001302, new TagMetaData(VRType.DS, "ROI Mean"));
            data_element_map.Add(0x60001303, new TagMetaData(VRType.DS, "ROI Standard Deviation"));
            data_element_map.Add(0x60003000, new TagMetaData(VRType.OW, "Overlay Data"));
            data_element_map.Add(0x60004000, new TagMetaData(VRType.SH, "Group 6000 Comments (RET)"));
            data_element_map.Add(0x7FE00000, new TagMetaData(VRType.UL, "Group 7FE0 Length"));
            data_element_map.Add(0x7FE00010, new TagMetaData(VRType.OX, "Pixel Data"));
            data_element_map.Add(0xFFFEE000, new TagMetaData(VRType.DL, "Item"));
            data_element_map.Add(0xFFFEE00D, new TagMetaData(VRType.DL, "Item Delimitation Item"));
            data_element_map.Add(0xFFFEE0DD, new TagMetaData(VRType.DL, "Sequence Delimitation Item"));
        }

        /**
         * Returns the value range (VR) of a given tag.
         * @param tag  the tag id (group number + element number)
         * @return     the value range
         */
        public VRType getVR(uint tag)
        {
            TagMetaData meta_data = data_element_map[tag];
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
            TagMetaData meta_data = data_element_map[tag];
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
            TagMetaData meta_data = data_element_map[tag];
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
            return media_storage_map[id];
        }

        public static uint ToTag(uint groupId, uint elementId)
        {
            return (groupId << 16) | elementId;
        }
    }
}

