using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using System.Extensions;
using Xamarin.Forms.Extensions;

namespace Xamarin.Forms.MathDisplay
{
    public static class SoftKeyboard
    {
        public static event StaticEventHandler<ChangedEventArgs<ViewLoc>> CursorMoved;

        public static CursorView Cursor { get; private set; }
        private static int Index;
        private static int RealIndex => Cursor.Parent == null ? 0 : Cursor.Index();

        static SoftKeyboard()
        {
            Cursor = new CursorView();
            Cursor.MeasureInvalidated += (sender, e) => Cursor.HeightRequest = Text.MaxTextHeight * Cursor.Parent.FontSize / Text.MaxFontSize;
        }

        private static async void Blink()
        {
            while (true)
            {
                await System.Threading.Tasks.Task.Delay(500);
                Cursor.Opacity = 1 - Cursor.Opacity;
                //Device.BeginInvokeOnMainThread(() => Cursor.IsVisible = false);
            }
        }

        public static void End(this Expression expression) => MoveCursor(expression, expression.Children.Count);

        public static void Type(string str)
        {
            Index = RealIndex;
            //Suround previous thing with parentheses if it's an exponent or a fraction
            if (str[0] == '^' && Index > 0 && ((Cursor.Parent.Children[Index - 1] is Expression && (Cursor.Parent.Children[Index - 1] as Expression).TextFormat == TextFormatting.Superscript) || Cursor.Parent.Children[Index - 1] is Fraction))
            {
                Cursor.Parent.Insert(Cursor.Parent.Children.BeginningOfPreviousMathObject(Index++), Text.CreateLeftParenthesis());
                Cursor.Parent.Insert(Index++, Text.CreateRightParenthesis());
            }
            
            View[] list;
            if (str == "(")
            {
                list = new View[] { Text.CreateLeftParenthesis() };
            }
            else if (str == ")")
            {
                list = new View[] { Text.CreateRightParenthesis() };
            }
            else
            {
                list = Reader.Render(Crunch.Machine.StringClassification.Simple(str));
            }

            if (list[0] is Fraction && (list[0] as Fraction).Numerator.Children.Count == 0)
            {
                (list[0] as Fraction).Numerator.Fill(Cursor.Parent.Children, Index - 1);
                (list[0] as Fraction).Numerator.Trim();
                Index -= (list[0] as Fraction).Numerator.Children.Count;
            }

            Cursor.Parent.Insert(Index, list);
            Index += list.Length;

            if ((list.Last() as MathLayout)?.InputContinuation != null)
            {
                (list.Last() as MathLayout).InputContinuation.Add(Cursor);
                Index = 0;
            }
            
            Cursor.Parent.OnInputChanged();
        }

        public static bool Delete()
        {
            Index = RealIndex;
            //print.log(index, Cursor.Parent.Children.Count, Cursor.Parent);
            //foreach (View v in Cursor.Parent.Children)
                //print.log(v, v.GetType());
            //Try to delete the container
            if (Index == 0)
            {
                Element parent = Cursor;

                do
                {
                    parent = parent.Parent;

                    //Can't delete container
                    if (parent == null)
                    {
                        return false;
                    }
                }
                //Looking for a MathLayout that we can break up into an Expression
                while (!(parent is MathLayout && parent.Parent is Expression && (parent.Parent as Expression).Editable));

                (parent as MathLayout).Lyse();
                Index = Cursor.Index();
            }
            //Otherwise just delete the thing before
            else
            {
                Index--;
                Cursor.Parent.RemoveAt(Index);
            }
            
            Cursor.Parent.OnInputChanged();
            
            return true;
        }

        public static void Right() => move(1);
        public static void Left() => move(-1);
        public static bool Up() => MoveVertical(-1);
        public static bool Down() => MoveVertical(1);

        private static bool MoveVertical(int direction)
        {
            View parent = Cursor.Parent;
            while (true)
            {
                if (parent?.Parent is Fraction)
                {
                    int index = parent.Index() + direction * 2;
                    if (index.IsBetween(0, 2))
                    {
                        Expression e = (Expression)(parent.Parent as Fraction).Children[index];
                        return Center(
                            e,
                            Cursor.PositionOn(parent.Parent as Fraction).X,
                            e.PositionOn(parent.Parent as Fraction).X,
                            direction
                            );
                    }
                }

                parent = parent?.Parent as View;
                if (parent == null)
                {
                    return false;
                }
            }
        }

        private static bool Center(Expression root, double target, double offset, int direction)
        {
            if (target <= offset + root.PadLeft)
            {
                return MoveCursor(root, 0);
            }
            else if (target >= offset + root.Width - root.PadRight)
            {
                return MoveCursor(root, root.Children.Count);
            }
            Index = RealIndex;
            Tuple<Expression, int> last = new Tuple<Expression, int>(root, Index = root.Children.Count * (direction - 1) / -2);
            Expression e = root;
            double left = offset + Position(root, Index);
            double right;
            while(true)
            {
                e = checkIndex(direction, e);
                if (e == null)
                {
                    return false;
                }

                right = offset;
                if (e != root)
                {
                    right += e.PositionOn(root).X;
                }
                right += Position(e, Index);

                Print.Log("a;lksjdklf;", left, target, right, last, e, Index);
                if (target.IsBetweenBetter(left, right) || Math.Sign(right - left) == direction * -1)
                {
                    if (Math.Abs(target - left) <= Math.Abs(target - right))
                    {
                        return MoveCursor(last.Item1, last.Item2);
                    }
                    else
                    {
                        return MoveCursor(e, Index);
                    }
                }

                left = right;
                if (e != root)
                {
                    left += e.Width * direction;
                    if (target.IsBetweenBetter(left, right))
                    {
                        return Center(e, target, Math.Min(left, right), direction);
                    }

                    Index = e.Children.Count * (direction + 1) / 2;
                }

                last = new Tuple<Expression, int>(e, Index);
            }
        }

        private static double Position(Expression parent, int index) => index < parent.Children.Count ? parent.Children[index].X : (parent.Children.Count == 0 ? 0 : parent.Width);

        public static bool MoveCursor(Expression parent, int i = 0)
        {
            //if (parent == null || i < 0 || i >= parent.Children.Count || (parent == Cursor.Parent && Index == i))
            Index = RealIndex;
            if (parent == Cursor.Parent && Index == i)
            {
                return false;
            }

            ViewLoc old = Cursor.Parent == null ? null : new ViewLoc(Cursor.Parent, Index);

            Index = i;
            parent.Insert(Index, Cursor);
            //print.log("cursor moved", parent, Index);
            CursorMoved?.Invoke(new ChangedEventArgs<ViewLoc>(old, new ViewLoc(parent, Index)));

            return true;
        }

        private static void move(int direction)
        {
            if (Cursor.Parent != null)
            {
                Index = RealIndex;
                int oldIndex = Index;
                var parent = checkIndex(direction, Cursor.Parent);

                if (parent != Cursor.Parent || Index != oldIndex)
                {
                    parent.Insert(Index, Cursor);
                }
            }
        }

        private static Expression checkIndex(int direction, Expression startingParent)
        {
            Layout<View> parent = startingParent;
            int index = Index;

            do
            {
                //Get the next thing
                View next = parent.ChildInDirection(index - (direction + 1) / 2, direction);
                //If it's a layout, go into it
                if (next is Layout<View> && !(next is Expression && !(next as Expression).Editable))
                {
                    parent = next as Layout<View>;
                    //I either want to be at the very beginning or the very end of the new layout, depending on which direction I'm going
                    index = parent.ChildCount() * (direction - 1) / -2;
                }
                //Continue along this parent
                else
                {
                    index += direction;
                }

                //If I stepped out of the current parent, try to go up
                if (!index.IsBetween(0, parent.ChildCount()))
                {
                    //I can't go up because up isn't a layout or isn't editable - return to where I started
                    if (!(parent.Parent is Layout<View>) || (parent.Parent is Expression && !(parent.Parent as Expression).Editable()))
                    {
                        index -= direction;
                        return startingParent;
                    }

                    index = (parent.Parent as Layout<View>).IndexOf(parent) + (direction + 1) / 2;
                    parent = parent.Parent as Layout<View>;
                }
            }
            while (!(parent is Expression));

            Index = index;
            return parent as Expression;
        }

        public static void Trim(this Expression e)
        {
            while (e.Children.Count > 0 && e.Children[e.Children.Count - 1].ToString().Trim() == ")" && e.Children[0].ToString().Trim() == "(")
            {
                e.RemoveAt(e.Children.Count - 1);
                e.RemoveAt(0);
            }
        }

        public static void Fill(this Expression e, IList<View> input, int index)
        {
            int count = index - input.BeginningOfPreviousMathObject(index);
            for (int i = 0; i <= count; i++)
            {
                e.Children.Add(input[index - count]);
            }
        }

        public static int BeginningOfPreviousMathObject(this IList<View> input, int index)
        {
            int imbalance = 0;
            View view = default;

            string current;
            //Grab stuff until we hit an operand
            while (index.IsBetween(0, input.Count - 1) && !(Crunch.Machine.StringClassification.IsOperator(input[index].ToString().Trim()) && input[index].ToString() != "-" && imbalance == 0))
            {
                view = input[index];

                string s = view.ToString().Trim();
                if (s == "(" || s == ")")
                {
                    if (s == "(")
                    {
                        if (imbalance == 0) break;
                        imbalance++;
                    }
                    if (s == ")") imbalance--;
                }

                index--;
            }

            return index + 1;
        }

        public static View ChildInDirection(this Layout<View> parent, int index, int direction)
        {
            if ((index + direction).IsBetween(0, parent.Children.Count - 1))
            {
                index += direction;
                if (parent.Children[index] is CursorView)// || (parent is Expression && !(parent.Children[index] is IMathView)))
                {
                    return parent.ChildInDirection(index, direction);
                }
                return parent.Children[index];
            }
            return null;
        }

        public static View ChildBefore(this Layout<View> parent, int index) => parent.ChildInDirection(index, -1);

        public static View ChildAfter(this Layout<View> parent, int index) => parent.ChildInDirection(index, 1);

        //private static bool HideCursor(this Layout<View> parent, int index) => Cursor.Parent == parent && index >= SoftKeyboard.index;

        //private static bool HideCursor(this Layout<View> parent) => Cursor.Parent == parent;

        //public static int ChildCount(this Layout<View> layout) => (layout as Expression)?.MathViewCount ?? layout.Children.Count - (Cursor.Parent == layout).ToInt();

        public static int ChildCount(this Layout<View> layout) => layout.Children.Count - (Cursor.Parent == layout).ToInt();

        public static int IndexOf(this Layout<View> layout, View child)
        {
            int index = 0;

            foreach(View v in layout.Children)
            {
                if (v == child)
                {
                    return index;
                }
                else if (!(v is CursorView))// && (!(layout is Expression) || v is IMathView))
                {
                    index++;
                }
            }

            return -1;

            //int index = layout.Children.IndexOf(child);
            //return index - layout.HideCursor(index).ToInt();
        }

        public static bool Editable(this Layout<View> layout) => layout is Expression && (layout as Expression).Editable;
    }
}
