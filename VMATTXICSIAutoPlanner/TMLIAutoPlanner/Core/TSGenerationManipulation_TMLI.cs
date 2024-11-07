using AutoPlannerHelpers.BaseCore;
using AutoPlannerHelpers.Models;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

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
        private List<TSRingStructureModel> _requestedRings;
        #endregion

        internal TSGenerationManipulation_TMLI(List<RequestedTSStructureModel> ts, 
                                               List<RequestedTSManipulationModel> manipulations, 
                                               List<TSRingStructureModel> rings,
                                               List<PrescriptionModel> presc)
        {
            TS_structures = new List<RequestedTSStructureModel>(ts);
            _requestedRings = new List<TSRingStructureModel>(rings);
            prescriptions = new List<PrescriptionModel>(presc);
        }

        [HandleProcessCorruptedStateExceptions]
        public override bool Run()
        {
            try
            {
                UpdateUILabel("Finished!");
                ProvideUIUpdate(100, "Finished Structure Tuning!");
                ProvideUIUpdate($"Run time: {GetElapsedTime()} (mm:ss)");
            }
            catch(Exception e)
            {
                ProvideUIUpdate($"{e.Message}", true);
                return true;
            }
            return false;
        }

        protected override bool PreliminaryChecks()
        {
            throw new System.NotImplementedException();
        }

        protected override bool CreateTSStructures()
        {
            throw new System.NotImplementedException();
        }

        protected override bool PerformTSStructureManipulation()
        {
            throw new System.NotImplementedException();
        }

        protected override bool CalculateNumIsos()
        {
            throw new System.NotImplementedException();
        }
    }
}
