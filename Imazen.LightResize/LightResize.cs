/*
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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Imazen.LightResize
{
    /// <summary>
    /// Instructs <see cref="ResizeJob"/> how to handle I/O (e.g. whether to buffer, dispose, and/or rewind the source stream, or whether to dispose the destination stream).
    /// </summary>
    [Flags]
    public enum JobOptions
    {
        /// <summary>
        /// Instructs <see cref="ResizeJob"/> to leave the source stream open even after it is no longer needed for the job.
        /// </summary>
        LeaveSourceStreamOpen = 1,
        /// <summary>
        /// Instructs <see cref="ResizeJob"/> to rewind the source stream to its original position after it has been used. (This is useful when reusing a stream or <c>HttpFileUpload</c>.)
        /// Implies <see cref="LeaveSourceStreamOpen"/>.
        /// </summary>
        RewindSourceStream = 2,
        /// <summary>
        /// Instructs <see cref="ResizeJob"/> to leave the target stream open after it is finished writing. Make sure you close it externally!
        /// </summary>
        LeaveTargetStreamOpen = 4,
        /// <summary>
        /// Instructs <see cref="ResizeJob"/> to preserve the target bitmap. (This will cause a memory leak unless disposed externally).
        /// </summary>
        PreserveTargetBitmap = 8,
        /// <summary>
        /// Instructs <see cref="ResizeJob"/> to create any needed parent folder levels when a file path is specified as the destination.
        /// </summary>
        CreateParentDirectory = 16,
        /// <summary>
        /// Instructs <see cref="ResizeJob"/> to copy the source stream into a memory buffer so that it can be closed earlier. (This is required if you are writing to the same file that you are reading from.)
        /// </summary>
        BufferEntireSourceStream = 32
    }
    /// <summary>
    /// Encapsulates a resizing operation. Very limited compared to ImageResizer, absolutely no ASP.NET support.
    /// </summary>
    public class ResizeJob {

        /// <summary>
        /// Gets or sets the width constraint.
        /// </summary>
        public int? Width { get; set; }
        /// <summary>
        /// Gets or sets the height constraint.
        /// </summary>
        public int? Height { get; set; }
        /// <summary>
        /// Gets or sets the constraint mode. Defaults to <see cref="FitMode.Max"/>.
        /// </summary>
        public FitMode Mode { get; set; }
        /// <summary>
        /// Gets or sets whether upscaling be permitted. Defaults to <see cref="ScaleMode.Down"/> (i.e. downscaling only).
        /// </summary>
        public ScaleMode ScalingRules { get; set; }
        /// <summary>
        /// Gets or sets the encoding format to use when writing the resized image to the destination stream. Defaults to <see cref="OutputFormat.Jpg"/>.
        /// </summary>
        public OutputFormat Format { get; set; }
        /// <summary>
        /// Gets or sets the JPEG encoding quality to use. 90 is the best value and the default. Seriously.
        /// </summary>
        public int JpegQuality { get; set; }
        /// <summary>
        /// Gets or sets the background color to apply. <c>null</c> denotes transparency. <see cref="Color.White"/> will be used if the encoding format (<see cref="Format"/>) is <see cref="OutputFormat.Jpg"/> and this is unspecified.
        /// </summary>
        public Color? Matte { get; set; }
        /// <summary>
        /// If <c>true</c>, the ICC profile will be ignored instead of being applied.
        /// </summary>
        public bool IgnoreIccProfile { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="ResizeJob"/>.
        /// </summary>
        /// <param name="mode">The initial value for the <see cref="Mode"/> property. Defaults to <see cref="FitMode.Max"/>.</param>
        /// <param name="scalingRules">The initial value for the <see cref="ScalingRules"/> property. Defaults to <see cref="ScaleMode.Down"/>.</param>
        /// <param name="ignoreIccProfile">The initial value for the <see cref="IgnoreIccProfile"/> property. Defaults to <c>false</c>.</param>
        /// <param name="format">The initial value for the <see cref="Format"/> property. Defaults to <see cref="OutputFormat.Jpg"/>.</param>
        /// <param name="jpegQuality">The initial value for the <see cref="JpegQuality"/> property. Defaults to 90.</param>
        public ResizeJob(FitMode mode = FitMode.Max, ScaleMode scalingRules = ScaleMode.Down, bool ignoreIccProfile = false, OutputFormat format = OutputFormat.Jpg, int jpegQuality = 90)
        {
            Mode = mode;
            ScalingRules = scalingRules;
            JpegQuality = jpegQuality;
            IgnoreIccProfile = ignoreIccProfile;
            Format = format;
        }
        /// <summary>
        /// Performs the image resize operation by reading from a file and writing to a file.
        /// </summary>
        /// <param name="sourcePath">The path of the file to read from.</param>
        /// <param name="destinationPath">The path of the file to write to.</param>
        /// <param name="options">Specifies how <see cref="ResizeJob"/> should handle I/O (e.g. whether to buffer, rewind, and/or dispose the source stream, and whether to dispose the target stream).</param>
        public void Build (string sourcePath, string destinationPath, JobOptions options)
        {
            if (sourcePath == destinationPath)
                options = options | JobOptions.BufferEntireSourceStream;

            Build(File.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read), destinationPath, options);
        }
        /// <summary>
        /// Performs the image resize operation by reading from a file and writing to a <see cref="Stream"/>.
        /// </summary>
        /// <param name="sourcePath">The path of the file to read from.</param>
        /// <param name="destination">The stream to write to.</param>
        /// <param name="options">Specifies how <see cref="ResizeJob"/> should handle I/O (e.g. whether to buffer, rewind, and/or dispose the source stream, and whether to dispose the target stream).</param>
        public void Build(string sourcePath, Stream destination, JobOptions options) {
            
            Build(File.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read), destination, options);
        }

        /// <summary>
        /// Performs the image resize operation by reading from a <see cref="Stream"/> and writing to a file.
        /// </summary>
        /// <param name="source">The stream to read from.</param>
        /// <param name="destinationPath">The path of the file to write to.</param>
        /// <param name="options">Specifies how <see cref="ResizeJob"/> should handle I/O (e.g. whether to buffer, rewind, and/or dispose the source stream, and whether to dispose the target stream).</param>
        public void Build(Stream source, string destinationPath, JobOptions options) {

            var createParents = ((options & JobOptions.CreateParentDirectory) != 0);
            if (createParents) {
                string dirName = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(dirName)) Directory.CreateDirectory(dirName);
            }

            Build(source,
                  delegate(Bitmap b, JobOptions option) {
                      using (var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write)) {
                          Encode(fs);
                      }

                  }, options);
        }

        /// <summary>
        /// Performs the image resize operation by reading from a <see cref="Stream"/> and writing to a <see cref="Stream"/>.
        /// </summary>
        /// <param name="source">The stream to read from.</param>
        /// <param name="destination">The stream to write to.</param>
        /// <param name="options">Specifies how <see cref="ResizeJob"/> should handle I/O (e.g. whether to buffer, rewind, and/or dispose the source stream, and whether to dispose the target stream).</param>
        /// <remarks>
        /// Ensure that the first stream you open will be safely closed if the second stream fails to open! This means a <c>using()</c> or <c>try</c>/<c>finally</c> clause.
        /// </remarks>
        public void Build(Stream source, Stream destination, JobOptions options)
        {
            Build(source, delegate(Bitmap b, JobOptions opts)
                         {
                             try
                             {
                                 //Encode from temp bitmap to target stream
                                 Encode(destination);
                             }
                             finally
                             {
                                 //Ensure target stream is disposed if requested
                                 if ((opts & JobOptions.LeaveTargetStreamOpen) == 0) destination.Dispose();
                             }

                         }, options);

        }
        /// <summary>
        /// Allows callers to handle the encoding/usage phase.
        /// </summary>
        /// <param name="bitmap">The resized bitmap image.</param>
        /// <param name="options">Specifies how <see cref="ResizeJob"/> should handle I/O (e.g. whether to buffer, rewind, and/or dispose the source stream, and whether to dispose the target stream).</param>
        protected delegate void BitmapConsumer(Bitmap bitmap, JobOptions options);

        /// <summary>
        /// Loads the bitmap from stream, processes, and renders, sending the result <see cref="Bitmap"/> to the <paramref name="consumer"/> callback for encoding or usage.
        /// </summary>
        /// <param name="source">The <see cref="Stream"/> to read from.</param>
        /// <param name="consumer">The <see cref="BitmapConsumer"/> that will receive the resized <see cref="Bitmap"/> for further processing (e.g. writing to a destination).</param>
        /// <param name="options">Specifies how <see cref="ResizeJob"/> should handle I/O (e.g. whether to buffer, rewind, and/or dispose the source stream, and whether to dispose the target stream).</param>
        protected void Build(Stream source, BitmapConsumer consumer, JobOptions options)
        {
            var leaveSourceStreamOpen = ((options & JobOptions.LeaveSourceStreamOpen) != 0 ||
                                 (options & JobOptions.RewindSourceStream) != 0);
            
            var bufferSource = ((options & JobOptions.BufferEntireSourceStream) != 0);
            var originalPosition = ((options & JobOptions.RewindSourceStream) != 0) ? source.Position : -1;
            var preserveTemp = ((options & JobOptions.PreserveTargetBitmap) != 0);

            try
            {
                try
                {
                    //Buffer source stream if requested
                    UnderlyingStream = bufferSource ? StreamUtils.CopyToMemoryStream(source, true, 0x1000) : source;

                    //Allow early disposal (enables same-file edits)
                    if (bufferSource && !leaveSourceStreamOpen)
                    {
                        source.Dispose();
                        source = null;
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
                    }
                    finally
                    {
                        Source = null; //Ensure reference is null
                        try
                        {
                            //Dispose buffer
                            if (UnderlyingStream != null && source != UnderlyingStream) UnderlyingStream.Dispose();
                        }
                        finally
                        {
                            UnderlyingStream = null; //Ensure reference is null
                            //Dispose source stream or restore its position
                            if (!leaveSourceStreamOpen && source != null) source.Dispose();
                            else if (originalPosition > -1 && source != null && source.CanSeek) source.Position = originalPosition;
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
        /// Gets or sets the source bitmap.
        /// </summary>
        protected Image Source { get; set; }
        /// <summary>
        /// Gets or sets the <see cref="Stream"/> underlying the source bitmap. (This stream cannot be disposed before the source bitmap.)
        /// </summary>
        protected Stream UnderlyingStream { get; set; }

        /// <summary>
        /// Gets or sets the dimensions of the source image.
        /// </summary>
        protected Size OriginalSize { get; set; }

        /// <summary>
        /// Layout: size and cropping constraints are calculated here.
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
        /// Which part of the source image to copy.
        /// </summary>
        protected RectangleF CopyRect { get; set; }
        /// <summary>
        /// Where on the target canvas to render the source image.
        /// </summary>
        protected RectangleF TargetRect { get; set; }
        /// <summary>
        /// The size to create the target image.
        /// </summary>
        protected Size DestSize { get; set; }

        /// <summary>
        /// Performs the actual bitmap resize operation.
        /// </summary>
        /// <remarks>
        /// Only rendering occurs here. See <see cref="Layout"/> for the math part of things. Neither <see cref="Dest"/> nor <see cref="Source"/> are disposed here!
        /// </remarks>
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
        /// The <see cref="Bitmap"/> object the target image is rendered to before encoding.
        /// </summary>
        protected Bitmap Dest { get; set; }

        /// <summary>
        /// Encodes <see cref="Dest"/>.
        /// </summary>
        /// <param name="target">The stream to write to.</param>
        protected virtual void Encode(Stream target)
        {
            if (Format == OutputFormat.Jpg) Encoding.SaveJpeg(Dest,target,JpegQuality);
            else if (Format == OutputFormat.Png) Encoding.SavePng(Dest,target);
        }
    }

    /// <summary>
    /// Provides adjustable JPEG encoding and 32-bit PNG encoding methods.
    /// </summary>
    public class Encoding
    {
        /// <summary>
        /// Looks up encoders by MIME type.
        /// </summary>
        /// <param name="mimeType">The MIME type to look up.</param>
        /// <returns>The <see cref="ImageCodecInfo"/> that matches the given MIME type, or <c>null</c> if no match was found.</returns>
        public static ImageCodecInfo GetImageCodecInfo(string mimeType) {
            var info = ImageCodecInfo.GetImageEncoders();
            foreach (var ici in info)
                if (ici.MimeType.Equals(mimeType, StringComparison.OrdinalIgnoreCase)) return ici;

            return null;
        }
        /// <summary>
        /// Saves the specified image to the specified stream using JPEG compression of the specified quality.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="quality">A number between 0 and 100. Defaults to 90 if passed a negative number. Numbers over 100 are truncated to 100. 90 is a *very* good setting.</param>
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
        /// Saves the image in PNG format. If the <see cref="Stream"/> denoted by <paramref name="target"/> is not seekable, a temporary <see cref="MemoryStream"/> will be used to buffer the image data into the stream.
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
    /// Provides simple layout math.
    /// </summary>
    public class BoxMath
    {

        /// <summary>
        /// Scales <paramref name="inner"/> to fit inside <paramref name="bounding"/> while maintaining aspect ratio. Upscales and downscales.
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
        /// Returns <c>true</c> if <paramref name="inner"/> fits inside or equals <paramref name="outer"/>.
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="outer"></param>
        /// <returns></returns>
        public static bool FitsInside(SizeF inner, SizeF outer)
        {
            return (inner.Width <= outer.Width && inner.Height <= outer.Height);
        }

        /// <summary>
        /// Rounds a floating-point <see cref="RectangleF"/> to an integer <see cref="Rectangle"/> using <see cref="System.Math.Round(double)"/>.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Rectangle ToRectangle(RectangleF r) {
            return new Rectangle((int)Math.Round(r.X), (int)Math.Round(r.Y), (int)Math.Round(r.Width), (int)Math.Round(r.Height));
        }
        /// <summary>
        /// Rounds a floating-point <see cref="SizeF"/> structure to an integer <see cref="Size"/> using <see cref="System.Math.Round(double)"/>.
        /// </summary>
        /// <param name="sizeF"></param>
        /// <returns></returns>
        public static Size RoundPoints(SizeF sizeF) {
            return new Size((int)Math.Round(sizeF.Width), (int)Math.Round(sizeF.Height));
        }

        /// <summary>
        /// Creates a rectangle of size <paramref name="size"/> with a center matching that of <paramref name="bounds"/>. No rounding is performed.
        /// </summary>
        /// <returns></returns>
        public static RectangleF CenterInside(SizeF size, RectangleF bounds) {
            return new RectangleF(bounds.Width / 2 + bounds.X - (size.Width / 2), bounds.Height / 2 + bounds.Y - (size.Height / 2), size.Width, size.Height);
        }
    }

    /// <summary>
    /// Provides methods for copying streams.
    /// </summary>
    public static class StreamUtils
    {

        /// <summary>
        /// Copies the <paramref name="source"/> stream into a new <see cref="MemoryStream"/> instance.
        /// </summary>
        /// <param name="source">The stream to copy.</param>
        /// <param name="entireStream"><c>true</c> to copy the entire stream if it is seekable; <c>false</c> to only copy the remaining data.</param>
        /// <param name="chunkSize">The buffer size to use (in bytes) if a buffer is required.</param>
        /// <returns></returns>
        public static MemoryStream CopyToMemoryStream(Stream source, bool entireStream, int chunkSize)
        {
            MemoryStream ms =
                new MemoryStream(source.CanSeek ? ((int) source.Length + 8 - (entireStream ? 0 : (int) source.Position)) : chunkSize);
            CopyToStream(source, ms, entireStream, chunkSize);
            ms.Position = 0;
            return ms;
        }

        /// <summary>
        /// Copies the <paramref name="source"/> stream to the <paramref name="destination"/> stream.
        /// </summary>
        /// <param name="source">The stream to read from.</param>
        /// <param name="destination">The stream to write to.</param>
        /// <param name="entireStream"><c>true</c> to copy the entire stream if it is seekable; <c>false</c> to only copy the remaining data.</param>
        /// <param name="chunkSize">The buffer size to use (in bytes) if a buffer is required.</param>
        public static void CopyToStream(Stream source, Stream destination, bool entireStream, int chunkSize)
        {
            if (entireStream && source.CanSeek) source.Seek(0, SeekOrigin.Begin);

            if (source is MemoryStream && source.CanSeek)
            {
                try
                {
                    int pos = (int) source.Position;
                    destination.Write(((MemoryStream) source).GetBuffer(), pos, (int) (source.Length - pos));
                    return;
                }
                catch (UnauthorizedAccessException) //If we can't slice it, then we read it like a normal stream
                {
                }
            }
            if (destination is MemoryStream && source.CanSeek)
            {
                try
                {
                    int srcPos = (int) source.Position;
                    int pos = (int) destination.Position;
                    int length = (int) (source.Length - srcPos) + pos;
                    destination.SetLength(length);

                    var data = ((MemoryStream) destination).GetBuffer();
                    while (pos < length)
                    {
                        pos += source.Read(data, pos, length - pos);
                    }
                    return;
                }
                catch (UnauthorizedAccessException) //If we can't write directly, fall back
                {
                }
            }
            int size = (source.CanSeek) ? Math.Min((int) (source.Length - source.Position), chunkSize) : chunkSize;
            byte[] buffer = new byte[size];
            int n;
            do
            {
                n = source.Read(buffer, 0, buffer.Length);
                destination.Write(buffer, 0, n);
            } while (n != 0);
        }
    }

    /// <summary>
    /// How to resolve aspect ratio differences between the requested size and the original image's size.
    /// </summary>
    public enum FitMode {
        /// <summary>
        /// <see cref="ResizeJob.Width"/> and <see cref="ResizeJob.Height"/> are considered maximum values. The resulting image may be smaller to maintain its aspect ratio. The image may also be smaller if the source image is smaller.
        /// </summary>
        Max,
        /// <summary>
        /// <see cref="ResizeJob.Width"/> and <see cref="ResizeJob.Height"/> are considered exact values. Padding is used if there is an aspect ratio difference.
        /// </summary>
        Pad,
        /// <summary>
        /// <see cref="ResizeJob.Width"/> and <see cref="ResizeJob.Height"/> are considered exact values. Cropping is used if there is an aspect ratio difference.
        /// </summary>
        Crop,
        /// <summary>
        /// <see cref="ResizeJob.Width"/> and <see cref="ResizeJob.Height"/> are considered exact values. If there is an aspect ratio difference, the image is stretched.
        /// </summary>
        Stretch,
    }

    /// <summary>
    /// Controls whether the image is allowed to upscale, downscale, both, or if only the canvas gets to be upscaled.
    /// </summary>
    public enum ScaleMode {
        /// <summary>
        /// Only downsample images, never enlarge. If an image is smaller than <see cref="ResizeJob.Width"/> and <see cref="ResizeJob.Height"/>, the image coordinates are used instead.
        /// </summary>
        Down,
        /// <summary>
        /// Upscales and downscales images according to <see cref="ResizeJob.Width"/> and <see cref="ResizeJob.Height"/>.
        /// </summary>
        Both,
        /// <summary>
        /// When the image is smaller than the requested size, padding is added instead of stretching the image.
        /// </summary>
        Canvas
    }
    /// <summary>
    /// Specifies the encoding format. Auto-detection is not enabled.
    /// </summary>
    public enum OutputFormat {
        /// <summary>
        /// The JPEG file format.
        /// </summary>
        Jpg,
        /// <summary>
        /// The PNG file format.
        /// </summary>
        Png
    }
}
