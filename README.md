Imazen LightResize
===========

Embeddable image resizing class for console, WPF, &amp; WinForms usage. 

## Goal: Make the shortest possible implementation of image resizing that:

1. Does not introduce visual artifacts, sacrifice quality, truncate pixels, or make rounding errors.
2. Does not leak or waste any memory
3. Can stream from filename->filename, stream->stream, stream->filename, filename->stream, and filename->same filename
4. Encodes optimally to jpeg and reliably to PNG.
5. Offers the 4 basic constraint modes: max, pad, crop, and stretch (Only adds 30 lines of code)

## What I had to cut:

1. ASP.NET virtual file and virtual path support.
2. Permissions/security enforcement
3. Any kind of disk-based caching (A proper implementation is 5-10KLOC)
4. Mime-type or extension smarts.
2. Direct Bitmap/Image access
3. Extensibility & querystring interface
4. Friendly error messages (you'll have to decrypt those GDI+ messages on your own)
5. HttpPostedFile support
4. Template paths (i.e., `~/images/<guid>.<ext>`)
5. Page or frame selection
2. Pre & Post rotation & flipping
3. Padding, margin, border, and drop-shadow support
4. DPI setting
5. Zoom factor 
6. Manual cropping
7. Cropping and padding alignent control (middlecenter)
8. GIF support
9. Automatic format inference



