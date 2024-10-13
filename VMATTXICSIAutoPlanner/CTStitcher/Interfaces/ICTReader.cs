using CTStitcher.Models;

namespace CTStitcher.Interfaces
{
    public interface ICTReader
    {
        public CTImageModel CT { get; }
        public string ErrorMessage { get; }

        public void SetImageToRead<T>(T image);
    }
}
