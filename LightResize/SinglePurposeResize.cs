/*
 * Copyright (c) 2017 stakx
 * Copyright (c) 2012 Imazen 
 * 
 * This software is not a replacement for ImageResizer (http://imageresizing.net); and is not designed for use within an ASP.NET application.
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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace LightResize {
    /// <summary>
    /// This implementation of image resizing sacrifices 30-50% performance for simplicty. 
    /// It only supports max-constraints resizing, JPEG encoding, and stream to stream processing.
    /// </summary>
    public class SinglePurposeResize {
        /// <summary>
        /// Performs an image resize operation by reading from a file, applying to specified max constraints and writing to a file with the specified JPEG encoding quality.
        /// </summary>
        /// <remarks>
        /// Less efficient than <see cref="ResizeJob"/>, and cannot resize files in place. The caller MUST ensure that the first stream is disposed if the second stream fails to open.
        /// I.e, place the first stream in a <c>using()</c> clause and open the second stream inside it before calling <see cref="Resize"/>.
        /// </remarks>
        /// <param name="source">The stream to read from.</param>
        /// <param name="destination">The stream to write to.</param>
        /// <param name="maxwidth">The width constraint.</param>
        /// <param name="maxheight">The height constraint.</param>
        /// <param name="jpegQuality">The JPEG encoding quality to use. A value between 0 and 100. 90 is the best value and the default.</param>
        public static void Resize(Stream source, Stream destination, int? maxwidth, int? maxheight, int jpegQuality = 90)
        {
            //Ensure source bitmap, source stream, and target stream are disposed in that order.
            using (destination)
            using (source)
            using (var b = new Bitmap(source,true))
            {
                var orig = b.Size;

                //Aspect ratio of the source image
                var imageRatio = (double) orig.Width/(double) orig.Height;

                //Establish outer bounds.
                double w = maxwidth ?? -1;
                double h = maxheight ?? -1;
                if (w < 1 && h < 1)
                {
                    w = orig.Width;
                    h = orig.Height;
                    //No change? Render-as-is...
                    RenderAndEncode(b, orig, destination, jpegQuality);
                    return;
                }
                else if (w < 1) h = w/imageRatio;
                else if (h < 1) w = h*imageRatio;

                //Scale within bounds
                Size scaled;
                var boundsRatio = w/h;
                if (boundsRatio > imageRatio)//Width is wider - so bound by height.
                    scaled = new Size((int) Math.Ceiling(imageRatio*h), (int) Math.Ceiling(h));
                else //Height is higher, or aspect ratios are identical.
                    scaled = new Size((int) Math.Ceiling(w), (int)Math.Ceiling (w/imageRatio));

                //Don't permit upscaling
                if (orig.Width <= scaled.Width && orig.Height <= scaled.Height) scaled = orig;
                //Render
                RenderAndEncode(b,scaled,destination,jpegQuality);

            }
        }

        /// <summary>
        /// Resizes the provided bitmap to the given target size and writes it to the target stream with jpeg encoding.
        /// Warning: Does NOT dispose the source bitmap, source stream, or target stream! 
        /// </summary>
        /// <param name="bitmap">The source bitmap.</param>
        /// <param name="targetSize">The target size.</param>
        /// <param name="destination">The stream to write to.</param>
        /// <param name="jpegQuality">The JPEG encoding quality to use. A value between 0 and 100. 90 is the best value.</param>
        private static void RenderAndEncode(Bitmap bitmap, Size targetSize, Stream destination, int jpegQuality)
        {
            //Validate quality
            if (jpegQuality < 0) jpegQuality = 90; //90 is a very good default to stick with.
            if (jpegQuality > 100) jpegQuality = 100;

            //Create new bitmap using calculated size. 
            using(var canvas = new Bitmap(targetSize.Width, targetSize.Height, PixelFormat.Format32bppArgb))
            {

                //Create graphics handle
                using (var g = Graphics.FromImage(canvas))
                {
                    //HQ Bicubic is 2 pass. It's only 30% slower than low quality, why not have HQ results?
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    //Ensures the edges are crisp
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    //Prevents artifacts at the edges
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    //Ensures matted PNGs look decent
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    //Prevents really ugly transparency issues
                    g.CompositingMode = CompositingMode.SourceOver;

                    //Fill background color in case original was transparent
                    g.Clear(Color.White);

                    using (var ia = new ImageAttributes())
                    {
                        //Fixes the 50% gray border issue on bright white or dark images
                        ia.SetWrapMode(WrapMode.TileFlipXY);

                        //Render!
                        g.DrawImage(bitmap,new Rectangle(0,0,targetSize.Width,targetSize.Height),0,0,bitmap.Width,bitmap.Height, GraphicsUnit.Pixel, ia);
                    }
                    g.Flush(FlushIntention.Flush);
                }
                //Locate Jpeg codec
                var codecs = ImageCodecInfo.GetImageEncoders();
                foreach (var ici in codecs)
                {
                    if (ici.MimeType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
                    {
                        //Create encoder paramter array and encoder parameter safely
                        using (var p = new EncoderParameters(1))
                        using (var ep = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)jpegQuality)) {
                            p.Param[0] = ep;
                            bitmap.Save(destination, ici, p);
                        }
                        break;
                    }
                }


            }
        }

    }
}
