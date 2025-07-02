using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Delegates;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using VMS.TPS.Common.Model.API;

namespace AutoPlannerHelpers.BaseCore
{
    public abstract class GeneratePreliminaryTargetsBase : SimpleProgressWindowViewModel
    {
        // Get methods
        public List<string> AddedTargetstructures { get => _targetsToDerive.Select(x => x.OutputStructure).Distinct().ToList(); }
        public string ErrorStackTrace { get => _stackTraceError; }

        //Dicom type, structure Id
        protected List<StructureOperationModel> _createPrelimTargetList;
        //Dicom type, structure Id
        protected List<StructureOperationModel> _targetsToDerive = new List<StructureOperationModel> { };
        protected string _stackTraceError;
        protected ProvideUIUpdateDelegate PUUD;
        protected UIUpdateMessageOnlyDelegate UIUD;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tgts"></param>
        public GeneratePreliminaryTargetsBase(IEnumerable<StructureOperationModel> tgts, bool closePWOnFinish)
        {
            _createPrelimTargetList = new List<StructureOperationModel>(tgts);
            SetCloseOnFinish(closePWOnFinish, 3000);
        }

        /// <summary>
        /// Run control
        /// </summary>
        /// <returns></returns>
        /// //to handle system access exception violation
        [HandleProcessCorruptedStateExceptions]
        protected override bool Run()
        {
            try
            {
                PUUD = ProvideUIUpdate;
                UIUD = ProvideUIUpdate;
                if (PreliminaryChecks()) return true;
                if (CheckForTargetStructures()) return true;
                if (_targetsToDerive.Any())
                {
                    ProvideUIUpdate("Deriving preliminary targets now!");
                    DeriveTargetStructures();
                    TargetPostProcessing();
                }

                UpdateUILabel("Finished!");
                ProvideUIUpdate(100, "Finished Preparing Structure Set for Targets!");
                ProvideUIUpdate($"Run time: {ElapsedRunTime} (mm:ss)");
                return false;
            }
            catch (Exception e)
            {
                ProvideUIUpdate($"{e.Message}", true);
                _stackTraceError = e.StackTrace;
                return true;
            }
        }

        #region preliminary checks and pre-processing
        /// <summary>
        /// Preliminary checks prior to generating prelim targets
        /// </summary>
        /// <returns></returns>
        protected abstract bool PreliminaryChecks();

        /// <summary>
        /// Generate the body structure if it is not present in the structure set. Set the structure id to 'body'
        /// </summary>
        /// <returns></returns>
        protected bool GenerateBodyStructure()
        {
            UpdateUILabel("Generating Body structure:");
            Structure body = EclipseContext.GetInstance().StructureSet.CreateAndSearchBody(EclipseContext.GetInstance().StructureSet.GetDefaultSearchBodyParameters());
            if (!string.Equals(body.Id, "Body"))
            {
                try
                {
                    body.Id = "Body";
                }
                catch (Exception e)
                {
                    ProvideUIUpdate($"Error. Could not change {body.Id} to 'Body' because {e.Message}", true);
                    return true;
                }
            }
            ProvideUIUpdate($"Body structure generated");
            return false;
        }
        #endregion

        #region Target Creation
        /// <summary>
        /// Check if the requested preliminary targets already exist in the structure set.
        /// </summary>
        /// <returns></returns>
        private bool CheckForTargetStructures()
        {
            UpdateUILabel("Checking For Missing Target Structures: ");
            ProvideUIUpdate(0, "Checking for missing target structures!");
            int calcItems = _createPrelimTargetList.Count;
            int counter = 0;
            foreach (StructureOperationModel itr in _createPrelimTargetList)
            {
                if (!StructureTuningHelper.DoesStructureExistInSS(itr.OutputStructure,true))
                {
                    ProvideUIUpdate($"Target: {itr.OutputStructure} is missing or empty!");
                    _targetsToDerive.Add(itr);
                }
                else ProvideUIUpdate($"Target: {itr.OutputStructure} is exists and is contoured! Skipping derivation step");
                ProvideUIUpdate(100 * ++counter / calcItems);
            }
            ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");
            return false;
        }

        /// <summary>
        /// Contour the preliminary targets according to the standard practice rules for the targets of interest
        /// <returns></returns>
        protected abstract bool DeriveTargetStructures();

        protected virtual bool TargetPostProcessing()
        {
            return false;
        }
        #endregion
    }
}
