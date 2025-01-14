using System.IO;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace MCERemote.Controller
{
    internal class ImageLoader
    {
        public static BitmapImage ConvertToBitmapImage(Image image)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                image.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

    }
}
