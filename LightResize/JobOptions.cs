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
    /// Instructs <see cref="ImageBuilder"/> how to handle I/O (e.g. whether to buffer, dispose, and/or rewind the source stream, or whether to dispose the destination stream).
    /// </summary>
    [Flags]
    public enum JobOptions
    {
        /// <summary>
        /// Instructs <see cref="ImageBuilder"/> to leave the source stream open even after it is no longer needed for the job.
        /// </summary>
        LeaveSourceStreamOpen = 1,

        /// <summary>
        /// Instructs <see cref="ImageBuilder"/> to rewind the source stream to its original position after it has been used. (This is useful when reusing a stream or <c>HttpFileUpload</c>.)
        /// Implies <see cref="LeaveSourceStreamOpen"/>.
        /// </summary>
        RewindSourceStream = 2,

        /// <summary>
        /// Instructs <see cref="ImageBuilder"/> to leave the target stream open after it is finished writing. Make sure you close it externally!
        /// </summary>
        LeaveTargetStreamOpen = 4,

        /// <summary>
        /// Instructs <see cref="ImageBuilder"/> to preserve the target bitmap. (This will cause a memory leak unless disposed externally).
        /// </summary>
        PreserveTargetBitmap = 8,

        /// <summary>
        /// Instructs <see cref="ImageBuilder"/> to create any needed parent folder levels when a file path is specified as the destination.
        /// </summary>
        CreateParentDirectory = 16,

        /// <summary>
        /// Instructs <see cref="ImageBuilder"/> to copy the source stream into a memory buffer so that it can be closed earlier. (This is required if you are writing to the same file that you are reading from.)
        /// </summary>
        BufferEntireSourceStream = 32,
    }
}
