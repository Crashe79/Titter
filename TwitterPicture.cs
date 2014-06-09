using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Titter
{
    public class TwitterPicture
    {
        private const string text2 = "Posted via Titter - Advanced tweets http://titter.webrunes.com";

        public static byte[] CreatePictureFromText(string text)
        {
            var textWithoutSpecSimbols = text.Replace("\n", "").Replace("\r", "");
            var words = textWithoutSpecSimbols.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            text = string.Join(" ", words);

            var btp = new Bitmap(460, 200);
            var font = new Font("Arial", 9.0f);

            var layoutSize = new SizeF(460.0F, 1000.0F);
            SizeF textSize;
            SizeF text2Size;

            using (var g = Graphics.FromImage(btp))
            {
                textSize = g.MeasureString(text, font, layoutSize);
                text2Size = g.MeasureString(text2, font, layoutSize);
            }

            var hText = (int)Math.Floor(textSize.Height * 1.05f);
            var hText2 = (int)Math.Floor(text2Size.Height * 1.05f);
            var h = hText + hText2 + 15;

            btp = new Bitmap(460, h);
            using (var g = Graphics.FromImage(btp))
            {
                g.Clear(Color.White);
                Brush brush = new SolidBrush(Color.FromArgb(255, 51, 51, 51));
                g.DrawString(text, font, brush, new RectangleF(0, 0, 460, hText));
                brush = new SolidBrush(Color.FromArgb(255, 170, 170, 170));
                g.DrawString(text2, font, brush, new RectangleF(0, hText + 12, 460, hText2));
                font.Dispose();
            }

            using (var stream = new System.IO.MemoryStream())
            {
                btp.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }
    }
}
