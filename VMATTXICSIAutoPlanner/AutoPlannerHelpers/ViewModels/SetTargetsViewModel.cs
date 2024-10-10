using System.Collections.Generic;
using System.Linq;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.Prompts;
using AutoPlannerHelpers.PlanTemplateModels;
using Prism.Commands;
using Prism.Mvvm;

namespace AutoPlannerHelpers.ViewModels
{
    public class SetTargetsViewModel : BindableBase
    {
        public ObservableCollectionPropertyNotify<UnstructuredTargetModel> Targets { get; set; }
        public ObservableCollectionPropertyNotify<string> TargetIds { get; set; }
        public List<PlanTargetsModel> PlanTargets { get => _planTargets; }

        #region properties
        private List<string> _targetIds;
        private List<string> _planIds;
        private List<PlanTargetsModel> _planTargets;
        private AutoPlanTemplateBase _selectedTemplate;

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
        public DelegateCommand<UnstructuredTargetModel> SelectedTargetChangedCommand { get; set; }
        public DelegateCommand<UnstructuredTargetModel> SelectedPlanChangedCommand { get; set; }
        private DelegateCommand _notifyMainVMExecuted;
        #endregion

        public SetTargetsViewModel(DelegateCommand NotifyMainVMExecuted)
        {
            _notifyMainVMExecuted = NotifyMainVMExecuted;
            TargetIds = new ObservableCollectionPropertyNotify<string> { "--Add New--", "red", "green", "blue" };
            PlanIds = new List<string> { "--Add New--","1", "2", "3" };
            Targets = new ObservableCollectionPropertyNotify<UnstructuredTargetModel>
            {
                new UnstructuredTargetModel("1", "green", 10),
                new UnstructuredTargetModel("2", "red", 5)
            };
            AddTargetCommand = new DelegateCommand(AddEmptyTarget);
            AddDefaultTargetsCommand = new DelegateCommand(AddDefaultTargets);
            RemoveAllTargetsCommand = new DelegateCommand(RemoveAllTargets);
            ClearRowCommand = new DelegateCommand<UnstructuredTargetModel>(ClearRow);
            SetTargetsCommand = new DelegateCommand(SetTargets);
            SelectedTargetChangedCommand = new DelegateCommand<UnstructuredTargetModel>(TargetIdSelectionChanged);
            SelectedPlanChangedCommand = new DelegateCommand<UnstructuredTargetModel>(PlanIdSelectionChanged);
        }

        public void TargetIdSelectionChanged(UnstructuredTargetModel value)
        {
            if (ReferenceEquals(value, null)) return;
            string id = value.TargetId;
            if(string.Equals(id, "--Add New--"))
            {
                string msg = "Enter the Id of the target structure!";
                EnterMissingInfoPrompt EMIP = new EnterMissingInfoPrompt(msg, "Id:");
                EMIP.ShowDialog();
                if (EMIP.GetSelection && !TargetIds.Contains(EMIP.EnteredValue))
                {
                    TargetIds.Add(EMIP.EnteredValue);
                    value.TargetId = TargetIds.Last();
                    Targets.Refresh();
                }
            }
        }

        public void PlanIdSelectionChanged(UnstructuredTargetModel value)
        {
            if (ReferenceEquals(value, null)) return;
            string id = value.PlanId;
            if (string.Equals(id, "--Add New--"))
            {
                string msg = "Enter the Id of the new plan!";
                EnterMissingInfoPrompt EMIP = new EnterMissingInfoPrompt(msg, "Id:");
                EMIP.ShowDialog();
                if (EMIP.GetSelection && !PlanIds.Contains(EMIP.EnteredValue))
                {
                    PlanIds.Add(EMIP.EnteredValue);
                    value.PlanId = PlanIds.Last();
                    Targets.Refresh();
                }
            }
        }

        public void AutoPlanTemplateSelectionChaged(AutoPlanTemplateBase template)
        {
            if(ReferenceEquals(template, null)) return;
            _selectedTemplate = template;
            UpdateViewWithAutoPlanTemplateTargets();
        }

        public void AddDefaultTargets()
        {
            if(ReferenceEquals(_selectedTemplate, null)) return;
            UpdateViewWithAutoPlanTemplateTargets();
        }

        public void UpdateViewWithAutoPlanTemplateTargets()
        {
            TargetIds.Clear();
            PlanIds.Clear();
            TargetIds.Add("--Add New--");
            foreach(string itr in _selectedTemplate.PlanTargets.SelectMany(x => x.Targets).Select(x => x.TargetId)) TargetIds.Add(itr);
            PlanIds.Add("--Add New--");
            PlanIds.AddRange(_selectedTemplate.PlanTargets.Select(x => x.PlanId));
            Targets.Clear();
            foreach (PlanTargetsModel itr in _selectedTemplate.PlanTargets) Targets.Add(new UnstructuredTargetModel(itr));
        }

        public void AddEmptyTarget()
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

        public IEnumerable<TargetModel> ConvertUnstructuredTargetListToTargetModelList(IEnumerable<UnstructuredTargetModel> targets)
        {
            List<TargetModel> targetList = new List<TargetModel>();
            foreach(UnstructuredTargetModel target in targets)
            {
                targetList.Add(new TargetModel(target.TargetId, target.TargetRxDose));
            }
            return targetList.OrderBy(x => x.TargetRxDose);
        }
    }
}
