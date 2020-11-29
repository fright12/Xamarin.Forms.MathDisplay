using System;
using System.Collections;
using System.Collections.Generic;
using System.Extensions;
using System.Linq;
using Crunch.Machine;

namespace Xamarin.Forms.MathDisplay
{
    public class Reader<T> : Crunch.Machine.Reader
        where T : IList, new()
    {
        //private static Func<LinkedListNode<object>, LinkedListNode<object>> NextOperand = (node) =>
        private static LinkedListNode<object> NextOperand(LinkedListNode<object> node)
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
        }

        public Reader(params KeyValuePair<string, Operator>[][] data) : base(data) { }
        private static Reader Instance;

        static Reader()
        {
            Instance = new Reader<T>(
                new KeyValuePair<string, Operator>[]
                {
                    new KeyValuePair<string, Operator>("√", UnaryOperator((o) => new RadicalViewModel { Root = new T(), Radicand = Wrap(o, false) })),
                    //new KeyValuePair<string, Operator>("_", UnaryOperator((o) => new ExpressionViewModel { Children = Wrap(o, false), TextFormat = TextFormatting.Subscript }))
                },
                new KeyValuePair<string, Operator>[2]
                {
                    new KeyValuePair<string, Operator>("^", BinaryOperator(exponents)),
                    new KeyValuePair<string, Operator>("log", new Operator((o) => new LinkedList<object>().Populate(new Text("log"), o[0], o[1]), NextOperand, (n) => n.Next == null ? null : NextOperand(n.Next)))
                },
                new KeyValuePair<string, Operator>[1]
                {
                    new KeyValuePair<string, Operator>("/", BinaryOperator((o1, o2) => new FractionViewModel { Children = new System.Collections.ObjectModel.ObservableCollection<MathViewModel> { ToNode(Wrap(o1, false)), ToNode(Wrap(o2, false)) } }))
                }
            );
        }

        private static Operator UnaryOperator(Func<object, object> f) => new UnaryOperator((o) => f(o), NextOperand);
        private static Operator BinaryOperator(Func<object, object, object> f) => new BinaryOperator((o1, o2) => f(o1, o2), (node) => node.Previous, NextOperand);

        private static object exponents(object o1, object o2)
        {
            T exponent = Wrap(o2, false);
            
            if (exponent.Count == 1 && exponent[0] is FractionViewModel f)
            {
                if (f.Numerator.ToString() == "(1)")
                {
                    return new RadicalViewModel
                    {
                        Root = Render(f.Denominator.ToString()),
                        Radicand = Wrap(o1, false)
                    };
                }
            }

            return new LinkedList<object>().Populate(o1, new ExpressionViewModel
            {
                //Children = exponent,
                TextFormat = TextFormatting.Superscript
            });
        }

        public static T Render(string str)
        {
            Print.Log("rendering " + str);
            return Wrap(Instance.Parse(str), false);
        }

        private static MathViewModel ToNode(T t)
        {
            MathViewModel model = new MathViewModel { Children = new System.Collections.ObjectModel.ObservableCollection<MathViewModel>() };

            foreach (object o in t)
            {
                ((IList)model.Children).Add(o);
            }

            return model;
        }

        private static Node<object> Convert(T t)
        {
            Node<object> node = new Node<object>();

            node.Children = t.Count == 0 ? new LinkedList<Node<object>>() : new LinkedList<Node<object>>(t.OfType<Node<object>>());
            node.Children.AddFirst(new Node<object>("("));
            node.Children.AddLast(new Node<object>(")"));

            return node;
        }

        public static T Wrap(object o, bool parend = true)
        {
            while (o is LinkedList<object> && (o as LinkedList<object>).First == (o as LinkedList<object>).Last && (o as LinkedList<object>).First.Value is LinkedList<object>)
            {
                o = (o as LinkedList<object>).First.Value;
            }

            MathViewModel view = null;
            if (o == null)
            {
                return new T();
            }
            else if (o is LinkedList<object> ll)
            {
                T list = new T();
                if (ll.Last.Value is ExpressionViewModel expression && expression.TextFormat == TextFormatting.Superscript)
                {
                    parend = false;
                }

                LinkedListNode<object> node = (o as LinkedList<object>).First;
                while (node != null)
                {
                    foreach (MathViewModel v in Wrap(node.Value))
                    {
                        list.Add(v);
                    }
                    node = node.Next;
                }

                if (parend)
                {
                    list.Insert(0, MathEntryViewModel.LeftParenthesis);
                    list.Add(MathEntryViewModel.RightParenthesis);
                }

                return list;
            }
            else if (o is MathViewModel)
            {
                view = o as MathViewModel;
            }
            else
            {
                string str = o.ToString();

                if (str == "-")
                {
                    view = new TextViewModel { Text = "-" };
                    //view = new Minus();
                }
                else
                {
                    T views = new T();
                    foreach (char chr in str)
                    {
                        /*char c = chr;
                        string pad = (c == '*' || c == '+' || c == '=') ? " " : "";
                        if (c == '*')
                        {
                            c = '×';
                        }*/
                        views.Add(new TextViewModel { Text = chr.ToString() });
                    }
                    return views;
                }
            }

            return new T { view };
        }
    }
}
