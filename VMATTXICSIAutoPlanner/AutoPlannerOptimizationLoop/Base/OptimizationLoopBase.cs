using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.EnumTypeHelpers;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using AutoPlannerOptimizationLoop.DataContainers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using AutoPlannerOptimizationLoop.UIHelpers;
using AutoPlannerOptimizationLoop.Utilities;
using AutoPlannerOptimizationLoop.Models;
using AutoPlannerOptimizationLoop.Helpers;
using AutoPlannerOptimizationLoop.ViewModels;
using AutoPlannerHelpers.Prompts;

namespace AutoPlannerOptimizationLoop.Base
{
    public class OptimizationLoopBase : OptimizationLoopProgressViewModel
    {
        protected OptDataContainer _data;
        protected bool _checkSupportStructures = false;
        protected int overallPercentCompletion = 0;
        protected int overallCalcItems = 1;

        /// <summary>
        /// Simple method to initialize the log file path, the log file name, and the file name for the temporary errors and warnings file
        /// </summary>
        protected void InitializeLogPathAndName()
        {
            string path = _data.LogFilePath + "\\optimization\\" + _data.MRN + "\\";
            string currentDateTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string logFileName = path + currentDateTime + ".txt";
            string errorsWarningsFile = path + currentDateTime + "-EWs" + ".txt";
            InitializeLogFile(path, logFileName, errorsWarningsFile);
            /*
             * prelimiary check
             * coverage check ?
             * num opt
             * per opt TBI --> opt, dose calc, opt, dose calc, norm, eval, update
             * per opt CSI initial --> opt, dose calc, norm, eval, update
             * per opt CSI sequential --> (opt, dose calc, norm, eval, update) x 2
             * additional opt
             */
        }

        #region print run setup, failed message, plan dose info, etc.
        /// <summary>
        /// Helper method to print that either optimization or dose calculation failed and the reason why it failed
        /// </summary>
        /// <param name="optorcalc"></param>
        /// <param name="reason"></param>
        protected void PrintFailedMessage(string optorcalc, string reason = "")
        {
            if (string.IsNullOrEmpty(reason))
            {
                ProvideUIUpdate($"Error! {optorcalc} failed!" + Environment.NewLine + " Try running the {0} manually Eclipse for more information!" + Environment.NewLine + Environment.NewLine + " Exiting!", true);
            }
            else
            {
                ProvideUIUpdate($"Error! {optorcalc} failed because: {reason}" + Environment.NewLine + Environment.NewLine + " Exiting!", true);
            }
        }

        /// <summary>
        /// Simple method to print all of the relevant optimization loop run setup information to the user
        /// </summary>
        protected void PrintRunSetupInfo()
        {
            ProvideUIUpdate(OptimizationLoopUIHelper.GetRunSetupInfoHeader(_data.Plans,
                                                                           _data.PlanType,
                                                                           _data.RunCoverageCheck,
                                                                           _data.NumberOfIterations,
            _data.OneMoreOptimization,
                                                                           _data.CopyAndSaveEachOptimizedPlan,
                                                                           _data.TargetCoverageNormalization));
            ProvideUIUpdate(OptimizationLoopUIHelper.PrintPlanObjectives(_data.PlanObjectives));
            ProvideUIUpdate(OptimizationLoopUIHelper.PrintRequestedTSStructures(_data.RequestedOptimizationTSStructures));
        }
        #endregion

        #region preliminary checks
        /// <summary>
        /// Preliminary checks for the couch structures if they exist. Primarily if support structure contours exist on the first and last slices of the CT image
        /// </summary>
        /// <param name="ss"></param>
        /// <returns></returns>
        protected bool PreliminaryChecksCouch(StructureSet ss)
        {
            int percentComplete = 0;
            int calcItems = 2;

            //grab all couch structures including couch surface, rails, etc. Also grab the matchline and spinning manny couch (might not be present depending on the size of the patient)
            List<Structure> couchAndRails = ss.Structures.Where(x => x.Id.ToLower().Contains("couch") || x.Id.ToLower().Contains("rail")).ToList();
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Retrieved list of couch structures ({couchAndRails.Count} structures found)");

            //check to see if the couch and rail structures are present in the structure set. If not, let the user know as an FYI. At this point, the user can choose to stop the optimization loop and add the couch structures
            if (!couchAndRails.Any())
            {
                ConfirmPrompt CP = new ConfirmPrompt("I didn't found any couch structures in the structure set!" + Environment.NewLine + Environment.NewLine + "Continue?!");
                CP.ShowDialog();
                if (!CP.GetSelection())
                {
                    ProvideUIUpdate("Quitting!", true);
                    return true;
                }
            }

            //now check if the couch and spinning manny structures are present on the first and last slices of the CT image
            if (couchAndRails.Any() && couchAndRails.Any(x => !x.IsEmpty))
            {
                if (couchAndRails.Any(x => x.GetContoursOnImagePlane(0).Any()) || couchAndRails.Any(x => x.GetContoursOnImagePlane(ss.Image.ZSize - 1).Any())) _checkSupportStructures = true;
                ProvideUIUpdate(100 * ++percentComplete / calcItems, "Checking if couch structures are on first or last slices of image");
            }
            else ProvideUIUpdate(100 * ++percentComplete / calcItems, "No couch structures present --> nothing to check");

            UpdateOverallProgress(100 * ++overallPercentCompletion / overallCalcItems);
            return false;
        }

        /// <summary>
        /// Helper method to check the attributes of the structure set, image, and integrity of the targets that will be used for optimization
        /// </summary>
        /// <param name="ss"></param>
        /// <param name="targetIDs"></param>
        /// <returns></returns>
        public bool PreliminaryChecksSSAndImage(StructureSet ss, IEnumerable<string> targetIDs)
        {
            int percentComplete = 0;
            int calcItems = 2 + targetIDs.Count();

            //check if the user assigned the imaging device Id. If not, the optimization will crash with no error
            if (string.IsNullOrEmpty(ss.Image.Series.ImagingDeviceId))
            {
                ProvideUIUpdate("Error! Did you forget to set the imaging device to 'Def_CTScanner'?", true);
                return true;
            }
            ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Imaging device Id: {ss.Image.Series.ImagingDeviceId}");

            //is the user origin set, does the body exist, and is the user origin inside the body
            if (!ss.Image.HasUserOrigin || !StructureTuningHelper.DoesStructureExistInSS("body", ss, true) || !StructureTuningHelper.GetStructureFromId("body", ss).IsPointInsideSegment(ss.Image.UserOrigin))
            {
                ProvideUIUpdate("Did you forget to set the user origin?" + Environment.NewLine + "User origin is NOT inside body contour!" + Environment.NewLine + "Please fix and try again!", true);
                return true;
            }
            ProvideUIUpdate(100 * ++percentComplete / calcItems, "User origin assigned and located within body structure");

            if (!targetIDs.Any())
            {
                ProvideUIUpdate("Error! No target ids present in list! Unable to verify targets! Exiting", true);
                return true;
            }
            foreach (string itr in targetIDs)
            {
                if (!StructureTuningHelper.DoesStructureExistInSS(itr, ss, true))
                {
                    ProvideUIUpdate($"Error! Target: {itr} is missing from structure set or empty! Please fix and try again!", true);
                    return true;
                }
                else ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Target: {itr} is in structure set and is not null");
            }

            UpdateOverallProgress(100 * ++overallPercentCompletion / overallCalcItems);
            return false;
        }

        /// <summary>
        /// Preliminary checks that should be performed for each of the supplied plans. In addition set some basic configuration settings for each
        /// of the supplied plans
        /// </summary>
        /// <param name="plans"></param>
        /// <returns></returns>
        protected bool PreliminaryChecksPlans(List<ExternalPlanSetup> plans)
        {
            int percentComplete = 0;
            int calcItems = 5 * plans.Count;

            foreach (ExternalPlanSetup itr in plans)
            {
                if (!itr.Beams.Any(x => !x.IsSetupField))
                {
                    ProvideUIUpdate($"No beams present in plan: {itr.Id}!", true);
                    return true;
                }
                ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Beams present in plan: {itr.Id}");

                //check each beam to ensure the isoposition is rounded-off to the nearest 5mm
                calcItems += itr.Beams.Where(x => !x.IsSetupField).Count();
                foreach (Beam b in itr.Beams.Where(x => !x.IsSetupField))
                {
                    BeamParameters bp = b.GetEditableParameters();
                    bp.Isocenter = RoundIsocenterPosition(b.IsocenterPosition, itr);
                    b.ApplyParameters(bp);
                    ProvideUIUpdate(100 * ++percentComplete / calcItems);
                }

                //turn on jaw tracking if available
                try
                {
                    itr.OptimizationSetup.UseJawTracking = true;
                    ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Enabled jaw tracking for plan: {itr.Id}");
                }
                catch (Exception e)
                {
                    ProvideUIUpdate(100 * ++percentComplete / calcItems, $"{e.Message}\nCannot set jaw tracking for this machine! Jaw tracking will not be enabled!");
                }

                //set auto NTO priority to zero (i.e., shut it off)
                itr.OptimizationSetup.AddAutomaticNormalTissueObjective(0.0);
                ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Set automatic NTO priority to 0 for plan: {itr.Id}");

                //be sure to set the dose value presentation to absolute! This is important for plan evaluation in the evaluateAndUpdatePlan method below
                itr.DoseValuePresentation = DoseValuePresentation.Absolute;
                ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Set dose value presentation to absolute for plan: {itr.Id}");
            }
            UpdateOverallProgress(100 * ++overallPercentCompletion / overallCalcItems);
            return false;
        }

        /// <summary>
        /// Helper method to round all of the calculated isocenter positions to the nearest integer
        /// </summary>
        /// <param name="v"></param>
        /// <param name="plan"></param>
        /// <returns></returns>
        public VVector RoundIsocenterPosition(VVector v, ExternalPlanSetup plan)
        {
            int counter = 0;
            int calcItems = 3;
            ProvideUIUpdate(100 * ++counter / calcItems, "Rounding Y- and Z-positions to nearest integer values");
            v = plan.StructureSet.Image.DicomToUser(v, plan);
            //round z position to the nearest integer
            v.x = Math.Round(v.x / 10.0f) * 10.0f;
            v.y = Math.Round(v.y / 10.0f) * 10.0f;
            v.z = Math.Round(v.z / 10.0f) * 10.0f;
            ProvideUIUpdate(100 * ++counter / calcItems, $"Calculated isocenter position (user coordinates): ({v.x}, {v.y}, {v.z})");
            ProvideUIUpdate(100 * ++counter / calcItems, "Adding calculated isocenter position to stack!");
            return plan.StructureSet.Image.UserToDicom(v, plan);
        }
        #endregion

        #region optimization loop
        /// <summary>
        /// Main controller for controlling the flow of the optimization loop for initial only and sequential boost cases
        /// </summary>
        /// <param name="plans"></param>
        /// <returns></returns>
        protected virtual bool RunOptimizationLoop(List<ExternalPlanSetup> plans)
        {
            UpdateUILabel("Optimization Loop:");
            //need to determine if we only need to optimize one plan (or an initial and boost plan)
            if (plans.Count == 1)
            {
                if (RunOptimizationLoopInitialPlanOnly(plans.First())) return true;
            }
            else return true;
            if (ResolveRunOptions(plans)) return true;
            if (!_data.IsDemo) _data.Application.SaveModifications();
            return false;
        }

        /// <summary>
        /// Virtual method to be overriden in the child classes to determine how to resolve final run options (specific to TBI/CSI plan types)
        /// </summary>
        /// <param name="plans"></param>
        /// <returns></returns>
        protected virtual bool ResolveRunOptions(List<ExternalPlanSetup> plans)
        {
            return true;
        }

        /// <summary>
        /// Helper method to run one more optimization for each of the supplied plans in an attempt to lower the hotspots in the plan
        /// </summary>
        /// <param name="plans"></param>
        /// <returns></returns>
        protected bool RunOneMoreOptionizationToLowerHotspots(List<ExternalPlanSetup> plans)
        {
            int percentComplete = 0;
            int calcItems = 3 * plans.Count;

            foreach (ExternalPlanSetup itr in plans)
            {
                ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Running one final optimization to try and reduce global plan hotspots for plan: {itr.Id}!");
                ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");

                //one final push to lower the global plan hotspot if the user asked for it
                if (OptimizePlan(_data.IsDemo, new OptimizationOptionsVMAT(OptimizationOption.ContinueOptimizationWithPlanDoseAsIntermediateDose, ""), itr, _data.Application)) return true;
                UpdateOverallProgress(100 * ++overallPercentCompletion / overallCalcItems);
                ProvideUIUpdate(100 * ++percentComplete / calcItems, "Optimization finished! Calculating dose!");
                ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");

                if (CalculateDose(_data.IsDemo, itr, _data.Application)) return true;
                UpdateOverallProgress(100 * ++overallPercentCompletion / overallCalcItems);
                ProvideUIUpdate(100 * ++percentComplete / calcItems, "Dose calculated, normalizing plan!");
                ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");

                //normalize
                double normalizationValue = NormalizePlan(itr,
                                                          TargetsHelper.GetTargetStructureForPlanType(_data.StructureSet,
                                                                                                      OptimizationLoopHelper.GetNormaliztionVolumeIdForPlan(itr.Id, _data.NormalizationVolumes),
                                                                                                      _data.UseFlash,
                                                                                                      _data.PlanType),
                                                          _data.TreatmentPercentage,
                                                          _data.TargetCoverageNormalization);
                if (double.IsNaN(normalizationValue)) return true;
                itr.PlanNormalizationValue = normalizationValue;
                ProvideUIUpdate($"{itr.Id} normalized. Normalization value = {normalizationValue:0.0}%");
                UpdateOverallProgress(100 * ++overallPercentCompletion / overallCalcItems);
                ProvideUIUpdate($"{itr.Id} normalized!");

                //print requested additional info about the plan
                ProvideUIUpdate(OptimizationLoopUIHelper.PrintAdditionalPlanDoseInfo(_data.RequestedPlanMetrics, itr, _data.NormalizationVolumes));
            }
            return false;
        }

        /// <summary>
        /// Method to control the flow of the optimization loop for initial-only plan cases (TBI and CSI-initial)
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        protected virtual bool RunOptimizationLoopInitialPlanOnly(ExternalPlanSetup plan)
        {
            int percentComplete = 0;
            int calcItems = 1 + 7 * _data.NumberOfIterations;

            ////update the current optimization parameters for this iteration
            //List<OptimizationConstraintModel> initializedConstraints = InitializeOptimizationConstriants(plan);
            //UpdateConstraints(initializedConstraints, plan);

            //print the initial optimization constraints. DO NOT apply 2/3 scaling to priorities!
            ProvideUIUpdate(100 * ++percentComplete / calcItems, OptimizationLoopUIHelper.PrintPlanOptimizationConstraints(plan, OptimizationSetupHelper.ReadConstraintsFromPlan(plan)));

            if (_data.IsDemo) Thread.Sleep(3000);
            else _data.Application.SaveModifications();

            ProvideUIUpdate("Starting optimization loop!");
            //counter to keep track of how many optimization iterations have been performed
            int count = 0;
            while (count < _data.NumberOfIterations)
            {
                bool isFinalOpt = (_data.OneMoreOptimization && ((count + 1) == _data.NumberOfIterations));
                ProvideUIUpdate(100 * (++percentComplete) / calcItems, $"Iteration {count + 1}:");
                ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");

                if (OptimizePlan(_data.IsDemo, new OptimizationOptionsVMAT(OptimizationIntermediateDoseOption.NoIntermediateDose, ""), plan, _data.Application)) return true;
                ProvideUIUpdate(100 * (++percentComplete) / calcItems, "Optimization finished! Calculating intermediate dose!");
                ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");

                if (CalculateDose(_data.IsDemo, plan, _data.Application)) return true;
                ProvideUIUpdate(100 * (++percentComplete) / calcItems, "Dose calculated! Continuing optimization!");
                ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");

                if (OptimizePlan(_data.IsDemo, new OptimizationOptionsVMAT(OptimizationOption.ContinueOptimizationWithPlanDoseAsIntermediateDose, ""), plan, _data.Application)) return true;
                ProvideUIUpdate(100 * (++percentComplete) / calcItems, "Optimization finished! Calculating dose!");
                ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");

                if (CalculateDose(_data.IsDemo, plan, _data.Application)) return true;
                ProvideUIUpdate(100 * (++percentComplete) / calcItems, "Dose calculated, normalizing plan!");
                ProvideUIUpdate($"Elapsed time: {ElapsedRunTime}");

                double normalizationValue = NormalizePlan(plan,
                                                          TargetsHelper.GetTargetStructureForPlanType(_data.StructureSet,
                                                                                                      OptimizationLoopHelper.GetNormaliztionVolumeIdForPlan(plan.Id, _data.NormalizationVolumes),
                                                                                                      _data.UseFlash,
                                                                                                      _data.PlanType),
                                                          _data.TreatmentPercentage,
                                                          _data.TargetCoverageNormalization);
                if (double.IsNaN(normalizationValue)) return true;
                plan.PlanNormalizationValue = normalizationValue;
                ProvideUIUpdate($"{plan.Id} normalized. Normalization value = {normalizationValue:0.0}%");
                ProvideUIUpdate(100 * (++percentComplete) / calcItems, "Evaluating plan quality and updating constraints!");

                //evaluate the new plan for quality and make any adjustments to the optimization parameters
                PlanEvaluationDataContainer e = EvaluateAndUpdatePlan(plan, _data.PlanObjectives, isFinalOpt);
                if (e.OptimizationKilledByUser) return true;
                else if (e.AllPlanObjectivesMet)
                {
                    //updated optimization constraint list is empty, which means that all plan objectives have been met. 
                    //Let the user know and break the loop. Also set oneMoreOpt to false so that extra optimization is not performed
                    ProvideUIUpdate("All plan objectives have been met! Exiting!", true);
                    _data.OneMoreOptimization = false;
                    return false;
                }

                //did the user request to copy and save each plan iteration from the optimization loop?
                //the last two boolean evaluations check if the user requested one more optimization (always copy and save) or this is not the last loop iteration (used in the case where the user elected NOT to do one more optimization
                //but still wants to copy and save each plan). We don't want to copy and save the plan on the last loop iteration when oneMoreOpt is false because we will end up with two copies of
                //the same plan!
                if (!_data.IsDemo && _data.CopyAndSaveEachOptimizedPlan && (_data.OneMoreOptimization || ((count + 1) != _data.NumberOfIterations))) CopyAndSavePlan(plan, count);

                ProvideUIUpdate(OptimizationLoopUIHelper.PrintPlanOptimizationResultVsConstraints(plan, OptimizationSetupHelper.ReadConstraintsFromPlan(plan), e.PlanDifferenceFromOptConstraints, e.TotalOptimizationCostOptConstraints));
                ProvideUIUpdate(OptimizationLoopUIHelper.PrintAdditionalPlanDoseInfo(_data.RequestedPlanMetrics, plan, _data.NormalizationVolumes));

                //really crank up the priority and lower the dose objective on the cooler on the last iteration of the optimization loop
                //this is basically here to avoid having to call op.updateConstraints a second time (if this batch of code was placed outside of the loop)
                if (isFinalOpt) e.UpdatedOptimizationObjectives = OptimizationLoopHelper.IncreaseOptConstraintPrioritiesForFinalOpt(e.UpdatedOptimizationObjectives);

                //print updated optimization constraints
                ProvideUIUpdate(100 * ++percentComplete / calcItems, OptimizationLoopUIHelper.PrintPlanOptimizationConstraints(plan, e.UpdatedOptimizationObjectives));

                //update the optimization constraints in the plan
                UpdateConstraints(e.UpdatedOptimizationObjectives, plan);

                //increment the counter, update d.optParams so it is set to the initial optimization constraints at the BEGINNING of the optimization iteration, and save the changes to the plan
                count++;
            }
            return false;
        }
        #endregion

        #region helper functions during optimization
        protected bool OptimizePlan(bool isDemo, OptimizationOptionsVMAT options, ExternalPlanSetup plan, Application app)
        {
            UpdateUILabel("Optimization:");
            if (isDemo) Thread.Sleep(3000);
            else
            {
                try
                {
                    OptimizerResult optRes = plan.OptimizeVMAT(options);
                    if (!optRes.Success)
                    {
                        PrintFailedMessage("Optimization");
                        return true;
                    }
                }
                catch (Exception except)
                {
                    PrintFailedMessage("Optimization", except.Message);
                    return true;
                }
                app.SaveModifications();
            }
            UpdateOverallProgress(100 * ++overallPercentCompletion / overallCalcItems);
            //check if user wants to stop
            if (AbortOptimization)
            {
                OptimizationRunAborted();
                return true;
            }
            return false;
        }

        public bool CalculateDose(bool isDemo, ExternalPlanSetup plan, Application app)
        {
            UpdateUILabel("Dose calculation:");
            if (isDemo) Thread.Sleep(3000);
            else
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                WinUtilitiesModified.LaunchWindowsClosingThread(cts.Token, fileNameErrorsWarnings);
                try
                {
                    CalculationResult calcRes = plan.CalculateDose();
                    if (!calcRes.Success)
                    {
                        cts.Cancel();
                        PrintFailedMessage("Dose calculation");
                        return true;
                    }
                }
                catch (Exception except)
                {
                    cts.Cancel();
                    PrintFailedMessage("Dose calculation", except.Message);
                    return true;
                }
                app.SaveModifications();
                cts.Cancel();
            }
            UpdateOverallProgress(100 * ++overallPercentCompletion / overallCalcItems);
            //check if user wants to stop
            if (AbortOptimization)
            {
                OptimizationRunAborted();
                return true;
            }
            return false;
        }

        protected bool CopyAndSavePlan(ExternalPlanSetup plan, int count)
        {
            UpdateUILabel("Copy and save plan:");
            Course c = plan.Course;
            //this copies the plan and the dose!
            ExternalPlanSetup newPlan = (ExternalPlanSetup)c.CopyPlanSetup(plan);
            string newPlanId = $"opt itr {plan.Id}{count + 1}";
            if (newPlanId.Length > 13) newPlanId = newPlanId.Substring(0, 13);
            newPlan.Id = newPlanId;
            ProvideUIUpdate($"Copying plan: {plan.Id} and saving as: {newPlan.Id}");
            return false;
        }

        public List<OptimizationConstraintModel> InitializeOptimizationConstriants(ExternalPlanSetup plan)
        {
            int percentComplete = 0;
            List<OptimizationConstraintModel> originalOptObj = OptimizationSetupHelper.ReadConstraintsFromPlan(plan);
            int calcItems = originalOptObj.Count();
            List<OptimizationConstraintModel> optObj = new List<OptimizationConstraintModel> { };
            int priority;

            UpdateUILabel("Initialize constraints:");
            ProvideUIUpdate(OptimizationLoopUIHelper.GetOptimizationObjectivesHeader(plan.Id));
            foreach (OptimizationConstraintModel opt in originalOptObj)
            {
                //leave the PTV priorities at their original values (i.e., 100)
                if (opt.StructureId.ToLower().Contains("ptv") || opt.StructureId.ToLower().Contains("ts_jnx")) priority = opt.Priority;
                //start OAR structure priorities at 2/3 of the values the user specified so there is some wiggle room for adjustment
                else priority = (int)Math.Ceiling(((double)opt.Priority * 2) / 3);
                optObj.Add(new OptimizationConstraintModel(opt.StructureId, opt.ConstraintType, opt.QueryDose, Units.cGy, opt.QueryVolume, priority));
                ProvideUIUpdate(100 * ++percentComplete / calcItems, String.Format("{0, -16} | {1, -16} | {2,-10:N1} | {3,-10:N1} | {4,-8} |", opt.StructureId, opt.ConstraintType, opt.QueryDose, opt.QueryVolume, priority));
            }
            ProvideUIUpdate(" ");

            UpdateOverallProgress(100 * ++overallPercentCompletion / overallCalcItems);
            return optObj;
        }

        protected bool UpdateConstraints(List<OptimizationConstraintModel> obj, ExternalPlanSetup plan)
        {
            int percentComplete = 0;
            int calcItems = plan.OptimizationSetup.Objectives.Count() + obj.Count();
            UpdateUILabel("Remove existing constraints:");
            //remove all existing optimization constraints
            foreach (OptimizationObjective o in plan.OptimizationSetup.Objectives)
            {
                plan.OptimizationSetup.RemoveObjective(o);
                ProvideUIUpdate(100 * ++percentComplete / calcItems);
            }

            UpdateUILabel("Assign updated constraints:");
            //assign the new optimization constraints
            foreach (OptimizationConstraintModel opt in obj)
            {
                double dose = opt.QueryDose;
                if (opt.QueryDoseUnits == Units.Percent) dose *= plan.TotalDose.Dose / 100.0;
                if (opt.ConstraintType != OptimizationObjectiveType.Mean)
                {
                    plan.OptimizationSetup.AddPointObjective(StructureTuningHelper.GetStructureFromId(opt.StructureId, plan.StructureSet),
                                                             OptimizationTypeHelper.GetObjectiveOperator(opt.ConstraintType),
                                                             new DoseValue(dose, DoseValue.DoseUnit.cGy),
                                                             opt.QueryVolume,
                                                             opt.Priority);
                }
                else
                {
                    plan.OptimizationSetup.AddMeanDoseObjective(StructureTuningHelper.GetStructureFromId(opt.StructureId, plan.StructureSet),
                                                                new DoseValue(dose, DoseValue.DoseUnit.cGy),
                                                                opt.Priority);
                }
                ProvideUIUpdate(100 * ++percentComplete / calcItems);
            }
            UpdateOverallProgress(100 * ++overallPercentCompletion / overallCalcItems);
            return false;
        }
        #endregion

        #region normalization
        /// <summary>
        /// Helper utility method to normalize the supplied plan to achieve the requested target coverage to the supplied target
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="target"></param>
        /// <param name="relativeDose"></param>
        /// <param name="targetVolCoverage"></param>
        /// <returns></returns>
        public double NormalizePlan(ExternalPlanSetup plan, Structure target, double relativeDose, double targetVolCoverage)
        {
            UpdateUILabel("Normalization:");
            double normValue = double.NaN;
            //in demo mode, dose might not be calculated for the plan
            if (!plan.IsDoseValid)
            {
                ProvideUIUpdate($"Error! Dose for plan {plan.Id} is NOT valid! Cannot normalize! Exiting!", true);
                return normValue;
            }
            if (ReferenceEquals(target, null) || target.IsEmpty)
            {
                ProvideUIUpdate($"Error! Target/normalization structure for plan {plan.Id} is NOT null or empty! Cannot normalize! Exiting!", true);
                return normValue;
            }
            //how to normalize a plan in the ESAPI workspace:
            //reference: https://github.com/VarianAPIs/Varian-Code-Samples/blob/master/webinars%20%26%20workshops/Research%20Symposium%202015/Eclipse%20Scripting%20API/Projects/AutomatedPlanningDemo/PlanGeneration.cs
            plan.PlanNormalizationValue = 100.0;
            //absolute dose
            double RxDose = plan.TotalDose.Dose;
            //construct a DoseValue from RxDose
            DoseValue dv = new DoseValue(relativeDose * RxDose / 100, DoseValue.DoseUnit.cGy);
            //get current coverage of the RxDose
            double coverage = plan.GetVolumeAtDose(target, dv, VolumePresentation.Relative);

            ProvideUIUpdate($"{target.Id} V{relativeDose}% = {coverage:0.0}%");
            //if the current coverage doesn't equal the desired coverage, then renormalize the plan
            if (coverage != targetVolCoverage)
            {
                ProvideUIUpdate($"Renormalizing plan: {plan.Id} to acheive {target.Id} V{relativeDose}% >= {targetVolCoverage}");
                //get the dose that does cover the targetVolCoverage of the target volume and scale the dose distribution by the ratio of that dose to the relative prescription dose
                dv = plan.GetDoseAtVolume(target, targetVolCoverage, VolumePresentation.Relative, DoseValuePresentation.Absolute);
                normValue = 100.0 * dv.Dose / (relativeDose * RxDose / 100);
                if (normValue < 0.01 || normValue > 10000.0)
                {
                    ProvideUIUpdate($"Calculated plan normalization value ({normValue}%) is outside of acceptable range: 0.01% - 10000.0%! Exiting", true);
                    return double.NaN;
                }
            }
            UpdateOverallProgress(100 * ++overallPercentCompletion / overallCalcItems);
            return normValue;
        }
        #endregion

        #region plan evaluation
        /// <summary>
        /// Helper method to control the flow of evaluating the plan quality of the supplied plan and updating the optimization constraints
        /// assigned to the plan
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="planObj"></param>
        /// <param name="finalOptimization"></param>
        /// <returns></returns>
        protected PlanEvaluationDataContainer EvaluateAndUpdatePlan(ExternalPlanSetup plan,
                                                       List<PlanObjectiveModel> planObj,
                                                       bool finalOptimization)
        {
            UpdateUILabel($"Plan evaluation: {plan.Id}");
            ProvideUIUpdate(Environment.NewLine + "Constructed evaluation data struct!");
            //create a new data structure to hold the results of the plan quality evaluation
            PlanEvaluationDataContainer e = new PlanEvaluationDataContainer();

            List<PlanObjectivesDeviationModel> differenceFromPlanObj = EvaluateResultVsPlanObjectives(plan, planObj);
            if (AbortOptimization)
            {
                OptimizationRunAborted();
                e.OptimizationKilledByUser = true;
                return e;
            }

            e.PlanDifferenceFromPlanObjectives = differenceFromPlanObj;
            //all constraints met, exiting
            if (differenceFromPlanObj.All(x => x.ObjectiveMet == true))
            {
                e.AllPlanObjectivesMet = true;
                return e;
            }
            ProvideUIUpdate("All plan objectives NOT met! Adjusting optimization parameters!");

            ProvideUIUpdate($"Parsing optimization objectives from plan: {plan.Id}");
            List<OptimizationConstraintModel> optParams = OptimizationSetupHelper.ReadConstraintsFromPlan(plan);
            List<PlanOptConstraintsDeviationModel> differenceFromOptConstraints = EvaluateResultVsOptimizationConstraints(plan, optParams);
            e.TotalOptimizationCostOptConstraints = differenceFromOptConstraints.Sum(x => x.OptimizationCost);
            e.PlanDifferenceFromOptConstraints = differenceFromOptConstraints;
            if (AbortOptimization)
            {
                OptimizationRunAborted();
                e.OptimizationKilledByUser = true;
                return e;
            }

            e.UpdatedOptimizationObjectives = DetermineNewOptimizationObjectives(plan, e.PlanDifferenceFromOptConstraints, e.TotalOptimizationCostOptConstraints, optParams);
            if (AbortOptimization)
            {
                OptimizationRunAborted();
                e.OptimizationKilledByUser = true;
                return e;
            }

            (bool wasKilled, List<OptimizationConstraintModel> updatedOptConstraints) = UpdateHeaterCoolerStructures(plan, finalOptimization, _data.RequestedOptimizationTSStructures);

            //did the user abort the program while updating the heater and cooler structures
            if (wasKilled)
            {
                //user killed operation while generating heater and cooler structures
                OptimizationRunAborted();
                e.OptimizationKilledByUser = true;
                return e;
            }
            e.UpdatedOptimizationObjectives.AddRange(updatedOptConstraints);

            UpdateOverallProgress(100 * ++overallPercentCompletion / overallCalcItems);
            return e;
        }

        /// <summary>
        /// Helper method to evaluate the plan quality of the supplied plan versus the supplied planning objectives
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="planObj"></param>
        /// <returns></returns>
        public List<PlanObjectivesDeviationModel> EvaluateResultVsPlanObjectives(ExternalPlanSetup plan,
                                                                                    List<PlanObjectiveModel> planObj)
        {
            ProvideUIUpdate("Evluating optimization result vs plan objectives");
            int percentComplete = 0;
            int calcItems = 1 + planObj.Count();
            List<PlanObjectivesDeviationModel> differenceFromPlanObj = new List<PlanObjectivesDeviationModel> { };

            //loop through all the plan objectives for this case and compare the actual dose to the dose in the plan objective.
            //If we met the constraint, increment numPass. At the end of the loop, if numPass == the number of plan objectives
            //then we have achieved the desired plan quality and can stop the optimization loop
            foreach (PlanObjectiveModel itr in planObj)
            {
                ProvideUIUpdate(100 * ++percentComplete / calcItems);
                //used to account for the case where there is a template plan objective that is not included in the current case (e.g., testes are not always spared)
                if (StructureTuningHelper.DoesStructureExistInSS(itr.StructureId, plan.StructureSet, true))
                {
                    //similar to code to the foreach loop used to cycle through the optimization parameters
                    Structure s = StructureTuningHelper.GetStructureFromId(itr.StructureId, plan.StructureSet);
                    double diff = PlanEvaluationHelper.GetDifferenceFromGoal(plan, itr, s);
                    if (diff <= 0.0)
                    {
                        //objective was met. Increment the counter for the number of objecives met
                        ProvideUIUpdate($"Plan objective met for: ({itr.StructureId},{itr.ConstraintType},{itr.QueryDose} {itr.QueryDoseUnits}, {itr.QueryVolume} {itr.QueryVolumeUnits})");
                    }
                    else
                    {
                        ProvideUIUpdate($"Plan objective NOT met for: ({itr.StructureId},{itr.ConstraintType},{itr.QueryDose} {itr.QueryDoseUnits}, {itr.QueryVolume} {itr.QueryVolumeUnits})");
                    }

                    //add this comparison to the list and increment the running total of the cost for the plan objectives
                    differenceFromPlanObj.Add(new PlanObjectivesDeviationModel(s, diff * diff, diff <= 0));
                }
            }
            ProvideUIUpdate(100, $"Elapsed time: {ElapsedRunTime}");
            return differenceFromPlanObj;
        }

        /// <summary>
        /// Helper method to evaluate the plan quality for the supplied plan and calculate the differences between the optimization dose objectives and the achieved doses
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="optParams"></param>
        /// <returns></returns>
        public List<PlanOptConstraintsDeviationModel> EvaluateResultVsOptimizationConstraints(ExternalPlanSetup plan,
                                                                                                 List<OptimizationConstraintModel> optParams)
        {
            ProvideUIUpdate("Evaluating optimization result vs optimization constraints:");
            //since we didn't meet all of the plan objectives, we now need to evaluate how well the plan compared to the desired plan objectives
            List<PlanOptConstraintsDeviationModel> differenceFromOptConstraints = new List<PlanOptConstraintsDeviationModel> { };
            int percentComplete = 0;
            int calcItems = 1 + optParams.Count();
            foreach (OptimizationConstraintModel itr in optParams)
            {
                ProvideUIUpdate(100 * ++percentComplete / calcItems);
                //get the structure for each optimization object in optParams and its associated DVH
                Structure s = StructureTuningHelper.GetStructureFromId(itr.StructureId, _data.StructureSet);
                //dose representation in optimization objectives is always absolute!
                double diff = PlanEvaluationHelper.GetDifferenceFromGoal(plan, itr, s);

                //calculate the cost for this constraint as the dose difference squared times the constraint priority
                double cost = diff * diff * itr.Priority;

                //structure, dvh data, current dose obj, dose diff^2, cost, current priority
                differenceFromOptConstraints.Add(new PlanOptConstraintsDeviationModel(s, itr.QueryDose, diff * diff, cost, itr.Priority));
                //add the cost for this constraint to the running total
            }
            ProvideUIUpdate(100, $"Elapsed time: {ElapsedRunTime}");
            //save the total cost from this optimization
            return differenceFromOptConstraints;
        }

        /// <summary>
        /// Helper method to take the calculated plan quality metrics (i.e., diffPlanOpt list) and determine new optimization constraints for the supplied plan
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="diffPlanOpt"></param>
        /// <param name="totalCostOptimizationConstraints"></param>
        /// <param name="optParams"></param>
        /// <returns></returns>
        protected virtual List<OptimizationConstraintModel> DetermineNewOptimizationObjectives(ExternalPlanSetup plan,
                                                                                          List<PlanOptConstraintsDeviationModel> diffPlanOpt,
                                                                                          double totalCostOptimizationConstraints,
                                                                                          List<OptimizationConstraintModel> optParams)
        {
            ProvideUIUpdate("Determining new optimization objectives for next iteration");
            //not all plan objectives were met and now we need to do some investigative work to find out what failed and by how much
            //update optimization parameters based on how each of the structures contained in diffPlanOpt performed
            List<OptimizationConstraintModel> updatedOptimizationConstraints = new List<OptimizationConstraintModel> { };
            int percentComplete = 0;
            int calcItems = 1 + diffPlanOpt.Count();
            int count = 0;
            foreach (PlanOptConstraintsDeviationModel itr in diffPlanOpt)
            {
                ProvideUIUpdate(100 * ++percentComplete / calcItems);
                double relative_cost = 0.0;
                //assign new objective dose and priority to the current dose and priority
                double newDose = itr.DoseConstraint;
                int newPriority = itr.Prioirty;
                //check to see if objective was met (i.e., was the cost > 0.). If objective was met, adjust nothing and copy the current optimization objective for this structure onto the updatedObj vector
                if (itr.OptimizationCost > 0.0)
                {
                    //objective was not met. Determine what to adjust based on OPTIMIZATION OBJECTIVE parameters (not plan objective parameters)
                    relative_cost = itr.OptimizationCost / totalCostOptimizationConstraints;

                    //do NOT adjust ptv dose constraints, only priorities (the ptv structures are going to have the highest relative cost of all the structures due to the difficulty in covering the entire PTV with 100% of the dose and keeing dMax low)
                    //If we starting adjusting the dose for these constraints, they would quickly escalate out of control, therefore, only adjust their priorities by a small amount
                    if (!itr.Structure.Id.ToLower().Contains("ptv") && !itr.Structure.Id.ToLower().Contains("ts_ring") && (relative_cost >= _data.DecisionThreshold))
                    {
                        //OAR objective is greater than threshold, adjust dose. Evaluate difference between current actual dose and current optimization parameter setting. Adjust new objective dose by dose difference weighted by the relative cost
                        //=> don't push the dose too low, otherwise the constraints won't make sense. Currently, the lowest dose limit is 10% of the Rx dose (set by adjusting lowDoseLimit)
                        //this equation was (more or less) determined empirically:
                        // current dose obj - sqrt(dose diff from current obj) * relative cost * 2
                        if ((newDose - (Math.Sqrt(itr.DoseDifferenceSquared) * relative_cost * 2)) >= plan.TotalDose.Dose * _data.LowDoseLimit)
                        {
                            newDose -= (Math.Sqrt(itr.DoseDifferenceSquared) * relative_cost * 2);
                        }
                        //else do nothing. This can be changed later to increase the priority instead of doing nothing
                    }
                    else
                    {
                        //OAR objective was less than threshold (or it was a ptv objective), adjust priority
                        //increase OAR objective priority by 100 times the relative cost of this objective
                        //increase PTV objective by 10 times the relative cost (need to have a much lower scaling factor, otherwise it will increase too rapidly)
                        double increase = 100 * relative_cost;
                        if (itr.Structure.Id.ToLower().Contains("ptv") || itr.Structure.Id.ToLower().Contains("ts_ring")) increase /= 10;
                        newPriority += (int)Math.Ceiling(increase);
                    }
                }

                //do NOT update the cooler and heater structure objectives (these will be removed, re-contoured, and re-assigned optimization objectives in the below statements)
                if (!optParams.ElementAt(count).StructureId.ToLower().Contains("ts_heater") && !optParams.ElementAt(count).StructureId.ToLower().Contains("ts_cooler"))
                {
                    updatedOptimizationConstraints.Add(new OptimizationConstraintModel(optParams.ElementAt(count).StructureId, optParams.ElementAt(count).ConstraintType, newDose, Units.cGy, optParams.ElementAt(count).QueryVolume, newPriority));
                }
                count++;
            }
            ProvideUIUpdate(100, String.Format("Elapsed time: {0}", ElapsedRunTime));
            return updatedOptimizationConstraints;
        }
        #endregion

        #region heaters and cooler structure generation removal
        /// <summary>
        /// Helper method to update the status of the heater and cooler structures based on the results of the prior optim ization loop
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="isFinalOptimization"></param>
        /// <param name="requestedTSStructures"></param>
        /// <param name="removeExistingHeaterCoolerStructures"></param>
        /// <returns></returns>
        protected virtual (bool, List<OptimizationConstraintModel>) UpdateHeaterCoolerStructures(ExternalPlanSetup plan,
                                                                                                 bool isFinalOptimization,
                                                                                                 List<RequestedOptimizationTSStructureModel> requestedTSStructures,
                                                                                                 bool removeExistingHeaterCoolerStructures = true)
        {
            UpdateUILabel("Update TS heaters & coolers:");
            bool wasKilled = false;
            ProvideUIUpdate("Updating heater and cooler tuning structures for next iteration");
            int percentComplete = 0;
            int calcItems = 2 + _data.RequestedOptimizationTSStructures.Count();
            //first remove existing structures
            if (removeExistingHeaterCoolerStructures) RemoveCoolHeatStructures(plan);

            //list to hold info related to optimization constraints for any added heater and cooler structures
            List<OptimizationConstraintModel> heaterCoolerOptConstraints = OptimizationSetupHelper.ReadConstraintsFromPlan(plan).Where(x => x.StructureId.ToLower().Contains("cooler") || x.StructureId.ToLower().Contains("heater")).ToList();
            //now create new cooler and heating structures
            ProvideUIUpdate($"Retrieving target structure for plan: {plan.Id}");
            Dictionary<string, string> plansTargets = TargetsHelper.GetHighestRxPlanTargetList(_data.Prescriptions);
            if (!plansTargets.Any())
            {
                ProvideUIUpdate("Error! Could not retrieve list of plans and associated targets! Exiting", true);
                wasKilled = true;
                return (wasKilled, heaterCoolerOptConstraints);
            }

            string targetId = "";
            if (plansTargets.Any(x => string.Equals(x.Key, plan.Id))) targetId = plansTargets.First(x => string.Equals(x.Key, plan.Id)).Value;

            Structure target = TargetsHelper.GetTargetStructureForPlanType(_data.StructureSet, targetId, _data.UseFlash, _data.PlanType);
            ProvideUIUpdate($"Retrieved target: {target.Id} for plan: {plan.Id} to evaluate requested heater/cooler structures");
            if (ReferenceEquals(target, null) || target.IsEmpty)
            {
                ProvideUIUpdate($"Error! Target structure not found or is empty for plan: {plan.Id}! Exiting!", true);
                wasKilled = true;
                return (wasKilled, heaterCoolerOptConstraints);
            }

            //iterate through the list of requested optimization tuning structures
            foreach (RequestedOptimizationTSStructureModel itr in requestedTSStructures)
            {
                ProvideUIUpdate(100 * ++percentComplete / calcItems);
                EvaluateHeaterCoolerCreationCriteria(plan, target, itr.CreationCriteria);
                //does it have constraints that need to be met before adding the TS structure?
                if (itr.AllCriteriaMet(isFinalOptimization))
                {
                    ProvideUIUpdate($"All conditions met for: {itr.TSStructureId}! Adding to structure set!");
                    Structure heaterCoolerStructure;
                    if (itr.GetType() == typeof(TSCoolerStructureModel))
                    {
                        //cooler
                        ProvideUIUpdate($"Generating cooler structure: {itr.TSStructureId} now");
                        heaterCoolerStructure = TSHeaterCoolerHelper.GenerateCooler(plan, (itr as TSCoolerStructureModel));
                    }
                    else
                    {
                        //heater
                        ProvideUIUpdate($"Generating heater structure: {itr.TSStructureId} now");
                        heaterCoolerStructure = TSHeaterCoolerHelper.GenerateHeater(plan, target, (itr as TSHeaterStructureModel));
                    }
                    if(ReferenceEquals(heaterCoolerStructure, null) || heaterCoolerStructure.IsEmpty)
                    {
                        ProvideUIUpdate($"Heater/Cooler structure ({itr.TSStructureId}) is null or empty! Removing from structure set");
                        if(_data.StructureSet.CanRemoveStructure(heaterCoolerStructure))
                        {
                            _data.StructureSet.RemoveStructure(heaterCoolerStructure);
                        }
                    }
                    else
                    {
                        ProvideUIUpdate($"{itr.TSStructureId} structure generated successfully. Adding optimization constraints now");
                        heaterCoolerOptConstraints.AddRange(itr.Constraints);
                    }
                }
                else ProvideUIUpdate($"All conditions NOT met for: {itr.TSStructureId}! Skipping!");

                if (AbortOptimization)
                {
                    wasKilled = true;
                    return (wasKilled, heaterCoolerOptConstraints);
                }
            }
            ProvideUIUpdate(100, $"Elapsed time: {ElapsedRunTime}");
            return (wasKilled, heaterCoolerOptConstraints);
        }

        protected void EvaluateHeaterCoolerCreationCriteria(ExternalPlanSetup plan, Structure target, List<OptTSCreationCriteriaModel> criteria)
        {
            foreach (OptTSCreationCriteriaModel itr in criteria)
            {
                itr.QueryResult = TSHeaterCoolerHelper.ExtractCreationCriteriaMetric(plan, target, itr);
            }
        }

        /// <summary>
        /// Helper method to retrieve all the heater and cooler structures in the structure set, and remove them
        /// </summary>
        /// <param name="plan"></param>
        protected void RemoveCoolHeatStructures(ExternalPlanSetup plan)
        {
            ProvideUIUpdate("Removing existing heater and cooler structures");
            StructureSet ss = plan.StructureSet;
            List<Structure> coolerHeater = ss.Structures.Where(x => x.Id.ToLower().Contains("ts_cooler") || x.Id.ToLower().Contains("ts_heater")).ToList();
            int percentComplete = 0;
            int calcItems = coolerHeater.Count();
            foreach (Structure itr in coolerHeater)
            {
                ProvideUIUpdate(100 * ++percentComplete / calcItems, $"Removing structure: {itr.Id}");
                if (ss.CanRemoveStructure(itr)) ss.RemoveStructure(itr);
                else ProvideUIUpdate($"Warning! Cannot remove {itr.Id} from the structure set! Skipping!");
            }
        }
        #endregion
    }
}
