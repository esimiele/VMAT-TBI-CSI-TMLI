using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using CSIAutoPlanner.Settings;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VMS.TPS.Common.Model.API;
using AutoPlannerHelpers.BaseCore;

namespace CSIAutoPlanner.Core
{
    internal class GeneratePreliminaryTargets_CSI : GeneratePreliminaryTargetsBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tgts"></param>
        public GeneratePreliminaryTargets_CSI(IEnumerable<StructureOperationModel> tgts) :
            base(tgts, CSIAutoPlannerSettings.CloseProgressWindowOnFinish)
        { }

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
            if (!StructureTuningHelper.DoesStructureExistInSS("body", true))
            {
                ProvideUIUpdate("Missing body structure! Generating it now!");
                if (GenerateBodyStructure()) return true;
            }
            ProvideUIUpdate(100 * ++counter / calcItems);

            if (ContourHelper.CheckHighResolutionAndConvert(_createPrelimTargetList.SelectMany(x => x.StructureIdList).Distinct().ToList(), PUUD)) return true;
            ProvideUIUpdate(100 * ++counter / calcItems, "Check and converted any high res base targets");

            ProvideUIUpdate(100, "Preliminary checks complete!");
            ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");
            return false;
        }
        #endregion

        #region Target Creation
        /// <summary>
        /// Contour the preliminary targets according to the standard practice rules for ctv_brain, ptv_brain, ctv_spine, ptv_spine, and ptv_csi
        /// </summary>
        /// <returns></returns>
        protected override bool DeriveTargetStructures()
        {
            //Structure tmp = null;
            //int counter = 0;
            //int calcItems = _addedTargetIds.Count + 2;
            //foreach (string itr in _addedTargetIds.OrderBy(x => x.ElementAt(0)))
            //{
            //    ProvideUIUpdate(100 * ++counter / calcItems, $"Contouring target: {itr}");
            //    Structure theTarget = StructureTuningHelper.GetStructureFromId(itr, EclipseContext.GetInstance().StructureSet);
            //    if (itr.ToLower().Contains("brain"))
            //    {
            //        tmp = StructureTuningHelper.GetStructureFromId("brain", EclipseContext.GetInstance().StructureSet);
            //        if (tmp != null && !tmp.IsEmpty)
            //        {
            //            if (itr.ToLower().Contains("ctv"))
            //            {
            //                //CTV structure. Brain CTV IS the brain structure
            //                theTarget.SegmentVolume = tmp.Margin(0.0);
            //            }
            //            else
            //            {
            //                //PTV structure
            //                //5 mm uniform margin to generate PTV
            //                theTarget.SegmentVolume = tmp.Margin(5.0);
            //            }
            //        }
            //        else
            //        {
            //            ProvideUIUpdate("Error! Could not retrieve brain structure! Exiting!", true);
            //            return true;
            //        }
            //    }
            //    else if (itr.ToLower().Contains("spine"))
            //    {
            //        tmp = StructureTuningHelper.GetStructureFromId("spinalcord", EclipseContext.GetInstance().StructureSet);
            //        if (tmp == null) tmp = StructureTuningHelper.GetStructureFromId("spinal_cord", EclipseContext.GetInstance().StructureSet);
            //        if (tmp != null && !tmp.IsEmpty)
            //        {
            //            if (itr.ToLower().Contains("ctv"))
            //            {
            //                //CTV structure. Brain CTV IS the brain structure
            //                //AxisAlignedMargins(inner or outer margin, margin from negative x, margin for negative y, margin for negative z, margin for positive x, margin for positive y, margin for positive z)
            //                //according to Nataliya: CTV_spine = spinal_cord+0.5cm ANT, +1.5cm Inf, and +1.0 cm in all other directions
            //                theTarget.SegmentVolume = tmp.AsymmetricMargin(new AxisAlignedMargins(StructureMarginGeometry.Outer,
            //                                                                                10.0,
            //                                                                                5.0,
            //                                                                                15.0,
            //                                                                                10.0,
            //                                                                                10.0,
            //                                                                                10.0));
            //            }
            //            else
            //            {
            //                //PTV structure
            //                //5 mm uniform margin to generate PTV
            //                tmp = StructureTuningHelper.GetStructureFromId("CTV_Spine", EclipseContext.GetInstance().StructureSet);
            //                if (tmp != null && !tmp.IsEmpty) theTarget.SegmentVolume = tmp.Margin(5.0);
            //                else { ProvideUIUpdate("Error! Could not retrieve CTV_Spine structure! Exiting!", true); return true; }
            //            }
            //        }
            //        else
            //        {
            //            ProvideUIUpdate("Error! Could not retrieve spinal cord structure! Exiting!", true);
            //            return true;
            //        }
            //    }
            //}

            //if (_addedTargetIds.Any(x => string.Equals(x.ToLower(), "ptv_csi")))
            //{
            //    if (ContourPTVCSI()) return true;
            //    ProvideUIUpdate(100 * ++counter / calcItems, "PTV_CSI generated and contoured!");
            //}
            //else if (_createPrelimTargetList.Any(x => string.Equals(x.StructureId.ToLower(), "ptv_csi")))
            //{
            //    ProvideUIUpdate(100 * ++counter / calcItems, "PTV_CSI already exists in the structure set! Skipping!");
            //}

            int counter = 0;
            int calcItems = _targetsToDerive.Count + 2;
            foreach (StructureOperationModel itr in _targetsToDerive)
            {
                if (itr.IsValidOperation)
                {
                    ProvideUIUpdate(100 * ++counter / calcItems, $"Contouring target: {itr}");
                    if (ContourHelper.PerformStructureOperation(itr, UIUD)) return true;
                }
                else ProvideUIUpdate($"Warning! {itr.FriendlyName} is not a valid operation! Skipping!");
            }
            ProvideUIUpdate(100, "Targets added and contoured!");
            ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");
            return false;
        }

        /// <summary>
        /// Helper method to contour PTV_CSI by combining ptv_brain and ptv_spine, then cropping the resulting structure 3 mm from body
        /// </summary>
        /// <returns></returns>
        //private bool ContourPTVCSI()
        //{
        //    int counter = 0;
        //    int calcItems = 4;
        //    ProvideUIUpdate("Generating: PTV_CSI");
        //    ProvideUIUpdate(100 * ++counter / calcItems, "Retrieving: PTV_CSI, PTV_Brain, and PTV_Spine");
        //    //used to create the ptv_csi structures
        //    Structure combinedTarget = StructureTuningHelper.GetStructureFromId("PTV_CSI");
        //    Structure brainTarget = StructureTuningHelper.GetStructureFromId("PTV_Brain");
        //    Structure spineTarget = StructureTuningHelper.GetStructureFromId("PTV_Spine");
        //    ProvideUIUpdate(100 * ++counter / calcItems, "Unioning PTV_Brain and PTV_Spine to make PTV_CSI");
        //    combinedTarget.SegmentVolume = brainTarget.Margin(0.0);
        //    combinedTarget.SegmentVolume = combinedTarget.Or(spineTarget.Margin(0.0));

        //    ProvideUIUpdate(100 * ++counter / calcItems, "Cropping PTV_CSI from body with 3 mm inner margin");
        //    //1/3/2022, crop PTV structure from body by 3mm
        //    (bool fail, StringBuilder errorMessage) = ContourHelper.CropStructureFromBody(combinedTarget, -0.3, EclipseContext.GetInstance().StructureSet.Structures.First(x => x.Id.ToLower().Contains("body")).Id);
        //    if (fail)
        //    {
        //        ProvideUIUpdate(errorMessage.ToString(), true);
        //        return true;
        //    }
        //    ProvideUIUpdate(100 * ++counter / calcItems, "PTV_CSI cropped from body with 3 mm inner margin");
        //    ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");
        //    return false;
        //}
        #endregion
    }
}
