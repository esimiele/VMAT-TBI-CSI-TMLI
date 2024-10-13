using CTStitcher.enums;
using I = itk.simple;
using CTStitcher.Helpers;
using SimpleProgressWindow;
using System.IO;
using CTStitcher.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CTStitcher.ImageWriters
{
    public class CTWriterDCM : SimpleMTbase
    {
        public string FinalWritePath { get; private set; } = "";
        //get method
        public string ErrorMessage { get; private set; }

        //data members
        private CTImageModel img;
        private string saveDir;
        private WriteFormat WriteFormat;
        private string lastName;
        private string firstName;
        private string middleName;
        private string mrn;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="i"></param>
        /// <param name="ln"></param>
        /// <param name="fn"></param>
        /// <param name="mn"></param>
        /// <param name="id"></param>
        /// <param name="sd"></param>
        /// <param name="format"></param>
        public CTWriterDCM(CTImageModel i, string ln, string fn, string mn, string id, string sd, WriteFormat format)
        {
            img = i;
            lastName = ln;
            firstName = fn;
            middleName = mn;
            mrn = id;
            saveDir = sd + "\\" + mrn + "\\";
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }
            FinalWritePath = saveDir;
            WriteFormat = format;
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
                UpdateUILabel($"Writing {img.MetaData.Id} to DCM");
                
                SaveImagesDICOM();
                return false;
            }
            catch(Exception e)
            {
                ProvideUIUpdate(0, $"Error! Failed because: {e.Message}", true); 
                ErrorMessage += e.StackTrace;
                return true;
            }
        }

        /// <summary>
        /// Utility method to take the CT image and convert it to an itk image, set the dicom header information, then
        /// save each image slice in dicom format to the specified directory
        /// </summary>
        private void SaveImagesDICOM()
        {
            I.PixelIDValueEnum pixelTypeDCM = I.PixelIDValueEnum.sitkInt16;
            I.VectorUInt32 imageSize = new I.VectorUInt32(new uint[] { (uint)img.MetaData.XSize, (uint)img.MetaData.YSize });
            I.Image itkImageDCM = new I.Image(imageSize, pixelTypeDCM);
            I.VectorDouble spacingDCM = new I.VectorDouble(new double[] { img.MetaData.XRes, img.MetaData.YRes });
            itkImageDCM.SetSpacing(spacingDCM); 
            I.ImageFileWriter writer = new I.ImageFileWriter();
            writer.KeepOriginalImageUIDOn();
            // DICOM metadata that are common to each slice
            itkImageDCM = UpdateDicomImageMetaData(itkImageDCM, GenerateImageMetaDataDictionary());
            ProvideUIUpdate($"Total {img.Slices.Count()} image slices to process.");
            uint sliceNum = 0;
            foreach(ImageSliceModel slice in img.Slices)
            {
                double progDec = (double)sliceNum / img.Slices.Count();
                var msg = $"Saving image slice #{sliceNum + 1}";
                if ((sliceNum + 1) % 10 == 0)
                {
                    // Slice is a multiple of 10, show message update
                    ProvideUIUpdate((int)(100 * progDec), $"Processing slice: {sliceNum}");
                }
                else
                {
                    ProvideUIUpdate((int)(100 * progDec));
                }
                UpdateUILabel(msg);

                string imgOrigin = string.Format("{0:0.00}\\{1:0.00}\\{2:0.00}", slice.Origin.X, slice.Origin.Y, slice.Origin.Z);
                itkImageDCM.SetMetaData("0020|0032", imgOrigin);  // slice origin
                itkImageDCM.SetMetaData("0020|1041", slice.SliceZLocation.ToString());  // slice location
                itkImageDCM.SetMetaData("0020|0013", (sliceNum + 1).ToString());  // instance numebr
                itkImageDCM = ITKImageHelper.ConvertCTSliceToItkImage(slice, itkImageDCM, sliceNum);

                writer.SetFileName(Path.Combine(saveDir, $"CT{mrn}_merged_{sliceNum}.DCM"));
                writer.Execute(itkImageDCM);
                sliceNum++;
            }
            ProvideUIUpdate($"All DICOM files were saved.");
        }

        /// <summary>
        /// Helper method to generate a dictionary of the relevant dicom tags needed for proper import into Eclipse
        /// (mainly elements contained in the dicom meta data)
        /// </summary>
        /// <returns></returns>
        private Dictionary<string,string> GenerateImageMetaDataDictionary()
        {
            return new Dictionary<string, string>
            {
                { "0008|0008", "ORIGINAL\\SECONDARY\\AXIAL"},
                { "0008|0020", DateTime.Now.ToString("yyyyMMdd")},
                { "0008|0030", DateTime.Now.ToString("HHmmss.ffffff")},
                { "0008|0060", "CT"},
                { "0008|0070", img.MetaData.ImagingDeviceManufacturer},
                { "0008|1010", $"HOST-{img.MetaData.ImagingDeviceSerialNumber}"},
                { "0008|1090", img.MetaData.ImagingDeviceModel},
                { "0010|0010", $"{lastName}^{firstName}^{middleName}"},
                { "0010|0020", mrn},
                { "0018|1000", img.MetaData.ImagingDeviceSerialNumber},
                { "0018|0050", img.MetaData.ZRes.ToString()}, // slice thickness
                { "0018|0060", "120"},
                { "0018|5100", img.MetaData.ScanOrientation == ScanOrientation.HeadFirstSupine ? "HFS" : "FFS"},
                { "0020|000D", img.MetaData.StudyUID},  // study UID.
                { "0020|000E", img.MetaData.SeriesUID}, // series UID.
                { "0020|0052", img.MetaData.FOR}, // use the same frame of reference UID as the original image series.
                { "0020|1040", "BB"}, // position reference indicator
                { "0020|0012", "1"}, // acquisition number
                { "0020|0037", string.Format("{0}\\{1}\\{1}\\{1}\\{2}\\{1}",img.MetaData.ImageOrientation.X,0,img.MetaData.ImageOrientation.Y) },
                { "0028|0002", "1"}, // samples per pixel
                { "0028|0010", img.MetaData.XSize.ToString()}, // rows
                { "0028|0011", img.MetaData.YSize.ToString()}, // columns
                { "0028|0030", string.Format("{0:0.00}\\{1:0.00}",img.MetaData.XRes.ToString(), img.MetaData.YRes.ToString())}, // x,y resolution
                { "0028|0100", "16"}, // bits allocated
                { "0028|0101", "16"}, // bits stored
                { "0028|0102", "15"}, // highBit
                { "0028|0103", "0"}, // pixel representation
                { "0028|1052", img.MetaData.RescaleIntercept.ToString()}, // rescale intercept
                { "0028|1053", img.MetaData.RescaleSlope.ToString()}, // rescale intercept
                { "0028|1054", "HU"}, // rescale intercept
            };
        }

        /// <summary>
        /// Utility method to assign each dicom element to the itk image
        /// </summary>
        /// <param name="itkImg"></param>
        /// <param name="metaData"></param>
        /// <returns></returns>
        private I.Image UpdateDicomImageMetaData(I.Image itkImg, Dictionary<string, string> metaData)
        {
            foreach(KeyValuePair<string,string> itr in metaData)
            {
                itkImg.SetMetaData(itr.Key, itr.Value);
            }
            return itkImg;
        }
    }
}
