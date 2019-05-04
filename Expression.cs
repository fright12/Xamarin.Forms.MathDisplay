using System;
using System.Collections.Generic;
using System.Text;

using System.Extensions;
using Xamarin.Forms.Extensions;

namespace Xamarin.Forms.MathDisplay
{
    public delegate void HeightChangedEventHandler(Expression e);
    public delegate void InputChangedEventHandler();
    public delegate void ChildrenChangedEventHandler(object sender, ElementEventArgs e, bool added);
    public delegate void DeletedEventHandler();

    public class Range
    {
        public double Upper { get; private set; }
        public double Lower { get; private set; }
        public double Size => Upper - Lower;

        public Range(double upper, double lower)
        {
            Upper = upper;
            Lower = lower;
        }

        public static Range Max(Range r1, Range r2) => new Range(Math.Max(r1.Upper, r2.Upper), Math.Min(r1.Lower, r2.Lower));
    }

    public enum TextFormatting { None = 0, Superscript = 94, Subscript = 95 }

    public class Expression : MathLayout
    {
        public event HeightChangedEventHandler HeightChanged;
        public event InputChangedEventHandler InputChanged;
        public event ChildrenChangedEventHandler ChildrenChanged;

        public new Expression Parent => base.Parent as Expression;
        public TextFormatting TextFormat { get; private set; }
        public bool Editable = false;

        public override double FontSize
        {
            set
            {
                base.FontSize = value * (TextFormat != TextFormatting.None ? Text.FontSizeDecrease : 1);
            }
        }

        public override double Middle
        {
            get
            {
                if (TextFormat == TextFormatting.Superscript)
                {
                    return 1 + (1 - Text.FontSizeDecrease) / 2;
                }
                else if (TextFormat == TextFormatting.Subscript)
                {
                    return -(1 - Text.FontSizeDecrease) / 2;
                }
                else
                {
                    return Midline;
                }
            }
        }
        public override Expression InputContinuation => TextFormat != TextFormatting.Subscript && Children.Count == 0 ? this : null;
        public override double MinimumHeight => System.Math.Ceiling(Text.MaxTextHeight * FontSize / Text.MaxFontSize);

        public Expression(TextFormatting format = TextFormatting.None)
        {
            //BackgroundColor = Color.LightGreen;
            Orientation = StackOrientation.Horizontal;
            HorizontalOptions = LayoutOptions.Center;
            VerticalOptions = LayoutOptions.Center;
            Spacing = 0;
            
            TextFormat = format;

            ChildAdded += (sender, e) => ChildrenChanged?.Invoke(sender, e, true);
            ChildRemoved += (sender, e) => ChildrenChanged?.Invoke(sender, e, false);
            //ChildrenChanged += delegate { CheckPadding(); };
            
            //CheckPadding();
        }

        public Expression(params View[] children) : this() => Add(children);
        public Expression(TextFormatting format, params View[] children) : this(format) => Add(children);

        public override void Lyse() => Lyse(this);

        public void Add(params View[] children) => Insert(Children.Count, children);

        //public void Insert<T>(int index, params T[] children) where T : View, IMathView
        public void Insert(int index, params View[] children)
        {
            for (int i = 0; i < children.Length; i++)
            {
                (children[i].Parent as Expression)?.Children.Remove(children[i]);
                Children.Insert(index + i, children[i]);
                
                if (children[i] is IMathView)
                {
                    (children[i] as IMathView).FontSize = FontSize;
                }

                if (children[i] is Layout<View>)
                {
                    PropogateProperty(children[i] as Layout<View>, Editable, (e, v) => e.Editable = e.Editable || v);
                }
            }
        }

        public void RemoveAt(int index) => Children.RemoveAt(index);

        internal void OnInputChanged()
        {
            InputChanged?.Invoke();
            Xamarin.Forms.Extensions.ExtensionMethods.Parent<Expression>(this)?.OnInputChanged();
        }

        private void PropogateProperty<T>(Layout<View> parent, T value, Action<Expression, T> setter)
        {
            if (parent is Expression)
            {
                setter(parent as Expression, value);
            }

            foreach(View v in parent.Children)
            {
                if (v is Layout<View>)
                {
                    PropogateProperty(v as Layout<View>, value, setter);
                }
            }
        }

        public static readonly int extraSpaceForCursor = 3;
        private int leftSpaceForCursor => (Editable && (Children.Count == 0 || Children[0] is Layout<View>)).ToInt() * extraSpaceForCursor;
        private int rightSpaceForCursor => (Editable && (Children.Count == 0 || Children.Last() is Layout<View>)).ToInt() * extraSpaceForCursor;

        protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
        {
            PadLeft = leftSpaceForCursor;
            PadRight = rightSpaceForCursor;
            return base.OnMeasure(widthConstraint, heightConstraint);
        }

        private double lastHeight = -1;

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            
            if (lastHeight != Height)
            {
                lastHeight = Height;
                HeightChanged?.Invoke(this);
            }
        }

        private string TextFormatString => (char)TextFormat == '\0' ? "" : ((char)TextFormat).ToString();

        public override string ToLatex()
        {
            string s = "";
            foreach (View v in Children)
            {
                if (v is Expression)
                {
                    s += (v as Expression).ToLatex();
                }
                else
                {
                    s += Crunch.Machine.StringClassification.Simple(v.ToString()).Trim();
                }
            }
            return TextFormatString + "{" + s + "}";
        }

        public override string ToString()
        {
            string s = "";
            foreach (View v in Children)
            {
                s += Crunch.Machine.StringClassification.Simple(v.ToString()).Trim();
            }
            
            if (s == "")
            {
                return "";
            }

            return TextFormatString + "(" + s + ")";
        }
    }
}
