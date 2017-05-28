using System.IO;
using NUnit.Framework;

namespace LightResize.Tests
{
    [TestFixture]
    public sealed class StreamUtilsTests
    {
        [Test]
        public void CopyToMemoryStream_WhenCopyingEntireStream_CreatesStreamOfSameLength()
        {
            var source = GetSource();
            var sink = StreamUtils.CopyToMemoryStream(source, true, 100);
            Assert.AreEqual(source.Length, sink.Length);
        }

        [Test]
        public void CopyToMemoryStream_WhenCopyingEntireStreamNotPositionedAtBeginning_CreatesStreamOfSameLength()
        {
            var source = GetSource();
            source.Seek(5, SeekOrigin.Begin);
            var sink = StreamUtils.CopyToMemoryStream(source, true, 100);
            Assert.AreEqual(source.Length, sink.Length);
        }

        [Test]
        public void CopyToMemoryStream_WhenNotCopyingEntireStream_CreatesStreamOfRemainingLength()
        {
            var source = GetSource();
            var offset = 5;
            source.Seek(offset, SeekOrigin.Begin);
            var sink = StreamUtils.CopyToMemoryStream(source, false, 100);
            Assert.AreEqual(source.Length - offset, sink.Length);
        }

        [Test]
        public void CopyToMemoryStream_WhenLengthLargerThanButNotMultipleOfChunkSize_CopiesWholeStream()
        {
            var source = GetSource();
            var sink = StreamUtils.CopyToMemoryStream(source, true, 7);
            Assert.AreEqual(source.Length, sink.Length);
        }

        [Test]
        public void CopyToMemoryStream_WhenLengthLargerThanAndMultipleOfChunkSize_CopiesWholeStream()
        {
            var source = GetSource();
            var sink = StreamUtils.CopyToMemoryStream(source, true, 6);
            Assert.AreEqual(source.Length, sink.Length);
        }

        [Test]
        public void CopyToMemoryStream_WhenLengthEqualsChunkSize_CopiesWholeStream()
        {
            var source = GetSource();
            var sink = StreamUtils.CopyToMemoryStream(source, true, 12);
            Assert.AreEqual(source.Length, sink.Length);
        }

        [Test]
        public void CopyToMemoryStream_WhenLengthSmallerThanChunkSize_CopiesWholeStream()
        {
            var source = GetSource();
            var sink = StreamUtils.CopyToMemoryStream(source, true, 100);
            Assert.AreEqual(source.Length, sink.Length);
        }

        private static MemoryStream GetSource()
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            return new MemoryStream(bytes);
        }
    }
}
