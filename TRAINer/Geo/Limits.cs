namespace TRAINer.Geo;

public class Limits(float minLat, float maxLat, float minLon, float maxLon)
{
    public float MinLat = minLat;

    public float MaxLat = maxLat;

    public float MinLon = minLon;

    public float MaxLon = maxLon;

    public bool IsInside(float lat, float lon)
    {
        return lat >= MinLat && lat <= MaxLat && lon >= MinLon && lon <= MaxLon;
    }
}
