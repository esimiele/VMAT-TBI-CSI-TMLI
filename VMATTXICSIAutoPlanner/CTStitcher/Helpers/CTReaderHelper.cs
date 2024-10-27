using CTStitcher.ImageReaders;
using CTStitcher.Interfaces;
using SimpleProgressWindow;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Context;
using VMS.TPS.Common.Model.API;
using CTStitcher.Models;
using CTStitcher.enums;

namespace CTStitcher.Helpers
{
    public class CTReaderHelper
    {
        //get methods
        public RegistrationPPModel RegistrationPP { get; private set; }
        public StringBuilder UILog {  get; private set; }
        public PatientMetaData PatientMetaData { get; private set; }
        //data members
        CTImageModel TargetCT = null;
        CTImageModel SourceCT = null;

        /// <summary>
        /// Generic type function to read a CT image. Works with both dicom input (i.e., a file path) or ESAPI image object input
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="img"></param>
        /// <param name="type"></param>
        /// <param name="constructPatientMetaData"></param>
        /// <returns></returns>
        public bool ReadCTImage<T>(T img, RegistrationImageType type, bool constructPatientMetaData = false)
        {
            ICTReader reader;
            if (img.GetType() == typeof(Image)) reader = new CTReaderEclipse();
            else reader = new CTReaderDCM();
            reader.SetImageToRead(img);
            if ((reader as SimpleMTbase).Execute())
            {
                Logger.GetInstance().LogError($"Reading {type} scan failed!");
                Logger.GetInstance().LogError(reader.ErrorMessage, true);
                UILog.AppendLine($"Reading {type} scan failed!");
                UILog.AppendLine(reader.ErrorMessage);
                return true;
            }
            if(type == RegistrationImageType.Target) TargetCT = reader.CT;
            else SourceCT = reader.CT;
            if(constructPatientMetaData && img.GetType() == typeof(string))
            {
                PatientMetaData = new PatientMetaData((reader as CTReaderDCM).PatientFirstName,
                                                         (reader as CTReaderDCM).PatientMiddleName,
                                                         (reader as CTReaderDCM).PatientLastName,
                                                         (reader as CTReaderDCM).PatientMRN,
                                                         (reader as CTReaderDCM).PatientDOB);
            }
            Logger.GetInstance().AppendLogOutput($"CT Reader Output ({type} image):", (reader as SimpleMTbase).GetLogOutput());
            return false;
        }

        /// <summary>
        /// Construct the RegistrationPP class using the target and source CT images. Specifically tailored to ESAPI using the input list of
        /// ESAPI registrations to construct RegistrationPP
        /// </summary>
        /// <param name="registrations"></param>
        /// <returns></returns>
        public bool BuildRegistrationPP(IEnumerable<Registration> registrations)
        {
            if (ReferenceEquals(null, registrations.SingleOrDefault(x => string.Equals(x.RegisteredFOR, TargetCT.MetaData.FOR) && string.Equals(x.SourceFOR, SourceCT.MetaData.FOR))))
            {
                Logger.GetInstance().LogError($"Error! No registration exists where the {TargetCT.MetaData.Id} image is the target and the {SourceCT.MetaData.Id} image is the source! Cannot stitch scans! Exiting");
                return true;
            }
            Registration reg = registrations.Single(x => string.Equals(x.RegisteredFOR, TargetCT.MetaData.FOR) && string.Equals(x.SourceFOR, SourceCT.MetaData.FOR));
            if (ReferenceEquals(TargetCT, null) || ReferenceEquals(SourceCT, null) || ReferenceEquals(reg, null))
            {
                Logger.GetInstance().LogError("Error in building RegistrationPP! Either the target image, source image, or Eclipse registration are null! Exiting!");
                UILog.AppendLine("Error in building RegistrationPP! Either the target image, source image, or Eclipse registration are null! Exiting!");
                return true;
            }
            RegistrationPP = new RegistrationPPModel(TargetCT, SourceCT, reg);
            return false;
        }

        /// <summary>
        /// Overloaded method specifically tailored when the script is operating on dicom files outside of Eclipse
        /// </summary>
        /// <param name="reg"></param>
        /// <returns></returns>
        public bool BuildRegistrationPP(double[,] reg)
        {
            if (ReferenceEquals(TargetCT, null) || ReferenceEquals(SourceCT, null) || ReferenceEquals(reg, null))
            {
                Logger.GetInstance().LogError("Error in building RegistrationPP! Either the target image, source image, or transform matrix are null! Exiting!");
                UILog.AppendLine("Error in building RegistrationPP! Either the target image, source image, or transform matrix are null! Exiting!");
                return true;
            }
            RegistrationPP = new RegistrationPPModel(TargetCT, SourceCT, reg);
            return false;
        }
    }
}
