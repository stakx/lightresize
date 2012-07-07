using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

// Goal: 
// Smallest implementation of Stream->different Stream resizing for jpeg only, max bounds resizing only, streams auto-disposed.
// 30-45% Performance sacrifices were made to render implementation more concise.
// 


namespace Imazen.LightResize {
    public class SinglePurposeResize {
        /// <summary>
        /// Less efficient than LightResize, and cannot resize files in place. 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="target"></param>
        /// <param name="maxwidth"></param>
        /// <param name="maxheight"></param>
        /// <param name="jpegQuality"></param>
        public static void Resize(Stream s, Stream target, int? maxwidth, int? maxheight, int jpegQuality = 90)
        {
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
        /// All rendering occurs here; 
        /// </summary>
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
