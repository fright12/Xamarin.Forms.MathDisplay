using System;
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
        public static int Index { get; private set; }

        static SoftKeyboard()
        {
            Cursor = new CursorView();
            Cursor.MeasureInvalidated += (sender, e) => Cursor.HeightRequest = Text.MaxTextHeight * Cursor.Parent.FontSize / Text.MaxFontSize;
        }

        public static void Type(string str)
        {
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
                if (parent.Parent is Fraction)
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

                parent = parent.Parent as View;
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

                print.log("a;lksjdklf;", left, target, right, last, e, Index);
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

        /*private static bool Center(Expression root, double target, double offset, int direction, int a)
        {
            /*if (target >= offset + root.Width)
            {
                return MoveCursor(root, root.Children.Count);
            }
            else if (target <= offset)
            {
                return MoveCursor(root, 0);
            }

            //Expression e = root;
            //;
            Tuple<Expression, int> last = new Tuple<Expression, int>(root, Index = root.Children.Count * (direction - 1) / -2);
            Expression e = root;
            double left = offset + Position(root, Index);
            double right;
            //while (Index != root.Children.Count * (direction + 1) / 2)
            //while (true)
            do
            {
                //int index = Index;
                //Expression prev = e;
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

                print.log("a;lksjdklf;", left, target, right, last, e, Index);
                /*if (target.IsBetweenBetter(left, right) || Math.Sign(right - left) != direction)
                {
                    if (Math.Abs(target - left) <= Math.Abs(target - right))
                    {
                        return MoveCursor(prev, index);
                    }
                    else
                    {
                        return MoveCursor(e, Index);
                    }
                }
                if (Math.Abs(target - left) <= Math.Abs(target - right) && left != right)
                //if (Math.Sign(target - right) != direction)
                //if (target.IsBetweenBetter(left, right))
                {
                    return MoveCursor(last.Item1, last.Item2);
                }

                bool stall = left == right;
                if (!stall)
                {
                    last = new Tuple<Expression, int>(e, Index);
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

                if (stall)
                {
                    left = right;
                }
            }
            while (root != e || (Index > 0 && Index < root.Children.Count));

            return MoveCursor(root, root.Children.Count * (direction + 1) / 2);
        }*/

        private static double Position(Expression parent, int index) => index < parent.Children.Count ? parent.Children[index].X : (parent.Children.Count == 0 ? 0 : parent.Width);

        /*private static bool Center(Expression root, double target, double offset, bool first = true)
        {
            View child = null;
            int index = 0;
            while (target > offset && index < root.Children.Count)
            {
                child = root.Children[index];

                double x = offset + child.X;
                if (target >= x && target < x + child.Width)
                {
                    if (child is MathLayout)
                    {
                        foreach(Expression e in MathLayoutExpressions(root, index))
                        {
                            double pos = x + e.PositionOn(child).X;
                            if (Center(e, target, pos, false))
                            {
                                return true;
                            }
                            else if (pos > target)
                            {
                                MoveCursor(e, 0);
                            }
                        }

                        return false;
                    }
                    else
                    {
                        return MoveCursor(root, index + (target > x + child.Width / 2).ToInt());
                    }
                }

                index++;
            }

            if (first)
            {
                return MoveCursor(root, index);
            }
            else
            {
                return false;
            }
        }*/

        /*private static bool Center(Expression root, double target, double offset)
        {
            Tuple<Expression, int, double> current = new Tuple<Expression, int, double>(root, 0, offset);
            Index = 0;
            double x = offset;
            do
            {
                Expression e = checkIndex(1, current.Item1);

                // Stepped into the beginning of a new parent
                if (Index == 0)
                {
                    x = offset + e.PositionOn(current.Item1).X;
                }
                else
                {
                    x = offset += e.Children[Index - 1].Width;
                }

                if (Math.Abs(target - current.Item3) <= Math.Abs(target - x))
                {
                    return MoveCursor(current.Item1, current.Item2);
                }
                else if (e == root && Index == e.Children.Count)
                {
                    return MoveCursor(root, e.Children.Count);
                }

                current = new Tuple<Expression, int, double>(e, Index, x);
            }
            while (true);
        }*/

        /*private static bool Center(Expression parent, double target, double offset)
        {
            if (parent.Children.Count == 0)
            {
                return false;
            }

            int index = 0;
            while (index < parent.Children.Count - 1 && target >= offset + parent.Children[index].Width)
            {
                /*if (Math.Abs(target - offset) < Math.Abs(target - (offset + parent.Children[index].Width)))
                {
                    view = parent.Children[index];
                    break;
                }

                offset += parent.Children[index].Width;
                index++;
            }
            print.log(";alksjdklf;js", target, offset, index);
            if (target != offset && parent.Children[index] is MathLayout)
            {
                Index = index;
                Expression e = parent;
                do
                {
                    e = checkIndex(1, e);
                }
                while (e != parent.Children[index] && !Center(e, target, offset));
            }
            else
            {
                MoveCursor(parent, index + (target > offset + parent.Children[index].Width / 2).ToInt());
            }

            return true;
        }*/

        /*private static bool MoveVertical(int direction)
        {
            View root = Cursor.Root<View>();
            Point start = Cursor.PositionOn(root);

            View parent = Cursor.Parent;
            while (true)
            {
                if (parent.Parent is Fraction)
                {
                    break;
                }

                parent = parent.Parent as View;
                if (parent == null)
                {
                    return false;
                }
            }

            Expression e = (Expression)parent;
            Tuple<Expression, int> lastPos = new Tuple<Expression, int>(e, Index = e.ChildCount() * (direction + 1) / 2);
            double lastDistance = double.PositiveInfinity;
            print.log("alksjdfkl;asjdfk;l", lastPos.Item1, lastPos.Item2, direction);
            do
            {
                e = checkIndex(direction, lastPos.Item1);
                print.log("checked", e, Index);

                int i = Index == e.Children.Count ? 1 : 0;

                View view = e.Children[Index - i];
                Point point = e.PositionOn(root);
                point = new Point(point.X + view.X + view.Width * i, point.Y + e.Height * e.Middle);
                double distance = start.Distance(point);
                print.log(lastDistance, distance);
                if (distance > lastDistance)
                {
                    break;
                }
                if (e == lastPos.Item1 && Index == lastPos.Item2)
                {
                    return false;
                }

                lastPos = new Tuple<Expression, int>(e, Index);
                lastDistance = distance;
            }
            while (true);

            MoveCursor(lastPos.Item1, lastPos.Item2);

            return true;
        }*/

        public static bool MoveCursor(Expression parent, int i = 0)
        {
            //if (parent == null || i < 0 || i >= parent.Children.Count || (parent == Cursor.Parent && Index == i))
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
            int index = SoftKeyboard.Index;

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

            SoftKeyboard.Index = index;
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
            while (index.IsBetween(0, input.Count - 1) && !(Crunch.Machine.StringClassification.IsOperand(input[index].ToString().Trim()) && input[index].ToString() != "-" && imbalance == 0))
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
                if (parent.Children[index] is CursorView || (parent is Expression && !(parent.Children[index] is IMathView)))
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
