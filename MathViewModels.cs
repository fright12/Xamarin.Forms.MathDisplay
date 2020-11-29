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
    public abstract class MathViewModel : Node
    {

    }

    public class TextViewModel : MathViewModel
    {
        public string Text { get; set; }

        public static implicit operator TextViewModel(string str) => new TextViewModel { Text = str };

        public override string ToString() => Text;
    }

    public class Enumerator : IEnumerator
    {
        public object Current => Top.Current;

        private IEnumerator Top => Parents.Peek();

        private Stack<IEnumerator> Parents;
        private IEnumerable Root;

        public Enumerator(IEnumerable enumerable)
        {
            Parents = new Stack<IEnumerator>();
            Parents.Push((Root = enumerable).GetEnumerator());
        }

        public bool MoveNext()
        {
            while (true)
            {
                if (!Top.MoveNext())
                {
                    if (Parents.Count == 1)
                    {
                        return false;
                    }

                    Parents.Pop();
                }
                else if (Top.Current is IEnumerable enumerable)
                {
                    Parents.Push(enumerable.GetEnumerator());
                }
                else
                {
                    break;
                }
            }

            return true;
        }

        public void Reset()
        {
            Parents.Clear();
            Parents.Push(Root.GetEnumerator());
        }
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

    public class ExpressionViewModel : BindableObject, IEditEnumerable<object>
    {
        public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(Expression), string.Empty, propertyChanged: (bindable, oldValue, newValue) => ((ExpressionViewModel)bindable).OnTextChanged((string)oldValue, (string)newValue));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public TextFormatting TextFormat { get; set; }

        public IEnumerable Children { get; set; }

        //public override IList InputContinuation => TextFormat != TextFormatting.Subscript && Children.Count == 0 ? Children : null;

        protected virtual void OnTextChanged(string oldText, string newText)
        {
            //Children = Reader<List<object>>.Render(newText);
            OnPropertyChanged(nameof(Children));
        }

        public IEditEnumerator<object> GetEnumerator() => (Children as IList<object>).GetEditEnumerator();

        IEditEnumerator IEditEnumerable.GetEnumerator() => GetEnumerator();

        IBiEnumerator<object> IBiEnumerable<object>.GetEnumerator() => GetEnumerator();

        IBiEnumerator IBiEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<object> IEnumerable<object>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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

    public class FractionViewModel : OperatorViewModel1
    {
        public IEnumerable Numerator { get; set; }// => (IList)ChildrenList[0]?.ChildrenList;

        public IEnumerable Denominator { get; set; }// => (IList)ChildrenList[1]?.ChildrenList;

        public FractionViewModel(Node<object> numerator, Node<object> denominator) : base(2)
        {
            /*SetChildren(new List<Node<object>>
            {
                numerator,
                denominator
            });*/

            Children = new LinkedList<Node<object>>();

            Children.AddLast(numerator);
            Children.AddLast(new Node<object>("/"));
            Children.AddLast(denominator);

            for (LinkedListNode<Node<object>> node = Children.First; node != null; node = node.Next)
            {
                if (node.Value.Children == null)
                {
                    continue;
                }

                foreach (Node<object> child in node.Value.Children)
                {
                    child.ParentNode = node;
                }
            }

            numerator.Value = Numerator = new ObservableLinkedList<object>(Children.First);
            denominator.Value = Denominator = new ObservableLinkedList<object>(Children.Last);

            InfixOrder = 1;
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

    public class ObservableLinkedList<T> : MathViewModel, INotifyCollectionChanged, IEnumerable
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public int Count { get; private set; }

        private Itr Visitor;
        private LinkedListNode<Node<T>> Root;
        private (LinkedListNode<Node<T>>, int) Context;

        public ObservableLinkedList(LinkedListNode<Node<T>> root)
        {
            Root = root;

            IEnumerator itr = GetEnumerator();
            while (itr.MoveNext())
            {
                Count++;
            }

            Visitor = new Itr(this);
            Context = (Root, -1);
        }

        private int IndexOf(LinkedListNode<Node<T>> node)
        {
            if (Context.Item1 == node)
            {
                return Context.Item2;
            }

            int index = 0;
            foreach (LinkedListNode<Node<T>> leaf in LeafNodes())
            {
                if (leaf == node)
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        public void AddBefore(LinkedListNode<Node<T>> node, T value) => AddValue(node, value, true);
        public void AddAfter(LinkedListNode<Node<T>> node, T value) => AddValue(node, value, false);
        private void AddValue(LinkedListNode<Node<T>> node, T value, bool before) => Add(node, new LinkedListNode<Node<T>>(new Node<T>(value)), before);

        public void AddBefore(LinkedListNode<Node<T>> node, LinkedListNode<Node<T>> newNode) => Add(node, newNode, true);
        public void AddAfter(LinkedListNode<Node<T>> node, LinkedListNode<Node<T>> newNode) => Add(node, newNode, false);

        private void Add(LinkedListNode<Node<T>> node, LinkedListNode<Node<T>> newNode, bool before)
        {
            int index = IndexOf(node);

            if (index == -1)
            {
                throw new InvalidOperationException();
            }

            if (newNode.Value.Value is MathViewModel)
            {
                Insert(index + (before ? 0 : 1), newNode);
                return;
            }
            else if (before)
            {
                node.List.AddBefore(node, newNode);
                //Context = (node, index + 1);
            }
            else
            {
                node.List.AddAfter(node, newNode);
                //index++;
            }
        }

        private LinkedListNode<Node<T>> this[int index]
        {
            get
            {
                LinkedListNode<Node<T>> node = Context.Item1;
                int sign = Math.Sign(index - Context.Item2);

                for (int i = 0; i < Math.Abs(index - Context.Item2); i++)
                {
                    do
                    {
                        node = sign == -1 ? Visitor.Before(node) : Visitor.After(node);
                    }
                    while (node.Value.Children != null || !(node.Value.Value is MathViewModel));
                }

                Context = (node, index);
                return node;
            }
        }

        public void Insert(int index, T value) => Insert(index, Convert(value));

        public void Insert(int index, LinkedListNode<Node<T>> newNode) //=> Insert(index, newNode, Add);

        //public void Insert<T1>(int index, T1 value, Action<LinkedListNode<Node<T>>, T1, bool> adder)
        {
            //adder(this[Math.Max(0, Math.Min(Count - 1, index))], value, index != Count);

            if (index < 0 || index > Count)
            {
                throw new IndexOutOfRangeException();
            }

            if (index == Count)
            {
                if (Root.Value.Children == null)
                {
                    Root.Value.Children = new LinkedList<Node<T>>();
                }

                // Expression
                if (Count == 0 && Root.Value.Children.Count == 2)
                {
                    Root.Value.Children.AddBefore(Root.Value.Children.Last, newNode);
                }
                else
                {
                    Root.Value.Children.AddLast(newNode);
                }

                Context = (newNode, index);
                newNode.Value.ParentNode = Root;
            }
            else
            {
                LinkedListNode<Node<T>> node = this[index];
                node.List.AddBefore(node, newNode);

                Context = (node, index + 1);
                newNode.Value.ParentNode = node.Value.ParentNode;
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newNode.Value.Value, index));
        }

        private LinkedListNode<Node<T>> Convert(T value) => new LinkedListNode<Node<T>>(new Node<T>(value));

        public void RemoveAt(int index)
        {
            if (index < 0 || index > Count - 1)
            {
                throw new IndexOutOfRangeException();
            }

            Remove(this[index]);
        }

        public bool Remove(LinkedListNode<Node<T>> node)
        {
            int index = node == Context.Item1 ? Context.Item2 : IndexOf(node);
            Context = (Visitor.Before(node) ?? Visitor.After(node), Math.Max(0, index - 1));

            node.List.Remove(node);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, node.Value.Value, index));

            return true;
        }

        public virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
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

        public IEnumerator GetEnumerator() => LeafNodes().Select(node => node.Value.Value).GetEnumerator();

        private IEnumerable<LinkedListNode<Node<T>>> LeafNodes()
        {
            Itr itr = new Itr(this);
            LinkedListNode<Node<T>> node = Root;
            //List<object> test = new List<object>();
            while (node != null)
            {
                if (node != Root && node.Value.Value is MathViewModel)
                {
                    //test.Add(itr.Node.Value.Value);
                    yield return node;
                }

                node = itr.After(node);
            }

            //return test.GetEnumerator();
        }

        public class Itr : TreeVisitor<T>
        {
            //public LinkedListNode<Node<T>> Node;
            private ObservableLinkedList<T> List;
            private int LocalIndex;

            public Itr(ObservableLinkedList<T> list) : base(list.Root)
            {
                List = list;
                //Current = list.Root;
            }

            protected override LinkedListNode<Node<T>> Visit(LinkedListNode<Node<T>> node, bool forward)
            {
                if (node.Value.Value is ObservableLinkedList<T> list && list != List)
                {
                    return null;
                }

                return base.Visit(node, forward);
            }
        }

        public class Enumerator
        {
            private Stack<Test<T>> Stack;

            private ObservableLinkedList<T> List => Stack.Peek().List;
            private int LocalIndex
            {
                get => Stack.Peek().Index;
                set => Stack.Peek().Index = value;
            }

            public LinkedListNode<Node<T>> Node { get; private set; }

            public Enumerator(ObservableLinkedList<T> list)
            {
                Stack = new Stack<Test<T>>();
                Stack.Push(new Test<T>(list, 0));

                //Node = MathEntryViewModel.Next(list.Root);
            }

            public T Current => Node.Value.Value;

            public void Insert(T value)
            {
                Node.List.AddBefore(Node, new Node<T>(value));
                List.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value, LocalIndex++));
            }

            public bool Delete()
            {
                LinkedListNode<Node<T>> prev = Node.Previous;

                List.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, Node.Value.Value, LocalIndex--));
                Node.List.Remove(Node);

                Node = prev;

                return true;
            }

            public bool Move(int n)
            {
                int sign = Math.Sign(n);

                if (sign == 0)
                {
                    return true;
                }

                for (int i = 0; i < Math.Abs(n); i++)
                {
                    do
                    {
                        LocalIndex += sign;

                        if (LocalIndex < 0 || LocalIndex > List.Count)
                        {
                            Stack.Pop();
                        }

                        if (sign < 0)
                        {
                            //Node = MathEntryViewModel.Prev(Node);
                        }
                        else if (sign > 0)
                        {
                            //Node = MathEntryViewModel.Next(Node);
                        }

                        if (Node.Value.Value is ObservableLinkedList<T> expression)
                        {
                            Stack.Push(new Test<T>(expression, 0));
                        }
                    }
                    while (!(Node.Value.Value is MathViewModel));
                }

                return true;
            }
        }
    }

    public class Test : Test<object>
    {
        public Test Parent { get; set; }

        public Test(ObservableLinkedList<object> list, int index) : base(list, index) { }
    }

    public class Test<T>
    {
        public LinkedListNode<Node<T>> Node;
        public ObservableLinkedList<T> List { get; set; }
        public int Index { get; set; }
        public int Position { get; set; }

        public Test(ObservableLinkedList<T> list, int index)
        {
            List = list;
            Index = index;
        }
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

        public event EventHandler<ChangedEventArgs<int>> CursorMoved;

        public static readonly BindableProperty CursorPositionProperty = BindableProperty.Create(nameof(CursorPosition), typeof(int), typeof(MathEntryViewModel), 0, coerceValue: (bindable, value) => ((MathEntryViewModel)bindable).CoerceCursorPosition((int)value));

        public int Count => Text.Length;
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

        public object Current => Local.Node?.Value.Value;

        private readonly TreeVisitor<object> Visitor;
        private readonly LinkedListNode<Node<object>> Root;
        private CursorViewModel Cursor;

        /*public ObservableLinkedList<object> Parent => Stack.Last().List;
        public int LocalIndex
        {
            get => Stack.Last().Index;
            private set => Stack.Last().Index = value;
        }*/
        //private LinkedListNode<Node<object>> Current { get; set; }
        private Test Local;

        /*public class Enumerator<T>
        {
            public ObservableLinkedList<T> Top => Stack.Peek();
            public LinkedListNode<Node<T>> Current;

            private Stack<ObservableLinkedList<T>> Stack;
            public readonly TreeVisitor<T> Tree;

            public Enumerator(LinkedListNode<Node<T>> root)
            {
                Stack = new Stack<ObservableLinkedList<T>>();
                Stack.Push(new ObservableLinkedList<T>(root));

                Tree = new TreeVisitor<T>(root);
                Current = root;
            }

            public bool MoveNext() => Move(1);
            public bool MovePrev() => Move(-1);

            public bool Move(int n)
            {
                int sign = Math.Sign(n);

                for (int i = 0; i < Math.Abs(n); i++)
                {
                    do
                    {
                        Current = sign == -1 ? Tree.Prev(Current) : Tree.Next(Current);

                        if (Current == null)
                        {
                            if (Stack.Count == 1)
                            {
                                return false;
                            }

                            Stack.Pop();
                        }
                        if (Current is ObservableLinkedList<T> expression)
                        {
                            Stack.Push(expression);
                        }
                    }
                    while (Current.Value.Children != null);
                }

                return true;
            }
        }*/

        /*private Stack<Test> Stack;
        private ObservableLinkedList<object> Expression => Stack.Peek().List;
        private int LocalIndex
        {
            get => Stack.Peek().Index;
            set => Stack.Peek().Index = value;
        }*/

        public MathEntryViewModel()
        {
            /*SetChildren(Children = new ObservableCollection<Node<object>>
            {
                (Cursor = new CursorViewModel(this))
            });*/
            Root = new LinkedListNode<Node<object>>(new Node<object>
            {
                Children = new LinkedList<Node<object>>
                {
                    //(CursorVM = new CursorViewModel(new Node { Value = this }))
                },
            });
            
            Visitor = new TreeVisitor<object>(Root);
            //Current = Visitor.After(Root);

            Cursor = new CursorViewModel(new Node { Value = this });
            Local = new Test(new ObservableLinkedList<object>(Root), 0);// { Node = Current };
            Children = Local.List;
            Local.List.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, Cursor, 0));

            //Editor = new Enumerator<object>(root);
            //Children = Editor.Top;
            //Editor.Move(1);

            //Stack = new Stack<ObservableLinkedList<object>.Itr>();
            //Stack.Push(new ObservableLinkedList<object>.Itr(list));
            //Itr.MoveNext();

            //Editor = new ObservableLinkedList<object>.Enumerator(list);

            //Stack = new Stack<Test>();
            //Stack.Push(new Test(list, 0));
            //Expressions.Push(new  new ObservableLinkedList<object>(root));

            //Cursor.MoveNext();

            //Cursor.Itr.AddNext(Cursor);
            //Cursor.Parent = this;
            //Cursor.Top.MoveNext();

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
            return;

            int index = CursorPosition + sign;

            if (index < 0 || index > Text.Length)
            {
                //return;
            }

            return;

            CursorPosition += sign;

            /*Test oldPosition = new Test(Expression, LocalIndex);
            LinkedListNode<Node<object>> node = Current;

            LinkedListNode<Node<object>> cursor = Current;
            RemoveAt(CursorPosition);
            oldPosition.List.RemoveAt(oldPosition.Index);

            Expression.Insert(LocalIndex, Current);*/
        }

        private object CoerceCursorPosition(int position)
        {
            Local.List.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, Cursor, Local.List.Count - 1 - Local.Index));

            Test old = new Test(Local.List, Local.Index) { Parent = Local.Parent, Node = Local.Node, Position = Local.Position };
            int diff = Local.Position - position;
            int sign = Math.Sign(diff);
            int index = Local.Index + sign;

            if (index < 0 || index > Local.List.Count)
            {
                diff += sign;
            }

            Local = Move(Local, diff);

            while ((Local.List == old.List && Local.Index == old.Index) || Local.Node.Value.Value.Equals("/"))
            {
                Local = Move(Local, sign);
            }

            Local.List.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, Cursor, Local.List.Count - Local.Index));

            return Local.Position;
            //entry.Local = entry.Move(entry.Local, (int)oldValue - (int)newValue);

            //return value;
        }

        public int IndexOf(object value)
        {
            Test pos = Local;
            while (pos.Node?.Value.Value != value)
            {
                throw new NotImplementedException();
            }

            return Text.Length - pos.Index;
        }

        public void Insert(object value) => Insert(CursorPosition, value);// AddRelative(true, Local.Node?.Value.Value, value, CursorPosition);
        /*private void Insert(LinkedListNode<Node<object>> newNode) => AddRelative(true, Local.Node?.Value.Value, newNode, CursorPosition);

        public void AddBefore(object reference, object value, int indexGuess = 0) => AddRelative(true, reference, value, indexGuess);
        public void AddAfter(object reference, object value, int indexGuess = 0) => AddRelative(false, reference, value, indexGuess);

        private void AddRelative(bool before, object reference, object value, int indexGuess) => AddRelative(before, reference, new LinkedListNode<Node<object>>(new Node<object>(value)), indexGuess);*/

        //private void AddRelative(bool before, object reference, LinkedListNode<Node<object>> newNode, int indexGuess)
        public void Insert(int index, object value) => Insert(index, new LinkedListNode<Node<object>>(new Node<object>(value)));

        private void Insert(int index, LinkedListNode<Node<object>> newNode)
        {
            //Text = (Text ?? string.Empty).Insert(index, newNode.Value.Value.ToString());
            //CursorPosition++;

            //OnPropertyChanging(nameof(CursorPosition));

            //Node<object> node = new Node<object>(newNode);
            Test pos = Move(Local, Local.Position - (Text.Length - index));
            /*while (pos.Node?.Value.Value != reference)
            {
                pos = Move(pos, Math.Sign(indexGuess - CursorPosition));
            }*/

            if (pos.Node == null)
            {
                Root.Value.Children.AddLast(newNode);
            }
            else
            {
                pos.Node.List.AddBefore(pos.Node, newNode);
            }

            newNode.Value.ParentNode = pos.Node?.Value.ParentNode ?? Root;
            pos.List.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newNode.Value.Value, pos.List.Count - 1 - pos.Index));
            
            Text += "l";// newNode.Value.Value.ToString();
            OnPropertyChanged(nameof(CursorPosition));
            //CursorPosition = CursorPosition;
            //pos.Index += (pos.List.Count > 1 ? 1 : 0);
            //CursorPosition++;
        }

        public bool Delete()
        {
            RemoveAt(CursorPosition - 1);

            return true;
        }

        private void RemoveAt(int index) //object value, int indexGuess = 0)
        {
            if (index < 0 || index >= Text.Length)
            {
                return;
            }

            int diff = Local.Position - (Text.Length - index);
            Test pos = new Test(Local.List, Local.Index) { Parent = Local.Parent, Node = Local.Node, Position = Local.Position };
            
            if (diff == 0)
            {
                Local = Move(Local, 1);
            }
            else
            {
                pos = Move(pos, diff);
            }

            /*if (pos.Node == Local.Node)
            {
                Test test = Move(new Test(pos.List, pos.Index) { Parent = pos.Parent, Node = pos.Node }, 1);
                Local.Node = test.Node;
            }*/

            pos.Node.List.Remove(pos.Node);
            pos.List.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, pos.Node.Value.Value, pos.List.Count - pos.Index));

            Text = Text.Remove(index, 1);
        }

        /*private Cursor(Cursor other, int diff)
        {
            Entry = other.Entry;
            Stack = new Stack<Test>(other.Stack.Select(test => new Test(test.List, test.Index)));
            Visitor = other.Visitor;
            Current = other.Current;

            int sign = Math.Sign(diff);

            for (diff = Math.Abs(diff); diff > 0; diff--)
            {
                ObservableLinkedList<object> last = null;
                if (Current.Value.Value is TextViewModel || Current.Value.Value is CursorViewModel)
                {
                    last = Parent;
                }

                do
                {
                    Current = sign == -1 ? Visitor.Before(Current) : Visitor.After(Current);

                    if (Current.Value.Value is ObservableLinkedList<object> expression)
                    {
                        if (expression == Parent)
                        {
                            Stack.Pop();
                        }
                        else
                        {
                            Stack.Push(new Test(expression, sign == -1 ? 0 : expression.Count));
                        }
                    }
                }
                while (Current.Value.Children != null);

                if (last == Parent || Current.Value.Value.Equals("/"))
                {
                    LocalIndex += sign;
                }
            }
        }

        public static Cursor operator +(Cursor cursor, int count) => new Cursor(cursor, count);
        public static Cursor operator -(Cursor cursor, int count) => new Cursor(cursor, -count);

        public void Insert(object value) => Insert(new LinkedListNode<Node<object>>(new Node<object>(value)));*/

        /*private void Merge((LinkedListNode<Node<object>>, Stack<Test>, int) offset)
        {
            Current = offset.Item1;
            Stack.RemoveRange(Stack.Count - 1 - offset.Item3, offset.Item3 + 1);

            foreach (Test test in offset.Item2.Reverse())
            {
                Stack.Add(test);
            }
        }*/

        private Test Move(Test local, int diff)
        {
            //Stack<Test> stack = new Stack<Test>();
            //stack.Push(new Test(Stack.Last().List, Stack.Last().Index));
            //int up = Stack.Count - 1;

            int sign = Math.Sign(diff);

            for (diff = Math.Abs(diff); diff > 0; diff--)
            {
                ObservableLinkedList<object> last = null;
                if (local.Node == null || local.Node.Value.Value is MathViewModel)// || local.Node.Value.Value is CursorViewModel)
                {
                    last = local.List;
                }

                do
                {
                    local.Node = sign == -1 ? (local.Node == null ? Root.Value.Children.Last : Visitor.Before(local.Node)) : Visitor.After(local.Node);

                    if (local.Node == null)
                    {
                        break;
                    }
                    else if (local.Node.Value.Value is ObservableLinkedList<object> expression)
                    {
                        if (expression == local.List)
                        {
                            /*if (stack.Count == 0)
                            {
                                if (--up < 0)
                                {
                                    return (node, stack, up);
                                }

                                stack.Push(new Test(Stack[up].List, Stack[up].Index));
                            }
                            else
                            {
                                stack.Pop();
                            }*/

                            if (local.Parent == null)
                            {
                                return local;
                            }

                            local = local.Parent;
                        }
                        else
                        {
                            local = new Test(expression, sign == -1 ? expression.Count : 0) { Parent = local, Node = local.Node };
                        }
                    }
                }
                while (local.Node.Value.Children != null);

                if (last == local.List || local.Node?.Value.Value.Equals("/") == true)
                //if (local.Node?.Value.Value is MathViewModel)
                {
                    local.Index -= sign;
                }

                local.Position -= sign;
            }

            return local.Index >= 0 && local.Index <= local.List.Count ? local : local.Parent;
        }

        public static readonly MathViewModel LeftParenthesis = new TextViewModel { Text = "(" };
        public static readonly MathViewModel RightParenthesis = new TextViewModel { Text = ")" };

        /*public void SetCursor(object left, object right, int direction = 1, bool fromCursor = false)
        {
            if (!fromCursor)
            {
                if (direction == 1)
                {
                    Cursor.Reset();
                }
                else if (direction == -1)
                {
                    Cursor.End();
                }
            }

            bool matchLeft = false, matchRight = false;
            while (!matchLeft || !matchRight)
            {
                matchLeft = Cursor.Top.Current == left;
                if (!Cursor.MoveNext())
                {
                    break;
                }
                matchRight = Cursor.Top.Current == right;
            }


        }*/

        public object this[int i]
        {
            get
            {
                if ((CursorPosition - i) * 2 < i + 1)
                {

                }

                return null;
            }
        }

        private string InternalText;

        protected override void OnTextChanged(string oldText, string newText)
        {
            return;
            if (newText == InternalText)
            {
                return;
            }

            int begin;
            for (begin = 0; begin < oldText.Length && begin < newText.Length && oldText[begin] == newText[begin]; begin++) { }
            int end;
            for (end = Math.Min(oldText.Length, newText.Length) - 1; end >= 0 && oldText[end] == newText[end]; end++) { }

            if (begin + end == oldText.Length)
            {
                // Insert
            }
            else if (begin + end == newText.Length)
            {
                // Delete
            }
            else
            {
                base.OnTextChanged(oldText, newText);
            }
        }

        public void Type(string str) => Type(CursorPosition, str);

        public void Type(int index, string str)
        {
            if (str.Length == 0)
            {
                return;
            }

            //Suround previous thing with parentheses if it's an exponent or a fraction
            /*if (str[0] == '^' && itr.MovePrev())
            {
                if ((itr.Current is ExpressionViewModel expression && expression.TextFormat == TextFormatting.Superscript) || itr.Current is FractionViewModel)
                {
                    //itr.MovePrev();
                    //itr.BeginningOfPreviousMathObject();

                    itr.AddPrev(LeftParenthesis);
                    itr.AddPrev(RightParenthesis);
                    itr.MoveNext();

                    //Expression.Children.Insert(Expression.Children.BeginningOfPreviousMathObject(Index++), LeftParenthesis);
                    //Expression.Children.Insert(Index++, RightParenthesis);
                }
                
                itr.MoveNext();
            }*/

            LinkedListNode<Node<object>> node;

            if (str == "(")
            {
                node = new LinkedListNode<Node<object>>(new Node<object> { Value = LeftParenthesis });
            }
            else if (str == ")")
            {
                node = new LinkedListNode<Node<object>>(new Node<object> { Value = RightParenthesis });
            }
            else
            {
                IList<Node<object>> list = Reader<ObservableCollection<Node<object>>>.Render(str);// Crunch.Machine.StringClassification.Simple(str));
                if (list.Count == 1)
                {
                    node = new LinkedListNode<Node<object>>(list[0]);
                }
                else
                {
                    node = new LinkedListNode<Node<object>>(null);
                    node.Value.Children = new LinkedList<Node<object>>(list);
                }

                if (node.Value.Children != null)
                {
                    foreach (Node<object> child in node.Value.Children)
                    {
                        child.ParentNode = node;
                    }
                }

                /*if (node.Value.Value is FractionViewModel fraction1)
                {
                    List<ObservableLinkedList<object>> parts = new List<ObservableLinkedList<object>>();

                    for (LinkedListNode<Node<object>> next = node.Value.Children.First; next != null; next = next.Next)
                    {
                        if (!(next.Value.Value is ObservableLinkedList<object>))
                        {
                            continue;
                        }

                        next.Value.Children = new LinkedList<Node<object>>
                        {
                            new Node<object>("("),
                            new Node<object>
                            {
                                Children = new LinkedList<Node<object>>()
                            },
                            new Node<object>(")"),
                        };
                        LinkedListNode<Node<object>> part = next.Value.Children.First.Next;
                        parts.Add(new ObservableLinkedList<object>(part));
                        part.Value.Value = parts.Last();

                        foreach (Node<object> child in next.Value.Children)
                        {
                            child.ParentNode = next;
                        }

                        next.Value.Value = null;
                    }

                    fraction1.Numerator = parts[0];
                    fraction1.Denominator = parts[1];
                }*/
            }

            Insert(CursorPosition, node);
            //Expression.Insert(LocalIndex++, node);

            if (node.Value.ChildrenList != null && node.Value.ChildrenList.Count > 0 && node.Value.ChildrenList.Last().Value == null)
            {
                MoveCursor(CursorKey.Left);
            }

            /*IBiEnumerator<Node<object>> itr = new Tree<object>.Enumerator(node);
            while (itr.MoveNext())
            {
                if (itr.Current.ChildrenList == null)
                {
                    continue;
                }

                for (int i = 0; i < itr.Current.ChildrenList.Count; i++)
                {
                    Node<object> item = itr.Current.ChildrenList[i];
                    IList<Node<object>> children = item?.ChildrenList;

                    if (item.Value != null)
                    {
                        continue;
                    }

                    if (children == null)
                    {
                        children = new ObservableCollection<Node<object>>();

                        if (item.Value != null)
                        {
                            children.Add(item);
                        }

                        item = itr.Current.ChildrenList[i] = new Node<object> { Parent = itr.Current };
                    }

                    children.Insert(0, new Node<object> { Value = new Parenthesis(item, true) });
                    children.Add(new Node<object> { Value = new Parenthesis(item, false) });

                    item.SetChildren(children);
                }
            }*/

            /*if (node.Value is FractionViewModel fraction && fraction.Children.First.Value.Children.Count == 0)
            {
                //Cursor.MovePrev();
                //fraction.ChildrenList[0].Children = new LinkedList<Node<object>>();
                LinkedListNode<Node<object>> current = Current;
                int count = MathViewModelExtensions.BeginningOfPreviousMathObject(ref current);
                
                while (current.Value.Value != Cursor)
                {
                    Node<object> value = current.Value;

                    current = current.Next;
                    //Remove(Current, -1);
                    //Expression.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, node, --LocalIndex));

                    fraction.Children.First.Value.Children.AddLast(value);
                    value.ParentNode = fraction.Children.First;
                }
            }*/

            //Current.List.AddBefore(Current, node);
            //Editor.Top.Insert(node.Value.Value);
            //node.Value.ParentNode = Current.Value.ParentNode;
            //Expression.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, node.Value.Value, LocalIndex++));

            /*foreach (Node<object> item in node.Value == null ? node.ChildrenList : new List<Node<object>> { node })
            {
                Cursor.AddPrev(item);
            }*/

            /*LinkedListNode<Node<object>> temp = Current.List.Last;
            while (temp != null)
            {
                Print.Log(temp.Value.Value?.GetType(), temp.Value.Value);
                temp = Prev(temp);
            }*/

            //CursorPosition += str.Length;
            //Text = Text?.Insert(CursorPosition + n + (n < 0 ? 1 : -1), str) ?? str;
            //OnPropertyChanged(nameof(CursorPosition));
        }

#if false
        private int InternalCursorPosition;

        /*public void SetCursor(int index, IList parent, int direction = 1, bool relative = false)
        {
            direction = Math.Sign(direction);

            if (!relative)
            {
                //Move(direction == 1 ? -CursorPosition : (Count - CursorPosition));
                Move(-CursorPosition);
            }

            parent.Insert(index, Cursor);

            while (Cursor.Current != Cursor)
            {
                Move(direction);
            }

            CursorPosition = InternalCursorPosition;
        }*/

        public bool SetCursor(object item, bool isAfter, bool searchFromEnd = false, bool? searchDirection = null) => SetCursor(isAfter ? 1 : -1, item, searchFromEnd, searchDirection);
        private bool SetCursor(int n, object item = null, bool? searchFromEnd = null, bool? searchDirection = null)
        {
            int sign = Math.Sign(n);

            //Cursor.Itr.Move(-sign);
            //Cursor.Itr.Remove(sign);

            /*string a = "test".ToString();
            string b = "test".ToString();
            List<object> list = new List<object> { a, b };
            Print.Log(string.IsInterned(a), string.IsInterned(b), a == b, a.Equals(b), b.Equals(a), Equals(a, b), ReferenceEquals(a, b));
            Print.Log(list[0] == list[1], list[0].Equals(list[1]), list[1].Equals(list[0]), Equals(list[0], list[1]), ReferenceEquals(list[0], list[1]));*/

            if (!CursorVM.Move(sign) || !Find(CursorVM, item ?? CursorVM.Current, searchFromEnd, searchDirection))// !Find(item ?? Cursor.Current, Cursor, sign, Cursor, searchFromEnd, searchDirection))
            {
                sign = -sign;
                //Cursor.Itr.Add(sign, Cursor);

                return false;
            }

            CursorVM.Add(n, CursorVM);
            //Cursor.Move(sign);

            return true;
        }

        public bool AddBefore(object item, object value, bool searchFromEnd = false, bool? searchDirection = null) => AddRelative(-1, item, value, searchFromEnd, searchDirection);
        public bool AddAfter(object item, object value, bool searchFromEnd = false, bool? searchDirection = null) => AddRelative(1, item, value, searchFromEnd, searchDirection);

        private bool AddRelative(int direction, object item, object value, bool searchFromEnd = false, bool? searchDirection = null)
        {
            IEditEnumerator<object> itr = GetEditEnumerator();

            if (!Find(itr, item, searchFromEnd, searchDirection))
            {
                return false;
            }

            itr.Add(Math.Sign(direction), value);
            return true;
        }

        private IEditEnumerator<object> GetEditEnumerator() => new CursorViewModel(null);

        private static bool Find<T>(IBiEnumerator<T> itr, T item, bool? searchFromEnd = null, bool? searchRight = null)
        {
            if (searchFromEnd == true)
            {
                itr.End();
                itr.MovePrev();
            }
            else if (searchFromEnd == false)
            {
                itr.Reset();
                itr.MoveNext();
            }

            int sign = searchRight.HasValue ? (searchRight.Value ? 1 : -1) : (searchFromEnd == true ? -1 : 1);

            object start = itr.Current;
            while (!Equals(itr.Current, item))
            {
                if (!itr.Move(sign))
                {
                    if (sign == -1)
                    {
                        itr.End();
                    }
                    else if (sign == 1)
                    {
                        itr.Reset();
                    }

                    itr.Move(sign);
                }

                if (Equals(itr.Current, start))
                {
                    return false;
                }
            }

            return true;

            //itr.Add(n, value);

            //sign = Math.Sign(n);

            /*for (int i = 0; i < Math.Abs(n) - 1; i++)
            {
                if (!itr.Move(sign))
                {
                    return false;
                }
            }

            itr.Add(sign, value);
            //itr.Move(sign);
            while (!Equals(itr.Current, value))
            {
                itr.Move(sign);
            }*/
        }

        protected virtual void OnCursorPositionChanging(int oldValue, int newValue)
        {
            CursorPositionChanging?.Invoke(this, new ChangedEventArgs<int>(oldValue, newValue));
        }

        protected virtual void OnCursorPositionChanged(int oldValue, int newValue)
        {
            int diff = newValue - oldValue;
            //Cursor.Itr.Move(diff);

            return;

            int sign = Math.Sign(diff);

            if (diff != 0 && !CursorVM.Move(diff))
            {
                sign *= -1;
            }

            InternalCursorPosition = newValue;
        }

        private void Add(LinkedListNode<Node<object>> node, LinkedListNode<Node<object>> value, int n)
        {
            if (n < 0)
            {
                node.List.AddBefore(node, value);
            }
            else if (n > 0)
            {
                node.List.AddAfter(node, value);
            }
        }

        /*public void RemoveAt(int index)
        {
            LinkedListNode<Node<object>> node = Current;
            int sign = Math.Sign(index - CursorPosition);

            for (int i = 0; i < Math.Abs(index - CursorPosition); i++)
            {
                node = sign == -1 ? node.Previous : node.Next;
            }

            if (node == Current)
            {

            }
            else
            {
                node.List.Remove(node);
            }
        }*/

        private void Remove(LinkedListNode<Node<object>> node, int n)
        {
            if (n < 0)
            {
                node.List.Remove(node.Previous);
            }
            else if (n > 0)
            {
                node.List.Remove(node.Next);
            }
        }

        public static LinkedListNode<Node<T>> Prev<T>(LinkedListNode<Node<T>> node)
        {
            if (node.Value.Children != null && node.Value.Children.Count > 0)
            {
                return node.Value.Children.Last;
            }

            while (node != null && node.Previous == null)
            {
                node = node.Value.ParentNode;
            }

            return node?.Previous;

            if (node.Previous == null)
            {
                return node.Value.ParentNode;
            }

            node = node.Previous;

            while (node.Value.Children != null && node.Value.Children.Count > 0)
            {
                node = node.Value.Children.Last;
            }

            return node;
        }

        public static LinkedListNode<Node<T>> Next<T>(LinkedListNode<Node<T>> node)
        {
            if (node.Value.Children != null && node.Value.Children.Count > 0)
            {
                return node.Value.Children.First;
            }

            while (node != null && node.Next == null)
            {
                node = node.Value.ParentNode;
            }

            return node?.Next;
        }

        private bool CanAddHere(bool before) // => Cursor.Current is Parenthesis parenthesis && parenthesis.Expression.Parent != null && parenthesis.Expression.Parent.Children[parenthesis.Expression.Parent.Children.IndexOf( //parenthesis.Expression.Parent.Children[sign == -1 ? 0 : (parenthesis.Expression.Parent.Children.Count - 1)] != parenthesis.Expression;
        {
            if (CursorVM.Current is Parenthesis parenthesis && parenthesis.Opening == before && parenthesis?.Expression.Parent != null)
            {
                IList<Node<object>> children = parenthesis.Expression.Parent.ChildrenList;
                int index = children.IndexOf(parenthesis.Expression) + (before ? -1 : 1);

                if (index >= 0 && index < children.Count && children[index].Value == null)
                {
                    return false;
                }
            }

            return true;
        }

        // Below taken from https://www.geeksforgeeks.org/edit-distance-dp-5/
        static int min(int x, int y, int z)
        {
            if (x <= y && x <= z)
                return x;
            if (y <= x && y <= z)
                return y;
            else
                return z;
        }

        static int editDist(string str1, string str2, int m, int n)
        {
            // If first string is empty, the only option is to 
            // insert all characters of second string into first 
            if (m == 0)
                return n;

            // If second string is empty, the only option is to 
            // remove all characters of first string 
            if (n == 0)
                return m;

            // If last characters of two strings are same, nothing 
            // much to do. Ignore last characters and get count for 
            // remaining strings. 
            if (str1[m - 1] == str2[n - 1])
                return editDist(str1, str2, m - 1, n - 1);

            // If last characters are not same, consider all three 
            // operations on last character of first string, recursively 
            // compute minimum cost for all three operations and take 
            // minimum of three values. 
            return 1 + min(editDist(str1, str2, m, n - 1), // Insert 
                           editDist(str1, str2, m - 1, n), // Remove 
                           editDist(str1, str2, m - 1, n - 1) // Replace 
                           );
        }
#endif

        /*public IEditEnumerator<object> GetEnumerator()
        {
            throw new NotImplementedException();
        }

#region EditEnumerableOverloads
        IEditEnumerator IEditEnumerable.GetEnumerator() => GetEnumerator();

        IBiEnumerator<object> IBiEnumerable<object>.GetEnumerator() => GetEnumerator();

        IBiEnumerator IBiEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<object> IEnumerable<object>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
#endregion*/
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