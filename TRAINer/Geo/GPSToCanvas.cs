using TRAINer.Data;

namespace TRAINer;

class GPSTToCanvas
{
    public float MinLat { get; private set; }
    public float MaxLat { get; private set; }
    public float MinAbsLat { get; private set; }

    public float MinLon { get; private set; }
    public float MaxLon { get; private set; }

    public void AddNode(Node node)
    {
        if (node.Latitude < MinLat)
            MinLat = node.Latitude;
        if (node.Latitude > MaxLat)
            MaxLat = node.Latitude;

        var latitudeAbs = Math.Abs(node.Latitude);
        if (latitudeAbs < MinAbsLat)
            MinAbsLat = latitudeAbs;

        if (node.Longitude < MinLon)
            MinLon = node.Longitude;
        if (node.Longitude > MaxLon)
            MaxLon = node.Longitude;
    }
}
