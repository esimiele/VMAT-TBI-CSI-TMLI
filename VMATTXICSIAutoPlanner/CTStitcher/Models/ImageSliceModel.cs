namespace CTStitcher.Models
{
    public class ImageSliceModel
    {
        //get methods
        public short[,] PixelData { get => _pixelValues; }

        public bool HasPixelData { get => !ReferenceEquals(_pixelValues, null); }

        //get/set methods
        public VectorModel Origin { get; set; } = new VectorModel(0, 0, 0);
        public double SliceZLocation { get; set; } = double.NaN;

        //data members
        private short[,] _pixelValues;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pixels"></param>
        public ImageSliceModel(short[,] pixels)
        {
            _pixelValues = pixels;
        }

        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="o"></param>
        public ImageSliceModel(short[,] pixels, VectorModel o)
        {
            _pixelValues = pixels;
            Origin = o;
            SliceZLocation = o.Z;
        }

        /// <summary>
        /// Utility method to cast the supplied int array as short
        /// </summary>
        /// <param name="pixels"></param>
        public ImageSliceModel(int[,] pixels)
        {
            _pixelValues = new short[pixels.GetLength(0), pixels.GetLength(1)];
            //row
            for (int i = 0; i < pixels.GetLength(0); i++)
            {
                //column
                for (int j = 0; j < pixels.GetLength(1); j++)
                {
                    _pixelValues[i, j] = (short)pixels[i, j];
                }
            }
        }
    }
}
