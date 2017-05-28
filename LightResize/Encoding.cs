/*
 * Copyright (c) 2017 stakx
 * Copyright (c) 2012 Imazen
 *
 * This software is not a replacement for ImageResizer (http://imageresizing.net); and is not optimized for use within an ASP.NET application.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
 * documentation files (the "Software"), to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO
 * THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace LightResize
{
    /// <summary>
    /// Provides adjustable JPEG encoding and 32-bit PNG encoding methods.
    /// </summary>
    internal static class Encoding
    {
        /// <summary>
        /// Looks up encoders by MIME type.
        /// </summary>
        /// <param name="mimeType">The MIME type to look up.</param>
        /// <returns>The <see cref="ImageCodecInfo"/> that matches the given MIME type, or <c>null</c> if no match was found.</returns>
        public static ImageCodecInfo GetImageCodecInfo(string mimeType)
        {
            var info = ImageCodecInfo.GetImageEncoders();
            foreach (var ici in info)
            {
                if (ici.MimeType.Equals(mimeType, StringComparison.OrdinalIgnoreCase))
                {
                    return ici;
                }
            }

            return null;
        }

        /// <summary>
        /// Saves the specified image to the specified stream using JPEG compression of the specified quality.
        /// </summary>
        /// <param name="b">The bitmap image to save.</param>
        /// <param name="target">The stream to write to.</param>
        /// <param name="quality">A number between 0 and 100. Defaults to 90 if passed a negative number. Numbers over 100 are truncated to 100. 90 is a *very* good setting.</param>
        public static void SaveJpeg(Image b, Stream target, int quality)
        {
            // JPEG compression quality should have already been validated.
            Debug.Assert(quality >= 0 && quality <= 100, "There is an unexpected code path for setting " + nameof(Instructions) + "." + nameof(Instructions.JpegQuality) + " to a value outside the range of [0..100].");

            // http://msdn.microsoft.com/en-us/library/ms533844(VS.85).aspx
            // Prepare parameter for encoder
            using (var p = new EncoderParameters(1))
            {
                using (var ep = new EncoderParameter(Encoder.Quality, (long)quality))
                {
                    p.Param[0] = ep;
                    b.Save(target, GetImageCodecInfo("image/jpeg"), p);
                }
            }
        }

        /// <summary>
        /// Saves the image in PNG format. If the <see cref="Stream"/> denoted by <paramref name="target"/> is not seekable, a temporary <see cref="MemoryStream"/> will be used to buffer the image data into the stream.
        /// </summary>
        /// <param name="img">The bitmap image to save.</param>
        /// <param name="target">The stream to write to.</param>
        public static void SavePng(Image img, Stream target)
        {
            if (!target.CanSeek)
            {
                // Write to an intermediate, seekable memory stream (PNG compression requires it)
                using (var ms = new MemoryStream(4096))
                {
                    img.Save(ms, ImageFormat.Png);
                    ms.WriteTo(target);
                }
            }
            else
            {
                // image/png
                // The parameter list requires 0 bytes.
                img.Save(target, ImageFormat.Png);
            }
        }
    }
}
