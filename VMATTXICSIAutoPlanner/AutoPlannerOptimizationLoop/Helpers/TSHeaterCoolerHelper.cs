using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.EnumTypeHelpers;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Models;
using System.Text;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace AutoPlannerOptimizationLoop.Helpers
{
    public static class TSHeaterCoolerHelper
    {
        /// <summary>
        /// Helper method to generate a TS cooler structure
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="doseLevel"></param>
        /// <param name="requestedDoseConstraint"></param>
        /// <param name="volume"></param>
        /// <param name="name"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static Structure GenerateCooler(ExternalPlanSetup plan, TSCoolerStructureModel ts)
        {
            Structure coolerStructure = null;
            //create an empty optimization objective
            StructureSet ss = plan.StructureSet;
            //grab the relevant dose, dose leve, priority, etc. parameters
            PlanningItemDose d = plan.Dose;
            DoseValue dv = new DoseValue(ts.UpperDoseValue * plan.TotalDose.Dose / 100, DoseValue.DoseUnit.cGy);
            if (ss.CanAddStructure("CONTROL", ts.TSStructureId))
            {
                //add the cooler structure to the structure list and convert the doseLevel isodose volume to a structure. Add this new structure to the list with a max dose objective of Rx * 105% and give it a priority of 80
                coolerStructure = ss.AddStructure("CONTROL", ts.TSStructureId);
                coolerStructure.ConvertDoseLevelToStructure(d, dv);
            }
            return coolerStructure;
        }

        /// <summary>
        /// Helper method to generate a TS heater structure
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="target"></param>
        /// <param name="doseLevelLow"></param>
        /// <param name="doseLevelHigh"></param>
        /// <param name="volume"></param>
        /// <param name="name"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static Structure GenerateHeater(ExternalPlanSetup plan, Structure target, TSHeaterStructureModel ts)
        {
            Structure heaterStructure = null;
            //similar to the generateCooler method
            StructureSet ss = plan.StructureSet;
            PlanningItemDose d = plan.Dose;
            DoseValue dv = new DoseValue(ts.LowerDoseValue * plan.TotalDose.Dose / 100, DoseValue.DoseUnit.cGy);
            if (ss.CanAddStructure("CONTROL", ts.TSStructureId))
            {
                //segment lower isodose volume
                heaterStructure = ss.AddStructure("CONTROL", ts.TSStructureId);
                heaterStructure.ConvertDoseLevelToStructure(d, dv);
                //segment higher isodose volume
                Structure dummy = ss.AddStructure("CONTROL", "dummy");
                dummy.ConvertDoseLevelToStructure(d, new DoseValue(ts.UpperDoseValue * plan.TotalDose.Dose / 100, DoseValue.DoseUnit.cGy));
                //subtract the higher isodose volume from the heater structure and assign it to the heater structure. 
                //This is the heater structure that will be used for optimization. Create a new optimization objective for this tunning structure
                ContourHelper.CropStructureFromStructure(heaterStructure, dummy, 0.0);
                //clean up
                ss.RemoveStructure(dummy);
                //only keep the overlapping regions of the heater structure with the taget structure
                ContourHelper.ContourOverlap(target, heaterStructure, 0.0);
            }
            return heaterStructure;
        }

        public static double ExtractCreationCriteriaMetric(ExternalPlanSetup plan, Structure target, OptTSCreationCriteriaModel criteria)
        {
            if (criteria.DVHMetric == DVHMetric.VolumeAtDose)
            {
                double dose = criteria.QueryValue;
                //convert query dose from a percent to absolute dose
                if (criteria.QueryUnits == Units.Percent) dose *= (plan.TotalDose.Dose / 100);
                DoseValue queryDose = new DoseValue(dose, DoseValue.DoseUnit.cGy);
                return plan.GetVolumeAtDose(target,
                                            queryDose,
                                            criteria.QueryResultUnits == Units.Percent ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3);
            }
            else if (criteria.DVHMetric == DVHMetric.DoseAtVolume)
            {
                return plan.GetDoseAtVolume(target,
                                            criteria.QueryValue,
                                            criteria.QueryUnits == Units.Percent ? VolumePresentation.Relative : VolumePresentation.AbsoluteCm3,
                                            criteria.QueryResultUnits == Units.Percent ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute).Dose;
            }
            else
            {
                DVHData dvhData = plan.GetDVHCumulativeData(target,
                                                             criteria.QueryResultUnits == Units.Percent ? DoseValuePresentation.Relative : DoseValuePresentation.Absolute,
                                                             VolumePresentation.Relative, 0.1);

                if (criteria.DVHMetric == DVHMetric.Dmax) return dvhData.MaxDose.Dose;
                else if (criteria.DVHMetric == DVHMetric.Dmean) return dvhData.MeanDose.Dose;
                else if (criteria.DVHMetric == DVHMetric.Dmin) return dvhData.MinDose.Dose;
                else return double.NaN;
            }
        }
    }
}
