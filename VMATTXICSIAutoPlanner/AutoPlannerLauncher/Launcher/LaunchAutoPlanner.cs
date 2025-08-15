using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using VMS.TPS.Common.Model.API;

namespace VMS.TPS
{
    public class Script
    {
        /// <summary>
        /// Simple method to launch the launcher executable while passing the patient mrn and structure set id
        /// </summary>
        /// <param name="context"></param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context)
        {
            try
            {
                if (context.Patient != null)
                {
                    string exeName = "Launcher";
                    string path = AppExePath(exeName);
                    if (!string.IsNullOrEmpty(path))
                    {
                        ProcessStartInfo p = new ProcessStartInfo(path);
                        p.Arguments = SerializeEclipseContext(context);
                        Process.Start(p);
                    }
                    else MessageBox.Show(string.Format("Error! {0} executable NOT found!", exeName));
                }
                else MessageBox.Show("Please open a patient before launching the autoplanning tool!");
            }
            catch (Exception e) { MessageBox.Show(e.Message); };
        }

        /// <summary>
        /// Retrieve the full file name of the executable
        /// </summary>
        /// <param name="exeName"></param>
        /// <returns></returns>
        private string AppExePath(string exeName)
        {
            return FirstExePathIn(Path.GetDirectoryName(GetSourceFilePath()) + @"\VMAT-TXI-CSI\", exeName);
        }

        /// <summary>
        /// Return the first identified executable in the supplied directory that has a name matching the supplied name
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="exeName"></param>
        /// <returns></returns>
        private string FirstExePathIn(string dir, string exeName)
        {
            return Directory.GetFiles(dir, "*.exe").FirstOrDefault(x => x.Contains(exeName));
        }

        /// <summary>
        /// Clever trick to return the full path of the currently executing file
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <returns></returns>
        private string GetSourceFilePath([CallerFilePath] string sourceFilePath = "")
        {
            return sourceFilePath;
        }

        private string SerializeEclipseContext(ScriptContext context)
        {
            string serializedContext = "";
            if (context != null)
            {
                if (!ReferenceEquals(context.Patient, null)) serializedContext += string.Format(" -m {0}", context.Patient.Id);
                if (!ReferenceEquals(context.StructureSet,null)) serializedContext += string.Format(" -s {0}", context.StructureSet.UID);
                if (!ReferenceEquals(context.Image,null)) serializedContext += string.Format(" -i {0}", context.Image.FOR);
                if (!ReferenceEquals(context.ExternalPlanSetup,null)) serializedContext += string.Format(" -p {0}", context.ExternalPlanSetup.UID);
                if (!ReferenceEquals(context.Course,null)) serializedContext += string.Format(" -c {0}", context.Course.Id);
            }
            return serializedContext;
        }
    }
}
