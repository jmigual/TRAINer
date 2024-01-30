using System.CommandLine;
using OsmSharp.Streams;
using TRAINer.Data;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var dataOption = new Option<DirectoryInfo?>(
            name: "--data",
            description: "Location of the directory where the OpenStreetMap data is stored", parseArgument: result =>
            {
                // Get current directory
                string path;

                if (result.Tokens.Count == 0)
                {
                    path = Path.Combine(Environment.CurrentDirectory, "data");
                    Console.Error.WriteLine($"Using default location of {path}");
                }
                else
                {
                    path = result.Tokens.Single().Value;
                }

                if (!Directory.Exists(path))
                {
                    result.ErrorMessage = $"Directory {path} does not exist";
                    return null;
                }

                return new DirectoryInfo(path);
            }, isDefault: true);
        var rootCommand = new RootCommand("Sample app for System.CommandLine");
        rootCommand.AddOption(dataOption);

        rootCommand.SetHandler((data) =>
        {
            if (data == null)
            {
                Console.Error.WriteLine("No data directory specified");
                return;
            }
            Process(data);
        }, dataOption);
        return await rootCommand.InvokeAsync(args);
    }

    internal static void Process(DirectoryInfo dataFolder)
    {
        // Find raw rails file. You can generate it from the OSM data using osmium
        // osmium tags-filter railway -o rails.osm.pbf planet-latest.osm.pbf "r/admin_level=2,4" --output-format pbf,add_metadata=false
        var file = new FileInfo(Path.Combine(dataFolder.FullName, "raw", "rails.osm.pbf"));

        if (!file.Exists)
        {
            Console.Error.WriteLine($"Missing raw rails file {file.FullName}");
            return;
        }

        var (nodes, ways) = GetData(file);

        // Now we need to paint the data
    }

    internal static (Dictionary<long, Node> nodes, Way[] ways) GetData(FileInfo file)
    {
        Dictionary<long, Node> nodes = [];
        List<Way> ways = [];

        var source = new PBFOsmStreamSource(file.OpenRead());
        int count = 0;
        foreach (var element in source)
        {
            if (element.Type == OsmSharp.OsmGeoType.Node)
            {
                var node = (OsmSharp.Node)element;
                if (node == null || node.Id == null || node.Latitude == null || node.Longitude == null)
                {
                    continue;
                }

                Dictionary<string, string> tags = [];
                if (node.Tags != null && node.Tags.TryGetValue("railway", out var railway))
                {
                    tags.Add("railway", railway);
                }

                var ourNode = new Node(node.Id.Value, node.Latitude.Value, node.Longitude.Value, node.Tags);
                nodes.Add(node.Id.Value, ourNode);
            }
            else if (element.Type == OsmSharp.OsmGeoType.Way)
            {
                var way = (OsmSharp.Way)element;
                if (way == null || way.Id == null)
                {
                    continue;
                }

                Way? ourWay = null;
                if (way.Tags != null && way.Tags.ContainsKey("railway"))
                {
                    ourWay = new RailWay(way.Id.Value, way.Nodes, way.Tags);
                }

                ways.Add(ourWay ?? new Way(way.Id.Value, way.Nodes, way.Tags));
            }

            if (++count % 200000 == 0)
            {
                Console.Error.WriteLine($"Processed {count} elements");
            }
        }
        return (nodes, ways.ToArray());
    }
}
