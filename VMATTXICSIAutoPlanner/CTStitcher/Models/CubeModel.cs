namespace CTStitcher.Models
{
    /// <summary>
    /// A simple class to hold the center voxel coordinates (and HU values) of the eight nearest-neighbors to the point of interest
    /// </summary>
    public class CubeModel
    {
        //get/set methods
        public Vector4DModel c000 { get; set; }
        public Vector4DModel c100 { get; set; }
        public Vector4DModel c010 { get; set; }
        public Vector4DModel c110 { get; set; }

        public Vector4DModel c001 { get; set; }
        public Vector4DModel c101 { get; set; }
        public Vector4DModel c011 { get; set; }
        public Vector4DModel c111 { get; set; }

        //data members
        public int lowX { get; }
        public int lowY { get; }
        public int lowXPP { get; }
        public int lowYPP { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="xpp"></param>
        /// <param name="y"></param>
        /// <param name="ypp"></param>
        public CubeModel(int x, int xpp, int y, int ypp)
        {
            lowX = x;
            lowXPP = xpp;
            lowY = y;
            lowYPP = ypp;
        }
    }
}
