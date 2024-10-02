using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using Prism.Commands;
using Prism.Mvvm;

namespace AutoPlannerHelpers.ViewModels
{
    public class SetTargetsViewModel : BindableBase
    {
        public ObservableCollectionPropertyNotify<UnstructuredTargetModel> Targets { get; set; }

        #region properties
        private List<string> _targetIds;
        private List<string> _planIds;

        public List<string> TargetIds
        {
            get { return _targetIds; }
            set { SetProperty(ref _targetIds, value); }
        }

        public List<string> PlanIds
        {
            get { return _planIds; }
            set { SetProperty(ref _planIds, value); }
        }

        #endregion

        #region commands
        public DelegateCommand AddTargetCommand { get; set; }
        public DelegateCommand AddDefaultTargetsCommand { get; set; }
        public DelegateCommand RemoveAllTargetsCommand { get; set; }
        public DelegateCommand<UnstructuredTargetModel> ClearRowCommand { get; set; }
        public DelegateCommand SetTargetsCommand { get; set; }
        #endregion

        public SetTargetsViewModel()
        {
            TargetIds = new List<string> { "red", "green", "blue" };
            PlanIds = new List<string> { "1", "2", "3" };
            Targets = new ObservableCollectionPropertyNotify<UnstructuredTargetModel>
            {
                new UnstructuredTargetModel("1", "green", 10),
                new UnstructuredTargetModel("2", "red", 5)
            };
            AddTargetCommand = new DelegateCommand(AddTarget);
            RemoveAllTargetsCommand = new DelegateCommand(RemoveAllTargets);
            ClearRowCommand = new DelegateCommand<UnstructuredTargetModel>(ClearRow);
            SetTargetsCommand = new DelegateCommand(SetTargets);
        }

        public void AutoPlanTemplateSelectionChaged(AutoPlanTemplateBase template)
        {
            if(ReferenceEquals(template, null)) return;
            Targets.Clear();
        }

        public void AddTarget()
        {
            Targets.Add(new UnstructuredTargetModel(PlanIds.First(), TargetIds.First(), 0.0));
        }

        public void RemoveAllTargets()
        {
            Targets.Clear();
        }

        public void ClearRow(UnstructuredTargetModel o)
        {
            Targets.Remove(o);
        }

        public void SetTargets()
        {

        }
    }
}
