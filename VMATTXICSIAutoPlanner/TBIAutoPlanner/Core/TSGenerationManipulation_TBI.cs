using AutoPlannerHelpers.BaseCore;
using AutoPlannerHelpers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;

namespace TBIAutoPlanner.Core
{
    internal class TSGenerationManipulation_TBI : TSGenerationManipulationBase
    {
        #region properties
        public int NumberofIsocenters { get; private set; } = -1;
        public int NumberofVMATIsocenters { get; private set; } = -1;
        //plan id, normalization volume
        public Dictionary<string, string> NormalizationVolumes { get; private set; } = new Dictionary<string, string> { };
        #endregion

        #region fields
        //DICOM types
        //Possible values are "AVOIDANCE", "CAVITY", "CONTRAST_AGENT", "CTV", "EXTERNAL", "GTV", "IRRAD_VOLUME", 
        //"ORGAN", "PTV", "TREATED_VOLUME", "SUPPORT", "FIXATION", "CONTROL", and "DOSE_REGION". 
        private List<PrescriptionModel> prescriptions;
        private List<RequestedTSStructureModel> TS_structures;
        private double targetMargin;
        private Structure flashStructure = null;
        private double flashMargin;
        private bool useFlash = false;
        #endregion

        internal TSGenerationManipulation_TBI(List<RequestedTSStructureModel> ts,
                                              List<RequestedTSManipulationModel> list,
                                              List<PrescriptionModel> presc,
                                              StructureSet ss, 
                                              double tm, 
                                              bool flash,
                                              bool closePW) 
        {
            
        }

        public override bool Run()
        {
            return false;
        }
    }
}
