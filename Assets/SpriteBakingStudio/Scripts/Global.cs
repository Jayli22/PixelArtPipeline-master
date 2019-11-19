namespace SBS
{
    public enum BlendFactor
    {
        Zero,
        One,
        SrcColor,
        OneMinusSrcColor,
        DstColor,
        OneMinusDstColor,
        SrcAlpha,
        OneMinusSrcAlpha,
        DstAlpha,
        OneMinusDstAlpha
    }
    
    public class TextureBound
    {
        public int minX, maxX, minY, maxY;

        public TextureBound()
        {
            minX = int.MaxValue;
            maxX = int.MinValue;
            minY = int.MaxValue;
            maxY = int.MinValue;
        }
    };

    public class Global
    {
        public const string HELP_BOX_STYLE = "HelpBox";

        public const int WIDE_BUTTON_HEIGHT = 22;
        public const int WIDE_BUTTON_FONT_SIZE = 12;

        public const int MIDDLE_BUTTON_WIDTH = 200;
        public const int MIDDLE_BUTTON_HEIGHT = 18;
        public const int MIDDLE_BUTTON_FONT_SIZE = 11;

        public const int NARROW_BUTTON_HEIGHT = 16;
        public const int NARROW_BUTTON_FONT_SIZE = 10;

        public const string TILES_OBJECT_NAME = "QuarterViewTiles";

        public const string SIMPLE_SHADOW_NAME = "SimpleShadow";
        public const string STATIC_SHADOW_NAME = "StaticShadow";
        public const string DYNAMIC_SHADOW_NAME = "DynamicShadow";

        public const string DEFAULT_EXTRACTOR_NAME = "DefaultExtractor";
    }
}
