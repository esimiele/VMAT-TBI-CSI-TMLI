using SimpleProgressWindow;
using EvilDICOM.Core;
using EvilDICOM.Core.Helpers;
using EvilDICOM.Core.Image;
using CTStitcher.enums;
using CTStitcher.Utilities;
using EvilDICOM.Core.Interfaces;
using CTStitcher.Interfaces;
using System.IO;
using CTStitcher.Models;
using System.Collections.Generic;
using System;
using System.Linq;

namespace CTStitcher.ImageReaders
{
    public class CTReaderDCM : SimpleMTbase, ICTReader
    {
        //get methods
        public CTImageModel CT { get; private set; }
        public string ErrorMessage { get; private set; }
        public string PatientFirstName { get; private set; } = "";
        public string PatientMiddleName { get; private set; } = "";
        public string PatientLastName { get; private set; } = "";
        public string PatientMRN { get; private set; } = "";
        public string PatientDOB { get; private set; } = "";

        //Set method
        public void SetImageToRead<T>(T image)
        {
            _filePath = image as string;
        }

        //data members
        private string _filePath;

        /// <summary>
        /// constructor
        /// </summary>
        public CTReaderDCM()
        {
            SetCloseOnFinish(true, 100);
        }

        /// <summary>
        /// Run control
        /// </summary>
        /// <returns></returns>
        public override bool Run()
        {
            try
            {
                List<string> files = ExtractCTFilesToRead();
                if (files.Any())
                {
                    CT = new CTImageModel(ReadDicomMetaData(files.First()));
                    if (ReadCTImages(files)) return true;
                    CT.Origin = CT.Slices.First().Origin;
                }
                UpdateUILabel("Finished!");
                ProvideUIUpdate($"Elapsed time: {GetElapsedTime()}");
            }
            catch (Exception e)
            {
                ProvideUIUpdate(0, $"Error! Failed because: {e.Message}", true);
                ErrorMessage = $"Error! Failed because: {e.Message}" + Environment.NewLine;
                ErrorMessage += e.StackTrace;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get the list of dicom file names that need to be read
        /// </summary>
        /// <returns></returns>
        public List<string> ExtractCTFilesToRead()
        {
            UpdateUILabel($"Extracting CT files to read:");
            ProvideUIUpdate($"Extracting CT files to read from {_filePath}");
            return Directory.GetFiles(_filePath, "*.dcm").ToList();
        }

        /// <summary>
        /// Parse the dicom meta data for the first slice of the CT image
        /// </summary>
        /// <param name="firstSlice"></param>
        /// <returns></returns>
        public CTImageMetaDataModel ReadDicomMetaData(string firstSlice)
        {
            UpdateUILabel("Reading CT meta data:");
            DICOMObject DCMObj = DICOMObject.Read(firstSlice);
            CTImageMetaDataModel DMD = new CTImageMetaDataModel();

            DMD.XSize = (ushort)DCMObj.FindFirst(TagHelper.Columns).DData;
            DMD.YSize = (ushort)DCMObj.FindFirst(TagHelper.Rows).DData;
            ProvideUIUpdate($"CT image size: {DMD.XSize} x {DMD.YSize}");

            int zSize = Directory.GetFiles(_filePath, "*.dcm").Count();
            ProvideUIUpdate($"Number of CT slices: {zSize}");
            DMD.ZSize = zSize;

            DMD.RescaleSlope = (double)DCMObj.FindFirst(TagHelper.RescaleSlope).DData;
            DMD.RescaleIntercept = (double)DCMObj.FindFirst(TagHelper.RescaleIntercept).DData;
            ProvideUIUpdate($"Rescale slope: {DMD.RescaleSlope} HU/pixel val");
            ProvideUIUpdate($"Rescale intercept: {DMD.RescaleIntercept} HU");

            IEnumerable<IDICOMElement> elements = DCMObj.Elements;
            if( elements != null )
            {
                //optional dicom elements
                if(elements.Any(x => x.Tag == TagHelper.StudyInstanceUID))
                {
                    DMD.StudyUID = (string)elements.First(x => x.Tag == TagHelper.StudyInstanceUID).DData;
                }
                if (elements.Any(x => x.Tag == TagHelper.SeriesInstanceUID))
                {
                    DMD.SeriesUID = (string)elements.First(x => x.Tag == TagHelper.SeriesInstanceUID).DData;
                }
                if (elements.Any(x => x.Tag == TagHelper.DeviceSerialNumber))
                {
                    DMD.ImagingDeviceSerialNumber = (string)elements.First(x => x.Tag == TagHelper.DeviceSerialNumber).DData;
                }
                if (elements.Any(x => x.Tag == TagHelper.Manufacturer))
                {
                    DMD.ImagingDeviceManufacturer = (string)elements.First(x => x.Tag == TagHelper.Manufacturer).DData;
                }
                if (elements.Any(x => x.Tag == TagHelper.ManufacturerModelName))
                {
                    DMD.ImagingDeviceModel = (string)elements.First(x => x.Tag == TagHelper.ManufacturerModelName).DData;
                }
                if (elements.Any(x => x.Tag == TagHelper.PatientName))
                {
                    string fullName = (string)elements.First(x => x.Tag == TagHelper.PatientName).DData;
                    if (!string.IsNullOrEmpty(fullName))
                    {
                        if(fullName.Contains('^'))
                        {
                            if(fullName.Split('^').Count() == 2)
                            {
                                PatientLastName = fullName.Split('^').ElementAt(0);
                                PatientFirstName = fullName.Split('^').ElementAt(1);
                            }
                            else
                            {
                                PatientLastName = fullName.Split('^').ElementAt(0);
                                PatientFirstName = fullName.Split('^').ElementAt(1);
                                PatientMiddleName = fullName.Split('^').ElementAt(2);
                            }
                        }
                        PatientLastName = fullName;
                    }
                }
                if(elements.Any(x => x.Tag == TagHelper.PatientBirthDate) && elements.First(x => x.Tag == TagHelper.PatientBirthDate).DData != null)
                {
                    PatientDOB = ((DateTime)elements.First(x => x.Tag == TagHelper.PatientBirthDate).DData).ToString();
                }
                if (elements.Any(x => x.Tag == TagHelper.PatientID))
                {
                    PatientMRN = (string)elements.First(x => x.Tag == TagHelper.PatientID).DData;
                }
            }
            //required dicom elements
            DMD.XRes = ((List<double>)DCMObj.FindFirst(TagHelper.PixelSpacing).DData_).ElementAt(0);
            DMD.YRes = ((List<double>)DCMObj.FindFirst(TagHelper.PixelSpacing).DData_).ElementAt(1); 
            DMD.ZRes = (double)DCMObj.FindFirst(TagHelper.SliceThickness).DData;
            DMD.ScanOrientation = DecodePatientScanOrientation((string)DCMObj.FindFirst(TagHelper.PatientPosition).DData);
            DMD.FOR = (string)DCMObj.FindFirst(TagHelper.FrameOfReferenceUID).DData;
            DMD.ImageOrientation = DecodeCTImageOrientation((List<double>)DCMObj.FindFirst(TagHelper.ImageOrientationPatient).DData_, DMD.ScanOrientation);

            return DMD;
        }

        /// <summary>
        /// Helper method to build the image direction cosines
        /// </summary>
        /// <param name="vals"></param>
        /// <param name="orientation"></param>
        /// <returns></returns>
        private VectorModel DecodeCTImageOrientation(List<double> vals, ScanOrientation orientation)
        {
            double zOrientation = orientation == ScanOrientation.HeadFirstSupine ? 1 : -1;
            return new VectorModel(vals.ElementAt(0), vals.ElementAt(4), zOrientation);
        }

        /// <summary>
        /// Helper method to translate the patient position dicom element to an enumerator
        /// </summary>
        /// <param name="orientation"></param>
        /// <returns></returns>
        private ScanOrientation DecodePatientScanOrientation(string orientation)
        {
            if (string.Equals(orientation, "HFS")) return ScanOrientation.HeadFirstSupine;
            else if (string.Equals(orientation, "FFS")) return ScanOrientation.FeetFirstSupine;
            else return ScanOrientation.Other;
        }

        /// <summary>
        /// Utility method to take the list of dicom file names and parse the pixel data from each dicom file. Add the created image slice to the CT image
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        private bool ReadCTImages(List<string> files)
        {
            int count = 0;
            int numFiles= files.Count;
            foreach (string s in files)
            {
                ProvideUIUpdate((int)(100 * ++count / numFiles), s.Substring(s.LastIndexOf(@"\") + 1, s.Length - s.LastIndexOf(@"\") - 1));
                (short[,] pixels, VectorModel origin) sliceData = ReadCTSlice(s);
                ImageSliceModel slice = new ImageSliceModel(sliceData.pixels, sliceData.origin);
                if (CT.AddImageSlice(slice)) return true;
            }
            return false;
        }

        /// <summary>
        /// Helper method to actually retrieve the pixel data and rescale it HU
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public (short[,], VectorModel) ReadCTSlice(string file)
        {
            //read the DICOM image data
            DICOMObject o = DICOMObject.Read(file);
            //get the pixel stream
            PixelStream pixels = EvilDICOM.Core.Extensions.DICOMObjectExtensions.GetPixelStream(o);
            //get the underlying pixel data as int16 type
            short[] pixelVals = pixels.GetValues16(true);
            short[,] data = new short[CT.MetaData.YSize, CT.MetaData.XSize];

            int count = 0;
            for (int j = 0; j < CT.MetaData.YSize; j++)
            {
                for (int i = 0; i < CT.MetaData.XSize; i++)
                {
                    data[i,j] = CTVoxelRescaler.ConvertFromVoxelValueToHU(pixelVals[count++], CT.MetaData.RescaleSlope, CT.MetaData.RescaleIntercept);
                }
            }
            List<double> location = (List<double>)o.FindFirst(TagHelper.ImagePositionPatient).DData_;

            return (data, new VectorModel(location));
        }
    }
}
