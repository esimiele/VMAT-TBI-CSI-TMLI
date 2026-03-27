using AutoPlannerHelpers.Helpers;
using AutoPlannerOptimizationLoop.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace AutoPlannerOptimizationLoop.Helpers
{
    public static class BeamHelper
    {
        public static List<Tuple<Beam, VRect<double>>> ExtractJawPositionsFromPlan(ExternalPlanSetup p)
        {
            List<Tuple<Beam, VRect<double>>> originalJawPos = new List<Tuple<Beam, VRect<double>>> { };
            //closed MLCs inside the field --> copy current jaw positions
            foreach (Beam itr in p.Beams.Where(x => !x.IsSetupField))
            {
                originalJawPos.Add(Tuple.Create(itr, new VRect<double>(itr.ControlPoints.First().JawPositions.X1, itr.ControlPoints.First().JawPositions.Y1, itr.ControlPoints.First().JawPositions.X2, itr.ControlPoints.First().JawPositions.Y2)));
            }
            return originalJawPos;
        }

        /// <summary>
        /// Utility method to extract any static closed MLCs inside the field. Very dumb workaround to account for machines that don't
        /// have jaw tracking
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static List<Tuple<Beam, List<int>>> GetStaticMLCsInField(ExternalPlanSetup p)
        {
            List<Tuple<Beam, List<int>>> closedMLCs = new List<Tuple<Beam, List<int>>> { };
            foreach (Beam itr in p.Beams.Where(x => !x.IsSetupField))
            {
                Console.WriteLine(itr.Id);
                IEnumerable<ControlPointParameters> cpc = itr.GetEditableParameters().ControlPoints;
                double y1 = cpc.First().JawPositions.Y1;
                double y2 = cpc.First().JawPositions.Y2;

                List<int> closedMLCIndexes = new List<int> { };
                int indexY1 = GetY1Index(y1);
                Console.WriteLine($"{y1} | {indexY1}");
                for (int i = 29; i >= indexY1; i--)
                {
                    if (cpc.All(x => CalculationHelper.AreEqual(x.LeafPositions[0, i], cpc.First().LeafPositions[0, i]) && CalculationHelper.AreEqual(x.LeafPositions[1, i], cpc.First().LeafPositions[0, i])))
                    {
                        Console.WriteLine(i);
                        closedMLCIndexes.Add(i);
                    }
                }

                int indexY2 = GetY2Index(y2);
                Console.WriteLine($"{y2} | {indexY2}");

                for (int i = 30; i <= indexY2; i++)
                {
                    if (cpc.All(x => CalculationHelper.AreEqual(x.LeafPositions[0, i], cpc.First().LeafPositions[0, i]) && CalculationHelper.AreEqual(x.LeafPositions[1, i], cpc.First().LeafPositions[1, i])))
                    {
                        Console.WriteLine(i);
                        closedMLCIndexes.Add(i);
                    }
                }
                if (closedMLCIndexes.Any())
                {
                    closedMLCs.Add(Tuple.Create(itr, new List<int>(closedMLCIndexes.OrderBy(x => x))));
                }
                Console.WriteLine("");
            }
            return closedMLCs;
        }

        /// <summary>
        /// Convert the identified MLC indexes into jaw positions that will exclude all static closed MLCs from the field
        /// </summary>
        /// <param name="closedMLCs"></param>
        /// <returns></returns>
        public static List<Tuple<Beam, VRect<double>>> ConvertMLCIndexesToJawPos(List<Tuple<Beam, List<int>>> closedMLCs)
        {
            List<Tuple<Beam, VRect<double>>> jawPos = new List<Tuple<Beam, VRect<double>>> { };
            foreach (Tuple<Beam, List<int>> itr in closedMLCs)
            {
                Console.WriteLine($"Beam Id: {itr.Item1.Id}");
                BeamParameters bp = itr.Item1.GetEditableParameters();
                IEnumerable<ControlPointParameters> cpc = bp.ControlPoints;
                double x1Pos = cpc.First().JawPositions.X1;
                double x2Pos = cpc.First().JawPositions.X2;
                double y1Pos = cpc.First().JawPositions.Y1;
                double y2Pos = cpc.First().JawPositions.Y2;
                if (itr.Item2.Any(x => x <= 30))
                {
                    Console.WriteLine(itr.Item2.Last(x => x <= 30));
                    y1Pos = ConvertY1IndexToPosition(itr.Item2.Last(x => x <= 30) + 1);
                    Console.WriteLine($"y1 pos: {y1Pos}");
                }
                if (itr.Item2.Any(x => x >= 31))
                {
                    Console.WriteLine(itr.Item2.First(x => x >= 31));
                    y2Pos = ConvertY2IndexToPosition(itr.Item2.First(x => x >= 31) - 1);
                    Console.WriteLine($"y2 pos: {y1Pos}");
                }
                jawPos.Add(Tuple.Create(itr.Item1, new VRect<double>(x1Pos, y1Pos, x2Pos, y2Pos)));
            }
            Console.WriteLine("");
            return jawPos;
        }

        /// <summary>
        /// Update the jaw positions for each beam at each control point
        /// </summary>
        /// <param name="updateJawPos"></param>
        /// <returns></returns>
        public static bool AdjustJawPositionsForBeams(List<Tuple<Beam, VRect<double>>> updateJawPos)
        {
            foreach (Tuple<Beam, VRect<double>> itr in updateJawPos)
            {
                BeamParameters bp = itr.Item1.GetEditableParameters();
                foreach (ControlPointParameters cp in bp.ControlPoints)
                {
                    cp.JawPositions = itr.Item2;
                }
                itr.Item1.ApplyParameters(bp);
            }
            return false;
        }

        /// <summary>
        /// Convert an mlc index to a position in BEV coordinates (for Y1 jaw)
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static int GetY1Index(double pos)
        {
            if (OptimizationLoopSettings.MLCIndexesY1.Any(x => CalculationHelper.AreEqual(x.Item2, pos)))
            {
                return OptimizationLoopSettings.MLCIndexesY1.First(x => x.Item2 == pos).Item1;
            }
            else if (pos > -5.0) return OptimizationLoopSettings.MLCIndexesY1.Last().Item1;
            else return OptimizationLoopSettings.MLCIndexesY1.Last(x => x.Item2 < pos).Item1 + 1;
        }

        /// <summary>
        /// Convert an mlc index to a position in BEV coordinates (for Y2 jaw)
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static int GetY2Index(double pos)
        {
            if (OptimizationLoopSettings.MLCIndexesY2.Any(x => CalculationHelper.AreEqual(x.Item2, pos)))
            {
                return OptimizationLoopSettings.MLCIndexesY2.First(x => x.Item2 == pos).Item1;
            }
            else if (pos < 5.0) return OptimizationLoopSettings.MLCIndexesY2.First().Item1;
            else return OptimizationLoopSettings.MLCIndexesY2.First(x => x.Item2 > pos).Item1 - 1;
        }

        /// <summary>
        /// Convert a position in BEV coordinates to a MLC index (y1 direction)
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static double ConvertY1IndexToPosition(int index)
        {
            if (index > 29) return 0.0;
            return OptimizationLoopSettings.MLCIndexesY1.First(x => x.Item1 == index).Item2;
        }

        /// <summary>
        /// Convert a position in BEV coordinates to a MLC index (y2 direction)
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static double ConvertY2IndexToPosition(int index)
        {
            if (index < 30) return 0.0;
            return OptimizationLoopSettings.MLCIndexesY2.First(x => x.Item1 == index).Item2;
        }
    }
}
