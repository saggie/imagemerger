using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ImageMerger
{
    [DataContract]
    public class ImageSettings
    {
        [DataMember] public string outputFileName { get; set; }
        [DataMember] public IList<SourceImageInfo> sourceImages { get; set; }
    }

    [DataContract]
    public class SourceImageInfo
    {
        [DataMember] public string fileName { get; set; }
        [DataMember] public int margin { get; set; }
        [DataMember] public float alpha { get; set; }
        [DataMember] public bool isShadow { get; set; }
    }
}
