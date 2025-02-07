using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using TMLIAutoPlanner.Settings;
using AutoPlannerHelpers.BaseCore;
using AutoPlannerHelpers.Enums;
using System.Text;

namespace TMLIAutoPlanner.Core
{
    internal class GeneratePreliminaryTargets_TMLI : GeneratePreliminaryTargetsBase
    {
        private List<string> _requiredStructuresForTarget = new List<string>
        {
            "bones_trunk",
            "bones_face",
            "lymphnodes",
            "spinalcanal",
            "spleen",
            "bones_extrem",
            "ribs",
        };

        private List<RequestedTSManipulationModel> _manipulations;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tgts"></param>
        public GeneratePreliminaryTargets_TMLI(IEnumerable<RequestedTSStructureModel> tgts, List<RequestedTSManipulationModel> manipulations) :
            base(tgts, TMLIAutoPlannerSettings.CloseProgressWindowOnFinish)
        {
            _manipulations = manipulations;
        }

        #region preliminary checks and pre-processing
        /// <summary>
        /// Preliminary checks prior to generating prelim targets. Verify body, brain, and spinal cord structures exist and are contoured. Also
        /// convert brain, spinal cord structures to default resolution if they are high resolution
        /// </summary>
        /// <returns></returns>
        protected override bool PreliminaryChecks()
        {
            UpdateUILabel("Performing Preliminary Checks: ");
            int calcItems = 3;
            int counter = 0;

            //verify body structure is present and contour
            if (!StructureTuningHelper.DoesStructureExistInSS("body", EclipseContext.GetInstance().StructureSet, true))
            {
                ProvideUIUpdate("Missing body structure! Generating it now!");
                if (GenerateBodyStructure()) return true;
            }
            ProvideUIUpdate(100 * ++counter / calcItems);

            //verify brain and spine structures are present
            foreach (string itr in _requiredStructuresForTarget)
            {
                if (!StructureTuningHelper.DoesStructureExistInSS(itr, EclipseContext.GetInstance().StructureSet, true))
                {
                    ProvideUIUpdate($"Error! {itr} structure is either empty or null! Fix and try again!", true);
                    return true;
                }
            }
            ProvideUIUpdate(100 * ++counter / calcItems, "All structures necessary for target creation present and not empty");


            if (ContourHelper.CheckHighResolutionAndConvert(_requiredStructuresForTarget, EclipseContext.GetInstance().StructureSet, PUUD)) return true;
            ProvideUIUpdate(100 * ++counter / calcItems, "Check and converted any high res base targets");

            ProvideUIUpdate(100, "Preliminary checks complete!");
            ProvideUIUpdate($"Elapsed time: {GetElapsedTime()}");
            return false;
        }
        #endregion

        #region Target Creation
        /// <summary>
        /// Contour the preliminary targets according to the standard practice rules for ctv_brain, ptv_brain, ctv_spine, ptv_spine, and ptv_csi
        /// </summary>
        /// <returns></returns>
        protected override bool ContourTargetStructures()
        {
            int counter = 0;
            int calcItems = _addedTargetIds.Count + 2;
            foreach (string itr in _addedTargetIds.OrderBy(x => x.ElementAt(0)))
            {
                ProvideUIUpdate(100 * ++counter / calcItems, $"Contouring target: {itr}");
                Structure theTarget = StructureTuningHelper.GetStructureFromId(itr, EclipseContext.GetInstance().StructureSet);
                if (itr.ToLower().Contains("ptv_tmli"))
                {
                    GeneratePTVTMLI(theTarget);
                    ManipulatePTVTMLI(theTarget);
                }
                else if (itr.ToLower().Contains("ptv_1200"))
                {
                   GeneratePTV1200(theTarget);
                }
            }
            
            ProvideUIUpdate(100, "Targets added and contoured!");
            ProvideUIUpdate($"Elapsed time: {GetElapsedTime()}");
            return false;
        }

        private bool GeneratePTVTMLI(Structure ptv)
        {
            StructureSet ss = EclipseContext.GetInstance().StructureSet;
            ContourHelper.CopyStructureOntoStructure(StructureTuningHelper.GetStructureFromId("bones_trunk", ss), ptv);
            ContourHelper.CropStructureFromStructure(ptv, StructureTuningHelper.GetStructureFromId("bones_face", ss), 0.0);
            List<Structure> structures = new List<Structure>
            {
                StructureTuningHelper.GetStructureFromId("lymphnodes", ss),
                StructureTuningHelper.GetStructureFromId("spinalcanal", ss),
                StructureTuningHelper.GetStructureFromId("spleen", ss)
            };
            ContourHelper.ContourUnion(structures, ptv);
            ptv.SegmentVolume = ptv.Margin(5.0);

            ContourHelper.ContourUnion(StructureTuningHelper.GetStructureFromId("ribs", ss).Margin(5.0), ptv, 0.0);
            ContourHelper.ContourUnion(StructureTuningHelper.GetStructureFromId("bones_extrem", ss).Margin(10.0), ptv, 0.0);
            //ContourHelper.CropStructureFromStructure(ptv, StructureTuningHelper.GetStructureFromId("lungs", ss).Margin(5.0), 0.0);
            //ContourHelper.CropStructureFromStructure(ptv, StructureTuningHelper.GetStructureFromId("kidneys", ss).Margin(5.0), 0.0);
            //ContourHelper.CropStructureFromStructure(ptv, StructureTuningHelper.GetStructureFromId("esophagus", ss).Margin(5.0), 0.0);
            return false;
        }

        private bool ManipulatePTVTMLI(Structure target)
        {
            foreach(RequestedTSManipulationModel manipulationItem in _manipulations)
            {
                Structure theStructure = StructureTuningHelper.GetStructureFromId(manipulationItem.StructureId, EclipseContext.GetInstance().StructureSet);
                if(!ReferenceEquals(theStructure, null) && !theStructure.IsEmpty)
                {
                    if (manipulationItem.ManipulationType == TSManipulationType.CropTargetFromStructure)
                    {
                        ProvideUIUpdate($"Cropping target {target.Id} from {manipulationItem.StructureId} with margin {manipulationItem.MarginInCM} cm");
                        //crop target from structure
                        (bool failCrop, StringBuilder errorCropMessage) = ContourHelper.CropStructureFromStructure(target, theStructure, manipulationItem.MarginInCM);
                        if (failCrop)
                        {
                            ProvideUIUpdate(errorCropMessage.ToString());
                            return true;
                        }
                    }
                }
                else
                {
                    ProvideUIUpdate($"Normal structure {manipulationItem.StructureId} is null or empty! Skipping manipulation");
                }
            }
            return false;
        }

        private bool GeneratePTV1200(Structure ptv)
        {
            StructureSet ss = EclipseContext.GetInstance().StructureSet;
            ContourHelper.CopyStructureOntoStructure(StructureTuningHelper.GetStructureFromId("brain", ss), ptv);
            ContourHelper.ContourUnion(StructureTuningHelper.GetStructureFromId("liver", ss), ptv, 5.0);
            return false;
        }
        #endregion
    }
}
