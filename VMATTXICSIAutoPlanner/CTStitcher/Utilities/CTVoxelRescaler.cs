namespace CTStitcher.Utilities
{
    public static class CTVoxelRescaler
    {
        /// <summary>
        /// Utility method to rescale the supplied value to HU using the supplied slope and intercept
        /// </summary>
        /// <param name="val"></param>
        /// <param name="slope"></param>
        /// <param name="intercept"></param>
        /// <returns></returns>
        public static short ConvertFromVoxelValueToHU(short val, double slope, double intercept)
        {
            return (short)(val * slope + intercept);
        }

        /// <summary>
        /// Utility method to rescale the supplied value to HU using the supplied slope and intercept
        /// </summary>
        /// <param name="val"></param>
        /// <param name="slope"></param>
        /// <param name="intercept"></param>
        /// <returns></returns>
        public static short ConvertFromVoxelValueToHU(int val, double slope, double intercept)
        {
            return (short)(val * slope + intercept);
        }

        /// <summary>
        /// Utility method to rescale the supplied HU to pixel number using the supplied slope and intercept
        /// </summary>
        /// <param name="val"></param>
        /// <param name="slope"></param>
        /// <param name="intercept"></param>
        /// <returns></returns>
        public static short ConvertFromHUToVoxelValue(short val, double slope, double intercept)
        {
            if ((val - intercept) >= 0)
            {
                return (short)((val - intercept) / slope);
            }
            return 0;
        }

        /// <summary>
        /// Utility method to rescale the supplied value using the supplied scalar value
        /// </summary>
        /// <param name="val"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static short RescaleVoxelIntensity(short val, double scalar)
        {
            return (short)(val / scalar);
        }
    }
}
