using Microsoft.ML.Data;

namespace ZefsjulaApi.Models
{
    public class TextData
    {
        public string Input { get; set; } = string.Empty;
    }

    public class TextFeatures
    {
        [VectorType]
        public float[] Features { get; set; } = Array.Empty<float>();
    }
}
