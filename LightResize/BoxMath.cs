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
using System.ComponentModel;
using System.Drawing;

namespace LightResize
{
    /// <summary>
    /// Provides simple layout math.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public class BoxMath
    {
        /// <summary>
        /// Scales <paramref name="inner"/> to fit inside <paramref name="bounding"/> while maintaining aspect ratio. Upscales and downscales.
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="bounding"></param>
        /// <returns></returns>
        public static SizeF ScaleInside(SizeF inner, SizeF bounding)
        {
            double innerRatio = inner.Width / inner.Height;
            double outerRatio = bounding.Width / bounding.Height;

            if (outerRatio > innerRatio)
            {
                //Width is wider - so bound by height.
                return new SizeF((float)(innerRatio * bounding.Height), (float)(bounding.Height));
            }
            else
            {
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
        public static Rectangle ToRectangle(RectangleF r)
        {
            return new Rectangle((int)Math.Round(r.X), (int)Math.Round(r.Y), (int)Math.Round(r.Width), (int)Math.Round(r.Height));
        }

        /// <summary>
        /// Rounds a floating-point <see cref="SizeF"/> structure to an integer <see cref="Size"/> using <see cref="System.Math.Round(double)"/>.
        /// </summary>
        /// <param name="sizeF"></param>
        /// <returns></returns>
        public static Size RoundPoints(SizeF sizeF)
        {
            return new Size((int)Math.Round(sizeF.Width), (int)Math.Round(sizeF.Height));
        }

        /// <summary>
        /// Creates a rectangle of size <paramref name="size"/> with a center matching that of <paramref name="bounds"/>. No rounding is performed.
        /// </summary>
        /// <returns></returns>
        public static RectangleF CenterInside(SizeF size, RectangleF bounds)
        {
            return new RectangleF(bounds.Width / 2 + bounds.X - (size.Width / 2), bounds.Height / 2 + bounds.Y - (size.Height / 2), size.Width, size.Height);
        }
    }
}
