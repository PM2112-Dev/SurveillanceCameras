using System.Buffers.Binary;

namespace SurveillanceCameras.Application.Local.Movies.Commands.StoreEpisodeVideos;

/// <summary>
/// Đưa phần mục lục của file MP4 (hộp <c>moov</c>) lên trước dữ liệu hình/tiếng (<c>mdat</c>) — "faststart". Thiếu bước
/// này, trình phát phải tải tới cuối file mới đọc được mục lục và bắt đầu phát.
/// </summary>
public static class Mp4FastStart
{
    private record Box(string Type, long Offset, long Size)
    {
        public long End => Offset + Size;
    }

    // Đường tới bảng vị trí khối dữ liệu: moov → trak → mdia → minf → stbl → stco/co64.
    private static readonly HashSet<string> Containers = ["moov", "trak", "mdia", "minf", "stbl"];

    /// <summary>Sắp xếp lại file tại chỗ nếu <c>moov</c> đang nằm sau <c>mdat</c>.</summary>
    /// <returns><c>true</c> nếu đã sửa file; <c>false</c> nếu file đã đúng, không phải MP4 thường, hoặc không sửa an toàn được.</returns>
    public static bool Apply(string path)
    {
        if (!File.Exists(path)) return false;

        byte[] moov;
        Box moovBox, mdatBox;
        using (var input = File.OpenRead(path))
        {
            var boxes = ReadBoxes(input);
            if (boxes is null || boxes.Any(x => x.Type == "moof")) return false; // MP4 phân mảnh: không thuộc trường hợp này
            var moovIndex = boxes.FindIndex(x => x.Type == "moov");
            var mdatIndex = boxes.FindIndex(x => x.Type == "mdat");
            if (moovIndex < 0 || mdatIndex < 0 || moovIndex < mdatIndex) return false;

            moovBox = boxes[moovIndex];
            mdatBox = boxes[mdatIndex];
            if (moovBox.Size > int.MaxValue) return false;
            moov = new byte[moovBox.Size];
            input.Position = moovBox.Offset;
            input.ReadExactly(moov);
        }

        // Phần [mdat, moov) lùi về sau đúng bằng kích thước moov; vị trí khối dữ liệu trỏ vào đó phải cộng thêm.
        if (!ShiftChunkOffsets(moov, 0, moov.Length, mdatBox.Offset, moovBox.Offset, moovBox.Size)) return false;

        var temp = path + ".faststart";
        using (var input = File.OpenRead(path))
        using (var output = File.Create(temp))
        {
            Copy(input, output, 0, mdatBox.Offset);
            output.Write(moov);
            Copy(input, output, mdatBox.Offset, moovBox.Offset - mdatBox.Offset);
            Copy(input, output, moovBox.End, input.Length - moovBox.End);
        }

        File.Move(temp, path, overwrite: true);
        return true;
    }

    /// <summary>Các hộp cấp cao nhất; <c>null</c> nếu cấu trúc không hợp lệ.</summary>
    private static List<Box>? ReadBoxes(Stream input)
    {
        var boxes = new List<Box>();
        var header = new byte[16];
        long offset = 0;
        while (offset < input.Length)
        {
            if (input.Length - offset < 8) return null;
            input.Position = offset;
            input.ReadExactly(header, 0, 8);
            long size = BinaryPrimitives.ReadUInt32BigEndian(header);
            var type = System.Text.Encoding.ASCII.GetString(header, 4, 4);
            if (size == 1)
            {
                if (input.Length - offset < 16) return null;
                input.ReadExactly(header, 8, 8);
                size = (long)BinaryPrimitives.ReadUInt64BigEndian(header.AsSpan(8));
            }
            else if (size == 0) size = input.Length - offset;

            if (size < 8 || offset + size > input.Length) return null;
            boxes.Add(new Box(type, offset, size));
            offset += size;
        }

        return boxes;
    }

    /// <summary>Cộng <paramref name="delta"/> vào mọi vị trí khối nằm trong [<paramref name="from"/>, <paramref name="to"/>).</summary>
    /// <returns><c>false</c> nếu cấu trúc hỏng hoặc vị trí mới vượt giới hạn 32 bit của <c>stco</c>.</returns>
    private static bool ShiftChunkOffsets(byte[] data, int start, int end, long from, long to, long delta)
    {
        // Hộp moov ở đầu vùng thì bỏ qua phần đầu của chính nó.
        var position = start == 0 ? HeaderSize(data, 0) : start;
        while (position < end)
        {
            if (end - position < 8) return false;
            long size = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(position));
            var type = System.Text.Encoding.ASCII.GetString(data, position + 4, 4);
            var header = 8;
            if (size == 1)
            {
                if (end - position < 16) return false;
                size = (long)BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(position + 8));
                header = 16;
            }
            else if (size == 0) size = end - position;
            if (size < header || position + size > end) return false;

            var body = position + header;
            var boxEnd = (int)(position + size);
            if (Containers.Contains(type))
            {
                if (!ShiftChunkOffsets(data, body, boxEnd, from, to, delta)) return false;
            }
            else if (type is "stco" or "co64")
            {
                // Sau version/flags (4 byte) là số mục rồi tới các vị trí 4 byte (stco) hoặc 8 byte (co64).
                if (boxEnd - body < 8) return false;
                var count = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(body + 4));
                var width = type == "stco" ? 4 : 8;
                if (count > (uint)((boxEnd - body - 8) / width)) return false;
                for (var i = 0; i < count; i++)
                {
                    var at = body + 8 + i * width;
                    long value = width == 4
                        ? BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(at))
                        : (long)BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(at));
                    if (value < from || value >= to) continue;
                    value += delta;
                    if (width == 4)
                    {
                        if (value > uint.MaxValue) return false;
                        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(at), (uint)value);
                    }
                    else BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(at), (ulong)value);
                }
            }

            position = boxEnd;
        }

        return true;
    }

    private static int HeaderSize(byte[] data, int position)
        => BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(position)) == 1 ? 16 : 8;

    private static void Copy(Stream input, Stream output, long offset, long length)
    {
        input.Position = offset;
        var buffer = new byte[81920];
        while (length > 0)
        {
            var read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, length));
            if (read == 0) throw new EndOfStreamException();
            output.Write(buffer, 0, read);
            length -= read;
        }
    }
}
