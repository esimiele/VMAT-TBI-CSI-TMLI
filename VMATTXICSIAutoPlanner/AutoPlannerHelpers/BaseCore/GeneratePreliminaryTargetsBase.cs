using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Delegates;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using SimpleProgressWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace AutoPlannerHelpers.BaseCore
{
    public abstract class GeneratePreliminaryTargetsBase : SimpleMTbase
    {
        // Get methods
        public List<string> GetAddedTargetStructures() { return _addedTargetIds; }
        public string GetErrorStackTrace() { return _stackTraceError; }

        //DICOM types
        //Possible values are "AVOIDANCE", "CAVITY", "CONTRAST_AGENT", "CTV", "EXTERNAL", "GTV", "IRRAD_VOLUME", 
        //"ORGAN", "PTV", "TREATED_VOLUME", "SUPPORT", "FIXATION", "CONTROL", and "DOSE_REGION". 
        //Dicom type, structure Id
        protected List<RequestedTSStructureModel> _createPrelimTargetList;
        //Dicom type, structure Id
        protected List<RequestedTSStructureModel> _missingTargets = new List<RequestedTSStructureModel> { };
        protected List<string> _addedTargetIds = new List<string> { };
        protected string _stackTraceError;
        protected ProvideUIUpdateDelegate PUUD;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tgts"></param>
        protected GeneratePreliminaryTargetsBase(List<RequestedTSStructureModel> tgts, bool closePWOnFinish)
        {
            _createPrelimTargetList = new List<RequestedTSStructureModel>(tgts);
            SetCloseOnFinish(closePWOnFinish, 3000);
        }

        /// <summary>
        /// Run control
        /// </summary>
        /// <returns></returns>
        public override bool Run()
        {
            try
            {
                PUUD = ProvideUIUpdate;
                if (PreliminaryChecks()) return true;
                if (CheckForTargetStructures()) return true;
                if (_missingTargets.Any())
                {
                    ProvideUIUpdate("Preliminary Targets missing from the structure set! Creating them now!");
                    if (CreateMissingTargetStructures()) return true;
                }
                if (_addedTargetIds.Any())
                {
                    ProvideUIUpdate("Contouring targets now");
                    if (ContourTargetStructures()) return true;
                }

                UpdateUILabel("Finished!");
                ProvideUIUpdate(100, "Finished Preparing Structure Set for Targets!");
                ProvideUIUpdate($"Run time: {GetElapsedTime()} (mm:ss)");
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
            foreach (RequestedTSStructureModel itr in _createPrelimTargetList)
            {
                Structure tmp = StructureTuningHelper.GetStructureFromId(itr.StructureId, EclipseContext.GetInstance().StructureSet);
                if (tmp == null)
                {
                    ProvideUIUpdate($"Target: {itr.StructureId} is missing");
                    _missingTargets.Add(itr);
                }
                else if (tmp.IsEmpty)
                {
                    ProvideUIUpdate($"Target: {itr.StructureId} exists, but is empty");
                    _addedTargetIds.Add(tmp.Id);
                }
                else ProvideUIUpdate($"Target: {itr.StructureId} is exists and is contoured");
                ProvideUIUpdate(100 * ++counter / calcItems);
            }
            ProvideUIUpdate($"Elapsed time: {GetElapsedTime()}");
            return false;
        }

        /// <summary>
        /// Create the identified missing preliminary targets
        /// </summary>
        /// <returns></returns>
        private bool CreateMissingTargetStructures()
        {
            UpdateUILabel("Create Missing Target Structures: ");
            ProvideUIUpdate(0, "Creating missing target structures!");
            //create the CTV and PTV structures
            //int calcItems = prospectiveTargets.Count;
            int calcItems = _missingTargets.Count;
            int counter = 0;
            foreach (RequestedTSStructureModel itr in _missingTargets)
            {
                if (EclipseContext.GetInstance().StructureSet.CanAddStructure(itr.DICOMType, itr.StructureId))
                {
                    _addedTargetIds.Add(itr.StructureId);
                    EclipseContext.GetInstance().StructureSet.AddStructure(itr.DICOMType, itr.StructureId);
                    ProvideUIUpdate(100 * ++counter / calcItems, $"Added target: {itr.StructureId}");
                }
                else
                {
                    ProvideUIUpdate($"Can't add {itr.StructureId} to the structure set!", true);
                    return true;
                }
            }
            ProvideUIUpdate($"Elapsed time: {GetElapsedTime()}");
            return false;
        }

        /// <summary>
        /// Contour the preliminary targets according to the standard practice rules for the targets of interest
        /// <returns></returns>
        protected abstract bool ContourTargetStructures();
        #endregion
    }
}
