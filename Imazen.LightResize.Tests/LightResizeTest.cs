using System;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web;
using NUnit.Framework;

namespace Imazen.LightResize.Tests
{
    [TestFixture]
    public class LightResizeTest {

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
        [TestCase(50, 50, "format=jpg&quality=100")]
        [TestCase(1, 1, "format=jpg&quality=100")]
        [TestCase(50, 50, "format=jpg&quality=-300")]
        [TestCase(50, 50, "format=png")]
        public void EncodeImage(int width, int height, string query){
            using (MemoryStream ms = new MemoryStream(8000)){
                Job(query).Build(GetBitmapStream(width, height), ms, JobOptions.LeaveTargetStreamOpen);
            }
        }
    }
}
