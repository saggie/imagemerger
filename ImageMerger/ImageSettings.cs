using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ImageMerger
{
    [DataContract]
    public class ImageSettings
    {
        [DataMember] public string id { get; set; }
        [DataMember] public string outputFileName { get; set; }
        [DataMember] public IList<SourceImageSettings> sourceImages { get; set; }
        [DataMember] public IList<ColorReplacementSettigns> colorReplacement { get; set; }
        [DataMember] public bool autoSaveAndExit { get; set; }
        [DataMember] public bool autoGrayScaling { get; set; }
    }

    [DataContract]
    public class SourceImageSettings
    {
        [DataMember] public string fileName { get; set; }
        [DataMember] public AlphaSettings alpha { get; set; }
        [DataMember] public bool isShadow { get; set; }
        [DataMember] public IList<RegionMaskInfo> regionMask { get; set; }
    }

    [DataContract]
    public class AlphaSettings
    {
        [DataMember] public float value { get; set; }
        [DataMember] public string[] ignoreList { get; set; }
        [DataMember] public bool excludeMask { get; set; }
    }

    [DataContract]
    public class RegionMaskInfo
    {
       [DataMember] public string targetColor { get; set; }
       [DataMember] public int margin { get; set; }
    }

    [DataContract]
    public class ColorReplacementSettigns
    {
        [DataMember] public string from { get; set; }
        [DataMember] public string to { get; set; }
    }
}
