using System;
using System.Collections.Generic;
using System.Text;

using Xamarin.Forms;
using Xamarin.Forms.Extensions;

namespace Crunch.GraphX
{
    public class Fraction : Expression
    {
        public Expression Numerator;
        public Expression Denominator;

        private readonly BoxView bar = new BoxView { HeightRequest = 2, WidthRequest = 0, BackgroundColor = Color.Black };

        public Fraction(View numerator, View denominator) : base()
        {
            Orientation = StackOrientation.Vertical;

            Numerator = toExpression(numerator);
            Denominator = toExpression(denominator);

            AddRange(Numerator, bar, Denominator);
        }

        private Expression toExpression(View view) => view is Expression ? (view as Expression) : new Expression(view);

        protected override double determineFontSize()
        {
            if (Parent != null && Parent.Parent != null && Parent.Parent is Fraction)
            {
                return Parent.FontSize * fontSizeDecrease;
            }
            return base.determineFontSize();
        }

        public override string ToLatex()
        {
            return "\frac{" + Numerator.ToString() + "}{" + Denominator.ToString() + "}";
        }

        public override string ToString()
        {
            return "(" + Numerator.ToString() + "/" + Denominator.ToString() + ")";
        }
    }
}
