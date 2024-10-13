using CTStitcher.Helpers;
using CTStitcher.Interfaces;
using CTStitcher.Models;
using CTStitcher.Utilities;
using SimpleProgressWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CTStitcher.Stitchers
{
    public class Stitcher3DOF : SimpleMTbase, IStitcher
    {
        //get methods
        public CTImageModel StitchedCT { get; private set; }
        public string ErrorMessage { get; private set; }
        public int MatchSlice { get; private set; }

        //data members
        private object locker = new object();
        private double[] targetXPos;
        private double[] targetYPos;
        private double[] transformedXPos;
        private double[] transformedYPos;
        private double[] transformedZPos;
        private short[][,] transformedCTPixelData;
        private int numSlices;
        private int slicesCompleted = 0;
        private List<Task<short[,]>> tasks = new List<Task<short[,]>>();
        private RegistrationPPModel Registration;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="registration"></param>
        public Stitcher3DOF(RegistrationPPModel registration)
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
                if (PreliminaryChecks()) return true;
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
        /// Preliminary checks prior to stitching the images together
        /// </summary>
        /// <returns></returns>
        private bool PreliminaryChecks()
        {
            UpdateUILabel("Preliminary checks");
            if(Registration.HasRotations)
            {
                ErrorMessage = $"Error! Selected image registration has rotations!" + Environment.NewLine;
                ErrorMessage += $"Rotations: ({Registration.Rotations.X}, {Registration.Rotations.Y}, {Registration.Rotations.Z})" + Environment.NewLine;
                ErrorMessage += $"Using 3DOF stitching algorithm with a registration that has rotations will not be accurate! Exiting";
                ProvideUIUpdate(ErrorMessage, true);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Method to perform the actual merging of the HFS and FFS CT scans. Only consider translations in registration
        /// </summary>
        /// <returns></returns>
        public CTImageModel StitchCTImages()
        {
            UpdateUILabel($"Starting CT Concatenation");
            ProvideUIUpdate("Using 3DOF algorithm for stitching images");
            
            //faster indexing of source image x/y positions then regular vector
            (targetXPos, targetYPos) = BuildXYPosArrayForImage(Registration.TargetImage);
            (double[] sourceXPos, double[] sourceYPos) = BuildXYPosArrayForImage(Registration.SourceImage);
            (transformedXPos, transformedYPos, transformedZPos) = BuildTransformedPositionArrayFast(sourceXPos, sourceYPos, Registration);
            
            UpdateUILabel($"Initializing new CT image");
            ProvideUIUpdate($"Adding existing target image slices to stitched CT");
            CTImageModel stitchedImage = AddSlicesToStitchedImage(CTStitcherHelper.InitializeStitchedImage(Registration), Registration.TargetImage);
            
            //only perform the transformation/calculation
            numSlices = stitchedImage.MetaData.ZSize - Registration.TargetImage.Slices.Count();
            ProvideUIUpdate($"Number of image slices to tranform: {numSlices}");
            transformedCTPixelData = new short[numSlices][,];
            MatchSlice = (int)((Registration.TargetImage.Origin.Z - stitchedImage.Origin.Z) / stitchedImage.MetaData.ZRes);
            ProvideUIUpdate($"Match Slice Number: {MatchSlice}");

            TransformCTDataMT(stitchedImage.Origin.Z, stitchedImage.MetaData.ZRes, stitchedImage.MetaData.ImageOrientation.Z);
            stitchedImage = AddTransformedSlicesToStitchedImage(stitchedImage);

            return stitchedImage;
        }

        /// <summary>
        /// Utility method to asynchronously transform the FFS CT slices to the HFS coordinate system
        /// </summary>
        /// <param name="initialTargetZLocation"></param>
        /// <param name="zRes"></param>
        /// <param name="orientation"></param>
        /// <returns></returns>
        private bool TransformCTDataMT(double initialTargetZLocation, double zRes, double orientation)
        {
            UpdateUILabel("Transforming slice data:");
            ProvideUIUpdate(0);
            for (int i = 0; i < numSlices; i++)
            {
                int slice = i;
                tasks.Add(new TaskFactory().StartNew(() => TransformCTSlice(initialTargetZLocation + slice * zRes * orientation)).ContinueWith((task) => UpdateArrayAndUI(task, slice)));
            }
            Task.WaitAll(tasks.ToArray());
            return false;
        }

        /// <summary>
        /// Utility methods to update the UI when a transform task finishes
        /// </summary>
        /// <param name="task"></param>
        /// <param name="slice"></param>
        /// <returns></returns>
        private short[,] UpdateArrayAndUI(Task<short[,]> task, int slice)
        {
            lock (locker)
            {
                transformedCTPixelData[slice] = task.Result;
                ProvideUIUpdate(100 * ++slicesCompleted / numSlices, $"Slice: {slice} transformed");
            }
            return task.Result;
        }

        /// <summary>
        /// Utility method to actually perform the position transformation of the CT slice of interest. Resample the transformed data onto the HFS CT grid
        /// using tri-linear interpolation
        /// </summary>
        /// <param name="targetSliceLocation"></param>
        /// <returns></returns>
        private short[,] TransformCTSlice(double targetSliceLocation)
        {
            
            //need to know image z direction
            int lowZIndex = LocalizePosInTransformedImagePositions(targetSliceLocation, transformedZPos);
            if (lowZIndex == -1) return null;

            short[,] newPixelData = new short[targetXPos.Length, targetYPos.Length];
            for (int i = 0; i < targetXPos.Length; i++)
            {
                int lowXIndex = LocalizePosInTransformedImagePositions(targetXPos[i], transformedXPos);
                for (int j = 0; j < targetYPos.Length; j++)
                {
                    int lowYIndex = LocalizePosInTransformedImagePositions(targetYPos[j], transformedYPos);
                    newPixelData[i, j] = Interpolators.TriLinearInterpolation(BuildCube(lowXIndex,
                                                                                        lowYIndex,
                                                                                        lowZIndex,
                                                                                        Registration.SourceImage.MetaData.XSize,
                                                                                        Registration.SourceImage.MetaData.YSize,
                                                                                        Registration.SourceImage.MetaData.ZSize),
                                                                                new VectorModel(targetXPos[i], targetYPos[j], targetSliceLocation),
                                                                                (short)Registration.SourceImage.MetaData.RescaleIntercept);
                }
            }
            return newPixelData;
        }

        /// <summary>
        /// Helper method to succiently build a cube of the eight nearest voxel neighbors (including HU value)
        /// </summary>
        /// <param name="lowX"></param>
        /// <param name="lowY"></param>
        /// <param name="lowZ"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <param name="maxZ"></param>
        /// <returns></returns>
        private CubeModel BuildCube(int lowX, int lowY, int lowZ, int maxX, int maxY, int maxZ)
        {
            int lowXPP = Math.Min(lowX + 1, maxX - 1);
            int lowYPP = Math.Min(lowY + 1, maxY - 1);
            int lowZPP = Math.Min(lowZ + 1, maxZ - 1);

            CubeModel cube = new CubeModel(lowX, lowY, lowXPP, lowYPP)
            {
                c000 = new Vector4DModel(transformedXPos[lowX], transformedYPos[lowY], transformedZPos[lowZ], Registration.SourceImage.Slices.ElementAt(lowZ).PixelData[lowX, lowY]),
                c100 = new Vector4DModel(transformedXPos[lowXPP], transformedYPos[lowY], transformedZPos[lowZ], Registration.SourceImage.Slices.ElementAt(lowZ).PixelData[lowXPP, lowY]),
                c010 = new Vector4DModel(transformedXPos[lowX], transformedYPos[lowYPP], transformedZPos[lowZ], Registration.SourceImage.Slices.ElementAt(lowZ).PixelData[lowX, lowYPP]),
                c110 = new Vector4DModel(transformedXPos[lowXPP], transformedYPos[lowYPP], transformedZPos[lowZ], Registration.SourceImage.Slices.ElementAt(lowZ).PixelData[lowXPP, lowYPP]),

                c001 = new Vector4DModel(transformedXPos[lowX], transformedYPos[lowY], transformedZPos[lowZPP], Registration.SourceImage.Slices.ElementAt(lowZPP).PixelData[lowX, lowY]),
                c101 = new Vector4DModel(transformedXPos[lowXPP], transformedYPos[lowY], transformedZPos[lowZPP], Registration.SourceImage.Slices.ElementAt(lowZPP).PixelData[lowXPP, lowY]),
                c011 = new Vector4DModel(transformedXPos[lowX], transformedYPos[lowYPP], transformedZPos[lowZPP], Registration.SourceImage.Slices.ElementAt(lowZPP).PixelData[lowX, lowYPP]),
                c111 = new Vector4DModel(transformedXPos[lowXPP], transformedYPos[lowYPP], transformedZPos[lowZPP], Registration.SourceImage.Slices.ElementAt(lowZPP).PixelData[lowXPP, lowYPP]),
            };

            return cube;
        }

        /// <summary>
        /// Method to add the transformed/interpolated slices of the CT image to the stitched image
        /// </summary>
        /// <param name="stitchedImage"></param>
        /// <returns></returns>
        private CTImageModel AddTransformedSlicesToStitchedImage(CTImageModel stitchedImage)
        {
            for (int slice = 0; slice < numSlices; slice++)
            {
                ImageSliceModel newSlice = new ImageSliceModel(transformedCTPixelData[slice]);
                double targetSliceLocation = stitchedImage.Origin.Z + slice * stitchedImage.MetaData.ZRes * stitchedImage.MetaData.ImageOrientation.Z;
                newSlice.Origin = new VectorModel(targetXPos[0], targetYPos[0], targetSliceLocation);
                newSlice.SliceZLocation = targetSliceLocation;
                stitchedImage.AddImageSlice(newSlice);
            }
            return stitchedImage;
        }

        /// <summary>
        /// Utility method to copy the existing slices in the target CT image into the stitched CT image
        /// </summary>
        /// <param name="stitchedImage"></param>
        /// <param name="targetImg"></param>
        /// <returns></returns>
        private CTImageModel AddSlicesToStitchedImage(CTImageModel stitchedImage, CTImageModel targetImg)
        {
            foreach(ImageSliceModel itr in targetImg.Slices)
            {
                ImageSliceModel newSlice = new ImageSliceModel(itr.PixelData);
                newSlice.Origin = new VectorModel(itr.Origin);
                newSlice.SliceZLocation = newSlice.Origin.Z;
                stitchedImage.AddImageSlice(newSlice);
            }
            return stitchedImage;
        }

        /// <summary>
        /// Utility method to quickly localize the nearest-neighbors in the array based on the supplied target position
        /// </summary>
        /// <param name="targetLocation"></param>
        /// <param name="posArray"></param>
        /// <returns></returns>
        private int LocalizePosInTransformedImagePositions(double targetLocation, double[] posArray)
        {
            double lowPos;
            List<double> PosList = posArray.ToList();
            bool increasingReadDirection = posArray[1] > posArray[0] ? true : false;
            if (increasingReadDirection)
            {
                //positive read direction --> voxel positions go from smallest to largets with increasing voxel index
                //HFS --> x,y increasing with increasing voxel index
                //z decreasing with increasing voxel index
                if (PosList.Any(x => x <= targetLocation))
                {
                    lowPos = PosList.Last(x => x <= targetLocation);
                }
                else return 0;
            }
            else
            {
                //negative read direction --> voxel positions go from largest to smallest with increasing voxel index
                //FFS --> z,y increasing with increasing voxel index
                //x decreasing with increasing voxel index
                if (PosList.Any(x => x >= targetLocation))
                {
                    lowPos = PosList.Last(x => x >= targetLocation);
                }
                else return posArray.Length - 1;
            }
            return PosList.IndexOf(lowPos);
        }

        /// <summary>
        /// Utility method to build 1D arrays of the x, y, and z positions of the transformed source image (3DOF registration --> spacing resultion between x,y,z 
        /// directions is preserved through the transform)
        /// </summary>
        /// <param name="xPos"></param>
        /// <param name="yPos"></param>
        /// <param name="reg"></param>
        /// <returns></returns>
        private (double[], double[], double[]) BuildTransformedPositionArrayFast(double[] xPos, double[] yPos, RegistrationPPModel reg)
        {
            UpdateUILabel("Transforming 1D x/y/z position arrays:");
            double[] transformedXPos = new double[reg.SourceImage.MetaData.XSize];
            double[] transformedYPos = new double[reg.SourceImage.MetaData.YSize];
            double[] transformedZPos = new double[reg.SourceImage.MetaData.ZSize];
            int percentComplete = 0;
            int calcItems = reg.SourceImage.Slices.Count() * xPos.Length * yPos.Length;
            ImageSliceModel[] slices = reg.SourceImage.Slices.ToArray();
            for (int k = 0; k < slices.Length; k++)
            {
                ProvideUIUpdate(100 * ++percentComplete / calcItems);
                transformedZPos[k] = reg.TranslateZ(slices[k].SliceZLocation);
            }
            for (int i = 0; i < xPos.Length; i++)
            {
                ProvideUIUpdate(100 * ++percentComplete / calcItems);
                transformedXPos[i] = reg.TranslateX(xPos[i]);
            }
            for (int i = 0; i < yPos.Length; i++)
            {
                ProvideUIUpdate(100 * ++percentComplete / calcItems);
                transformedYPos[i] = reg.TranslateY(yPos[i]);
            }
            ProvideUIUpdate($"Finished translating 1D x/y/z position arrays for source image: {reg.SourceImage.MetaData.Id}");
            return (transformedXPos, transformedYPos, transformedZPos);
        }

        /// <summary>
        /// Helper method to build 1D position arrays for the x,y directions from the supplied CT image
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        private (double[], double[]) BuildXYPosArrayForImage(CTImageModel img)
        {
            UpdateUILabel("Building 1D x/y position arrays:");
            double[] xPos = new double[img.MetaData.XSize];
            double[] yPos = new double[img.MetaData.YSize];
            for (int i = 0; i < img.MetaData.XSize; i++)
            {
                //need to know positive x and y directions
                xPos[i] = img.Origin.X + i * img.MetaData.ImageOrientation.X * img.MetaData.XRes;
            }
            for (int i = 0; i < img.MetaData.YSize; i++)
            {
                //need to know positive x and y directions
                yPos[i] = img.Origin.Y + i * img.MetaData.ImageOrientation.Y * img.MetaData.YRes;
            }
            ProvideUIUpdate($"Finished building 1D x/y position arrays for image: {img.MetaData.Id}");
            return (xPos, yPos);
        }
    }
}
