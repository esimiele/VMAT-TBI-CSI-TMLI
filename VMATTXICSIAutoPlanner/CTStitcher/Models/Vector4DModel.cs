namespace CTStitcher.Models
{
    /// <summary>
    /// Simple class to hold positional and intensity information and reduce reliance on ESAPI libraries.
    /// Didn't use Vector4 as all underlying data is stored as float in that class (makes for a nightmare in terms of casting)
    /// </summary>
    public class Vector4DModel
    {
        public short Value { get; private set; }
        public VectorModel Position { get; private set; }
        public Vector4DModel(VectorModel v, short value)
        {
            Position = v;
            Value = value;
        }

        public Vector4DModel(double x, double y, double z, short w)
        {
            Position = new VectorModel(x, y, z);
            Value = w;
        }
    }
}
