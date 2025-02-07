using AutoPlannerHelpers.Models;
using System.Text;
using System.Windows;
using AutoPlannerHelpers.PlanTemplateModels;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace AutoPlannerHelpers.ViewModels
{
    public class TSGenerationViewModel : ObservableObject
    {
        public ObservableCollectionPropertyNotify<RequestedTSStructureModel> RequestedTuningStructures { get; set; }

        #region properties
        private AutoPlanTemplateBase _selectedTemplate;
        #endregion

        #region commands
        public ICommand DisplayInfoCommand { get; set; }
        public ICommand AddDefaultTSStructuresCommand { get; set; }
        public ICommand RemoveAllTSStructuresCommand { get; set; }
        public RelayCommand<RequestedTSStructureModel> ClearRowCommand { get; set; }
        #endregion

        public TSGenerationViewModel()
        {
            RequestedTuningStructures = new ObservableCollectionPropertyNotify<RequestedTSStructureModel> { };
            DisplayInfoCommand = new RelayCommand(DisplayTSGenerationInfo);
            AddDefaultTSStructuresCommand = new RelayCommand(AddDefaultTSStructures);
            RemoveAllTSStructuresCommand = new RelayCommand(RemoveAllTSStructures);
            ClearRowCommand = new RelayCommand<RequestedTSStructureModel>(ClearRow);
        }

        private void DisplayTSGenerationInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("What's the difference between TS structure generation vs manipulation?");
            sb.AppendLine("TS structure generation involves adding structures to the structure set to shape the dose distribution. These include rings, preliminary targets, etc. E.g.,");
            sb.AppendLine("TS_ring900  -->  ring structure around the targets using a nominal dose level of 900 cGy to determine fall-off");
            sb.AppendLine("PTV_Spine  -->  preliminary target used to aid physician contouring of the final target that will be approved");
            sb.AppendLine("TS structure manipulation involves manipulating/modifying the structure itself or target structures. E.g.,");
            sb.AppendLine("(Ovaries, Crop target from structure, 1.5cm)  -->  modify the target structure such that the ovaries structure is cropped from the target with a 1.5 cm margin");
            sb.AppendLine("(Brainstem, Contour overlap, 0.0 cm)  -->  Identify the overlapping regions between the brainstem and target structure(s) and contour them as new structures");
            sb.AppendLine("Kidneys-1cm  -->  substructure for the Kidneys volume where the Kidneys are contracted by 1 cm");
            MessageBox.Show(sb.ToString());
        }

        public void AutoPlanTemplateSelectionChanged(AutoPlanTemplateBase template)
        {
            if (ReferenceEquals(template, null)) return;
            _selectedTemplate = template;
            UpdateViewWithAutoPlanTemplateTSStructures();
        }

        public void AddDefaultTSStructures()
        {
            if (ReferenceEquals(_selectedTemplate, null)) return;
            UpdateViewWithAutoPlanTemplateTSStructures();
        }

        public void ClearRow(RequestedTSStructureModel o)
        {
            RequestedTuningStructures.Remove(o);
        }

        public void UpdateViewWithAutoPlanTemplateTSStructures()
        {
            RequestedTuningStructures.Clear();
            foreach (RequestedTSStructureModel itr in _selectedTemplate.CreateTSStructures) RequestedTuningStructures.Add(itr);
        }

        private void RemoveAllTSStructures()
        {
            RequestedTuningStructures.Clear();
        }
    }
}
