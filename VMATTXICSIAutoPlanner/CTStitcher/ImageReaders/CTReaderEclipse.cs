using CTStitcher.Interfaces;
using CTStitcher.Models;
using CTStitcher.Utilities;
using SimpleProgressWindow;
using System;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace CTStitcher.ImageReaders
{
    public class CTReaderEclipse : SimpleMTbase, ICTReader
    {
        //get methods
        public CTImageModel CT { get; private set; }
        public string ErrorMessage { get; private set; }

        //set methods
        public void SetImageToRead<T>(T image)
        {
            _image = image as Image;
        }

        //data memebers
        private Image _image;

        /// <summary>
        /// Constructor
        /// </summary>
        public CTReaderEclipse() 
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
                if (Initialize()) return true;
                if (ReadCTImageSlices()) return true;
                UpdateUILabel("Finished!");
                ProvideUIUpdate($"Elapsed time: {GetElapsedTime()}");
            }
            catch(Exception e)
            {
                ProvideUIUpdate(0, $"Error! Failed because: {e.Message}", true);
                ErrorMessage = $"Error! Failed because: {e.Message}" + Environment.NewLine;
                ErrorMessage += e.StackTrace;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Initialize a new CT image object to hold the ESAPI image data. Assign the relevant dicom meta data
        /// </summary>
        /// <returns></returns>
        private bool Initialize()
        {
            UpdateUILabel("Initializing...");
            ProvideUIUpdate(0, $"CT Image: {_image.Id}");
            CT = new CTImageModel(ReadImageMetaData(_image));
            CT.Origin = new VectorModel(_image.Origin.x, _image.Origin.y, _image.Origin.z);
            return false;
        }

        /// <summary>
        /// Helper method to parse each slice of the CT into a new Image slice object and add the slice to the created CT object
        /// </summary>
        /// <returns></returns>
        private bool ReadCTImageSlices()
        {
            UpdateUILabel("Reading CT data:");
            for (int i = 0; i < CT.MetaData.ZSize; i++)
            {
                ProvideUIUpdate((int)(100 * (i + 1) / CT.MetaData.ZSize), $"Slice: {i + 1}");
                ImageSliceModel slice = new ImageSliceModel(ReadSliceVoxels(i, CT.MetaData.XSize, CT.MetaData.YSize, CT.MetaData.RescaleSlope, CT.MetaData.RescaleIntercept));
                slice.Origin = new VectorModel(CT.Origin.X, CT.Origin.Y, CT.Origin.Z + i * CT.MetaData.ImageOrientation.Z * CT.MetaData.ZRes);
                slice.SliceZLocation = slice.Origin.Z;
                CT.AddImageSlice(slice);
            }
            return false;
        }

        /// <summary>
        /// Helper method to parse the relevent CT image metadata for this CT image
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public CTImageMetaDataModel ReadImageMetaData(Image img)
        {
            CTImageMetaDataModel data = new CTImageMetaDataModel();
            data.Id = img.Id;
            data.ScanOrientation = img.ImagingOrientation == PatientOrientation.HeadFirstSupine ? enums.ScanOrientation.HeadFirstSupine : enums.ScanOrientation.FeetFirstSupine;
            data.XSize = img.XSize;
            data.YSize = img.YSize;
            data.ZSize = img.ZSize;
            data.XRes = img.XRes;
            data.YRes = img.YRes;
            data.ZRes = img.ZRes;
            //use two voxel value points to determine slope and intercept
            data.RescaleSlope = (img.VoxelToDisplayValue(1000) - img.VoxelToDisplayValue(0)) / 1000.0;
            data.RescaleIntercept = img.VoxelToDisplayValue(1000) - 1000 * data.RescaleSlope;
            data.ImageOrientation = new VectorModel(img.XDirection.x, img.YDirection.y, img.ZDirection.z);
            data.FOR = img.FOR;
            data.StudyUID = img.Series.Study.UID;
            data.SeriesUID = img.Series.UID;
            data.ImagingDeviceSerialNumber = img.Series.ImagingDeviceSerialNo;
            data.ImagingDeviceManufacturer = img.Series.ImagingDeviceManufacturer;
            data.ImagingDeviceModel = img.Series.ImagingDeviceModel;
            return data;
        }

        /// <summary>
        /// Utility method to actually read the pixel data from each slice and convert it to HU
        /// </summary>
        /// <param name="slice"></param>
        /// <param name="xSize"></param>
        /// <param name="ySize"></param>
        /// <param name="rescaleSlope"></param>
        /// <param name="rescaleIntercept"></param>
        /// <returns></returns>
        public short[,] ReadSliceVoxels(int slice, int xSize, int ySize, double rescaleSlope, double rescaleIntercept)
        {
            int[,] intBuffer = new int[xSize, ySize];
            _image.GetVoxels(slice, intBuffer);
            short[,] data = new short[xSize, ySize];
            for(int i = 0; i < xSize; i++)
            {
                for(int j = 0; j < ySize; j++)
                {
                    data[i, j] = CTVoxelRescaler.ConvertFromVoxelValueToHU(intBuffer[i, j], rescaleSlope, rescaleIntercept);
                }
            }
            return data;
        }
    }
}
