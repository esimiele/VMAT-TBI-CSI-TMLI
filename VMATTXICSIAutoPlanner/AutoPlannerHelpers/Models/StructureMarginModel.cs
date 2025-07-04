using AutoPlannerHelpers.Enums;
using System;
using VMS.TPS.Common.Model.Types;

namespace AutoPlannerHelpers.Models
{
    public class StructureMarginModel
    {
        public bool IsValidMargin { get => !double.IsNaN(x1) && x1 >= -5.0 && x1 <= 5.0 &&
                                           (MarginType == StructureMarginType.Uniform ||
                                           (MarginType == StructureMarginType.Asymmetric &&
                                           x1 > 0.0 && 
                                           !double.IsNaN(x2) && x2 >= 0.0 && x2 <= 5.0 &&
                                           !double.IsNaN(y1) && y1 >= 0.0 && y1 <= 5.0 &&
                                           !double.IsNaN(y2) && y2 >= 0.0 && y2 <= 5.0 &&
                                           !double.IsNaN(z1) && z1 >= 0.0 && z1 <= 5.0 &&
                                           !double.IsNaN(z2) && z2 >= 0.0 && z2 <= 5.0)); }
        public AxisAlignedMargins AxisAlignedMargins
        {
            get => IsValidMargin ? new AxisAlignedMargins(GeometryType == MarginGeometryType.Outer ? StructureMarginGeometry.Outer : StructureMarginGeometry.Inner,
                                                                                      this.x1 * 10,
                                                                                      this.y1 * 10,
                                                                                      this.z1 * 10,
                                                                                      this.x2 * 10,
                                                                                      this.y2 * 10,
                                                                                      this.z2 * 10) :
                                    new AxisAlignedMargins();
        }
        public StructureMarginType MarginType { get; set; } = StructureMarginType.Uniform;
        public MarginGeometryType GeometryType { get; set; } = MarginGeometryType.Outer;
        public double x1 { get; set; } = double.NaN;
        public double y1 { get; set; } = double.NaN;
        public double z1 { get; set; } = double.NaN;
        public double x2 { get; set; } = double.NaN;
        public double y2 { get; set; } = double.NaN;
        public double z2 { get; set; } = double.NaN;

        public StructureMarginModel() { }

        public StructureMarginModel(double margin) 
        { 
            MarginType = StructureMarginType.Uniform;
            GeometryType = margin > 0 ? MarginGeometryType.Outer : MarginGeometryType.Inner;
            x1 = x2 = y1 = y2 = z1 = z2 = Math.Abs(margin);
        }

        public StructureMarginModel(MarginGeometryType geoType, double X1, double Y1, double Z1, double X2, double Y2, double Z2)
        {
            MarginType = StructureMarginType.Asymmetric;
            GeometryType = geoType;
            x1 = X1;
            x2 = X2;
            y1 = Y1;
            y2 = Y2;
            z1 = Z1;
            z2 = Z2;
        }
    }
}
