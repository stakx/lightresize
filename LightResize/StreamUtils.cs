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
using System.IO;

namespace LightResize
{
    /// <summary>
    /// Provides methods for copying streams.
    /// </summary>
    internal static class StreamUtils
    {
        /// <summary>
        /// Copies the <paramref name="source"/> stream into a new <see cref="MemoryStream"/> instance.
        /// </summary>
        /// <param name="source">The stream to copy.</param>
        /// <param name="entireStream"><c>true</c> to copy the entire stream if it is seekable; <c>false</c> to only copy the remaining data.</param>
        /// <param name="chunkSize">The buffer size to use (in bytes) if a buffer is required.</param>
        /// <returns>A <see cref="MemoryStream"/> containing data from the original stream.</returns>
        public static MemoryStream CopyToMemoryStream(Stream source, bool entireStream, int chunkSize)
        {
            MemoryStream ms =
                new MemoryStream(source.CanSeek ? ((int)source.Length + 8 - (entireStream ? 0 : (int)source.Position)) : chunkSize);
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
            if (entireStream && source.CanSeek)
            {
                source.Seek(0, SeekOrigin.Begin);
            }

            if (source is MemoryStream && source.CanSeek)
            {
                try
                {
                    int pos = (int)source.Position;
                    destination.Write(((MemoryStream)source).GetBuffer(), pos, (int)(source.Length - pos));
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    // If we can't slice it, then we read it like a normal stream
                }
            }

            if (destination is MemoryStream && source.CanSeek)
            {
                try
                {
                    int srcPos = (int)source.Position;
                    int pos = (int)destination.Position;
                    int length = (int)(source.Length - srcPos) + pos;
                    destination.SetLength(length);

                    var data = ((MemoryStream)destination).GetBuffer();
                    while (pos < length)
                    {
                        pos += source.Read(data, pos, length - pos);
                    }

                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    // If we can't write directly, fall back
                }
            }

            int size = source.CanSeek ? Math.Min((int)(source.Length - source.Position), chunkSize) : chunkSize;
            byte[] buffer = new byte[size];
            int n;
            do
            {
                n = source.Read(buffer, 0, buffer.Length);
                destination.Write(buffer, 0, n);
            }
            while (n != 0);
        }
    }
}
