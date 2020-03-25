namespace IK.Imager.Api.Configuration
{
    public class ImageLimitationSettings
    {
        public Range Width { get; set; }
        public Range Height { get; set; }
        public Range Size { get; set; }
        public string[] Types { get; set; }
    }

    public class Range
    {
        public int? Min { get; set; }
        public int? Max { get; set; }
    }
}