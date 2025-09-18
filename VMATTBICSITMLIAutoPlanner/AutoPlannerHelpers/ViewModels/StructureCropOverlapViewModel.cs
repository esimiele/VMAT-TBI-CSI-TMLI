using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using AutoPlannerHelpers.Messengers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace AutoPlannerHelpers.ViewModels
{
    public class StructureCropOverlapViewModel : ObservableObject
    {
        public ObservableCollectionPropertyNotify<string> CropOverlapStructures { get; set; }
        public ObservableCollectionPropertyNotify<string> StructureIdsPostUnion { get; set; }
        
        #region fields
        private bool _skipStructureIdCheck = false;
        private List<string> _defaultCropOverlapStructures = new List<string> { };
        #endregion

        #region commands
        public ICommand AddCropOverlapStructureCommand { get; set; }
        public ICommand AddDefaultCropOverlapStructuresCommand { get; set; }
        public ICommand ClearCropOverlapStructureListCommand { get; set; }
        public RelayCommand<string> RemoveCropOverlapStructureCommand { get; set; }
        #endregion

        public StructureCropOverlapViewModel()
        {
            StructureIdsPostUnion = new ObservableCollectionPropertyNotify<string> { };
            CropOverlapStructures = new ObservableCollectionPropertyNotify<string> { };
            AddCropOverlapStructureCommand = new RelayCommand(AddCropOverlapStructure);
            AddDefaultCropOverlapStructuresCommand = new RelayCommand(AddDefaultCropOverlapStructures);
            ClearCropOverlapStructureListCommand = new RelayCommand(ClearCropOverlapStructureList);
            RemoveCropOverlapStructureCommand = new RelayCommand<string>(RemoveCropOverlapStructure);
            InitializeMessengers();
        }

        private void InitializeMessengers()
        {
            WeakReferenceMessenger.Default.Register<RequestUpdateCropOverlapStructures>(this, (r, m) =>
            {
                UpdateDefaultCropOverlapStructures(m.CropOverlapStructures, m.SkipStructureIdCheck);
            });
            WeakReferenceMessenger.Default.Register<RequestCropOverlapStructures>(this, (r, m) =>
            {
                m.Reply(CropOverlapStructures.ToList());
            });
            WeakReferenceMessenger.Default.Register<RequestUpdateStructureIds>(this, (r, m) =>
            {
                StructureIdsPostUnion.Clear();
                StructureIdsPostUnion.AddRange(m.StructureIds);
            });
        }

        public void AddCropOverlapStructure()
        {
            CropOverlapStructures.Add(StructureIdsPostUnion.FirstOrDefault());
        }

        public void UpdateDefaultCropOverlapStructures(List<string> cropOverlapStructures, bool skipStructureCheck)
        {
            _defaultCropOverlapStructures = new List<string>(cropOverlapStructures);
            _skipStructureIdCheck = skipStructureCheck;
            AddDefaultCropOverlapStructures();
        }

        public void AddDefaultCropOverlapStructures()
        {
            CropOverlapStructures.Clear();
            foreach (string itr in _defaultCropOverlapStructures)
            {
                if (_skipStructureIdCheck)
                {
                    if (!StructureIdsPostUnion.Any(x => string.Equals(x, itr, StringComparison.OrdinalIgnoreCase))) StructureIdsPostUnion.Add(itr);
                    CropOverlapStructures.Add(StructureIdsPostUnion.First(x => string.Equals(x, itr, StringComparison.OrdinalIgnoreCase)));
                }
                else if (StructureIdsPostUnion.Any(x => string.Equals(x, itr, StringComparison.OrdinalIgnoreCase)))
                {
                    CropOverlapStructures.Add(StructureIdsPostUnion.First(x => string.Equals(x, itr, StringComparison.OrdinalIgnoreCase)));
                }
            }
        }

        public void ClearCropOverlapStructureList()
        {
            CropOverlapStructures.Clear();
        }

        public void RemoveCropOverlapStructure(string item)
        {
            if (CropOverlapStructures.Contains(item)) CropOverlapStructures.Remove(item);
        }
    }
}
