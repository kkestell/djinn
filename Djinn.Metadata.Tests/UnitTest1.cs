namespace Djinn.Metadata.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var audioFile = AudioFileLoader.Load(@"C:\Users\Kyle\Downloads\03 Calling You.flac");
            audioFile.Load();
            audioFile.Save();
        }
    }
}