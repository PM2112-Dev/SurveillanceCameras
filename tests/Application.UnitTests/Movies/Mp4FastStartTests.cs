using System.Buffers.Binary;
using System.Text;
using NUnit.Framework;
using Shouldly;
using SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;

namespace SurveillanceCameras.Application.UnitTests.Movies;

public class Mp4FastStartTests
{
    private string _path = null!;

    [SetUp]
    public void SetUp() => _path = Path.Combine(Path.GetTempPath(), $"faststart-{Guid.NewGuid():N}.mp4");

    [TearDown]
    public void TearDown() => File.Delete(_path);

    private static byte[] Box(string type, params byte[][] parts)
    {
        var body = parts.SelectMany(x => x).ToArray();
        var box = new byte[8 + body.Length];
        BinaryPrimitives.WriteUInt32BigEndian(box, (uint)box.Length);
        Encoding.ASCII.GetBytes(type).CopyTo(box, 4);
        body.CopyTo(box, 8);
        return box;
    }

    /// <summary>Bảng vị trí khối: stco (4 byte) hoặc co64 (8 byte).</summary>
    private static byte[] Offsets(string type, params long[] values)
    {
        var width = type == "stco" ? 4 : 8;
        var body = new byte[8 + values.Length * width];
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(4), (uint)values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            if (width == 4) BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(8 + i * 4), (uint)values[i]);
            else BinaryPrimitives.WriteUInt64BigEndian(body.AsSpan(8 + i * 8), (ulong)values[i]);
        }

        return Box(type, body);
    }

    private static byte[] Moov(byte[] table) => Box("moov", Box("trak", Box("mdia", Box("minf", Box("stbl", table)))));

    /// <summary>Đọc lại vị trí trong bảng stco/co64 đầu tiên và đoạn dữ liệu 4 byte tại đó.</summary>
    private static (List<string> Order, string Chunk) Inspect(byte[] file, string table)
    {
        var order = new List<string>();
        for (var at = 0; at < file.Length; at += (int)BinaryPrimitives.ReadUInt32BigEndian(file.AsSpan(at)))
            order.Add(Encoding.ASCII.GetString(file, at + 4, 4));
        var index = file.AsSpan().IndexOf(Encoding.ASCII.GetBytes(table)) + 4;
        var offset = table == "stco"
            ? BinaryPrimitives.ReadUInt32BigEndian(file.AsSpan(index + 8))
            : (long)BinaryPrimitives.ReadUInt64BigEndian(file.AsSpan(index + 8));
        return (order, Encoding.ASCII.GetString(file, (int)offset, 4));
    }

    [TestCase("stco")]
    [TestCase("co64")]
    public void MovesIndexBeforeMediaAndKeepsChunkOffsetsPointingAtTheSameData(string table)
    {
        var ftyp = Box("ftyp", Encoding.ASCII.GetBytes("isom0000"));
        var mdat = Box("mdat", Encoding.ASCII.GetBytes("xxxxCHUNKyyyy"));
        var chunk = ftyp.Length + 8 + 4; // vị trí chữ "CHUN" trong mdat
        File.WriteAllBytes(_path, [.. ftyp, .. mdat, .. Moov(Offsets(table, chunk))]);
        var before = new FileInfo(_path).Length;

        Mp4FastStart.Apply(_path).ShouldBeTrue();

        var file = File.ReadAllBytes(_path);
        file.Length.ShouldBe((int)before);
        var (order, data) = Inspect(file, table);
        order.ShouldBe(["ftyp", "moov", "mdat"]);
        data.ShouldBe("CHUN");
    }

    [Test]
    public void LeavesFastStartFilesAndNonMp4FilesUntouched()
    {
        var ftyp = Box("ftyp", Encoding.ASCII.GetBytes("isom0000"));
        byte[] ready = [.. ftyp, .. Moov(Offsets("stco", 100)), .. Box("mdat", new byte[200])];
        File.WriteAllBytes(_path, ready);
        Mp4FastStart.Apply(_path).ShouldBeFalse();
        File.ReadAllBytes(_path).ShouldBe(ready);

        File.WriteAllText(_path, "<html>không phải video</html>");
        Mp4FastStart.Apply(_path).ShouldBeFalse();

        Mp4FastStart.Apply(_path + ".missing").ShouldBeFalse();
    }
}
