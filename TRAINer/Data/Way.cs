using OsmSharp.Tags;

namespace TRAINer.Data;

public class Way
{
    public long Id { get; }
    public long[] Nodes { get; }
    public Dictionary<string, string> Tags { get; protected set; }
    public Way(long id, long[] nodes, TagsCollectionBase? tags)
    {
        Id = id;
        Nodes = nodes;
        Tags = [];
    }
}
