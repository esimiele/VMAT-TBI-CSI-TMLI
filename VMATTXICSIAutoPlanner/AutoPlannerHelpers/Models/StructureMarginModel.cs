using AutoPlannerHelpers.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using VMS.TPS.Common.Model.Types;

namespace AutoPlannerHelpers.Models
{
    public class StructureMarginModel : ObservableObject
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
        private StructureMarginType _marginType = StructureMarginType.Uniform;

        public StructureMarginType MarginType
        {
            get { return _marginType; }
            set { SetProperty(ref _marginType, value); OnPropertyChanged(nameof(AxisAlignedMargins)); }
        }

        private MarginGeometryType _geometryType = MarginGeometryType.Outer;
        public MarginGeometryType GeometryType
        {
            get => _geometryType;
            set { SetProperty(ref _geometryType, value); OnPropertyChanged(nameof(AxisAlignedMargins)); }
        }

        private double _x1 = double.NaN;
        public double x1
        {
            get => _x1;
            set { SetProperty(ref _x1, value); OnPropertyChanged(nameof(AxisAlignedMargins)); }
        }

        private double _y1 = double.NaN;
        public double y1
        {
            get => _y1;
            set { SetProperty(ref _y1, value); OnPropertyChanged(nameof(AxisAlignedMargins)); }
        }

        private double _z1 = double.NaN;
        public double z1
        {
            get => _z1;
            set { SetProperty(ref _z1, value); OnPropertyChanged(nameof(AxisAlignedMargins)); }
        }

        private double _x2 = double.NaN;
        public double x2
        {
            get => _x2;
            set { SetProperty(ref _x2, value); OnPropertyChanged(nameof(AxisAlignedMargins)); }
        }

        private double _y2 = double.NaN;
        public double y2
        {
            get => _y2;
            set { SetProperty(ref _y2, value); OnPropertyChanged(nameof(AxisAlignedMargins)); }
        }

        private double _z2 = double.NaN;
        public double z2
        {
            get => _z2;
            set { SetProperty(ref _z2, value); OnPropertyChanged(nameof(AxisAlignedMargins)); }
        }

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

        public StructureMarginModel(StructureMarginModel model)
        {
            UpdateMargin(model);
        }

        public void UpdateMargin(StructureMarginModel model)
        {
            MarginType = model.MarginType;
            if (_marginType == StructureMarginType.Uniform)
            {
                GeometryType = model.x1 > 0 ? MarginGeometryType.Outer : MarginGeometryType.Inner;
                x1 = x2 = y1 = y2 = z1 = z2 = Math.Abs(model.x1);
            }
            else
            {
                GeometryType = model.GeometryType;
                x1 = model.x1;
                x2 = model.x2;
                y1 = model.y1;
                y2 = model.y2;
                z1 = model.z1;
                z2 = model.z2;
            }
        }
    }
}
