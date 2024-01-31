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

    public SkiaSharp.SKColor GetMix(float fraction)
    {
        fraction = Math.Clamp(fraction, 0, 1);

        var r = (byte)(Main.Red + (Secondary.Red - Main.Red) * fraction);
        var g = (byte)(Main.Green + (Secondary.Green - Main.Green) * fraction);
        var b = (byte)(Main.Blue + (Secondary.Blue - Main.Blue) * fraction);
        var a = (byte)(Main.Alpha + (Secondary.Alpha - Main.Alpha) * fraction);

        return new SkiaSharp.SKColor(r, g, b, a);
    }
}
