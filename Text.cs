using System;
using System.Collections.Generic;
using System.Text;

using Xamarin.Forms.Extensions;

namespace Xamarin.Forms.MathDisplay
{
    public class Text : Label, IMathView
    {
        public static Func<View> CreateLeftParenthesis = () => new Text("(") { VerticalTextAlignment = TextAlignment.Center };
        public static Func<View> CreateRightParenthesis = () => new Text(")") { VerticalTextAlignment = TextAlignment.Center };
        public static Func<View> CreateRadical = () => new Text("sqrt(") { VerticalTextAlignment = TextAlignment.Center };

        public static int MaxFontSize = 33;
        public static double MaxTextHeight;
        public static readonly double FontSizeDecrease = 0.8;

        public new Expression Parent => base.Parent as Expression;
        
        public Text(bool isVisible = true)
        {
            //BackgroundColor = Color.Yellow;
            VerticalOptions = LayoutOptions.Center;
            HorizontalOptions = LayoutOptions.Center;
            IsVisible = isVisible;
        }

        public Text(string text) : this() => Text = text;

        public double Middle => 0.5;

        //public void Format(double maxFontSize) => FontSize = maxFontSize;

        public string ToLatex() => ToString();
        public override string ToString() => Crunch.Machine.StringClassification.Simple(Text);
    }
}
