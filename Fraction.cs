using System;
using System.Collections.Generic;
using System.Text;

using System.Extensions;

using Xamarin.Forms;
using Xamarin.Forms.Extensions;

namespace Crunch.GraphX
{
    public class Fraction : MathLayout
    {
        public Expression Numerator { get; private set; }
        public Expression Denominator { get; private set; }

        public override double FontSize
        {
            set
            {
                Numerator.FontSize = value * Text.FontSizeDecrease;
                Denominator.FontSize = value * Text.FontSizeDecrease;
            }
        }

        public override double Middle => (Numerator.Measure().Height + 1) / this.Measure().Height;
        public override Expression InputContinuation => Denominator;
        public override double MinimumHeight => Numerator.MinimumHeight + 2 + Denominator.MinimumHeight;

        private static readonly int NestedFractionPadding = 20;
        private static BoxView Bar => new BoxView { HeightRequest = 2, WidthRequest = 0, BackgroundColor = Color.Black };

        public Fraction(View numerator, View denominator)
        {
            //BackgroundColor = Color.Beige;
            Orientation = StackOrientation.Vertical;
            HorizontalOptions = LayoutOptions.Center;
            VerticalOptions = LayoutOptions.Center;
            Spacing = 0;

            Numerator = toExpression(numerator);
            Denominator = toExpression(denominator);
            
            Children.AddRange(Numerator, Bar, Denominator);
        }

        public override void Lyse() => Lyse(Numerator, Denominator);

        /*private void Lyse()
        {
            int index = this.Index();
            for (int i = Denominator.Children.Count - 1; i >= 0; i--)
            {
                (Parent as Expression)?.Insert(index + 1, Denominator.Children[i]);
            }
            for (int i = Numerator.Children.Count - 1; i >= 0; i--)
            {
                (Parent as Expression)?.Insert(index + 1, Numerator.Children[i]);
            }
            (Parent as Expression).RemoveAt(index);
        }*/

        private Expression toExpression(View view) => view is Expression ? (view as Expression) : new Expression(view);

        private bool isOnlyChildFraction(Expression e) => e.ChildCount() == 1 && e.ChildAfter(-1) is Fraction;

        protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
        {
            Size size = base.OnMeasure(widthConstraint, heightConstraint).Request;
            double extraPadding = (isOnlyChildFraction(Numerator) || isOnlyChildFraction(Denominator)).ToInt() * NestedFractionPadding;
            return new SizeRequest(new Size(size.Width + extraPadding, size.Height));
        }

        public override string ToLatex() => "\frac{" + Numerator.ToString() + "}{" + Denominator.ToString() + "}";

        public override string ToString() => "(" + Numerator.ToString() + "/" + Denominator.ToString() + ")";
    }
}
