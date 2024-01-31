using System.CommandLine;
using System.Data;
using OsmSharp.Streams;
using SkiaSharp;
using TRAINer.Config;
using TRAINer.Data;
using TRAINer.Geo;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var dataOption = new Option<DirectoryInfo?>(
            name: "--data",
            description: "Location of the directory where the OpenStreetMap data is stored. The program will look for the file <data>/raw/rails.osm.pbf.",
            parseArgument: result =>
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
            },
            isDefault: true
        );

        var minLatOption = new Option<float>(
            name: "--min-lat",
            description: "Minimum latitude to include in the output",
            getDefaultValue: () => -90
        );

        var maxLatOption = new Option<float>(
            name: "--max-lat",
            description: "Maximum latitude to include in the output",
            getDefaultValue: () => 90
        );

        var minLonOption = new Option<float>(
            name: "--min-lon",
            description: "Minimum longitude to include in the output",
            getDefaultValue: () => -180
        );

        var maxLonOption = new Option<float>(
            name: "--max-lon",
            description: "Maximum longitude to include in the output",
            getDefaultValue: () => 180
        );

        var colorMainOption = new Option<string>(
            name: "--color-main",
            description: "Color of the main line as hex value",
            getDefaultValue: () => SKColors.RoyalBlue.ToString()
        );

        var colorSecondaryOption = new Option<string>(
            name: "--color-secondary",
            description: "Color of the secondary line as hex value",
            getDefaultValue: () => SKColors.DarkGreen.ToString()
        );

        var colorBackgroundOption = new Option<string>(
            name: "--color-background",
            description: "Color of the background as hex value",
            getDefaultValue: () => SKColors.White.ToString()
        );

        var rootCommand = new RootCommand("Sample app for System.CommandLine");
        rootCommand.AddOption(dataOption);
        rootCommand.AddOption(minLatOption);
        rootCommand.AddOption(maxLatOption);
        rootCommand.AddOption(minLonOption);
        rootCommand.AddOption(maxLonOption);
        rootCommand.AddOption(colorMainOption);
        rootCommand.AddOption(colorSecondaryOption);
        rootCommand.AddOption(colorBackgroundOption);

        rootCommand.SetHandler(
            (data, minLat, maxLat, minLon, maxLon, mainColor, secondaryColor, backgroundColor) =>
            {
                if (data == null)
                {
                    Console.Error.WriteLine("No data directory specified");
                    return;
                }
                Process(
                    data,
                    new Limits(minLat, maxLat, minLon, maxLon),
                    new ColorPalette(mainColor, secondaryColor, backgroundColor)
                );
            },
            dataOption,
            minLatOption,
            maxLatOption,
            minLonOption,
            maxLonOption,
            colorMainOption,
            colorSecondaryOption,
            colorBackgroundOption
        );
        return await rootCommand.InvokeAsync(args);
    }

    internal static void Process(DirectoryInfo dataFolder, Limits limits, ColorPalette colors)
    {
        // Find raw rails file. You can generate it from the OSM data using osmium
        // osmium tags-filter -o temp1.osm.pbf -t --output-format pbf,add_metadata=false europe-latest.osm.pbf "w/railway=bridge,goods,light_rail,monorail,narrow_gauge,rail,subway,tram"
        // osmium tags-filter -o temp2.osm.pbf -t --output-format pbf,add_metadata=false temp1.osm.pbf -i "w/railway:preserved=yes"
        // osmium tags-filter -o rails.osm.pbf -t --output-format pbf,add_metadata=false temp2.osm.pbf "w/railway=bridge,goods,light_rail,monorail,narrow_gauge,rail,subway,tram"

        // All railway types: rail, abandoned, subway, light_rail, razed, funicular, tram,
        // narrow_gauge, disused, construction, dismantled, platform, miniature, proposed, historic,
        // overline_bridge, depot, workshop, monorail, roundhouse, turntable, platform_edge, ferry,
        // station, museum, preserved, wash, signal_box, yard, engine_shed, stop, service_station,
        // ventilation_shaft, halt;station, loading_ramp, container_terminal, disused_station, no,
        // train_depot, a, signal_bridge, traverser, subway_entrance, historic_path, ticket_office,
        // goods, interlocking, site, historic_station, level_crossing, water_tower, crossing_box,
        // goods_shed, transfer_shed, coaling_facility, halt, demolished, terminal, planned,
        // technical_center, DE, crossover, yes, crossing, unused, gauge_conversion,
        // train_station_entrance, facility, trolley_rails, works, tram_stop, service, blockpost,
        // station_site, fuel, station_area, control_tower, signal_box_site, track_diagram, bridge,
        // waiting_room, approved, signal_box;crossing_box, terminal_site, phone,
        // interlocking_tower, junction, incline, funicular_entrance, engine shed, switch, store,
        // water_crane, overbridge, spur_junction, buffer_stop, tram_level_crossing,
        // crossing_controller, miniature_facility, watchmans_house, air_shaft, station_master,
        // crane_rail, power_mast, never_built, storage, gantry, pit, single_rail, meadow,
        // Wendeanlage, elevator, crane, track_ballast, ground_frame, office, Ortsstellbereich,
        // tram_crossing, shed, uncompleted, Hilfshandlungstafel, signalbox, waste_disposal,
        // Rangierbezirk, track_scale, loading_gauge, communication, electric_supply,
        // compressed_air_supply, sand_store, abandoned:cableway, booth, technical_station,
        // cranetrack, driveway, 4, disused_platform, ticket_hall, radio, jetty, model, residential,
        // hyperloop, ticket office, loading_rack, loading_zone, abandoned;razed, boat_slipway,
        // abandoned:platform, level, modeltrain, Retarder, weight, railway_crossing, storage_area,
        // switchgear, underline_bridge, 16, debris_pile, shop, Construction, monorack,
        // recovery_train, *, disused:station, ash_pit, telephone, without, signal, train,
        // industrial_rail, proposed:platform, station_building, abandoned:rail, loading_dock

        // Important tags: bridge,goods,light_rail,monorail,narrow_gauge,rail,subway,tram

        var file = new FileInfo(Path.Combine(dataFolder.FullName, "raw", "rails.osm.pbf"));

        if (!file.Exists)
        {
            Console.Error.WriteLine($"Missing raw rails file {file.FullName}");
            return;
        }

        var (nodes, ways) = GetData(file, limits);

        Console.Error.WriteLine($"Found {nodes.Count} nodes and {ways.Length} ways");

        // Now we need to paint the data
        DirectoryInfo output = new DirectoryInfo(Path.Combine(dataFolder.FullName, "output"));
        if (!output.Exists)
        {
            output.Create();
        }

        var latexFile = new FileInfo(Path.Combine(output.FullName, "rails.png"));
        PaintPng(latexFile, nodes, ways, colors);
    }

    internal static (Dictionary<long, Node> nodes, Way[] ways) GetData(FileInfo file, Limits limits)
    {
        Dictionary<long, Node> nodes = [];
        List<Way> ways = [];
        HashSet<long> removedNodes = [];

        var source = new PBFOsmStreamSource(file.OpenRead());
        int count = 0;

        foreach (var element in source)
        {
            if (element.Type == OsmSharp.OsmGeoType.Node)
            {
                var node = (OsmSharp.Node)element;
                if (
                    node == null
                    || node.Id == null
                    || node.Latitude == null
                    || node.Longitude == null
                )
                {
                    continue;
                }

                if (!limits.IsInside((float)node.Latitude.Value, (float)node.Longitude.Value))
                {
                    removedNodes.Add(node.Id.Value);
                    continue;
                }

                var ourNode = new Node((float)node.Latitude.Value, (float)node.Longitude.Value);
                nodes.Add(node.Id.Value, ourNode);
            }
            else if (element.Type == OsmSharp.OsmGeoType.Way)
            {
                var way = (OsmSharp.Way)element;
                if (way == null || way.Id == null)
                {
                    continue;
                }

                if (way.Nodes.Length < 2)
                {
                    continue;
                }

                Way? ourWay = null;
                if (way.Tags != null && way.Tags.ContainsKey("railway"))
                {
                    ourWay = new RailWay(way.Id.Value, way.Nodes, way.Tags);
                }

                ways.Add(ourWay ?? new Way(way.Id.Value, way.Nodes));
            }

            if (++count % 200000 == 0)
            {
                Console.Error.Write($"\rProcessed {count, 12:N0} elements");
            }
        }

        // Check all the ways again for removed nodes
        var arrWays = ways.Select(way =>
            {
                var newNodes = way.Nodes.Where(nodeId => !removedNodes.Contains(nodeId)).ToArray();
                if (newNodes.Length < 2)
                    return null;

                way.Nodes = newNodes;
                return way;
            })
            .Where(way => way != null)
            .Cast<Way>()
            .ToArray();

        Console.Error.WriteLine();
        return (nodes, arrWays);
    }

    internal static void PaintPng(
        FileInfo file,
        Dictionary<long, Node> nodes,
        Way[] ways,
        ColorPalette colors
    )
    {
        Console.Error.WriteLine($"Gauges: {RailWay.MinGauge} - {RailWay.MaxGauge}");
        Console.Error.WriteLine($"Speeds: {RailWay.MinSpeed} - {RailWay.MaxSpeed}");

        var converter = new GPSToCanvas(20000, 20000);
        foreach (var node in nodes.Values)
        {
            converter.AddNode(node);
        }

        int count = 0;
        var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            IsAntialias = true
        };

        var width = converter.Width;
        var height = converter.Height;

        using var bitmap = new SKBitmap(converter.Width, converter.Height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(colors.Background);

        // Sort by Color
        var selectedWays = ways.Where(way => way.Visible).ToArray();
        Array.Sort(selectedWays, (a, b) => a.Color.CompareTo(b.Color));
        foreach (var way in selectedWays)
        {
            bool first = true;
            var path = new SKPath();

            foreach (var nodeId in way.Nodes)
            {
                var node = nodes[nodeId];
                var (x, y) = converter.Convert(node);
                var point = new SKPoint(x, y);

                if (first)
                {
                    first = false;
                    path.MoveTo(point);
                    continue;
                }

                path.LineTo(point);
            }

            // Now we can paint
            paint.Color = colors.GetMix(way.Color);
            paint.StrokeWidth = way.Weight;

            canvas.DrawPath(path, paint);

            if (++count % 20000 == 0)
            {
                Console.Error.Write($"\rPainted {count, 12:N0} ways");
            }
        }

        Console.Error.WriteLine();
        Console.Error.WriteLine("Saving image");

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 80);
        using var stream = File.OpenWrite(file.FullName);
        data.SaveTo(stream);
    }
}
