using System.CommandLine;
using OSMPBF;

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
        // Create folder if it doesn't exist
        var rawDataFolder = new DirectoryInfo(Path.Combine(dataFolder.FullName, "raw"));

        if (!rawDataFolder.Exists)
        {
            Console.Error.WriteLine($"Missing raw data folder {rawDataFolder.FullName}");
        }

        // Find files ending in .osm.pbf
        var files = rawDataFolder.EnumerateFiles("*.osm.pbf", SearchOption.AllDirectories);

        Console.WriteLine($"Found {files.Count()} files");
        foreach (var file in files)
        {
            Console.WriteLine($"Processing {file.FullName}");
            using var stream = file.OpenRead();

            var fileBlobHeader = FileBlobHeader.Parse(stream);

            Console.WriteLine($"Blob type: {fileBlobHeader.Type}");
            Console.WriteLine($"Blob data size: {fileBlobHeader.Datasize}");

            // Now read the blob data
            var blobDataBytes = new byte[fileBlobHeader.Datasize];
            stream.Read(blobDataBytes, 0, fileBlobHeader.Datasize);

            // Now parse the blob data
            using var blobDataStream = new MemoryStream(blobDataBytes);
            var blobData = Blob.Parser.ParseFrom(blobDataStream);
            Console.WriteLine($"Blob data raw size: {blobData.RawSize}");
            Console.WriteLine($"Blob data zlib data size: {blobData.HasZlibData}");

        }

    }
}