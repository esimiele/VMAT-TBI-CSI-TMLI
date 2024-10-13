using CTStitcher.Helpers;
using CTStitcher.Interfaces;
using CTStitcher.Models;
using SimpleProgressWindow;
using System;
using I = itk.simple;

namespace CTStitcher.Stitchers
{
    public class Stitcher6DOF : SimpleMTbase, IStitcher
    {
        //get methods
        public CTImageModel StitchedCT { get; private set; }
        public string ErrorMessage { get; private set; }
        public int MatchSlice { get; private set; }

        //data members
        private RegistrationPPModel Registration;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="registration"></param>
        public Stitcher6DOF(RegistrationPPModel registration)
        {
            Registration = registration;
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
                StitchedCT = StitchCTImages();
                UpdateUILabel("Finished Stitching Images!");
                ProvideUIUpdate($"Elapsed time: {GetElapsedTime()}");
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

        /// <summary>
        /// Method to perform the actual merging of the HFS and FFS CT scans. Consider translations and rotations in registration
        /// </summary>
        /// <returns></returns>
        public CTImageModel StitchCTImages()
        {
            UpdateUILabel($"Starting CT Concatenation");
            ProvideUIUpdate("Using 6DOF algorithm for stitching images");

            UpdateUILabel($"Converting CT image ({Registration.TargetImage.MetaData.Id}) to itk image");
            I.Image itkImagePrimary = ITKImageHelper.ConvertCTImageToItkImage(Registration.TargetImage, ProvideUIUpdate);
            ProvideUIUpdate(0, $"Conversion of the target image is complete.");

            UpdateUILabel($"Converting CT image ({Registration.SourceImage.MetaData.Id}) to itk image");
            I.Image itkImageSecondary = ITKImageHelper.ConvertCTImageToItkImage(Registration.SourceImage, ProvideUIUpdate);
            ProvideUIUpdate(0, $"Conversion of the source image is complete");

            UpdateUILabel($"Transforming source image:");
            I.Image itkImageSecondaryTransformed = TransformImage(itkImageSecondary);
            ProvideUIUpdate(0, "Source image transformation is complete");

            UpdateUILabel("Stitching images:");
            I.Image itkImageStitched = StitchImages(itkImagePrimary, itkImageSecondaryTransformed);
            ProvideUIUpdate(0, "Images are concatenated");

            UpdateUILabel("Converting stitched ITK image to CTImage:");
            CTImageModel mergedCT = ITKImageHelper.ConvertItkImageToCTImage(itkImageStitched, 
                                                                       CTStitcherHelper.InitializeStitchedImage(Registration), 
                                                                       ProvideUIUpdate);

            return mergedCT;
        }

        /// <summary>
        /// Take the source image and apply the rotation (using Euler3DTransform) and then translate the origin of the image
        /// </summary>
        /// <param name="itkImageSecondary"></param>
        /// <returns></returns>
        private I.Image TransformImage(I.Image itkImageSecondary)
        {
            ProvideUIUpdate("Transforming source image now");
            ProvideUIUpdate("Constructing Euler3D transform operator");
            // rotate the image
            // According to SimpleITK documentation, Euler3DTransform is a rigid 3D transform with rotation in radians around the fixed center with translation.
            I.Euler3DTransform eulerTransform = new I.Euler3DTransform();
            // First set the center of rotations --> should be the physical CENTER of the image
            double xCenter = itkImageSecondary.GetOrigin()[0] + itkImageSecondary.GetDirection()[0] * (itkImageSecondary.GetSize()[0] - 1) * itkImageSecondary.GetSpacing()[0] / 2;
            double yCenter = itkImageSecondary.GetOrigin()[1] + itkImageSecondary.GetDirection()[4] * (itkImageSecondary.GetSize()[1] - 1) * itkImageSecondary.GetSpacing()[1] / 2;
            double zCenter = itkImageSecondary.GetOrigin()[2] + itkImageSecondary.GetDirection()[8] * (itkImageSecondary.GetSize()[2] - 1) * itkImageSecondary.GetSpacing()[2] / 2;
            eulerTransform.SetCenter(new I.VectorDouble(new double[] { xCenter, yCenter, zCenter }));
            ProvideUIUpdate($"Image rotation center set to: ({xCenter}, {yCenter}, {zCenter}) mm");
            
            // Next, define the rotation angles
            //3-10-24 From empirical testing, the Euler 3D rotation angles are inverted from the Eclipse rotation angles
            eulerTransform.SetRotation(-Registration.Rotations.X, -Registration.Rotations.Y, -Registration.Rotations.Z);
            ProvideUIUpdate($"Euler rotations: ({-Registration.Rotations.X}, {-Registration.Rotations.Y}, {-Registration.Rotations.Z}) rad");
            //3-10-24 From comparing the calculated 4x4 transform matrix using the angles reported in Eclipse vs the 4x4 transform matrix provided by
            //ESAPI, it appears the order of rotations is x, y, then z
            eulerTransform.SetComputeZYX(true);
            ProvideUIUpdate("Transforming and resampling source image now");
            var rotated = I.SimpleITK.Resample(itkImageSecondary, 
                                               eulerTransform, 
                                               I.InterpolatorEnum.sitkLinear, 
                                               Registration.SourceImage.MetaData.RescaleIntercept);

            //3-10-24 still struggling with this transform as it's unclear if the origin should be translated or transformed (including rotations)
            I.VectorDouble originOld = itkImageSecondary.GetOrigin();
            //Vector newOrigin = Registration.TransformPoint(Registration.SourceImage.Origin);
            //originOld[0] = newOrigin.X;
            //originOld[1] = newOrigin.Y;
            //originOld[2] = newOrigin.Z;
            originOld[0] = Registration.TranslateX(originOld[0]);
            originOld[1] = Registration.TranslateY(originOld[1]);
            originOld[2] = Registration.TranslateZ(originOld[2]);
            rotated.SetOrigin(originOld);
            ProvideUIUpdate($"Transformed source image origin: ({originOld[0]}, {originOld[1]}, {originOld[2]}) mm");
            return rotated;
        }

        /// <summary>
        /// Actually merge the target image and the transformed source image
        /// </summary>
        /// <param name="itkImagePrimary"></param>
        /// <param name="itkImageSecondaryTransformed"></param>
        /// <returns></returns>
        private I.Image StitchImages(I.Image itkImagePrimary, I.Image itkImageSecondaryTransformed)
        {
            ProvideUIUpdate("Constructing new itk image to hold stitched CT data");
            I.Image itkImageMerged = InitializeStitchedITKImage(itkImagePrimary, itkImageSecondaryTransformed);
            MatchSlice = (int)((itkImagePrimary.GetOrigin()[2] - itkImageMerged.GetOrigin()[2]) / itkImageMerged.GetSpacing()[2]);
            ProvideUIUpdate($"Matchslice location: {MatchSlice}");

            I.VectorInt64 pixelIndex64 = new I.VectorInt64(new long[] { 0, 0, 0 });
            I.VectorUInt32 pixelIndex32 = new I.VectorUInt32(new uint[] { 0, 0, 0 });
            I.VectorUInt32 indexOriginal32 = new I.VectorUInt32(new uint[] { 0, 0, 0 });

            ProvideUIUpdate("Stitching transformed source image now");
            //interpolate CT pixel data from transformed source image
            for (int z = 0; z < MatchSlice; z++)
            {
                ProvideUIUpdate((int)(100 * z / itkImageMerged.GetSize()[2]), $"Processing slice: {z}");
                pixelIndex32[2] = (uint)z;
                pixelIndex64[2] = z;

                for (int x = 0; x < itkImageMerged.GetSize()[0]; x++)
                {
                    pixelIndex64[0] = x;
                    pixelIndex32[0] = (uint)x;
                    for (int y = 0; y < itkImageMerged.GetSize()[1]; y++)
                    {
                        pixelIndex64[1] = y;
                        pixelIndex32[1] = (uint)y;
                        using (I.VectorDouble physicalCoordinate = itkImageMerged.TransformIndexToPhysicalPoint(pixelIndex64))
                        {
                            using (I.VectorInt64 index = itkImageSecondaryTransformed.TransformPhysicalPointToIndex(physicalCoordinate))
                            {
                                if (PixelIndexOutofBound(index, itkImageSecondaryTransformed))
                                {
                                    itkImageMerged.SetPixelAsInt16(pixelIndex32, (short)Registration.SourceImage.MetaData.RescaleIntercept);
                                }
                                else
                                {
                                    indexOriginal32[0] = (uint)index[0];
                                    indexOriginal32[1] = (uint)index[1];
                                    indexOriginal32[2] = (uint)index[2];
                                    var pixelValue = itkImageSecondaryTransformed.GetPixelAsInt16(indexOriginal32);
                                    itkImageMerged.SetPixelAsInt16(pixelIndex32, pixelValue);
                                }
                            }
                        }
                    }
                }
            }
            itkImageMerged = AddPrimaryImageSlicesToStitchedImage(itkImageMerged, itkImagePrimary);
            
            return itkImageMerged;
        }

        /// <summary>
        /// Utility method to determine if the requested voxel index is outside of the transformed source image
        /// </summary>
        /// <param name="indexPrimary"></param>
        /// <param name="itkImageSecondaryTransformed"></param>
        /// <returns></returns>
        private bool PixelIndexOutofBound(I.VectorInt64 indexPrimary, I.Image itkImageSecondaryTransformed)
        {
            uint x = (uint)indexPrimary[0];
            uint y = (uint)indexPrimary[1];
            uint z = (uint)indexPrimary[2];
            if (x < 0 || x >= itkImageSecondaryTransformed.GetSize()[0]) return true;
            if (y < 0 || y >= itkImageSecondaryTransformed.GetSize()[1]) return true;
            if (z < 0 || z >= itkImageSecondaryTransformed.GetSize()[2]) return true;
            return false;
        }

        /// <summary>
        /// Helper method to add the existing image slice data for the target image to the stitched CT image. No need to interpolate since the voxel positions
        /// in the stitched image are identical to the target image
        /// </summary>
        /// <param name="itkImageMerged"></param>
        /// <param name="itkImagePrimary"></param>
        /// <returns></returns>
        private I.Image AddPrimaryImageSlicesToStitchedImage(I.Image itkImageMerged, I.Image itkImagePrimary)
        {
            ProvideUIUpdate("Adding existing target image slices to stitched image");
            I.VectorUInt32 pixelIndex32 = new I.VectorUInt32(new uint[] { 0, 0, 0 });
            //take CT data from target CT image. No need to interpolate as the target pixel grid is the same as the merged image pixel grid
            for (int z = MatchSlice; z < itkImageMerged.GetSize()[2]; z++)
            {
                ProvideUIUpdate((int)(100 * z / itkImageMerged.GetSize()[2]), $"Processing slice: {z}");
                pixelIndex32[2] = (uint)z;

                for (int x = 0; x < itkImageMerged.GetSize()[0]; x++)
                {
                    pixelIndex32[0] = (uint)x;
                    for (int y = 0; y < itkImageMerged.GetSize()[1]; y++)
                    {
                        pixelIndex32[1] = (uint)y;
                        var pixelValue = itkImagePrimary.GetPixelAsInt16(new I.VectorUInt32(new uint[] { pixelIndex32[0],
                                                                                                         pixelIndex32[1],
                                                                                                         (uint)(pixelIndex32[2] - MatchSlice) }));
                        itkImageMerged.SetPixelAsInt16(pixelIndex32, pixelValue);
                    }
                }
            }
            return itkImageMerged;
        }

        /// <summary>
        /// Helper method to initialize an empty itk image with the appropriate meta data for the stitched CT image
        /// </summary>
        /// <param name="itkImagePrimary"></param>
        /// <param name="itkImageSecondaryTransformed"></param>
        /// <returns></returns>
        private I.Image InitializeStitchedITKImage(I.Image itkImagePrimary, I.Image itkImageSecondaryTransformed)
        {
            ProvideUIUpdate("Calculating new number of slices for stitched image");
            // calculate the new number of slices
            double newSlices = CTStitcherHelper.CalculateNumberOfNewSlices(itkImagePrimary,
                                                                           itkImageSecondaryTransformed,
                                                                           Registration.SourceImage.MetaData.ImageOrientation.Z);
            ProvideUIUpdate($"Number of image slices: {newSlices}");

            I.VectorUInt32 image3DSize = new I.VectorUInt32(new uint[] { (uint)itkImagePrimary.GetSize()[0],
                                                                         (uint)itkImagePrimary.GetSize()[1],
                                                                         (uint)Math.Ceiling(newSlices) });
            ProvideUIUpdate($"Stitched image size: {image3DSize[0]}, {image3DSize[1]}, {image3DSize[2]}");

            I.PixelIDValueEnum pixelType = I.PixelIDValueEnum.sitkInt16;
            I.Image itkImageMerged = new I.Image(image3DSize, pixelType);

            itkImageMerged.SetSpacing(new I.VectorDouble(new double[] { itkImagePrimary.GetSpacing()[0],
                                                                        itkImagePrimary.GetSpacing()[1],
                                                                        itkImagePrimary.GetSpacing()[2] }));
            ProvideUIUpdate($"Stitched image resolution: {itkImageMerged.GetSpacing()[0]}, {itkImageMerged.GetSpacing()[1]}, {itkImageMerged.GetSpacing()[2]}");

            double newOriginZ = CTStitcherHelper.CalculateNewZOrigin(itkImagePrimary.GetOrigin()[2], 
                                                                     itkImagePrimary.GetSpacing()[2],
                                                                     (int)itkImagePrimary.GetSize()[2],
                                                                     newSlices);

            itkImageMerged.SetOrigin(new I.VectorDouble(new double[] { itkImagePrimary.GetOrigin()[0], 
                                                                       itkImagePrimary.GetOrigin()[1], 
                                                                       newOriginZ }));
            ProvideUIUpdate($"Stitched image origin: {itkImageMerged.GetOrigin()[0]}, {itkImageMerged.GetOrigin()[1]}, {itkImageMerged.GetOrigin()[2]}");

            itkImageMerged.SetDirection(itkImagePrimary.GetDirection());
            ProvideUIUpdate($"Stitched image orientation: {itkImagePrimary.GetDirection()[0]}, {itkImagePrimary.GetDirection()[1]}, {itkImagePrimary.GetDirection()[2]}");
            return itkImageMerged;
        }
    }
}
