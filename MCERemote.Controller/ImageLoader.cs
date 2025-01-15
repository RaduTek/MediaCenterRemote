using System.IO;
using System.Windows.Media.Imaging;

namespace MCERemote.Controller
{
    internal class ImageLoader
    {
        public static BitmapImage ByteArrayToBitmapImage(byte[] byteArray)
        {
            BitmapImage bitmap = new BitmapImage();
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze(); // Freeze to make it cross-thread accessible
            }
            return bitmap;
        }

    }
}
