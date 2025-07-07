using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;
using TMLIAutoPlanner.Settings;
using AutoPlannerHelpers.BaseCore;
using VMS.TPS.Common.Model.Types;

namespace TMLIAutoPlanner.Core
{
    internal class GeneratePreliminaryTargets_TMLI : GeneratePreliminaryTargetsBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tgts"></param>
        public GeneratePreliminaryTargets_TMLI(IEnumerable<StructureOperationModel> tgts) :
            base(tgts, TMLIAutoPlannerSettings.CloseProgressWindowOnFinish)
        {
        }

        #region preliminary checks and pre-processing
        /// <summary>
        /// Preliminary checks prior to generating prelim targets. Verify body, brain, and spinal cord structures exist and are contoured. Also
        /// convert brain, spinal cord structures to default resolution if they are high resolution
        /// </summary>
        /// <returns></returns>
        protected override bool PreliminaryChecks()
        {
            UpdateUILabel("Performing Preliminary Checks:");
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
            if (StructureTuningHelper.UnionLRStructures(PUUD)) return true;
            UpdateUILabel("Contouring targets now:");
            int counter = 0;
            int calcItems = _targetsToDerive.Count + 2;
            foreach(StructureOperationModel itr in _targetsToDerive)
            {
                if (itr.IsValidOperation)
                {
                    ProvideUIUpdate(100 * ++counter / calcItems, $"Contouring target: {itr}");
                    if(ContourHelper.PerformStructureOperation(itr, UIUD)) return true;
                }
                else ProvideUIUpdate($"Warning! {itr.FriendlyName} is not a valid operation! Skipping!");
            }
            //foreach (string itr in _addedTargetIds.OrderBy(x => x.ElementAt(0)))
            //{
            //    Structure theTarget = StructureTuningHelper.GetStructureFromId(itr, EclipseContext.GetInstance().StructureSet);
            //    if(string.Equals(itr, "ptv_tmli_12", StringComparison.OrdinalIgnoreCase))
            //    {
            //        GeneratePTV1200(theTarget);

            //    }
            //    else if (string.Equals(itr,  "ptv_tmli_20",StringComparison.OrdinalIgnoreCase) || string.Equals(itr, "ptv_tmli", StringComparison.OrdinalIgnoreCase))
            //    {
            //        GeneratePTVTMLI(theTarget);
            //        ManipulatePTVTMLI(theTarget);
            //    }
            //}
            
            ProvideUIUpdate(100, "Targets added and contoured!");
            ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");
            return false;
        }

        //private bool GeneratePTVTMLI(Structure ptv)
        //{
        //    ContourHelper.CopyStructureOntoStructure(StructureTuningHelper.GetStructureFromId("bones_trunk"), ptv);
        //    ProvideUIUpdate($"Unioned bones_trunk with PTV_TMLI");
        //    ContourHelper.CropStructureFromStructure(ptv, StructureTuningHelper.GetStructureFromId("bones_face"), 0.0);
        //    ProvideUIUpdate($"Cropped bones_face from PTV_TMLI");

        //    List<Structure> structures = new List<Structure>
        //    {
        //        StructureTuningHelper.GetStructureFromId("lymphnodes"),
        //        StructureTuningHelper.GetStructureFromId("spinalcanal"),
        //        StructureTuningHelper.GetStructureFromId("spleen"),
        //    };
        //    //need to know target dosing
        //    if (StructureTuningHelper.DoesStructureExistInSS("testes", true)) structures.Add(StructureTuningHelper.GetStructureFromId("testes"));

        //    ContourHelper.ContourUnion(structures, ptv, 0.0);
        //    foreach (string itr in structures.Select(x => x.Id)) ProvideUIUpdate($"Unioned {itr} with PTV_TMLI");
        //    ptv.SegmentVolume = ptv.Margin(5.0);
        //    ProvideUIUpdate("Expanded PTV_TMLI with uniform 5mm margin");

        //    //ContourHelper.ContourUnion(StructureTuningHelper.GetStructureFromId("bones_extrem", ss).Margin(10.0), ptv, 0.0);
        //    ProvideUIUpdate($"Unioned bones_extrem with PTV_TMLI with 10 mm outer margin");
        //    //PostProcessPTVTMLI(ptv);
        //    return false;
        //}

        protected override bool TargetPostProcessing()
        {
            Structure expandedBrain = StructureTuningHelper.GetStructureFromId("brain+1.0cm", true);
            expandedBrain.SegmentVolume = StructureTuningHelper.GetStructureFromId("Brain").Margin(10.0);
            int supOralCavitySlice = CalculationHelper.ComputeSlice(StructureTuningHelper.GetStructureFromId("oralcavity").MeshGeometry.Positions.Max(p => p.Z),
                                                                    EclipseContext.GetInstance().StructureSet.Image.Origin.z,
                                                                    EclipseContext.GetInstance().StructureSet.Image.ZRes);
            int supSlice = CalculationHelper.ComputeSlice(StructureTuningHelper.GetStructureFromId("eyes").MeshGeometry.Positions.Max(p => p.Z) + 15.0,
                                                                    EclipseContext.GetInstance().StructureSet.Image.Origin.z,
                                                                    EclipseContext.GetInstance().StructureSet.Image.ZRes);

            double zPos = StructureTuningHelper.GetStructureFromId("eyes").MeshGeometry.Positions.OrderByDescending(x => x.Z).First().Z + 15.0 - EclipseContext.GetInstance().StructureSet.Image.UserOrigin.z;
            ProvideUIUpdate($"{zPos}");

            foreach (Structure target in StructureTuningHelper.GetStructuresFromIdList(new List<string> { "PTV_TMLI", "PTV_TMLI_12", "PTV_TMLI_20" }, true))
            {
                Structure tmp = EclipseContext.GetInstance().StructureSet.AddStructure("CONTROL", "_tmp");
                int percentComplete = 0;
                int calcItems = supSlice - supOralCavitySlice + 1;
                for (int i = supOralCavitySlice; i <= supSlice; i++)
                {
                    VVector[][] ptvPoints = target.GetContoursOnImagePlane(i);
                    target.ClearAllContoursOnImagePlane(i);
                    for (int j = 0; j < ptvPoints.Count(); j++)
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
                ContourHelper.ContourOverlapAndUnion(expandedBrain, tmp, target, 0.0);
                EclipseContext.GetInstance().StructureSet.RemoveStructure(tmp);
            }
            
            return false;
        }

        //private bool GeneratePTV1200(Structure ptv)
        //{
        //    StructureSet ss = EclipseContext.GetInstance().StructureSet;
        //    ContourHelper.CopyStructureOntoStructure(StructureTuningHelper.GetStructureFromId("brain"), ptv, 0.5);
        //    //ContourHelper.ContourUnion(StructureTuningHelper.GetStructureFromId("liver", ss), ptv, 0.5);
        //    //ContourHelper.ContourUnion(StructureTuningHelper.GetStructureFromId("Rib", ss), ptv, 0.7);
        //    ContourHelper.CropStructureFromStructure(ptv, StructureTuningHelper.GetStructureFromId("Lungs"), 0.5);
        //    ContourHelper.CropStructureFromStructure(ptv, StructureTuningHelper.GetStructureFromId("Heart"), 0.5);
        //    ContourHelper.CropStructureFromStructure(ptv, StructureTuningHelper.GetStructureFromId("Kidneys"), 0.5);
        //    ContourHelper.CropStructureFromBody(ptv, -0.3);
        //    return false;
        //}
        #endregion
    }
}
