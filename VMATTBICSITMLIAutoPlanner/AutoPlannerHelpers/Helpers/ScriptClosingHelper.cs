using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Prompts;
using System.Windows;

namespace AutoPlannerHelpers.Helpers
{
    public static class ScriptClosingHelper
    {
        /// <summary>
        /// Simple helper method for closing down the main prep script UIs
        /// </summary>
        /// <param name="autoSave"></param>
        public static void CloseApplication(bool autoSave)
        {
            Logger.GetInstance().User = $"{EclipseContext.GetInstance().Application.CurrentUser.Name} ({EclipseContext.GetInstance().Application.CurrentUser.Id})";
            //be sure to close the patient before closing the application. Not doing so will result in unclosed timestamps in eclipse
            if (!ReferenceEquals(EclipseContext.GetInstance().Patient, null) && EclipseContext.GetInstance().Patient.HasModifiedData)
            {
                if (autoSave)
                {
                    //Save the results without asking the user
                    EclipseContext.GetInstance().Application.SaveModifications();
                    Logger.GetInstance().AppendLogOutput("Modifications saved to database!");
                    Logger.GetInstance().ChangesSaved = true;
                }
                else
                {
                    //ask the user if they want to save their changes
                    SaveChangesPrompt SCP = new SaveChangesPrompt();
                    SCP.ShowDialog();
                    if (SCP.GetSelection())
                    {
                        EclipseContext.GetInstance().Application.SaveModifications();
                        Logger.GetInstance().AppendLogOutput("Modifications saved to database!");
                        Logger.GetInstance().ChangesSaved = true;
                    }
                    else
                    {
                        Logger.GetInstance().AppendLogOutput("Modifications NOT saved to database!");
                        Logger.GetInstance().ChangesSaved = false;
                    }
                }
            }
            else
            {
                //no modifications made to database, don't bother saving
                Logger.GetInstance().AppendLogOutput("No modifications made to database objects!");
                Logger.GetInstance().ChangesSaved = false;
            }
            if (Logger.GetInstance().Dump())
            {
                MessageBox.Show("Error! Could not save log file!");
            }
            EclipseContext.GetInstance().Application.ClosePatient();
            EclipseContext.GetInstance().Application.Dispose();
        }
    }
}
