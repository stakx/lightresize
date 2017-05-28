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

namespace LightResize
{
    /// <summary>
    /// Specifies how an image should be resized.
    /// </summary>
    public sealed class Instructions
    {
        private OutputFormat format = OutputFormat.Jpeg;
        private int? height;
        private int jpegQuality = 90;
        private FitMode mode = FitMode.Max;
        private ScaleMode scale = ScaleMode.DownscaleOnly;
        private int? width;

        /// <summary>
        /// Gets or sets the width constraint.
        /// </summary>
        public int? Width
        {
            get => width;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), nameof(Width) + " must be null or a positive number.");
                }

                width = value;
            }
        }

        /// <summary>
        /// Gets or sets the height constraint.
        /// </summary>
        public int? Height
        {
            get => height;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), nameof(Height) + " must be null or a positive number.");
                }

                height = value;
            }
        }

        /// <summary>
        /// Gets or sets the constraint mode. Defaults to <see cref="FitMode.Max"/>.
        /// </summary>
        public FitMode Mode
        {
            get => mode;
            set
            {
                if (value < FitMode.Max || value > FitMode.Stretch)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                mode = value;
            }
        }

        /// <summary>
        /// Gets or sets whether upscaling is permitted. Defaults to <see cref="ScaleMode.DownscaleOnly"/>.
        /// </summary>
        public ScaleMode Scale
        {
            get => scale;
            set
            {
                if (value < ScaleMode.DownscaleOnly || value > ScaleMode.UpscaleCanvas)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                scale = value;
            }
        }

        /// <summary>
        /// Gets or sets the encoding format to use when writing the resized image to the destination stream. Defaults to <see cref="OutputFormat.Jpeg"/>.
        /// </summary>
        public OutputFormat Format
        {
            get => format;
            set
            {
                if (value < OutputFormat.Jpeg || value > OutputFormat.Png)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                format = value;
            }
        }

        /// <summary>
        /// Gets or sets the JPEG encoding quality to use. Must be a value between 0 and 100 (inclusive). 90 is the best value and the default. Seriously.
        /// </summary>
        public int JpegQuality
        {
            get => jpegQuality;
            set
            {
                if (value < 0 || value > 100)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                jpegQuality = value;
            }
        }

        /// <summary>
        /// Gets or sets the background color to apply. Defaults to <see cref="Color.Transparent"/>. If the output format (<see cref="Format"/>) is <see cref="OutputFormat.Jpeg"/> (which does not support transparency), <see cref="Color.White"/> will be used instead of <see cref="Color.Transparent"/>.
        /// </summary>
        public Color BackgroundColor { get; set; } = Color.Transparent;

        /// <summary>
        /// Gets or sets a value indicating whether the ICC profile will be ignored instead of being applied. Defaults to <c>false</c>.
        /// </summary>
        public bool IgnoreICC { get; set; } = false;
    }
}
