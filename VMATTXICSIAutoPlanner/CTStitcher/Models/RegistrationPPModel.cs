using System;
using VMS.TPS.Common.Model.API;

namespace CTStitcher.Models
{
    public class RegistrationPPModel
    {
        //get methods
        public CTImageModel SourceImage { get; private set; }
        public CTImageModel TargetImage { get; private set; }
        public VectorModel Translations { get; private set; } = new VectorModel(); // in mm
        public VectorModel Rotations { get; private set; } = new VectorModel(); //in deg
        public double[,] TransformMatrix { get; private set; }
        public bool HasRotations { get => Math.Abs(Rotations.X) >= 0.001 || Math.Abs(Rotations.Y) >= 0.001 || Math.Abs(Rotations.Z) >= 0.001; }

        //get/set methods
        public string Id { get; set; } = "";

        /// <summary>
        /// Constructor, first image is the target and the second image is the source in the Eclipse image registration
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <param name="reg"></param>
        public RegistrationPPModel(CTImageModel target, CTImageModel source, Registration reg)
        {
            SourceImage = source;
            TargetImage = target;
            TransformMatrix = reg.TransformationMatrix;
            ParseRotationsTranslations();
            Id = reg.Id;
        }

        /// <summary>
        /// Overloaded constructor and intended to be used for dicom images
        /// </summary>
        /// <param name="hfs"></param>
        /// <param name="ffs"></param>
        /// <param name="rigidTransform"></param>
        /// <param name="id"></param>
        public RegistrationPPModel(CTImageModel hfs, CTImageModel ffs, double[,] rigidTransform, string id = "")
        {
            //Assign FFS image as the source image
            SourceImage = ffs;
            //Assing HFS image as the target image
            TargetImage = hfs;
            TransformMatrix = rigidTransform;
            ParseRotationsTranslations();
            Id = id;
        }

        /// <summary>
        /// Utility method to parse the x,y,z translations and rotations from the 4x4 transformation matrix
        /// </summary>
        private void ParseRotationsTranslations()
        {
            Translations = new VectorModel(TransformMatrix[0, 3], TransformMatrix[1, 3], TransformMatrix[2, 3]);
            //calculate rotations from 4x4 transformation matrix
            double y_theta = -Math.Asin(TransformMatrix[2, 0]);
            double x_theta = Math.Asin(TransformMatrix[2, 1] / Math.Cos(y_theta));
            double z_theta = Math.Asin(TransformMatrix[1, 0] / Math.Cos(y_theta));
            Rotations = new VectorModel(x_theta, y_theta, z_theta);
        }

        /// <summary>
        /// Helper method to translate the supplied position by the x offset
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public double TranslateX(double x)
        {
            return x + TransformMatrix[0, 3];
        }

        /// <summary>
        /// Helper method to translate the supplied position by the y offset
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        public double TranslateY(double y)
        {
            return y + TransformMatrix[1, 3];
        }

        /// <summary>
        /// Helper method to translate the supplied position by the z offset
        /// </summary>
        /// <param name="z"></param>
        /// <returns></returns>
        public double TranslateZ(double z)
        {
            return z + TransformMatrix[2, 3];
        }

        /// <summary>
        /// Helper method to transform the supplied position using the full 6DOF transfrom from the transform matrix
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public VectorModel TransformPoint(VectorModel v)
        {
            return new VectorModel(v.X * TransformMatrix[0, 0] + v.Y * TransformMatrix[0, 1] + v.Z * TransformMatrix[0, 2] + TransformMatrix[0, 3],
                              v.X * TransformMatrix[1, 0] + v.Y * TransformMatrix[1, 1] + v.Z * TransformMatrix[1, 2] + TransformMatrix[1, 3],
                              v.X * TransformMatrix[2, 0] + v.Y * TransformMatrix[2, 1] + v.Z * TransformMatrix[2, 2] + TransformMatrix[2, 3]);
        }

        /// <summary>
        /// Helper method to transform (translation and rotations) the supplied x position
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public double TransformX(VectorModel v)
        {
            return v.X * TransformMatrix[0, 0] + v.Y * TransformMatrix[0, 1] + v.Z * TransformMatrix[0, 2] + TransformMatrix[0, 3];
        }

        /// <summary>
        /// Helper method to transform (translation and rotations) the supplied y position
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public double TransformY(VectorModel v)
        {
            return v.X * TransformMatrix[1, 0] + v.Y * TransformMatrix[1, 1] + v.Z * TransformMatrix[1, 2] + TransformMatrix[1, 3];
        }

        /// <summary>
        /// Helper method to transform (translation and rotations) the supplied z position
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public double TransformZ(VectorModel v)
        {
            return v.X * TransformMatrix[2, 0] + v.Y * TransformMatrix[2, 1] + v.Z * TransformMatrix[2, 2] + TransformMatrix[2, 3];
        }
    }
}
