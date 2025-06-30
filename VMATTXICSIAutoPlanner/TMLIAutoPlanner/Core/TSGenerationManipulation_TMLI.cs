using AutoPlannerHelpers.BaseCore;
using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Enums;
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
        public List<TSRingStructureModel> AddedRings { get; private set; } = new List<TSRingStructureModel> { };
        #endregion

        #region fields
        //DICOM types
        //Possible values are "AVOIDANCE", "CAVITY", "CONTRAST_AGENT", "CTV", "EXTERNAL", "GTV", "IRRAD_VOLUME", 
        //"ORGAN", "PTV", "TREATED_VOLUME", "SUPPORT", "FIXATION", "CONTROL", and "DOSE_REGION". 
        private List<PrescriptionModel> prescriptions;
        private List<TSRingStructureModel> _requestedRings;
        
        #endregion

        internal TSGenerationManipulation_TMLI(List<StructureOperationModel> operations,
                                               List<TSRingStructureModel> rings,
                                               List<PrescriptionModel> presc)
        {
            _structureOperations = new List<StructureOperationModel>(operations);
            _requestedRings = new List<TSRingStructureModel>(rings);
            prescriptions = new List<PrescriptionModel>(presc);
            SetCloseOnFinish(TMLIAutoPlannerSettings.CloseProgressWindowOnFinish, 3000);
        }

        [HandleProcessCorruptedStateExceptions]
        protected override bool Run()
        {
            try
            {
                PlanIsocentersList.Clear();
                if (PreliminaryChecks()) return true;
                if (UnionLRStructures()) return true;
                if (_structureOperations.Any()) if (CheckHighResolution()) return true;
                //if (CreateTSStructures()) return true;
                if (PerformStructureDerivations()) return true;
                if (_requestedRings.Any())
                {
                    AddedRings = new List<TSRingStructureModel>(GenerateRings(_requestedRings));
                    if (!AddedRings.Any()) return true;
                }
                if (CalculateNumIsos()) return true; 
                UpdateUILabel("Finished!");
                ProvideUIUpdate(100, "Finished Structure Tuning!");
                ProvideUIUpdate($"Run time: {ElapsedRunTime} (mm:ss)");
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

        //protected override bool CreateTSStructures()
        //{
        //    UpdateUILabel("Create TS Structures:");
        //    ProvideUIUpdate("Adding remaining tuning structures to stack!");
        //    if (RemoveOldTSStructures(TS_structures, true)) return true;

        //    int counter = 0;
        //    int calcItems = TS_structures.Count;

        //    foreach (RequestedTSStructureModel itr in TS_structures)
        //    {
        //        ProvideUIUpdate(100 * ++counter / calcItems, $"Adding {itr.StructureId} to the structure set!");
        //        AddTSStructures(itr);
        //    }

        //    ProvideUIUpdate(100, "Finished adding tuning structures!");
        //    ProvideUIUpdate(0, "Contouring tuning structures!");

        //    counter = 0;
        //    calcItems = AddedStructureIds.Count;
        //    //now contour the various structures
        //    foreach (string itr in AddedStructureIds)
        //    {
        //        ProvideUIUpdate($"Contouring TS: {itr}");
        //        Structure addedStructure = StructureTuningHelper.GetStructureFromId(itr, EclipseContext.GetInstance().StructureSet);
        //        ProvideUIUpdate($"Retrieved structure: {addedStructure.Id}");
        //        //logic goes here
        //        //
        //        ProvideUIUpdate(100 * ++counter / calcItems);
        //    }
        //    ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");
        //    return false;
        //}

        protected override bool PerformStructureDerivations()
        {
            UpdateUILabel("Perform TS Manipulations: ");
            int counter = 0;
            int calcItems = _structureOperations.Count * prescriptions.Count;

            //construct all ts targets 
            //prescriptions are inherently sorted by increasing cumulative Rx to targets
            List<TargetModel> tmpTSTargetList = new List<TargetModel> { };
            foreach (PrescriptionModel itr in prescriptions)
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

            if (!TMLIAutoPlannerSettings.AllBeamsVMAT && StructureTuningHelper.DoesStructureExistInSS("matchline", EclipseContext.GetInstance().StructureSet, true))
            {
                ProvideUIUpdate($"Cutting {tmpTSTargetList.Last().TsTargetId} at the matchline!");

                //find the image plane where the matchline is location. Record this value and break the loop. Also find the first slice where the ptv_body contour starts and record this value
                Structure matchline = StructureTuningHelper.GetStructureFromId("matchline", EclipseContext.GetInstance().StructureSet);
                ProvideUIUpdate($"Retrieved matchline structure: {matchline.Id}");

                if (ContourTSLegs("TS_PTV_Legs", matchline, StructureTuningHelper.GetStructureFromId(tmpTSTargetList.Last().TsTargetId, EclipseContext.GetInstance().StructureSet))) return true;
                if (CutTSPTVAtMatchline(StructureTuningHelper.GetStructureFromId(tmpTSTargetList.Last().TsTargetId, EclipseContext.GetInstance().StructureSet), matchline)) return true;
            }

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
            NormalizationVolumes.Add(prescriptions.Last().PlanId, tmpTSTargetList.OrderByDescending(x => x.TargetRxDose).First().TsTargetId);
            PlanTargets.Add(new PlanTargetsModel(prescriptions.Last().PlanId, new List<TargetModel>(tmpTSTargetList)));

            ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");
            return false;
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
            UpdateUILabel("Calculate number of isos:");
            int percentComplete = 0;
            int calcItems = 5;
            Structure body = StructureTuningHelper.GetStructureFromId("body", EclipseContext.GetInstance().StructureSet);
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
                planIsos.Add(new PlanIsocenterModel(prescriptions.First().PlanId, isos));
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
                planIsos.Add(new PlanIsocenterModel(prescriptions.First().PlanId, isos));
            }
            
            return planIsos;
        }

        private List<PlanIsocenterModel> CalculateNumIsosVMATAndAPPA(double bodyExtent, double bodyZMax, double bodyZMin)
        {
            int percentComplete = 1;
            int calcItems = 5;
            List<PlanIsocenterModel> planIsos = new List<PlanIsocenterModel>();

            //calculate number of required isocenters
            if (!StructureTuningHelper.DoesStructureExistInSS("matchline", EclipseContext.GetInstance().StructureSet))
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
                if (!StructureTuningHelper.DoesStructureExistInSS("matchline", EclipseContext.GetInstance().StructureSet, true))
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
                    Structure matchline = StructureTuningHelper.GetStructureFromId("matchline", EclipseContext.GetInstance().StructureSet);
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
            planIsos.Add(new PlanIsocenterModel(prescriptions.First().PlanId, IsoNameHelper.GetTBIVMATIsoNames(NumberofVMATIsocenters, NumberofIsocenters)));
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
    }
}
