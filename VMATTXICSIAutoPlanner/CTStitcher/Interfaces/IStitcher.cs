using CTStitcher.Models;

namespace CTStitcher.Interfaces
{
    /// <summary>
    /// Simple interface that each stitching algorithm can derive from
    /// </summary>
    public interface IStitcher
    {
        public CTImageModel StitchedCT { get; }
        public string ErrorMessage { get; }
        public int MatchSlice { get; }

        public CTImageModel StitchCTImages();
    }
}
