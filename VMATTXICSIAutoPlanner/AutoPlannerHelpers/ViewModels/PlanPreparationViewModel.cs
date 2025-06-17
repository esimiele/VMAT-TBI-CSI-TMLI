using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Messengers;
using AutoPlannerHelpers.Prompts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace AutoPlannerHelpers.ViewModels
{
    public class PlanPreparationViewModel : ObservableObject
    {
        #region properties
        private string _generateShiftText;

        public string GenerateShiftText
        {
            get { return _generateShiftText; }
            set { SetProperty(ref _generateShiftText, value); }
        }

        private string _separatePlanText;

        public string SeparatePlanText
        {
            get { return _separatePlanText; }
            set { SetProperty(ref _separatePlanText, value); }
        }

        private string  _planId;

        public string  PlanId
        {
            get { return _planId; }
            set { SetProperty(ref _planId, value); }
        }

        private Visibility _doseCalculationVisibility;

        public Visibility DoseCalculationVisibility
        {
            get { return _doseCalculationVisibility; }
            set { SetProperty(ref _doseCalculationVisibility, value); }
        }

        private string _calculateDoseText;

        public string CalculateDoseText
        {
            get { return _calculateDoseText; }
            set { SetProperty(ref _calculateDoseText, value); }
        }

        #endregion

        #region fields
        private bool _canSeparatePlans = false;
        #endregion

        #region commands
        public ICommand GenerateShiftNoteCommand { get; set; }
        public ICommand SeparatePlansCommand { get; set; }
        public ICommand CalculateDoseCommand { get; set; }
        #endregion

        public PlanPreparationViewModel()
        {
            GenerateShiftText = "NO";
            SeparatePlanText = "NO";
            if (EclipseContext.GetInstance().IsInitialized && EclipseContext.GetInstance().VMATPlans.Any())
            {
                PlanId = EclipseContext.GetInstance().VMATPlans.First().Id;
            }
            GenerateShiftNoteCommand = new RelayCommand(GenerateShiftNote);
            SeparatePlansCommand = new RelayCommand(SeparatePlans);
            CalculateDoseCommand = new RelayCommand(CalculateDose);
            DoseCalculationVisibility = Visibility.Collapsed;
        }

        private void GenerateShiftNote()
        {
            if (!EclipseContext.GetInstance().IsInitialized || ReferenceEquals(EclipseContext.GetInstance().VMATPlans.FirstOrDefault(), null))
            {
                Logger.GetInstance().LogError("Error! Script is not connected to aria or no vmat plans loaded into context! Cannot perform preparation for treatment!");
                return;
            }
            Logger.GetInstance().OpType = ScriptOperationType.PlanPrep;

            //logic needs to be handled by specific plan type classes
            var result = WeakReferenceMessenger.Default.Send(new RequestGenerateShiftNoteMessage());
            if (!result)
            {
                MessageBox.Show("Shifts have been copied to the clipboard! \r\nPaste them into the journal note!");

                GenerateShiftText = "YES";
                _canSeparatePlans = true;
            }
        }

        private void SeparatePlans()
        {
            if (!_canSeparatePlans)
            {
                Logger.GetInstance().LogError("Error! The shift note must be generated before separating the plans! Exiting!");
                return;
            }
            if (!EclipseContext.GetInstance().VMATPlans.FirstOrDefault().Beams.Any(x => x.IsSetupField))
            {
                ConfirmPrompt CUI = new ConfirmPrompt($"I didn't find any setup fields in the {EclipseContext.GetInstance().VMATPlans.FirstOrDefault().Id}." + Environment.NewLine + Environment.NewLine + "Are you sure you want to continue?!");
                CUI.ShowDialog();
                if (!CUI.GetSelection()) return;
            }

            var result = WeakReferenceMessenger.Default.Send(new RequestSeparatePlanMessage());
            if (!result)
            {
                //inform the user it's done
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Original plan(s) have been separated!");
                sb.AppendLine("Be sure to set the target volume and primary reference point!");
                if (EclipseContext.GetInstance().VMATPlans.FirstOrDefault().Beams.Any(x => x.IsSetupField))
                {
                    sb.AppendLine("Also reset the isocenter position of the setup fields!");
                }
                MessageBox.Show(sb.ToString());

                SeparatePlanText = "YES";
                bool doseRecalcNeeded = WeakReferenceMessenger.Default.Send(new RequestDoSeparatedPlansRequireDoseRecalculation());
                if(WeakReferenceMessenger.Default.Send(new RequestAreSeparatedPlansAutomaticallyRecalculated()) && WeakReferenceMessenger.Default.Send(new RequestDoSeparatedPlansRequireDoseRecalculation()))
                {
                    DoseCalculationVisibility = Visibility.Visible;
                }
            }
        }

        private void CalculateDose()
        {
            //ask the user if they are sure they want to do this. Each plan will calculate dose sequentially, which will take time
            ConfirmPrompt CUI = new ConfirmPrompt("Warning!" + Environment.NewLine + "This will take some time as each plan needs to be calculated sequentionally!" + Environment.NewLine + "Continue?!");
            CUI.ShowDialog();
            if (!CUI.GetSelection()) return;

            bool recalculationFailed = WeakReferenceMessenger.Default.Send(new RequestRecalculateDoseForSeparatedPlans());
            if(!recalculationFailed)
            {
                //let the user know this step has been completed
                CalculateDoseText = "YES";
            }
        }
    }
}
