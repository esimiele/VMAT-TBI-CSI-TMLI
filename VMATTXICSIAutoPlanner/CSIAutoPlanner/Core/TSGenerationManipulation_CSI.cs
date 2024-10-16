using AutoPlannerHelpers.BaseCore;
using AutoPlannerHelpers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSIAutoPlanner.Core
{
    class TSGenerationManipulation_CSI : TSGenerationManipulationBase
    {
        public int NumberofIsocenters { get; private set; } = -1;
        public int NumberofVMATIsocenters { get; private set; } = -1;
        //plan id, normalization volume
        public Dictionary<string, string> NormalizationVolumes { get; private set; } = new Dictionary<string, string> { };
        public TSGenerationManipulation_CSI(List<RequestedTSStructureModel> ts, List<RequestedTSManipulationModel> manipulation, List<PrescriptionModel> presc) { }
    }
}
