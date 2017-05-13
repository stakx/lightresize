using System;
using System.Drawing;
using System.IO;

namespace LightResize.Tests
{
    internal sealed class ResizeJobWithExplicitConsumer : ResizeJob
    {
        public void Build(Stream source, JobOptions jobOptions, Action<Bitmap> consumer)
        {
            Build(source, (bitmap, _) => consumer?.Invoke(bitmap), jobOptions);
        }
    }
}
