using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Messengers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EvilDICOM.RT.Data.DVH;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AutoPlannerOptimizationLoop.ViewModels
{
    public class SelectPatientViewModel : ObservableObject
    {
        #region properties
        private string _selectedLogFileTBI;

        public string SelectedLogFileTBI
        {
            get { return _selectedLogFileTBI; }
            set
            {
                if (SetProperty(ref _selectedLogFileTBI, value))
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        UpdateSelectedLogFileTBI(value);
                    }
                }
            }
        }

        private string _selectedLogFileCSI;

        public string SelectedLogFileCSI
        {
            get { return _selectedLogFileCSI; }
            set
            {
                if (SetProperty(ref _selectedLogFileCSI, value))
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        UpdateSelectedLogFileCSI(value);
                    }
                }
            }
        }

        private string _selectedLogFileTMLI;

        public string SelectedLogFileTMLI
        {
            get { return _selectedLogFileTMLI; }
            set
            {
                if (SetProperty(ref _selectedLogFileTMLI, value))
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        UpdateSelectedLogFileTMLI(value);
                    }
                }
            }
        }


        private string _fullLogFileName;

        public string FullLogFileName
        {
            get { return _fullLogFileName; }
        }


        private PlanType _selectedPlanType;

        public PlanType SelectedPlanType
        {
            get { return _selectedPlanType; }
            set { SetProperty(ref _selectedPlanType, value); }
        }

        private string _mrn;

        public string MRN
        {
            get { return _mrn; }
            set { SetProperty(ref _mrn, value); }
        }

        public ObservableCollection<string> PatientMRNsCSI { get; set; }
        public ObservableCollection<string> PatientMRNsTBI { get; set; }
        public ObservableCollection<string> PatientMRNsTMLI { get; set; }
        #endregion

        #region fields
        private List<string> _logsCSI;
        private List<string> _logsTBI;
        private List<string> _logsTMLI;
        #endregion

        #region commands
        public ICommand OpenPatientCommand { get; set; }
        #endregion

        #region events
        public event EventHandler RequestClose;
        #endregion

        public SelectPatientViewModel()
        {
            LoadPatientMRNsFromLogs();
            OpenPatientCommand = new RelayCommand(OpenPatient);
            SelectedPlanType = PlanType.None;
        }

        private void LoadPatientMRNsFromLogs()
        {
            if (Directory.Exists(Logger.GetInstance().LogPath + "\\preparation\\"))
            {
                if (Directory.Exists(Logger.GetInstance().LogPath + "\\preparation\\CSI\\"))
                {
                    PatientMRNsCSI = new ObservableCollection<string>() { "--select--" };
                    _logsCSI = new List<string>(Directory.GetDirectories(Logger.GetInstance().LogPath + "\\preparation\\CSI\\", "*", SearchOption.TopDirectoryOnly).OrderByDescending(x => Directory.GetLastWriteTimeUtc(x)));
                    foreach (string itr in _logsCSI)
                    {
                        if (Directory.GetFiles(itr, ".", SearchOption.TopDirectoryOnly).Any())
                        {
                            string CSILogFile = Directory.GetFiles(itr, ".", SearchOption.TopDirectoryOnly).First();
                            PatientMRNsCSI.Add(CSILogFile.Substring(itr.LastIndexOf("\\") + 1, CSILogFile.Length - CSILogFile.LastIndexOf("\\") - 1 - 4));
                        }
                    }
                }
                if (Directory.Exists(Logger.GetInstance().LogPath + "\\preparation\\TBI\\"))
                {
                    PatientMRNsTBI = new ObservableCollection<string>() { "--select--" };
                    _logsTBI = new List<string>(Directory.GetDirectories(Logger.GetInstance().LogPath + "\\preparation\\TBI\\", "*", SearchOption.TopDirectoryOnly).OrderByDescending(x => Directory.GetLastWriteTimeUtc(x)));
                    foreach (string itr in _logsTBI)
                    {
                        if (Directory.GetFiles(itr, ".", SearchOption.TopDirectoryOnly).Any())
                        {
                            string TBILogFile = Directory.GetFiles(itr, ".", SearchOption.TopDirectoryOnly).First();
                            PatientMRNsTBI.Add(TBILogFile.Substring(TBILogFile.LastIndexOf("\\") + 1, TBILogFile.Length - TBILogFile.LastIndexOf("\\") - 1 - 4));
                        }
                    }
                }
                if (Directory.Exists(Logger.GetInstance().LogPath + "\\preparation\\TMLI\\"))
                {
                    PatientMRNsTMLI = new ObservableCollection<string>() { "--select--" };
                    _logsTMLI = new List<string>(Directory.GetDirectories(Logger.GetInstance().LogPath + "\\preparation\\TMLI\\", "*", SearchOption.TopDirectoryOnly).OrderByDescending(x => Directory.GetLastWriteTimeUtc(x)));
                    foreach (string itr in _logsTMLI)
                    {
                        if (Directory.GetFiles(itr, ".", SearchOption.TopDirectoryOnly).Any())
                        {
                            string TMLILogFile = Directory.GetFiles(itr, ".", SearchOption.TopDirectoryOnly).First();
                            PatientMRNsTMLI.Add(TMLILogFile.Substring(TMLILogFile.LastIndexOf("\\") + 1, TMLILogFile.Length - TMLILogFile.LastIndexOf("\\") - 1 - 4));
                        }
                    }
                }
            }
            else
            {
                Logger.GetInstance().LogError($"Log file directory: {(Logger.GetInstance().LogPath + "\\preparation\\")}\nDoes not exist! Please open an patient by manually entering an MRN.");
            }
        }

        private void UpdateSelectedLogFileTBI(string selection)
        {
            if (string.Equals(selection, "--select--"))
            {
                ClearAllSelections();
                return;
            }
            
            SelectedPlanType = PlanType.VMAT_TBI;
            MRN = selection;
            _fullLogFileName = _logsTBI.FirstOrDefault(x => x.Contains(selection));
            Application.Current.Dispatcher.BeginInvoke(new Action(() => SelectedLogFileCSI = null), System.Windows.Threading.DispatcherPriority.Background);
            Application.Current.Dispatcher.BeginInvoke(new Action(() => SelectedLogFileTMLI = null), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void UpdateSelectedLogFileCSI(string selection)
        {
            if (string.Equals(selection, "--select--"))
            {
                ClearAllSelections();
                return;
            }

            SelectedPlanType = PlanType.VMAT_CSI;
            MRN = selection;
            _fullLogFileName = _logsCSI.FirstOrDefault(x => x.Contains(selection));
            Application.Current.Dispatcher.BeginInvoke(new Action(() => SelectedLogFileTBI = null), System.Windows.Threading.DispatcherPriority.Background);
            Application.Current.Dispatcher.BeginInvoke(new Action(() => SelectedLogFileTMLI = null), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void UpdateSelectedLogFileTMLI(string selection)
        {
            if (string.Equals(selection, "--select--"))
            {
                ClearAllSelections();
                return;
            }

            SelectedPlanType = PlanType.VMAT_TMLI;
            MRN = selection;
            _fullLogFileName = _logsTMLI.FirstOrDefault(x => x.Contains(selection));
            Application.Current.Dispatcher.BeginInvoke(new Action(() => SelectedLogFileTBI = null), System.Windows.Threading.DispatcherPriority.Background);
            Application.Current.Dispatcher.BeginInvoke(new Action(() => SelectedLogFileCSI = null), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void ClearAllSelections()
        {
            SelectedPlanType = PlanType.None;
            MRN = string.Empty;
            _fullLogFileName = string.Empty;
            Application.Current.Dispatcher.BeginInvoke(new Action(() => SelectedLogFileTBI = null), System.Windows.Threading.DispatcherPriority.Background);
            Application.Current.Dispatcher.BeginInvoke(new Action(() => SelectedLogFileCSI = null), System.Windows.Threading.DispatcherPriority.Background);
            Application.Current.Dispatcher.BeginInvoke(new Action(() => SelectedLogFileTMLI = null), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void OpenPatient()
        {
            WeakReferenceMessenger.Default.Send(new RequestSelectPatient(_mrn, _selectedPlanType, _fullLogFileName));
            CloseWindow();
        }

        private void CloseWindow()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
