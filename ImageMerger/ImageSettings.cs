using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ImageMerger
{
    [DataContract]
    public class ImageSettings
    {
        [DataMember] public string outputFileName { get; set; }
        [DataMember] public IList<SourceImageSettings> sourceImages { get; set; }
        [DataMember] public IList<ColorReplacementSettigns> colorReplacement { get; set; }
    }

    [DataContract]
    public class SourceImageSettings
    {
        [DataMember] public string fileName { get; set; }
        [DataMember] public float alphaValue { get; set; }
        [DataMember] public bool isShadow { get; set; }
        [DataMember] public IList<MaskInfo> maskInfo { get; set; }
    }

    [DataContract]
    public class MaskInfo
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
