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

        #region Target Creation
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
        #endregion
    }
}
