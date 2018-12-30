using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Extensions;

namespace Crunch.GraphX
{
    public class Radical : MathLayout
    {
        private Expression Operand;
        private Expression Root;
        private View RadicalSign;

        private readonly double MaxRadicalWidth;
        private double RadicalWidth => MaxRadicalWidth * Operand.MinimumHeight / Text.MaxTextHeight;

        public override double FontSize
        {
            set
            {
                Operand.FontSize = value;
                Root.FontSize = value * Text.FontSizeDecrease;

                RadicalSign.WidthRequest = RadicalWidth;
                //Root.MinimumWidth = RadicalWidth * 0.5;
                AbsoluteLayout.SetLayoutBounds(RadicalSign.Parent, new Rectangle(0, 0, RadicalWidth * 0.5, 1));
            }
        }

        public override double Middle => 1 - Operand.Measure().Height * (1 - Operand.Middle) / this.Measure().Height;
        public override Expression InputContinuation => Operand;
        public override double MinimumHeight => Operand.MinimumHeight + 3;

        public Radical(params View[] children) : this(new Expression(), children) { }

        public Radical(Expression root, params View[] children)
        {
            //BackgroundColor = Color.Yellow;
            Orientation = StackOrientation.Horizontal;
            HorizontalOptions = LayoutOptions.Center;
            VerticalOptions = LayoutOptions.Center;
            Spacing = 0;

            if (root.ToString() == "(2)")
            {
                root = new Expression();
            }

            //******* _______ *******
            //*******         *******
            //*******         *******
            StackLayout BarOperand = new StackLayout() { Orientation = StackOrientation.Vertical, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center, Spacing = 0 };
            BarOperand.Children.Add(new BoxView() { HeightRequest = 3, WidthRequest = 0, BackgroundColor = Color.Gray });
            BarOperand.Children.Add(Operand = new Expression(children));

            //*******       / *******
            //*******  __  /  *******
            //*******    \/   *******
            RadicalSign = Render.CreateRadical();
            MaxRadicalWidth = RadicalSign.Measure().Width;

            AbsoluteLayout RadicalContainer = new AbsoluteLayout() { VerticalOptions = LayoutOptions.FillAndExpand };
            RadicalContainer.Children.Add(RadicalSign, new Rectangle(1, 0, AbsoluteLayout.AutoSize, 1), AbsoluteLayoutFlags.PositionProportional | AbsoluteLayoutFlags.HeightProportional);
            AbsoluteLayout ShiftedRadicalContainer = new AbsoluteLayout { VerticalOptions = LayoutOptions.FillAndExpand };
            ShiftedRadicalContainer.Children.Add(RadicalContainer, new Rectangle(), AbsoluteLayoutFlags.HeightProportional);

            //*******        _______ *******
            //*******       /        *******
            //*******  __  /         *******
            //*******    \/          *******
            StackLayout RadicalBarOperand = new StackLayout() { Orientation = StackOrientation.Horizontal, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center, Spacing = 0 };
            RadicalBarOperand.Children.Add(ShiftedRadicalContainer);
            RadicalBarOperand.Children.Add(BarOperand);

            Children.Add(Root = root);
            Children.Add(RadicalBarOperand);
        }

        public override void Lyse() => Lyse(Root, Operand);

        protected override double GetMidline(View view) => view == Root ? 1 : base.GetMidline(view);

        protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
        {
            PadLeft = Math.Max(0, RadicalWidth * 0.5 - Root.Measure().Width);
            Size size = base.OnMeasure(widthConstraint, heightConstraint).Request;
            return new SizeRequest(new Size(Math.Max(size.Width, Operand.Measure().Width + RadicalWidth), size.Height));
        }

        public override string ToLatex() => Operand.ToLatex();

        public override string ToString()
        {
            string root = Root.ToString();
            return "(" + Operand.ToString() + "^(1/" + (root == "" ? "2" : root) + "))";
        }
    }
}
