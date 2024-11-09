using AutoPlannerHelpers.BaseCore;
using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.Prompts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Windows.Media.Media3D;
using TMLIAutoPlanner.Settings;
using VMS.TPS.Common.Model.API;

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
        private List<string> _requiredStructuresForTarget = new List<string>
        {
            "bones_body",
            "mandible",
            "lymphnodes",
            "spinalcanal",
            "spleen",
            "bones_extern",
            "lungs",
            "kidneys",
            "esophagus"
        };
        #endregion

        internal TSGenerationManipulation_TMLI(List<RequestedTSStructureModel> ts, 
                                               List<RequestedTSManipulationModel> manipulations, 
                                               List<TSRingStructureModel> rings,
                                               List<PrescriptionModel> presc)
        {
            TS_structures = new List<RequestedTSStructureModel>(ts);
            _requestedRings = new List<TSRingStructureModel>(rings);
            prescriptions = new List<PrescriptionModel>(presc);
            SetCloseOnFinish(TMLIAutoPlannerSettings.CloseProgressWindowOnFinish, 3000);
        }

        [HandleProcessCorruptedStateExceptions]
        public override bool Run()
        {
            try
            {
                PlanIsocentersList.Clear();
                if (PreliminaryChecks()) return true;
                if (UnionLRStructures()) return true;
                if (TSManipulationList.Any()) if (CheckHighResolution()) return true;
                if (CreateTSStructures()) return true;
                if (PerformTSStructureManipulation()) return true;
                if (CalculateNumIsos()) return true; 
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

        #region preliminary checks
        protected override bool PreliminaryChecks()
        {
            UpdateUILabel("Performing Preliminary Checks: ");
            int calcItems = 4;
            int counter = 0;
            //check body structure exists and is contoured
            if (!StructureTuningHelper.DoesStructureExistInSS("body", EclipseContext.GetInstance().StructureSet, true))
            {
                ProvideUIUpdate("Error! Body structure is either empty or null! Fix and try again!", true);
                return true;
            }
            ProvideUIUpdate(100 * ++counter / calcItems, "Body structure exists and is not empty");

            //check if user origin was set
            if (IsUOriginInside()) return true;
            ProvideUIUpdate(100 * ++counter / calcItems, "User origin is inside body");

            if (CheckBodyExtentAndMatchline()) return true;
            ProvideUIUpdate(100 * ++counter / calcItems, "Body structure exists and matchline appropriate");

            foreach (string itr in _requiredStructuresForTarget)
            {
                if (!StructureTuningHelper.DoesStructureExistInSS(itr, EclipseContext.GetInstance().StructureSet, true))
                {
                    ProvideUIUpdate($"Error! {itr} structure is either empty or null! Fix and try again!", true);
                    return true;
                }
            }
            ProvideUIUpdate(100 * ++counter / calcItems, "All structures necessary for target creation present and not empty");
            ProvideUIUpdate($"Elapsed time: {GetElapsedTime()}");
            return false;
        }

        /// <summary>
        /// Check the body height against the limits for treating in the HFS position. If body is taller than limit (116 cm), verify that the matchline
        /// structure is present and contoured
        /// </summary>
        /// <returns></returns>
        private bool CheckBodyExtentAndMatchline()
        {
            //get the points collection for the Body (used for calculating number of isocenters)
            Structure body = StructureTuningHelper.GetStructureFromId("Body", EclipseContext.GetInstance().StructureSet);
            Point3DCollection pts = body.MeshGeometry.Positions;

            //check if patient length is > 116cm, if so, check for matchline contour
            if ((pts.Max(p => p.Z) - pts.Min(p => p.Z)) > 1160.0 && !StructureTuningHelper.DoesStructureExistInSS("matchline", EclipseContext.GetInstance().StructureSet, true))
            {
                ProvideUIUpdate($"Body extent ({pts.Max(p => p.Z) - pts.Min(p => p.Z)} mm) is greater than 116.0 cm and no matchline structure was found!");
                //check to see if the user wants to proceed even though there is no matchplane contour or the matchplane contour exists, but is not filled
                ConfirmPrompt CP = new ConfirmPrompt("No matchplane contour found even though patient length > 116.0 cm!" + Environment.NewLine + Environment.NewLine + "Continue?!");
                CP.ShowDialog();
                if (!CP.GetSelection())
                {
                    ProvideUIUpdate("", true);
                    return true;
                }
            }
            return false;
        }
        #endregion

        protected override bool CreateTSStructures()
        {
            UpdateUILabel("Create TS Structures:");
            ProvideUIUpdate("Adding remaining tuning structures to stack!");
            if (RemoveOldTSStructures(TS_structures, true)) return true;

            int counter = 0;
            int calcItems = TS_structures.Count;

            foreach (RequestedTSStructureModel itr in TS_structures)
            {
                ProvideUIUpdate(100 * ++counter / calcItems, $"Adding {itr.StructureId} to the structure set!");
                AddTSStructures(itr);
            }

            ProvideUIUpdate(100, "Finished adding tuning structures!");
            ProvideUIUpdate(0, "Contouring tuning structures!");

            counter = 0;
            calcItems = AddedStructureIds.Count;
            //now contour the various structures
            foreach (string itr in AddedStructureIds)
            {
                ProvideUIUpdate($"Contouring TS: {itr}");
                Structure addedStructure = StructureTuningHelper.GetStructureFromId(itr, EclipseContext.GetInstance().StructureSet);
                ProvideUIUpdate($"Retrieved structure: {addedStructure.Id}");
                if (itr.ToLower().Contains("ptv_tmli"))
                {
                    if (GeneratePTVTMLI(addedStructure)) return true;
                }
                else if (itr.ToLower().Contains("ptv_1200"))
                {
                    if (GeneratePTV1200(addedStructure)) return true;
                }
                ProvideUIUpdate(100 * ++counter / calcItems);
            }

            ProvideUIUpdate($"Elapsed time: {GetElapsedTime()}");
            return false;
        }

        private bool GeneratePTVTMLI(Structure addedStructure)
        {
            StructureSet ss = EclipseContext.GetInstance().StructureSet;
            ContourHelper.CopyStructureOntoStructure(StructureTuningHelper.GetStructureFromId("bones_body", ss), addedStructure);
            ContourHelper.CropStructureFromStructure(addedStructure, StructureTuningHelper.GetStructureFromId("mandible", ss), 0.0);
            List<Structure> structures = new List<Structure>
            {
                StructureTuningHelper.GetStructureFromId("lymphnodes", ss),
                StructureTuningHelper.GetStructureFromId("spinalcanal", ss),
                StructureTuningHelper.GetStructureFromId("spleen", ss)
            };
            ContourHelper.ContourUnion(structures, addedStructure);
            addedStructure.SegmentVolume = addedStructure.Margin(5.0);

            ContourHelper.ContourUnion(StructureTuningHelper.GetStructureFromId("bones_extrem", ss).Margin(10.0), addedStructure, 0.0);
            ContourHelper.CropStructureFromStructure(addedStructure, StructureTuningHelper.GetStructureFromId("lungs", ss).Margin(5.0), 0.0);
            ContourHelper.CropStructureFromStructure(addedStructure, StructureTuningHelper.GetStructureFromId("kidneys", ss).Margin(5.0), 0.0);
            ContourHelper.CropStructureFromStructure(addedStructure, StructureTuningHelper.GetStructureFromId("esophagus", ss).Margin(5.0), 0.0);
            return false;
        }

        private bool GeneratePTV1200(Structure addedStructure)
        {
            StructureSet ss = EclipseContext.GetInstance().StructureSet;
            ContourHelper.CopyStructureOntoStructure(StructureTuningHelper.GetStructureFromId("brain", ss),addedStructure);
            ContourHelper.ContourUnion(StructureTuningHelper.GetStructureFromId("liver", ss), addedStructure, 5.0);
            return false;
        }

        protected override bool PerformTSStructureManipulation()
        {
            UpdateUILabel("Perform TS Manipulations: ");
            int counter = 0;
            int calcItems = TSManipulationList.Count * prescriptions.Count;

            List<TargetModel> tmpTSTargetList = new List<TargetModel> { };
            //prescriptions are inherently sorted by increasing cumulative Rx to targets
            foreach (PrescriptionModel itr in prescriptions)
            {
                Structure target = null;
                //special logic. We want to actually manipulate ptv_body itself rather than a TS_PTV_Body structure
                if (string.Equals(itr.TargetId.ToLower(), "ptv_tmli"))
                {
                    target = StructureTuningHelper.GetStructureFromId(itr.TargetId, EclipseContext.GetInstance().StructureSet);
                }
                else
                {
                    //target Id is not ptv_body, generate a new TSTarget
                    target = GetTSTarget(itr.TargetId);
                    tmpTSTargetList.Add(new TargetModel(itr.TargetId, itr.CumulativeDoseToTarget, target.Id));
                }
                if (target == null || target.IsEmpty)
                {
                    ProvideUIUpdate($"Error! Target structure: {itr.TargetId} is null or empty! Cannot perform tuning structure manipulations! Exiting!", true);
                    return true;
                }
                if (TSManipulationList.Any())
                {
                    //perform all relevant TS manipulations for the specified target
                    foreach (RequestedTSManipulationModel itr1 in TSManipulationList)
                    {
                        if (ManipulateTuningStructures(itr1, target)) return true;
                        ProvideUIUpdate(100 * ++counter / calcItems);
                    }
                }
                else ProvideUIUpdate("No TS manipulations requested!");
                if (string.Equals(itr.TargetId.ToLower(), "ptv_tmli"))
                {
                    //ts_ptv_vmat needs to be handled AFTER ts manipulation because ptv_body itself needs to be cropped from all the relevant structures
                    (bool fail, string tsPTVVMATId) = GenerateTSPTVTarget(target, "TS_PTV_TMLI");
                    if (fail) return true;
                    tmpTSTargetList.Add(new TargetModel(itr.TargetId, itr.CumulativeDoseToTarget, tsPTVVMATId));
                }
            }
            //only one plan is allowed for the prescriptions --> last item is the highest Rx target for this plan and needs to be set as the normalization volume
            NormalizationVolumes.Add(prescriptions.Last().PlanId, tmpTSTargetList.OrderByDescending(x => x.TargetRxDose).First().TsTargetId);
            PlanTargets.Add(new PlanTargetsModel(prescriptions.Last().PlanId, new List<TargetModel>(tmpTSTargetList)));

            ProvideUIUpdate($"Elapsed time: {GetElapsedTime()}");
            return false;
        }

        private (bool fail, string tsPTVVMATId) GenerateTSPTVTarget(Structure baseTarget, string requestedTsTargetId)
        {
            UpdateUILabel($"Create {requestedTsTargetId}:");
            int percentComplete = 0;
            int calcItems = 2;
            Structure addedTSTarget = GetTSTarget(baseTarget.Id, requestedTsTargetId);
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Contoured TS target: {addedTSTarget.Id}");

            if (StructureTuningHelper.DoesStructureExistInSS("matchline", EclipseContext.GetInstance().StructureSet, true))
            {
                ProvideUIUpdate($"Cutting {addedTSTarget} at the matchline!");

                //find the image plane where the matchline is location. Record this value and break the loop. Also find the first slice where the ptv_body contour starts and record this value
                Structure matchline = StructureTuningHelper.GetStructureFromId("matchline", EclipseContext.GetInstance().StructureSet);
                ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Retrieved matchline structure: {matchline.Id}");

                if (ContourTSLegs("TS_PTV_Legs", matchline, addedTSTarget)) return (true, addedTSTarget.Id);
                if (CutTSPTVAtMatchline(addedTSTarget, matchline)) return (true, addedTSTarget.Id);
            }
            return (false, addedTSTarget.Id);
        }

        private bool ContourTSLegs(string TSLegsId, Structure matchline, Structure target)
        {
            UpdateUILabel($"Contour {TSLegsId}:");
            int percentComplete = 0;
            int calcItems = 3;

            //do the structure manipulation
            (bool failTSLegs, Structure TS_legs) = RemoveAndGenerateStructure(TSLegsId);
            if (failTSLegs) return true;
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Created structure: {TS_legs.Id}");

            (bool failCopyTarget, StringBuilder copyErrorMessage) = ContourHelper.CopyStructureOntoStructure(target, TS_legs);
            if (failCopyTarget)
            {
                ProvideUIUpdate(copyErrorMessage.ToString(), true);
                return true;
            }
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Copied structure {target.Id} onto {TS_legs.Id}");

            int matchlineSliceLoc = CalculationHelper.ComputeSlice(matchline.CenterPoint.z, EclipseContext.GetInstance().StructureSet.Image.Origin.z, EclipseContext.GetInstance().StructureSet.Image.ZRes);

            for (int i = matchlineSliceLoc; i < EclipseContext.GetInstance().StructureSet.Image.ZSize; i++)
            {
                TS_legs.ClearAllContoursOnImagePlane(i);
            }
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Contoured TS Legs");
            return false;
        }

        private bool CutTSPTVAtMatchline(Structure addedTSTarget, Structure matchline)
        {
            int matchlineSliceLoc = CalculationHelper.ComputeSlice(matchline.CenterPoint.z, EclipseContext.GetInstance().StructureSet.Image.Origin.z, EclipseContext.GetInstance().StructureSet.Image.ZRes);

            for(int i = 0; i < matchlineSliceLoc; i++)
            {
                addedTSTarget.ClearAllContoursOnImagePlane(i);
            }
            return false;
        }

        protected override bool CalculateNumIsos()
        {
            throw new System.NotImplementedException();
        }
    }
}
