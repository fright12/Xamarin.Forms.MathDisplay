using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Extensions;
using Xamarin.Forms.Extensions;

namespace Xamarin.Forms.MathDisplay
{
    public abstract class MathLayout : TouchableStackLayout, IMathView
    {
        public MathLayout ParentMathLayout
        {
            get
            {
                Element parent = this;

                do
                {
                    parent = parent.Parent;
                }
                while (parent != null && !(parent is MathLayout));

                return parent as MathLayout;
            }
        }

        public virtual double FontSize
        {
            get
            {
                return fontSize;
            }
            set
            {
                fontSize = value;

                //Ripple the font change down through the children
                foreach (View view in Children)
                {
                    if (view is IMathView)
                    {
                        (view as IMathView).FontSize = value;
                    }
                }

                InvalidateMeasure();
            }
        }
        private double fontSize = Text.MaxFontSize;

        public abstract double Middle { get; }
        public virtual Expression InputContinuation => null;
        public abstract double MinimumHeight { get; }

        protected double Midline = 0.5;

        public abstract void Lyse();
        protected void Lyse(params Expression[] sources)
        {
            int index = this.Index();
            for (int i = sources.Length - 1; i >= 0; i--)
            {
                for (int j = sources[i].Children.Count - 1; j >= 0; j--)
                {
                    (Parent as Expression).Insert(index + 1, sources[i].Children[j]);
                }
            }
            (Parent as Expression).RemoveAt(index);
        }

        private Dictionary<Size, Tuple<SizeRequest, double>> LayoutDataCache = new Dictionary<Size, Tuple<SizeRequest, double>>();

        protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
        {
            if (Orientation == StackOrientation.Vertical)
            {
                return base.OnMeasure(widthConstraint, heightConstraint);
            }

            Size size = new Size(widthConstraint, heightConstraint);

            if (Children.Count == 0)
            {
                return new SizeRequest(new Size(PadLeft + PadRight, MinimumHeight));
            }
            else if (LayoutDataCache.ContainsKey(size))
            {
                Tuple<SizeRequest, double> tuple = LayoutDataCache[size];
                Midline = tuple.Item2;
                return tuple.Item1;
            }

            SizeRequest request = base.OnMeasure(widthConstraint, heightConstraint);

            double aboveMidline = 0;
            double belowMidline = 0;

            for (int i = 0; i < Children.Count; i++)
            {
                View v = Children[i];

                if (v.IsVisible && v.ToString() != "(" && v.ToString() != ")")
                {
                    double height = (v is Expression) && (v as Expression).Children.Count == 0 ? 0 : v.Measure().Height;
                    double above = GetMidline(Children[i]) * height;

                    aboveMidline = System.Math.Max(aboveMidline, above);
                    belowMidline = System.Math.Max(belowMidline, height - above);
                }
            }

            if (aboveMidline + belowMidline != 0)
            {
                Midline = aboveMidline / (belowMidline + aboveMidline);
            }
            request = new SizeRequest(new Size(PadLeft + request.Request.Width + PadRight, System.Math.Max(MinimumHeight, aboveMidline + belowMidline)));

            LayoutDataCache.Add(size, new Tuple<SizeRequest, double>(request, Midline));
            return request;
        }

        protected virtual double GetMidline(View view) => (view as IMathView)?.Middle ?? 0.5;

        public double PadLeft = 0;
        public double PadRight = 0;

        protected override void LayoutChildren(double x, double y, double width, double height)
        {
            base.LayoutChildren(x, y, width, height);

            if (Orientation == StackOrientation.Vertical)
            {
                return;
            }

            Stack<View> openingParenthesis = new Stack<View>();
            Stack<Range> bounds = new Stack<Range>();

            Action<View, Range> resize = (v, r) =>
            {
                (v as dynamic).HeightRequest = r.Size;
                LayoutChildIntoBoundingRegion(v, new Rectangle(PadLeft + v.X, Midline * height - r.Upper, v.Width, r.Size));
            };
            Action open = () => bounds.Push(new Range(MinimumHeight / 2, -MinimumHeight / 2));
            Action close = () =>
            {
                resize(openingParenthesis.Pop(), bounds.Peek());

                if (bounds.Count > 1)
                {
                    bounds.Push(Range.Max(bounds.Pop(), bounds.Pop()));
                }
            };

            open();

            for (int i = 0; i < Children.Count; i++)
            {
                if (!Children[i].IsVisible)
                {
                    continue;
                }

                if (Children[i].ToString() == "(")
                {
                    openingParenthesis.Push(Children[i]);
                    open();
                }
                else if (Children[i].ToString() == ")")
                {
                    resize(Children[i], bounds.Peek());

                    if (openingParenthesis.Count > 0)
                    {
                        close();
                    }
                }
                else
                {
                    double above = Children[i].Height * GetMidline(Children[i]);
                    
                    LayoutChildIntoBoundingRegion(Children[i], new Rectangle(PadLeft + Children[i].X, Midline * height - above, Children[i].Width, Children[i].Height));

                    if (above > bounds.Peek().Upper || Children[i].Height - above > -bounds.Peek().Lower)
                    {
                        bounds.Push(Range.Max(bounds.Pop(), new Range(above, -(Children[i].Height - above))));
                    }
                }
            }

            while (openingParenthesis.Count > 0)
            {
                close();
            }

            /*Point pos = Point.Zero;

            for (int i = 0; i < Children.Count; i++)
            {
                Size size = Children[i].Measure(double.PositiveInfinity, double.PositiveInfinity).Request;
                size = new Size(size.Width == 0 ? width : size.Width, size.Height == 0 ? height : size.Height);

                LayoutChildIntoBoundingRegion(Children[i], new Rectangle(pos, size));

                if (Orientation == StackOrientation.Horizontal)
                {
                    pos = pos.Add(new Point(size.Width, 0));
                }
                else if (Orientation == StackOrientation.Vertical)
                {
                    pos = pos.Add(new Point(0, size.Height));
                }
            }*/
        }

        protected override void InvalidateLayout()
        {
            base.InvalidateLayout();
            LayoutDataCache.Clear();
        }

        protected override void OnChildMeasureInvalidated()
        {
            base.OnChildMeasureInvalidated();
            LayoutDataCache.Clear();
        }
        
        public abstract string ToLatex();
    }
}
