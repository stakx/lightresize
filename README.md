# LightResize #

An embeddable image resizing library for non-ASP.NET apps, extracted from [ImageResizer](http://imageresizing.net). All ASP.NET support (and dependencies) were removed.

[![NuGet](https://img.shields.io/nuget/v/LightResize.svg)](https://www.nuget.org/packages/LightResize/)

## What's new? ##

**28 May 2017**: LightResize is now [available as a NuGet package](https://www.nuget.org/packages/LightResize/).

**8 May 2017**: Wondering why you're at [stakx/lightresize](https://github.com/stakx/lightresize) when you followed a link to [Imazen/lightresize](https://github.com/Imazen/lightresize)? Don't worry, this is not by mistake: [@nathanaeljones](https://github.com/nathanaeljones), the original author of LightResize, has handed maintenance of his project over to me, so it's got a new home and GitHub automatically brought you here. My aim is to keep this small library useful and easily available to those who want to use it.

## Basic usage ##

First, add a reference to LightResize to your project (`Install-Package LightResize`) and import LightResize's namespace:

```csharp
using LightResize;
```

Perform a resizing operation by calling any of the static `ImageBuilder.Build` method overloads. For example:

```csharp
ImageBuilder.Build(@"path\to\source.jpg", @"path\to\resized.jpg", new Instructions { Height = 150 });
```

The `Instructions` you pass to `Build` define the resizing parameters. You can set any of several properties (check the IntelliSense documentation or source code to learn what's available), but at the very least you probably want to set a `Width` and/or `Height` constraint.

Instead of file paths, you can also pass `Stream` instances (either for the source bitmap, the destination, or both):

```csharp
using (var destination = new MemoryStream())
{
    ImageBuilder.Build(@"path\to\source.png", destination, leaveDestinationOpen: true, instructions: …);
    …
})
```

The above example also shows that you can specify additional options for how LightResize should handle the streams passed to it. Check the IntelliSense documentation to find out about all available options and overloads of the static `Build` method.

## Origins of this project ##

LightResize started as a demonstration of how to do safe image resizing; it could be considered the 'how-to' counterpart of [29 Image Resizing Pitfalls](http://nathanaeljones.com/163/20-image-resizing-pitfalls/).

Before LightResize was made available as a NuGet package, it consisted of two separate code files. Each of these could be directly embedded into your code, but they both had a different API and aimed for somewhat different goals:

### Goal 1: Make the shortest possible *reusable* implementation of image resizing that: ###

1. Does not introduce visual artifacts, sacrifice quality, truncate pixels, or make rounding errors.
2. Does not leak or waste any memory, and does not perform wasteful operations.
3. Can stream from filename->filename, stream->stream, stream->filename, filename->stream, and file->same file
4. Encodes optimally to jpeg and reliably to PNG.
5. Offers the 4 basic constraint modes: max, pad, crop, and stretch (Only adds 30 lines of code)

#### What had to be cut: ####

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

#### [LightResize.cs](https://github.com/stakx/lightresize/blob/dcee788b76fe5f3c8965f04066fd874a718723c9/LightResize.cs) ####

The result is < 700 LOC, which is ideal for limited needs embedded usage scenarios like command-line or WinForms apps.

It's definitely a poor choice for ASP.NET usage, though - you're better off using the library designed for that: [ImageResizer](http://imageresizing.net/)

### Goal 2: Make a single-purpose implementation of image resizing that: ###

1. Does not introduce visual artifacts, sacrifice quality, truncate pixels, or make rounding errors.
2. Does not leak memory, but does sacrifice up to 50% performance for simplicity.
3. Can only go from stream A to a different stram B, disposing both when done.
4. Encodes only in Jpeg form.
5. Only offers maximum constraint.

#### [SinglePurposeResize.cs](https://github.com/stakx/lightresize/blob/dcee788b76fe5f3c8965f04066fd874a718723c9/SinglePurposeResize.cs) ####
