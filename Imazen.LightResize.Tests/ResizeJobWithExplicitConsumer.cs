using System;
using System.Drawing;
using System.IO;

namespace Imazen.LightResize.Tests
{
    internal sealed class ResizeJobWithExplicitConsumer : ResizeJob
    {
        public void Build(Stream source, JobOptions jobOptions, Action<Bitmap> consumer)
        {
            base.Build(source, (bitmap, _) => consumer?.Invoke(bitmap), jobOptions);
        }
    }
}
