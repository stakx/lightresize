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

namespace LightResize
{
    /// <summary>
    /// Instructs <see cref="ImageBuilder"/> how to handle a stream passed to it.
    /// </summary>
    [Flags]
    public enum StreamOptions
    {
        /// <summary>
        /// The stream will be closed after it has been used. This is the default.
        /// </summary>
        Close = 0b000,

        /// <summary>
        /// The stream will be buffered entirely in memory. Applies only for source (input) streams.
        /// </summary>
        BufferInMemory = 0b001,

        /// <summary>
        /// The stream will be left open after it has been used.
        /// </summary>
        LeaveOpen = 0b010,

        /// <summary>
        /// The stream will be rewound to the initial position. This implies <see cref="LeaveOpen"/>.
        /// </summary>
        Rewind = LeaveOpen | 0b100,
    }
}
