using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xamarin.Forms.MathDisplay
{
    public class ImageText : Image, IMathView
    {
        new public FontImageSource Source
        {
            get => base.Source as FontImageSource;
            set => base.Source = value;
        }

        public string Text
        {
            get => Source?.Glyph;
            set => (Source ?? (Source = new FontImageSource { FontFamily = (OnPlatform<string>)Application.Current.Resources["SymbolaFont"] })).Glyph = value;
        }

        public double Middle => 0.5;

        public double FontSize
        {
            set { }
        }

        public override string ToString() => Text ?? base.ToString();

        public string ToLatex() => ToString();
    }
}
