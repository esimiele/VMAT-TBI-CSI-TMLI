using System;
using System.Collections.Generic;
using System.Linq;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Logging;
using VMS.TPS.Common.Model.API;

namespace AutoPlannerHelpers.Context
{
    public static class EclipseContextHelper
    {
        //data members
        private static string PatientId = "";
        private static string StructureSetUID = "";
        private static string ImageFOR = "";
        private static string PlanUID = "";
        private static string CourseID = "";

        /// <summary>
        /// Helper method to de-serialize the list of arguments passed from Eclipse to generate a fake Eclipse context
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static bool GenerateEclipseContext(List<string> args) 
        {
            try
            {
                DecodeStringContext(args);
                EclipseContext.GetInstance().Application = Application.CreateApplication();
                EclipseContext.GetInstance().UserName = EclipseContext.GetInstance().Application.CurrentUser.Name;
                EclipseContext.GetInstance().UserId = EclipseContext.GetInstance().Application.CurrentUser.Id;
                if (!string.IsNullOrEmpty(PatientId))
                {
                    EclipseContext.GetInstance().Patient = EclipseContext.GetInstance().Application.OpenPatientById(PatientId);
                    if (!ReferenceEquals(EclipseContext.GetInstance().Patient, null))
                    {
                        Logger.GetInstance().MRN = EclipseContext.GetInstance().Patient.Id;
                        EclipseContext.GetInstance().Registrations = EclipseContext.GetInstance().Patient.Registrations;
                        EclipseContext.GetInstance().CTImages = EclipseContext.GetInstance().Patient.Studies.SelectMany(x => x.Series).Where(x => x.Modality == VMS.TPS.Common.Model.Types.SeriesModality.CT).SelectMany(x => x.Images).Where(x => !double.IsNaN(x.Origin.x));
                        if (!EclipseContext.GetInstance().CTImages.Any())
                        {
                            Logger.GetInstance().LogError($"Patient (${EclipseContext.GetInstance().Patient.Id}) has NO CT images!");
                        }
                        if (!string.IsNullOrEmpty(StructureSetUID))
                        {
                            EclipseContext.GetInstance().StructureSet = EclipseContext.GetInstance().Patient.StructureSets.FirstOrDefault(x => string.Equals(StructureSetUID, x.UID));
                        }
                        EclipseContext.GetInstance().ImageFOR = ImageFOR;
                        if (!string.IsNullOrEmpty(PlanUID))
                        {
                            EclipseContext.GetInstance().VMATPlans = new List<ExternalPlanSetup> { EclipseContext.GetInstance().Patient.Courses.SelectMany(x => x.ExternalPlanSetups).FirstOrDefault(x => string.Equals(PlanUID, x.UID)) };
                            if (EclipseContext.GetInstance().VMATPlans.Any()) Logger.GetInstance().PlanUIDs = new List<string> { EclipseContext.GetInstance().VMATPlans.First().UID };
                        }
                        if (!string.IsNullOrEmpty(CourseID))
                        {
                            EclipseContext.GetInstance().Course = EclipseContext.GetInstance().Patient.Courses.FirstOrDefault(x => string.Equals(CourseID, x.Id));
                            //if (!ReferenceEquals(EclipseContext.Course, null)) Logger.GetInstance().CourseId = EclipseContext.Course.Id;
                        }
                    }
                }
                else
                {
                    Logger.GetInstance().LogError($"Error! Patient Id ({PatientId}) not found! Exiting!");
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                Logger.GetInstance().LogError($"Error! Unable to generate Eclipse Context instance because: {e.Message}");
                Logger.GetInstance().LogError(e.StackTrace, true);
                return true;
            }
        }

        /// <summary>
        /// Helper method to parse the meaning of each of the string arguments from Eclipse
        /// </summary>
        /// <param name="contextArgs"></param>
        private static void DecodeStringContext(List<string> contextArgs)
        {
            //assumes there will be an even number of arguments that are "paired"
            for (int i = 0; i < contextArgs.Count(); i += 2)
            {
                if (Settings.AutoPlannerHelperSettings.ContextKeyDictionary.TryGetValue(contextArgs.ElementAt(i), out EclipseDecodeKey theKey))
                {
                    if (theKey == EclipseDecodeKey.Patient) PatientId = contextArgs.ElementAt(i + 1);
                    else if (theKey == EclipseDecodeKey.StructureSet) StructureSetUID = contextArgs.ElementAt(i + 1);
                    else if (theKey == EclipseDecodeKey.Image) ImageFOR = contextArgs.ElementAt(i + 1);
                    else if (theKey == EclipseDecodeKey.Plan) PlanUID = contextArgs.ElementAt(i + 1);
                    else if (theKey == EclipseDecodeKey.Course) CourseID = contextArgs.ElementAt(i + 1);
                }
            }
        }
    }
}
