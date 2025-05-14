using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AutoPlannerHelpers.ViewModels
{
    public class ImportSSViewModel : ObservableObject
    {
        #region commands
        public ICommand ShowImportSSInfoCommand { get; set; }
        public ICommand ImportSSCommand { get; set; }
        #endregion

        #region fields
        ImportExportDataModel _IEData;
        string _mrn;
        PlanType _planType;
        #endregion

        public ImportSSViewModel(ImportExportDataModel iedata, PlanType type, string mrn = "")
        {
            _IEData = iedata;
            _mrn = mrn;
            _planType = type;
            ShowImportSSInfoCommand = new RelayCommand(ShowImportSSInfo);
            ImportSSCommand = new RelayCommand(ImportSS);
        }

        private void ShowImportSSInfo()
        {
            string message = "Launch the import listener script to try and import the auto-contoured structure set." + Environment.NewLine;
            message += "If the import listener does not find the structure set within the first 30 seconds, the structure set likely does not exist!";
            MessageBox.Show(message);
        }

        private void ImportSS()
        {
            if (string.IsNullOrEmpty(_mrn) || ReferenceEquals(_IEData, null)) return;
            //CT image stack panel, patient structure set list, patient id, image export path, image export format
            if (Directory.GetFiles(_IEData.ImportLocation, "*.dcm").Any())
            {
                string listener = ImportListenerHelper.GetImportListenerExePath();
                if (ImportListenerHelper.LaunchImportListener(listener, _IEData, _mrn, _planType))
                {
                    Logger.GetInstance().LogError("Error! Could not find listener executable or could not launch executable! Exiting!");
                    return;
                }
                Logger.GetInstance().OpType = ScriptOperationType.ImportSS;
            }
            else Logger.GetInstance().LogError($"No Structure set files found in import location: {_IEData.ImportLocation}");
        }
    }
}
