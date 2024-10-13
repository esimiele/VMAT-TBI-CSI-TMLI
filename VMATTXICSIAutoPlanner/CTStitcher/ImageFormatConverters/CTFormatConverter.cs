using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using CTStitcher.Models;
using CTStitcher.Utilities;
using SimpleProgressWindow;

namespace CTStitcher.ImageFormatConverters
{
    public class CTFormatConverter : SimpleMTbase
    {
        //get methods
        public Bitmap[] ProcessedCTData { get; private set; }
        public string ErrorMessage { get; private set; }

        //data members
        private Bitmap[] CTAxialData;
        private short[][,] _scaledCTData;
        private int slicesCompleted = 0;
        private object locker = new object();
        private List<Task<short[,]>> rescaleTasks = new List<Task<short[,]>>();
        private List<Task<Bitmap>> convertTasks = new List<Task<Bitmap>>();
        private CTImageModel theImage;
        private int matchSlice = 1;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="image"></param>
        /// <param name="ms"></param>
        public CTFormatConverter(CTImageModel image, int ms)
        {
            theImage = image;
            CTAxialData = new Bitmap[theImage.MetaData.ZSize];
            _scaledCTData = new short[theImage.MetaData.ZSize][,];
            matchSlice = ms;
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
                RescaleCTData();
                slicesCompleted = 0;
                ConvertShortArrayToBmp();
                AddMarkIndicatorsToMatchSlice();
                ProcessedCTData = CTAxialData;
            }
            catch (Exception e)
            {
                ProvideUIUpdate(0, $"Error! Failed because: {e.Message}", true);
                ErrorMessage = $"Error! Failed because: {e.Message}";
                ErrorMessage += e.StackTrace;
                return true;
            }
            return false;
        }

        #region rescale data
        /// <summary>
        /// Utility method to rescale the CT data such that it can be displayed with a bit depth of 8 (maximum for bmp)
        /// </summary>
        /// <returns></returns>
        private bool RescaleCTData()
        {
            UpdateUILabel("Rescaling CT data for viewing:");
            ProvideUIUpdate(0);
            for (int i = 0; i < theImage.Slices.Count(); i++)
            {
                int slice = i;
                //ProvideUIUpdate((int)(100 * i / theImage.Slices.Count()), $"Slice: {i} rescaled for viewing");
                //CTData[slice] = ConvertToBmp(theImage.Slices.ElementAt(slice).PixelData, theImage.MetaData.XSize, theImage.MetaData.YSize);
                rescaleTasks.Add(new TaskFactory().StartNew(() => RescalePixelDataForSlice(theImage.Slices.ElementAt(slice).PixelData, theImage.MetaData.XSize, theImage.MetaData.YSize)).ContinueWith((task) => UpdateArrayAndUI(task, slice)));
            }
            Task.WaitAll(rescaleTasks.ToArray());
            return false;
        }

        /// <summary>
        /// Utility method to take the pixel data for the slice, convert it from HU to pixel number, then rescale it to have a bit depth of 8
        /// </summary>
        /// <param name="data"></param>
        /// <param name="xSize"></param>
        /// <param name="ySize"></param>
        /// <returns></returns>
        private short[,] RescalePixelDataForSlice(short[,] data, int xSize, int ySize)
        {
            short[,] scaledData = new short[xSize, ySize];
            double scale = Math.Pow(2, 12) / 255.0;
            for (int j = 0; j < ySize; j++)
            {
                for (int i = 0; i < xSize; i++)
                {
                    short val = CTVoxelRescaler.RescaleVoxelIntensity(CTVoxelRescaler.ConvertFromHUToVoxelValue(data[j, i], theImage.MetaData.RescaleSlope, theImage.MetaData.RescaleIntercept), scale);
                    if (val > 255) val = 255;
                    scaledData[j, i] = val;
                }
            }
            return scaledData;
        }
        #endregion

        #region Convert data to bmp
        /// <summary>
        /// Utility method to to convert the rescaled CT data to bitmap format so it can be easily displayed to the user
        /// </summary>
        /// <returns></returns>
        private bool ConvertShortArrayToBmp()
        {
            UpdateUILabel("Converting to Bmp array:");
            ProvideUIUpdate(0);
            for (int i = 0; i < theImage.Slices.Count(); i++)
            {
                int slice = i;
                //ProvideUIUpdate((int)(100 * i / theImage.Slices.Count()), $"Slice: {slice} converted to bitmap");
                //CTData[slice] = ConvertToBmp(theImage.Slices.ElementAt(slice).PixelData, theImage.MetaData.XSize, theImage.MetaData.YSize);
                convertTasks.Add(new TaskFactory().StartNew(() => ConvertPixelDataForSliceToBmp(_scaledCTData[slice], theImage.MetaData.XSize, theImage.MetaData.YSize)).ContinueWith((task) => UpdateArrayAndUI(task, slice)));
            }
            Task.WaitAll(convertTasks.ToArray());
            return false;
        }

        /// <summary>
        /// Utility method to allow for async processing of the rescale/convert to bitmap operations
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <param name="slice"></param>
        /// <returns></returns>
        private T UpdateArrayAndUI<T>(Task<T> task, int slice)
        {
            lock (locker)
            {
                string message;
                if (task.Result.GetType() == typeof(short[,]))
                {
                    _scaledCTData[slice] = task.Result as short[,];
                    message = $"Slice: {slice} pixel data rescaled";
                }
                else
                {
                    CTAxialData[slice] = task.Result as Bitmap;
                    message = $"Slice: {slice} converted to bitmap";
                }
                ProvideUIUpdate(100 * ++slicesCompleted / theImage.MetaData.ZSize, message);
            }
            return task.Result;
        }

        /// <summary>
        /// Utility method to build the actual bitmap image for each slice
        /// </summary>
        /// <param name="data"></param>
        /// <param name="xSize"></param>
        /// <param name="ySize"></param>
        /// <returns></returns>
        private Bitmap ConvertPixelDataForSliceToBmp(short[,] data, int xSize, int ySize)
        {
            Bitmap bmp = new Bitmap(xSize, ySize, PixelFormat.Format32bppRgb);
            for (int j = 0; j < ySize; j++)
            {
                for (int i = 0; i < xSize; i++)
                {
                    bmp.SetPixel(j, i, Color.FromArgb(255, data[j, i], data[j, i], data[j, i]));
                }
            }
            return bmp;
        }
        #endregion

        /// <summary>
        /// Helper function to make the outer border of the bmp image red for the match slice and one slice below the match slice. Helps the user identify the 
        /// junction region
        /// </summary>
        private void AddMarkIndicatorsToMatchSlice()
        {
            for (int i = matchSlice - 1; i < matchSlice + 1; i++)
            {
                Bitmap bmp = CTAxialData[i];
                for (int j = 0; j < CTAxialData[i].Width; j++)
                {
                    for (int k = 0; k < 10; k++)
                    {
                        bmp.SetPixel(j, k, Color.Red);
                        bmp.SetPixel(j, CTAxialData[i].Height - k - 1, Color.Red);
                    }
                }
                for (int j = 0; j < CTAxialData[i].Height; j++)
                {
                    for (int k = 0; k < 10; k++)
                    {
                        bmp.SetPixel(k, j, Color.Red);
                        bmp.SetPixel(CTAxialData[i].Width - k - 1, j, Color.Red);
                    }
                }
                CTAxialData[i] = bmp;
            }
        }
    }
}
