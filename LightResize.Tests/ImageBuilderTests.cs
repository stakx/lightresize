using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using NUnit.Framework;

using static LightResize.SourceOptions;

[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1312:Variable names must begin with lower-case letter", Justification = "We'll be using a variable named `_` for things that we don't care about.", Scope = "type", Target = "~T:LightResize.Tests.ImageBuilderTests")]

namespace LightResize.Tests
{
    [TestFixture]
    public class ImageBuilderTests
    {
        [Test]
        public void Build_ThrowsArgumentNullException_WhenSourceNull()
        {
            Assert.Throws<ArgumentNullException>(() => ImageBuilder.Build((Stream)null, Stream.Null, new Instructions()));
        }

        [Test]
        public void Build_ThrowsArgumentNullException_WhenSourcePathNull()
        {
            Assert.Throws<ArgumentNullException>(() => ImageBuilder.Build((string)null, Stream.Null, new Instructions()));
        }

        [Test]
        public void Build_ThrowsArgumentNullException_WhenDestinationNull()
        {
            Assert.Throws<ArgumentNullException>(() => ImageBuilder.Build(Stream.Null, (Stream)null, new Instructions()));
        }

        [Test]
        public void Build_ThrowsArgumentNullException_WhenDestinationPathNull()
        {
            Assert.Throws<ArgumentNullException>(() => ImageBuilder.Build(Stream.Null, (string)null, new Instructions()));
        }

        [Test]
        public void Build_ThrowsArgumentNullException_WhenInstructionsNull()
        {
            Assert.Throws<ArgumentNullException>(() => ImageBuilder.Build(Stream.Null, Stream.Null, (Instructions)null));
        }

        [Test]
        [TestCase(None)]
        [TestCase(BufferInMemory)]
        [TestCase(BufferInMemory | LeaveOpen)]
        [TestCase(LeaveOpen)]
        [TestCase(Rewind)]
        public void Build_Succeeds_EvenWhenSourceStreamPositionNotAt0(SourceOptions sourceOptions)
        {
            TestDelegate action = () =>
            {
                using (var source = GetBitmapStream(100, 100))
                using (var _ = Stream.Null)
                {
                    source.Seek(17, SeekOrigin.Begin);
                    var instructions = new Instructions { Width = 50 };
                    ImageBuilder.Build(source, sourceOptions, _, instructions);
                }
            };
            Assert.DoesNotThrow(action);
        }

        [Test]
        [TestCase(LeaveOpen)]
        [TestCase(Rewind)]
        public void Build_LeavesSourceStreamOpen_WhenAskedTo(SourceOptions sourceOptions)
        {
            using (var source = GetBitmapStream(100, 100))
            using (var _ = Stream.Null)
            {
                var instructions = new Instructions { Width = 50 };
                ImageBuilder.Build(source, sourceOptions, _, instructions);
                Assert.True(source.CanRead);
            }
        }

        [Test]
        [TestCase(None)]
        [TestCase(BufferInMemory)]
        public void Build_ClosesSourceStream_WhenNotAskedToLeaveItOpen(SourceOptions sourceOptions)
        {
            using (var source = GetBitmapStream(100, 100))
            using (var _ = Stream.Null)
            {
                var instructions = new Instructions { Width = 50 };
                ImageBuilder.Build(source, sourceOptions, _, instructions);
                Assert.False(source.CanRead);
            }
        }

        [Test]
        public void Build_LeavesTargetStreamOpen_WhenAskedTo()
        {
            using (var _ = GetBitmapStream(100, 100))
            using (var destination = new MemoryStream())
            {
                var instructions = new Instructions { Width = 50 };
                ImageBuilder.Build(_, destination, true, instructions);
                Assert.True(destination.CanRead);
            }
        }

        [Test]
        public void Build_ClosesTargetStream_WhenNotAskedToLeaveItOpen()
        {
            using (var _ = GetBitmapStream(100, 100))
            using (var destination = new MemoryStream())
            {
                var instructions = new Instructions { Width = 50 };
                ImageBuilder.Build(_, destination, instructions);
                Assert.False(destination.CanRead);
            }
        }

        [Test]
        public void Build_RewindsSourceStream_WhenAskedTo()
        {
            using (var source = GetBitmapStream(100, 100))
            using (var _ = Stream.Null)
            {
                source.Seek(1, SeekOrigin.Begin);
                var originalPosition = source.Position;
                var instructions = new Instructions { Width = 50 };
                ImageBuilder.Build(source, Rewind, _, instructions);
                Assume.That(source.CanSeek);
                Assert.AreEqual(originalPosition, source.Position);
            }
        }

        [Test]
        [TestCase(None)]
        [TestCase(BufferInMemory)]
        [TestCase(LeaveOpen)]
        public void Build_DoesNotRewindSourceStream_WhenNotAskedTo(SourceOptions sourceOptions)
        {
            using (var source = GetBitmapStream(100, 100))
            using (var _ = Stream.Null)
            {
                source.Seek(1, SeekOrigin.Begin);
                var originalPosition = source.Position;
                var instructions = new Instructions { Width = 50 };
                ImageBuilder.Build(source, sourceOptions | LeaveOpen, _, instructions);
                Assume.That(source.CanSeek);
                Assert.AreNotEqual(originalPosition, source.Position);
            }
        }

        [Test]
        public void Build_CreatesTargetDirectory_WhenAskedTo()
        {
            using (var source = GetBitmapStream(100, 100))
            using (var targetStream = new MemoryStream())
            {
                source.Seek(1, SeekOrigin.Begin);
                var originalPosition = source.Position;
                var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                Assume.That(!Directory.Exists(directory));
                var destinationPath = Path.Combine(directory, "test.jpg");
                try
                {
                    var instructions = new Instructions { Width = 50 };
                    ImageBuilder.Build(source, destinationPath, true, instructions);
                    Assert.True(Directory.Exists(directory));
                }
                finally
                {
                    File.Delete(destinationPath);
                    Directory.Delete(directory, false);
                }
            }
        }

        [Test]
        public void Build_CannotWriteToNonExistentDirectory_WhenNotAskedToCreateIt()
        {
            TestDelegate action = () =>
            {
                using (var source = GetBitmapStream(100, 100))
                {
                    source.Seek(1, SeekOrigin.Begin);
                    var originalPosition = source.Position;
                    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                    Assume.That(!Directory.Exists(directory));
                    var destinationPath = Path.Combine(directory, "test.jpg");
                    var instructions = new Instructions { Width = 50 };
                    ImageBuilder.Build(source, destinationPath, instructions);
                }
            };
            Assert.Throws<DirectoryNotFoundException>(action);
        }

        [Test]
        [TestCase(FitMode.Crop)]
        [TestCase(FitMode.Pad)]
        [TestCase(FitMode.Stretch)]
        public void Build_ProducesBitmapWithExactSpecifiedWidthAndHeight_GivenFitMode(FitMode mode)
        {
            using (var sourceStream = GetBitmapStream(100, 100))
            using (var targetStream = new MemoryStream())
            {
                var instructions = new Instructions { Mode = mode, Width = 12, Height = 34 };
                ImageBuilder.Build(sourceStream, targetStream, true, instructions);
                using (var output = new Bitmap(targetStream))
                {
                    Assert.AreEqual(output.Width, 12);
                    Assert.AreEqual(output.Height, 34);
                }
            }
        }

        [Test]
        public void Build_ProducesBitmapWithSmallerMaxAndSameAspectRatio_GivenFitModeMax()
        {
            using (var sourceStream = GetBitmapStream(100, 66))
            using (var targetStream = new MemoryStream())
            {
                var instructions = new Instructions { Width = 12, Height = 34 };
                ImageBuilder.Build(sourceStream, targetStream, true, instructions);
                using (var output = new Bitmap(targetStream))
                {
                    Assert.AreEqual(output.Width, 12);
                    Assert.AreEqual(output.Height, 8);
                }
            }
        }

        [Test]
        public void Build_ProducesBitmapNoLargerThanOriginal_GivenScaleModeDown()
        {
            using (var source = GetBitmapStream(100, 100))
            using (var destination = new MemoryStream())
            {
                var instructions = new Instructions { Scale = ScaleMode.DownscaleOnly, Width = 200, Height = 200 };
                ImageBuilder.Build(source, destination, true, instructions);
                using (var output = new Bitmap(destination))
                {
                    Assert.AreEqual(output.Width, 100);
                    Assert.AreEqual(output.Height, 100);
                }
            }
        }

        [Test]
        [TestCase(ScaleMode.Both)]
        [TestCase(ScaleMode.UpscaleCanvas)]
        public void Build_ProducesBitmapLargerThanOriginal_GivenScaleMode(ScaleMode scale)
        {
            using (var source = GetBitmapStream(100, 100))
            using (var destination = new MemoryStream())
            {
                var instructions = new Instructions { Scale = scale, Width = 200, Height = 200 };
                ImageBuilder.Build(source, destination, true, instructions);
                using (var output = new Bitmap(destination))
                {
                    Assert.AreEqual(output.Width, 200);
                    Assert.AreEqual(output.Height, 200);
                }
            }
        }

        [Test]
        [TestCase(50, 50, OutputFormat.Jpeg, 100)]
        [TestCase(1, 1, OutputFormat.Jpeg, 100)]
        [TestCase(50, 50, OutputFormat.Jpeg, 0)]
        [TestCase(50, 50, OutputFormat.Png)]
        public void EncodeImage(int width, int height, OutputFormat format, int? jpegQuality = null)
        {
            using (var source = GetBitmapStream(width, height))
            using (var _ = Stream.Null)
            {
                var instructions = new Instructions { Format = format, JpegQuality = jpegQuality ?? 90 };
                ImageBuilder.Build(source, _, instructions);
            }
        }

        private Bitmap GetBitmap(int width, int height)
        {
            Bitmap b = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.DrawString("Hello!", new Font(FontFamily.GenericSansSerif, 1), new SolidBrush(Color.Beige), new PointF(0, 0));
                g.Flush();
            }

            return b;
        }

        private Stream GetBitmapStream(int width, int height)
        {
            MemoryStream ms = new MemoryStream(4096);
            using (var b = GetBitmap(width, height))
            {
                b.Save(ms, ImageFormat.Png);
            }

            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
    }
}
