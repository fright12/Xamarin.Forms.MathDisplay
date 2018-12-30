using System;
using System.Collections.Generic;
using System.Text;

using Xamarin.Forms;
using Xamarin.Forms.Extensions;

namespace Crunch.GraphX
{
    public class Text : TouchableLabel, IMathView
    {
        public static int MaxFontSize = 33;
        public static double MaxTextHeight;

        public new Expression Parent => base.Parent as Expression;
        
        public Text(bool isVisible = true)
        {
            VerticalOptions = LayoutOptions.Center;
            HorizontalOptions = LayoutOptions.Center;
            IsVisible = isVisible;
        }

        public Text(string text) : this()
        {
            Text = text;
        }

        public void Format(double maxFontSize) => FontSize = maxFontSize;

        public override string ToString() => Machine.StringClassification.Simple(Text);
    }
}
