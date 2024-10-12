using System;
using System.Collections.Generic;
using System.Linq;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Logging;
using VMS.TPS.Common.Model.API;

namespace AutoPlannerHelpers.Context
{
    public class EclipseContextHelper
    {
        //get methods
        public EclipseContext EclipseContext { get; private set; }
        public string ErrorMessage { get; private set; }
        public string StackTraceMessage { get; private set; }
        //data members
        private string PatientId = "";
        private string StructureSetUID = "";
        private string ImageFOR = "";
        private string PlanUID = "";
        private string CourseID = "";

        /// <summary>
        /// Helper method to de-serialize the list of arguments passed from Eclipse to generate a fake Eclipse context
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool GenerateEclipseContext(List<string> args) 
        {
            try
            {
                DecodeStringContext(args);
                EclipseContext = EclipseContext.GetInstance();
                EclipseContext.Application = Application.CreateApplication();
                EclipseContext.UserName = EclipseContext.Application.CurrentUser.Name;
                EclipseContext.UserId = EclipseContext.Application.CurrentUser.Id;
                if (!string.IsNullOrEmpty(PatientId))
                {
                    EclipseContext.Patient = EclipseContext.Application.OpenPatientById(PatientId);
                    if (!ReferenceEquals(EclipseContext.Patient, null))
                    {
                        Logger.GetInstance().MRN = EclipseContext.Patient.Id;

                        EclipseContext.Registrations = EclipseContext.Patient.Registrations;
                        EclipseContext.CTImages = EclipseContext.Patient.Studies.SelectMany(x => x.Series).Where(x => x.Modality == VMS.TPS.Common.Model.Types.SeriesModality.CT).SelectMany(x => x.Images).Where(x => !double.IsNaN(x.Origin.x));
                        if (!EclipseContext.CTImages.Any())
                        {
                            Logger.GetInstance().LogError($"Patient (${EclipseContext.Patient.Id}) has NO CT images!");
                        }
                        if (!string.IsNullOrEmpty(StructureSetUID))
                        {
                            EclipseContext.StructureSet = EclipseContext.Patient.StructureSets.FirstOrDefault(x => string.Equals(StructureSetUID, x.UID));
                        }
                        EclipseContext.ImageFOR = ImageFOR;
                        if (!string.IsNullOrEmpty(PlanUID))
                        {
                            //EclipseContext.Plans = EclipseContext.Patient.Courses.SelectMany(x => x.ExternalPlanSetups).FirstOrDefault(x => string.Equals(PlanUID, x.UID));
                            //if(!ReferenceEquals(EclipseContext.Plan, null))
                            //{
                            //    Logger.GetInstance().VMATPlanUID = EclipseContext.Plan.UID;
                            //    Logger.GetInstance().VMATPlanId = EclipseContext.Plan.Id;
                            //}
                        }
                        if (!string.IsNullOrEmpty(CourseID))
                        {
                            EclipseContext.Course = EclipseContext.Patient.Courses.FirstOrDefault(x => string.Equals(CourseID, x.Id));
                            //if (!ReferenceEquals(EclipseContext.Course, null)) Logger.GetInstance().CourseId = EclipseContext.Course.Id;
                        }
                    }
                }
                else
                {
                    ErrorMessage = $"Error! Patient Id ({PatientId}) not found! Exiting!";
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                ErrorMessage = $"Error! Unable to generate Eclipse Context instance because: {e.Message}";
                StackTraceMessage = e.StackTrace;
                return true;
            }
        }

        /// <summary>
        /// Helper method to parse the meaning of each of the string arguments from Eclipse
        /// </summary>
        /// <param name="contextArgs"></param>
        private void DecodeStringContext(List<string> contextArgs)
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
