using AutoPlannerHelpers.Context;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Helpers;
using AutoPlannerHelpers.Logging;
using AutoPlannerHelpers.Messengers;
using AutoPlannerHelpers.Prompts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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
        #endregion

        #region fields
        private bool _canSeparatePlans = false;
        #endregion

        #region commands
        public ICommand GenerateShiftNoteCommand { get; set; }
        public ICommand SeparatePlansCommand { get; set; }
        #endregion

        public PlanPreparationViewModel()
        {
            GenerateShiftText = "NO";
            SeparatePlanText = "NO";
            if(EclipseContext.GetInstance().IsInitialized && EclipseContext.GetInstance().VMATPlans.Any())
            {
                PlanId = EclipseContext.GetInstance().VMATPlans.First().Id;
            }
            GenerateShiftNoteCommand = new RelayCommand(GenerateShiftNote);
            SeparatePlansCommand = new RelayCommand(SeparatePlans);
        }

        private void GenerateShiftNote()
        {
            if (!EclipseContext.GetInstance().IsInitialized || ReferenceEquals(EclipseContext.GetInstance().VMATPlans.FirstOrDefault(), null))
            {
                Logger.GetInstance().LogError("Script not initialized or no vmat plans present! Exiting!");
                return;
            }
            Logger.GetInstance().OpType = ScriptOperationType.PlanPrep;
            //logic needs to be handled by specific plan type classes
            var result = WeakReferenceMessenger.Default.Send(new RequestGenerateShiftNoteMessage());
            if (!result)
            {
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
            var result = WeakReferenceMessenger.Default.Send(new RequestSeparatePlanMessage());
            if (!result)
            {
                SeparatePlanText = "YES";
            }
        }
    }
}
