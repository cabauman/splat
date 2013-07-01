using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#if UIKIT
using MonoTouch.UIKit;
using MonoTouch.Foundation;
#else
using MonoMac.AppKit;
using MonoMac.Foundation;

using UIImage = MonoMac.AppKit.NSImage;
#endif

namespace Splat
{
    public class PlatformBitmapLoader : IBitmapLoader
    {
        public Task<IBitmap> Load(Stream sourceStream, float? desiredWidth, float? desiredHeight)
        {
            return Task.Run(() => {
                var data = NSData.FromStream(sourceStream);

                #if UIKIT
                return (IBitmap)new CocoaBitmap(UIImage.LoadFromData(data));
                #else
                return (IBitmap) new CocoaBitmap(new UIImage(data));
                #endif
            });
        }

        public IBitmap Create(float width, float height)
        {
            throw new NotImplementedException();
        }
    }

    sealed class CocoaBitmap : IBitmap
    {
        internal UIImage inner;
        public CocoaBitmap(UIImage inner)
        {
            this.inner = inner;
        }

        public float Width {
            get { return inner.Size.Width; }
        }

        public float Height {
            get { return inner.Size.Height; }
        }

        public Task Save(CompressedBitmapFormat format, float quality, Stream target)
        {
            return Task.Run(() => {
                #if UIKIT
                var data = format == CompressedBitmapFormat.Jpeg ? inner.AsJPEG((float)quality) : inner.AsPNG();
                data.AsStream().CopyTo(target);
                #else
                var imageRep = (NSBitmapImageRep)NSBitmapImageRep.ImageRepFromData(inner.AsTiff());
                var props = format == CompressedBitmapFormat.Png ? 
                    new NSDictionary() : 
                        new NSDictionary(new NSNumber(quality), new NSString("NSImageCompressionFactor"));
                var type = format == CompressedBitmapFormat.Png ? NSBitmapImageFileType.Png : NSBitmapImageFileType.Jpeg;

                var outData = imageRep.RepresentationUsingTypeProperties(type, props);
                outData.AsStream().CopyTo(target);
                #endif
            });
        }

        public void Dispose()
        {
            var disp = Interlocked.Exchange(ref inner, null);
            if (disp != null) disp.Dispose();
        }
    }

    public static class BitmapMixins
    {
        public static UIImage ToNative(this IBitmap This)
        {
            return ((CocoaBitmap)This).inner;
        }

        public static IBitmap FromNative(this UIImage This, bool copy = false)
        {
            if (copy) return new CocoaBitmap((UIImage)This.Copy());

            return new CocoaBitmap(This);
        }
    }
}