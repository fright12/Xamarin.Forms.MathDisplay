using System;
using System.Collections.Generic;
using System.Text;

using System.Extensions;
using Xamarin.Forms;
using Xamarin.Forms.Extensions;

namespace Crunch.GraphX
{
    public delegate void HeightChangedEventHandler(Expression e);

    public class Expression : TouchableStackLayout
    {
        public event HeightChangedEventHandler HeightChanged;
        public bool Editable = false;

        public new Expression Parent => base.Parent as Expression;

        //public View ChildAt(int index) => Children[index + this.HideCursor(index).ToInt()];

        public double FontSize = Text.MaxFontSize;

        public static readonly float fontSizeDecrease = 4f / 5f;

        public double lastHeight = -1;

        public Expression()
        {
            //BackgroundColor = Color.SeaGreen;
            //Padding = new Thickness(10, 10, 10, 10);
            Orientation = StackOrientation.Horizontal;
            HorizontalOptions = LayoutOptions.Center;
            VerticalOptions = LayoutOptions.Center;
            HeightRequest = Text.MaxTextHeight;
            Spacing = 0;

            ChildAdded += delegate { CheckPadding(); };
            ChildRemoved += delegate { CheckPadding(); };
            CheckPadding();
        }

        public Expression(params View[] children) : this() => AddRange(children);

        /*public Expression rim()
        {
            while (Children.Count > 0 && Children[Children.Count - 1] is Text && (Children[Children.Count - 1] as Text).Text == ")" && Children[0] is Text && (Children[0] as Text).Text == "(")
            {
                RemoveAt(Children.Count - 1);
                RemoveAt(0);
            }

            return this;
        }*/

        public void AddRange(params View[] list)
        {
            InsertRange(Children.Count, list);
            //Trim();
        }

        public void InsertRange(int index, params View[] list)
        {
            //int removeParends = (list.Length > 0 && (list[0] as Text)?.Text == "(" && (list[list.Length - 1] as Text)?.Text == ")").ToInt();

            for (int i = 0; i < list.Length; i++)
            {
                Children.Insert(index + i, list[i]);
            }
        }

        public void RemoveAt(int index)
        {
            Children.RemoveAt(index);
        }

        public void Add(View view) => Insert(Children.Count, view);

        public void Insert(int index, View view)
        {
            (view.Parent as Expression)?.Children.Remove(view);
            Children.Insert(index, view);
        }

        private static readonly int extraSpaceForCursor = 3;
        private static readonly int nestedFractionPadding = 10;

        public void CheckPadding()
        {
            if (true)//e.Editable)
            {
                bool empty = Children.Count == 0;

                bool left = Orientation == StackOrientation.Horizontal && (empty || Children[0] is Fraction);
                bool right = Orientation == StackOrientation.Horizontal && (empty || Children.Last() is Expression);
                bool isOnlyChildFraction = Parent is Fraction && ((this.ChildCount() == 1 && this.ChildAfter(-1) is Fraction) || this.ChildCount() == 0);

                Padding = new Thickness(
                    Math.Min(nestedFractionPadding, left.ToInt() * extraSpaceForCursor + isOnlyChildFraction.ToInt() * nestedFractionPadding),
                    Padding.Top,
                    Math.Min(nestedFractionPadding, right.ToInt() * extraSpaceForCursor + isOnlyChildFraction.ToInt() * nestedFractionPadding),
                    Padding.Bottom);
            }
        }

        protected virtual double determineFontSize() => Parent.FontSize;

        protected override void OnRemoved(View view)
        {
            base.OnRemoved(view);

            if (Children.Count == 0)
            {
                HeightRequest = Text.MaxTextHeight;
            }
        }

        protected override void OnAdded(View view)
        {
            base.OnAdded(view);

            if (Children.Count == 1)
            {
                HeightRequest = -1;
            }

            //print.log("child added", view);
            //print.log("view is selectable: " + Selectable, FontSize);//, this == MainPage.focus, MainPage.focus.Selectable);
            //view.SetSelectable(Selectable);

            if (view is Text)
            {
                (view as Text).FontSize = FontSize;
            }
            else if (view is Expression)
            {
                Expression e = view as Expression;
                e.FontSize = e.Parent != null ? e.determineFontSize() : Text.MaxFontSize;
                e.Editable = Editable || e.Editable;
                foreach (View v in e.Children)
                {
                    e.OnAdded(v);
                }
            }
            else if (view == SoftKeyboard.Cursor)
            {
                SoftKeyboard.Cursor.HeightRequest = Text.MaxTextHeight * FontSize / Text.MaxFontSize;
            }

            //CheckPadding();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            
            if (lastHeight != Height)
            {
                lastHeight = Height;
                HeightChanged?.Invoke(this);
            }
        }

        public virtual string ToLatex()
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
                    s += Machine.StringClassification.Simple(v.ToString()).Trim();
                }
            }
            return s;
        }

        public override string ToString()
        {
            string s = "";
            foreach (View v in Children)
            {
                s += Machine.StringClassification.Simple(v.ToString()).Trim();
            }
            return "(" + s + ")";
        }
    }
}
