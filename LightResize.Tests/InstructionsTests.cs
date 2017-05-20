using System;
using NUnit.Framework;

namespace LightResize.Tests
{
    public sealed class InstructionsTests
    {
        [Test]
        [TestCase(-1)]
        [TestCase(101)]
        public void JpegQuality_MustNotBeLessThan0OrGreaterThan100(int jpegQuality)
        {
            // This is a departure from the original LightResize. Given a negative value, it would reset
            // the quality setting to 90, and given a value over 100, it would set it to 100. We no longer do that.
            Assert.Throws<ArgumentOutOfRangeException>(() => new Instructions { JpegQuality = jpegQuality });
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(99)]
        [TestCase(100)]
        public void JpegQuality_CanBeSetToValueBetween0And100Inclusive(int jpegQuality)
        {
            var instructions = new Instructions { JpegQuality = jpegQuality };
            Assert.AreEqual(jpegQuality, instructions.JpegQuality);
        }

        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        public void Width_MustNotBeZeroOrNegativeNumber(int width)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Instructions { Width = width });
        }

        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        public void Height_MustNotBeZeroOrNegativeNumber(int height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Instructions { Height = height });
        }
    }
}
