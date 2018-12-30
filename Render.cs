using System;
using System.Collections.Generic;
using System.Text;

using System.Extensions;
using Xamarin.Forms;
using Crunch.Machine;

namespace Crunch.GraphX
{
    public static class Render
    {
        private static OrderedTrie<Operator> operations;// = new OrderedTrie<Operator>();

        static Render()
        {
            operations = new OrderedTrie<Operator>(
                new KeyValuePair<string, Operator>[1]
                {
                    new KeyValuePair<string, Operator>("^", new BinaryOperator((o1, o2) => new Quantity(o1, new Exponent(o2.Wrap(false)))))
                },
                new KeyValuePair<string, Operator>[1]
                {
                    new KeyValuePair<string, Operator>("/", new BinaryOperator((o1, o2) => new Fraction(new Expression(o1.Wrap(false)), new Expression(o2.Wrap(false)))))
                }
                );
        }

        public static View[] Math(string str)
        {
            Func<object, object> negate = (o) => new Quantity("-", o);
            /*Resolver negate = (q, n) =>
            {
                //if (n.Previous != null && n.Next != null && (n.Previous.Value is string && operations.ContainsKey(n.Previous.Value.ToString())))
                //{
                    //Parse.UnaryOperator(q, n, (o) => "-" + o.ToString());
                //}
            };*/

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
                if ((o as Quantity).Last.Value is Exponent)
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

                /*Quantity q = o as Quantity;
                if (q.First != q.Last && list[0] is Text && (list[0] as Text).Text == "(" && list.Last() is Text && (list.Last() as Text).Text == ")")
                {
                    //list.RemoveAt(0);
                    //list.RemoveAt(list.Count - 1);
                }*/

                if (parend)
                {
                    list.Insert(0, new Text("("));
                    list.Add(new Text(")"));
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
