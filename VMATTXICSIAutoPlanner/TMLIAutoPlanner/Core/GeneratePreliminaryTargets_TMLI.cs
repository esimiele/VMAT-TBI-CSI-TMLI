using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;
using TMLIAutoPlanner.Settings;
using AutoPlannerHelpers.BaseCore;
using AutoPlannerHelpers.Enums;
using System.Text;
using VMS.TPS.Common.Model.Types;
using System;

namespace TMLIAutoPlanner.Core
{
    internal class GeneratePreliminaryTargets_TMLI : GeneratePreliminaryTargetsBase
    {
        private List<string> _requiredStructuresForTarget = new List<string>
        {
            "body",
            "bones_trunk",
            "bones_face",
            "lymphnodes",
            "spinalcanal",
            "spleen",
            "bones_extrem",
            "brain",
            "OralCavity",
            //"ribs",
        };

        private List<RequestedTSManipulationModel> _manipulations;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tgts"></param>
        public GeneratePreliminaryTargets_TMLI(IEnumerable<RequestedTSStructureModel> tgts, 
                                               List<RequestedTSManipulationModel> manipulations, 
                                               bool includeTestesInPTV) :
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
            ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");
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
            if (UnionLRStructures()) return true;
            UpdateUILabel("Contouring targets now:");
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
            ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");
            return false;
        }

        private bool UnionLRStructures()
        {
            UpdateUILabel("Unioning Structures: ");
            ProvideUIUpdate(0, "Checking for L and R structures to union!");
            List<UnionStructureModel> structuresToUnion = StructureTuningHelper.CheckStructuresToUnion(EclipseContext.GetInstance().StructureSet);
            if (structuresToUnion.Any())
            {
                int calcItems = structuresToUnion.Count;
                int numUnioned = 0;
                foreach (UnionStructureModel itr in structuresToUnion)
                {
                    (bool fail, StringBuilder output) = StructureTuningHelper.UnionLRStructures(itr, EclipseContext.GetInstance().StructureSet);
                    if (!fail) ProvideUIUpdate(100 * ++numUnioned / calcItems, $"Unioned {itr.ProposedUnionStructureId}");
                    else
                    {
                        ProvideUIUpdate(output.ToString(), true);
                        return true;
                    }
                }
                ProvideUIUpdate(100, "Structures unioned successfully!");
            }
            else ProvideUIUpdate(100, "No structures to union!");
            ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");
            return false;
        }

        private bool GeneratePTVTMLI(Structure ptv)
        {
            StructureSet ss = EclipseContext.GetInstance().StructureSet;
            ContourHelper.CopyStructureOntoStructure(StructureTuningHelper.GetStructureFromId("bones_trunk", ss), ptv);
            ProvideUIUpdate($"Unioned bones_trunk with PTV_TMLI");
            ContourHelper.CropStructureFromStructure(ptv, StructureTuningHelper.GetStructureFromId("bones_face", ss), 0.0);
            ProvideUIUpdate($"Cropped bones_face from PTV_TMLI");

            List<Structure> structures = new List<Structure>
            {
                StructureTuningHelper.GetStructureFromId("lymphnodes", ss),
                StructureTuningHelper.GetStructureFromId("spinalcanal", ss),
                StructureTuningHelper.GetStructureFromId("spleen", ss),
            };
            //need to know target dosing
            if (StructureTuningHelper.DoesStructureExistInSS("testes", EclipseContext.GetInstance().StructureSet, true)) structures.Add(StructureTuningHelper.GetStructureFromId("testes", ss));

            ContourHelper.ContourUnion(structures, ptv);
            foreach (string itr in structures.Select(x => x.Id)) ProvideUIUpdate($"Unioned {itr} with PTV_TMLI");
            ptv.SegmentVolume = ptv.Margin(5.0);
            ProvideUIUpdate("Expanded PTV_TMLI with uniform 5mm margin");

            //ContourHelper.ContourUnion(StructureTuningHelper.GetStructureFromId("rib", ss).Margin(5.0), ptv, 0.0);
            ContourHelper.ContourUnion(StructureTuningHelper.GetStructureFromId("bones_extrem", ss).Margin(10.0), ptv, 0.0);
            ProvideUIUpdate($"Unioned bones_extrem with PTV_TMLI with 10 mm outer margin");
            PostProcessPTVTMLI(ptv);
            return false;
        }

        private bool PostProcessPTVTMLI(Structure target)
        {
            Structure expandedBrain = EclipseContext.GetInstance().StructureSet.AddStructure("CONTROL", "Brain+1.0cm");
            expandedBrain.SegmentVolume = StructureTuningHelper.GetStructureFromId("Brain", EclipseContext.GetInstance().StructureSet).Margin(10.0);
            int supOralCavitySlice = CalculationHelper.ComputeSlice(StructureTuningHelper.GetStructureFromId("oralcavity", EclipseContext.GetInstance().StructureSet).MeshGeometry.Positions.Max(p => p.Z),
                                                                    EclipseContext.GetInstance().StructureSet.Image.Origin.z,
                                                                    EclipseContext.GetInstance().StructureSet.Image.ZRes);
            int supSlice = CalculationHelper.ComputeSlice(StructureTuningHelper.GetStructureFromId("eyes", EclipseContext.GetInstance().StructureSet).MeshGeometry.Positions.Max(p => p.Z) + 15.0,
                                                                    EclipseContext.GetInstance().StructureSet.Image.Origin.z,
                                                                    EclipseContext.GetInstance().StructureSet.Image.ZRes);

            double zPos = StructureTuningHelper.GetStructureFromId("eyes", EclipseContext.GetInstance().StructureSet).MeshGeometry.Positions.OrderByDescending(x => x.Z).First().Z + 15.0 - EclipseContext.GetInstance().StructureSet.Image.UserOrigin.z;
            ProvideUIUpdate($"{zPos}");

            //Structure tmpTarget = EclipseContext.GetInstance().StructureSet.AddStructure("CONTROL", "_tmpTarget");
            //ContourHelper.CopyStructureOntoStructure(target, tmpTarget);

            Structure tmp = EclipseContext.GetInstance().StructureSet.AddStructure("CONTROL", "_tmp");

            int percentComplete = 0;
            int calcItems = supSlice - supOralCavitySlice + 1;
            for (int i = supOralCavitySlice; i <= supSlice; i++)
            {
                VVector[][] ptvPoints = target.GetContoursOnImagePlane(i);
                target.ClearAllContoursOnImagePlane(i);
                for(int j = 0; j < ptvPoints.Count(); j ++)
                {
                    List<VVector> ptvContourPoints = ptvPoints[j].ToList();
                    if (ptvContourPoints.Any(x => tmp.IsPointInsideSegment(x)))
                    {
                        //points inside ptv contour --> subtract this segment
                        tmp.SubtractContourOnImagePlane(ptvPoints[j], i);
                        ProvideUIUpdate($"Points inside ptv. Subtracting contours from image slice: {i}");
                    }
                    else
                    {
                        tmp.AddContourOnImagePlane(ptvPoints[j], i);
                        ProvideUIUpdate($"Adding contours on image slice: {i}");
                    }
                }
                ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Image slice: {i}");
            }

            ContourHelper.ContourOverlapAndUnion(expandedBrain, tmp, target, EclipseContext.GetInstance().StructureSet, 0.0);
            EclipseContext.GetInstance().StructureSet.RemoveStructure(tmp);
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
            ContourHelper.CropStructureFromBody(target, EclipseContext.GetInstance().StructureSet, -0.3);
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
