using AutoPlannerHelpers.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AutoPlannerOptimizationLoop.Prompts
{
    /// <summary>
    /// Interaction logic for SelectPatient.xaml
    /// </summary>
    public partial class SelectPatient : Window
    {
        private string _patientMRN = "";
        private string _fullLogFileName = "";
        private string logPath = "";
        private List<string> logs = new List<string> { };
        public bool selectionMade = false;
        public (string, string) GetPatientMRN()
        {
            return (_patientMRN, _fullLogFileName);
        }

        //ATTENTION! THE FOLLOWING LINE HAS TO BE FORMATTED THIS WAY, OTHERWISE THE DATA BINDING WILL NOT WORK!
        public ObservableCollection<string> PatientMRNs { get; set; }
        public SelectPatient(string path)
        {
            InitializeComponent();
            logPath = path;
            DataContext = this;
            LoadPatientMRNsFromLogs();
        }

        private void LoadPatientMRNsFromLogs()
        {
            if (Directory.Exists(logPath + "\\preparation\\"))
            {
                PatientMRNs = new ObservableCollection<string>() { "--select--" };
                List<string> directories = new List<string>(Directory.GetDirectories(logPath + "\\preparation\\", "*", SearchOption.TopDirectoryOnly).OrderByDescending(x => Directory.GetLastWriteTimeUtc(x)));
                foreach(string directory in directories)
                {
                    logs = new List<string>(Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly).OrderByDescending(x => Directory.GetLastWriteTimeUtc(x)));
                    foreach (string itr in logs)
                    {
                        if (Directory.GetFiles(itr, ".", SearchOption.TopDirectoryOnly).Any())
                        {
                            string LogFile = Directory.GetFiles(itr, ".", SearchOption.TopDirectoryOnly).First();
                            PatientMRNs.Add(LogFile.Substring(itr.LastIndexOf("\\") + 1, LogFile.Length - LogFile.LastIndexOf("\\") - 1 - 4));
                        }
                    }
                }
            }
            else
            {
                //implement file selection system to select folder
                MessageBox.Show($"Log file directory: {(logPath + "\\preparation\\")}\nDoes not exist! Please open an patient by manually entering an MRN.");
            }
        }

        private void OpenPatient_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(MRNTB.Text) || !string.IsNullOrEmpty(_patientMRN))
            {
                //give priority to the text box data
                if (string.IsNullOrEmpty(MRNTB.Text)) _fullLogFileName = LogHelper.GetFullLogFileFromExistingMRN(_patientMRN, logPath);
                else _patientMRN = MRNTB.Text;
                selectionMade = true;
            }
            this.Close();
        }

        private void mrnList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string temp = mrnList.SelectedItem as string;
            if (string.IsNullOrEmpty(temp)) return;
            if (temp != "--select--")
            {
                _patientMRN = mrnList.SelectedItem as string;
                _fullLogFileName = logs.FirstOrDefault(x => x.Contains(_patientMRN));
            }
            else
            {
                mrnList.UnselectAll();
                _fullLogFileName = "";
                _patientMRN = "";
            }
        }
    }
}
