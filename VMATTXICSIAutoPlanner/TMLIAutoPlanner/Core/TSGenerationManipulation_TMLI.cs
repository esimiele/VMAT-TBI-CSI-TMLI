using AutoPlannerHelpers.BaseCore;
using AutoPlannerHelpers.Models;
using System.Collections.Generic;

namespace TMLIAutoPlanner.Core
{
    internal class TSGenerationManipulation_TMLI : TSGenerationManipulationBase
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
        #endregion

        internal TSGenerationManipulation_TMLI(List<RequestedTSStructureModel> ts, List<RequestedTSManipulationModel> manipulations, List<PrescriptionModel> presc)
        {
            TS_structures = new List<RequestedTSStructureModel>(ts);
            prescriptions = new List<PrescriptionModel>(presc);
        }
    }
}
