using System.Collections.Generic;

namespace CTStitcher.Models
{
    public class CTImageModel
    {
        //get method
        public CTImageMetaDataModel MetaData { get; private set; }
        public IEnumerable<ImageSliceModel> Slices { get => _slices; }

        //get/set methods
        public VectorModel Origin { get; set; }

        //data members
        private List<ImageSliceModel> _slices;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data"></param>
        public CTImageModel(CTImageMetaDataModel data)
        {
            MetaData = data;
            _slices = new List<ImageSliceModel> { };
        }

        /// <summary>
        /// utility method to add a new image slice object to the stack of image slices currently belonging to this CT image
        /// </summary>
        /// <param name="slice"></param>
        /// <returns></returns>
        public bool AddImageSlice(ImageSliceModel slice)
        {
            if (VerifySliceIntegritry(slice)) _slices.Add(slice);
            else return true;
            //resort the slices as they are added
            if (MetaData.ScanOrientation == enums.ScanOrientation.HeadFirstSupine)
            {
                //slice z location goes from negative to positive(ascending sort)
                _slices.Sort((x, y) => x.SliceZLocation.CompareTo(y.SliceZLocation));
            }
            else if (MetaData.ScanOrientation == enums.ScanOrientation.FeetFirstSupine)
            {
                //slice z goes from positive to negative (descending sort)
                _slices.Sort((x, y) => y.SliceZLocation.CompareTo(x.SliceZLocation));
            }
            return false;
        }

        /// <summary>
        /// Helper method to update the metadata for this CT image
        /// </summary>
        /// <param name="data"></param>
        public void UpdateMetaData(CTImageMetaDataModel data)
        {
            MetaData = data;
        }

        /// <summary>
        /// Utility method to perform a 'deep copy' of another CT image
        /// </summary>
        /// <param name="numAdditionalSlices"></param>
        /// <returns></returns>
        public CTImageModel DeepCopy(int numAdditionalSlices)
        {
            CTImageModel other = (CTImageModel)MemberwiseClone();
            CTImageMetaDataModel data = new CTImageMetaDataModel();
            data = (CTImageMetaDataModel)other.MetaData;
            data.ZSize = data.ZSize + numAdditionalSlices;
            UpdateMetaData(data);
            return other;
        }

        /// <summary>
        /// Utility method to ensure the image slice integrity prior to adding it to the CT image
        /// </summary>
        /// <param name="slice"></param>
        /// <returns></returns>
        private bool VerifySliceIntegritry(ImageSliceModel slice)
        {
            if (slice.HasPixelData && slice.SliceZLocation != double.NaN) return true;
            else return false;
        }
    }
}
