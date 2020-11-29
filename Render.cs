using System;
using System.Collections.Generic;
using System.Text;

using System.Extensions;
using Crunch.Machine;

namespace Xamarin.Forms.MathDisplay
{
    public class Reader : Crunch.Machine.Reader
    {
        private static Func<LinkedListNode<object>, LinkedListNode<object>> NextOperand = (node) =>
        {
            if (node.Next != null && node.Next.Value.ToString() == "-")
            {
                //Delete the negative sign
                /*node.Next.Value = null;
                node.Next = node.Next.Next;
                node.Next.Previous = node;*/
                node.List.Remove(node.Next);

                //Negate what's after
                node.Next.Value = new LinkedList<object>().Populate("-", node.Next.Value);
            }

            return node.Next;
        };

        public Reader(params KeyValuePair<string, Operator>[][] data) : base(data) { }
        private static Reader Instance;

        static Reader()
        {
            Instance = new Reader(
                new KeyValuePair<string, Operator>[2]
                {
                    new KeyValuePair<string, Operator>("sqrt", UnaryOperator((o) => new Radical(Wrap(o, false)))),
                    new KeyValuePair<string, Operator>("_", UnaryOperator((o) => new Expression(TextFormatting.Subscript, Wrap(o, false))))
                },
                new KeyValuePair<string, Operator>[2]
                {
                    new KeyValuePair<string, Operator>("^", BinaryOperator(exponents)),
                    new KeyValuePair<string, Operator>("log", new Operator((o) => new LinkedList<object>().Populate(new Text("log"), o[0], o[1]), NextOperand, (n) => n.Next == null ? null : NextOperand(n.Next)))
                },
                new KeyValuePair<string, Operator>[1]
                {
                    new KeyValuePair<string, Operator>("/", BinaryOperator((o1, o2) => new Fraction(new Expression(Wrap(o1, false)), new Expression(Wrap(o2, false)))))
                }
            );
        }

        private static Operator UnaryOperator(Func<object, object> f) => new UnaryOperator((o) => f(o), NextOperand);
        private static Operator BinaryOperator(Func<object, object, object> f) => new BinaryOperator((o1, o2) => f(o1, o2), (node) => node.Previous, NextOperand);

        private static object exponents(object o1, object o2)
        {
            View[] exponent = Wrap(o2, false);
            
            if (exponent.Length == 1 && exponent[0] is Fraction)
            {
                Fraction f = exponent[0] as Fraction;
                if (f.Numerator.ToString() == "(1)")
                {
                    return new Radical(new Expression(Render(f.Denominator.ToString())), Wrap(o1, false));
                }
            }

            return new LinkedList<object>().Populate(o1, new Expression(TextFormatting.Superscript, exponent));
        }

        public static View[] Render(string str)
        {
            Print.Log("rendering " + str);
            return Wrap(Instance.Parse(str), false);
        }

        /*public View[] Math(string str)
        {
            print.log("rendering " + str);
            return Wrap(Parse(str), false);
        }*/

        private static View[] Wrap(object o, bool parend = true)
        {
            while (o is LinkedList<object> && (o as LinkedList<object>).First == (o as LinkedList<object>).Last && (o as LinkedList<object>).First.Value is LinkedList<object>)
            {
                o = (o as LinkedList<object>).First.Value;
            }

            View view = null;
            if (o == null)
            {
                return new View[0];
            }
            else if (o is LinkedList<object>)
            {
                List<View> list = new List<View>();
                if ((o as LinkedList<object>).Last.Value is Expression && ((o as LinkedList<object>).Last.Value as Expression).TextFormat == TextFormatting.Superscript)
                {
                    parend = false;
                }

                LinkedListNode<object> node = (o as LinkedList<object>).First;
                while (node != null)
                {
                    foreach (View v in Wrap(node.Value))
                    {
                        list.Add(v);
                    }
                    node = node.Next;
                }

                if (parend)
                {
                    list.Insert(0, Text.CreateLeftParenthesis());
                    list.Add(Text.CreateRightParenthesis());
                }

                return list.ToArray();
            }
            else if (o is View)
            {
                view = o as View;
            }
            else
            {
                string str = o.ToString();

                if (str == "-")
                {
                    view = new Minus();
                }
                else
                {
                    List<View> views = new List<View>();
                    foreach (char chr in str)
                    {
                        char c = chr;
                        string pad = (c == '*' || c == '+') ? " " : "";
                        if (c == '*')
                        {
                            c = '×';
                        }
                        views.Add(new Text(pad + c + pad));
                    }
                    return views.ToArray();
                }
            }

            return new View[] { view };
        }
    }
}
