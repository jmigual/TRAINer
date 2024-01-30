using OsmSharp.Tags;

namespace TRAINer.Data;

public class Node
{
    public long Id { get; }
    public double Latitude { get; }
    public double Longitude { get; }
    public Dictionary<string, string> Tags { get; }

    public static readonly string[] AcceptedTags = ["railway"];

    public Node(long id, double latitude, double longitude, TagsCollectionBase? tags)
    {
        Id = id;
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
