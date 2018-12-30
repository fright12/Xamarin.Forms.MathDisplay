﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace Crunch.GraphX
{
    public class CursorView : BoxView, IMathView
    {
        public new Expression Parent => base.Parent as Expression;

        public CursorView()
        {
            WidthRequest = 1;
            VerticalOptions = LayoutOptions.Center;
        }

        public void Format(double maxFontSize) => HeightRequest = Text.MaxTextHeight * maxFontSize / Text.MaxFontSize;

        public override string ToString() => "";
    }
}
