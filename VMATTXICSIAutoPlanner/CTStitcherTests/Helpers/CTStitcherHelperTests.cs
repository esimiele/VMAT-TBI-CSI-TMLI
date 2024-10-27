using CTStitcher.Models;
using I = itk.simple;
using Telerik.JustMock;

namespace CTStitcher.Helpers.Tests
{
    [TestClass()]
    public class CTStitcherHelperTests
    {
        [TestMethod()]
        public void CalculateNumberOfNewSlicesTestCTImage()
        {
            RegistrationPPModel rpp = BuildMockRegistrationPPObj();
            double expected = 148;
            double result = CTStitcherHelper.CalculateNumberOfNewSlices(rpp.TargetImage, rpp.SourceImage, rpp.TransformZ(rpp.SourceImage.Origin));
            Assert.AreEqual(expected, result, 0.01);
        }

        public RegistrationPPModel BuildMockRegistrationPPObj()
        {
            CTImageMetaDataModel tgtdata = new CTImageMetaDataModel();
            tgtdata.ImageOrientation = new VectorModel(1, 1, 1);
            tgtdata.ZSize = 100;
            tgtdata.ZRes = 3;
            CTImageModel target = new CTImageModel(tgtdata);
            target.Origin = new VectorModel(300, 0, 0);

            CTImageMetaDataModel srcdata = new CTImageMetaDataModel();
            srcdata.ImageOrientation = new VectorModel(-1, 1, -1);
            srcdata.ZSize = 50;
            srcdata.ZRes = 3;
            CTImageModel source = new CTImageModel(srcdata);
            source.Origin = new VectorModel(-300, 0, -50);

            double[,] transformMatrix = new double[4, 4] { { 1,0,0, 0},
                                                            { 0,1,0,0},
                                                            { 0,0,1,50},
                                                            { 0, 0, 0, 1} };

            return new RegistrationPPModel(target, source, transformMatrix);
        }

        [TestMethod()]
        public void CalculateNewZOriginTest()
        {
            RegistrationPPModel rpp = BuildMockRegistrationPPObj();
            double newNumSlices = CTStitcherHelper.CalculateNumberOfNewSlices(rpp.TargetImage, rpp.SourceImage, rpp.TransformZ(rpp.SourceImage.Origin));
            double expected = -144.0;
            double result = CTStitcherHelper.CalculateNewZOrigin(rpp.TargetImage.Origin.Z, rpp.TargetImage.MetaData.ZRes, rpp.TargetImage.MetaData.ZSize, newNumSlices);
            Assert.AreEqual(expected, result, 0.01);
        }

        [TestMethod()]
        public void CalculateNumberOfNewSlicesTestItkImage()
        {
            I.Image target = Mock.Create<I.Image>();
            I.VectorDouble tgtorigin = new I.VectorDouble(new double[] { 300, 0, 0 });
            I.VectorUInt32 tgtsize = new I.VectorUInt32(new uint[] { 0, 0, 100 });
            I.VectorDouble tgtres = new I.VectorDouble(new double[] { 0, 0, 3 });
            Mock.Arrange(() => target.GetOrigin()).Returns(tgtorigin);
            Mock.Arrange(() => target.GetSize()).Returns(tgtsize);
            Mock.Arrange(() => target.GetSpacing()).Returns(tgtres);

            I.Image transformedsource = Mock.Create<I.Image>();
            I.VectorDouble srcorigin = new I.VectorDouble(new double[] { -300, 0, 0 });
            I.VectorUInt32 srcsize = new I.VectorUInt32(new uint[] { 0, 0, 50 });
            I.VectorDouble srcres = new I.VectorDouble(new double[] { 0, 0, 3 });
            Mock.Arrange(() => transformedsource.GetOrigin()).Returns(srcorigin);
            Mock.Arrange(() => transformedsource.GetSize()).Returns(srcsize);
            Mock.Arrange(() => transformedsource.GetSpacing()).Returns(srcres);

            double expected = 148;
            double result = CTStitcherHelper.CalculateNumberOfNewSlices(target, transformedsource, -1);
            Assert.AreEqual(expected, result, 0.01);
        }
    }
}