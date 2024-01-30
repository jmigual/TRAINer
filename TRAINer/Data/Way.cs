using OsmSharp.Tags;

namespace TRAINer.Data;

public class Way
{
    public long Id { get; }
    public long[] Nodes { get; }
    public Dictionary<string, string> Tags { get; }

    public static readonly string[] StoreTags = ["railway"];

    public float Speed { get; } = 0;

    public float Gauge { get; } = 0;

    public float Weight {
        get {
            // This way, we can distinguish visually between narrow gauge and standard gauge
            // as well as between high speed and low speed lines
            return 1 + Speed / 300 + Gauge / 1400;
        }
    }

    public Way(long id, long[] nodes, TagsCollectionBase? tags)
    {
        Id = id;
        Nodes = nodes;
        Tags = [];

        if (tags == null)
        {
            return;
        }

        if (tags.TryGetValue("gauge", out var gauge))
        {
            if (float.TryParse(gauge, out var gaugeValue))
            {
                Gauge = gaugeValue;
            }
        }

        if (tags.TryGetValue("maxspeed", out var maxSpeed))
        {
            if (float.TryParse(maxSpeed, out var speedValue))
            {
                Speed = speedValue;
            }
        }

        foreach (var acceptedTag in StoreTags)
        {
            if (tags.TryGetValue(acceptedTag, out var value))
            {
                Tags.Add(acceptedTag, value);
            }
        }
    }
}
