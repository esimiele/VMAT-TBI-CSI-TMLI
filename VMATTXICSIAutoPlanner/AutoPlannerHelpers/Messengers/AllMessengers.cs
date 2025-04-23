using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.PlanTemplateModels;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace AutoPlannerHelpers.Messengers
{
    public class RequestGenerateShiftNoteMessage : RequestMessage<bool> { }
    public class RequestSeparatePlanMessage : RequestMessage<bool> { }
    public class RequestSetTargetsMessage 
    { 
        public List<PlanTargetsModel> PlanTargets { get; private set; }
        public RequestSetTargetsMessage(List<PlanTargetsModel> planTargets)
        {
            this.PlanTargets = planTargets;
        }
    }
    public class RequestGenerateManipulateTuningStructuresMessage : RequestMessage<bool> { }
    public class RequestPlaceBeamsMessage : RequestMessage<bool> { }
    public class RequestUpdateStructureIds
    {
        public List<string> StructureIds { get; private set; }
        public RequestUpdateStructureIds(IEnumerable<string> structureIds) { this.StructureIds = structureIds.ToList(); }
    }
    public class RequestSetOptimizationConstraintsMessage
    {
        public List<PlanOptimizationSetupModel> PlanOptimizationSetup { get; private set; }
        public RequestSetOptimizationConstraintsMessage(IEnumerable<PlanOptimizationSetupModel> constraints) { this.PlanOptimizationSetup = constraints.ToList(); }
    }

    public class RequestUpdateOptimizationConstraintsMessage
    {
        public List<PlanOptimizationSetupModel> PlanOptimizationSetup { get; private set; }
        public RequestUpdateOptimizationConstraintsMessage(IEnumerable<PlanOptimizationSetupModel> constraints) { this.PlanOptimizationSetup = constraints.ToList(); }
    }

    public class RequestAutoPlanTemplateChangedMessage
    {
        public AutoPlanTemplateBase AutoPlanTemplate { get; private set; }
        public RequestAutoPlanTemplateChangedMessage(AutoPlanTemplateBase autoPlanTemplate) { this.AutoPlanTemplate = autoPlanTemplate; }
    }
}
