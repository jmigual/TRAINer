using OsmSharp.Tags;

namespace TRAINer.Data;

public class Node
{
    public float Latitude { get; }
    public float Longitude { get; }

    public string RailWay { get; } = "";

    public static readonly string[] AcceptedTags = ["railway"];

    public Node(float latitude, float longitude, TagsCollectionBase? tags)
    {
        Latitude = latitude;
        Longitude = longitude;

        if (tags == null)
        {
            return;
        }
        if (tags.TryGetValue("railway", out var value))
        {
            RailWay = value;
        }
    }
}
