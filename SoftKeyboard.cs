using System;
using System.Collections.Generic;
using System.Text;

using System.Extensions;
using Xamarin.Forms.Extensions;

namespace Xamarin.Forms.MathDisplay
{
    public static class SoftKeyboard
    {
        public static CursorView Cursor { get; private set; }

        private static int index;

        static SoftKeyboard()
        {
            Cursor = new CursorView() { Color = Color.Gray };
        }

        public static void Type(string str)
        {
            //Suround previous thing with parentheses if it's an exponent or a fraction
            if (str[0] == '^' && index > 0 && ((Cursor.Parent.Children[index - 1] is Expression && (Cursor.Parent.Children[index - 1] as Expression).TextFormat == TextFormatting.Superscript) || Cursor.Parent.Children[index - 1] is Fraction))
            {
                Cursor.Parent.Insert(Cursor.Parent.Children.BeginningOfPreviousMathObject(index++), Render.CreateLeftParenthesis());
                Cursor.Parent.Insert(index++, Render.CreateRightParenthesis());
            }
            
            View[] list;
            if (str == "(")
            {
                list = new View[] { Render.CreateLeftParenthesis() };
            }
            else if (str == ")")
            {
                list = new View[] { Render.CreateRightParenthesis() };
            }
            else
            {
                list = Render.Math(Crunch.Machine.StringClassification.Simple(str));
            }

            if (list[0] is Fraction && (list[0] as Fraction).Numerator.Children.Count == 0)
            {
                (list[0] as Fraction).Numerator.Fill(Cursor.Parent.Children, index - 1);
                (list[0] as Fraction).Numerator.Trim();
                index -= (list[0] as Fraction).Numerator.Children.Count;
            }

            Cursor.Parent.Insert(index, list);
            index += list.Length;

            if ((list.Last() as MathLayout)?.InputContinuation != null)
            {
                (list.Last() as MathLayout).InputContinuation.Add(Cursor);
                index = 0;
            }
            
            Cursor.Parent.OnInputChanged();
        }

        public static bool Delete()
        {
            print.log(index, Cursor.Parent.Children.Count);
            foreach (View v in Cursor.Parent.Children)
                print.log(v, v.GetType());
            //Try to delete the container
            if (index == 0)
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
                index = Cursor.Index();
            }
            //Otherwise just delete the thing before
            else
            {
                index--;
                Cursor.Parent.RemoveAt(index);
            }
            
            Cursor.Parent.OnInputChanged();

            return true;
        }

        public static void Right() => move(1);
        public static void Left() => move(-1);

        public static bool MoveCursor(Expression parent, int i = 0)
        {
            if (parent == Cursor.Parent && index == i)
            {
                return false;
            }

            index = i;
            parent.Insert(index, Cursor);

            return true;
        }

        private static void move(int direction)
        {
            if (Cursor.Parent != null)
            {
                int oldIndex = index;
                var parent = checkIndex(direction, Cursor.Parent);
                
                if (parent != Cursor.Parent || index != oldIndex)
                {
                    parent.Insert(index, Cursor);
                }
            }
        }

        private static Expression checkIndex(int direction, Expression startingParent)
        {
            Layout<View> parent = startingParent;
            int index = SoftKeyboard.index;

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

            SoftKeyboard.index = index;
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
                if (parent.Children[index] is CursorView)
                {
                    return parent.ChildInDirection(index, direction);
                }
                return parent.Children[index];
            }
            return null;
        }

        public static View ChildBefore(this Layout<View> parent, int index) => parent.ChildInDirection(index, -1);

        public static View ChildAfter(this Layout<View> parent, int index) => parent.ChildInDirection(index, 1);

        public static bool HideCursor(this Layout<View> parent, int index) => Cursor.Parent == parent && index >= SoftKeyboard.index;

        public static bool HideCursor(this Layout<View> parent) => Cursor.Parent == parent;

        public static int ChildCount(this Layout<View> layout) => layout.Children.Count - layout.HideCursor().ToInt();

        public static int IndexOf(this Layout<View> layout, View child)
        {
            int index = layout.Children.IndexOf(child);
            return index - layout.HideCursor(index).ToInt();
        }

        public static bool Editable(this Layout<View> layout) => layout is Expression && (layout as Expression).Editable;
    }
}
