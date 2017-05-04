Imazen LightResize
===========

For all the whiners on StackOverflow - Here's an embeddable image resizing class for non-ASP.NET apps.

It's extracted from [ImageResizer](http://imageresizing.net). All ASP.NET support (and dependencies) were removed. 

Consider this the 'how-to' counterpart of [29 Image Resizing Pitfalls](http://nathanaeljones.com/163/20-image-resizing-pitfalls/).

## Goal 1: Make the shortest possible *reusable* implementation of image resizing that:

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
5. Locked file management.
5. UrlAuthorization support, file security, cache headers, mime-type detection, size limits.
5. Friendly error messages. Back to the uninformative GDI+ ArgumentException and ExternalException errors.
6. Disk caching (A safe implementation of this requires 5-10KLOC).
7. File extension and mime-type intelligence.
8. Margins, padding, and border support.
9. Safe Template paths (i.e., `~/images/<guid>.<ext>`)
10. Direct Bitmap/Image access
11. Extensibility. No plugins, no events, no flexible command interface.
12. Self-diagnostics and configuration. 

### [LightResize.cs](https://github.com/imazen/lightresize/blob/master/Imazen.LightResize/LightResize.cs)

The result is < 700 LOC, which is ideal for limited needs embedded usage scenarios like command-line or WinForms apps. 

It's definitely a poor choice for ASP.NET usage, though - you're better off using the library designed for that: [ImageResizer](http://imageresizing.net/)

## Goal 2: Make a single-purpose implementation of image resizing that:

1. Does not introduce visual artifacts, sacrifice quality, truncate pixels, or make rounding errors.
2. Does not leak memory, but does sacrifice up to 50% performance for simplicity.
3. Can only go from stream A to a different stram B, disposing both when done.
4. Encodes only in Jpeg form.
5. Only offers maximum constraint.

### [SinglePurposeResize.cs](https://github.com/imazen/lightresize/blob/master/Imazen.LightResize/SinglePurposeResize.cs)
