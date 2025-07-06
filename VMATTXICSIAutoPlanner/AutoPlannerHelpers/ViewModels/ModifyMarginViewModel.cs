using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using System.Windows.Input;

namespace AutoPlannerHelpers.ViewModels
{
    public class ModifyMarginViewModel : ObservableObject
    {
        #region properties
        public string StructureId { get; set; }
        private StructureMarginModel _structureMargin;
        private StructureMarginModel _originalMarginCopy;

        public StructureMarginModel StructureMargin
        {
            get { return _structureMargin; }
            set { SetProperty(ref _structureMargin, value); }
        }

        private Visibility _asymItemsVisible;

        public Visibility AsymItemsVisible
        {
            get { return _asymItemsVisible; }
            set { SetProperty(ref _asymItemsVisible, value); }
        }
        #endregion

        #region commands
        public RelayCommand<StructureMarginType> MarginTypeChangedCommand { get; set; }
        public ICommand SetMarginCommand { get; set; }
        #endregion

        #region events
        public event EventHandler RequestClose;
        #endregion

        public ModifyMarginViewModel(string structureId, StructureMarginModel model)
        {
            StructureId = structureId;
            StructureMargin = new StructureMarginModel(model);
            _originalMarginCopy = model;
            if (model.MarginType == StructureMarginType.Uniform) AsymItemsVisible = Visibility.Collapsed;
            else AsymItemsVisible = Visibility.Visible;
            MarginTypeChangedCommand = new RelayCommand<StructureMarginType>(MarginTypeChanged);
            SetMarginCommand = new RelayCommand(SetMargin);
        }

        private void MarginTypeChanged(StructureMarginType value)
        {
            if (value == StructureMarginType.Uniform) AsymItemsVisible = Visibility.Collapsed;
            else AsymItemsVisible = Visibility.Visible;
        }

        private void SetMargin()
        {
            if(_structureMargin.IsValidMargin) _originalMarginCopy.UpdateMargin(_structureMargin);
            CloseWindow();
        }

        private void CloseWindow()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
