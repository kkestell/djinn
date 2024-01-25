using System.Text;

namespace Djinn.Metadata;

public class FlacFile(Stream stream) : AudioFile(stream)
{
    private long _audioDataStart;
    private byte[]? _streamInfo;

    public override void Load()
    {
        AudioStream.Seek(0, SeekOrigin.Begin);

        using var reader = new BinaryReader(AudioStream, Encoding.Default, leaveOpen: true);

        var marker = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (marker != "fLaC")
        {
            throw new FormatException("Not a valid FLAC file.");
        }

        var isLastBlock = false;

        while (!isLastBlock && reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var header = reader.ReadByte();
            isLastBlock = (header & 0x80) != 0;
            var blockType = header & 0x7F;
            var blockSize = reader.ReadInt24BE();

            var blockData = reader.ReadBytes(blockSize);

            ProcessMetadataBlock(blockType, blockData);

            if (isLastBlock)
            {
                _audioDataStart = reader.BaseStream.Position;
            }
        }

        AudioStream.Seek(0, SeekOrigin.Begin);
    }

    public override Stream Save()
    {
        if (_streamInfo == null)
        {
            throw new InvalidOperationException("StreamInfo is missing.");
        }

        var newVorbisCommentBlock = EncodeVorbisComments();

        var outputStream = new MemoryStream();
        using (var writer = new BinaryWriter(outputStream, Encoding.Default, leaveOpen: true))
        {
            writer.Write(Encoding.ASCII.GetBytes("fLaC"));

            writer.Write((byte)BlockType.StreamInfo);
            writer.WriteInt24BE(_streamInfo.Length);
            writer.Write(_streamInfo);

            writer.Write((byte)((int)BlockType.VorbisComment | 0x80)); // This is the last metadata block
            writer.WriteInt24BE(newVorbisCommentBlock.Length);
            writer.Write(newVorbisCommentBlock);

            AudioStream.Seek(_audioDataStart, SeekOrigin.Begin);
            AudioStream.CopyTo(outputStream);
        }

        outputStream.Seek(0, SeekOrigin.Begin);

        return outputStream;
    }


    private void ProcessMetadataBlock(int blockType, byte[] blockData)
    {
        var type = blockType is >= 7 and < 127 ? BlockType.Reserved : (BlockType)blockType;

        switch (type)
        {
            case BlockType.StreamInfo:
                _streamInfo = blockData;
                break;
            case BlockType.VorbisComment:
                DecodeVorbisComments(blockData);
                break;
        }
    }

    private void DecodeVorbisComments(byte[] blockData)
    {
        using var stream = new MemoryStream(blockData);
        using var reader = new BinaryReader(stream);

        var vendorLength = reader.ReadInt32();
        var vendorString = Encoding.UTF8.GetString(reader.ReadBytes(vendorLength));

        var commentCount = reader.ReadInt32();

        for (var i = 0; i < commentCount; i++)
        {
            var commentLength = reader.ReadInt32();
            var comment = Encoding.UTF8.GetString(reader.ReadBytes(commentLength));

            var parts = comment.Split(['='], 2);
            if (parts.Length == 2)
            {
                var key = parts[0]
                    .ToUpperInvariant();
                var rawValue = parts[1];

                SetField(key, rawValue);
            }
        }
    }

    private void SetField(string key, string rawValue)
    {
        switch (key)
        {
            case "TITLE":
            {
                Set(Field.Title, rawValue);
                break;
            }
            case "ALBUM":
            {
                Set(Field.Album, rawValue);
                break;
            }
            case "ARTIST":
            case "ALBUMARTIST":
            case "ALBUM ARTIST":
            case "PERFORMER":
            {
                Set(Field.Artist, rawValue);
                break;
            }
            case "YEAR":
            {
                if (int.TryParse(rawValue, out var year))
                {
                    Set(Field.Year, year);
                }

                break;
            }
            case "DATE":
            {
                if (DateOnly.TryParse(rawValue, out var date))
                {
                    Set(Field.Date, date);
                    Set(Field.Year, date.Year);
                }
                else if (int.TryParse(rawValue, out var year))
                {
                    Set(Field.Year, year);
                }

                break;
            }
            case "TRACKNUMBER":
            case "TRACK NUMBER":
            case "TRACK":
            {
                if (int.TryParse(rawValue, out int trackNumber))
                {
                    Set(Field.TrackNumber, trackNumber);
                }

                break;
            }
            case "TOTALTRACKS":
            case "TRACKCOUNT":
            case "TRACKTOTAL":
            case "TRACK COUNT":
            case "TRACK TOTAL":
            {
                if (int.TryParse(rawValue, out int trackCount))
                {
                    Set(Field.TotalTracks, trackCount);
                }

                break;
            }
            case "DISCNUMBER":
            case "DISC NUMBER":
            case "DISC":
            {
                if (int.TryParse(rawValue, out int discNumber))
                {
                    Set(Field.DiscNumber, discNumber);
                }

                break;
            }
            case "TOTALDISCS":
            case "DISCCOUNT":
            case "DISCTOTAL":
            case "DISC COUNT":
            case "DISC TOTAL":
            {
                if (int.TryParse(rawValue, out int discCount))
                {
                    Set(Field.TotalDiscs, discCount);
                }

                break;
            }
            case "MUSICBRAINZ_TRACKID":
            {
                if (Guid.TryParse(rawValue, out Guid musicbrainzTrackId))
                {
                    Set(Field.MusicbrainzTrackId, musicbrainzTrackId);
                }

                break;
            }
            case "MUSICBRAINZ_RELEASEGROUPID":
            {
                if (Guid.TryParse(rawValue, out Guid musicbrainzReleaseGroupId))
                {
                    Set(Field.MusicbrainzReleaseGroupId, musicbrainzReleaseGroupId);
                }

                break;
            }
            case "MUSICBRAINZ_ARTISTID":
            {
                if (Guid.TryParse(rawValue, out Guid musicbrainzArtistId))
                {
                    Set(Field.MusicbrainzArtistId, musicbrainzArtistId);
                }

                break;
            }
            default:
                Console.WriteLine($"Ignoring key: {key}");
                break;
        }
    }

    private byte[] EncodeVorbisComments()
    {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream, Encoding.UTF8);

        var vendorString = Encoding.UTF8.GetBytes("Djinn");
        writer.WriteInt32LE(vendorString.Length);
        writer.Write(vendorString);

        writer.WriteInt32LE(Fields.Count);

        foreach (var kvp in Fields)
        {
            var comment = Encoding.UTF8.GetBytes($"{kvp.Key}={kvp.Value}");
            writer.WriteInt32LE(comment.Length);
            writer.Write(comment);
        }

        return memoryStream.ToArray();
    }
}