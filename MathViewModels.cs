using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Extensions;
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

    public class CursorViewModel : TextViewModel, IEditEnumerator<object>
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
            Text = "|";
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
            node.Parent = parent;

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
        public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(Expression), propertyChanged: (bindable, oldValue, newValue) => ((ExpressionViewModel)bindable).OnTextChanged((string)oldValue, (string)newValue));

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
        public Node<T> Parent { get; set; }

        public T Value { get; set; }

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

                    node.Parent = this;
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
        public IList Numerator => (IList)ChildrenList[0]?.ChildrenList;

        public IList Denominator => (IList)ChildrenList[1]?.ChildrenList;

        public FractionViewModel(Node<object> numerator = null, Node<object> denominator = null) : base(2)
        {
            SetChildren(new List<Node<object>>
            {
                numerator,
                denominator
            });

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

    public class ObservableLinkedList<T> : INotifyCollectionChanged, IEnumerable
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private LinkedListNode<T> Root;

        public ObservableLinkedList(LinkedListNode<T> root)
        {
            Root = root;
        }

        public virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

        public IEnumerator GetEnumerator()
        {
            LinkedListNode<T> node = Root;

            while (node != null)
            {
                yield return node.Value;
                node = node.Next;
            }
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

        public event EventHandler<ChangedEventArgs<int>> CursorPositionChanging;

        public static readonly BindableProperty CursorPositionProperty = BindableProperty.Create(nameof(CursorPosition), typeof(int), typeof(MathEntryViewModel), 0, propertyChanging: (bindable, oldValue, newValue) => ((MathEntryViewModel)bindable).OnCursorPositionChanging((int)oldValue, (int)newValue), propertyChanged: (bindable, oldValue, newValue) => ((MathEntryViewModel)bindable).OnCursorPositionChanged((int)oldValue, (int)newValue));

        public int CursorPosition
        {
            get => (int)GetValue(CursorPositionProperty);
            set => SetValue(CursorPositionProperty, value);
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

        private CursorViewModel Cursor;
        private LinkedListNode<Node<object>> Current;

        private Stack<ObservableLinkedList<Node<object>>> Expressions;
        private List<int> Indices;
        private int LocalIndex
        {
            get => Indices[Indices.Count - 1];
            set => Indices[Indices.Count - 1] = value;
        }

        public MathEntryViewModel()
        {
            /*SetChildren(Children = new ObservableCollection<Node<object>>
            {
                (Cursor = new CursorViewModel(this))
            });*/
            var list = new LinkedList<Node<object>>();
            list.AddLast(Cursor = new CursorViewModel(new Node { Value = this }));

            var children = new ObservableLinkedList<Node<object>>(Current = list.First);
            Children = children;

            Expressions = new Stack<ObservableLinkedList<Node<object>>>();
            Expressions.Push(children);
            Indices = new List<int>();
            Indices.Add(0);

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

        public int IndexOf(object item)
        {
            int index = 0;

            foreach (object o in this)
            {

            }

            return index;
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

            Node<object> node;

            if (str == "(")
            {
                node = new Node<object> { Value = LeftParenthesis };
            }
            else if (str == ")")
            {
                node = new Node<object> { Value = RightParenthesis };
            }
            else
            {
                IList<Node<object>> list = Reader<ObservableCollection<Node<object>>>.Render(str);// Crunch.Machine.StringClassification.Simple(str));
                if (list.Count == 1)
                {
                    node = list[0];
                }
                else
                {
                    node = new Node<object>();
                    node.SetChildren(list);
                }
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

            if (node is FractionViewModel fraction && fraction.ChildrenList[0].Value == null)
            {
                //Cursor.MovePrev();
                Cursor.BeginningOfPreviousMathObject();

                while (Cursor.Current != Cursor)
                {
                    Node<object> current = (Node<object>)Cursor.Current;

                    Cursor.MoveNext();
                    Cursor.RemovePrev();

                    fraction.ChildrenList[0].ChildrenList.Insert(1, current);
                    current.Parent = fraction.ChildrenList[0];
                }
            }

            Current.List.AddBefore(Current, node);
            Expressions.Peek().OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, node, LocalIndex++));

            /*foreach (Node<object> item in node.Value == null ? node.ChildrenList : new List<Node<object>> { node })
            {
                Cursor.AddPrev(item);
            }*/

            if (node.ChildrenList != null && node.ChildrenList.Count > 0 && node.ChildrenList.Last().Value == null)
            {
                MoveCursor(CursorKey.Left);
            }

            //CursorPosition += str.Length;
            //Text = Text?.Insert(CursorPosition + n + (n < 0 ? 1 : -1), str) ?? str;
            //OnPropertyChanged(nameof(CursorPosition));
        }

        public bool Delete()
        {
            Cursor.Delete();
            return true;
        }

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

            if (!Cursor.Move(sign) || !Find(Cursor, item ?? Cursor.Current, searchFromEnd, searchDirection))// !Find(item ?? Cursor.Current, Cursor, sign, Cursor, searchFromEnd, searchDirection))
            {
                sign = -sign;
                //Cursor.Itr.Add(sign, Cursor);

                return false;
            }

            Cursor.Add(n, Cursor);
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

            if (diff != 0 && !Cursor.Move(diff))
            {
                sign *= -1;
            }

            InternalCursorPosition = newValue;
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

            //SetCursor(sign);
            CursorPosition += sign;

            LinkedListNode<Node<object>> next = Next(sign);
            Current.List.Remove(Current);

            /*if (!CanAddHere(sign == -1))
            {
                Cursor.Move(sign);
            }*/

            Add(next, Current, sign);
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

        private LinkedListNode<Node<object>> Next(int n)
        {
            if (n < 0)
            {
                return Current.Previous;
            }
            else if (n > 0)
            {
                return Current.Next;
            }

            return Current;
        }

        private bool CanAddHere(bool before) // => Cursor.Current is Parenthesis parenthesis && parenthesis.Expression.Parent != null && parenthesis.Expression.Parent.Children[parenthesis.Expression.Parent.Children.IndexOf( //parenthesis.Expression.Parent.Children[sign == -1 ? 0 : (parenthesis.Expression.Parent.Children.Count - 1)] != parenthesis.Expression;
        {
            if (Cursor.Current is Parenthesis parenthesis && parenthesis.Opening == before && parenthesis?.Expression.Parent != null)
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

        public static void BeginningOfPreviousMathObject(this IEditEnumerator itr)
        {
            int imbalance = 0;

            //Grab stuff until we hit an operand
            while (itr.MovePrev() && !(Crunch.Machine.StringClassification.IsOperator(itr.Current.ToString().Trim()) && itr.Current.ToString() != "-" && imbalance == 0))
            {
                string s = itr.Current.ToString().Trim();

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

            itr.MoveNext();
        }
    }
}