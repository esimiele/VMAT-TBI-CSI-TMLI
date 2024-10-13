using System;

namespace CTStitcher.Utilities
{
    public static class UIDHelper
    {
        /// <summary>
        /// Utility method to generate a new UID for the stitched CT image
        /// Code taken from: https://github.com/Varian-MedicalAffairsAppliedSolutions/MAAS-TXIhelper/tree/main/MAAS-TXIHelper
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public static string MakeNewUID(string uid)
        {
            string newUID = "";
            string timeTag = DateTime.Now.ToString("yyMMddHHmmssff");
            string[] segments = uid.Split('.');
            if (segments.Length > 5)
            {
                segments[segments.Length - 2] = timeTag;
            }
            else
            {
                segments[segments.Length - 1] = timeTag;
            }
            foreach (string segment in segments)
            {
                newUID += segment + '.';
            }
            newUID = newUID.Substring(0, newUID.Length - 1);
            if (segments.Length == 5)
            {
                return newUID;
            }
            if (segments.Length == 6 && newUID.Length > 64)
            {
                int deltaLength = newUID.Length - 64;
                segments[5] = segments[deltaLength];
                foreach (string segment in segments)
                {
                    newUID += segment + '.';
                }
                newUID = newUID.Substring(0, newUID.Length - 1);
                return newUID;
            }
            // truncate some digits since the UID length needs to be no more than 64.
            if (segments.Length > 6 && newUID.Length > 64)  
            {
                segments = newUID.Split('.');
                int deltaLength = 64 - newUID.Length;
                string root = "";
                for (int i = 0; i < 4; i++)
                {
                    root += segments[i] + ".";
                }
                string mid = "";
                for (int i = 4; i < segments.Length - 2; i++)
                {
                    mid += segments[i] + '.';
                }
                string tail = "";
                for (int i = segments.Length - 2; i < segments.Length; i++)
                {
                    tail += segments[i] + '.';
                }
                tail = tail.Substring(0, tail.Length - 1);
                mid = mid.Substring(0, mid.Length - 1);
                if (mid.Length > deltaLength)
                {
                    mid = mid.Substring(0, mid.Length - deltaLength);
                }
                mid += '.';
                mid = mid.Replace("..", ".");
                newUID = root + mid + tail;
            }
            return newUID;
        }
    }
}
