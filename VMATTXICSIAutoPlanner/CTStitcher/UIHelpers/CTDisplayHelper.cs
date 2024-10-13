using System;
using System.Drawing;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Drawing.Imaging;
using System.Windows.Media;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace CTStitcher.UIHelpers
{
    public static class CTDisplayHelper
    {
        /// <summary>
        /// Method to take a bitmap image, rescale it according to the supplied pixel size argument, then return a bitmap source object that
        /// can be displayed in the UI
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="pixelSize"></param>
        /// <returns></returns>
        public static BitmapSource Bitmap2BitmapImage(Bitmap bitmap, int pixelSize)
        {
            BitmapSource bmps = Imaging.CreateBitmapSourceFromHBitmap(
                           bitmap.GetHbitmap(),
                           IntPtr.Zero,
                           Int32Rect.Empty,
                           BitmapSizeOptions.FromWidthAndHeight(pixelSize, pixelSize));
            return bmps;
        }

        /// <summary>
        /// Utility method to take a processed CT slice (as bmp) and assign it to the supplied image control for display
        /// </summary>
        /// <param name="imgControl"></param>
        /// <param name="processedCTSlice"></param>
        /// <param name="pixelSize"></param>
        public static void DisplayCTSlice(System.Windows.Controls.Image imgControl, Bitmap processedCTSlice, int pixelSize)
        {
            imgControl.Source = Bitmap2BitmapImage(processedCTSlice, pixelSize);
        }

        /// <summary>
        /// Helper function to generate a bitmap of a coronal slice of the stitched CT image
        /// </summary>
        /// <param name="xsize"></param>
        /// <param name="yslice"></param>
        /// <param name="matchSlice"></param>
        /// <param name="lowerZMargin"></param>
        /// <param name="upperZMargin"></param>
        /// <param name="processedAxialCT"></param>
        /// <returns></returns>
        public static Bitmap GenerateCoronalBMPFromCTData(int xsize, int yslice, int matchSlice, int lowerZMargin, int upperZMargin, Bitmap[] processedAxialCT)
        {
            Bitmap bmp = new Bitmap(xsize, upperZMargin + lowerZMargin + 1, PixelFormat.Format32bppRgb);
            for (int j = (matchSlice - lowerZMargin); j <= (matchSlice + upperZMargin); j++)
            {
                for (int i = 0; i < xsize; i++)
                {
                    //Need to flip the image for display
                    bmp.SetPixel(i, (matchSlice + upperZMargin - j), processedAxialCT[j].GetPixel(i, yslice));
                }
            }
            return new Bitmap(bmp);
        }

        /// <summary>
        /// Helper function to generate a bitmap image of a sagittal slice of the stitched CT image. Separate from the GenerateCoronalBMP function so I can
        /// keep the get pixel functions straight
        /// </summary>
        /// <param name="xsize"></param>
        /// <param name="xslice"></param>
        /// <param name="matchSlice"></param>
        /// <param name="lowerZMargin"></param>
        /// <param name="upperZMargin"></param>
        /// <param name="processedAxialCT"></param>
        /// <returns></returns>
        public static Bitmap GenerateSagittalBMPFromCTData(int xsize, int xslice, int matchSlice, int lowerZMargin, int upperZMargin, Bitmap[] processedAxialCT)
        {
            Bitmap bmp = new Bitmap(xsize, upperZMargin + lowerZMargin + 1, PixelFormat.Format32bppRgb);
            for (int j = (matchSlice - lowerZMargin); j <= (matchSlice + upperZMargin); j++)
            {
                for (int i = 0; i < xsize; i++)
                {
                    //Need to flip the image for display
                    bmp.SetPixel(i, (matchSlice + upperZMargin - j), processedAxialCT[j].GetPixel(xslice, i));
                }
            }
            return new Bitmap(bmp);
        }

        public static TransformGroup GetNewTransformGroup()
        {
            TransformGroup group = new TransformGroup();
            group.Children.Add(new ScaleTransform());
            group.Children.Add(new TranslateTransform());
            return group;
        }
    }
}
