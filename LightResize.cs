using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace Imazen.LightResize
{

    /// <summary>
    /// How to resolve aspect ratio differences between the requested size and the original image's size.
    /// </summary>
    public enum FitMode {
        /// <summary>
        /// Width and height are considered maximum values. The resulting image may be smaller to maintain its aspect ratio. The image may also be smaller if the source image is smaller
        /// </summary>
        Max,
        /// <summary>
        /// Width and height are considered exact values - padding is used if there is an aspect ratio difference.
        /// </summary>
        Pad,
        /// <summary>
        /// Width and height are considered exact values - cropping is used if there is an aspect ratio difference.
        /// </summary>
        Crop,
        /// <summary>
        /// Width and height are considered exact values - if there is an aspect ratio difference, the image is stretched.
        /// </summary>
        Stretch,

    }
    /// <summary>
    /// Controls whether the image is allowed to upscale, downscale, both, or if only the canvas gets to be upscaled.
    /// </summary>
    public enum ScaleMode {
        /// <summary>
        /// The default. Only downsamples images - never enlarges. If an image is smaller than 'width' and 'height', the image coordinates are used instead.
        /// </summary>
        Down,
        /// <summary>
        /// Upscales and downscales images according to 'width' and 'height'.
        /// </summary>
        Both,
        /// <summary>
        /// When the image is smaller than the requested size, padding is added instead of stretching the image
        /// </summary>
        Canvas
    }
    /// <summary>
    /// Controls the encoding format
    /// </summary>
    public enum OutputFormat
    {
        Jpg, Png, Nearest
    }
    /// <summary>
    /// Encapsulates a resizing operation.
    /// </summary>
    public class ResizeJob {

        public ResizeJob()
        {
            Mode = FitMode.Max;
            ScalingRules = ScaleMode.Down;
            JpegQuality = 90;
        }

        private readonly string[] _supportedFileExtensions = new string[] { "bmp", "gif", "exif", "png", "tif", "tiff", "tff", "jpg", "jpeg", "jpe", "jif", "jfif", "jfi" };

        public int? Width { get; set; }
        public int? Height { get; set; }
        public FitMode Mode { get; set; }
        public ScaleMode ScalingRules { get; set; }
        public int JpegQuality { get; set; }
        public Color? Matte { get; set; }


        public void LoadImage()
        {
            
        }

        protected Image Source { get; set; }
        private Size OriginalSize { get; set; }
        private ImageFormat OriginalFormat { get; set; }


        public void Layout()
        {
            //Aspect ratio of the image
            var imageRatio = (double)OriginalSize.Width / (double)OriginalSize.Height;
            //The target size for the image 
            SizeF targetSize;
            //Target area for the image
            SizeF areaSize;

            var originalRect = new RectangleF(0, 0, OriginalSize.Width, OriginalSize.Height);
  
            CopyRect = originalRect;

            if (Width.HasValue || Height.HasValue)
            {
                //A dimension was specified. 
                //We first calculate the largest size the image can be under the width/height restrictions.
                //- pretending mode=stretch and scale=both

                //Temp vars - results stored in targetSize and areaSize
                double width = Width ?? -1;
                double height = Height ?? -1;


                //Calculate missing value (a missing value is handled the same everywhere). 
                if (width > 0 && height <= 0) height = width / imageRatio;
                else if (height > 0 && width <= 0) width = height * imageRatio;

                //We now have width & height, our target size. It will only be a different aspect ratio from the image if both 'width' and 'height' are specified.

                //FitMode.Max
                if (Mode == FitMode.Max) {
                    areaSize = targetSize = BoxMath.ScaleInside(CopyRect.Size, new SizeF((float)width, (float)height));
                    //FitMode.Pad
                } else if (Mode == FitMode.Pad) {
                    areaSize = new SizeF((float)width, (float)height);
                    targetSize = BoxMath.ScaleInside(CopyRect.Size, areaSize);
                    //FitMode.crop
                } else if (Mode == FitMode.Crop) {
                    //We autocrop - so both target and area match the requested size
                    areaSize = targetSize = new SizeF((float)width, (float)height);
                    //Determine the size of the area we are copying
                    Size sourceSize = BoxMath.RoundPoints(BoxMath.ScaleInside(areaSize, CopyRect.Size));
                    //Center the portion we are copying within the manualCropSize
                    CopyRect = BoxMath.ToRectangle(BoxMath.CenterInside(sourceSize, CopyRect));

                } else { //Stretch and carve both act like stretching, so do that:
                    areaSize = targetSize = new SizeF((float)width, (float)height);
                }

            }else
            {
                //No dimensions specified, no fit mode needed. Use original dimensions
                areaSize = targetSize = OriginalSize;
            }




            //Now do upscale/downscale checks. If they take effect, set targetSize to imageSize
            if (ScalingRules == ScaleMode.Down) {
                if (BoxMath.FitsInside(originalRect.Size, targetSize)) {
                    //The image is smaller or equal to its target polygon. Use original image coordinates instead.
                    areaSize = targetSize = originalRect.Size;
                    CopyRect = originalRect;
                }
            
            } else if (ScalingRules == ScaleMode.Canvas) {
                //Same as downscaleonly, except areaSize isn't changed.
                if (BoxMath.FitsInside(originalRect.Size, targetSize)) {
                    //The image is smaller or equal to its target polygon. Use original image coordinates instead.

                    targetSize = originalRect.Size;
                    CopyRect = originalRect;
                }
            }


            //May 12: require max dimension and round values to minimize rounding differences later.
            areaSize.Width = Math.Max(1, (float)Math.Round(areaSize.Width));
            areaSize.Height = Math.Max(1, (float)Math.Round(areaSize.Height));
            targetSize.Width = Math.Max(1, (float)Math.Round(targetSize.Width));
            targetSize.Height = Math.Max(1, (float)Math.Round(targetSize.Height));

            DestSize = new Size((int)areaSize.Width,(int)areaSize.Height);
            TargetRect = BoxMath.CenterInside(targetSize, new RectangleF(0, 0, areaSize.Width, areaSize.Height));

        }

        protected RectangleF CopyRect { get; set; }
        protected RectangleF TargetRect { get; set; }
        protected Size DestSize { get; set; }
        

        public void Render()
        {
            //Create new bitmap using calculated size. 
            Dest = new Bitmap(DestSize.Width, DestSize.Height, PixelFormat.Format32bppArgb);


            //Create graphics handle
            using (Graphics g = Graphics.FromImage(Dest))
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


                //If the image doesn't support transparency, we need to fill the background color now.
                Color background = Matte ?? Color.Transparent;

                //Find out if we can safely know that nothing will be showing through or around the image.
                bool nothingToShow = (Source != null &&
                                      (Source.PixelFormat == PixelFormat.Format24bppRgb ||
                                       Source.PixelFormat == PixelFormat.Format32bppRgb ||
                                       Source.PixelFormat == PixelFormat.Format48bppRgb) &&
                                      TargetRect.Width == DestSize.Width && TargetRect.Height == DestSize.Height 
                                      && TargetRect.X == 0 && TargetRect.Y == 0);

                //Set the background to white if the background will be showing and the destination format doesn't support transparency.
                if (background == Color.Transparent && !s.supportsTransparency & !nothingToShow)
                    background = Color.White;



                //Fill background
                if (background != Color.Transparent)
                    //This causes increased aliasing at the edges - i.e., a faint white border that is even more pronounced than usual.
                    g.Clear(background); //Does this work for Color.Transparent? -- 



                using (var ia = new ImageAttributes())
                {
                    //Fixes the 50% gray border issue on bright white or dark images
                    ia.SetWrapMode(WrapMode.TileFlipXY);
                        
                    g.DrawImage(Source,TargetRect, CopyRect, GraphicsUnit.Pixel, ia);
                }
                g.Flush(FlushIntention.Flush);
            }


        }

        protected Bitmap Dest { get; set; }

        public void Encode()
        {
            
        }

        /// <summary>
        /// Looks up encoders by mime-type
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        public static ImageCodecInfo GetImageCodecInfo(string mimeType) {
            var info = ImageCodecInfo.GetImageEncoders();
            foreach (var ici in info)
                if (ici.MimeType.Equals(mimeType, StringComparison.OrdinalIgnoreCase)) return ici;
            
            return null;
        }
        /// <summary>
        /// Saves the specified image to the specified stream using jpeg compression of the specified quality.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="quality">A number between 0 and 100. Defaults to 90 if passed a negative number. Numbers over 100 are truncated to 100. 
        /// 90 is a *very* good setting.
        /// </param>
        /// <param name="target"></param>
        public static void SaveJpeg(Image b, Stream target, int quality)
        {
            //Validate quality
            if (quality < 0) quality = 90; //90 is a very good default to stick with.
            if (quality > 100) quality = 100;
            //http://msdn.microsoft.com/en-us/library/ms533844(VS.85).aspx
            //Prepare paramater for encoder
            using (var p = new EncoderParameters(1))
            {
                using (var ep = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long) quality))
                {
                    p.Param[0] = ep;
                    b.Save(target, GetImageCodecInfo("image/jpeg"), p);
                }
            }
        }

        /// <summary>
        /// Saves the image in png form. If Stream 'target' is not seekable, a temporary MemoryStream will be used to buffer the image data into the stream
        /// </summary>
        /// <param name="img"></param>
        /// <param name="target"></param>
        public static void SavePng(Image img, Stream target) {
            if (!target.CanSeek) {
                //Write to an intermediate, seekable memory stream (PNG compression requires it)
                using (var ms = new MemoryStream(4096)) {
                    img.Save(ms, ImageFormat.Png);
                    ms.WriteTo(target);
                }
            } else {
                //image/png
                //  The parameter list requires 0 bytes.
                img.Save(target, ImageFormat.Png);
            }
        }
    }



    public class BoxMath
    {

        /// <summary>
        /// Scales 'inner' to fit inside 'bounding' while maintaining aspect ratio. Upscales and downscales.
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="bounding"></param>
        /// <returns></returns>
        public static SizeF ScaleInside(SizeF inner, SizeF bounding) {
            double innerRatio = inner.Width / inner.Height;
            double outerRatio = bounding.Width / bounding.Height;

            if (outerRatio > innerRatio) {
                //Width is wider - so bound by height.
                return new SizeF((float)(innerRatio * bounding.Height), (float)(bounding.Height));
            } else {
                //Height is higher, or aspect ratios are identical.
                return new SizeF((float)(bounding.Width), (float)(bounding.Width / innerRatio));
            }
        }

        /// <summary>
        /// Returns true if 'inner' fits inside or equals 'outer'
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="outer"></param>
        /// <returns></returns>
        public static bool FitsInside(SizeF inner, SizeF outer)
        {
            return (inner.Width <= outer.Width && inner.Height <= outer.Height);
        }

        /// <summary>
        /// Rounds a floating-point rectangle to an integer rectangle using System.Round
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Rectangle ToRectangle(RectangleF r) {
            return new Rectangle((int)Math.Round(r.X), (int)Math.Round(r.Y), (int)Math.Round(r.Width), (int)Math.Round(r.Height));
        }
        /// <summary>
        /// Rounds a SizeF structure using System.Round
        /// </summary>
        /// <param name="sizeF"></param>
        /// <returns></returns>
        public static Size RoundPoints(SizeF sizeF) {
            return new Size((int)Math.Round(sizeF.Width), (int)Math.Round(sizeF.Height));
        }

        /// <summary>
        /// Creates a rectangle of size 'size' with a center matching that of bounds. No rounding is performed.
        /// </summary>
        /// <returns></returns>
        public static RectangleF CenterInside(SizeF size, RectangleF bounds) {
            return new RectangleF(bounds.Width / 2 + bounds.X - (size.Width / 2), bounds.Height / 2 + bounds.Y - (size.Height / 2), size.Width, size.Height);
        }

    }
}
