using CTStitcher.Models;
using System;
using System.Text;

namespace CTStitcher.UIHelpers
{
    public class CTStitcherUIHelper
    {
        /// <summary>
        /// Write the CT metadata properties to the UI log textblock
        /// </summary>
        /// <param name="data"></param>
        /// <param name="origin"></param>
        /// <param name="tb"></param>
        public static string FormatCTMetaDataForUILog(CTImageMetaDataModel data, VectorModel origin)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CT meta data:");
            sb.AppendLine($"    Id: {data.Id}");
            sb.AppendLine($"    Imaging device manfacturer: {data.ImagingDeviceManufacturer}");
            sb.AppendLine($"    Imaging device model: {data.ImagingDeviceModel}");
            sb.AppendLine($"    Imaging device S/N: {data.ImagingDeviceSerialNumber}");
            sb.AppendLine($"    Scan orientation: {data.ScanOrientation}");
            sb.AppendLine($"    Image orientation: ({data.ImageOrientation.X}, {data.ImageOrientation.Y}, {data.ImageOrientation.Z})");
            sb.AppendLine($"    Image origin: ({origin.X}, {origin.Y}, {origin.Z})");
            sb.AppendLine($"    XSize: {data.XSize}");
            sb.AppendLine($"    YSize: {data.YSize}");
            sb.AppendLine($"    ZSize: {data.ZSize}");
            sb.AppendLine($"    X resolution: {data.XRes} mm");
            sb.AppendLine($"    Y resolution: {data.YRes} mm");
            sb.AppendLine($"    Z resolution: {data.ZRes} mm");
            sb.AppendLine($"    Rescale slope: {data.RescaleSlope} HU/pixel num");
            sb.AppendLine($"    Rescale intercept: {data.RescaleIntercept} HU");
            sb.AppendLine($"    FOR: {data.FOR}");
            sb.AppendLine($"    Series UID: {data.SeriesUID}");
            sb.AppendLine($"    Study UID: {data.StudyUID}");
            return sb.ToString();
        }

        /// <summary>
        /// Write the properties of the registration++ class to the UI log textblock
        /// </summary>
        /// <param name="rpp"></param>
        /// <param name="tb"></param>
        public static string FormatRegisitrationPPDataForUILog(RegistrationPPModel rpp)
        {
            StringBuilder sb = new StringBuilder();
            FormatCTMetaDataForUILog(rpp.TargetImage.MetaData, rpp.TargetImage.Origin);
            FormatCTMetaDataForUILog(rpp.SourceImage.MetaData, rpp.SourceImage.Origin);
            sb.AppendLine("RegistrationPP data:");
            sb.AppendLine($"    Id: {rpp.Id}");
            sb.AppendLine($"    Source Image: {rpp.SourceImage.MetaData.Id}");
            sb.AppendLine($"    Target Image: {rpp.TargetImage.MetaData.Id}");
            sb.AppendLine($"    Transform matrix:");
            string msg = "";
            for (int i = 0; i < 4; i++)
            {
                msg += $"        |";
                for (int j = 0; j < 4; j++)
                {
                    msg += string.Format(" {0:0.000000}", rpp.TransformMatrix[i, j]);
                }
                msg += " |";
                if (i != 3) msg += Environment.NewLine;
            }
            sb.AppendLine(msg);
            sb.AppendLine($"    Has rotations: {rpp.HasRotations}");
            if (rpp.HasRotations)
            {
                sb.AppendLine($"    Rotations:");
                sb.AppendLine($"        Theta X: {rpp.Rotations.X} rad");
                sb.AppendLine($"        Theta Y: {rpp.Rotations.Y} rad");
                sb.AppendLine($"        Theta Z: {rpp.Rotations.Z} rad");
            }
            sb.AppendLine($"    Translations:");
            sb.AppendLine($"        X: {rpp.Translations.X} mm");
            sb.AppendLine($"        Y: {rpp.Translations.Y} mm");
            sb.AppendLine($"        Z: {rpp.Translations.Z} mm");
            return sb.ToString();
        }
    }
}
