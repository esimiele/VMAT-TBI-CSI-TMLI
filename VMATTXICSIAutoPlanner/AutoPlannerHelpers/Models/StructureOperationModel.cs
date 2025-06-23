using AutoPlannerHelpers.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPlannerHelpers.Models
{
    public class StructureOperationModel
    {
        public string StructureA { get; set; } = string.Empty;
        public StructureManipulationOperation Operation { get; set; } = StructureManipulationOperation.None;
        public string StructureB { get; set; } = string.Empty;
        public double MarginInCM { get; set; } = double.NaN;
        public StructureOperationModel(string a, StructureManipulationOperation op, string b, double margin)
        {
            StructureA = a;
            Operation = op;
            StructureB = b;
            MarginInCM = margin;
        }
    }
}
