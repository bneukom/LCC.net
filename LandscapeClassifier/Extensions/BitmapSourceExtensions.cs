using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LandscapeClassifier.Extensions
{
    public static class BitmapSourceExtensions
    {
        /// <summary>
        /// Converts the format of this bitmap and returns a new bitmap source with the new format.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static BitmapSource ConvertFormat(this BitmapSource source, PixelFormat format)
        {
            return new FormatConvertedBitmap(source, format, source.Palette, 0);
        }

        /// <summary>
        /// Crops the bitmap source to the given width and height and returns the result as a Pbgra32 image.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static BitmapSource Crop(this BitmapSource source, int x, int y, int width, int height)
        {
            return new CroppedBitmap(source, new Int32Rect(x, y, width, height));
        }

        /// <summary>
        /// Crops the bitmap source to the given width and height and returns the result as a Pbgra32 image.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static BitmapSource Resize(this BitmapSource source, int width, int height)
        {
            return new TransformedBitmap(source, new ScaleTransform(width / (float)source.PixelWidth, height / (float)source.PixelHeight));
            /*
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            var group = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
            group.Children.Add(new ImageDrawing(source, new Rect(0, 0, source.Width, source.Height)));
            group.ClipGeometry = new RectangleGeometry(new Rect(0, 0, width, height));

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
                drawingContext.DrawDrawing(group);

            renderTargetBitmap.Render(drawingVisual);

            return renderTargetBitmap;
            */
        }

        /// <summary>
        /// Resizes the image to the given width and height and returns the result as a Pbgra32 image.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static BitmapSource Scale(this BitmapSource source, int width, int height)
        {
            var rect = new Rect(0, 0, width, height);

            var group = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
            group.Children.Add(new ImageDrawing(source, rect));

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
                drawingContext.DrawDrawing(group);

            var resizedImage = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Default);
            resizedImage.Render(drawingVisual);

            return resizedImage;
        }
    }
}
