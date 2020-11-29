using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Extensions;
using System.Linq;
using System.Windows.Input;

namespace Xamarin.Forms.MathDisplay
{
    public class MathViewModel : BindableObject, INotifyCollectionChanged //: Node
    {
        public MathViewModel Parent { get; private set; }

        public int Count { get; private set; }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void Insert(int index, MathViewModel child)
        {
            if (CollectionChanged == null)
            {
                Parent.Insert(4 + index, child);
            }
            else
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, child, index));
            }

            child.Parent = this;
        }

        public void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Count += e.NewItems.Count;
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                Count -= e.OldItems.Count;
            }

            CollectionChanged?.Invoke(this, e);
        }
    }

    public class TextViewModel : MathViewModel
    {
        public string Text { get; set; }

        public static implicit operator TextViewModel(string str) => new TextViewModel { Text = str };

        public override string ToString() => Text;
    }

    public class CursorViewModel : MathViewModel, IEditEnumerator<object>
    {
        public object Current => Tree.Current?.Value is string str ? str[InnerIndex] : Tree.Current?.Value;

        //public Stack<IBiEnumerator> Parents { get; } = new Stack<IBiEnumerator>();
        //public IBiEnumerator Top => Parents.Peek();
        //public IEditEnumerator<object> Itr => null;// Top as IEditEnumerator<object>;
        private Node Root;

        public int Position { get; private set; }

        private IBiEnumerator<Node<object>> Tree;
        private int LocalIndex;

        public CursorViewModel(Node root)
        {
            Root = root;
            Tree = new Tree<object>.Enumerator(root);
            Tree.MoveNext();
            Reset();

            LocalIndex = 0;
            //Text = "|";
        }

        public void Add(int n, object item)
        {
            if (!(item is Node node))
            {
                throw new Exception();
            }

            Node<object> parent = Tree.Current.Parent;
            int index = LocalIndex + n + (n < 0 ? 1 : 0);
            int sign = Math.Sign(n);

            if (index < 0 || index > parent.ChildrenList.Count || (Tree.Current.Value is Parenthesis parenthesis && parenthesis.Opening == (sign == -1)))
            {
                Move(sign);

                parent = Tree.Current.Parent;
                index = parent.ChildrenList.IndexOf(Tree.Current) + (sign == -1 ? 1 : 0);

                Move(-sign);
            }

            parent.ChildrenList.Insert(index, node);
            //node.Parent = parent;

            if (node.Parent == Tree.Current.Parent && index <= LocalIndex)
            {
                Tree.MoveNext();
                LocalIndex -= n;
            }
        }

        public bool Remove(int n)
        {
            int sign = Math.Sign(n);

            Move(sign);
            Node<object> node = Tree.Current;
            Move(-sign);

            node.Parent.ChildrenList.Remove(node);

            if (node.Parent == Tree.Current.Parent && n < 0)
            {
                Tree.MovePrev();
                LocalIndex += n;
            }

            return true;
        }

        public bool Delete()
        {
            Tree.Current.Parent.ChildrenList.RemoveAt(LocalIndex-- - 1);
            Tree.MovePrev();

            return true;
        }

        public bool MoveNext() => Move(1);

        private int InnerIndex;
        private bool InfixParent;

        public bool Move(int n)
        {
            int sign = Math.Sign(n);
            Node<object> parent = Tree.Current.Parent;

            LocalIndex += sign;

            if (Tree.Current?.Value is string str)
            {
                InnerIndex = sign == 1 ? 0 : str.Length - 1;
            }

            if (!(Tree.Current?.Value is string value) || (InnerIndex += sign) < 0 || InnerIndex >= value.Length)
            {
                do
                {
                    if (!Tree.Move(sign))
                    {
                        return false;
                    }
                }
                while (Tree.Current.Value == null || Tree.Current.Value is OperatorViewModel1);

                if (Tree.Current.Parent.Value is OperatorViewModel1 op && Tree.Current.Parent.ChildrenList[op.InfixOrder + (sign == 1 ? 0 : -1)] == Tree.Current)
                {

                }
            }

            if (Tree.Current.Parent != parent)
            {
                if (Tree.Current.Value is Parenthesis parenthesis)
                {
                    LocalIndex = parenthesis.Opening ? 0 : parenthesis.Expression.ChildrenList.Count - 1;

#if DEBUG
                    if (parenthesis.Expression != Tree.Current.Parent)
                    {
                        throw new Exception();
                    }
#endif
                }
                else
                {
                    LocalIndex = Tree.Current.Parent.ChildrenList.IndexOf(Tree.Current);
                }
            }

            return true;
        }

        public void Reset()
        {

        }

        public void End()
        {
            Tree.End();
        }

        public void Dispose()
        {

        }
    }

    public abstract class MathLayoutViewModel : MathViewModel
    {
        public virtual IList InputContinuation => null;
    }

    public class Parenthesis
    {
        public Node<object> Expression { get; private set; }
        public bool Opening { get; private set; }

        public Parenthesis(Node<object> expression, bool opening)
        {
            Expression = expression;
            Opening = opening;
        }

        public override string ToString() => Opening ? "(" : ")";
    }

    public abstract class OperatorViewModel : MathLayoutViewModel, IBiEnumerable<object>
    {
        public abstract IEnumerable GetOperands();

        public IBiEnumerator<object> GetEnumerator()
        {
            //new ListBiEnumerator<object>(System.Linq.Enumerable.ToList(GetOperands().OfType<object>()));

            List<object> list = new List<object>();

            foreach (object operand in GetOperands())
            {
                //list.Add(new Parenthesis(operand as IEnumerable, true));
                list.Add(operand);
                //list.Add(new Parenthesis(operand as IEnumerable, false));
            }

            return new ListBiEnumerator<object>(list);
        }

        IBiEnumerator IBiEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<object> IEnumerable<object>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public class Expression<T> : IEditEnumerable<T>
        {
            private T Content;

            public Expression(T content) => Content = content;

            public IEditEnumerator<T> GetEnumerator() => new Enumerator<T>(this);

            IEditEnumerator IEditEnumerable.GetEnumerator() => GetEnumerator();

            IBiEnumerator<T> IBiEnumerable<T>.GetEnumerator() => GetEnumerator();

            IBiEnumerator IBiEnumerable.GetEnumerator() => GetEnumerator();

            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public struct Enumerator<T> : IEditEnumerator<T>
        {
            public T Current => Itr.Current;
            object IEnumerator.Current => ((IEnumerator)Itr).Current;

            private IEditEnumerator<T> Itr;
            private int InvalidDirection;

            public Enumerator(IEditEnumerable<T> expression)
            {
                Itr = expression.GetEditEnumerator();
                InvalidDirection = -1;
            }

            public void Add(int n, T item)
            {
                Itr.Add(n, item);
            }

            public void Dispose()
            {
                Itr.Dispose();
            }

            public void End()
            {
                Itr.End();
            }

            public bool Move(int n)
            {
                int sign = Math.Sign(n);
                return true;
                if (sign == InvalidDirection)
                {
                    return false;
                }
                else if (InvalidDirection != 0)
                {
                    InvalidDirection = 0;
                    n -= sign;
                }

                if (!Itr.Move(n - sign))
                {
                    InvalidDirection = sign;
                    return false;
                }
                else
                {

                }

                for (int i = 0; i < Math.Abs(n); i++)
                {
                    if (InvalidDirection != 0)
                    {
                        InvalidDirection += sign;
                    }
                    else
                    {
                        Itr.Move(sign);
                    }
                }

                if (sign == InvalidDirection)
                {
                    return false;
                }
                else if (sign == -InvalidDirection)
                {

                }

                if (!Itr.Move(n))
                {

                }
            }

            public bool MoveNext() => Move(1);

            public bool Remove(int n)
            {
                return Itr.Remove(n);
            }

            public void Reset()
            {
                Itr.Reset();
            }
        }
    }

    public class ExpressionViewModel : MathViewModel//, IEditEnumerable<object>
    {
        public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(Expression), string.Empty, propertyChanged: (bindable, oldValue, newValue) => ((ExpressionViewModel)bindable).OnTextChanged((string)oldValue, (string)newValue));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public TextFormatting TextFormat { get; set; }

        //public IEnumerable Children { get; set; }

        //public override IList InputContinuation => TextFormat != TextFormatting.Subscript && Children.Count == 0 ? Children : null;

        protected virtual void OnTextChanged(string oldText, string newText)
        {
            //Children = Reader<List<object>>.Render(newText);
            //OnPropertyChanged(nameof(Children));
        }

        /*public IEditEnumerator<object> GetEnumerator() => (Children as IList<object>).GetEditEnumerator();

        IEditEnumerator IEditEnumerable.GetEnumerator() => GetEnumerator();

        IBiEnumerator<object> IBiEnumerable<object>.GetEnumerator() => GetEnumerator();

        IBiEnumerator IBiEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<object> IEnumerable<object>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();*/
    }

    public class RadicalViewModel : OperatorViewModel
    {
        public IList Root { get; set; }
        public IList Radicand { get; set; }

        public override IList InputContinuation => Radicand;

        public override IEnumerable GetOperands()
        {
            yield return Root;
            yield return Radicand;
        }
    }

    public class Node : Node<object>
    {
        public Node()
        {
            Value = this;
        }
    }

    public class Node<T> : BindableObject
    {
        public IList<Node<T>> ChildrenList { get; private set; }
        public LinkedList<Node<T>> Children;
        public Node<T> Parent { get; }
        public LinkedListNode<Node<T>> ParentNode { get; set; }

        public T Value { get; set; }

        public Node() { }

        public Node(T value) => Value = value;

        public void SetChildren(IEnumerable value)
        {
            if (value is IList<Node<T>> list)
            {
                ChildrenList = list;

                foreach (Node<T> node in list)
                {
                    if (node == null)
                    {
                        continue;
                    }

                    //node.Parent = this;
                }
            }
        }

        public override string ToString() => Value is Node<T> ? base.ToString() : Value.ToString();
    }

    public class OperatorViewModel1 : MathViewModel
    {
        public int Arity { get; private set; }
        public int InfixOrder { get; set; }

        public OperatorViewModel1(int arity) => Arity = arity;
    }

    public class FractionViewModel : MathViewModel
    {
        public IEnumerable Numerator => ((MathViewModel)((IList)Children)[0]).Children;

        public IEnumerable Denominator => ((MathViewModel)((IList)Children)[1]).Children;

        public FractionViewModel()//IEnumerable numerator, IEnumerable denominator) : base(2)
        {
            

            /*SetChildren(new List<Node<object>>
            {
                numerator,
                denominator
            });*/

            //Children = new LinkedList<Node<object>>();

            /*Children.AddLast(numerator);
            Children.AddLast(new Node<object>("/"));
            Children.AddLast(denominator);*/

            /*for (LinkedListNode<Node<object>> node = Children.First; node != null; node = node.Next)
            {
                if (node.Value.Children == null)
                {
                    continue;
                }

                foreach (Node<object> child in node.Value.Children)
                {
                    child.ParentNode = node;
                }
            }*/

            //numerator.Value = Numerator;
            //denominator.Value = Denominator;

            //InfixOrder = 1;
        }

        //public override IList InputContinuation => Denominator;

        /*public override IEnumerable GetOperands()
        {
            yield return Numerator;
            yield return Denominator;
        }*/
    }

    public class Tree<T> : IBiEnumerable
    {
        private Node<T> Root;

        public IBiEnumerator GetEnumerator() => new Enumerator(Root);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static Node<T> Prev(Node<T> Node)
        {
            if (Node.Parent == null)
            {
                return null;
            }

            int index = Node.Parent.ChildrenList.IndexOf(Node);
            Node = Node.Parent;

            //Print.Log("here", Node == null ? -1 : Index, Node?.Children?.Count);
            if (--index >= 0)
            {
                while ((Node = Node.ChildrenList[index])?.ChildrenList != null && Node.ChildrenList.Count > 0)
                {
                    index = Node.ChildrenList.Count - 1;
                }
            }

            return Node;
        }

        public static Node<T> Next(Node<T> Node)
        {
            int index;

            if (Node.ChildrenList != null && Node.ChildrenList.Count > 0)
            {
                index = 0;
            }
            else
            {
                //Print.Log("moving up", Node, Node?.Parent, Current, Index);
                do
                {
                    if (Node.Parent == null)
                    {
                        return null;
                    }

                    index = Node.Parent.ChildrenList.IndexOf(Node);
                    Node = Node.Parent;
                }
                while (++index >= Node.ChildrenList.Count);
            }

            return Node.ChildrenList[index];
        }

        public class Enumerator : IBiEnumerator<Node<T>>
        {
            public Node<T> Current => Node;// == null ? default : Node.Value;
            object IEnumerator.Current => Current;

            private Node<T> Root;
            private Node<T> Node;
            private List<int> Indices;
            private int Index
            {
                get => Indices[Indices.Count - 1];
                set => Indices[Indices.Count - 1] = value;
            }

            public Enumerator(Node<T> root)
            {
                Root = root;
                Node = null;
                Indices = new List<int>();

                Reset();
            }

            public void Dispose()
            {

            }

            public void End()
            {
                Node = null;
            }

            private Node<T> LastParent;

            public bool Move(int n)
            {
                int sign = Math.Sign(n);

                /*Node<T> parent = Node?.Parent;
                if (parent != LastParent)
                {
                    LastParent = parent;

                    if (LastParent != null && Indices.Count > 0)
                    {
                        Index = Node.Parent.Children.IndexOf(Node);
                    }
                }*/

                if (sign == -1)
                {
                    return MovePrev();
                }

                if (Node == null || (Node.ChildrenList != null && Node.ChildrenList.Count > 0))
                {
                    //Node = Node?.Children[0] ?? Root;
                    Indices.Add(0);
                }
                else
                {
                    //Print.Log("moving up", Node, Node?.Parent, Current, Index);
                    while ((Node = Node.Parent) != null && ++Index >= Node.ChildrenList.Count)
                    {
                        Indices.RemoveAt(Indices.Count - 1);
                    }

                    if (Node == null)
                    {
                        return false;
                    }
                }

                Node = Node?.ChildrenList[Index] ?? Root;

                return true;
            }

            public bool MoveNext() => Move(1);

            private bool MovePrev()
            {
                if (Node != null)
                {
                    Node = Node.Parent;

                    if (Node == null)
                    {
                        return false;
                    }
                }
                //Print.Log("here", Node == null ? -1 : Index, Node?.Children?.Count);
                if (Node != null && --Index < 0)
                {
                    Indices.RemoveAt(Indices.Count - 1);
                }
                else
                {
                    while ((Node = Node?.ChildrenList[Index] ?? Root)?.ChildrenList != null && Node.ChildrenList.Count > 0)
                    {
                        Indices.Add(Node.ChildrenList.Count - 1);
                        //Node = Node.Children[Node.Children.Count - 1];
                    }
                }

                return true;
            }

            public void Reset()
            {
                Indices.Clear();
                Node = null;
            }
        }
    }

    public class TreeVisitor<T> //: IEnumerator<T>
    {
        //public T Current => Node.Value.Value;
        //object IEnumerator.Current => Current;

        private LinkedListNode<Node<T>> Root;
        private (LinkedListNode<Node<T>>, bool) LastMove;
        //public LinkedListNode<Node<T>> Node;

        public TreeVisitor(LinkedListNode<Node<T>> root)
        {
            Root = root;
            LastMove = (null, false);
        }

        public LinkedListNode<Node<T>> After(LinkedListNode<Node<T>> node) => Move(node, true);
        public LinkedListNode<Node<T>> Before(LinkedListNode<Node<T>> node) => Move(node, false);

        private LinkedListNode<Node<T>> Move(LinkedListNode<Node<T>> node, bool forward)
        {
            LinkedListNode<Node<T>> current = node;

            node = Visit(current, forward);
            LastMove = (current, forward);

            if (node == null)
            {
                if (current == Root)
                {
                    return null;
                }

                node = (forward ? current.Next : current.Previous) ?? current.Value.ParentNode;
            }

            return node == Root ? null : node;// node != null && node != Root;
        }

        protected virtual LinkedListNode<Node<T>> Visit(LinkedListNode<Node<T>> node, bool forward)
        {
            LinkedListNode<Node<T>> next = forward ? VisitFirstChild(node) : VisitLastChild(node);
            return forward == LastMove.Item2 && next?.Value.ParentNode == LastMove.Item1?.Value.ParentNode ? null : next;
        }

        protected LinkedListNode<Node<T>> VisitFirstChild(LinkedListNode<Node<T>> node) => node.Value.Children?.First;
        protected LinkedListNode<Node<T>> VisitLastChild(LinkedListNode<Node<T>> node) => node.Value.Children?.Last;
    }

    public class MathEntryViewModel : ExpressionViewModel//, IEditEnumerable<object>
    {
        public enum CursorKey { Left, Right, Up, Down, End, Home };

        /*public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(MathEntryViewModel), string.Empty, propertyChanged: TextPropertyChanged);

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }*/

        //public event EventHandler<ChangedEventArgs<int>> CursorMoved;
        //public event EventHandler<CharCollectionChangedEventArgs> RichTextChanged;

        public MathViewModel this[int index] => List[index];

        public static readonly BindableProperty CursorPositionProperty = BindableProperty.Create(nameof(CursorPosition), typeof(int), typeof(MathEntryViewModel), 0, validateValue: (bindable, value) =>
        {
            int position = (int)value;
            return position >= 0 && position <= ((MathEntryViewModel)bindable).Count;
        }, coerceValue: (bindable, value) =>
        {
            MathEntryViewModel entry = (MathEntryViewModel)bindable;
            return entry.Count - (int)entry.CoerceCursorPosition(entry.Count - (int)value);
        });

        public int Count => List.Count;
        public int CursorPosition
        {
            get => Count - (int)GetValue(CursorPositionProperty);
            set => SetValue(CursorPositionProperty, Count - value);
        }

        public static readonly BindableProperty FocusedProperty = BindableProperty.Create(nameof(Focused), typeof(bool), typeof(MathEntryViewModel), false);

        public bool Focused
        {
            get => (bool)GetValue(FocusedProperty);
            set => SetValue(FocusedProperty, value);
        }

        public ICommand InputCommand { get; set; }
        public ICommand BackspaceCommand { get; set; }
        public ICommand MoveCursorCommand { get; set; }

        private readonly TreeVisitor<object> Visitor;
        //private readonly LinkedListNode<Node<object>> Root;
        private CursorViewModel Cursor;
        private List<MathViewModel> List;
        private List<int> Indices;
        private int LocalIndex
        {
            get => Indices.Last();
            set => Indices[Indices.Count - 1] = value;
        }
        private MathViewModel Current;

        public MathEntryViewModel()
        {
            List = new List<MathViewModel>();
            Cursor = new CursorViewModel(new Node { Value = this });
            Indices = new List<int> { -1 };
            Current = this;

            InputCommand = new Command<string>(value =>
            {
                /*MainPage page = SoftKeyboard.Cursor.Parent<MainPage>();
                if (page == null)
                {
                    App.Current.Home.AddCalculation();
                }*/

                //Input(value);
                Type(value);
                //Text = Text.Insert(CursorPosition, value);
            });

            BackspaceCommand = new Command(() => Delete());
            MoveCursorCommand = new Command<CursorKey>(MoveCursor);
#if false
            {
                if (value == CursorKey.Up)
                {
                    if (!SoftKeyboard.Up())
                    {
                        //SoftKeyboard.Cursor.Parent<Calculation>()?.Up();
                    }
                }
                else if (value == CursorKey.Down)
                {
                    if (!SoftKeyboard.Down())
                    {
                        //SoftKeyboard.Cursor.Parent<Calculation>()?.Down();
                    }
                }
                else if (value == CursorKey.Right)
                {
                    Cursor.MoveNext();
                    CursorPosition++;
                }
                else if (value == CursorKey.Left)
                {
                    Cursor.MovePrev();
                    CursorPosition--;
                }
            });
#endif
        }

        public void Delete()
        {

        }

        public void MoveCursor(CursorKey key)
        {
            int sign;
            if (key == CursorKey.Left)
            {
                sign = -1;
            }
            else if (key == CursorKey.Right)
            {
                sign = 1;
            }
            else
            {
                return;
            }

            CursorPosition += sign;
        }

        private object CoerceCursorPosition(int position)
        {
            /*Current?.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, Cursor, LocalIndex + 1));

            Section current = Current;
            int sign = Math.Sign(position - CursorPosition);

            for (int i = CursorPosition; i != position; i += sign)
            {
                MathViewModel math = List[i - 1];
                LocalIndex += sign;

                if (LocalIndex < 0 || LocalIndex > current.Count)
                {
                    current = math.Children as Section;
                    Indices.RemoveAt(Indices.Count - 1);
                }
                else if (math.Children != null)
                {
                    Indices.Add(sign == 1 ? 0 : (math.Children as Section)?.Count ?? (math.Children as IList).Count);
                }
            }

            Current?.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, Cursor, LocalIndex + 1));*/

            return position;
        }

        public static readonly MathViewModel LeftParenthesis = new TextViewModel { Text = "(" };
        public static readonly MathViewModel RightParenthesis = new TextViewModel { Text = ")" };

        public void Type(string str)
        {
            if (str.Length == 0)
            {
                return;
            }

            //Suround previous thing with parentheses if it's an exponent or a fraction

            IList<MathViewModel> list;

            if (str == "(")
            {
                list = new List<MathViewModel> { LeftParenthesis };
            }
            else if (str == ")")
            {
                list = new List<MathViewModel> { RightParenthesis };
            }
            else
            {
                list = Reader<ObservableCollection<MathViewModel>>.Render(str);// Crunch.Machine.StringClassification.Simple(str));

                /*if (node.Value.Children != null)
                {
                    foreach (Node<object> child in node.Value.Children)
                    {
                        child.ParentNode = node;
                    }
                }*/

                // Populate numerator of fraction
            }

            // Insert into structure
            Insert(str, list[0]);

            //RichTextChanged?.Invoke(this, new CharCollectionChangedEventArgs { Action = NotifyCollectionChangedAction.Add, Tree = null, Text = str, Index = index });

            /*if (node.Value.ChildrenList != null && node.Value.ChildrenList.Count > 0 && node.Value.ChildrenList.Last().Value == null)
            {
                MoveCursor(CursorKey.Left);
            }*/
        }

        public class NodeWrapper : INode
        {
            private object SomeNode;

            public string Classification => throw new NotImplementedException();

            public IEnumerator<INode> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        public interface INode : IEnumerable<INode>
        {
            string Classification { get; }
        }

        public class ParentNode
        {
            public event EventHandler<NotifyCollectionChangedEventArgs> ChildrenChangedEvent;

            public void OnChildrenChanged(NotifyCollectionChangedEventArgs e)
            {
                ChildrenChangedEvent?.Invoke(this, e);
            }
        }

        public class CursorTest : BindableObject
        {
            public event EventHandler<NotifyCollectionChangedEventArgs> ChildrenChangedEvent;

            public static BindableProperty ParentProperty;

            private List<int> Trace;
            private int Index
            {
                get => Trace.Last();
                set => Trace[Trace.Count - 1] = value;
            }

            private Stack<ParentNode> Parents;

            public INode Parent
            {
                get => (INode)GetValue(ParentProperty);
                set => SetValue(ParentProperty, value);
            }

            public void Advance(int count)
            {
                int depthChange = 0;
                int sign = Math.Sign(count);

                for (int i = 0; i < depthChange; i++)
                {
                    // Going further down the tree
                    if (depthChange > 0)
                    {
                        Trace.Add(sign > 0 ? 0 : -1);
                        Parents.Push(new ParentNode());
                    }
                    // Heading back up the tree
                    else if (depthChange < 0)
                    {
                        Trace.RemoveAt(Trace.Count - 1);
                    }
                }

                Index++;
            }

            public void Insert(string text, MathViewModel math = null)
            {
                if (ChildrenChangedEvent == null)
                {
                    return;
                }

                NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, math, Index);
                Parents.Peek().OnChildrenChanged(e);

                Delegate[] listeners = ChildrenChangedEvent.GetInvocationList();
                listeners[listeners.Length - 1].DynamicInvoke(this, new EventArgs());
            }
        }

        public void RemoveAt(int index)
        {
            MathViewModel math = List[index];
            //int localIndex = index == CursorPosition ? LocalIndex : LocalIndexOf(math);

            

            if (index - 1 >= CursorPosition)
            {
                CursorPosition--;
            }
        }

        public void Insert(string text, MathViewModel math = null)
        {

        }

        private List<Cursor> Cursors = new List<Cursor>();

        private class Cursor : IDisposable
        {
            public int Index
            {
                get => Entry.Count - IndexFromEnd;
                set => IndexFromEnd = Entry.Count - value;
            }

            private int IndexFromEnd;

            private class Test
            {
                public MathViewModel Parent;
                public int LocalIndex;
            }

            private List<Test> Trace;
            private int LocalIndex
            {
                get => Trace.Last().LocalIndex;
                set => Trace[Trace.Count - 1].LocalIndex = value;
            }

            private MathViewModel Parent => Trace.Last().Parent;

            private MathEntryViewModel Entry;

            public Cursor(MathEntryViewModel entry)
            {
                Entry = entry;

                Entry.Cursors.Add(this);
                Trace.Add(new Test { LocalIndex = -1, Parent = Entry });
            }

            public bool Advance(int count)
            {
                int sign = Math.Sign(count);

                for (int i = 0; i < count; i++)
                {
                    LocalIndex += sign;

                    if (LocalIndex < 0 || LocalIndex > Parent.Count)
                    {
                        Trace.RemoveAt(Trace.Count - 1);
                    }
                    else if (false)
                    {

                    }
                }

                return true;
            }

            public void Insert(string text, MathViewModel math = null)
            {
                Entry.List.Insert(Index, math);
                Parent.Insert(LocalIndex, math);
            }

            public void Dispose()
            {
                Entry.Cursors.Remove(this);
            }
        }

        public void Insert(int index, string text, MathViewModel math = null)
        {
            if (index < 0 || index > Count)
            {
                throw new IndexOutOfRangeException();
            }

            IEnumerable tree = new List<MathViewModel> { math };

            if (math == null)
            {
                // parse
                throw new NotImplementedException();
            }

            Text = Text.Insert(index, text);
            List.InsertRange(index, text.Select(c => new TextViewModel { Text = c.ToString() }));

            MathViewModel parent = index == Count ? this : List[index].Parent;
            parent.Insert(LocalIndex, math);
        }

        public void AddBefore(MathViewModel source, MathViewModel value) => AddRelative(source, value, true);
        public void AddAfter(MathViewModel source, MathViewModel value) => AddRelative(source, value, false);

        private void AddRelative(MathViewModel source, MathViewModel value, bool before)
        {

        }

        private int LocalIndexOf(MathViewModel math)
        {
            if (math.Parent.Children is IList list)
            {
                return list.IndexOf(math);
            }
            else
            {
                IEnumerator enumerator = math.Children.GetEditEnumerator();
                int i = 0;
                for (; enumerator.MoveNext(); i++)
                {
                    if (enumerator.Current == math)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        private class Section : INotifyCollectionChanged, IEnumerable
        {
            public int Count { get; private set; }

            private MathEntryViewModel Entry;
            private MathViewModel Value;
            private int Index;

            public Section(MathEntryViewModel entry, MathViewModel value, int index)
            {
                Entry = entry;
                Value = value;
                Index = index;
            }

            public event NotifyCollectionChangedEventHandler CollectionChanged;

            public void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    Count += e.NewItems.Count;
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    Count -= e.OldItems.Count;
                }

                CollectionChanged?.Invoke(this, e);
            }

            public IEnumerator GetEnumerator()
            {
                if (Count > 0)
                {
                    MathViewModel parent = Value?.Parent ?? Entry;

                    foreach (MathViewModel math in Entry.List)
                    {
                        if (math.Parent == parent)
                        {
                            yield return math;
                        }
                    }
                }
            }
        }
    }

    public static class MathViewModelExtensions
    {
        public static void Trim(this IList list)
        {
            while (list.Count > 0 && list[list.Count - 1].ToString().Trim() == ")" && list[0].ToString().Trim() == "(")
            {
                list.RemoveAt(list.Count - 1);
                list.RemoveAt(0);
            }
        }

        public static int BeginningOfPreviousMathObject(ref LinkedListNode<Node<object>> node)
        {
            int imbalance = 0;
            int count = 0;

            //Grab stuff until we hit an operand
            while ((node = node.Previous) != null && !(Crunch.Machine.StringClassification.IsOperator(node.Value.Value.ToString().Trim()) && node.Value.Value.ToString() != "-" && imbalance == 0))
            {
                string s = node.Value.Value.ToString().Trim();

                if (s == "(" || s == ")")
                {
                    if (s == "(")
                    {
                        if (imbalance == 0) break;
                        imbalance++;
                    }
                    if (s == ")") imbalance--;
                }

                count++;
            }

            node = node.Next;
            return count;
        }
    }
}