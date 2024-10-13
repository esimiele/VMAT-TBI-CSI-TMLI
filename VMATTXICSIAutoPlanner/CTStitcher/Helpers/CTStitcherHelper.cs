using CTStitcher.Models;
using CTStitcher.Utilities;
using System;
using I = itk.simple;

namespace CTStitcher.Helpers
{
    public static class CTStitcherHelper
    {
        /// <summary>
        /// Helper method to initialize a new CT image to hold the results of the stitching operation.
        /// Ensure the new image has the correct origin and number of slices based on the transform applied
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static CTImageModel InitializeStitchedImage(RegistrationPPModel registration)
        {
            //z posigion of origin in hfs coordinate system
            double zpos = registration.TransformZ(registration.SourceImage.Origin);
            double newSlices = CalculateNumberOfNewSlices(registration.TargetImage, 
                                                          registration.SourceImage, 
                                                          zpos);
            double newZOrigin = CalculateNewZOrigin(registration.TargetImage.Origin.Z, 
                                                    registration.TargetImage.MetaData.ZRes, 
                                                    registration.TargetImage.MetaData.ZSize, 
                                                    newSlices);

            CTImageMetaDataModel data = registration.TargetImage.MetaData;
            data.ZSize = (int)Math.Ceiling(newSlices);
            data.SeriesUID = UIDHelper.MakeNewUID(data.SeriesUID);
            data.StudyUID = UIDHelper.MakeNewUID(data.StudyUID);
            CTImageModel img = new CTImageModel(data);
            img.Origin = new VectorModel(registration.TargetImage.Origin.X, 
                                    registration.TargetImage.Origin.Y, 
                                    newZOrigin);
            //CTImage img = (CTImage)hfs.DeepCopy(numAdditionalSlices);
            return img;
        }

        /// <summary>
        /// Helper method to calculate the new number of image slices for the stitched image. Specifically targeting itk images (6DOF stitcher)
        /// </summary>
        /// <param name="target"></param>
        /// <param name="transformedSource"></param>
        /// <param name="zOrientation"></param>
        /// <returns></returns>
        public static double CalculateNumberOfNewSlices(I.Image target, I.Image transformedSource, double zOrientation)
        {
            double newSlices = target.GetOrigin()[2] + (target.GetSize()[2] - 1) * target.GetSpacing()[2] - transformedSource.GetOrigin()[2];
            if (zOrientation == -1)
            {
                //ffs z data is descending -- > origin is at sup extent of image
                newSlices += (transformedSource.GetSize()[2] - 1) * transformedSource.GetSpacing()[2];
            }
            newSlices /= target.GetSpacing()[2];
            return newSlices;
        }

        /// <summary>
        /// Overloaded method specifically targeting CTImages (3DOF stitcher)
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <param name="transformedSourceOriginZ"></param>
        /// <returns></returns>
        public static double CalculateNumberOfNewSlices(CTImageModel target, CTImageModel source, double transformedSourceOriginZ)
        {
            double newSlices = target.Origin.Z + (target.MetaData.ZSize - 1) * target.MetaData.ZRes - transformedSourceOriginZ;
            if (source.MetaData.ImageOrientation.Z == -1)
            {
                //ffs z data is descending -- > origin is at sup extent of image
                newSlices += (source.MetaData.ZSize - 1) * source.MetaData.ZRes;
            }
            newSlices /= target.MetaData.ZRes;
            return newSlices;
        }

        /// <summary>
        /// Helper method to calculate the new z origin position of the stitched image
        /// </summary>
        /// <param name="tgtImageOriginZ"></param>
        /// <param name="tgtImageZRes"></param>
        /// <param name="tgtImageNumSlices"></param>
        /// <param name="numNewSlices"></param>
        /// <returns></returns>
        public static double CalculateNewZOrigin(double tgtImageOriginZ, double tgtImageZRes, int tgtImageNumSlices, double numNewSlices)
        {
            return tgtImageOriginZ + tgtImageZRes * (tgtImageNumSlices - Math.Ceiling(numNewSlices));
        }
    }
}
