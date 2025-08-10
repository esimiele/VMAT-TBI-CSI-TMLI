using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AutoPlannerHelpers.ViewModels
{
    public class AdditionalRingOperationViewModel : ObservableObject
    {
        #region properties
        private StructureOperationModel _additionalRingOperation;
        private StructureOperationModel _originalOperationCopy;

        public StructureOperationModel AdditionalRingOperation
        {
            get { return _additionalRingOperation; }
            set { SetProperty(ref _additionalRingOperation, value); }
        }

        private List<string> _structureIds;

        public List<string> StructureIds
        {
            get { return _structureIds; }
            set { SetProperty(ref _structureIds, value); }
        }

        public string RingId { get; set; } = string.Empty;
        #endregion

        #region commands
        public ICommand SetAdditionalRingOperationCommand { get; set; }
        public RelayCommand<(string, StructureMarginModel)> ModifyMarginCommand { get; set; }
        #endregion

        #region events
        public event EventHandler RequestClose;
        #endregion

        public AdditionalRingOperationViewModel(string ringId, StructureOperationModel op, List<string> structureIds)
        {
            StructureIds = new List<string>(structureIds);
            RingId = ringId;
            _originalOperationCopy = op;
            if(op.IsValidOperation)
            {
                AdditionalRingOperation = new StructureOperationModel(ringId, op.Operation, op.StructureB, ringId, op.MarginA, op.MarginB);
            }
            else
            {
                AdditionalRingOperation = new StructureOperationModel(ringId, Enums.StructureDerivationOperation.None, _structureIds.First(), ringId);
            }
            SetAdditionalRingOperationCommand = new RelayCommand(SetAdditionalRingOperation);
            ModifyMarginCommand = new RelayCommand<(string, StructureMarginModel)>(ModifyMargin, CanModifyMargin);
        }

        private void ModifyMargin((string structureid, StructureMarginModel model) parameters)
        {
            ModifyMarginView view = new ModifyMarginView { DataContext = new ModifyMarginViewModel(parameters.structureid, parameters.model) };
            view.ShowDialog();
        }

        public void RequestedReEvaluationOfCanExecute()
        {
            Application.Current.Dispatcher.BeginInvoke(() => { ModifyMarginCommand.NotifyCanExecuteChanged(); }, DispatcherPriority.Render);
        }

        private bool CanModifyMargin((string structureid, StructureMarginModel model) parameters)
        {
            return !string.IsNullOrEmpty(parameters.structureid) && !ReferenceEquals(parameters.model, null);
        }

        private void SetAdditionalRingOperation()
        {
            if (_additionalRingOperation.IsValidOperation)
            {
                if(ReferenceEquals(_originalOperationCopy,null)) _originalOperationCopy = new StructureOperationModel(_additionalRingOperation);
                else _originalOperationCopy.UpdateStructureOperation(_additionalRingOperation);
            }
            CloseWindow();
        }

        private void CloseWindow()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
