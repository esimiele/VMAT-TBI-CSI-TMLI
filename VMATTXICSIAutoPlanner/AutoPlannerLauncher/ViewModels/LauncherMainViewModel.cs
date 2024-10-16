using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using System.Windows;
using Prism.Commands;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace AutoPlannerLauncher.ViewModels
{
    internal class LauncherMainViewModel : BindableBase
    {
        #region properties
        private Visibility _launchOptimizationLoopVisible;

        public Visibility LaunchOptimizationLoopVisible
        {
            get { return _launchOptimizationLoopVisible; }
            set { SetProperty(ref _launchOptimizationLoopVisible, value); }
        }
        #endregion

        #region commands
        public DelegateCommand LaunchVMATTBICommand { get; set; }
        public DelegateCommand LaunchVMATCSICommand { get; set; }
        public DelegateCommand LaunchVMATTMLICommand { get; set; }
        #endregion

        #region fields
        private string[] _arguments;
        #endregion

        internal LauncherMainViewModel(string[] args) 
        { 
            _arguments = args;
            if (bool.TryParse(args[1], out bool showLauncher))
            {
                if(showLauncher) LaunchOptimizationLoopVisible = Visibility.Visible;
            }
            LaunchVMATTBICommand = new DelegateCommand(LaunchVMATTBI);
            LaunchVMATCSICommand = new DelegateCommand(LaunchVMATCSI);
            LaunchVMATTMLICommand = new DelegateCommand(LaunchVMATTMLI);
        }

        public void LaunchVMATTBI()
        {
            LaunchExe("TBIAutoPlanner");
        }

        public void LaunchVMATCSI()
        {
            LaunchExe("CSIAutoPlanner");
        }

        public void LaunchVMATTMLI()
        {
            LaunchExe("TMLIAutoPlanner");
        }

        /// <summary>
        /// Helper method to launch the executable with name matching the supplied name
        /// </summary>
        /// <param name="exeName"></param>
        private void LaunchExe(string exeName)
        {
            string path = AppExePath(exeName);
            if (!string.IsNullOrEmpty(path))
            {
                ProcessStartInfo p = new ProcessStartInfo(path)
                {
                    Arguments = SerializeInputArguments()
                };
                Process.Start(p);
                //this.Close();
            }
            else MessageBox.Show(String.Format("Error! {0} executable NOT found!", exeName));
        }

        private string SerializeInputArguments()
        {
            StringBuilder sb = new StringBuilder();
            for(int i = 2; i < _arguments.Length; i++)
            {
                sb.Append($"{_arguments[i]} ");
            }
            return sb.ToString().Trim();
        }

        /// <summary>
        /// Same method in the .cs launcher (can't use external libraries in single file plugins)
        /// </summary>
        /// <param name="exeName"></param>
        /// <returns></returns>
        private string AppExePath(string exeName)
        {
            return FirstExePathIn(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), exeName);
        }

        /// <summary>
        /// Same method in the .cs launcher (can't use external libraries in single file plugins)
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="exeName"></param>
        /// <returns></returns>
        private string FirstExePathIn(string dir, string exeName)
        {
            return Directory.GetFiles(dir, "*.exe").FirstOrDefault(x => x.Contains(exeName));
        }
    }
}
