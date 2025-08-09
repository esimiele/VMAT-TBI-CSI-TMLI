using AutoPlannerHelpers.BaseCore;
using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.Prompts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using TMLIAutoPlanner.Settings;
using VMS.TPS.Common.Model.API;

namespace TMLIAutoPlanner.Core
{
    internal class TSGenerationManipulation_TMLI : TSGenerationManipulationBase
    {
        #region properties
        public List<TSRingStructureModel> AddedRings { get; private set; } = new List<TSRingStructureModel> { };
        #endregion

        #region fields
        private List<TSRingStructureModel> _requestedRings;
        #endregion

        internal TSGenerationManipulation_TMLI(List<SpecialOptimizationStructureModel> specialOptStructs,
                                               List<StructureOperationModel> operations,
                                               List<TSRingStructureModel> rings,
                                               List<PrescriptionModel> presc)
        {
            _specialOptimizationStructures = specialOptStructs;
            _structureOperations = new List<StructureOperationModel>(operations);
            _requestedRings = new List<TSRingStructureModel>(rings);
            _prescriptions = new List<PrescriptionModel>(presc);
            SetCloseOnFinish(TMLIAutoPlannerSettings.CloseProgressWindowOnFinish, 3000);
        }

        #region preliminary checks
        protected override bool PreliminaryChecks()
        {
            UpdateUILabel("Performing Preliminary Checks: ");
            int calcItems = 4;
            int counter = 0;
            //check body structure exists and is contoured
            if (!StructureTuningHelper.DoesStructureExistInSS("body", true))
            {
                ProvideUIUpdate("Error! Body structure is either empty or null! Fix and try again!", true);
                return true;
            }
            ProvideUIUpdate(100 * ++counter / calcItems, "Body structure exists and is not empty");

            //check if user origin was set
            if (IsUserOriginInsideBody()) return true;
            ProvideUIUpdate(100 * ++counter / calcItems, "User origin is inside body");

            if (CheckBodyExtentAndMatchline()) return true;
            ProvideUIUpdate(100 * ++counter / calcItems, "Body structure exists and matchline appropriate");

            ProvideUIUpdate(100, "Preliminary checks complete!");
            ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");
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
            Structure body = StructureTuningHelper.GetStructureFromId("Body");
            Point3DCollection pts = body.MeshGeometry.Positions;

            //check if patient length is > 116cm, if so, check for matchline contour
            if ((pts.Max(p => p.Z) - pts.Min(p => p.Z)) > 1160.0 && !StructureTuningHelper.DoesStructureExistInSS("matchline", true))
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

        #region structure derivation
        protected override bool CreateSpecialOptimizationStructures()
        {
            return false;
        }

        protected override bool PerformStructureDerivations()
        {
            UpdateUILabel("Perform TS Manipulations: ");
            int counter = 0;
            int calcItems = _structureOperations.Count * _prescriptions.Count;

            //construct all ts targets 
            //prescriptions are inherently sorted by increasing cumulative Rx to targets
            List<TargetModel> tmpTSTargetList = new List<TargetModel> { };
            foreach (PrescriptionModel itr in _prescriptions)
            {
                //Generate a new TSTarget
                Structure addedTSTarget = GetTSTarget(itr.TargetId);
                tmpTSTargetList.Add(new TargetModel(itr.TargetId, itr.CumulativeDoseToTarget, addedTSTarget.Id));
                if (ReferenceEquals(addedTSTarget, null) || addedTSTarget.IsEmpty)
                {
                    ProvideUIUpdate($"Error! Target structure: {itr.TargetId} is null or empty! Cannot perform tuning structure manipulations! Exiting!", true);
                    return true;
                }
            }

            if (StructureTuningHelper.DoesStructureExistInSS("matchline", true))
            {
                //main target structure won't always have the same id (ptv_tmli vs ptv_tmli_20 vs ptv_tmli_12)
                string highestDoseTSTarget = tmpTSTargetList.Last().TsTargetId;
                _structureOperations.Add(new StructureOperationModel(highestDoseTSTarget, StructureDerivationOperation.CopyContractExpand, "", "ts_ptv_legs", new StructureMarginModel(0), new StructureMarginModel(0)));
                _structureOperations.Add(new StructureOperationModel(highestDoseTSTarget, StructureDerivationOperation.CutInferiorTo, "matchline", highestDoseTSTarget, new StructureMarginModel(0), new StructureMarginModel(0)));
                _structureOperations.Add(new StructureOperationModel("ts_ptv_legs", StructureDerivationOperation.CutSuperiorTo, "matchline", "ts_ptv_legs", new StructureMarginModel(0), new StructureMarginModel(0)));
            }

            foreach (StructureOperationModel itr in _structureOperations)
            {
                if (itr.IsValidOperation)
                {
                    ProvideUIUpdate(100 * ++counter / calcItems, $"Performing: {itr.FriendlyName}");
                    if (ContourHelper.PerformStructureOperation(itr, UIUD)) return true;
                }
                else ProvideUIUpdate($"Warning! {itr.FriendlyName} is not a valid operation! Skipping!");
            }

            //if(TSManipulationList.Any(x => !string.IsNullOrEmpty(x.TargetId)))
            //{
            //    foreach (RequestedTSManipulationModel itr in TSManipulationList.Where(x => !string.IsNullOrEmpty(x.TargetId)))
            //    {
            //        //target operations
            //        if (tmpTSTargetList.Any(x => string.Equals(x.TargetId, itr.TargetId, StringComparison.OrdinalIgnoreCase)))
            //        {
            //            string tsTargetId = tmpTSTargetList.First(x => string.Equals(x.TargetId, itr.TargetId, StringComparison.OrdinalIgnoreCase)).TsTargetId;
            //            Structure tsTarget = StructureTuningHelper.GetStructureFromId(tsTargetId, EclipseContext.GetInstance().StructureSet);
            //            if (ManipulateTargetTuningStructures(itr, tsTarget)) return true;
            //            ProvideUIUpdate(100 * ++counter / calcItems);
            //        }
            //    }
            //}
            //else ProvideUIUpdate("No target TS manipulations requested!");

            //if (TSManipulationList.Any(x => string.IsNullOrEmpty(x.TargetId)))
            //{
            //    foreach (RequestedTSManipulationModel itr in TSManipulationList.Where(x => string.IsNullOrEmpty(x.TargetId)))
            //    {
            //        if (ManipulateOARTuningStructures(itr)) return true;
            //        ProvideUIUpdate(100 * ++counter / calcItems);
            //    }
            //}
            //else ProvideUIUpdate("No OAR TS manipulations requested!");

            //if (!TMLIAutoPlannerSettings.AllBeamsVMAT && StructureTuningHelper.DoesStructureExistInSS("matchline", true))
            //{
            //    ProvideUIUpdate($"Cutting {tmpTSTargetList.Last().TsTargetId} at the matchline!");

            //    //find the image plane where the matchline is location. Record this value and break the loop. Also find the first slice where the ptv_body contour starts and record this value
            //    Structure matchline = StructureTuningHelper.GetStructureFromId("matchline");
            //    ProvideUIUpdate($"Retrieved matchline structure: {matchline.Id}");

            //    if (ContourTSLegs("TS_PTV_Legs", matchline, StructureTuningHelper.GetStructureFromId(tmpTSTargetList.Last().TsTargetId))) return true;
            //    if (CutTSPTVAtMatchline(StructureTuningHelper.GetStructureFromId(tmpTSTargetList.Last().TsTargetId), matchline)) return true;
            //}

            ////prescriptions are inherently sorted by increasing cumulative Rx to targets
            //foreach (PrescriptionModel itr in prescriptions)
            //{
            //    //Generate a new TSTarget
            //    Structure addedTSTarget = GetTSTarget(itr.TargetId);
            //    tmpTSTargetList.Add(new TargetModel(itr.TargetId, itr.CumulativeDoseToTarget, addedTSTarget.Id));
            //    if (ReferenceEquals(addedTSTarget, null) || addedTSTarget.IsEmpty)
            //    {
            //        ProvideUIUpdate($"Error! Target structure: {itr.TargetId} is null or empty! Cannot perform tuning structure manipulations! Exiting!", true);
            //        return true;
            //    }
            //    if (TSManipulationList.Any())
            //    {
            //        //perform all relevant TS manipulations for the specified target
            //        foreach (RequestedTSManipulationModel itr1 in TSManipulationList)
            //        {
            //            if (ManipulateTuningStructures(itr1, addedTSTarget)) return true;
            //            ProvideUIUpdate(100 * ++counter / calcItems);
            //        }
            //    }
            //    else ProvideUIUpdate("No TS manipulations requested!");
            //    if (string.Equals(itr.TargetId.ToLower(), "ptv_tmli") && !TMLIAutoPlannerSettings.AllBeamsVMAT && StructureTuningHelper.DoesStructureExistInSS("matchline", EclipseContext.GetInstance().StructureSet, true))
            //    {
            //        ProvideUIUpdate($"Cutting {addedTSTarget} at the matchline!");

            //        //find the image plane where the matchline is location. Record this value and break the loop. Also find the first slice where the ptv_body contour starts and record this value
            //        Structure matchline = StructureTuningHelper.GetStructureFromId("matchline", EclipseContext.GetInstance().StructureSet);
            //        ProvideUIUpdate($"Retrieved matchline structure: {matchline.Id}");

            //        if (ContourTSLegs("TS_PTV_Legs", matchline, addedTSTarget)) return true;
            //        if (CutTSPTVAtMatchline(addedTSTarget, matchline)) return true;
            //    }
            //}
            //only one plan is allowed for the prescriptions --> last item is the highest Rx target for this plan and needs to be set as the normalization volume
            NormalizationVolumes.Add(_prescriptions.Last().PlanId, tmpTSTargetList.OrderByDescending(x => x.TargetRxDose).First().TsTargetId);
            PlanTargets.Add(new PlanTargetsModel(_prescriptions.Last().PlanId, new List<TargetModel>(tmpTSTargetList)));

            ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");
            return false;
        }

        protected override bool PerformPlanSpecificStructureDerivations()
        {
            if (_requestedRings.Any())
            {
                AddedRings = new List<TSRingStructureModel>(GenerateRings(_requestedRings));
                if (!AddedRings.Any()) return true;
            }
            return false;
        }
        #endregion

        #region Isocenter calculation
        protected override bool CalculateNumIsos()
        {
            UpdateUILabel("Calculate number of isos:");
            int percentComplete = 0;
            int calcItems = 5;
            Structure body = StructureTuningHelper.GetStructureFromId("body");
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Retrieved body structure");
            Point3DCollection pts = body.MeshGeometry.Positions;
            double bodyExtent = pts.Max(p => p.Z) - pts.Min(p => p.Z);
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Calculated maximum extent of body: {bodyExtent:0.0} mm");

            //calculate number of required isocenters
            if (TMLIAutoPlannerSettings.AllBeamsVMAT) PlanIsocentersList.AddRange(CalculateNumIsosAllVMAT(bodyExtent));
            else PlanIsocentersList.AddRange(CalculateNumIsosVMATAndAPPA(bodyExtent, pts.Max(p => p.Z), pts.Min(p => p.Z)));
            if(!PlanIsocentersList.Any())
            {
                ProvideUIUpdate("Error! No plan isocenters in the list! Calculation of number of isocenters failed! Exiting!", true);
                return true;
            }

            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Retrieved appropriate isocenter names:");
            foreach (PlanIsocenterModel itr in PlanIsocentersList)
            {
                ProvideUIUpdate($"Plan Id: {itr.PlanId}");
                foreach (IsocenterModel itr1 in itr.Isocenters)
                {
                    ProvideUIUpdate($" {itr1.IsocenterId}");
                }
            }
            return false;
        }

        private List<PlanIsocenterModel> CalculateNumIsosAllVMAT(double bodyExtent)
        {
            int percentComplete = 1;
            int calcItems = 5;
            List<PlanIsocenterModel> planIsos = new List<PlanIsocenterModel>();
            //calculate number of required isocenters
            ProvideUIUpdate("All beams requested to be VMAT");
            //no matchline implying that this patient will be treated with VMAT only. For these cases the maximum number of allowed isocenters is 3.
            //the reason for the explicit statements calculating the number of isos and then truncating them to 3 was to account for patients requiring < 3 isos and if, later on, we want to remove the restriction of 3 isos
            NumberofIsocenters = NumberofVMATIsocenters = (int)Math.Ceiling(bodyExtent / (TMLIAutoPlannerSettings.MaxFieldYExtent - TMLIAutoPlannerSettings.MinFieldOverlap));
            ProvideUIUpdate(100 * ++percentComplete / calcItems);
            ProvideUIUpdate($"Calculated required number of VMAT Isos: {NumberofVMATIsocenters}");
            ProvideUIUpdate($"Calculated total number of Isos: {NumberofIsocenters}");

            if (NumberofIsocenters == 6)
            {
                List<IsocenterModel> isos = IsoNameHelper.GetTBIVMATIsoNames(4, 4);
                isos.Add(new IsocenterModel("upper legs", 2, BeamType.VMAT));
                isos.Add(new IsocenterModel("lower legs", 2, BeamType.VMAT));
                planIsos.Add(new PlanIsocenterModel(_prescriptions.First().PlanId, isos));
            }
            else
            {
                int numUpperIsos = 3;
                int numLowerIsos = NumberofIsocenters - numUpperIsos;
                List<IsocenterModel> isos = IsoNameHelper.GetTBIVMATIsoNames(numUpperIsos, NumberofIsocenters);

                if (numLowerIsos == 2)
                {
                    isos.Add(new IsocenterModel("upper legs", 2, BeamType.VMAT));
                    isos.Add(new IsocenterModel("lower legs", 2, BeamType.VMAT));
                }
                else
                {
                    isos.Add(new IsocenterModel("legs", 2, BeamType.VMAT));
                }
                planIsos.Add(new PlanIsocenterModel(_prescriptions.First().PlanId, isos));
            }
            
            return planIsos;
        }

        private List<PlanIsocenterModel> CalculateNumIsosVMATAndAPPA(double bodyExtent, double bodyZMax, double bodyZMin)
        {
            int percentComplete = 1;
            int calcItems = 5;
            List<PlanIsocenterModel> planIsos = new List<PlanIsocenterModel>();

            //calculate number of required isocenters
            if (!StructureTuningHelper.DoesStructureExistInSS("matchline"))
            {
                ProvideUIUpdate("matchline structure not present in structure set");
                //no matchline implying that this patient will be treated with VMAT only. For these cases the maximum number of allowed isocenters is 3.
                //the reason for the explicit statements calculating the number of isos and then truncating them to 3 was to account for patients requiring < 3 isos and if, later on, we want to remove the restriction of 3 isos
                NumberofIsocenters = NumberofVMATIsocenters = (int)Math.Ceiling(bodyExtent / (TMLIAutoPlannerSettings.MaxFieldYExtent - TMLIAutoPlannerSettings.MinFieldOverlap));
                if (NumberofIsocenters > 4) NumberofIsocenters = NumberofVMATIsocenters = 4;
                ProvideUIUpdate(100 * ++percentComplete / calcItems);
            }
            else
            {
                //matchline structure is present, but empty
                if (!StructureTuningHelper.DoesStructureExistInSS("matchline", true))
                {
                    ConfirmPrompt CP = new ConfirmPrompt("I found a matchline structure in the structure set, but it's empty!" + Environment.NewLine + Environment.NewLine + "Do you want to continue without using the matchline structure?!");
                    CP.ShowDialog();
                    if (!CP.GetSelection()) return planIsos;

                    //continue and ignore the empty matchline structure (same calculation as VMAT only)
                    NumberofIsocenters = NumberofVMATIsocenters = (int)Math.Ceiling(bodyExtent / (TMLIAutoPlannerSettings.MaxFieldYExtent - TMLIAutoPlannerSettings.MinFieldOverlap));
                    if (NumberofIsocenters > 4) NumberofIsocenters = NumberofVMATIsocenters = 4;
                    ProvideUIUpdate(100 * ++percentComplete / calcItems);

                }
                //matchline structure is present and not empty
                else
                {
                    calcItems += 2;
                    Structure matchline = StructureTuningHelper.GetStructureFromId("matchline");
                    ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Retrieved matchline structure");
                    //get number of isos for PTV superior to matchplane (always truncate this value to a maximum of 4 isocenters)
                    NumberofVMATIsocenters = (int)Math.Ceiling((bodyZMax - matchline.CenterPoint.z) / (TMLIAutoPlannerSettings.MaxFieldYExtent - TMLIAutoPlannerSettings.MinFieldOverlap));
                    if (NumberofVMATIsocenters > 4) NumberofVMATIsocenters = 4;
                    ProvideUIUpdate($"Separation between body z max and matchline center z: {(bodyZMax - matchline.CenterPoint.z):0.0}");
                    ProvideUIUpdate($"numVAMTIsos calculated as double: {(bodyZMax - matchline.CenterPoint.z) / (TMLIAutoPlannerSettings.MaxFieldYExtent - TMLIAutoPlannerSettings.MinFieldOverlap):0.0}");
                    ProvideUIUpdate(100 * ++percentComplete / calcItems);

                    //Only add a second legs iso if the extent of the body is > 40.0 cm
                    ProvideUIUpdate($"Separation between matchline z center and body z min: {matchline.CenterPoint.z - bodyZMin:0.0}");
                    if (matchline.CenterPoint.z - bodyZMin <= TMLIAutoPlannerSettings.MaxFieldYExtent)
                    {
                        ProvideUIUpdate($"Separation between matchline z center and body z min is <= maximum field extent ({TMLIAutoPlannerSettings.MaxFieldYExtent} mm)");
                        ProvideUIUpdate($"Only one APPA isocenters is required for coverage");
                        NumberofIsocenters = NumberofVMATIsocenters + 1;
                    }
                    else
                    {
                        ProvideUIUpdate($"Separation between matchline z center and body z min is > maximum field extent ({TMLIAutoPlannerSettings.MaxFieldYExtent} mm)");
                        ProvideUIUpdate($"Two APPA isocenters are required for coverage");
                        NumberofIsocenters = NumberofVMATIsocenters + 2;
                    }
                    ProvideUIUpdate(100 * ++percentComplete / calcItems);
                }
            }
            ProvideUIUpdate($"Calculated required number of VMAT Isos: {NumberofVMATIsocenters}");
            ProvideUIUpdate($"Calculated total number of Isos: {NumberofIsocenters}");

            //set isocenter names based on numIsos and numVMATIsos (determined these names from prior cases)
            planIsos.Add(new PlanIsocenterModel(_prescriptions.First().PlanId, IsoNameHelper.GetTBIVMATIsoNames(NumberofVMATIsocenters, NumberofIsocenters)));
            if (NumberofIsocenters > NumberofVMATIsocenters)
            {
                if (NumberofIsocenters == NumberofVMATIsocenters + 2)
                {
                    planIsos.Add(new PlanIsocenterModel("_upper legs", new IsocenterModel("upper legs", 2, BeamType.APPA)));
                    planIsos.Add(new PlanIsocenterModel("_lower legs", new IsocenterModel("lower legs", 2, BeamType.APPA)));
                }
                else
                {
                    planIsos.Add(new PlanIsocenterModel("_legs", new IsocenterModel("legs", 2, BeamType.APPA)));
                }
            }
            
            return planIsos;
        }
        #endregion
    }
}
