using AutoPlannerHelpers.BaseCore;
using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.Prompts;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Windows.Media.Media3D;
using TBIAutoPlanner.Settings;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace TBIAutoPlanner.Core
{
    internal class TSGenerationManipulation_TBI : TSGenerationManipulationBase
    {
        #region fields
        private bool _useFlash;
        private double _flashMargin;
        private double _ptvMarginFromBody;
        #endregion

        internal TSGenerationManipulation_TBI(List<SpecialOptimizationStructureModel> specialOptStruct,
                                              List<StructureOperationModel> list,
                                              List<PrescriptionModel> presc,
                                              bool flash,
                                              double flashMargin,
                                              double ptvMargin) 
        {
            _prescriptions = new List<PrescriptionModel>(presc);
            _specialOptimizationStructures = specialOptStruct;
            _structureOperations = new List<StructureOperationModel>(list);
            _useFlash = flash;
            _flashMargin = flashMargin;
            _ptvMarginFromBody = ptvMargin;
            SetCloseOnFinish(TBIAutoPlannerSettings.CloseProgressWindowOnFinish, 3000);
        }

        #region Preliminary checks
        /// <summary>
        /// Preliminary checks to ensure the body exists, the user origin is inside the body, body extent and matchline presence, and check if the prep 
        /// script was running previously
        /// </summary>
        /// <returns></returns>
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

            if (CheckIfScriptRunPreviously()) return true;
            ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");

            //check if user origin was set
            if (IsUserOriginInsideBody()) return true;
            ProvideUIUpdate(100 * ++counter / calcItems, "User origin is inside body");

            if (CheckBodyExtentAndMatchline()) return true;
            ProvideUIUpdate(100 * ++counter / calcItems, "Body structure exists and matchline appropriate");

            return false;
        }

        /// <summary>
        /// Check the structure set for indications that this script was run previously
        /// </summary>
        /// <returns></returns>
        private bool CheckIfScriptRunPreviously()
        {
            if (StructureTuningHelper.DoesStructureExistInSS("human_body", true))
            {
                if (EclipseContext.GetInstance().StructureSet.Structures.Any(x => x.Id.ToLower().Contains("flash")))
                {
                    ProvideUIUpdate($"Script has been run previously and flash structures exist!");
                    //copy human_body back onto body if flash was used in previous run of the script
                    Structure body = StructureTuningHelper.GetStructureFromId("body");
                    Structure humanBody = StructureTuningHelper.GetStructureFromId("human_body");
                    body.SegmentVolume = ContourHelper.CopyStructure(humanBody, new StructureMarginModel(0));
                    ProvideUIUpdate($"Copied {humanBody.Id} structure onto {body.Id}!");
                }
            }
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

        #region Helper methods for create ts structures
        /// <summary>
        /// Simple method to union the left and right lung block structures (for scleroderma trial)
        /// </summary>
        /// <param name="addedStructure"></param>
        /// <returns></returns>
        //private bool ContourLungsEvalVolume(Structure addedStructure)
        //{
        //    Structure lung_block_left = StructureTuningHelper.GetStructureFromId("lung_block_l");
        //    ProvideUIUpdate("Retrived left lung block structure");
        //    Structure lung_block_right = StructureTuningHelper.GetStructureFromId("lung_block_r");
        //    ProvideUIUpdate("Retrived right lung block structure");
        //    if (lung_block_left == null || lung_block_left.IsEmpty)
        //    {
        //        ProvideUIUpdate($"Error! Lung_Block_L volume is null or empty! Could not contour Lungs_Eval structure! Exiting!", true);
        //        return true;
        //    }
        //    if (lung_block_right == null || lung_block_right.IsEmpty)
        //    {
        //        ProvideUIUpdate($"Error! Lung_Block_R volume is null or empty! Could not contour Lungs_Eval structure! Exiting!", true);
        //        return true;
        //    }
        //    addedStructure.SegmentVolume = lung_block_left.Or(lung_block_right.Margin(0.0));
        //    ProvideUIUpdate($"Contoured lung eval structure: {addedStructure.Id}");
        //    return false;
        //}

        /// <summary>
        /// Dedicated method for contouring the lung and kidney block volumes required by the scleroderma trial
        /// </summary>
        /// <param name="addedStructure"></param>
        /// <returns></returns>
        //private bool ContourBlockVolume(Structure addedStructure)
        //{
        //    Structure baseStructure;
        //    AxisAlignedMargins margins;
        //    ProvideUIUpdate($"Contouring block structure:");
        //    if (addedStructure.Id.ToLower().Contains("lung_block_l"))
        //    {
        //        //AxisAlignedMargins(inner or outer margin, margin from negative x, margin for negative y, margin for negative z, margin for positive x, margin for positive y, margin for positive z)
        //        baseStructure = StructureTuningHelper.GetStructureFromId("lung_l");
        //        margins = new AxisAlignedMargins(StructureMarginGeometry.Inner, 10.0, 10.0, 15.0, 10.0, 10.0, 10.0);
        //    }
        //    else if (addedStructure.Id.ToLower().Contains("lung_block_r"))
        //    {
        //        baseStructure = StructureTuningHelper.GetStructureFromId("lung_r");
        //        margins = new AxisAlignedMargins(StructureMarginGeometry.Inner, 10.0, 10.0, 15.0, 10.0, 10.0, 10.0);
        //    }
        //    else if (addedStructure.Id.ToLower().Contains("kidney_block_l"))
        //    {
        //        baseStructure = StructureTuningHelper.GetStructureFromId("kidney_l");
        //        margins = new AxisAlignedMargins(StructureMarginGeometry.Outer, 5.0, 20.0, 20.0, 20.0, 20.0, 20.0);
        //    }
        //    else
        //    {
        //        baseStructure = StructureTuningHelper.GetStructureFromId("kidney_r");
        //        margins = new AxisAlignedMargins(StructureMarginGeometry.Outer, 5.0, 20.0, 20.0, 20.0, 20.0, 20.0);
        //    }
        //    if (baseStructure == null || baseStructure.IsEmpty)
        //    {
        //        ProvideUIUpdate($"Error! Could not retrieve base structure to contour {addedStructure.Id}! Exiting!", true);
        //        return true;
        //    }
        //    ProvideUIUpdate($"Base structure: {baseStructure.Id}");
        //    ProvideUIUpdate("Margins:");
        //    ProvideUIUpdate($"Inner or outer: {margins.Geometry}");
        //    ProvideUIUpdate($"X1: {margins.X1:0.0} mm");
        //    ProvideUIUpdate($"X2: {margins.X2:0.0} mm");
        //    ProvideUIUpdate($"Y1: {margins.Y1:0.0} mm");
        //    ProvideUIUpdate($"Y2: {margins.Y2:0.0} mm");
        //    ProvideUIUpdate($"Z1: {margins.Z1:0.0} mm");
        //    ProvideUIUpdate($"Z2: {margins.Z2:0.0} mm");

        //    addedStructure.SegmentVolume = baseStructure.AsymmetricMargin(margins);
        //    ProvideUIUpdate($"Contoured block volume for structure: {addedStructure.Id}");
        //    return false;
        //}
        #endregion

        #region structure derivation
        protected override bool CreateSpecialOptimizationStructures()
        {
            return false;
        }

        /// <summary>
        /// Directory method for controlling the flow of TS structure manipulations
        /// </summary>
        /// <returns></returns>
        protected override bool PerformStructureDerivations()
        {
            UpdateUILabel("Contouring opt structures now:");
            int counter = 0;
            int calcItems = _structureOperations.Count * _prescriptions.Count;

            List<TargetModel> tmpTSTargetList = new List<TargetModel> { };
            foreach (PrescriptionModel itr in _prescriptions)
            {
                //Generate a new TSTarget
                Structure addedTSTarget = GetTSTarget(itr.TargetId, string.Equals(itr.TargetId, "PTV_Body", StringComparison.OrdinalIgnoreCase) ? "TS_PTV_VMAT" : "");
                tmpTSTargetList.Add(new TargetModel(itr.TargetId, itr.CumulativeDoseToTarget, addedTSTarget.Id));
                if (ReferenceEquals(addedTSTarget, null) || addedTSTarget.IsEmpty)
                {
                    ProvideUIUpdate($"Error! Target structure: {itr.TargetId} is null or empty! Cannot perform tuning structure manipulations! Exiting!", true);
                    return true;
                }
            }
            if(StructureTuningHelper.DoesStructureExistInSS("matchline", true))
            {
                _structureOperations.Add(new StructureOperationModel("ts_ptv_vmat", StructureDerivationOperation.CopyContractExpand, "", "TS_PTV_legs",new StructureMarginModel(0), new StructureMarginModel(0)));
                _structureOperations.Add(new StructureOperationModel("ts_ptv_vmat", StructureDerivationOperation.CutInferiorTo, "matchline", "ts_ptv_vmat", new StructureMarginModel(0), new StructureMarginModel(0)));
                _structureOperations.Add(new StructureOperationModel("ts_ptv_legs", StructureDerivationOperation.CutSuperiorTo, "matchline", "TS_PTV_legs", new StructureMarginModel(0), new StructureMarginModel(0)));
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

            //only one plan is allowed for the prescriptions --> last item is the highest Rx target for this plan and needs to be set as the normalization volume
            NormalizationVolumes.Add(_prescriptions.Last().PlanId, tmpTSTargetList.OrderByDescending(x => x.TargetRxDose).First().TsTargetId);
            PlanTargets.Add(new PlanTargetsModel(_prescriptions.Last().PlanId, new List<TargetModel>(tmpTSTargetList)));

            ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");
            return false;
        }


        /// <summary>
        /// Utility method for creating virtual bolus/flash
        /// </summary>
        /// <returns></returns>
        private bool CreateFlash()
        {
            UpdateUILabel("Create flash:");
            int percentComplete = 0;
            int calcItems = 10;
            //create flash for the plan per the users request
            //NOTE: IT IS IMPORTANT THAT ALL OF THE STRUCTURES CREATED IN THIS METHOD (I.E., ALL STRUCTURES USED TO GENERATE FLASH HAVE THE KEYWORD 'FLASH' SOMEWHERE IN THE STRUCTURE ID)!
            //first need to create a bolus structure (remove it if it already exists)
            (bool failBolus, Structure bolusFlash) = RemoveAndGenerateStructure("BOLUS_FLASH");
            if (failBolus) return true;
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Created structure: {bolusFlash.Id}");
            //now create the ptv_flash structure
            (bool failPTVFlash, Structure ptvBodyFlash) = RemoveAndGenerateStructure("PTV_BODY_FLASH");
            if (failPTVFlash) return true;
            (bool failFlashTarget, Structure TSPTVFlash) = RemoveAndGenerateStructure("TS_PTV_FLASH");
            if (failFlashTarget) return true;
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Created structure: {ptvBodyFlash.Id}");

            List<StructureOperationModel> operations = new List<StructureOperationModel>
            {
                //create bolus
                new StructureOperationModel("body", StructureDerivationOperation.CopyContractExpand, "", "Human_Body"),
                new StructureOperationModel("body", StructureDerivationOperation.CopyContractExpand, "", "BOLUS_FLASH", new StructureMarginModel(_flashMargin), new StructureMarginModel(0)),
                new StructureOperationModel("BOLUS_FLASH", StructureDerivationOperation.Crop, "body", "BOLUS_FLASH"),
                StructureTuningHelper.DoesStructureExistInSS("matchline", true) ? new StructureOperationModel("BOLUS_FLASH", StructureDerivationOperation.CutInferiorTo, "matchline", "BOLUS_FLASH") : new StructureOperationModel(),
                new StructureOperationModel("body", StructureDerivationOperation.Union, "BOLUS_FLASH", "body"),

                //create flash ptvs
                new StructureOperationModel("human_body", StructureDerivationOperation.CopyContractExpand, "", "_tmpBody", new StructureMarginModel(-_ptvMarginFromBody - 0.1), new StructureMarginModel(0), true),
                new StructureOperationModel("_tmpBody", StructureDerivationOperation.CopyContractExpand, "", "_tmpBolus", new StructureMarginModel(_flashMargin + 0.1), new StructureMarginModel(0.0), true),
                new StructureOperationModel("_tmpBolus", StructureDerivationOperation.Crop, "_tmpBody", "_tmpBolus", true),
                StructureTuningHelper.DoesStructureExistInSS("matchline", true) ? new StructureOperationModel("_tmpBolus", StructureDerivationOperation.CutInferiorTo, "matchline", "_tmpBolus") : new StructureOperationModel(),
                new StructureOperationModel("_tmpBolus", StructureDerivationOperation.Union,"PTV_Body", ptvBodyFlash.Id),
                new StructureOperationModel(ptvBodyFlash.Id, StructureDerivationOperation.CopyContractExpand, "",TSPTVFlash.Id),
                StructureTuningHelper.DoesStructureExistInSS("matchline", true) ? new StructureOperationModel(TSPTVFlash.Id, StructureDerivationOperation.CutInferiorTo, "matchline", TSPTVFlash.Id) : new StructureOperationModel(),
            };
            foreach (StructureOperationModel itr in operations)
            {
                if (itr.IsValidOperation)
                {
                    ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Performing: {itr.FriendlyName}");
                    if (ContourHelper.PerformStructureOperation(itr, UIUD)) return true;
                }
                else ProvideUIUpdate($"Warning! {itr.FriendlyName} is not a valid operation! Skipping!");
            }

            //assign the water to the bolus volume (HU = 0.0)
            bolusFlash.SetAssignedHU(0.0);
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Assigned {bolusFlash.Id} HU to 0.0");

            ContourHelper.CleanTemporaryStructures(operations);

            ////Now extend the body contour to include the bolus_flash structure. The reason for this is because Eclipse automatically sets the dose calculation grid to the body structure contour (no overriding this)
            //body.SegmentVolume = ContourHelper.ContourUnion(bolusFlash, body, new StructureMarginModel(0.0), new StructureMarginModel(0.0));
            //ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Contour union betwen between {bolusFlash.Id} and body onto body");

            ////copy the NEW body structure (i.e., body + bolus_flash)
            //if (GeneratePTVFromBody(ptvBodyFlash)) return true;
            //ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Contoured {ptvBodyFlash.Id} structure from body structure");

            //foreach (RequestedTSManipulationModel itr in TSManipulationList.Where(x => !string.IsNullOrEmpty(x.TargetId) && string.Equals(x.TargetId, "ptv_body", StringComparison.OrdinalIgnoreCase)))
            //{
            //    //only grab the ts target manipulations intended for ptv_body
            //    ManipulateTargetTuningStructures(itr, ptvBodyFlash);
            //    ProvideUIUpdate(100 * ++percentComplete / calcItems);
            //}

            ////now create the ptv_flash structure (analogous to PTV_Body)
            
            //ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Created structure: {TSPTVFlash.Id}");

            //(bool failCopyTarget, StringBuilder copyErrorMessage) = ContourHelper.CopyStructureOntoStructure(ptvBodyFlash, TSPTVFlash);
            //if (failCopyTarget)
            //{
            //    ProvideUIUpdate(copyErrorMessage.ToString(), true);
            //    return true;
            //}
            //ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Copied structure {ptvBodyFlash.Id} onto {TSPTVFlash.Id}");

            //if (StructureTuningHelper.DoesStructureExistInSS("matchline", true))
            //{
            //    //crop flash at matchline ONLY if global flash is used
            //    Structure dummyBox = StructureTuningHelper.GetStructureFromId("dummybox");
            //    ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Retrieved dummy box structure: {dummyBox.Id}");

            //    if (CutTSTargetFromMatchline(TSPTVFlash, StructureTuningHelper.GetStructureFromId("matchline"), dummyBox)) return true;
            //    ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Cut {TSPTVFlash.Id} structure at matchline structure");
            //}
            NormalizationVolumes = new Dictionary<string, string>(UpdateNormVolumesWithFlash(NormalizationVolumes));
            PlanTargets = new List<PlanTargetsModel>(UpdateTsTargetsWithFlash(PlanTargets));
            return false;
        }

        protected override bool PerformPlanSpecificStructureDerivations()
        {
            if (_useFlash) if (CreateFlash()) return true;
            return false;
        }
        #endregion

        /// <summary>
        /// Helper method to update the TS targets list with the analogous flash targets
        /// </summary>
        /// <returns></returns>
        private List<PlanTargetsModel> UpdateTsTargetsWithFlash(List<PlanTargetsModel> plantargets)
        {
            //we know ts_PTV_VMAT was listed as a ts target, so we will need to go in and replace that with the corresponding flash targets
            List<TargetModel> targets = plantargets.First().Targets;
            if (targets.Any(x => string.Equals(x.TsTargetId, "TS_PTV_VMAT", StringComparison.OrdinalIgnoreCase)))
            {
                targets.First(x => string.Equals(x.TsTargetId, "TS_PTV_VMAT", StringComparison.OrdinalIgnoreCase)).TsTargetId = "TS_PTV_FLASH";
            }
            return plantargets;
        }

        /// <summary>
        /// Helper method to update the normalization volumes list with the analogous flash targets
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> UpdateNormVolumesWithFlash(Dictionary<string, string> volumes)
        {
            Dictionary<string, string> updatedNormVolumes = new Dictionary<string, string>(volumes);
            //only update the normalization volumes if ts_ptv_vmat was set to the normalization volume for this plan
            if (string.Equals(updatedNormVolumes.First().Value, "TS_PTV_VMAT"))
            {
                //normalization volume for plan is ts_ptv_vmat
                //--> update to ts_ptv_flash
                updatedNormVolumes.Clear();
                updatedNormVolumes.Add(_prescriptions.First().PlanId, "TS_PTV_FLASH");
            }
            return updatedNormVolumes;
        }

        #region isocenter calculation
        /// <summary>
        /// Method to calculate the required number of VMAT isocenters and the total number of isocenters (including AP/PA isocenters is needed)
        /// </summary>
        /// <returns></returns>
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
            if (!StructureTuningHelper.DoesStructureExistInSS("matchline"))
            {
                ProvideUIUpdate("matchline structure not present in structure set");
                //no matchline implying that this patient will be treated with VMAT only. For these cases the maximum number of allowed isocenters is 3.
                //the reason for the explicit statements calculating the number of isos and then truncating them to 3 was to account for patients requiring < 3 isos and if, later on, we want to remove the restriction of 3 isos
                NumberofIsocenters = NumberofVMATIsocenters = (int)Math.Ceiling(bodyExtent / (TBIAutoPlannerSettings.MaxFieldYExtent - TBIAutoPlannerSettings.MinFieldOverlap));
                if (NumberofIsocenters > 3) NumberofIsocenters = NumberofVMATIsocenters = 3;
                ProvideUIUpdate(100 * ++percentComplete / calcItems);
            }
            else
            {
                //matchline structure is present, but empty
                if (!StructureTuningHelper.DoesStructureExistInSS("matchline", true))
                {
                    ConfirmPrompt CP = new ConfirmPrompt("I found a matchline structure in the structure set, but it's empty!" + Environment.NewLine + Environment.NewLine + "Do you want to continue without using the matchline structure?!");
                    CP.ShowDialog();
                    if (!CP.GetSelection()) return true;

                    //continue and ignore the empty matchline structure (same calculation as VMAT only)
                    NumberofIsocenters = NumberofVMATIsocenters = (int)Math.Ceiling(bodyExtent / (TBIAutoPlannerSettings.MaxFieldYExtent - TBIAutoPlannerSettings.MinFieldOverlap));
                    if (NumberofIsocenters > 3) NumberofIsocenters = NumberofVMATIsocenters = 3;
                    ProvideUIUpdate(100 * ++percentComplete / calcItems);

                }
                //matchline structure is present and not empty
                else
                {
                    calcItems += 2;
                    Structure matchline = StructureTuningHelper.GetStructureFromId("matchline");
                    ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Retrieved matchline structure");
                    //get number of isos for PTV superior to matchplane (always truncate this value to a maximum of 4 isocenters)
                    NumberofVMATIsocenters = (int)Math.Ceiling((pts.Max(p => p.Z) - matchline.CenterPoint.z) / (TBIAutoPlannerSettings.MaxFieldYExtent - TBIAutoPlannerSettings.MinFieldOverlap));
                    if (NumberofVMATIsocenters > 4) NumberofVMATIsocenters = 4;
                    ProvideUIUpdate($"Separation between body z max and matchline center z: {(pts.Max(p => p.Z) - matchline.CenterPoint.z):0.0}");
                    ProvideUIUpdate($"numVAMTIsos calculated as double: {(pts.Max(p => p.Z) - matchline.CenterPoint.z) / (TBIAutoPlannerSettings.MaxFieldYExtent - TBIAutoPlannerSettings.MinFieldOverlap):0.0}");
                    ProvideUIUpdate(100 * ++percentComplete / calcItems);

                    //Only add a second legs iso if the extent of the body is > 40.0 cm
                    ProvideUIUpdate($"Separation between matchline z center and body z min: {matchline.CenterPoint.z - pts.Min(p => p.Z):0.0}");
                    if (matchline.CenterPoint.z - pts.Min(p => p.Z) <= TBIAutoPlannerSettings.MaxFieldYExtent)
                    {
                        ProvideUIUpdate($"Separation between matchline z center and body z min is <= maximum field extent ({TBIAutoPlannerSettings.MaxFieldYExtent})");
                        ProvideUIUpdate($"Only one APPA isocenters is required for coverage");
                        NumberofIsocenters = NumberofVMATIsocenters + 1;
                    }
                    else
                    {
                        ProvideUIUpdate($"Separation between matchline z center and body z min is > maximum field extent ({TBIAutoPlannerSettings.MaxFieldYExtent})");
                        ProvideUIUpdate($"Two APPA isocenters are required for coverage");
                        NumberofIsocenters = NumberofVMATIsocenters + 2;
                    }
                    ProvideUIUpdate(100 * ++percentComplete / calcItems);
                }
            }
            ProvideUIUpdate($"Calculated required number of VMAT Isos: {NumberofVMATIsocenters}");
            ProvideUIUpdate($"Calculated total number of Isos: {NumberofIsocenters}");

            //set isocenter names based on numIsos and numVMATIsos (determined these names from prior cases)
            PlanIsocentersList.Add(new PlanIsocenterModel(_prescriptions.First().PlanId, IsoNameHelper.GetTBIVMATIsoNames(NumberofVMATIsocenters, NumberofIsocenters)));
            if (NumberofIsocenters > NumberofVMATIsocenters)
            {
                if (NumberofIsocenters == NumberofVMATIsocenters + 2)
                {
                    PlanIsocentersList.Add(new PlanIsocenterModel("_upper legs", new IsocenterModel("upper legs", 2, BeamType.APPA)));
                    PlanIsocentersList.Add(new PlanIsocenterModel("_lower legs", new IsocenterModel("lower legs", 2, BeamType.APPA)));
                }
                else
                {
                    PlanIsocentersList.Add(new PlanIsocenterModel("_legs", new IsocenterModel("legs", 2, BeamType.APPA)));
                }
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
        #endregion
    }
}
