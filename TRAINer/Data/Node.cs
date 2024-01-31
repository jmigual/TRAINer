using OsmSharp.Tags;

namespace TRAINer.Data;

public class Node
{
    public float Latitude { get; }
    public float Longitude { get; }

    public Node(float latitude, float longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }
}
