using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using Prism.Mvvm;

namespace TBIAutoPlanner.ViewModels
{
    public class MainViewModel : BindableBase
    {
        private ObservableCollection<TBIAutoPlanTemplate> PlanTemplates { get; set; }

        #region properties
        private string _patientMRN;
        private List<string> _structureSetIds;
        private double _dosePerFraction;
        private int _numberOfFractions;
        private double _planTotalDose;
        private TBIAutoPlanTemplate _selectedTemplate;

        public string PatientMRN
        {
            get { return _patientMRN; }
            set { SetProperty(ref _patientMRN, value); }
        }

        public List<string> StructureSetIds
        {
            get { return _structureSetIds; }
            set { SetProperty(ref _structureSetIds, value); }
        }

        public double DosePerFraction
        {
            get { return _dosePerFraction; }
            set { SetProperty(ref _dosePerFraction, value); ResetRxDose(); }
        }

        public int NumberOfFractions
        {
            get { return _numberOfFractions; }
            set { SetProperty(ref _numberOfFractions, value); ResetRxDose(); }
        }

        public double PlanTotalDose
        {
            get { return _planTotalDose; }
            set { SetProperty(ref _planTotalDose, value); }
        }

        public TBIAutoPlanTemplate SelectedTemplate
        {
            get { return _selectedTemplate; }
            set { SetProperty(ref _selectedTemplate, value); UpdateUIWithSelectedPlanTemplate(); }
        }

        #endregion

        #region commands
        #endregion

        public MainViewModel(List<string> args)
        {
            PlanTemplates = new ObservableCollection<TBIAutoPlanTemplate> { };
            Initialize();
        }

        public void Initialize()
        {
            PlanTemplates.Clear();
        }

        private void ResetRxDose()
        {
            if (NumberOfFractions > 0 && DosePerFraction > 0)
            {
                //double priorTotalDose = PlanTotalDose;
                PlanTotalDose = DosePerFraction * NumberOfFractions;
                //if (PlanTotalDose != priorTotalDose)
                //{
                //    foreach (PlanObjectiveModel itr in PlanObjectives)
                //    {
                //        if (itr.QueryDoseUnits == Units.cGy)
                //        {
                //            itr.QueryDose = Math.Round(itr.QueryDose * PlanTotalDose / priorTotalDose, 1);
                //        }
                //    }
                //    PlanObjectives.Refresh();
                //    foreach (OptimizationConstraintModel itr in OptimizationConstraints)
                //    {
                //        if (itr.QueryDoseUnits == Units.cGy)
                //        {
                //            itr.QueryDose = Math.Round(itr.QueryDose * PlanTotalDose / priorTotalDose, 1);
                //        }
                //    }
                //    OptimizationConstraints.Refresh();
                //}
            }
        }

        private void UpdateUIWithSelectedPlanTemplate()
        {
            if (ReferenceEquals(SelectedTemplate, null)) return;

            TBIAutoPlanTemplate template = SelectedTemplate as TBIAutoPlanTemplate;
            DosePerFraction = template.InitialRxDosePerFx;
            NumberOfFractions = template.InitialRxNumberOfFractions;
            //PlanObjectives.Clear();
            //foreach (PlanObjectiveModel itr in template.PlanObjectives)
            //{
            //    PlanObjectives.Add(new PlanObjectiveModel(itr));
            //}
            //OptimizationConstraints.Clear();
            //foreach (OptimizationConstraintModel itr in template.InitialOptimizationConstraints)
            //{
            //    OptimizationConstraints.Add(new OptimizationConstraintModel(itr));
            //}
        }
    }
}
