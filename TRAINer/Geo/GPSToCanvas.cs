using TRAINer.Data;

namespace TRAINer.Geo;

class GPSToCanvas(int width, int height)
{
    public float? MinLat { get; private set; }
    public float? MaxLat { get; private set; }
    public float? MinAbsLat { get; private set; }

    public float? MinLon { get; private set; }
    public float? MaxLon { get; private set; }

    public int Width
    {
        get
        {
            // We always grow the smallest dimension unless they are the same. Then we keep the
            // original width.
            if (_width <= _height)
            {
                return (int)(_height * (Ratio ?? 1));
            }
            return (int)_width;
        }
    }

    public int Height
    {
        get
        {
            if (_width > _height)
            {
                return (int)(_width / (Ratio ?? 1));
            }
            return (int)_height;
        }
    }

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

        var x = (float)((node.Longitude - MinLon) * Width / (MaxLon - MinLon));
        var y = Height - (float)((node.Latitude - MinLat) * Height / (MaxLat - MinLat));

        return (x, y);
    }

    private float? Ratio
    {
        get
        {
            if (
                MinLon == null
                || MaxLon == null
                || MinAbsLat == null
                || MinLat == null
                || MaxLat == null
            )
            {
                return null;
            }

            var horizontalDistance = (float)(
                Math.Cos((double)MinAbsLat * Math.PI / 180)
                * 6371000
                * (MaxLon - MinLon)
                * Math.PI
                / 180
            );
            var verticalDistance = (float)(6371000 * (MaxLat - MinLat) * Math.PI / 180);

            return horizontalDistance / verticalDistance;
        }
    }

    private float _width = width;

    private float _height = height;
}
