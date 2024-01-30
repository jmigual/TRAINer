using System.IO.Compression;
using SevenZip.Compression.LZMA;

namespace OSMPBF;

public class FileBlob 
{

    public byte[] Data { get; private set; }

    protected FileBlob(byte[] data)
    {
        Data = data;
    }
    public static FileBlob Parse(Stream stream, FileBlobHeader header)
    {
        // Now read the blob data
        var blobDataBytes = new byte[header.Datasize];
        stream.Read(blobDataBytes, 0, header.Datasize);

        // Now parse the blob data
        var blob = Blob.Parser.ParseFrom(blobDataBytes);

        // Check the type of data and decode it

        if (blob.HasRaw)
        {
            return new FileBlob(blob.Raw.ToByteArray());
        }
        if (blob.HasZlibData)
        {
            var compressedStream = new MemoryStream(blob.ZlibData.ToByteArray());
            var decompressedStream = new ZLibStream(compressedStream, CompressionMode.Decompress);
            var memoryStream = new MemoryStream();
            decompressedStream.CopyTo(memoryStream);
            return new FileBlob(memoryStream.ToArray());
        }
        if (blob.HasLzmaData)
        {
            var compressedStream = new MemoryStream(blob.LzmaData.ToByteArray());
            var decoder = new Decoder();
            var decompressedStream = new MemoryStream();
            decoder.Code(compressedStream, decompressedStream, blob.LzmaData.Length, blob.RawSize, null);

            return new FileBlob(decompressedStream.ToArray());
        }
        throw new NotImplementedException();
    }
}