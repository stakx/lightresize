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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace LightResize
{
    /// <summary>
    /// Contains methods to resize images. Very limited compared to ImageResizer, absolutely no ASP.NET support.
    /// </summary>
    public static class ResizeJob
    {
        /// <summary>
        /// Allows callers to handle the encoding/usage phase.
        /// </summary>
        /// <param name="bitmap">The resized bitmap image.</param>
        /// <param name="options">Specifies how <see cref="ResizeJob"/> should handle I/O (e.g. whether to buffer, rewind, and/or dispose the source stream, and whether to dispose the target stream).</param>
        protected delegate void BitmapConsumer(Bitmap bitmap, JobOptions options);

        /// <summary>
        /// Performs the image resize operation by reading from a file and writing to a file.
        /// </summary>
        /// <param name="sourcePath">The path of the file to read from.</param>
        /// <param name="destinationPath">The path of the file to write to.</param>
        /// <param name="options">Specifies how <see cref="ResizeJob"/> should handle I/O (e.g. whether to buffer, rewind, and/or dispose the source stream, and whether to dispose the target stream).</param>
        /// <param name="instructions">Specifies how to resize the source image.</param>
        public static void Build(string sourcePath, string destinationPath, JobOptions options, Instructions instructions)
        {
            if (sourcePath == destinationPath)
            {
                options = options | JobOptions.BufferEntireSourceStream;
            }

            Build(File.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read), destinationPath, options, instructions);
        }

        /// <summary>
        /// Performs the image resize operation by reading from a file and writing to a <see cref="Stream"/>.
        /// </summary>
        /// <param name="sourcePath">The path of the file to read from.</param>
        /// <param name="destination">The stream to write to.</param>
        /// <param name="options">Specifies how <see cref="ResizeJob"/> should handle I/O (e.g. whether to buffer, rewind, and/or dispose the source stream, and whether to dispose the target stream).</param>
        /// <param name="instructions">Specifies how to resize the source image.</param>
        public static void Build(string sourcePath, Stream destination, JobOptions options, Instructions instructions)
        {
            Build(File.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read), destination, options, instructions);
        }

        /// <summary>
        /// Performs the image resize operation by reading from a <see cref="Stream"/> and writing to a file.
        /// </summary>
        /// <param name="source">The stream to read from.</param>
        /// <param name="destinationPath">The path of the file to write to.</param>
        /// <param name="options">Specifies how <see cref="ResizeJob"/> should handle I/O (e.g. whether to buffer, rewind, and/or dispose the source stream, and whether to dispose the target stream).</param>
        /// <param name="instructions">Specifies how to resize the source image.</param>
        public static void Build(Stream source, string destinationPath, JobOptions options, Instructions instructions)
        {
            var createParents = (options & JobOptions.CreateParentDirectory) != 0;
            if (createParents)
            {
                string dirName = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }
            }

            Build(
                source,
                (Bitmap b, JobOptions option) =>
                {
                    using (var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                    {
                        Encode(b, fs, instructions);
                    }
                },
                options,
                instructions);
        }

        /// <summary>
        /// Performs the image resize operation by reading from a <see cref="Stream"/> and writing to a <see cref="Stream"/>.
        /// </summary>
        /// <param name="source">The stream to read from.</param>
        /// <param name="destination">The stream to write to.</param>
        /// <param name="options">Specifies how <see cref="ResizeJob"/> should handle I/O (e.g. whether to buffer, rewind, and/or dispose the source stream, and whether to dispose the target stream).</param>
        /// <param name="instructions">Specifies how to resize the source image.</param>
        /// <remarks>
        /// Ensure that the first stream you open will be safely closed if the second stream fails to open! This means a <c>using()</c> or <c>try</c>/<c>finally</c> clause.
        /// </remarks>
        public static void Build(Stream source, Stream destination, JobOptions options, Instructions instructions)
        {
            Build(
                source,
                (Bitmap b, JobOptions opts) =>
                {
                    try
                    {
                        // Encode from temp bitmap to target stream
                        Encode(b, destination, instructions);
                    }
                    finally
                    {
                        // Ensure target stream is disposed if requested
                        if ((opts & JobOptions.LeaveTargetStreamOpen) == 0)
                        {
                            destination.Dispose();
                        }
                    }
                },
                options,
                instructions);
        }

        /// <summary>
        /// Loads the bitmap from stream, processes, and renders, sending the result <see cref="Bitmap"/> to the <paramref name="consumer"/> callback for encoding or usage.
        /// </summary>
        /// <param name="source">The <see cref="Stream"/> to read from.</param>
        /// <param name="consumer">The <see cref="BitmapConsumer"/> that will receive the resized <see cref="Bitmap"/> for further processing (e.g. writing to a destination).</param>
        /// <param name="options">Specifies how <see cref="ResizeJob"/> should handle I/O (e.g. whether to buffer, rewind, and/or dispose the source stream, and whether to dispose the target stream).</param>
        /// <param name="instructions">Specifies how to resize the source image.</param>
        private static void Build(Stream source, BitmapConsumer consumer, JobOptions options, Instructions instructions)
        {
            var leaveSourceStreamOpen = (options & JobOptions.LeaveSourceStreamOpen) != 0 ||
                                 (options & JobOptions.RewindSourceStream) != 0;

            var bufferSource = (options & JobOptions.BufferEntireSourceStream) != 0;
            var originalPosition = (options & JobOptions.RewindSourceStream) != 0 ? source.Position : -1;
            var preserveTemp = (options & JobOptions.PreserveTargetBitmap) != 0;

            Bitmap destBitmap = null;
            try
            {
                Stream underlyingStream = null;
                Bitmap sourceBitmap = null;
                try
                {
                    // Buffer source stream if requested
                    underlyingStream = bufferSource ? StreamUtils.CopyToMemoryStream(source, true, 0x1000) : source;

                    // Allow early disposal (enables same-file edits)
                    if (bufferSource && !leaveSourceStreamOpen)
                    {
                        source.Dispose();
                        source = null;
                    }

                    // Load bitmap
                    sourceBitmap = new Bitmap(underlyingStream, !instructions.IgnoreICC);

                    // Use size
                    var originalSize = sourceBitmap.Size;

                    // Do math
                    Layout(originalSize, instructions, out RectangleF copyRect, out Size destSize, out RectangleF targetRect);

                    // Render to 'destBitmap'
                    Render(sourceBitmap, copyRect, destSize, targetRect, instructions, out destBitmap);
                }
                finally
                {
                    try
                    {
                        // Dispose loaded bitmap instance
                        if (sourceBitmap != null)
                        {
                            sourceBitmap.Dispose();
                        }
                    }
                    finally
                    {
                        sourceBitmap = null; // Ensure reference is null
                        try
                        {
                            // Dispose buffer
                            if (underlyingStream != null && source != underlyingStream)
                            {
                                underlyingStream.Dispose();
                            }
                        }
                        finally
                        {
                            underlyingStream = null; // Ensure reference is null

                            // Dispose source stream or restore its position
                            if (!leaveSourceStreamOpen && source != null)
                            {
                                source.Dispose();
                            }
                            else if (originalPosition > -1 && source != null && source.CanSeek)
                            {
                                source.Position = originalPosition;
                            }
                        }
                    }
                }

                // Fire callback to write to disk or use Bitmap instance directly
                consumer(destBitmap, options);
            }
            finally
            {
                // Temporary bitmap must be disposed
                if (!preserveTemp && destBitmap != null)
                {
                    destBitmap.Dispose();
                    destBitmap = null;
                }
            }
        }

        // Layout: size and cropping constraints are calculated here.
        private static void Layout(Size originalSize, Instructions instructions, out RectangleF copyRect, out Size destSize, out RectangleF targetRect)
        {
            // Aspect ratio of the source image
            var imageRatio = (double)originalSize.Width / (double)originalSize.Height;

            // Target image size
            SizeF targetSize;

            // Target canvas size
            SizeF canvasSize;

            var originalRect = new RectangleF(0, 0, originalSize.Width, originalSize.Height);

            copyRect = originalRect;

            /* Normalize */

            var width = instructions.Width;
            if (width.HasValue && width < 1)
            {
                width = null;
            }

            var height = instructions.Height;
            if (height.HasValue && height < 1)
            {
                height = null;
            }

            if (width.HasValue || height.HasValue)
            {
                // Establish constraint bounds
                SizeF bounds = width.HasValue && height.HasValue
                                   ? new SizeF((float)width, (float)height)
                                   : (width.HasValue
                                         ? new SizeF((float)width, (float)((double)width / imageRatio))
                                         : (height.HasValue
                                               ? new SizeF((float)((double)height * imageRatio), (float)height)
                                               : SizeF.Empty));

                /* We now have width & height, our target size. It will only be a different aspect ratio from the image if both 'width' and 'height' are specified. */

                var mode = instructions.Mode;
                if (mode == FitMode.Max)
                {
                    // FitMode.Max
                    canvasSize = targetSize = BoxMath.ScaleInside(copyRect.Size, bounds);
                }
                else if (mode == FitMode.Pad)
                {
                    // FitMode.Pad
                    canvasSize = bounds;
                    targetSize = BoxMath.ScaleInside(copyRect.Size, canvasSize);
                }
                else if (mode == FitMode.Crop)
                {
                    // FitMode.crop
                    // We auto-crop - so both target and area match the requested size
                    canvasSize = targetSize = bounds;

                    // Determine the size of the area we are copying
                    var sourceSize = BoxMath.RoundPoints(BoxMath.ScaleInside(canvasSize, copyRect.Size));

                    // Center the portion we are copying within the manualCropSize
                    copyRect = BoxMath.ToRectangle(BoxMath.CenterInside(sourceSize, copyRect));
                }
                else
                {
                    // Stretch and carve both act like stretching, so do that:
                    canvasSize = targetSize = bounds;
                }
            }
            else
            {
                // No dimensions specified, no fit mode needed. Use original dimensions
                canvasSize = targetSize = originalSize;
            }

            // Now, unless upscaling is enabled, ensure the image is no larger than it was originally
            var scale = instructions.Scale;
            if (scale != ScaleMode.Both && BoxMath.FitsInside(originalSize, targetSize))
            {
                targetSize = originalSize;
                copyRect = originalRect;

                // And reset the canvas size, unless canvas upscaling is enabled.
                if (scale != ScaleMode.UpscaleCanvas)
                {
                    canvasSize = targetSize;
                }
            }

            // May 12: require max dimension and round values to minimize rounding differences later.
            canvasSize.Width = Math.Max(1, (float)Math.Round(canvasSize.Width));
            canvasSize.Height = Math.Max(1, (float)Math.Round(canvasSize.Height));
            targetSize.Width = Math.Max(1, (float)Math.Round(targetSize.Width));
            targetSize.Height = Math.Max(1, (float)Math.Round(targetSize.Height));

            destSize = new Size((int)canvasSize.Width, (int)canvasSize.Height);
            targetRect = BoxMath.CenterInside(targetSize, new RectangleF(0, 0, canvasSize.Width, canvasSize.Height));
        }

        // Performs the actual bitmap resize operation.
        // Only rendering occurs here. See 'Layout' for the math part of things. Neither 'dest' nor 'source' are disposed here!
        private static void Render(Bitmap source, RectangleF copyRect, Size destSize, RectangleF targetRect, Instructions instructions, out Bitmap dest)
        {
            // Create new bitmap using calculated size.
            dest = new Bitmap(destSize.Width, destSize.Height, PixelFormat.Format32bppArgb);

            // Create graphics handle
            using (var g = Graphics.FromImage(dest))
            {
                // HQ bi-cubic is 2 pass. It's only 30% slower than low quality, why not have HQ results?
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // Ensures the edges are crisp
                g.SmoothingMode = SmoothingMode.HighQuality;

                // Prevents artifacts at the edges
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                // Ensures matted PNGs look decent
                g.CompositingQuality = CompositingQuality.HighQuality;

                // Prevents really ugly transparency issues
                g.CompositingMode = CompositingMode.SourceOver;

                // If the image doesn't support transparency, we need to fill the background color now.
                var background = instructions.BackgroundColor;

                // Find out if we can safely know that nothing will be showing through or around the image.
                var nothingToShow = (source.PixelFormat == PixelFormat.Format24bppRgb ||
                                       source.PixelFormat == PixelFormat.Format32bppRgb ||
                                       source.PixelFormat == PixelFormat.Format48bppRgb) &&
                                      targetRect.Width == destSize.Width && targetRect.Height == destSize.Height
                                      && targetRect.X == 0 && targetRect.Y == 0;

                // Set the background to white if the background will be showing and the destination format doesn't support transparency.
                if (background == Color.Transparent && instructions.Format == OutputFormat.Jpeg & !nothingToShow)
                {
                    background = Color.White;
                }

                // Fill background
                if (background != Color.Transparent)
                {
                    g.Clear(background);
                }

                using (var ia = new ImageAttributes())
                {
                    // Fixes the 50% gray border issue on bright white or dark images
                    ia.SetWrapMode(WrapMode.TileFlipXY);

                    // Make poly from rectF
                    var r = new PointF[3];
                    r[0] = targetRect.Location;
                    r[1] = new PointF(targetRect.Right, targetRect.Top);
                    r[2] = new PointF(targetRect.Left, targetRect.Bottom);

                    // Render!
                    g.DrawImage(source, r, copyRect, GraphicsUnit.Pixel, ia);
                }

                g.Flush(FlushIntention.Flush);
            }
        }

        // Encodes 'dest'.
        private static void Encode(Bitmap dest, Stream target, Instructions instructions)
        {
            var format = instructions.Format;
            if (format == OutputFormat.Jpeg)
            {
                Encoding.SaveJpeg(dest, target, instructions.JpegQuality);
            }
            else if (format == OutputFormat.Png)
            {
                Encoding.SavePng(dest, target);
            }
        }
    }
}
