using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PlanType = AutoPlannerHelpers.Enums.PlanType;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using DialogResult = System.Windows.Forms.DialogResult;
using System.Reflection;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.Enums;
using System.IO;
using System.Windows;

namespace AutoPlannerHelpers.Logging
{
    public class Logger
    {
        /// <summary>
        /// Helpful trick to force a static instance of the logger class and has this one instance accesible everywhere
        /// </summary>
        /// <returns></returns>
        public static Logger GetInstance()
        {
            if (_instance != null) return _instance;
            else return _instance = new Logger();
        }

        #region properties
        //general patient info
        public string MRN { set => mrn = value; }
        public string Template { set => template = value; }
        public string StructureSet { set => selectedSS = value; }
        public bool ChangesSaved { set => changesSaved = value; }
        public string User { set => userId = value; }
        public string LogPath
        {
            get { return _logPath; }
            set { _logPath = value; }
        }
        public List<PrescriptionModel> Prescriptions { set => prescriptions = new List<PrescriptionModel>(value); }
        public List<string> AddedPrelimTargetsStructures { set => addedPrelimTargets = new List<string>(value); }
        //ts generation and manipulation
        public List<string> AddedStructures { set => addedStructures = new List<string>(value); }
        public List<StructureOperationModel> TargetStructureDerivations { get; set; } = new List<StructureOperationModel>();
        public List<StructureOperationModel> OptimizationStructureDerivations { get; set; } = new List<StructureOperationModel>();
        //plan id, list<original target id, ts target id>
        public Dictionary<string, string> TSTargets { set => tsTargets = new Dictionary<string, string>(value); }
        //plan id, normalization volume for plan
        public Dictionary<string, string> NormalizationVolumes { set => normVolumes = new Dictionary<string, string>(value); }
        //plan Id, list of isocenter names for this plan
        public List<PlanIsocenterModel> PlanIsocenters { set => planIsocenters = new List<PlanIsocenterModel>(value); }
        //plan generation and beam placement
        public List<string> PlanUIDs 
        { 
            get => planUIDs;
            set => planUIDs = new List<string>(value); 
        }
        //optimization setup
        public List<PlanOptimizationSetupModel> OptimizationConstraints { get; set; } = new List<PlanOptimizationSetupModel>();
        public ScriptOperationType OpType { set => opType = value; }
        public PlanType PlanType { set => planType = value; }
        #endregion

        private static Logger _instance;
        //path to location to write log file
        private string _logPath;
        //stringbuilder object to log output from ts generation/manipulation and beam placement
        private StringBuilder _logFromOperations = new StringBuilder();
        private StringBuilder _logFromErrors = new StringBuilder();
        private string userId = string.Empty;
        private string mrn = string.Empty;
        private PlanType planType = PlanType.None;
        private string template = string.Empty;
        private string selectedSS = string.Empty;
        bool changesSaved = false;
        List<PrescriptionModel> prescriptions = new List<PrescriptionModel>();
        private List<string> addedPrelimTargets = new List<string>();
        private List<string> addedStructures = new List<string>();
        private Dictionary<string, string> tsTargets = new Dictionary<string, string>();
        private Dictionary<string, string> normVolumes = new Dictionary<string, string>();
        private List<PlanIsocenterModel> planIsocenters = new List<PlanIsocenterModel>();
        private List<string> planUIDs = new List<string>();
        private ScriptOperationType opType = ScriptOperationType.None;

        /// <summary>
        /// Constructor
        /// </summary>
        public Logger()
        {
            _logPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\logs";
        }

        /// <summary>
        /// Helper method to copy a stringbuilder object to the logs. First argument is the operation type as a string and the second argument
        /// is the detailed logs from that operation
        /// </summary>
        /// <param name="info"></param>
        /// <param name="s"></param>
        public void AppendLogOutput(string info, StringBuilder s)
        {
            _logFromOperations.AppendLine(info);
            _logFromOperations.Append(s);
            _logFromOperations.AppendLine("");
        }

        public void AppendLogOutput(string info, string s)
        {
            _logFromOperations.AppendLine(info);
            _logFromOperations.AppendLine(s);
        }

        /// <summary>
        /// Simple method to copy a string to the logs
        /// </summary>
        /// <param name="info"></param>
        public void AppendLogOutput(string info)
        {
            _logFromOperations.AppendLine(info);
            _logFromOperations.AppendLine("");
        }

        /// <summary>
        /// Helper method to record an error or warning that occurred during script operation. Also optional to suppress the message box
        /// warning
        /// </summary>
        /// <param name="error"></param>
        /// <param name="suppressMessage"></param>
        public void LogError(string error, bool suppressMessage = false)
        {
            if (!suppressMessage) MessageBox.Show(error);
            _logFromErrors.AppendLine(error);
            _logFromErrors.AppendLine("");
        }

        /// <summary>
        /// Same as above except the input argument is a string builder instead of a string
        /// </summary>
        /// <param name="error"></param>
        /// <param name="suppressMessage"></param>
        public void LogError(StringBuilder error, bool suppressMessage = false)
        {
            if (!suppressMessage) MessageBox.Show(error.ToString());
            _logFromErrors.Append(error);
            _logFromErrors.AppendLine("");
        }

        #region Dump logs
        /// <summary>
        /// Method to build and write the log file. Includes both cases where changes were and were not made to the database
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool Dump()
        {
            string type;
            if (planType == PlanType.VMAT_TBI) type = "TBI";
            else if (planType == PlanType.VMAT_CSI) type = "CSI";
            else type = "TMLI";

            if (string.IsNullOrEmpty(_logPath))
            {
                LogError("Log file path not set during script configuration! Please select a folder to write the log file!");
                FolderBrowserDialog FBD = new FolderBrowserDialog
                {
                    SelectedPath = Path.GetFullPath(Assembly.GetExecutingAssembly().Location)
                };
                if (FBD.ShowDialog() == DialogResult.OK)
                {
                    _logPath = FBD.SelectedPath;
                }
                else return true;
            }

            _logPath += "\\preparation\\" + type + "\\" + mrn + "\\";
            string fileName = _logPath + mrn + ".txt";
            if (!changesSaved && opType != ScriptOperationType.ExportCT)
            {
                _logPath += "unsaved" + "\\";
                fileName = _logPath + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt";
            }
            else if (opType != ScriptOperationType.FullPreparationForOptimization)
            {
                _logPath += "MISC" + "\\";
                fileName = _logPath + opType.ToString() + ".txt";
            }

            if (!Directory.Exists(_logPath))
            {
                Directory.CreateDirectory(_logPath);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(DateTime.Now.ToString());
            sb.AppendLine($"User={userId}");
            sb.AppendLine($"Patient={mrn}");
            sb.AppendLine($"Plan type={planType}");
            //consider the operation that was performed when constructing the log file
            if (opType == ScriptOperationType.FullPreparationForOptimization) sb.Append(BuildFullPrepOpLog());
            else if (opType == ScriptOperationType.GeneratePrelimTargets) sb.Append(BuildPrelimTargetOpLog());
            else sb.AppendLine("");

            sb.AppendLine("Errors and warnings:");
            sb.Append(_logFromErrors);
            sb.AppendLine("");

            sb.AppendLine("Detailed log output:");
            sb.Append(_logFromOperations);
            sb.AppendLine("");
            try
            {
                File.WriteAllText(fileName, sb.ToString());
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            return false;
        }

        /// <summary>
        /// Method to build the log file for a normal, full preparation for optimization run of the preparation script where the focus was to prepare the structure
        /// set for optimization
        /// </summary>
        /// <returns></returns>
        private StringBuilder BuildFullPrepOpLog()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Structure set={selectedSS}");
            sb.AppendLine($"Template={template}");
            sb.AppendLine("");
            sb.AppendLine("Prescriptions:");
            foreach (PrescriptionModel itr in prescriptions) sb.AppendLine($"    {{{itr.PlanId},{itr.TargetId},{itr.NumberOfFractions},{itr.DosePerFraction.Dose},{itr.CumulativeDoseToTarget}}}");
            sb.AppendLine("");

            sb.AppendLine("Added TS structures:");
            foreach (string itr in addedStructures) sb.AppendLine($"    {itr}");
            sb.AppendLine("");

            sb.AppendLine("Structure manipulations:");
            foreach (StructureOperationModel itr in OptimizationStructureDerivations) sb.AppendLine($"    {itr.FriendlyName}");
            sb.AppendLine("");

            sb.AppendLine("Isocenter names:");
            foreach (PlanIsocenterModel itr in planIsocenters)
            {
                sb.AppendLine($"    {itr.PlanId}");
                foreach (string s in itr.Isocenters.Select(x => x.IsocenterId))
                {
                    sb.AppendLine($"        {s}");
                }
            }
            sb.AppendLine("");

            sb.AppendLine("Plan UIDs:");
            foreach (string itr in planUIDs)
            {
                sb.AppendLine($"    {itr}");
            }
            sb.AppendLine("");

            sb.AppendLine("TS Targets:");
            foreach (KeyValuePair<string, string> itr in tsTargets)
            {
                sb.AppendLine($"    {{{itr.Key},{itr.Value}}}");
            }
            sb.AppendLine("");

            sb.AppendLine("Normalization volumes:");
            foreach (KeyValuePair<string, string> itr in normVolumes)
            {
                sb.AppendLine($"    {{{itr.Key},{itr.Value}}}");
            }
            sb.AppendLine("");

            sb.AppendLine("Optimization constraints:");
            foreach (PlanOptimizationSetupModel itr in OptimizationConstraints)
            {
                sb.AppendLine($"    {itr.PlanId}");
                foreach (OptimizationConstraintModel itr1 in itr.OptimizationConstraints)
                {
                    sb.AppendLine($"        {{{itr1.StructureId},{itr1.ConstraintType},{itr1.QueryDose},{itr1.QueryVolume},{itr1.Priority}}}");
                }
            }
            sb.AppendLine("");
            return sb;
        }

        /// <summary>
        /// Method to build the log file when the script was only used to generate the preliminary targets
        /// </summary>
        /// <returns></returns>
        private StringBuilder BuildPrelimTargetOpLog()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Added preliminary targets:");
            foreach (string itr in addedPrelimTargets) sb.AppendLine("    " + itr);
            sb.AppendLine("");
            return sb;
        }
        #endregion
    }
}
