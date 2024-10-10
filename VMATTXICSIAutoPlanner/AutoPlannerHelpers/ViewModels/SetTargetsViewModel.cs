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
        public List<PlanTargetsModel> PlanTargets { get => _planTargets; }

        #region properties
        private List<string> _targetIds;
        private List<string> _planIds;
        private List<PlanTargetsModel> _planTargets;

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
        private DelegateCommand _notifyMainVMExecuted;
        #endregion

        public SetTargetsViewModel(DelegateCommand NotifyMainVMExecuted)
        {
            _notifyMainVMExecuted = NotifyMainVMExecuted;
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
            _planTargets = GroupTargetsByPlanIdAndOrderByTargetRx(Targets.ToList());
            _notifyMainVMExecuted.Execute();
        }

        /// <summary>
        /// Helper method to take an ungrouped, unordered list of plan target models and first group them by plan Id, then order the targets by target prescription dose
        /// </summary>
        /// <param name="ungrouped"></param>
        /// <returns></returns>
        public List<PlanTargetsModel> GroupTargetsByPlanIdAndOrderByTargetRx(List<UnstructuredTargetModel> targets)
        {
            return targets.GroupBy(x => x.PlanId, (planId, groupedTargets) => new PlanTargetsModel(planId, ConvertUnstructuredTargetListToTargetModelList(groupedTargets))).ToList();
        }

        public List<TargetModel> ConvertUnstructuredTargetListToTargetModelList(IEnumerable<UnstructuredTargetModel> targets)
        {
            List<TargetModel> targetList = new List<TargetModel>();
            foreach(UnstructuredTargetModel target in targets)
            {
                targetList.Add(new TargetModel(target.TargetId, target.TargetRxDose));
            }
            return targetList;
        }
    }
}
