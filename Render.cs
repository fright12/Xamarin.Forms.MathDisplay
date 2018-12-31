using System;
using System.Collections.Generic;
using System.Text;

using System.Extensions;
using Crunch.Machine;

namespace Xamarin.Forms.MathDisplay
{
    public static class Render
    {
        public static Func<View> CreateLeftParenthesis = () => new Text("(") { VerticalTextAlignment = TextAlignment.Center };
        public static Func<View> CreateRightParenthesis = () => new Text(")") { VerticalTextAlignment = TextAlignment.Center };
        public static Func<View> CreateRadical = () => new Text("sqrt(") { VerticalTextAlignment = TextAlignment.Center };

        private static OrderedTrie<Operator> operations;

        static Render()
        {
            operations = new OrderedTrie<Operator>(
                new KeyValuePair<string, Operator>[2]
                {
                    new KeyValuePair<string, Operator>("sqrt", new UnaryOperator((o) => new Radical(o.Wrap(false)))),
                    new KeyValuePair<string, Operator>("_", new UnaryOperator((o) => new Expression(TextFormatting.Subscript, o.Wrap(false))))
                },
                new KeyValuePair<string, Operator>[2]
                {
                    new KeyValuePair<string, Operator>("^", new BinaryOperator(exponents)),
                    new KeyValuePair<string, Operator>("log", new Operator((o) => new Quantity(new Text("log"), o[0], o[1]), Node<object>.NextNode, (n) => n.Next?.Next))
                },
                new KeyValuePair<string, Operator>[1]
                {
                    new KeyValuePair<string, Operator>("/", new BinaryOperator((o1, o2) => new Fraction(new Expression(o1.Wrap(false)), new Expression(o2.Wrap(false)))))
                }
                );
        }

        private static object exponents(object o1, object o2)
        {
            View[] exponent = o2.Wrap(false);
            
            if (exponent.Length == 1 && exponent[0] is Fraction)
            {
                Fraction f = exponent[0] as Fraction;
                if (f.Numerator.ToString() == "(1)")
                {
                    return new Radical(new Expression(Math(f.Denominator.ToString())), o1.Wrap(false));
                }
            }

            return new Quantity(o1, new Expression(TextFormatting.Superscript, exponent));
        }

        public static View[] Math(string str)
        {
            Func<object, object> negate = (o) => new Quantity("-", o);

            print.log("rendering " + str);
            return Parse.Math(str, operations, negate).Wrap(false);
        }

        private static View[] Wrap(this object o, bool parend = true)
        {
            while (o is Quantity && (o as Quantity).First == (o as Quantity).Last && (o as Quantity).First.Value is Quantity)
            {
                o = (o as Quantity).First.Value;
            }

            View view = null;
            if (o == null)
            {
                return new View[0];
            }
            else if (o is Quantity)
            {
                List<View> list = new List<View>();
                if ((o as Quantity).Last.Value is Expression && ((o as Quantity).Last.Value as Expression).TextFormat == TextFormatting.Superscript)
                {
                    parend = false;
                }

                Node<object> node = (o as Quantity).First;
                while (node != null)
                {
                    foreach (View v in node.Value.Wrap())
                    {
                        list.Add(v);
                    }
                    node = node.Next;
                }

                if (parend)
                {
                    list.Insert(0, CreateLeftParenthesis());
                    list.Add(CreateRightParenthesis());
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
