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

namespace LightResize
{
    /// <summary>
    /// How to resolve aspect ratio differences between the requested size and the original image's size.
    /// </summary>
    public enum FitMode
    {
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
}
