using System.Collections.Generic;
using System.Linq;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.Prompts;
using AutoPlannerHelpers.PlanTemplateModels;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AutoPlannerHelpers.Messengers;

namespace AutoPlannerHelpers.ViewModels
{
    public class SetTargetsViewModel : ObservableObject
    {

        #region properties
        public ObservableCollectionPropertyNotify<UnstructuredTargetModel> Targets { get; set; }
        public ObservableCollectionPropertyNotify<string> TargetIds { get; set; }
        public ObservableCollectionPropertyNotify<string> PlanIds { get; set; }

        #endregion

        #region fields
        private List<PlanTargetsModel> _planTargets = new List<PlanTargetsModel> { };
        #endregion

        #region commands
        public RelayCommand AddTargetCommand { get; set; }
        public ICommand AddDefaultTargetsCommand { get; set; }
        public ICommand RemoveAllTargetsCommand { get; set; }
        public RelayCommand<UnstructuredTargetModel> ClearRowCommand { get; set; }
        public ICommand SetTargetsCommand { get; set; }
        public RelayCommand<UnstructuredTargetModel> SelectedTargetChangedCommand { get; set; }
        public RelayCommand<UnstructuredTargetModel> SelectedPlanChangedCommand { get; set; }
        #endregion

        public SetTargetsViewModel()
        {
            TargetIds = new ObservableCollectionPropertyNotify<string> { " ", "--Add New--" };
            PlanIds = new ObservableCollectionPropertyNotify<string> { " ", "--Add New--" };
            Targets = new ObservableCollectionPropertyNotify<UnstructuredTargetModel> { };
            AddTargetCommand = new RelayCommand(AddEmptyTarget);
            AddDefaultTargetsCommand = new RelayCommand(AddDefaultTargets);
            RemoveAllTargetsCommand = new RelayCommand(RemoveAllTargets);
            ClearRowCommand = new RelayCommand<UnstructuredTargetModel>(ClearRow);
            SetTargetsCommand = new RelayCommand(SetTargets);
            SelectedTargetChangedCommand = new RelayCommand<UnstructuredTargetModel>(TargetIdSelectionChanged);
            SelectedPlanChangedCommand = new RelayCommand<UnstructuredTargetModel>(PlanIdSelectionChanged);
            InitializeMessengers();
        }

        private void InitializeMessengers()
        {
            WeakReferenceMessenger.Default.Register<RequestUpdatePlanTargetsList>(this, (r, m) =>
            {
                UpdateDefaultPlanTargetsList(m.PlanTargets);
            });
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

        public void UpdateDefaultPlanTargetsList(List<PlanTargetsModel> planTargets)
        {
            _planTargets = new List<PlanTargetsModel>(planTargets);
            AddDefaultTargets();
        }

        public void AddDefaultTargets()
        {
            TargetIds.Clear();
            PlanIds.Clear();
            TargetIds.Add("--Add New--");
            foreach (string itr in _planTargets.SelectMany(x => x.Targets).Select(x => x.TargetId)) TargetIds.Add(itr);
            PlanIds.Add("--Add New--");
            PlanIds.AddRange(_planTargets.Select(x => x.PlanId));
            Targets.Clear();
            foreach (PlanTargetsModel itr in _planTargets) Targets.Add(new UnstructuredTargetModel(itr));
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
            WeakReferenceMessenger.Default.Send(new RequestSetTargetsMessage(GroupTargetsByPlanIdAndOrderByTargetRx(Targets.ToList())));
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
