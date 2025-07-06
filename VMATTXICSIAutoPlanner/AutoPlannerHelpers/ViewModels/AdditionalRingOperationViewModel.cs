using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AutoPlannerHelpers.ViewModels
{
    public class AdditionalRingOperationViewModel : ObservableObject
    {
        #region properties
        private StructureOperationModel _additionalRingOperation;

        public StructureOperationModel AdditionalRingOperation
        {
            get { return _additionalRingOperation; }
            set { SetProperty(ref _additionalRingOperation, value); }
        }

        private List<string> _structureIdsPostUnion;

        public List<string> StructureIdsPostUnion
        {
            get { return _structureIdsPostUnion; }
            set { SetProperty(ref _structureIdsPostUnion, value); }
        }
        #endregion

        #region commands
        public ICommand SetAdditionalRingOperationCommand { get; set; }
        public RelayCommand<(string, StructureMarginModel)> ModifyMarginCommand { get; set; }
        #endregion

        public AdditionalRingOperationViewModel(string ringId, StructureOperationModel op, List<string> structureIds)
        {
            StructureIdsPostUnion = new List<string>(structureIds);
            if(ReferenceEquals(op, null) || !op.IsValidOperation)
            {
                AdditionalRingOperation = new StructureOperationModel(ringId, Enums.StructureDerivationOperation.None, _structureIdsPostUnion.First(), ringId);
            }
            else
            {
                AdditionalRingOperation = op;
            }
            SetAdditionalRingOperationCommand = new RelayCommand(SetAdditionalRingOperation);
            ModifyMarginCommand = new RelayCommand<(string, StructureMarginModel)>(ModifyMargin, CanModifyMargin);
        }

        private void ModifyMargin((string structureid, StructureMarginModel model) parameters)
        {
            ModifyMarginView view = new ModifyMarginView { DataContext = new ModifyMarginViewModel(parameters.structureid, parameters.model) };
            view.ShowDialog();
        }

        private bool CanModifyMargin((string structureid, StructureMarginModel model) parameters)
        {
            return !string.IsNullOrEmpty(parameters.structureid) && !ReferenceEquals(parameters.model, null);
        }

        private void SetAdditionalRingOperation()
        {
            
        }
    }
}
