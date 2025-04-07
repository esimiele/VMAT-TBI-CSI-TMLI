using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Input;
using System.Windows;
using VMS.TPS.Common.Model.API;

namespace AutoPlannerLauncher.ViewModels
{
    public class LauncherMainViewModel : ObservableObject
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
        public ICommand LaunchVMATTBICommand { get; set; }
        public ICommand LaunchVMATCSICommand { get; set; }
        public ICommand LaunchVMATTMLICommand { get; set; }
        public ICommand LaunchOptimizationLoopCommand { get; set; }
        #endregion

        #region fields
        private string[] _arguments;
        #endregion

        #region events
        public event EventHandler RequestClose;
        #endregion

        internal LauncherMainViewModel(string[] args)
        {
            _arguments = args;
            if (args.ToList().Any(x => string.Equals(x, "-p")))
            {
                LaunchOptimizationLoopVisible = Visibility.Visible;
            }
            LaunchVMATTBICommand = new RelayCommand(LaunchVMATTBI);
            LaunchVMATCSICommand = new RelayCommand(LaunchVMATCSI);
            LaunchVMATTMLICommand = new RelayCommand(LaunchVMATTMLI);
            LaunchOptimizationLoopCommand = new RelayCommand(LaunchOptimizationLoop);
        }

        private void CloseWindow()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void LaunchVMATTBI()
        {
            LaunchExe("TBIAutoPlanner");
            CloseWindow();
        }

        public void LaunchVMATCSI()
        {
            LaunchExe("CSIAutoPlanner");
            CloseWindow();
        }

        public void LaunchVMATTMLI()
        {
            LaunchExe("TMLIAutoPlanner");
            CloseWindow();
        }

        public void LaunchOptimizationLoop()
        {
            LaunchExe("AutoPlannerOptimizationLoop");
            CloseWindow();
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
            else MessageBox.Show(string.Format("Error! {0} executable NOT found!", exeName));
        }

        private string SerializeInputArguments()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _arguments.Length; i++)
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

