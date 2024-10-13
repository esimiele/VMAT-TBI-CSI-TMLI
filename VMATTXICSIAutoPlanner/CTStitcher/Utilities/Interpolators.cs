using CTStitcher.Models;
using System;

namespace CTStitcher.Utilities
{
    public static class Interpolators
    {
        /// <summary>
        /// Utility method to perform linear interpolation
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="x1"></param>
        /// <param name="y0"></param>
        /// <param name="y1"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static short LinearInterpolation(double x0, double x1, double y0, double y1, double x)
        {
            return (short)Math.Round((y0 * (x1 - x) + y1 * (x - x0)) / (x1 - x0));
        }

        /// <summary>
        /// Utility method to perform tri-linear interpolation to the target position
        /// </summary>
        /// <param name="c"></param>
        /// <param name="targetPos"></param>
        /// <param name="minValue"></param>
        /// <returns></returns>
        public static short TriLinearInterpolation(CubeModel c, VectorModel targetPos, short minValue)
        {
            Vector4DModel c00;
            Vector4DModel c01;
            Vector4DModel c10;
            Vector4DModel c11;

            if (c.lowX < c.lowXPP)
            {
                //interpolate along x on lowY,lowZ
                c00 = new Vector4DModel(new VectorModel(targetPos.X,
                                                       c.c000.Position.Y,
                                                       c.c000.Position.Z),
                                                       LinearInterpolation(c.c000.Position.X, c.c100.Position.X, c.c000.Value, c.c100.Value, targetPos.X));

                //interpolate along x on lowY,lowZ+1
                c01 = new Vector4DModel(new VectorModel(targetPos.X,
                                                       c.c001.Position.Y,
                                                       c.c001.Position.Z),
                                                       LinearInterpolation(c.c001.Position.X, c.c101.Position.X, c.c001.Value, c.c101.Value, targetPos.X));

                //interpolate along x on lowY+1,lowZ
                c10 = new Vector4DModel(new VectorModel(targetPos.X,
                                                       c.c010.Position.Y,
                                                       c.c010.Position.Z),
                                                       LinearInterpolation(c.c010.Position.X, c.c110.Position.X, c.c010.Value, c.c110.Value, targetPos.X));

                //interpolate along x on lowY+1,lowZ+1
                c11 = new Vector4DModel(new VectorModel(targetPos.X,
                                                       c.c011.Position.Y,
                                                       c.c011.Position.Z),
                                                       LinearInterpolation(c.c011.Position.X, c.c111.Position.X, c.c011.Value, c.c111.Value, targetPos.X));
            }
            else
            {
                //no need to interpolate along x since lowX == lowXPP
                //interpolate along x on lowY,lowZ
                c00 = new Vector4DModel(new VectorModel(targetPos.X,
                                                       c.c000.Position.Y,
                                                       c.c000.Position.Z),
                                                       c.c000.Value);

                //interpolate along x on lowY,lowZ+1
                c01 = new Vector4DModel(new VectorModel(targetPos.X,
                                                       c.c001.Position.Y,
                                                       c.c001.Position.Z),
                                                       c.c001.Value);

                //interpolate along x on lowY+1,lowZ
                c10 = new Vector4DModel(new VectorModel(targetPos.X,
                                                       c.c010.Position.Y,
                                                       c.c010.Position.Z),
                                                       c.c010.Value);

                //interpolate along x on lowY+1,lowZ+1
                c11 = new Vector4DModel(new VectorModel(targetPos.X,
                                                       c.c011.Position.Y,
                                                       c.c011.Position.Z),
                                                       c.c011.Value);
            }

            Vector4DModel c0;
            Vector4DModel c1;
            if (c.lowY < c.lowYPP)
            {
                //interpolate along y on targetX,lowZ
                c0 = new Vector4DModel(new VectorModel(c00.Position.X,
                                                targetPos.Y,
                                                c00.Position.Z),
                                                LinearInterpolation(c00.Position.Y, c10.Position.Y, c00.Value, c10.Value, targetPos.Y));

                //interpolate along y on targetX,lowZ+1
                c1 = new Vector4DModel(new VectorModel(c01.Position.X,
                                                targetPos.Y,
                                                c01.Position.Z),
                                                LinearInterpolation(c01.Position.Y, c11.Position.Y, c01.Value, c11.Value, targetPos.Y));
            }
            else
            {
                //interpolate along y on targetX,lowZ
                c0 = new Vector4DModel(new VectorModel(c00.Position.X,
                                                targetPos.Y,
                                                c00.Position.Z),
                                                c00.Value);

                //interpolate along y on targetX,lowZ+1
                c1 = new Vector4DModel(new VectorModel(c01.Position.X,
                                                targetPos.Y,
                                                c01.Position.Z),
                                                c01.Value);
            }

            //interpolate along z on targetX, targetY
            return Math.Max(LinearInterpolation(c0.Position.Z, c1.Position.Z, c0.Value, c1.Value, targetPos.Z), minValue);
        }
    }
}
