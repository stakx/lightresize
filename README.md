Imazen LightResize
===========

Embeddable image resizing class for console, WPF, &amp; WinForms usage. 

## Goal 1: Make the shortest possible reusable implementation of image resizing that:

1. Does not introduce visual artifacts, sacrifice quality, truncate pixels, or make rounding errors.
2. Does not leak or waste any memory, and does not perform wasteful operations.
3. Can stream from filename->filename, stream->stream, stream->filename, filename->stream, and file->same file
4. Encodes optimally to jpeg and reliably to PNG.
5. Offers the 4 basic constraint modes: max, pad, crop, and stretch (Only adds 30 lines of code)

## What I had to cut:

1. All .GIF support, animated and otherwise. No frame selection or TIFF page selection either.
2. 8-bit PNG support
3. Cropping, rotation, flipping, zoom, dpi, and alignment control.
4. ASP.NET support. Virtual paths, VirtualPathProviders, HttpPostedFile, and automatic format inference are gone. 
5. No UrlAuthorization support, no file security, no cache headers, no mime-type detection, no size limits.
5. All friendly error messages. Back to the uninformative ArgumentException and ExternalException errors.
6. No disk caching (A safe implementation of this requires 5-10KLOC).
7. No file extension or mime-type intelligence.
8. Margins, padding, and border support.
9. Safe Template paths (i.e., `~/images/<guid>.<ext>`)
10. Direct Bitmap/Image access
11. Extensibility. No plugins, no events, no flexible command interface.
12. No self-diagnostics or configuration. 



## Goal 2: Make a single-purpose implementation of image resizing that:

1. Does not introduce visual artifacts, sacrifice quality, truncate pixels, or make rounding errors.
2. Does not leak memory, but does sacrifice up to 50% performance for simplicity.
3. Can only go from stream A to a different stram B, disposing both when done.
4. Encodes only in Jpeg form.
5. Only offers maximum constraint.


