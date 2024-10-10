using AutoPlannerHelpers.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Prism.Mvvm;
using AutoPlannerHelpers.PlanTemplateModels;

namespace AutoPlannerHelpers.ViewModels
{
    public class TSGenerationViewModel : BindableBase
    {
        public ObservableCollectionPropertyNotify<RequestedTSStructureModel> RequestedTuningStructures { get; set; }
        public ObservableCollectionPropertyNotify<string> TSStructureIds { get; set; }

        #region properties
        private AutoPlanTemplateBase _selectedTemplate;
        private List<string> _dicomTypes;
        private List<string> _tsStructureIds;

        public List<string> DicomTypes
        {
            get { return _dicomTypes; }
            set { _dicomTypes = value; }
        }

        #endregion

        #region commands
        public DelegateCommand DisplayInfoCommand { get; set; }
        public DelegateCommand AddTSStructureCommand { get; set; } 
        public DelegateCommand AddDefaultTSStructuresCommand { get; set; }
        public DelegateCommand RemoveAllTSStructuresCommand { get; set; }
        #endregion

        public TSGenerationViewModel()
        {
            DicomTypes = new List<string> { "AVOIDANCE",
                                            "CAVITY",
                                            "CONTRAST_AGENT",
                                            "CTV",
                                            "EXTERNAL",
                                            "GTV",
                                            "IRRAD_VOLUME",
                                            "ORGAN",
                                            "PTV",
                                            "TREATED_VOLUME",
                                            "SUPPORT",
                                            "FIXATION",
                                            "CONTROL",
                                            "DOSE_REGION" };
            TSStructureIds = new ObservableCollectionPropertyNotify<string> { };
            RequestedTuningStructures = new ObservableCollectionPropertyNotify<RequestedTSStructureModel> { };
            DisplayInfoCommand = new DelegateCommand(DisplayTSGenerationInfo);
            AddTSStructureCommand = new DelegateCommand(AddTSStructure);
            AddDefaultTSStructuresCommand = new DelegateCommand(AddDefaultTSStructures);
            RemoveAllTSStructuresCommand = new DelegateCommand(RemoveAllTSStructures);
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

        public void AddTSStructure()
        {
            RequestedTuningStructures.Add(new RequestedTSStructureModel(_dicomTypes.First(), _tsStructureIds.First()));
        }

        public void AutoPlanTemplateSelectionChaged(AutoPlanTemplateBase template)
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

        public void UpdateViewWithAutoPlanTemplateTSStructures()
        {
            TSStructureIds.Clear();
            foreach(string itr in _selectedTemplate.CreateTSStructures.Select(x => x.StructureId)) TSStructureIds.Add(itr);
            RequestedTuningStructures.Clear();
            foreach (RequestedTSStructureModel itr in _selectedTemplate.CreateTSStructures) RequestedTuningStructures.Add(itr);
            //TargetIds.Add("--Add New--");
            //foreach (string itr in _selectedTemplate.PlanTargets.SelectMany(x => x.Targets).Select(x => x.TargetId)) TargetIds.Add(itr);
            //PlanIds.Add("--Add New--");
            //PlanIds.AddRange(_selectedTemplate.PlanTargets.Select(x => x.PlanId));
            //Targets.Clear();
            //foreach (PlanTargetsModel itr in _selectedTemplate.PlanTargets) Targets.Add(new UnstructuredTargetModel(itr));
        }

        private void RemoveAllTSStructures()
        {
            RequestedTuningStructures.Clear();
        }
    }
}
