using System.Collections.Generic;
using System.Linq;

namespace CTStitcher.Models
{
    /// <summary>
    /// Simple class to hold positional information and reduce reliance on ESAPI libraries (i.e., VVector).
    /// Didn't use Vector3 as all underlying data is stored as float in that class (makes for a nightmare in terms of casting)
    /// </summary>
    public class VectorModel
    {
        public double X { get => _x; set => _x = value; }
        public double Y { get => _y; set => _y = value; }
        public double Z { get => _z; set => _z = value; }

        //data member
        private double _x, _y, _z;

        public VectorModel(double x, double y, double z)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        public VectorModel(VectorModel v)
        {
            _x = v.X;
            _y = v.Y;
            _z = v.Z;
        }

        public VectorModel()
        {
            _x = 0;
            _y = 0;
            _z = 0;
        }

        public VectorModel(IEnumerable<double> pos)
        {
            _x = pos.ElementAt(0);
            _y = pos.ElementAt(1);
            _z = pos.ElementAt(2);
        }
    }
}
