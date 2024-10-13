using CTStitcher.enums;
using CTStitcher.Stitchers;
using CTStitcher.Interfaces;
using SimpleProgressWindow;
using System.Text;
using CTStitcher.Models;

namespace CTStitcher.Runners
{
    public class CTStitcherRunner
    {
        //get methods
        public CTImageModel StitchedCT { get; private set; }
        public int MatchSlice { get; private set; } = 0;
        public StringBuilder LogOutput { get; private set; } = new StringBuilder();
        public string ErrorMessages { get; private set; }

        /// <summary>
        /// Helper method to controls the flow of stitching the source and target images together
        /// </summary>
        /// <returns></returns>
        public bool StitchCTImages(RegistrationPPModel reg, StitchingAlgorithm sa)
        {
            //stitch the CTs together
            IStitcher stitcher;
            if (sa == StitchingAlgorithm.TranslationOnly)
            {
                stitcher = new Stitcher3DOF(reg);
            }
            else if (sa == StitchingAlgorithm.TranslationAndRotations)
            {
                stitcher = new Stitcher6DOF(reg);
            }
            else
            {
                ErrorMessages = "Error! Requested Stitching algorithm not recognized! Exiting!";
                return true;
            }
            if((stitcher as SimpleMTbase).Execute())
            {
                ErrorMessages = stitcher.ErrorMessage;
                return true;
            }
            StitchedCT = stitcher.StitchedCT;
            MatchSlice = stitcher.MatchSlice;
            LogOutput.Append((stitcher as SimpleMTbase).GetLogOutput());
            return false;
        }
    }
}
