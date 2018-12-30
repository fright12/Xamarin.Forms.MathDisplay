using System;
using System.Collections.Generic;
using System.Text;

using System.Extensions;
using Xamarin.Forms;
using Xamarin.Forms.Extensions;

namespace Crunch.GraphX
{
    public static class SoftKeyboard
    {
        public static Expression Focus;
        public static CursorView Cursor { get; private set; }

        private static int index;

        static SoftKeyboard()
        {
            Cursor = new CursorView() { Color = Color.Gray };
        }

        public static void Type(string str)
        {
            View[] list;
            if (str == "(" || str == ")")
            {
                list = new View[] { new Text(str) };
            }
            else
            {
                list = Render.Math(Machine.StringClassification.Simple(str));
            }

            if (list[0] is Fraction && (list[0] as Fraction).Numerator.Children.Count == 0)
            {
                (list[0] as Fraction).Numerator.Fill(Cursor.Parent.Children, index - 1);
                index -= (list[0] as Fraction).Numerator.Children.Count;
            }
            
            Cursor.Parent.InsertRange(index, list);
            index += list.Length;

            if (list[list.Length - 1] is Fraction && (list[list.Length - 1] as Fraction).Denominator.Children.Count == 0)
            {
                (list[list.Length - 1] as Fraction).Denominator.Add(Cursor);
                index = 0;
            }
            if (list[list.Length - 1] is Exponent && (list[list.Length - 1] as Exponent).Children.Count == 0)
            {
                (list[list.Length - 1] as Exponent).Add(Cursor);
                index = 0;
            }
        }

        public static bool Delete()
        {
            if (index == 0)
            {
                int loc;
                if (Cursor.Parent.Parent is null)
                {
                    return false;
                }
                else if (Cursor.Parent.Parent is Fraction)
                {
                    Fraction f = Cursor.Parent.Parent as Fraction;
                    loc = f.Index() + 1;
                    lyse(f.Denominator, f.Parent, loc);
                    lyse(f.Numerator, f.Parent, loc);
                }
                else if (Cursor.Parent is Expression)
                {
                    loc = Cursor.Parent.Index() + 1;
                    lyse(Cursor.Parent, Cursor.Parent.Parent, loc);
                }
                else
                {
                    return false;
                }

                Cursor.Parent.RemoveAt(loc - 1);
                index = Cursor.Index();
            }
            else
            {
                index--;
                Cursor.Parent.RemoveAt(index);
            }
            
            return true;
        }

        private static void lyse(Layout<View> target, Expression destination, int index)
        {
            for (int i = target.Children.Count - 1; i >= 0; i--)
            {
                destination.Insert(index, target.Children[i]);
            }
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

        private static Expression checkIndex(int direction, Expression parent)
        {
            //Stepping once in this direction will keep me in my current expression
            if ((index + direction).IsBetween(0, parent.ChildCount()))
            {
                //Step into a new parent
                if (parent.ChildInDirection(index - (direction + 1) / 2, direction) is Expression)
                {
                    parent = parent.ChildInDirection(index - (direction + 1) / 2, direction) as Expression;
                    //I either want to be at the very beginning or the very end of the new expression,
                    //depending on which direction I'm going
                    index = parent.ChildCount() * (direction - 1) / -2;
                }
                else
                {
                    index += direction;
                }
            }
            //I'm stepping out of this parent
            else
            {
                //Try to go up
                if (parent.HasParent() && parent.Parent.Editable())
                {
                    index = parent.Index() + (direction + 1) / 2;
                    parent = parent.Parent;
                }
            }

            //I'm somewhere I shouldn't be (like a fraction); keep going
            if (parent.GetType() == typeof(Fraction))
            {
                //index -= (direction + 1) / 2;
                parent = checkIndex(direction, parent);
            }

            return parent;
        }

        public static void Fill(this Expression e, IList<View> input, int index) // where T : IMathList, new()
        {
            int imbalance = 0;
            View view = default(View);

            print.log("starting", index);
            //Grab stuff until we hit an operand
            while ((index).IsBetween(0, input.Count - 1) && !(input[index] is Text && Machine.StringClassification.IsOperand((input[index] as Text).Text.Trim()) && (input[index] as Text).Text.Length > 1 && imbalance == 0))
            {
                view = input[index];

                input.RemoveAt(index);

                if (view is Text)
                {
                    string s = (view as Text).Text;
                    if (s == "(" || s == ")")
                    {
                        if (s == "(")
                        {
                            if (imbalance == 0) break;
                            imbalance++;
                        }
                        if (s == ")") imbalance--;
                    }
                }

                e.Children.Insert(0, view);

                index--;
            }

            //e.Trim();
        }

        public static View ChildInDirection(this Expression parent, int index, int direction)
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

        public static View ChildBefore(this Expression parent, int index) => parent.ChildInDirection(index, -1);

        public static View ChildAfter(this Expression parent, int index) => parent.ChildInDirection(index, 1);

        public static bool HideCursor(this Expression parent, int index) => Cursor.Parent == parent && index >= SoftKeyboard.index;

        public static bool HideCursor(this Expression parent) => Cursor.Parent == parent;

        public static int ChildCount(this Expression e) => e.Children.Count - e.HideCursor().ToInt();

        public static int IndexOf(this Expression e, View child)
        {
            int index = e.Children.IndexOf(child);
            return index - e.HideCursor(index).ToInt();
        }

        public static bool Editable(this Layout<View> layout) => layout is Expression && (layout as Expression).Editable;
    }
}
