/*
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

namespace Imazen.LightResize {
    /// <summary>
    /// This implementation of image resizing sacrifices 30-50% performance for simplicty. 
    /// It only supports max-constraints resizing, jpeg encoding, and stream->stream processing.
    /// </summary>
    public class SinglePurposeResize {
        /// <summary>
        /// Less efficient than LightResize, and cannot resize files in place. Caller MUST ensure the first stream is disposed if the second stream fails to open. 
        /// 
        /// I.e, place first stream in using(){} clause, and open the second stream inside it before calling Resize()
        /// </summary>
        /// <param name="s"></param>
        /// <param name="target"></param>
        /// <param name="maxwidth"></param>
        /// <param name="maxheight"></param>
        /// <param name="jpegQuality"></param>
        public static void Resize(Stream s, Stream target, int? maxwidth, int? maxheight, int jpegQuality = 90)
        {
            //Ensure source bitmap, source stream, and target stream are disposed in that order.
            using (target)
            using (s)
            using (var b = new Bitmap(s,true))
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
                    RenderAndEncode(b, orig, target, jpegQuality);
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
                RenderAndEncode(b,scaled,target,jpegQuality);

            }
        }

        /// <summary>
        /// Resizes the provided bitmap to the given target size and writes it to the target stream with jpeg encoding.
        /// Warning: Does NOT dispose the source bitmap, source stream, or target stream! 
        /// </summary>
        /// <param name="b">The source bitmap</param>
        /// <param name="targetSize">The target size</param>
        /// <param name="target">The target stream</param>
        /// <param name="jpegQuality">The target quality</param>
        private static void RenderAndEncode(Bitmap b, Size targetSize, Stream target, int jpegQuality)
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
                        g.DrawImage(b,new Rectangle(0,0,targetSize.Width,targetSize.Height),0,0,b.Width,b.Height, GraphicsUnit.Pixel, ia);
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
                            b.Save(target, ici, p);
                        }
                        break;
                    }
                }


            }
        }

    }
}
