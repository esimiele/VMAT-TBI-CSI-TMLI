using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoPlannerHelpers.BaseViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoPlannerHelpers.Models;
using AutoPlannerHelpers.Enums;
using AutoPlannerHelpers.PlanTemplateModels;
using AutoPlannerHelpersTests.EqualityComparers;
using AutoPlannerHelpersTests.BaseViewModel;
using VMS.TPS.Common.Model.Types;
using PlanType = AutoPlannerHelpers.Enums.PlanType;

namespace AutoPlannerHelpers.BaseViewModel.Tests
{
    [TestClass()]
    public class BaseViewModelTests
    {
        public List<OptimizationConstraintModel> SetupDummyInitialOptObjList()
        {
            //rx = 3600
            return new List<OptimizationConstraintModel>
            {
                new OptimizationConstraintModel("PTV_CSI", OptimizationObjectiveType.Lower, 3600, Units.cGy, 0.0, 100),
                new OptimizationConstraintModel("PTV_CSI", OptimizationObjectiveType.Upper, 3672, Units.cGy, 0.0, 100),
                new OptimizationConstraintModel("Brainstem", OptimizationObjectiveType.Upper, 3650, Units.cGy, 0.0, 80),
                new OptimizationConstraintModel ("Brainstem_PRV", OptimizationObjectiveType.Upper, 3650, Units.cGy, 0.0, 60),
                new OptimizationConstraintModel ("OpticChiasm", OptimizationObjectiveType.Upper, 3420, Units.cGy, 0.0, 80),
                new OptimizationConstraintModel ("OpticChiasm_PRV", OptimizationObjectiveType.Upper, 3420, Units.cGy, 0.0, 60),
                new OptimizationConstraintModel ("TS_cooler107", OptimizationObjectiveType.Upper, 3672.0, Units.cGy, 0.0, 80)
            };
        }

        public List<OptimizationConstraintModel> SetupDummyBoostOptObjList()
        {
            //rx = 3600
            return new List<OptimizationConstraintModel>
            {
                new OptimizationConstraintModel ("PTV_Boost", OptimizationObjectiveType.Lower, 1800, Units.cGy, 0.0, 100),
                new OptimizationConstraintModel ("PTV_Boost", OptimizationObjectiveType.Upper, 1850, Units.cGy, 0.0, 100),
                new OptimizationConstraintModel ("Brainstem", OptimizationObjectiveType.Upper, 1760, Units.cGy, 0.0, 80),
                new OptimizationConstraintModel ("Brainstem_PRV", OptimizationObjectiveType.Upper, 1760, Units.cGy, 0.0, 60),
                new OptimizationConstraintModel ("OpticChiasm", OptimizationObjectiveType.Upper, 1650, Units.cGy, 0.0, 80),
                new OptimizationConstraintModel ("OpticChiasm_PRV", OptimizationObjectiveType.Upper, 1650, Units.cGy, 0.0, 60),
                new OptimizationConstraintModel ("TS_cooler107", OptimizationObjectiveType.Upper, 1836, Units.cGy, 0.0, 80)
            };
        }

        public CSIAutoPlanTemplate ConstructTestCSIAutoPlanTemplate()
        {
            CSIAutoPlanTemplate template = new CSIAutoPlanTemplate("test");
            template.InitialRxNumberOfFractions = 20;
            template.InitialRxDosePerFx = 180.0;
            template.BoostRxNumberOfFractions = 10;
            template.BoostRxDosePerFx = 180.0;
            template.InitialOptimizationConstraints = new List<OptimizationConstraintModel>(SetupDummyInitialOptObjList());
            template.BoostOptimizationConstraints = new List<OptimizationConstraintModel>(SetupDummyBoostOptObjList());
            return template;
        }

        [TestMethod()]
        public void UpdateOptimizationConstraintsWithTSTargetsTest()
        {
            List<PlanTargetsModel> planTargets = new List<PlanTargetsModel>
            {
                new PlanTargetsModel("CSI-init", new List<TargetModel> {  new TargetModel("1", 1200), new TargetModel("2", 2400), new TargetModel("PTV_CSI", 3600, "TS_PTV_CSI")}),
                new PlanTargetsModel("CSI-bst", new List<TargetModel> {  new TargetModel("4", 4800), new TargetModel("PTV_Boost", 5400, "TS_PTV_Boost")})
            };

            CSIAutoPlanTemplate testTemplate = ConstructTestCSIAutoPlanTemplate();
            List<PlanOptimizationSetupModel> inputPlanOptimizationSetup = new List<PlanOptimizationSetupModel>
            {
                new PlanOptimizationSetupModel("CSI-init", testTemplate.InitialOptimizationConstraints),
                new PlanOptimizationSetupModel("CSI-bst", testTemplate.BoostOptimizationConstraints),
            };

            List<OptimizationConstraintModel> exepctedInitial = SetupDummyInitialOptObjList();
            foreach (OptimizationConstraintModel itr in exepctedInitial.Where(x => string.Equals(x.StructureId, "PTV_CSI"))) itr.StructureId = "TS_PTV_CSI";
            List<OptimizationConstraintModel> exepctedBoost = SetupDummyBoostOptObjList();
            foreach (OptimizationConstraintModel itr in exepctedBoost.Where(x => string.Equals(x.StructureId, "PTV_Boost"))) itr.StructureId = "TS_PTV_Boost";
            List<PlanOptimizationSetupModel> expectedPlanOptimizationSetup = new List<PlanOptimizationSetupModel>
            {
                new PlanOptimizationSetupModel("CSI-init", exepctedInitial),
                new PlanOptimizationSetupModel("CSI-bst", exepctedBoost),
            };

            OptimizationConstraintComparer comparer = new OptimizationConstraintComparer();
            DummyVM baseVM = new DummyVM(PlanType.None, new string[] { });
            inputPlanOptimizationSetup = baseVM.UpdateOptimizationConstraintsWithTSTargets(planTargets, inputPlanOptimizationSetup);

            Assert.AreEqual(inputPlanOptimizationSetup.Count, expectedPlanOptimizationSetup.Count);
            for (int i = 0; i < inputPlanOptimizationSetup.Count; i++)
            {
                Console.WriteLine($"{expectedPlanOptimizationSetup.ElementAt(i).PlanId} | {inputPlanOptimizationSetup.ElementAt(i).PlanId}");
                Assert.AreEqual(expectedPlanOptimizationSetup.ElementAt(i).PlanId, inputPlanOptimizationSetup.ElementAt(i).PlanId);
                Assert.AreEqual(expectedPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.Count, inputPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.Count);
                for (int j = 0; j < inputPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.Count; j++)
                {
                    Console.WriteLine($"{comparer.Print(expectedPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j))} | {comparer.Print(inputPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j))}");
                    Assert.IsTrue(comparer.Equals(expectedPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j), inputPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j)));
                }
                Console.WriteLine("");
            }
        }

        [TestMethod()]
        public void UpdateOptimizationConstraintsWithRingsTest()
        {
            CSIAutoPlanTemplate testTemplate = ConstructTestCSIAutoPlanTemplate();
            List<PlanOptimizationSetupModel> inputPlanOptimizationSetup = new List<PlanOptimizationSetupModel>
            {
                new PlanOptimizationSetupModel("CSI-init", testTemplate.InitialOptimizationConstraints),
                new PlanOptimizationSetupModel("CSI-bst", testTemplate.BoostOptimizationConstraints),
            };
            List<TSRingStructureModel> rings = new List<TSRingStructureModel>
            {
                new TSRingStructureModel("PTV_CSI", 1.5, 2.0, 1800),
                new TSRingStructureModel("PTV_Boost", 1.5, 2.0, 900),
            };

            List<PrescriptionModel> prescriptions = new List<PrescriptionModel>
            {
                new PrescriptionModel("CSI-init", "PTV_CSI", 20, new DoseValue(180, DoseValue.DoseUnit.cGy), 3600),
                new PrescriptionModel("CSI-bst", "PTV_Boost", 10, new DoseValue(180, DoseValue.DoseUnit.cGy), 5400),
            };
            DummyVM baseVM = new DummyVM(PlanType.None, new string[] { });
            baseVM.SetPrescriptions(prescriptions);

            OptimizationConstraintComparer comparer = new OptimizationConstraintComparer();
            List<OptimizationConstraintModel> exepctedInitial = SetupDummyInitialOptObjList();
            exepctedInitial.Add(new OptimizationConstraintModel("TS_ring1800", OptimizationObjectiveType.Upper, 1800, Units.cGy, 0.0, 80));
            List<OptimizationConstraintModel> exepctedBoost = SetupDummyBoostOptObjList();
            exepctedBoost.Add(new OptimizationConstraintModel("TS_ring900", OptimizationObjectiveType.Upper, 900, Units.cGy, 0.0, 80));
            List<PlanOptimizationSetupModel> expectedPlanOptimizationSetup = new List<PlanOptimizationSetupModel>
            {
                new PlanOptimizationSetupModel("CSI-init", exepctedInitial),
                new PlanOptimizationSetupModel("CSI-bst", exepctedBoost),
            };

            inputPlanOptimizationSetup = baseVM.UpdateOptimizationConstraintsWithRings(rings, inputPlanOptimizationSetup);

            Assert.AreEqual(inputPlanOptimizationSetup.Count, expectedPlanOptimizationSetup.Count);
            for (int i = 0; i < inputPlanOptimizationSetup.Count; i++)
            {
                Console.WriteLine($"{expectedPlanOptimizationSetup.ElementAt(i).PlanId} | {inputPlanOptimizationSetup.ElementAt(i).PlanId}");
                Assert.AreEqual(expectedPlanOptimizationSetup.ElementAt(i).PlanId, inputPlanOptimizationSetup.ElementAt(i).PlanId);
                Assert.AreEqual(expectedPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.Count, inputPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.Count);
                for (int j = 0; j < inputPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.Count; j++)
                {
                    Console.WriteLine($"{comparer.Print(expectedPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j))} | {comparer.Print(inputPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j))}");
                    Assert.IsTrue(comparer.Equals(expectedPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j), inputPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j)));
                }
                Console.WriteLine("");
            }
        }

        [TestMethod()]
        public void UpdateOptimizationConstraintsWithTSJunctionsTest()
        {
            CSIAutoPlanTemplate testTemplate = ConstructTestCSIAutoPlanTemplate();
            List<PlanOptimizationSetupModel> inputPlanOptimizationSetup = new List<PlanOptimizationSetupModel>
            {
                new PlanOptimizationSetupModel("CSI-init", testTemplate.InitialOptimizationConstraints),
                new PlanOptimizationSetupModel("CSI-bst", testTemplate.BoostOptimizationConstraints),
            };
            List<PlanFieldJunctionModel> junctions = new List<PlanFieldJunctionModel>
            {
                new PlanFieldJunctionModel("CSI-init", new List<FieldJunctionModel> { new FieldJunctionModel("TS_jnx1")}),
                new PlanFieldJunctionModel("CSI-init", new List<FieldJunctionModel> { new FieldJunctionModel("TS_jnx2")}),
            };

            List<PrescriptionModel> prescriptions = new List<PrescriptionModel>
            {
                new PrescriptionModel("CSI-init", "PTV_CSI", 20, new DoseValue(180, DoseValue.DoseUnit.cGy), 3600),
                new PrescriptionModel("CSI-bst", "PTV_Boost", 10, new DoseValue(180, DoseValue.DoseUnit.cGy), 5400),
            };
            DummyVM baseVM = new DummyVM(PlanType.None, new string[] { });
            baseVM.SetPrescriptions(prescriptions);

            OptimizationConstraintComparer comparer = new OptimizationConstraintComparer();
            List<OptimizationConstraintModel> exepctedInitial = SetupDummyInitialOptObjList();
            exepctedInitial.Add(new OptimizationConstraintModel("TS_jnx1", OptimizationObjectiveType.Lower, 3600, Units.cGy, 100.0, 100));
            exepctedInitial.Add(new OptimizationConstraintModel("TS_jnx1", OptimizationObjectiveType.Upper, 1.02 * 3600, Units.cGy, 0.0, 100));
            exepctedInitial.Add(new OptimizationConstraintModel("TS_jnx2", OptimizationObjectiveType.Lower, 3600, Units.cGy, 100.0, 100));
            exepctedInitial.Add(new OptimizationConstraintModel("TS_jnx2", OptimizationObjectiveType.Upper, 1.02 * 3600, Units.cGy, 0.0, 100));

            List<OptimizationConstraintModel> exepctedBoost = SetupDummyBoostOptObjList();
            List<PlanOptimizationSetupModel> expectedPlanOptimizationSetup = new List<PlanOptimizationSetupModel>
            {
                new PlanOptimizationSetupModel("CSI-init", exepctedInitial),
                new PlanOptimizationSetupModel("CSI-bst", exepctedBoost),
            };

            inputPlanOptimizationSetup = baseVM.UpdateOptimizationConstraintsWithTSJunctions(junctions, inputPlanOptimizationSetup);

            Assert.AreEqual(inputPlanOptimizationSetup.Count, expectedPlanOptimizationSetup.Count);
            for (int i = 0; i < inputPlanOptimizationSetup.Count; i++)
            {
                Console.WriteLine($"{expectedPlanOptimizationSetup.ElementAt(i).PlanId} | {inputPlanOptimizationSetup.ElementAt(i).PlanId}");
                Assert.AreEqual(expectedPlanOptimizationSetup.ElementAt(i).PlanId, inputPlanOptimizationSetup.ElementAt(i).PlanId);
                Assert.AreEqual(expectedPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.Count, inputPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.Count);
                for (int j = 0; j < inputPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.Count; j++)
                {
                    Console.WriteLine($"{comparer.Print(expectedPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j))} | {comparer.Print(inputPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j))}");
                    Assert.IsTrue(comparer.Equals(expectedPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j), inputPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j)));
                }
                Console.WriteLine("");
            }
        }

        [TestMethod()]
        public void BuildPlanOptimizationSetupListTest()
        {
            CSIAutoPlanTemplate testTemplate = ConstructTestCSIAutoPlanTemplate();
            List<PrescriptionModel> prescriptions = new List<PrescriptionModel>
            {
                new PrescriptionModel("CSI-init", "PTV_CSI", 20, new DoseValue(180, DoseValue.DoseUnit.cGy), 3600),
                new PrescriptionModel("CSI-bst", "PTV_Boost", 10, new DoseValue(180, DoseValue.DoseUnit.cGy), 5400),
            };
            DummyVM baseVM = new DummyVM(PlanType.VMAT_CSI, new string[] { });
            baseVM.SetPrescriptions(prescriptions);
            baseVM.SetSelectedTemplate(testTemplate);

            OptimizationConstraintComparer comparer = new OptimizationConstraintComparer();
            List<PlanOptimizationSetupModel> expectedPlanOptimizationSetup = new List<PlanOptimizationSetupModel>
            {
                new PlanOptimizationSetupModel("CSI-init", SetupDummyInitialOptObjList()),
                new PlanOptimizationSetupModel("CSI-bst",  SetupDummyBoostOptObjList()),
            };

            List<PlanOptimizationSetupModel> resultPlanOptimizationSetup = baseVM.BuildPlanOptimizationSetupList();

            Assert.AreEqual(resultPlanOptimizationSetup.Count, expectedPlanOptimizationSetup.Count);
            for (int i = 0; i < resultPlanOptimizationSetup.Count; i++)
            {
                Console.WriteLine($"{expectedPlanOptimizationSetup.ElementAt(i).PlanId} | {resultPlanOptimizationSetup.ElementAt(i).PlanId}");
                Assert.AreEqual(expectedPlanOptimizationSetup.ElementAt(i).PlanId, resultPlanOptimizationSetup.ElementAt(i).PlanId);
                Assert.AreEqual(expectedPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.Count, resultPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.Count);
                for (int j = 0; j < resultPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.Count; j++)
                {
                    Console.WriteLine($"{comparer.Print(expectedPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j))} | {comparer.Print(resultPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j))}");
                    Assert.IsTrue(comparer.Equals(expectedPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j), resultPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j)));
                }
                Console.WriteLine("");
            }

            baseVM.SetSelectedTemplate(null);

            expectedPlanOptimizationSetup = new List<PlanOptimizationSetupModel>
            {
                new PlanOptimizationSetupModel("CSI-init", new List<OptimizationConstraintModel>
                {
                    new OptimizationConstraintModel("PTV_CSI", OptimizationObjectiveType.Lower, 3600, Units.cGy, 100.0, 100),
                    new OptimizationConstraintModel("PTV_CSI", OptimizationObjectiveType.Upper, 3672, Units.cGy, 0.0, 100),
                }),
                new PlanOptimizationSetupModel("CSI-bst",  new List<OptimizationConstraintModel>
                {
                    new OptimizationConstraintModel ("PTV_Boost", OptimizationObjectiveType.Lower, 5400, Units.cGy, 100.0, 100),
                    new OptimizationConstraintModel ("PTV_Boost", OptimizationObjectiveType.Upper, 5508, Units.cGy, 0.0, 100),
                }),
            };

            resultPlanOptimizationSetup = baseVM.BuildPlanOptimizationSetupList();

            Assert.AreEqual(resultPlanOptimizationSetup.Count, expectedPlanOptimizationSetup.Count);
            for (int i = 0; i < resultPlanOptimizationSetup.Count; i++)
            {
                Console.WriteLine($"{expectedPlanOptimizationSetup.ElementAt(i).PlanId} | {resultPlanOptimizationSetup.ElementAt(i).PlanId}");
                Assert.AreEqual(expectedPlanOptimizationSetup.ElementAt(i).PlanId, resultPlanOptimizationSetup.ElementAt(i).PlanId);
                Assert.AreEqual(expectedPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.Count, resultPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.Count);
                for (int j = 0; j < resultPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.Count; j++)
                {
                    Console.WriteLine($"{comparer.Print(expectedPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j))} | {comparer.Print(resultPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j))}");
                    Assert.IsTrue(comparer.Equals(expectedPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j), resultPlanOptimizationSetup.ElementAt(i).OptimizationConstraints.ElementAt(j)));
                }
                Console.WriteLine("");
            }
        }
    }
}