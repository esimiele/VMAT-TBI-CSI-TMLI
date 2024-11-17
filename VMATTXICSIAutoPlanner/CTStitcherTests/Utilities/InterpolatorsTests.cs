using CTStitcher.Models;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTStitcher.Utilities.Tests
{
    [TestClass()]
    public class InterpolatorsTests
    {
        [TestMethod()]
        public void LinearInterpolationTest()
        {
            double x0 = 0;
            double x1 = 10;
            double y0 = 10;
            double y1 = 20;
            double x = 3;

            double expected = 13;
            double result = Interpolators.LinearInterpolation(x0, x1, y0, y1, x);
            Console.WriteLine($"{expected} | {result}");
            Assert.AreEqual(expected, result, 0.001);
        }

        [TestMethod()]
        public void TriLinearInterpolationTest()
        {
            CubeModel c = new CubeModel(0, 1, 0, 1);
            c.c000 = new Vector4DModel(10, 10, 10, 20);
            c.c100 = new Vector4DModel(11, 10, 10, 40);
            c.c010 = new Vector4DModel(10, 11, 10, 60);
            c.c110 = new Vector4DModel(11, 10, 10, 80);
            c.c001 = new Vector4DModel(10, 10, 11, 100);
            c.c101 = new Vector4DModel(11, 10, 11, 120);
            c.c011 = new Vector4DModel(10, 11, 11, 140);
            c.c111 = new Vector4DModel(11, 11, 11, 160);

            VectorModel targetPos = new VectorModel(10.4, 10.4, 10.4);
            //c00 = 10.4,10,10,28
            //c01 = 10.4,10,11,108
            //c10 = 10.4,11,10,68
            //c11 = 10.4,11,11,148

            //c0 = 10.4,10.4,10,44
            //c1 = 10.4,10.4,11,124

            //result =10.4,10.4,10.4,76

            short expected = 76;
            short result = Interpolators.TriLinearInterpolation(c, targetPos, -1024);
            Console.WriteLine($"{expected} | {result}");
            Assert.AreEqual(expected, result, 0.001);
        }
    }
}