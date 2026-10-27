using System.Buffers.Binary;
using System.IO.Compression;

namespace Competency.OrgStructure.Import;

/// <summary>
/// Checks the ZIP container of an uploaded workbook before the library reads it: the declared sizes of the entries may lie,
/// so each entry is inflated through a byte counter and the check stops at the limit.
/// </summary>
internal static class XlsxArchiveGuard
{
    public const int MaxEntries = 1_000;
    public const long MaxUnpackedBytes = 256L * 1024 * 1024;
    public const string TooLargeMessage = "Файл слишком большой или повреждён.";
    public const string NotWorkbookMessage = "Файл не является книгой Excel (.xlsx) или повреждён.";

    private const int EndRecordLength = 22;
    private const int EntryCountOffset = 10;
    private static readonly byte[] EndRecordSignature = [0x50, 0x4B, 0x05, 0x06];

    /// <summary>
    /// Finds out whether the content is a ZIP container within the limits on entries and unpacked size.
    /// The entry count of the end record is read first, so an archive declaring more entries than the limit is refused before any entry table is built.
    /// </summary>
    /// <param name="content">The uploaded file.</param>
    /// <returns>The reason to refuse the file, addressed to the user; <see langword="null"/> when it is within the limits.</returns>
    public static string? Problem(byte[] content)
    {
        var tail = content.AsSpan(Math.Max(0, content.Length - (ushort.MaxValue + EndRecordLength)));
        var end = tail.LastIndexOf(EndRecordSignature);
        if (end < 0 || end + EndRecordLength > tail.Length)
        {
            return NotWorkbookMessage;
        }

        if (BinaryPrimitives.ReadUInt16LittleEndian(tail[(end + EntryCountOffset)..]) > MaxEntries)
        {
            return TooLargeMessage;
        }

        try
        {
            using var archive = new ZipArchive(new MemoryStream(content, writable: false), ZipArchiveMode.Read);
            return archive.Entries.Count > MaxEntries ? TooLargeMessage : InflatedProblem(archive);
        }
        catch (Exception exception) when (exception is InvalidDataException or NotSupportedException)
        {
            return NotWorkbookMessage;
        }
    }

    private static string? InflatedProblem(ZipArchive archive)
    {
        var total = 0L;
        var buffer = new byte[81_920];
        foreach (var entry in archive.Entries)
        {
            using var stream = entry.Open();
            int read;
            while ((read = stream.Read(buffer)) > 0)
            {
                total += read;
                if (total > MaxUnpackedBytes)
                {
                    return TooLargeMessage;
                }
            }
        }

        return null;
    }
}
