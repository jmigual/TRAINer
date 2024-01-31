using OsmSharp.Tags;

namespace TRAINer.Data;

public class Node
{
    public float Latitude { get; }
    public float Longitude { get; }
    public Dictionary<string, string> Tags { get; }

    public static readonly string[] AcceptedTags = ["railway"];

    public Node(float latitude, float longitude, TagsCollectionBase? tags)
    {
        Latitude = latitude;
        Longitude = longitude;
        Tags = [];

        if (tags == null)
        {
            return;
        }

        foreach (var acceptedTag in AcceptedTags)
        {
            if (tags.TryGetValue(acceptedTag, out var value))
            {
                Tags.Add(acceptedTag, value);
            }
        }
    }
}
