using TRAINer.Data;

namespace TRAINer.Geo;

class GPSToCanvas(int width, int height, int margin)
{
    public float? MinLat { get; private set; }
    public float? MaxLat { get; private set; }
    public float? MinAbsLat { get; private set; }

    public float? MinLon { get; private set; }
    public float? MaxLon { get; private set; }

    public int Margin { get; private set; } = margin;

    public int Width { get; private set; } = width - 2 * margin;

    public int Height { get; private set; } = height - 2 * margin;

    public int RealWidth { get; private set; } = width;

    public int RealHeight { get; private set; } = height;

    public void AddNode(Node node)
    {
        if (node.Latitude < MinLat || MinLat == null)
            MinLat = node.Latitude;
        if (node.Latitude > MaxLat || MaxLat == null)
            MaxLat = node.Latitude;

        var latitudeAbs = Math.Abs(node.Latitude);
        if (latitudeAbs < MinAbsLat || MinAbsLat == null)
            MinAbsLat = latitudeAbs;

        if (node.Longitude < MinLon || MinLon == null)
            MinLon = node.Longitude;
        if (node.Longitude > MaxLon || MaxLon == null)
            MaxLon = node.Longitude;

        var horizontalDistance = (float)(
            Math.Cos((double)MinAbsLat * Math.PI / 180)
            * 6371000
            * (MaxLon - MinLon)
            * Math.PI
            / 180
        );
        var verticalDistance = (float)(6371000 * (MaxLat - MinLat) * Math.PI / 180);

        Ratio = horizontalDistance / verticalDistance;

        if (_width < _height)
        {
            Width = (int)((_height - 2 * Margin) * (Ratio ?? 1));
            Height = (int)(_width - 2 * Margin);

            RealWidth = (int)(_height * (Ratio ?? 1));
            RealHeight = (int)_height;
        }
        else
        {
            Width = (int)(_width - 2 * Margin);
            Height = (int)((_width - 2 * Margin) / (Ratio ?? 1));

            RealWidth = (int)_width;
            RealHeight = (int)(_width / (Ratio ?? 1));
        }
    }

    public (float, float) Convert(Node node)
    {
        if (
            MinLat == null
            || MaxLat == null
            || MinAbsLat == null
            || MinLon == null
            || MaxLon == null
        )
        {
            throw new InvalidOperationException("Not all values are set");
        }

        var x = (float)((node.Longitude - MinLon) * Width / (MaxLon - MinLon) + Margin);

        // Y axis goes from top to bottom
        var y =
            RealHeight - (float)((node.Latitude - MinLat) * Height / (MaxLat - MinLat) + Margin);

        return (x, y);
    }

    private float? Ratio;

    private float _width = width;

    private float _height = height;
}
