using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

/*
 * Goal: Make the shortest possible implementation of image resizing that:
 * 1) Does not introduce visual artifacts or sacrifice quality
 * 2) Does not leak or waste any memory
 * 3) Does not have rounding or mathematical errors
 * 4) Can go from filename->filename, stream->stream, stream->filename, filename->stream, and filename->same filename
 * 5) Encodes optimally to jpeg and reliably to PNG.
 * 6) Offers 4 constraint modes: max, pad, crop, and stretch (Only adds 30 lines of code)
 * 7) Offers upscaling prevention options
 * 
 * What I had to sacrifice:
 * 
 * ASP.NET support, 
 * Crop/padding alignment selection, manual cropping, source rotate/final rotate, flipping, 
 * 
 * All GIF support, 8-bit PNG support, automatic output format selection based on input format, 
 * all file extension and mime-type logic, 
 * 
 * 
 */

namespace Imazen.LightResize
{
    /// <summary>
    /// Allows you to customize how IO is handled
    /// </summary>
    [Flags]
    public enum JobOptions
    {
        /// <summary>
        /// Instructs ResizeJob to leave the source stream open even after it is no longer needed for the job.
        /// </summary>
        LeaveSourceStreamOpen, 
        /// <summary>
        /// The source stream will be rewound to its original position after it is used. (Useful for reusing a stream or HttpFileUpload)
        /// Implies LeaveSourceStreamOpen
        /// </summary>
        RewindSourceStream,
        /// <summary>
        /// Instructs ResizeJob to leave the target stream open after it is finished writing. Make sure you close it externally!
        /// </summary>
        LeaveTargetStreamOpen,
        /// <summary>
        /// Instructs ResizeJob to preserve the target bitmap (will cause mem leak unless disposed externally)
        /// </summary>
        PreserveTargetBitmap,
        /// <summary>
        /// When a filename is specified, instructs ResizeJob to create any needed parent folder levels
        /// </summary>
        CreateParentDirectory,
        /// <summary>
        /// The source stream will be copied into a memory-based stream so the original stream can be closed earlier. Required if you are writing to the same file you are reading from.
        /// </summary>
        BufferEntireSourceStream
    }
    /// <summary>
    /// Encapsulates a resizing operation.
    /// </summary>
    public class ResizeJob {

        public int? Width { get; set; }
        public int? Height { get; set; }
        public FitMode Mode { get; set; }
        public ScaleMode ScalingRules { get; set; }
        public OutputFormat Format { get; set; }
        public int JpegQuality { get; set; }
        public Color? Matte { get; set; }
        /// <summary>
        /// If true, the ICC profile will be ignored instead of being applied
        /// </summary>
        public bool IgnoreIccProfile { get; set; }

        public ResizeJob()
        {
            Mode = FitMode.Max;
            ScalingRules = ScaleMode.Down;
            JpegQuality = 90;
            IgnoreIccProfile = false;
            Format = OutputFormat.Jpg;
        }
        
        public void Build (string sourcePath, string destPath, JobOptions options)
        {
            if (sourcePath == destPath)
                options = options | JobOptions.BufferEntireSourceStream;

            Build(File.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read), destPath, options);
        }

        public void Build(string sourcePath, Stream target, JobOptions options) {
            
            Build(File.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read), target, options);
        }


        public void Build(Stream s, string destPath, JobOptions options) {

            var createParents = ((options & JobOptions.CreateParentDirectory) != 0);
            if (createParents) {
                string dirName = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(dirName)) Directory.CreateDirectory(dirName);
            }

            Build(s,
                  delegate(Bitmap b, JobOptions option) {
                      using (var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write)) {
                          Encode(fs);
                      }


                  }, options);
        }


        public void Build(Stream s, Stream target, JobOptions options)
        {
            Build(s, delegate(Bitmap b, JobOptions opts)
                         {
                             try
                             {
                                 //Encode from temp bitmap to target stream
                                 Encode(target);
                             }
                             finally
                             {
                                 //Ensure target stream is disposed if requested
                                 if ((opts & JobOptions.LeaveTargetStreamOpen) == 0) target.Dispose();
                             }

                         }, options);

        }

        protected delegate void BitmapConsumer(Bitmap b, JobOptions options);

        /// <summary>
        /// Loads the bitmap from stream, processes, and renders, sending the result Bitmap to the 'consumer' callback for encoding or usage. 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="consumer"></param>
        /// <param name="options"></param>
        protected void Build(Stream s, BitmapConsumer consumer, JobOptions options)
        {
            var leaveSourceStreamOpen = ((options & JobOptions.LeaveSourceStreamOpen) != 0 ||
                                 (options & JobOptions.RewindSourceStream) != 0);
            
            var bufferSource = ((options & JobOptions.BufferEntireSourceStream) != 0);
            var originalPosition = ((options & JobOptions.RewindSourceStream) != 0) ? s.Position : -1;
            var preserveTemp = ((options & JobOptions.PreserveTargetBitmap) != 0);

            try
            {
                try
                {
                    //Buffer source stream if requested
                    UnderlyingStream = bufferSource ? StreamUtils.CopyToMemoryStream(s) : s;

                    //Allow early disposal (enables same-file edits)
                    if (bufferSource && !leaveSourceStreamOpen)
                    {
                        s.Dispose();
                        s = null;
                    }
                    //Load bitmap
                    Source = new Bitmap(UnderlyingStream, !IgnoreIccProfile);
                    //Use size
                    OriginalSize = Source.Size;

                    //Do math
                    Layout();
                    //Render to 'Dest'
                    Render();
                }
                finally
                {
                    try
                    {
                        //Dispose loaded bitmap instance
                        if (Source != null) Source.Dispose();
                        Source = null;
                    }
                    finally
                    {
                        try
                        {
                            //Dispose buffer
                            if (UnderlyingStream != null && s != UnderlyingStream) UnderlyingStream.Dispose();
                        }
                        finally
                        {
                            //Dispose source stream or restore its position
                            if (!leaveSourceStreamOpen && s != null) s.Dispose();
                            else if (originalPosition > -1 && s != null && s.CanSeek) s.Position = originalPosition;
                        }
                    }
                }
                //Fire callback to write to disk or use Bitmap instance directly
                consumer(Dest,options);
            }
            finally
            {
                //Temporary bitmap must  be disposed
                if (!preserveTemp && Dest != null) {
                    Dest.Dispose();
                    Dest = null;
                }
            }

        }

        /// <summary>
        /// The source bitmap
        /// </summary>
        protected Image Source { get; set; }
        /// <summary>
        /// The stream underlying the bitmap -- cannot be disposed before the bitmap
        /// </summary>
        protected Stream UnderlyingStream { get; set; }

        /// <summary>
        /// The dimensions of the source image
        /// </summary>
        protected Size OriginalSize { get; set; }

        /// <summary>
        /// Layout: size and cropping constraints are calculated here
        /// </summary>
        protected virtual void Layout()
        {
            //Aspect ratio of the source image
            var imageRatio = (double)OriginalSize.Width / (double)OriginalSize.Height;

            //Target image size
            SizeF targetSize;
            //Target canvas size
            SizeF canvasSize;

            var originalRect = new RectangleF(0, 0, OriginalSize.Width, OriginalSize.Height);
  
            CopyRect = originalRect;

            //Normalize
            if (Width.HasValue && Width < 1) Width = null;
            if (Height.HasValue && Height < 1) Height = null;

            if (Width.HasValue || Height.HasValue)
            {
                //Establish constraint bounds
                SizeF bounds = Width.HasValue && Height.HasValue
                                   ? new SizeF((float) Width, (float) Height)
                                   : (Width.HasValue
                                         ? new SizeF((float) Width, (float) ((double) Width/imageRatio))
                                         : (Height.HasValue
                                               ? new SizeF((float) ((double)Height*imageRatio),(float)Height)
                                               : SizeF.Empty));



                //We now have width & height, our target size. It will only be a different aspect ratio from the image if both 'width' and 'height' are specified.

                //FitMode.Max
                if (Mode == FitMode.Max) {
                    canvasSize = targetSize = BoxMath.ScaleInside(CopyRect.Size, bounds);
                    //FitMode.Pad
                } else if (Mode == FitMode.Pad){
                    canvasSize = bounds;
                    targetSize = BoxMath.ScaleInside(CopyRect.Size, canvasSize);
                    //FitMode.crop
                } else if (Mode == FitMode.Crop) {
                    //We autocrop - so both target and area match the requested size
                    canvasSize = targetSize = bounds;
                    //Determine the size of the area we are copying
                    var sourceSize = BoxMath.RoundPoints(BoxMath.ScaleInside(canvasSize, CopyRect.Size));
                    //Center the portion we are copying within the manualCropSize
                    CopyRect = BoxMath.ToRectangle(BoxMath.CenterInside(sourceSize, CopyRect));

                } else { //Stretch and carve both act like stretching, so do that:
                    canvasSize = targetSize = bounds;
                }

            }else
            {
                //No dimensions specified, no fit mode needed. Use original dimensions
                canvasSize = targetSize = OriginalSize;
            }

            //Now, unless upscaling is enabled, ensure the image is no larger than it was originally
            if (ScalingRules != ScaleMode.Both && BoxMath.FitsInside(OriginalSize, targetSize))
            {
                targetSize = OriginalSize;
                CopyRect = originalRect;
                //And reset the canvas size, unless canvas upscaling is enabled.
                if (ScalingRules != ScaleMode.Canvas) canvasSize = targetSize;
            }
            

            //May 12: require max dimension and round values to minimize rounding differences later.
            canvasSize.Width = Math.Max(1, (float)Math.Round(canvasSize.Width));
            canvasSize.Height = Math.Max(1, (float)Math.Round(canvasSize.Height));
            targetSize.Width = Math.Max(1, (float)Math.Round(targetSize.Width));
            targetSize.Height = Math.Max(1, (float)Math.Round(targetSize.Height));

            DestSize = new Size((int)canvasSize.Width,(int)canvasSize.Height);
            TargetRect = BoxMath.CenterInside(targetSize, new RectangleF(0, 0, canvasSize.Width, canvasSize.Height));

        }

        /// <summary>
        /// Which part of the source image to copy
        /// </summary>
        protected RectangleF CopyRect { get; set; }
        /// <summary>
        /// Where on the target canvas to render the source image
        /// </summary>
        protected RectangleF TargetRect { get; set; }
        /// <summary>
        /// The size to create the target image
        /// </summary>
        protected Size DestSize { get; set; }

        /// <summary>
        /// All rendering occurs here; see layout for the math part of things. Neither 'Dest' nor 'Source' are disposed here!
        /// </summary>
        protected virtual void Render()
        {
            //Create new bitmap using calculated size. 
            Dest = new Bitmap(DestSize.Width, DestSize.Height, PixelFormat.Format32bppArgb);

            //Create graphics handle
            using (var g = Graphics.FromImage(Dest))
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
                var background = Matte ?? Color.Transparent;

                //Find out if we can safely know that nothing will be showing through or around the image.
                var nothingToShow = ((Source.PixelFormat == PixelFormat.Format24bppRgb ||
                                       Source.PixelFormat == PixelFormat.Format32bppRgb ||
                                       Source.PixelFormat == PixelFormat.Format48bppRgb) &&
                                      TargetRect.Width == DestSize.Width && TargetRect.Height == DestSize.Height 
                                      && TargetRect.X == 0 && TargetRect.Y == 0);

                //Set the background to white if the background will be showing and the destination format doesn't support transparency.
                if (background == Color.Transparent && Format == OutputFormat.Jpg & !nothingToShow)
                    background = Color.White;

                //Fill background
                if (background != Color.Transparent) g.Clear(background);

                using (var ia = new ImageAttributes())
                {
                    //Fixes the 50% gray border issue on bright white or dark images
                    ia.SetWrapMode(WrapMode.TileFlipXY);

                    //Make poly from rectF
                    var r= new PointF[3];
                    r[0] = TargetRect.Location;
                    r[1] = new PointF(TargetRect.Right, TargetRect.Top);
                    r[2] = new PointF(TargetRect.Left, TargetRect.Bottom);
                    //Render!
                    g.DrawImage(Source,r, CopyRect, GraphicsUnit.Pixel, ia);
                }
                g.Flush(FlushIntention.Flush);
            }


        }

        /// <summary>
        /// The Bitmap object the target image is rendered to before encoding.
        /// </summary>
        protected Bitmap Dest { get; set; }

        /// <summary>
        /// Dest is encoded. 
        /// </summary>
        /// <param name="target"></param>
        protected virtual void Encode(Stream target)
        {
            if (Format == OutputFormat.Jpg) Encoding.SaveJpeg(Dest,target,JpegQuality);
            else if (Format == OutputFormat.Png) Encoding.SavePng(Dest,target);
        }

    
    }

    /// <summary>
    /// Provides adjustable Jpeg encoding and 32-bit PNG encoding methods
    /// </summary>
    public class Encoding
    {
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
        public static void SaveJpeg(Image b, Stream target, int quality) {
            //Validate quality
            if (quality < 0) quality = 90; //90 is a very good default to stick with.
            if (quality > 100) quality = 100;
            //http://msdn.microsoft.com/en-us/library/ms533844(VS.85).aspx
            //Prepare paramater for encoder
            using (var p = new EncoderParameters(1)) {
                using (var ep = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality)) {
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

    /// <summary>
    /// Provides simple layout math
    /// </summary>
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

    /// <summary>
    /// Provides  methods for copying streams
    /// </summary>
    public static class StreamUtils
    {

        /// <summary>
        /// Copies the remaining data in the current stream to a new MemoryStream instance.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static MemoryStream CopyToMemoryStream(Stream s)
        {
            return CopyToMemoryStream(s, false);
        }

        /// <summary>
        /// Copies the current stream into a new MemoryStream instance.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="entireStream">True to copy entire stream if seeakable, false to only copy remaining data</param>
        /// <returns></returns>
        public static MemoryStream CopyToMemoryStream(Stream s, bool entireStream)
        {
            return CopyToMemoryStream(s, entireStream, 0x1000);
        }

        /// <summary>
        /// Copies the current stream into a new MemoryStream instance.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="entireStream">True to copy entire stream if seeakable, false to only copy remaining data</param>
        /// <param name="chunkSize">The buffer size to use (in bytes) if a buffer is required. Default: 4KiB</param>
        /// <returns></returns>
        public static MemoryStream CopyToMemoryStream(Stream s, bool entireStream, int chunkSize)
        {
            MemoryStream ms =
                new MemoryStream(s.CanSeek ? ((int) s.Length + 8 - (entireStream ? 0 : (int) s.Position)) : chunkSize);
            CopyToStream(s, ms, entireStream, chunkSize);
            ms.Position = 0;
            return ms;
        }

        /// <summary>
        /// Copies the remaining data from the this stream into the given stream.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="other">The stream to write to</param>
        public static void CopyToStream(Stream s, Stream other)
        {
            CopyToStream(s, other, false);
        }

        /// <summary>
        /// Copies this stream into the given stream
        /// </summary>
        /// <param name="s"></param>
        /// <param name="other">The stream to write to</param>
        /// <param name="entireStream">True to copy entire stream if seeakable, false to only copy remaining data</param>
        public static void CopyToStream(Stream s, Stream other, bool entireStream)
        {
            CopyToStream(s, other, entireStream, 0x1000);
        }





        /// <summary>
        /// Copies this stream into the given stream
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest">The stream to write to</param>
        /// <param name="entireStream">True to copy entire stream if seeakable, false to only copy remaining data</param>
        /// <param name="chunkSize">True to copy entire stream if seeakable, false to only copy remaining data</param>
        public static void CopyToStream(Stream src, Stream dest, bool entireStream, int chunkSize)
        {
            if (entireStream && src.CanSeek) src.Seek(0, SeekOrigin.Begin);

            if (src is MemoryStream && src.CanSeek)
            {
                try
                {
                    int pos = (int) src.Position;
                    dest.Write(((MemoryStream) src).GetBuffer(), pos, (int) (src.Length - pos));
                    return;
                }
                catch (UnauthorizedAccessException) //If we can't slice it, then we read it like a normal stream
                {
                }
            }
            if (dest is MemoryStream && src.CanSeek)
            {
                try
                {
                    int srcPos = (int) src.Position;
                    int pos = (int) dest.Position;
                    int length = (int) (src.Length - srcPos) + pos;
                    dest.SetLength(length);

                    var data = ((MemoryStream) dest).GetBuffer();
                    while (pos < length)
                    {
                        pos += src.Read(data, pos, length - pos);
                    }
                    return;
                }
                catch (UnauthorizedAccessException) //If we can't write directly, fall back
                {
                }
            }
            int size = (src.CanSeek) ? Math.Min((int) (src.Length - src.Position), chunkSize) : chunkSize;
            byte[] buffer = new byte[size];
            int n;
            do
            {
                n = src.Read(buffer, 0, buffer.Length);
                dest.Write(buffer, 0, n);
            } while (n != 0);
        }


    }

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
    public enum OutputFormat {
        Jpg, 
        Png
    }
}
