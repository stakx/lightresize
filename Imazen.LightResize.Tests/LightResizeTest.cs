using System;
using System.Collections.Generic;
using System.Text;
using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Core;
using MbUnit.Framework.ContractVerifiers;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Net;
using Imazen.LightResize;
using System.Web;
using System.Collections.Specialized;

namespace Imazen.LightTest {
    [TestFixture]
    public class LightResizeTest {

        public LightResizeTest() {

        }


        private Bitmap GetBitmap(int width, int height) {
            Bitmap b = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(b)) {
                g.DrawString("Hello!", new Font(FontFamily.GenericSansSerif, 1), new SolidBrush(Color.Beige), new PointF(0, 0));
                g.Flush();
            }
            return b;
        }

        private Stream GetBitmapStream(int width, int height) {
            MemoryStream ms = new MemoryStream(4096);
            using (var b = GetBitmap(width, height))
                b.Save(ms, ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
        /// <summary>
        /// Allows an ResizeJob to be configured from a querystring-style string. 
        /// Supports width, height, quality, scale, mode, format, ignoreicc, but not matte.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public ResizeJob Job(string str) {
            
            var nvc = HttpUtility.ParseQueryString(str);

            var j = new ResizeJob();
            j.Width = ParseInt(nvc, "width", j.Width);
            j.Height = ParseInt(nvc, "height", j.Height);
            j.JpegQuality = ParseInt(nvc, "quality", j.JpegQuality).Value;
            if ("true".Equals(nvc["ignoreicc"], StringComparison.OrdinalIgnoreCase)) j.IgnoreIccProfile = true;

            j.ScalingRules = ParseEnum<ScaleMode>(nvc, "scale", j.ScalingRules).Value;
            j.Mode = ParseEnum<FitMode>(nvc, "mode", j.Mode).Value;
            j.Format = ParseEnum<OutputFormat>(nvc, "format", j.Format).Value;
            //Didn't parse color, too hard
            
            return j;
        }

        private T? ParseEnum<T>(NameValueCollection nvc, string key, T? def) where T : struct, IConvertible {
            string s = nvc[key];
            if (!string.IsNullOrEmpty(s)){
                T temp;
                if (Enum.TryParse<T>(s,out temp)) return temp;

            }
            return def;
        }

        private int? ParseInt(NameValueCollection nvc, string key, int? def) {
            string s = nvc[key];
            if (!string.IsNullOrEmpty(s)){
                int temp;
                if (int.TryParse(s,out temp)) return temp;
            }
            return def;
        }



        [Test]
        [Row(50,50,"format=jpg&quality=100")]
        [Row(1,1,"format=jpg&quality=100")]
        [Row(50,50,"format=jpg&quality=-300")]
        [Row(50,50,"format=png")]
        public void EncodeImage(int width, int height, string query){
            using (MemoryStream ms = new MemoryStream(8000)){
                Job(query).Build(GetBitmapStream(width, height), ms, JobOptions.LeaveTargetStreamOpen);
            }
        }




        //[Test]
        //[Row(200,200,50,50,"?width=50&height=50")]
        //[Row(10, 10, 50, 50, "?paddingWidth=10&borderWidth=10&borderColor=green")]
        //[Row(10, 10, 50, 50, "?paddingWidth=10&borderWidth=10")]
        //[Row(10, 10, 70, 70, "?paddingWidth=10&margin=10&borderWidth=10&borderColor=green")]
        //[Row(10, 10, 70, 70, "?paddingWidth=10&margin=10&borderWidth=10")]
        //public void TestBitmapSize(int originalWidth, int originalHeight, int expectedWidth, int expectedHeight, string query) {
        //    ResizerSection config = new ResizerSection();
        //    Config c = new Config(config);
        //    using (Bitmap b = c.CurrentImageBuilder.Build(GetBitmap(originalWidth,originalHeight), new ResizeSettings(query))) {
        //        Assert.AreEqual<Size>(new Size(expectedWidth,expectedHeight),b.Size);
        //    }
        //}
        ///// <summary>
        ///// Verifies GetFinalSize() and Build() always agree
        ///// </summary>
        ///// <param name="c"></param>
        ///// <param name="original"></param>
        ///// <param name="query"></param>
        //[Test]
        //[CombinatorialJoin]
        //[Row(100,1, "?width=20")]
        //[Row(20,100, "?height=30")]
        //public void GetFinalSize(int originalWidth,int originalHeight, string query){
        //    using (Bitmap b = c.CurrentImageBuilder.Build(GetBitmap(originalWidth,originalHeight), new ResizeSettings(query))) {
        //        Assert.AreEqual<Size>(b.Size,c.CurrentImageBuilder.GetFinalSize(new Size(originalWidth,originalHeight),new ResizeSettings(query)));
        //    }
        //}

        //[Test]
        //[Row(200,200,100,100,100,100,"rotate=90")]
        //[Row(200,200,100,100,50,50, "width=100")]
        //[Row(200,200,100,100,50,10, "width=100&height=20&stretch=fill")]
        //public void TranslatePoints(int imgWidth, int imgHeight, float x, float y, float expectedX, float expectedY, string query) {
        //    PointF result = c.CurrentImageBuilder.TranslatePoints(new PointF[] { new PointF(x,y) }, new Size(imgWidth,imgHeight), new ResizeSettings(query))[0];
        //    Assert.AreEqual<PointF>(new PointF(expectedX,expectedY), result );
        //}

        //[Test]
        //public void TestWithWebResponseStream() {
        //    WebRequest request = WebRequest.Create("http://www.google.com/intl/en_com/images/srpr/logo2w.png");
        //    WebResponse response = request.GetResponse();

        //    using(Stream input = response.GetResponseStream())
        //    using(MemoryStream output = new MemoryStream())
        //    {

        //    ResizeSettings rs = new ResizeSettings();

        //    rs.Height = 100;

        //    rs.Stretch = StretchMode.Fill;

        //    rs.Scale = ScaleMode.Both;

        //    //ImageBuilder.Current.Build(@"C:\Temp\Images\clock.gif", output, rs);

        //    ImageBuilder.Current.Build(input, output, rs); 
        //    }

        //}

        //[Test]
        //public void ResizeInPlace() {
        //    GetBitmap(100, 100).Save("test-image.png", ImageFormat.Png);
        //    ImageBuilder.Current.Build("test-image.png", "test-image.png", new ResizeSettings("width=20"));
        //    File.Delete("test-image.png");


        //}

        //[Test]
        //public void TestSourceBitmapDisposed([Column(true, false)] bool dispose,
        //                                    [Column(true, false)] bool useDestinationStream,
        //                                    [Column(true, false)] bool useCorruptedSource,
        //                                    [Column(true, false)] bool loadTwice,
        //                                    [Column(true, false)] bool useSourceStream) {
        //    if (useCorruptedSource) useSourceStream = true;//Required

        //    object source = null;
        //    if (!useSourceStream){ //Source is a bitmap here
        //        source = GetBitmap(10,10);
        //    }else if (useCorruptedSource){ //A corrupted stream
                
        //        byte[] randomBytes = new byte[256];
        //        new Random().NextBytes(randomBytes);
        //        source = new MemoryStream(randomBytes);
        //        ((MemoryStream)source).Position = 0;
        //        ((MemoryStream)source).SetLength(randomBytes.Length);
        //    }else{ //A png stream
        //        using(Bitmap b = GetBitmap(10,10)){
        //            MemoryStream ms = new MemoryStream();
        //            b.Save(ms,ImageFormat.Png);
        //            ms.Position = 0;
        //            source = ms;
        //        }
        //    }
            
        //    //The destination object, if it exists.
        //    object dest = useDestinationStream ? new MemoryStream() : null;

            
        //    if (loadTwice){
        //        bool corrupted = false;
        //        try {
        //            source = c.CurrentImageBuilder.LoadImage(source, new ResizeSettings());
        //        } catch (ImageCorruptedException) {
        //            corrupted = true;
        //            source = null;
        //        }
        //        Assert.AreEqual<bool>(useCorruptedSource,corrupted);
        //    }

        //    if (source == null) return;

        //    bool wasCorrupted = false;
        //    try{
        //        if (dest != null)
        //            c.CurrentImageBuilder.Build(source, dest,new ResizeSettings(""), dispose);
        //        else
        //            using (Bitmap b2 = c.CurrentImageBuilder.Build(source, new ResizeSettings(""), dispose)) { }
        //    }catch(ImageCorruptedException){
        //        wasCorrupted = true;
        //    }
        //    Assert.AreEqual<bool>(useCorruptedSource,wasCorrupted);

        //    bool wasDisposed = false;
        //    try {
        //        if (source is Bitmap) ((Bitmap)source).Clone();
        //        if (source is MemoryStream) wasDisposed = !((MemoryStream)source).CanRead;
        //    }catch (ArgumentException){wasDisposed = true;}

        //    Assert.AreEqual<bool>(dispose,wasDisposed);

        //}


    }
}
