using System;
using System.Drawing;
using System.IO;

namespace LightResize.Tests
{
    internal sealed class ResizeJobWithExplicitConsumer : ResizeJob
    {
        public void Build(Stream source, Action<Bitmap> consumer, JobOptions jobOptions, Instructions instructions)
        {
            Build(source, (bitmap, _) => consumer?.Invoke(bitmap), jobOptions, instructions);
        }
    }
}
