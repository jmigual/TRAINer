namespace TRAINer.Config;

public class ColorPalette
{
    public SkiaSharp.SKColor Main { get; set; }

    public SkiaSharp.SKColor Secondary { get; set; }

    public SkiaSharp.SKColor Background { get; set; }

    public ColorPalette(string main, string secondary, string background)
    {
        Main = SkiaSharp.SKColor.Parse(main);
        Secondary = SkiaSharp.SKColor.Parse(secondary);
        Background = SkiaSharp.SKColor.Parse(background);
    }
}
