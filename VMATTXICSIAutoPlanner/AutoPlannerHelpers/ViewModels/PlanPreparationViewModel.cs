using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Prompts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace AutoPlannerHelpers.ViewModels
{
    public class PlanPreparationViewModel : ObservableObject
    {
        #region properties
        private string _planId;
        private string _planIdPrefix;
        private bool _createBackupPlans;
        private string _fieldNaming;
        private string _separateIsos;
        private string _refPoints;
        private string _setupFields;
        private string _planSumCreated;
        private string _muQA;
        private string _backupPlan;
        private bool _allItemsCompleted;

        public string PlanId
        {
            get { return _planId; }
            set { SetProperty(ref _planId, value); }
        }

        public string PlanIdPrefix
        {
            get { return _planIdPrefix; }
            set { SetProperty(ref _planIdPrefix, value); }
        }

        public bool CreateBackupPlans
        {
            get { return _createBackupPlans; }
            set { SetProperty(ref _createBackupPlans, value); }
        }

        public string FieldNaming
        {
            get { return _fieldNaming; }
            set { SetProperty(ref _fieldNaming, value); }
        }

        public string SeparateIsos
        {
            get { return _separateIsos; }
            set { SetProperty(ref _separateIsos, value); }
        }

        public string RefPoints
        {
            get { return _refPoints; }
            set { SetProperty(ref _refPoints, value); }
        }

        public string SetupFields
        {
            get { return _setupFields; }
            set { SetProperty(ref _setupFields, value); }
        }

        public string PlanSumCreated
        {
            get { return _planSumCreated; }
            set { SetProperty(ref _planSumCreated, value); }
        }

        public string MUQA
        {
            get { return _muQA; }
            set { SetProperty(ref _muQA, value); }
        }

        public string BackupPlan
        {
            get { return _backupPlan; }
            set { SetProperty(ref _backupPlan, value); }
        }

        public bool AllItemsCompleted
        {
            get { return _allItemsCompleted; }
            set { SetProperty(ref _allItemsCompleted, value); }
        }
        #endregion

        #region commands
        private ICommand _notifyMainVMExecuted;
        public ICommand RunCommand { get; set; }
        #endregion

        public PlanPreparationViewModel(ICommand notifyMainVM)
        {
            _notifyMainVMExecuted = notifyMainVM;
            FieldNaming = "NO";
            SeparateIsos = "NO";
            RefPoints = "NO";
            SetupFields = "NO";
            PlanSumCreated = "NO";
            MUQA = "NO";
            BackupPlan = "NO";
            RunCommand = new RelayCommand(PreparePlanForTreatment);
        }

        private void PreparePlanForTreatment()
        {
            //logic needs to be handled by specific plan type classes
            _notifyMainVMExecuted.Execute(null);
        }

        public void UpdateUIAllPrepItemsCompleted()
        {
            FieldNaming = "YES";
            SeparateIsos = "YES";
            RefPoints = "YES";
            SetupFields = "YES";
            PlanSumCreated = "YES";
            MUQA = "YES";
            AllItemsCompleted = true;

            if (CreateBackupPlans)
            {
                BackupPlan = "YES";
            }
            else
            {
                BackupPlan = "N/A";
            }
        }
    }
}
