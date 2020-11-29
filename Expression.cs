using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using System.Extensions;
using Xamarin.Forms.Extensions;
using Xamarin.Forms.Xaml;

namespace Xamarin.Forms.MathDisplay
{
    public delegate void HeightChangedEventHandler(Expression e);
    public delegate void InputChangedEventHandler();
    public delegate void ChildrenChangedEventHandler(object sender, ElementEventArgs e, bool added);
    
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

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Expression : MathLayout
    {
        public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(Expression));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public event HeightChangedEventHandler HeightChanged;
        public event InputChangedEventHandler InputChanged;
        public event ChildrenChangedEventHandler ChildrenChanged;

        public new Expression Parent => base.Parent as Expression;
        public TextFormatting TextFormat => (BindingContext as ExpressionViewModel)?.TextFormat ?? TextFormatting.None;
        //public bool Editable = false;

        public static readonly BindableProperty EditableProperty = BindableProperty.Create("Editable", typeof(bool), typeof(Expression), defaultBindingMode: BindingMode.TwoWay, propertyChanged: OnEditableChanged);

        public bool Editable
        {
            get { return (bool)GetValue(EditableProperty); }
            set { SetValue(EditableProperty, value); }
        }

        private static void OnEditableChanged(object bindable, object oldValue, object newValue)
        {
            if (bindable.GetType() == typeof(Expression))
            {
                (bindable as Expression).Editable = (bool)newValue;
            }
            else if (bindable is Expression)
            {
                return;
            }

            if (bindable is Layout<View>)
            {
                Layout<View> layout = bindable as Layout<View>;

                foreach (View v in layout.Children)
                {
                    if (v is Layout<View>)
                    {
                        OnEditableChanged(v as Layout<View>, oldValue, newValue);
                    }
                }
            }
        }

        public override double FontSize
        {
            set
            {
                base.FontSize = value * (TextFormat != TextFormatting.None ? MathDisplay.Text.FontSizeDecrease : 1);
            }
        }

        public override double Middle
        {
            get
            {
                if (TextFormat == TextFormatting.Superscript)
                {
                    return 1 + (1 - MathDisplay.Text.FontSizeDecrease) / 2;
                }
                else if (TextFormat == TextFormatting.Subscript)
                {
                    return -(1 - MathDisplay.Text.FontSizeDecrease) / 2;
                }
                else
                {
                    return Midline;
                }
            }
        }

        public override Expression InputContinuation => TextFormat != TextFormatting.Subscript && Children.Count == 0 ? this : null;
        public override double MinimumHeight => System.Math.Ceiling(MathDisplay.Text.MaxTextHeight * FontSize / MathDisplay.Text.MaxFontSize);

        public Expression() : this(TextFormatting.None) { }

        public Expression(TextFormatting format = TextFormatting.None)
        {
            InitializeComponent();

            //BackgroundColor = Color.LightGreen;
            Orientation = StackOrientation.Horizontal;
            HorizontalOptions = LayoutOptions.Center;
            VerticalOptions = LayoutOptions.Center;
            Spacing = 0;

            /*this.WhenPropertyChanged(TextProperty, (sender, e) =>
            {
                Children.Clear();
                Add(Reader.Render(Text));
            });*/

            ChildAdded += (sender, e) => ChildrenChanged?.Invoke(sender, e, true);
            ChildRemoved += (sender, e) => ChildrenChanged?.Invoke(sender, e, false);
            //ChildrenChanged += delegate { CheckPadding(); };
            
            //CheckPadding();
        }

        //public Expression(IEnumerable<View> children) : this() => AddRange(children);
        internal Expression(params View[] children) : this() => Add(children);
        internal Expression(TextFormatting format, params View[] children) : this(format) => Add(children);

        public override void Lyse() => Lyse(this);

        public void Add(params View[] children) => InsertRange(Children.Count, children);
        public void AddRange(IEnumerable<View> children) => InsertRange(Children.Count, children);

        //public void Insert<T>(int index, params T[] children) where T : View, IMathView
        public void Insert(int index, params View[] children) => InsertRange(index, children);
        public void InsertRange(int index, IEnumerable<View> children)
        {
            //for (int i = 0; i < children.Length; i++)
            int i = 0;
            foreach (View view in children)
            {
                (view.Parent as Expression)?.Children.Remove(view);
                Children.Insert(index + i, view);

                if (view is Layout<View>)
                {
                    //PropogateProperty(children[i] as Layout<View>, Editable, (e, v) => e.Editable = e.Editable || v);
                }
                i++;
            }
        }

        public void RemoveAt(int index) => Children.RemoveAt(index);

        private void OnInputChanged()
        {
            InputChanged?.Invoke();
            this.Parent<Expression>()?.OnInputChanged();
            //Xamarin.Forms.Extensions.ElementExtensions.Parent<Expression>(this)?.OnInputChanged();
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

        protected override void OnChildAdded(Element child)
        {
            base.OnChildAdded(child);

            if (child is IMathView)
            {
                (child as IMathView).FontSize = FontSize;
            }
            OnEditableChanged(child, Editable, Editable);

            if (child is IMathView)
            {
                OnInputChanged();
            }
        }

        protected override void OnChildRemoved(Element child)
        {
            base.OnChildRemoved(child);

            if (child is IMathView)
            {
                OnInputChanged();
            }
        }

        public static readonly int extraSpaceForCursor = 3;
        private int leftSpaceForCursor => (Editable && (this.ChildCount() == 0 || Children[0] is Layout<View>)).ToInt() * extraSpaceForCursor;
        private int rightSpaceForCursor => (Editable && (this.ChildCount() == 0 || Children.Last() is Layout<View>)).ToInt() * extraSpaceForCursor;

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

        private string RelevantToString(object o)
        {
            string ans = o.ToString();
            return ans == o.GetType().ToString() ? "" : ans;
        }

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
                if (v is IMathView)
                {
                    s += Crunch.Machine.StringClassification.Simple(v.ToString()).Trim();
                }
            }
            
            if (s == "")
            {
                return "";
            }

            return TextFormatString + "(" + s + ")";
        }
    }
}
