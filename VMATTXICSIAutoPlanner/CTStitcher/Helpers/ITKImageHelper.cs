using CTStitcher.Delegates;
using CTStitcher.Models;
using CTStitcher.Utilities;

namespace CTStitcher.Helpers
{
    public static class ITKImageHelper
    {
        /// <summary>
        /// Helper method to take the supplied CT image object and convert it to an itk image object
        /// </summary>
        /// <param name="img"></param>
        /// <param name="ProvideUIUpdate"></param>
        /// <returns></returns>
        public static itk.simple.Image ConvertCTImageToItkImage(CTImageModel img, ProvideUIUpdateDelegate ProvideUIUpdate)
        {
            int percentComplete = 0;
            int calcItems = 4 + img.MetaData.ZSize;
            ProvideUIUpdate(0, $"Constructing new itk image from CT image: {img.MetaData.Id}");
            ProvideUIUpdate((int)(100 * ++percentComplete / calcItems), $"Building itk image meta data...");
            itk.simple.PixelIDValueEnum pixelType = itk.simple.PixelIDValueEnum.sitkInt16;
            itk.simple.VectorUInt32 image3DSize = new itk.simple.VectorUInt32(new uint[] { (uint)img.MetaData.XSize, (uint)img.MetaData.YSize, (uint)img.MetaData.ZSize });
            itk.simple.Image itkImage = new itk.simple.Image(image3DSize, pixelType);
            itk.simple.VectorDouble spacing3D = new itk.simple.VectorDouble(new double[] { img.MetaData.XRes, img.MetaData.YRes, img.MetaData.ZRes });
            itkImage.SetSpacing(spacing3D);
            itk.simple.VectorDouble origin = new itk.simple.VectorDouble(new double[] { img.Origin.X, img.Origin.Y, img.Origin.Z });
            itkImage.SetOrigin(origin);
            itkImage.SetDirection(new itk.simple.VectorDouble(new double[3, 3]
            {
                { img.MetaData.ImageOrientation.X, 0, 0 },
                { 0, img.MetaData.ImageOrientation.Y, 0 },
                { 0, 0, img.MetaData.ImageOrientation.Z }
            }));
            ProvideUIUpdate((int)(100 * ++percentComplete / calcItems), $"Finished constructing itk image meta data");

            ProvideUIUpdate((int)(100 * ++percentComplete / calcItems), $"Total {img.MetaData.ZSize} image slices to process.");
            itk.simple.VectorUInt32 index = new itk.simple.VectorUInt32(new uint[] { 0, 0, 0 });
            uint sliceNum = 0;
            foreach (ImageSliceModel slice in img.Slices)
            {
                ProvideUIUpdate((int)(100 * ++percentComplete / calcItems), $"Processing slice: {sliceNum}");
                index[2] = sliceNum;
                for (uint x = 0; x < img.MetaData.XSize; x++)
                {
                    index[0] = x;
                    for (uint y = 0; y < img.MetaData.YSize; y++)
                    {
                        index[1] = y;
                        itkImage.SetPixelAsInt16(index, slice.PixelData[x, y]);
                    }
                }
                sliceNum++;
            }
            ProvideUIUpdate(100, $"Finished building itk image");
            return itkImage;
        }

        /// <summary>
        /// Helper method to convert a single image slice to an itk image (assigned to the itk image that was supplied as an argument)
        /// </summary>
        /// <param name="slice"></param>
        /// <param name="itkImage"></param>
        /// <param name="sliceNum"></param>
        /// <returns></returns>
        public static itk.simple.Image ConvertCTSliceToItkImage(ImageSliceModel slice, itk.simple.Image itkImage, uint sliceNum)
        {
            itk.simple.VectorUInt32 index = new itk.simple.VectorUInt32(new uint[] { 0, 0, 0 });
            index[2] = sliceNum;
            for (uint x = 0; x < slice.PixelData.GetLength(0); x++)
            {
                index[0] = x;
                for (uint y = 0; y < slice.PixelData.GetLength(1); y++)
                {
                    index[1] = y;
                    itkImage.SetPixelAsInt16(index, slice.PixelData[x, y]);
                }
            }
            return itkImage;
        }

        /// <summary>
        /// Helper method to convert an itk image back to a CT image (all converted data is assigned to the CTImage that was supplied as an argument)
        /// </summary>
        /// <param name="merged"></param>
        /// <param name="stitchedImage"></param>
        /// <param name="ProvideUIUpdate"></param>
        /// <returns></returns>
        public static CTImageModel ConvertItkImageToCTImage(itk.simple.Image merged, CTImageModel stitchedImage, ProvideUIUpdateDelegate ProvideUIUpdate)
        {
            ProvideUIUpdate(0, $"Converting itk image to CT image: {stitchedImage.MetaData.Id}");
            itk.simple.VectorUInt32 index = new itk.simple.VectorUInt32(new uint[] { 0, 0, 0 });
            ProvideUIUpdate(0, "Converting image slice data now");
            stitchedImage.MetaData.ZSize = (int)merged.GetSize()[2];
            for (uint z = 0; z < merged.GetSize()[2]; z++)
            {
                short[,] newPixelData = new short[stitchedImage.MetaData.XSize, stitchedImage.MetaData.YSize];
                index[2] = z;
                for (uint x = 0; x < stitchedImage.MetaData.XSize; x++)
                {
                    index[0] = x;
                    for (uint y = 0; y < stitchedImage.MetaData.YSize; y++)
                    {
                        index[1] = y;
                        newPixelData[x, y] = merged.GetPixelAsInt16(index);
                    }
                }
                ImageSliceModel newSlice = new ImageSliceModel(newPixelData);
                double targetSliceLocation = stitchedImage.Origin.Z + z * stitchedImage.MetaData.ZRes * stitchedImage.MetaData.ImageOrientation.Z;
                newSlice.Origin = new VectorModel(stitchedImage.Origin.X, stitchedImage.Origin.Y, targetSliceLocation);
                newSlice.SliceZLocation = targetSliceLocation;
                stitchedImage.AddImageSlice(newSlice);
                ProvideUIUpdate((int)(100 * z / merged.GetSize()[2]), $"Added slice {z} to CT image");
            }
            return stitchedImage;
        }
    }
}
