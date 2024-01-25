namespace Djinn.Metadata.Tests;

public class FlacTests : AudioFileTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        using var audioFile1Stream = LoadResource("test.flac");
        var audioFile1 = AudioFileLoader.Load(audioFile1Stream);
        
        Assert.Multiple(() =>
        {
            Assert.That(audioFile1.Get(Field.Title), Is.EqualTo("Title"));
            Assert.That(audioFile1.Get(Field.Artist), Is.EqualTo("Artist"));
            Assert.That(audioFile1.Get(Field.Album), Is.EqualTo("Album"));
            Assert.That(audioFile1.Get(Field.Year), Is.EqualTo(2024));
            Assert.That(audioFile1.Get(Field.TrackNumber), Is.EqualTo(1));
            Assert.That(audioFile1.Get(Field.TotalTracks), Is.EqualTo(10));
            Assert.That(audioFile1.Get(Field.DiscNumber), Is.EqualTo(1));
            Assert.That(audioFile1.Get(Field.TotalDiscs), Is.EqualTo(2));
        });
        
        audioFile1.Set(Field.Title, "New Title");
        audioFile1.Set(Field.Artist, "New Artist");
        audioFile1.Set(Field.Album, "New Album");
        audioFile1.Set(Field.Year, 2025);
        audioFile1.Set(Field.TrackNumber, 2);
        audioFile1.Set(Field.TotalTracks, 20);
        audioFile1.Set(Field.DiscNumber, 2);
        audioFile1.Set(Field.TotalDiscs, 3);
        
        using var audioFile2Stream = audioFile1.Save();
        var audioFile2 = AudioFileLoader.Load(audioFile2Stream);
        
        Assert.Multiple(() =>
        {
            Assert.That(audioFile2.Get(Field.Title), Is.EqualTo("New Title"));
            Assert.That(audioFile2.Get(Field.Artist), Is.EqualTo("New Artist"));
            Assert.That(audioFile2.Get(Field.Album), Is.EqualTo("New Album"));
            Assert.That(audioFile2.Get(Field.Year), Is.EqualTo(2025));
            Assert.That(audioFile2.Get(Field.TrackNumber), Is.EqualTo(2));
            Assert.That(audioFile2.Get(Field.TotalTracks), Is.EqualTo(20));
            Assert.That(audioFile2.Get(Field.DiscNumber), Is.EqualTo(2));
            Assert.That(audioFile2.Get(Field.TotalDiscs), Is.EqualTo(3));
        });
    }
}