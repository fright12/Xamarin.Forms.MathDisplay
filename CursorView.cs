using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;

namespace Xamarin.Forms.MathDisplay
{
    public class CursorView : BoxView
    {
        private event EventHandler<ChangedEventArgs<Layout<View>>> ParentChanged;

        public new Expression Parent => base.Parent as Expression;
        //public double Middle => 0.5;
        /*public double FontSize
        {
            set
            {
                HeightRequest = Text.MaxTextHeight * value / Text.MaxFontSize;
            }
        }*/

        public CursorView()
        {
            WidthRequest = 1;
            VerticalOptions = LayoutOptions.Center;
        }

        private Layout<View> LastParent = null;

        protected override void OnParentSet()
        {
            base.OnParentSet();

            if (LastParent != Parent)
            {
                ParentChanged?.Invoke(this, new ChangedEventArgs<Layout<View>>(LastParent, Parent));
                LastParent = Parent;
            }
        }
    }
}
