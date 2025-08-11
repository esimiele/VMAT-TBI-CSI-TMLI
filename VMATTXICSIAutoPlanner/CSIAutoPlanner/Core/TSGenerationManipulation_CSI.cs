using AutoPlannerHelpers.BaseCore;
using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using CSIAutoPlanner.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace CSIAutoPlanner.Core
{
    internal class TSGenerationManipulation_CSI : TSGenerationManipulationBase
    {
        #region properties
        public List<TSTargetCropOverlapModel> TargetCropOverlapManipulations { get; private set; } = new List<TSTargetCropOverlapModel> { };
        //plan id, normalization volume
        public List<TSRingStructureModel> AddedRings { get; private set; } = new List<TSRingStructureModel> { };
        #endregion

        #region fields
        //plan id, structure id, num fx, dose per fx, cumulative dose
        private List<TSRingStructureModel> _requestedRings;
        //plan id, normalization volume
        //structure id of oars requested for crop/overlap eval with targets
        private List<string> _cropAndOverlapStructures = new List<string> { };
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="list"></param>
        /// <param name="tgtRings"></param>
        /// <param name="presc"></param>
        /// <param name="cropStructs"></param>
        public TSGenerationManipulation_CSI(List<SpecialOptimizationStructureModel> specialOptStructs,
                                            List<StructureOperationModel> ops, 
                                            List<TSRingStructureModel> tgtRings,
                                            List<PrescriptionModel> presc, 
                                            List<string> cropStructs)
        {
            _specialOptimizationStructures = specialOptStructs;
            _requestedRings = new List<TSRingStructureModel>(tgtRings);
            _structureOperations = new List<StructureOperationModel>(ops);
            _prescriptions = new List<PrescriptionModel>(presc);
            _cropAndOverlapStructures = new List<string>(cropStructs);
            SetCloseOnFinish(CSIAutoPlannerSettings.CloseProgressWindowOnFinish, 3000);
        }

        #region Preliminary Checks
        /// <summary>
        /// Preliminary checks to ensure body exists and is contoured, user origin is inside body, and spinal cord structure exists
        /// </summary>
        /// <returns></returns>
        protected override bool PreliminaryChecks()
        {
            UpdateUILabel("Performing Preliminary Checks: ");
            int calcItems = 3;
            int counter = 0;

            if (!StructureTuningHelper.DoesStructureExistInSS("body", true))
            {
                ProvideUIUpdate("Error! Body structure not found or is empty! Exiting", true);
                return true;
            }
            ProvideUIUpdate(100 * ++counter / calcItems, "Body structure found and is contoured");

            //check if user origin was set
            if (IsUserOriginInsideBody()) return true;
            ProvideUIUpdate(100 * ++counter / calcItems, "User origin is inside body");

            //only need spinal cord to determine number of spine isocenters. Otherwise, just need target structures for this class
            if (!StructureTuningHelper.DoesStructureExistInSS(new List<string> { "spinalcord", "spinal_cord" }, true))
            {
                ProvideUIUpdate("Missing brain and/or spine structures! Please add and try again!", true);
                return true;
            }

            ProvideUIUpdate(100 * ++counter / calcItems, "Brain and spinal cord structures exist");
            ProvideUIUpdate(100, "Preliminary checks complete!");
            ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");
            return false;
        }
        #endregion

        #region TS Structure Creation and Manipulation
        /// <summary>
        /// Custom method to create a ring structure on a give CT slice. This method is used in the generation of TS_Eyes and TS_Lenses to avoid
        /// using the built-in methods of structure manipulation provided by the API (slow and prone to memory errors)
        /// </summary>
        /// <param name="target"></param>
        /// <param name="normal"></param>
        /// <param name="addedStructure"></param>
        /// <param name="margin"></param>
        /// <param name="thickness"></param>
        /// <returns></returns>
        private (bool, StringBuilder) ContourPartialRing(Structure target, Structure normal, Structure addedStructure, double margin, double thickness)
        {
            StringBuilder sb = new StringBuilder();
            bool fail = false;
            ProvideUIUpdate($"Contouring partial ring to generate {addedStructure.Id}");
            int percentComplete = 0;
            int calcItems = 1;
            //get the start and stop image planes for this structure (+/- 5 slices)
            int startSlice = CalculationHelper.ComputeSlice(normal.MeshGeometry.Positions.Min(p => p.Z), EclipseContext.GetInstance().StructureSet.Image.Origin.z, EclipseContext.GetInstance().StructureSet.Image.ZRes) - 5;
            int stopSlice = CalculationHelper.ComputeSlice(normal.MeshGeometry.Positions.Max(p => p.Z), EclipseContext.GetInstance().StructureSet.Image.Origin.z, EclipseContext.GetInstance().StructureSet.Image.ZRes) + 5;
            calcItems += stopSlice - startSlice + 1;
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Number of slices to contour: {stopSlice - startSlice + 1}");
            if (addedStructure.CanEditSegmentVolume(out string error))
            {
                for (int slice = startSlice; slice <= stopSlice; slice++)
                {
                    ProvideUIUpdate(100 * ++percentComplete / calcItems);
                    //get the target contour points on this CT slice
                    VVector[][] points = target.GetContoursOnImagePlane(slice);
                    //Generate contour points for partial ring from target points + supplied margin + thickness
                    addedStructure.AddContourOnImagePlane(ContourHelper.GenerateContourPoints(points[0], (margin + thickness) * 10), slice);
                    //Subtract contour points for partial ring from target points + supplied margin
                    addedStructure.SubtractContourOnImagePlane(ContourHelper.GenerateContourPoints(points[0], margin * 10), slice);
                }
            }
            else
            {
                ProvideUIUpdate($"Could not create partial ring for {addedStructure.Id} because: {error}");
                fail = true;
            }
            return (fail, sb);
        }

        /// <summary>
        /// Method to generate TS Eyes and TS Lenses per Nataliya's instructions
        /// </summary>
        /// <param name="addedStructure"></param>
        /// <returns></returns>
        private bool GenerateTSGlobesLenses(Structure addedStructure)
        {
            int counter = 0;
            int calcItems = 4;

            string addedStructureId = addedStructure.Id;
            string normalId;
            double thickness;
            double margin;
            if (addedStructureId.ToLower().Contains("eyes"))
            {
                //TS_eyes
                normalId = "Eyes";
                //margin in cm. 
                margin = 1.0;
                thickness = 2.0;
            }
            else
            {
                //TS_Lenses
                normalId = "Lenses";
                margin = 0.7;
                thickness = 2.0;
            }

            //grab the highest Rx target for the initial CSI plan (should be PTV_CSI)
            //6/11/23 THIS CODE WILL NEED TO BE MODIFIED FOR SIB PLANS
            string initTargetId = TargetsHelper.GetHighestRxTargetIdForPlan(_prescriptions, _prescriptions.First().PlanId);

            if (!StructureTuningHelper.DoesStructureExistInSS(initTargetId, true))
            {
                ProvideUIUpdate(100 * ++counter / calcItems, $"Failed to retrieve {initTargetId} to generate partial ring! Exiting!", true);
                return true;
            }
            Structure targetStructure = StructureTuningHelper.GetStructureFromId(initTargetId);
            ProvideUIUpdate(100 * ++counter / calcItems, $"Retrieved initial plan target: {targetStructure.Id}");

            if (StructureTuningHelper.DoesStructureExistInSS(normalId, true))
            {
                Structure normal = StructureTuningHelper.GetStructureFromId(normalId);
                ProvideUIUpdate(100 * ++counter / calcItems, $"Retrieved structure: {normal.Id}");
                ProvideUIUpdate($"Generating ring {addedStructureId} for target {targetStructure.Id}");

                (bool partialRingFail, StringBuilder partialRingErrorMessage) = ContourPartialRing(targetStructure, normal, addedStructure, margin, thickness);
                if (partialRingFail)
                {
                    StrackTraceError = partialRingErrorMessage.ToString();
                    return true;
                }
                ProvideUIUpdate(100 * ++counter / calcItems, $"Finished contouring ring: {addedStructureId}");

                if (normal.IsHighResolution)
                {
                    ProvideUIUpdate($"Normal structure ({normal.Id}) is high resolution. Attempting to convert {addedStructureId} to high resolution");
                    if (addedStructure.CanConvertToHighResolution())
                    {
                        addedStructure.ConvertToHighResolution();
                        ProvideUIUpdate($"Converted {addedStructureId} to high resolution");
                    }
                    else
                    {
                        ProvideUIUpdate($"Error! Could not convert {addedStructureId} to high resolution! Exiting!", true);
                        return true;
                    }
                }

                ProvideUIUpdate($"Contouring overlap between ring and {normalId}");
                addedStructure.SegmentVolume = ContourHelper.ContourIntersection(normal, addedStructure, new StructureMarginModel(0.0), new StructureMarginModel(0.0));
                ProvideUIUpdate(100 * ++counter / calcItems, "Overlap Contoured!");

                if (CheckTSGlobesLensesStructureIntegrity(addedStructure)) return true;
                ProvideUIUpdate($"Finished contouring: {addedStructureId}");
            }
            else ProvideUIUpdate($"Warning! Could not retrieve normal structure! Skipping {addedStructureId}");
            return false;
        }

        /// <summary>
        /// Helper method to verify the integrity of ts eyes/lenses following contouring. Checks if the resulting structure is empty & 
        /// volume <= 0.1cc. If either or true, the structure is removed from the structure set
        /// </summary>
        /// <param name="addedStructure"></param>
        /// <returns></returns>
        private bool CheckTSGlobesLensesStructureIntegrity(Structure addedStructure)
        {
            bool removalRequired = false;
            if (addedStructure.IsEmpty)
            {
                ProvideUIUpdate($"{addedStructure.Id} is empty!");
                removalRequired = true;
            }
            else if (addedStructure.Volume <= 0.1)
            {
                ProvideUIUpdate($"{addedStructure.Id} volume <= 0.1 cc!");
                removalRequired = true;
            }
            if (removalRequired)
            {
                if (EclipseContext.GetInstance().StructureSet.CanRemoveStructure(addedStructure))
                {
                    ProvideUIUpdate($"Removing structure: {addedStructure.Id}");
                    EclipseContext.GetInstance().StructureSet.RemoveStructure(addedStructure);
                }
                else
                {
                    ProvideUIUpdate($"Error! Unable to remove {addedStructure.Id}! Exiting", true);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Method to create/generate the requested tuning structures
        /// </summary>
        /// <returns></returns>
        protected override bool CreateSpecialOptimizationStructures()
        {
            UpdateUILabel("Create TS Structures:");
            ProvideUIUpdate("Adding remaining tuning structures to stack!");
            //get all TS structures that do not contain 'ctv' or 'ptv' in the title
            List<SpecialOptimizationStructureModel> remainingTS = _specialOptimizationStructures.Where(x => !x.StructureId.ToLower().Contains("ctv") && !x.StructureId.ToLower().Contains("ptv")).ToList();

            ProvideUIUpdate(100, "Finished adding tuning structures!");
            ProvideUIUpdate(0, "Contouring tuning structures!");
            //now contour the various structures
            foreach (SpecialOptimizationStructureModel itr in _specialOptimizationStructures)
            {
                ProvideUIUpdate(0, $"Contouring TS: {itr.StructureId}");
                Structure addedStructure = StructureTuningHelper.GetStructureFromId(itr.StructureId,true, itr.DICOMType);
                if (itr.StructureId.ToLower().Contains("ts_eyes") || itr.StructureId.ToLower().Contains("ts_lenses"))
                {
                    if (GenerateTSGlobesLenses(addedStructure)) return true;
                }
                else if (itr.StructureId.ToLower().Contains("armsavoid"))
                {
                    if (CreateArmsAvoid(addedStructure)) return true;
                }
                else
                {
                    ProvideUIUpdate($"The requested tuning structure generation operation is not recognized: {itr}. Skipping!");
                }
            }
            ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");
            return false;
        }

        /// <summary>
        /// Helper method to create ts_armsavoid
        /// </summary>
        /// <param name="armsAvoid"></param>
        /// <returns></returns>
        protected bool CreateArmsAvoid(Structure armsAvoid)
        {
            ProvideUIUpdate("Preparing to contour TS_arms...");
            //generate arms avoid structures
            //need lungs, body, and ptv spine structures
            if (!StructureTuningHelper.DoesStructureExistInSS("lungs", true) || !StructureTuningHelper.DoesStructureExistInSS("body", true))
            {
                ProvideUIUpdate("Error! Body and/or lungs structures were not found or are empty! Exiting!", true);
                return true;
            }
            Structure lungs = StructureTuningHelper.GetStructureFromId("lungs");
            Structure body = StructureTuningHelper.GetStructureFromId("body");

            //get longest target for initial plan (first item in gettargetlistforeachplan should be the plan,list of targets for initial plan)
            (bool fail, Structure initPlanTarget, double length, StringBuilder errorMessage) = TargetsHelper.GetLongestTargetInPlan(TargetsHelper.GetTargetListForEachPlan(_prescriptions).First(), EclipseContext.GetInstance().StructureSet);
            if (fail)
            {
                ProvideUIUpdate(errorMessage.ToString(), true);
                return true;
            }
            //get most inferior slice of ptv_csi (mesgeometry.bounds.z indicates the most inferior part of a structure)
            int startSlice = CalculationHelper.ComputeSlice(initPlanTarget.MeshGeometry.Positions.Min(p => p.Z), EclipseContext.GetInstance().StructureSet.Image.Origin.z, EclipseContext.GetInstance().StructureSet.Image.ZRes);
            //only go to the most superior part of the lungs for contouring the arms
            int stopSlice = CalculationHelper.ComputeSlice(lungs.MeshGeometry.Positions.Max(p => p.Z), EclipseContext.GetInstance().StructureSet.Image.Origin.z, EclipseContext.GetInstance().StructureSet.Image.ZRes);

            //generate two dummy structures (L and R)
            Structure dummyBoxL = StructureTuningHelper.GetStructureFromId("DummyBoxL", true);
            Structure dummyBoxR = StructureTuningHelper.GetStructureFromId("DummyBoxR", true);

            //use the center point of the lungs as the y axis anchor
            //extend box in y direction +/- 20 cm
            double yMax = lungs.CenterPoint.y + 200.0;
            double yMin = lungs.CenterPoint.y - 200.0;
            //set box width in lateral direction
            double boxXWidth = 50.0;

            ProvideUIUpdate($"Number of image slices to contour: {stopSlice - startSlice + 1}");
            ProvideUIUpdate("Preparation complete!");
            ProvideUIUpdate("Contouring TS_arms now...");
            int calcItems = stopSlice - startSlice + 4;
            int counter = 0;

            for (int slice = startSlice; slice <= stopSlice; slice++)
            {
                //get body contour points
                VVector[][] bodyPts = body.GetContoursOnImagePlane(slice);
                double xMax = -500000000000.0;
                double xMin = 500000000000.0;
                //find min and max x positions for the body on this slice (so we can adapt the box positions for each slice)
                for (int i = 0; i < bodyPts.GetLength(0); i++)
                {
                    xMax = Math.Max(bodyPts[i].Max(p => p.x), xMax);
                    xMin = Math.Min(bodyPts[i].Min(p => p.x), xMin);
                }

                //box with contour points located at (x,y), (x,0), (x,-y), (0,-y), (-x,-y), (-x,0), (-x, y), (0,y)
                VVector[] ptsL = new[] {
                                        new VVector(xMax, yMax, 0),
                                        new VVector(xMax, 0, 0),
                                        new VVector(xMax, yMin, 0),
                                        new VVector(0, yMin, 0),
                                        new VVector(xMax-boxXWidth, yMin, 0),
                                        new VVector(xMax-boxXWidth, 0, 0),
                                        new VVector(xMax-boxXWidth, yMax, 0),
                                        new VVector(0, yMax, 0)};

                VVector[] ptsR = new[] {
                                        new VVector(xMin + boxXWidth, yMax, 0),
                                        new VVector(xMin + boxXWidth, 0, 0),
                                        new VVector(xMin + boxXWidth, yMin, 0),
                                        new VVector(0, yMin, 0),
                                        new VVector(xMin, yMin, 0),
                                        new VVector(xMin, 0, 0),
                                        new VVector(xMin, yMax, 0),
                                        new VVector(0, yMax, 0)};

                //added in case structures are existing and need to be removed (shouldn't be an issue if they are already null)
                dummyBoxL.ClearAllContoursOnImagePlane(slice);
                dummyBoxR.ClearAllContoursOnImagePlane(slice);
                //add contours on this slice
                dummyBoxL.AddContourOnImagePlane(ptsL, slice);
                dummyBoxR.AddContourOnImagePlane(ptsR, slice);
                ProvideUIUpdate(100 * ++counter / calcItems);
            }

            ProvideUIUpdate(100 * ++counter / calcItems, "Unioning left and right arms avoid structures together!");
            //now contour the arms avoid structure as the union of the left and right dummy boxes
            armsAvoid.SegmentVolume = ContourHelper.ContourUnion(dummyBoxL, dummyBoxR, new StructureMarginModel(0), new StructureMarginModel(0));
            if (ReferenceEquals(armsAvoid, null) || armsAvoid.IsEmpty) return true;

            ProvideUIUpdate(100 * ++counter / calcItems, "Contouring overlap between arms avoid and body with 5mm outer margin!");
            //contour the arms as the overlap between the current armsAvoid structure and the body with a 5mm outer margin
            if (ContourHelper.CropStructureFromBody(armsAvoid.Id, 0.5, UIUD)) return true;

            ProvideUIUpdate(100 * ++counter / calcItems, "Cleaning up!");
            EclipseContext.GetInstance().StructureSet.RemoveStructure(dummyBoxR);
            EclipseContext.GetInstance().StructureSet.RemoveStructure(dummyBoxL);
            ProvideUIUpdate(100, "Finished contouring arms avoid!");
            return false;
        }

        /// <summary>
        /// Method to perform tuning structure manipulations
        /// </summary>
        /// <returns></returns>
        protected override bool PerformStructureDerivations()
        {
            UpdateUILabel("Contouring opt structures now:");
            string tmpPlanId = _prescriptions.First().PlanId;
            List<TargetModel> tmpTSTargetList = new List<TargetModel> { };
            string prevTargetId = "";
            //prescriptions are inherently sorted by increasing cumulative Rx to targets
            foreach (PrescriptionModel itr in _prescriptions)
            {
                if (!string.Equals(itr.PlanId, tmpPlanId))
                {
                    //new plan
                    PlanTargets.Add(new PlanTargetsModel(tmpPlanId, new List<TargetModel>(tmpTSTargetList)));
                    //last target id represents highest Rx target for previous plan
                    NormalizationVolumes.Add(tmpPlanId, prevTargetId);
                    tmpTSTargetList = new List<TargetModel> { };
                    tmpPlanId = itr.PlanId;
                }
                //create a new TS target for optimization and copy the original target structure onto the new TS structure
                Structure addedTSTarget = GetTSTarget(itr.TargetId);
                prevTargetId = addedTSTarget.Id;
                tmpTSTargetList.Add(new TargetModel(itr.TargetId, itr.CumulativeDoseToTarget, addedTSTarget.Id));

                //ensure the target is cropped 3mm from body
                ProvideUIUpdate($"Cropping TS target from body with {3.0} mm inner margin");
                if (ContourHelper.CropStructureFromBody(addedTSTarget.Id, -0.3, UIUD)) return true;
            }

            if (ContourHelper.ExecuteStructureOperations(_structureOperations, PUUD, UIUD)) return true;

            //iterated through entire prescription list, need to add final values to normVolumes and tsTargets
            NormalizationVolumes.Add(tmpPlanId, prevTargetId);
            PlanTargets.Add(new PlanTargetsModel(tmpPlanId, new List<TargetModel>(tmpTSTargetList)));

            ProvideUIUpdate("Finished performing optimization structure generation");
            ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");

            return false;
        }

        /// <summary>
        /// Helper method to check each structure in the crop/overlap list to ensure each structure actually overlaps with each target.
        /// If a structure does not overlap with all targets, remove that structure from the crop/overlap list
        /// </summary>
        /// <returns></returns>
        private bool CheckAllRequestedTargetCropAndOverlapManipulations()
        {
            List<string> structuresToRemove = new List<string> { };
            List<string> highResStructuresToReplace = new List<string> { };
            Dictionary<string, string> tgts = TargetsHelper.GetHighestRxPlanTargetList(_prescriptions);
            int percentCompletion = 0;
            int calcItems = ((1 + 2 * tgts.Count) * _cropAndOverlapStructures.Count) + 1;
            ProvideUIUpdate(100 * ++percentCompletion / calcItems, "Retrieved plan-target list");
            foreach (string itr in _cropAndOverlapStructures)
            {
                Structure normal = StructureTuningHelper.GetStructureFromId(itr);
                if (normal != null)
                {
                    ProvideUIUpdate(100 * ++percentCompletion / calcItems, $"Retrieved normal structure: {normal.Id}");
                    //verify structures requested for cropping target from structure actually overlap with structure
                    if (!DoesStructureOverlapWithAllTargets(normal, tgts))
                    {
                        //structure does not overlap with all targets
                        ProvideUIUpdate("Removing from TS manipulation list!");
                        structuresToRemove.Add(itr);
                    }
                    else
                    {
                        //structure does overlap with all targets. Need to check if structure is high resolution
                        if (normal.IsHighResolution)
                        {
                            if(!_highResStructureConversions.Any(x => string.Equals(x.Key, itr)))
                            {
                                ProvideUIUpdate($"Structure {normal.Id} is high resolution. Converting to low resolution now");
                                //get the high res structure mesh geometry
                                MeshGeometry3D mesh = normal.MeshGeometry;
                                //get the start and stop image planes for this structure
                                int startSlice = CalculationHelper.ComputeSlice(mesh.Positions.Min(p => p.Z), EclipseContext.GetInstance().StructureSet.Image.Origin.z, EclipseContext.GetInstance().StructureSet.Image.ZRes);
                                int stopSlice = CalculationHelper.ComputeSlice(mesh.Positions.Max(p => p.Z), EclipseContext.GetInstance().StructureSet.Image.Origin.z, EclipseContext.GetInstance().StructureSet.Image.ZRes);

                                //create an Id for the low resolution struture that will be created. The name will be '_lowRes' appended to the current structure Id
                                (bool fail, Structure lowRes) = CreateLowResStructure(normal);
                                if (fail) return true;
                                ProvideUIUpdate($"Contouring {lowRes.Id} now");

                                ContourLowResStructure(normal, lowRes, startSlice, stopSlice);
                                _highResStructureConversions.Add(itr, lowRes.Id);
                            }
                            highResStructuresToReplace.Add(itr);
                        }
                    }
                }
                else
                {
                    ProvideUIUpdate($"Warning! Could not retrieve structure: {itr}! Skipping and removing from list!");
                    structuresToRemove.Add(itr);
                }
            }

            if (structuresToRemove.Any()) RemoveStructuresFromCropOverlapList(structuresToRemove);
            if (highResStructuresToReplace.Any()) UpdateCropOverlapManipulationList(highResStructuresToReplace);
            ProvideUIUpdate(100, "Removed missing structures or normals that do not overlap with all targets from crop/overlap list");
            return false;
        }

        private void UpdateCropOverlapManipulationList(IEnumerable<string> highResStructureList)
        {
            foreach(string itr in highResStructureList)
            {
                ProvideUIUpdate($"Updating crop overlap manipulation list to replace {itr} with {_highResStructureConversions.First(x => string.Equals(x.Key, itr)).Value}");
                int index = _cropAndOverlapStructures.IndexOf(itr);
                _cropAndOverlapStructures.RemoveAt(index);
                _cropAndOverlapStructures.Insert(index, _highResStructureConversions.First(x => string.Equals(x.Key, itr)).Value);
            }
        }

        /// <summary>
        /// Helper method to check if the supplied normal structure overlaps with all targets listed in the prescriptions
        /// </summary>
        /// <param name="normal"></param>
        /// <param name="tgts"></param>
        /// <returns></returns>
        private bool DoesStructureOverlapWithAllTargets(Structure normal, Dictionary<string, string> tgts)
        {
            int percentComplete = 0;
            int calcItems = 2;
            foreach (KeyValuePair<string, string> itr1 in tgts)
            {
                Structure target = StructureTuningHelper.GetStructureFromId(itr1.Value);
                if (target != null)
                {
                    ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Retrieved target structure: {target.Id}");
                    if (!StructureTuningHelper.IsOverlap(target, normal.MeshGeometry.Positions))
                    {
                        ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Warning! {normal.Id} does not overlap with all plan target ({target.Id}) structures!");
                        return false;
                    }
                    else ProvideUIUpdate(100 * ++percentComplete / calcItems, $"{normal.Id} overlaps with target {target.Id}");
                }
                else ProvideUIUpdate($"Warning! Could not retrieve target: {itr1.Value}! Skipping");
            }
            ProvideUIUpdate($"Normal structure ({normal.Id}) overlaps with all targets");
            return true;
        }

        /// <summary>
        /// Helper method to remove the supplied structure ids from the requested crop/overlap structure list
        /// </summary>
        /// <param name="structuresToRemove"></param>
        private void RemoveStructuresFromCropOverlapList(List<string> structuresToRemove)
        {
            foreach (string itr in structuresToRemove)
            {
                ProvideUIUpdate($"Removing {itr} from crop/overlap list");
                _cropAndOverlapStructures.RemoveAt(_cropAndOverlapStructures.IndexOf(itr));
            }
        }

        /// <summary>
        /// Helper method to create a target crop structure and copy the target contour onto the target crop structure
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private (bool, Structure) CreateCropStructure(Structure target)
        {
            bool fail = false;
            string cropName = $"{target.Id}crop";
            if (cropName.Length > 16) cropName = cropName.Substring(0, 16);
            Structure cropStructure;
            if (!string.Equals(cropName, target.Id))
            {
                cropStructure = AddTSStructures(new SpecialOptimizationStructureModel("CONTROL", cropName));
                if (cropStructure == null)
                {
                    ProvideUIUpdate($"Error! Could not create crop structure: {cropName}! Exiting", true);
                    fail = true;
                    return (fail, null);
                }
                cropStructure.SegmentVolume = target.Margin(0.0);
                ProvideUIUpdate($"Created and contoured crop structure: {cropName}");
            }
            else
            {
                ProvideUIUpdate($"Warning! Ran out of characters for structure Id! Using existing TS target: {target.Id}");
                cropStructure = target;
            }
            return (fail, cropStructure);
        }

        /// <summary>
        /// Helper method to create an empty target overlap structure
        /// </summary>
        /// <param name="target"></param>
        /// <param name="prescriptionCount"></param>
        /// <returns></returns>
        private (bool, Structure) CreateOverlapStructure(Structure target, int prescriptionCount)
        {
            bool fail = false;
            string overlapName = $"{target.Id}over";
            if (overlapName.Length > 16) overlapName = overlapName.Substring(0, 16);
            Structure overlapStructure;
            if (string.Equals(overlapName, target.Id))
            {
                ProvideUIUpdate($"Warning! Ran out of characters for structure Id! Using structure Id: TS_overlap{prescriptionCount}");
                overlapName = $"TS_overlap{prescriptionCount}";
            }
            overlapStructure = AddTSStructures(new SpecialOptimizationStructureModel("CONTROL", overlapName));
            if (overlapStructure == null)
            {
                ProvideUIUpdate($"Error! Could not create overlap structure: {overlapName}! Exiting");
                fail = true;
            }
            else ProvideUIUpdate($"Created overlap structure: {overlapName}");
            return (fail, overlapStructure);
        }

        /// <summary>
        /// Method to perform crop/overlap operation between the prescription targets and supplied list of oar structures
        /// </summary>
        /// <returns></returns>
        private bool CropAndContourOverlapWithTargets()
        {
            //only do this for the highest dose target in each plan!
            UpdateUILabel("Crop/overlap with targets:");
            //evaluate overlap of each structure with each target
            //if structure dose not overlap BOTH targets, remove from structure manipulations list and remove added structure
            ProvideUIUpdate("Evaluating overlap between targets and normal structures requested for target cropping!");
            if (CheckAllRequestedTargetCropAndOverlapManipulations()) return true;

            int percentComplete = 0;
            int calcItems = 1 + (3 + 3 * _cropAndOverlapStructures.Count) * _prescriptions.Count();

            //sort by cumulative Rx to the targets (item 5)
            List<PrescriptionModel> sortedPrescriptions = _prescriptions.OrderBy(x => x.CumulativeDoseToTarget).ToList();
            ProvideUIUpdate(100 * ++percentComplete / calcItems, "Sorted prescriptions by cumulative dose");

            if (_cropAndOverlapStructures.Any())
            {
                //clear the normalization volumes list as this will be updated with the crop/overlap targets
                NormalizationVolumes.Clear();
                for (int i = 0; i < sortedPrescriptions.Count(); i++)
                {
                    string targetId = $"TS_{sortedPrescriptions.ElementAt(i).TargetId}";
                    if (StructureTuningHelper.DoesStructureExistInSS(targetId, true))
                    {
                        Structure target = StructureTuningHelper.GetStructureFromId(targetId);
                        ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Retrieved target: {targetId}");

                        (bool fail, Structure cropStructure) cropResult = CreateCropStructure(target);
                        if (cropResult.fail) return true;
                        ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Added crop structure ({cropResult.Item2.Id}) to stack");

                        (bool fail, Structure overlapStructure) overlapRresult = CreateOverlapStructure(target, i);
                        if (overlapRresult.fail) return true;
                        ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Added overlap structure ({overlapRresult.Item2.Id}) to stack");

                        foreach (string itr in _cropAndOverlapStructures)
                        {
                            if (!StructureTuningHelper.DoesStructureExistInSS(itr, true))
                            {
                                ProvideUIUpdate($"Error! Requested normal for crop/overlap structure ({itr}) is empty or missing from structure set! Please fix and try again!", true);
                                return true;
                            }
                            Structure normal = StructureTuningHelper.GetStructureFromId(itr);
                            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Retrieved normal structure: {normal.Id}");

                            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Contouring overlap between structure ({itr}) and target ({target.Id})");
                            (bool fail, StringBuilder errorMessage) cropAndContourOverlapResult = ContourHelper.ContourOverlapAndUnion(normal, target, overlapRresult.overlapStructure, 0.0);
                            if (cropAndContourOverlapResult.fail)
                            {
                                ProvideUIUpdate(cropAndContourOverlapResult.errorMessage.ToString(), true);
                                return true;
                            }

                            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Cropping structure ({itr}) from target ({target.Id})");
                            cropResult.cropStructure.SegmentVolume = ContourHelper.CropStructureFromStructure(cropResult.cropStructure, normal, new StructureMarginModel(0.0), new StructureMarginModel(0.0));
                        }
                        NormalizationVolumes.Add(sortedPrescriptions.ElementAt(i).PlanId, cropResult.Item2.Id);
                        TargetCropOverlapManipulations.Add(new TSTargetCropOverlapModel(sortedPrescriptions.ElementAt(i).PlanId, target.Id, cropResult.cropStructure.Id, AutoPlannerHelpers.Enums.TSManipulationType.CropTargetFromStructure));
                        TargetCropOverlapManipulations.Add(new TSTargetCropOverlapModel(sortedPrescriptions.ElementAt(i).PlanId, target.Id, overlapRresult.overlapStructure.Id, AutoPlannerHelpers.Enums.TSManipulationType.ContourOverlapWithTarget));
                    }
                    else ProvideUIUpdate($"Could not retrieve ts target: {targetId}");
                }
            }
            else ProvideUIUpdate(100, "No structures remaining to crop and contour overlap with structures! Skipping!");
            ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");
            return false;
        }
        #endregion

        protected override bool PerformPlanSpecificStructureDerivations()
        {
            if (_cropAndOverlapStructures.Any())
            {
                if (CropAndContourOverlapWithTargets()) return true;
            }
            if (_requestedRings.Any())
            {
                AddedRings = new List<TSRingStructureModel>(GenerateRings(_requestedRings));
                if (!AddedRings.Any()) return true;
            }
            if (RegeneratePTVBrainSpine()) return true;
            return false;
        }

        #region Recontour the brain spine targets
        /// <summary>
        /// Helper method to take the approved PTV_CSI target (or the highest Rx target for the initial plan) and use its contour points
        /// to re-contour _Brain and _Spine
        /// </summary>
        /// <returns></returns>
        private bool RegeneratePTVBrainSpine()
        {
            UpdateUILabel("Generating _Spine/_Brain:");
            ProvideUIUpdate("Generating _Spine/_Brain:");
            int percentComplete = 0;
            int calcItems = 9;

            Structure ptvBrain = StructureTuningHelper.GetStructureFromId("_Brain", true);
            Structure ptvSpine = StructureTuningHelper.GetStructureFromId("_Spine", true);
            if (ptvBrain == null || ptvSpine == null)
            {
                ProvideUIUpdate($"Error! _Brain or _Spine are null! Fix and try again!", true);
                return true;
            }
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Retrieved structure: {ptvBrain.Id}");
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Retrieved structure: {ptvSpine.Id}");
            if (!ptvSpine.IsEmpty) ClearContourPointsFromAllPlanes(ptvSpine);
            if (!ptvBrain.IsEmpty) ClearContourPointsFromAllPlanes(ptvBrain);
            ProvideUIUpdate($"Cleared all contour points for {ptvBrain.Id} and {ptvSpine.Id}");

            if (ptvBrain.ApprovalHistory.First().ApprovalStatus == StructureApprovalStatus.Approved || ptvSpine.ApprovalHistory.First().ApprovalStatus == StructureApprovalStatus.Approved)
            {
                ProvideUIUpdate($"Error! _Brain or _Spine are approved and I can't modify them! Fix and try again!", true);
                return true;
            }
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Verified approval status of {ptvBrain.Id} and {ptvSpine.Id}");

            int cutSlice = -1;
            (bool fail, double cutPos) = GetCutSliceZPosition();
            if (fail) return true;
            ProvideUIUpdate(100 * ++percentComplete / calcItems);

            cutSlice = CalculationHelper.ComputeSlice(cutPos, EclipseContext.GetInstance().StructureSet.Image.Origin.z, EclipseContext.GetInstance().StructureSet.Image.ZRes);
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Z cut slice: {cutSlice}");

            Structure csiInitTarget = StructureTuningHelper.GetStructureFromId(TargetsHelper.GetHighestRxTargetIdForPlan(_prescriptions, _prescriptions.First().PlanId));
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Retrieved structure: {csiInitTarget.Id}");

            //stop slice for ptv spine is the cut plane
            ContourStructure(ptvSpine, csiInitTarget, CalculationHelper.ComputeSlice(csiInitTarget.MeshGeometry.Positions.Min(p => p.Z), EclipseContext.GetInstance().StructureSet.Image.Origin.z, EclipseContext.GetInstance().StructureSet.Image.ZRes), cutSlice);
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Contoured structure: {ptvSpine.Id}");

            //start slice for ptv brain is the cut plane
            ContourStructure(ptvBrain, csiInitTarget, cutSlice, CalculationHelper.ComputeSlice(csiInitTarget.MeshGeometry.Positions.Max(p => p.Z), EclipseContext.GetInstance().StructureSet.Image.Origin.z, EclipseContext.GetInstance().StructureSet.Image.ZRes));
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Contoured structure: {ptvBrain.Id}");
            return false;
        }

        /// <summary>
        /// Helper method to determine the z position of the cut plane that should be used to split the initial csi target into _Brain
        /// and _Spine. Either min z of brain or max z of spinal cord will work
        /// </summary>
        /// <returns></returns>
        private (bool, double) GetCutSliceZPosition()
        {
            Structure cutStructure = StructureTuningHelper.GetStructureFromId("brain");
            double cutPos = 0.0;
            if (cutStructure == null || cutStructure.IsEmpty)
            {
                cutStructure = StructureTuningHelper.GetStructureFromId("spinal_cord");
                if (cutStructure == null) cutStructure = StructureTuningHelper.GetStructureFromId("spinalcord");
                if (cutStructure == null || cutStructure.IsEmpty)
                {
                    //give up
                    ProvideUIUpdate($"Error! Brain/Spinal cord structures are null or empty! Fix and try again!", true);
                    return (true, 0.0);
                }
                else cutPos = cutStructure.MeshGeometry.Positions.Max(p => p.Z);
            }
            else cutPos = cutStructure.MeshGeometry.Positions.Min(p => p.Z);
            ProvideUIUpdate($"Retrieved structure used to determine cut plan: {cutStructure.Id}");
            VVector origin = EclipseContext.GetInstance().StructureSet.Image.Origin;
            ProvideUIUpdate($"Dicom origin ({origin.x:0.0}, {origin.y:0.0}, {origin.z:0.0}) mm");
            ProvideUIUpdate($"Image z resolution: {EclipseContext.GetInstance().StructureSet.Image.ZRes:0.0} mm");
            ProvideUIUpdate($"Number of z slices: {EclipseContext.GetInstance().StructureSet.Image.ZSize}");
            ProvideUIUpdate($"Z cut position: {cutPos:0.0} mm");
            return (false, cutPos);
        }

        /// <summary>
        /// Helper method to clear all contour points from all image planes for the supplied structure
        /// </summary>
        /// <param name="structToRemove"></param>
        /// <returns></returns>
        private bool ClearContourPointsFromAllPlanes(Structure structToRemove)
        {
            ProvideUIUpdate($"Removing structure: {structToRemove.Id}");
            int startSlice = CalculationHelper.ComputeSlice(structToRemove.MeshGeometry.Positions.Min(p => p.Z), EclipseContext.GetInstance().StructureSet.Image.Origin.z, EclipseContext.GetInstance().StructureSet.Image.ZRes);
            int stopSlice = CalculationHelper.ComputeSlice(structToRemove.MeshGeometry.Positions.Max(p => p.Z), EclipseContext.GetInstance().StructureSet.Image.Origin.z, EclipseContext.GetInstance().StructureSet.Image.ZRes);
            ProvideUIUpdate($"Start slice: {startSlice}");
            ProvideUIUpdate($"Stop slice: {stopSlice}");
            int percentComplete = 0;
            int calcItems = stopSlice - startSlice + 1;
            for (int slice = startSlice; slice <= stopSlice; slice++)
            {
                ProvideUIUpdate(100 * ++percentComplete / calcItems);
                if (structToRemove.GetContoursOnImagePlane(slice).Any()) structToRemove.ClearAllContoursOnImagePlane(slice);
            }
            return false;
        }

        /// <summary>
        /// Helper method to copy the contour points from the supplied base structure onto the structure to contour
        /// </summary>
        /// <param name="structToContour"></param>
        /// <param name="baseStructure"></param>
        /// <param name="startSlice"></param>
        /// <param name="stopSlice"></param>
        /// <returns></returns>
        private bool ContourStructure(Structure structToContour, Structure baseStructure, int startSlice, int stopSlice)
        {
            ProvideUIUpdate($"Contouring structure: {structToContour.Id}");
            ProvideUIUpdate($"Base structure: {baseStructure.Id}");
            ProvideUIUpdate($"Start slice: {startSlice}");
            ProvideUIUpdate($"Stop slice: {stopSlice}");
            int percentComplete = 0;
            int calcItems = stopSlice - startSlice + 1;
            for (int slice = startSlice; slice <= stopSlice; slice++)
            {
                ProvideUIUpdate(100 * ++percentComplete / calcItems);
                VVector[][] pts = baseStructure.GetContoursOnImagePlane(slice);
                if (pts.Any())
                {
                    for (int i = 0; i < pts.GetLength(0); i++)
                    {
                        if (structToContour.IsPointInsideSegment(pts[i][0]) || structToContour.IsPointInsideSegment(pts[i][pts[i].GetLength(0) - 1]))
                        {
                            structToContour.SubtractContourOnImagePlane(pts[i], slice);
                        }
                        else structToContour.AddContourOnImagePlane(pts[i], slice);
                    }
                }
            }
            return false;
        }
        #endregion

        #region Isocenter Calculation
        /// <summary>
        /// Method to calculate the required number of vmat isocenters for each plan
        /// </summary>
        /// <returns></returns>
        protected override bool CalculateNumIsos()
        {
            UpdateUILabel("Calculating Number of Isocenters:");
            int calcItems = 1;
            int counter = 0;

            //For these cases the maximum number of allowed isocenters is 3. One isocenter is reserved for the brain and either one or two isocenters are used for the spine (depending on length).
            //revised to get the number of unique plans list, for each unique plan, find the target with the greatest z-extent and determine the number of isocenters based off that target. 
            //plan Id, list of targets assigned to that plan

            List<PlanTargetsModel> planIdTargets = new List<PlanTargetsModel>(TargetsHelper.GetTargetListForEachPlan(_prescriptions));
            ProvideUIUpdate(100 * ++counter / calcItems, "Generated list of plans each containing list of targets");

            foreach (PlanTargetsModel itr in planIdTargets)
            {
                calcItems = itr.Targets.Count;
                counter = 0;
                //determine for each plan which target has the greatest z-extent
                (bool fail, Structure longestTargetInPlan, double maxTargetLength, StringBuilder errorMessage) = TargetsHelper.GetLongestTargetInPlan(itr, EclipseContext.GetInstance().StructureSet);
                if (fail)
                {
                    ProvideUIUpdate($"Error! No structure named: {errorMessage} found or contoured!", true);
                    return true;
                }
                ProvideUIUpdate($"Determined target with greatest extent: {longestTargetInPlan.Id}, Plan: {itr.PlanId}");

                counter = 0;
                calcItems = 3;

                //Minimum requested field overlap.
                double minFieldOverlap = 50.0;
                double maxFieldExtent = 400.0;
                //subtract 50 mm from the numerator as the brain fields have a 50 mm inferior margin on the _Brain 
                double brainInfMargin = 50.0;

                //If the target ID is PTV_CSI, calculate the number of isocenters based on _Spine and add one iso for the brain
                //planId, target list
                if (string.Equals(longestTargetInPlan.Id, TargetsHelper.GetHighestRxTargetIdForPlan(_prescriptions, _prescriptions.First().PlanId)))
                {
                    calcItems += 1;
                    //special rules for initial plan,
                    //first, determine the number of isocenters required to treat _Spine
                    //Grab extent of _Spine and add a 2 cm margin to this distance to give 2 cm buffer on the sup portion of the target to ensure adequate coverage/overlap between upper spine field and brain fields
                    (bool isFail, double spineTargetExtent) = GetSpineTargetExtent(2.0);
                    if (isFail) return true;
                    ProvideUIUpdate(100 * ++counter / calcItems);

                    NumberofVMATIsocenters = CalculateNumberofVMATIsocentersForPTVCSI(spineTargetExtent, brainInfMargin, maxFieldExtent, minFieldOverlap);
                    ProvideUIUpdate(100 * ++counter / calcItems, $"Final calculated number of VMAT isocenters: {NumberofVMATIsocenters}");
                }
                else
                {
                    NumberofVMATIsocenters = (int)Math.Ceiling(maxTargetLength / (maxFieldExtent - minFieldOverlap));
                    ProvideUIUpdate(100 * ++counter / calcItems, $"{NumberofVMATIsocenters}");
                }
                if (NumberofVMATIsocenters > 3) NumberofVMATIsocenters = 3;

                //set isocenter names based on numIsos and NumberofVMATIsocenters (be sure to pass 'true' for the third argument to indicate that this is a CSI plan(s))
                //plan Id, list of isocenter names for this plan
                PlanIsocentersList.Add(new PlanIsocenterModel(itr.PlanId, IsoNameHelper.GetCSIIsoNames(NumberofVMATIsocenters)));
                ProvideUIUpdate(100 * ++counter / calcItems, "Added isocenter to stack!");
            }
            ProvideUIUpdate($"Required Number of Isocenters: {NumberofVMATIsocenters}");
            ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");
            return false;
        }

        /// <summary>
        /// Helper method to calculate the number of vmat iso
        /// </summary>
        /// <param name="spineTargetExtent"></param>
        /// <param name="brainInfMargin"></param>
        /// <param name="maxFieldExtent"></param>
        /// <param name="minFieldOverlap"></param>
        /// <returns></returns>
        private int CalculateNumberofVMATIsocentersForPTVCSI(double spineTargetExtent, double brainInfMargin, double maxFieldExtent, double minFieldOverlap)
        {
            double NumberofVMATIsocentersAsDouble = (spineTargetExtent - brainInfMargin) / (maxFieldExtent - minFieldOverlap);
            ProvideUIUpdate($"Spine target extent: {spineTargetExtent:0.00}");
            ProvideUIUpdate($"Num VMAT isos as double: {(spineTargetExtent - brainInfMargin) / (maxFieldExtent - minFieldOverlap):0.00}");
            if (NumberofVMATIsocentersAsDouble > 1 && NumberofVMATIsocentersAsDouble % 1 < 0.1)
            {
                ProvideUIUpdate($"Calculated number of vmat isos MOD 1 is < 0.1 (i.e. an extra {0.1 * (maxFieldExtent - minFieldOverlap):0.0} mm of field is required to cover the spine");
                NumberofVMATIsocentersAsDouble = Math.Floor(NumberofVMATIsocentersAsDouble);
                ProvideUIUpdate($"Truncating number of isos to {NumberofVMATIsocentersAsDouble}");
            }
            else NumberofVMATIsocentersAsDouble = Math.Ceiling(NumberofVMATIsocentersAsDouble);
            ProvideUIUpdate($"Adding one additional isocenter for the brain");
            //one iso reserved for _Brain
            return (int)NumberofVMATIsocentersAsDouble + 1;
        }

        /// <summary>
        /// Helper method to calculate the extent of _Spine with a user-supplied additional margin
        /// </summary>
        /// <param name="addedMarginInCm"></param>
        /// <returns></returns>
        private (bool, double) GetSpineTargetExtent(double addedMarginInCm)
        {
            bool fail = false;
            double spineTargetExtent = 0.0;
            if (StructureTuningHelper.DoesStructureExistInSS("_Spine", true))
            {
                Structure spineTarget = StructureTuningHelper.GetStructureFromId("_Spine");
                ProvideUIUpdate("Retrieved spinal cord structure");
                Point3DCollection pts = spineTarget.MeshGeometry.Positions;
                //ESAPI default distances are in mm
                spineTargetExtent = pts.Max(p => p.Z) - pts.Min(p => p.Z) + addedMarginInCm * 10;
            }
            else
            {
                ProvideUIUpdate("Error! No structure named _Spine was found or it was empty!", true);
            }
            return (fail, spineTargetExtent);
        }
        #endregion
    }
}
