using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Extensions;
using System.Linq;
using System.Security.Cryptography;
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
        public object Current => Top.Current;

        public Stack<IBiEnumerator> Parents { get; } = new Stack<IBiEnumerator>();
        public IBiEnumerator Top => Parents.Peek();
        public IEditEnumerator<object> Itr => Top as IEditEnumerator<object>;
        private IEnumerable<object> Enumerable;

        public int Position { get; private set; }

        public CursorViewModel(IEnumerable<object> enumerable)
        {
            Enumerable = enumerable;
            Reset();
            
            Text = "|";
        }

        public void Add(int n, object item)
        {
            int sign = Math.Sign(n);
            int offset = n - sign;

            if (Move(offset))
            {
                int direction = sign;

                while (true)
                {
                    if (!(Current is Parenthesis parenthesis))
                    {
                        Itr.Add(direction, item);
                    }
                    else if (!parenthesis.Add(sign, item))
                    {
                        Move(sign);

                        offset += sign;
                        direction = -sign;

                        continue;
                    }

                    if (item == this)
                    {
                        Move(direction);
                    }
                    break;
                }
            }

            //Move(-offset);
        }

        public bool Remove(int n)
        {
            Position += n;
            return Itr?.Remove(n) ?? false;
        }

        public int Seek(int direction, bool relative = true)
        {
            if (!relative)
            {
                if (direction == 1)
                {
                    Reset();
                }
                else if (direction == -1)
                {
                    End();
                }
            }

            int count = 0;
            while (Top.Current != this)
            {
                Move(direction);
                count++;
            }

            return count;
        }

        public bool MoveNext() => Move(1);

        public bool Move(int n)
        {
            int sign = Math.Sign(n);

            for (int i = 0; i < Math.Abs(n); i++)
            {
                do
                {
                    if (!Top.Move(sign))
                    {
                        if (Parents.Count == 1)
                        {
                            return false;
                        }

                        Parents.Pop();
                    }
                    else if (Top.Current is IEnumerable<object> enumerable && enumerable.GetBiEnumerator() != null)
                    {
                        Parents.Push(enumerable.GetEditEnumerator() ?? enumerable.GetBiEnumerator());

                        if (sign == -1)
                        {
                            Top.End();
                        }
                    }
                    else //if (Itr != null)
                    {
                        break;
                    }
                }
                while (true);// Itr == null);
            }

            return true;
        }

        public void Reset()
        {
            Clear();
            Top.Reset();

            Position = 0;
        }

        public void End()
        {
            Clear();
            Top.End();
        }

        private void Clear()
        {
            Parents.Clear();
            Parents.Push(Enumerable.GetEditEnumerator());
        }

        public void Dispose() => Itr?.Dispose();
    }

    /*public class ImageTextViewModel : TextViewModel
    {

    }*/

    public abstract class MathLayoutViewModel : MathViewModel
    {
        public virtual IList InputContinuation => null;
    }

    public class Parenthesis
    {
        private IEditEnumerator<object> Itr;

        public bool Opening { get; private set; }

        public Parenthesis(IEnumerable expression, bool opening)
        {
            Itr = (expression as IEnumerable<object>)?.GetEditEnumerator();
            
            if (Opening = opening)
            {
                Itr.Reset();
            }
            else
            {
                Itr.End();
            }
        }

        public bool Add(int n, object item)
        {
            if (Itr == null)
            {
                return false;
            }

            int sign = Math.Sign(n);

            if ((Opening && sign == -1) || (!Opening && sign == 1))
            {
                return false;
            }

            Itr.Add(sign, item);

            return true;
        }
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
                list.Add(new Parenthesis(operand as IEnumerable, true));
                list.Add(operand);
                list.Add(new Parenthesis(operand as IEnumerable, false));
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

    public class ExpressionViewModel : MathLayoutViewModel, IEditEnumerable<object>
    {
        public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(Expression), propertyChanged: (bindable, oldValue, newValue) => ((ExpressionViewModel)bindable).OnTextChanged((string)oldValue, (string)newValue));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public IList Children { get; set; }

        public TextFormatting TextFormat { get; set; }

        public override IList InputContinuation => TextFormat != TextFormatting.Subscript && Children.Count == 0 ? Children : null;

        protected virtual void OnTextChanged(string oldText, string newText)
        {
            Children = Reader<List<object>>.Render(newText);
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
        public IList<Node<T>> Children { get; set; }
        public Node<T> Parent { get; set; }

        public T Value { get; set; }
    }

    public class FractionViewModel : MathViewModel
    {
        public IList Numerator
        {
            get => (IList)Children[0].Value;
            set => Children[0].Value = value;
        }

        public IList Denominator
        {
            get => (IList)Children[1].Value;
            set => Children[1].Value = value;
        }

        public FractionViewModel()
        {
            Children = new List<Node<object>>
            {
                new Node<object> { Parent = this },
                new Node<object> { Parent = this },
            };
        }

        //public override IList InputContinuation => Denominator;

        /*public override IEnumerable GetOperands()
        {
            yield return Numerator;
            yield return Denominator;
        }*/
    }

    public class FakeList : INotifyCollectionChanged, IBiEnumerable
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        private Node Root;

        public void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        public IBiEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public struct Enumerator<T> : IBiEnumerator<T>
        {
            public T Current => Node.Value;
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
                Node = Root;

                while (Node?.Children != null)
                {
                    Node = Node.Children[Node.Children.Count - 1];
                }
            }

            public bool Move(int n)
            {
                if (Node == null)
                {
                    Node = Root;
                }
                else if (Node.Children != null)
                {
                    Node = Node.Children[0];
                    Indices.Add(0);
                }
                else
                {
                    while ((Node = Node.Parent) != null && ++Index >= Node.Children.Count)
                    {
                        Indices.RemoveAt(Indices.Count - 1);
                    }

                    if (Node == null)
                    {
                        return false;
                    }

                    Node = Node.Children[Index];
                }

                return true;
            }

            public bool MoveNext() => Move(1);

            public void Reset()
            {
                Indices.Clear();
                Node = null;
            }
        }
    }

    public class MathEntryViewModel1
    {
        public int CursorPosition;

        private LinkedList<Node> Items;
        private LinkedListNode<Node> Cursor;

        public MathEntryViewModel1()
        {
            Items = new LinkedList<Node>();

            Cursor = Items.First;
        }

        public void Insert(int index, char c)
        {
            LinkedListNode<Node> node = Items.First;
            for (int i = 0; i < index; i++)
            {
                node = node.Next;
            }

            Items.AddAfter(node, new Node
            {
                Char = c,
                ViewModel = new FractionViewModel(),
                Parent = node.Value.Parent
            });
        }

        private class Node
        {
            public object ViewModel;
            public object Parent;

            public char Char;
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

        public static readonly BindableProperty CursorPositionProperty = BindableProperty.Create(nameof(CursorPosition), typeof(int), typeof(MathEntryViewModel), 0, propertyChanging: (bindable, oldValue, newValue) => ((MathEntryViewModel)bindable).OnCursorPositionChanging((int)oldValue, (int)newValue),  propertyChanged: (bindable, oldValue, newValue) => ((MathEntryViewModel)bindable).OnCursorPositionChanged((int)oldValue, (int)newValue));

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

        /*public int CursorPosition
        {
            get => InternalCursorPosition;
            set
            {
                if (value == CursorPosition)
                {
                    return;
                }

                //Move(value - CursorPosition);
                //OnCursorPositionChanged(CursorPosition, value);
            }
        }*/

        public ICommand InputCommand { get; set; }
        public ICommand BackspaceCommand { get; set; }
        public ICommand MoveCursorCommand { get; set; }

        //public IList Children { get; set; }

        private CursorViewModel Cursor;

        public MathEntryViewModel()
        {
            Children = new ObservableCollection<object>();
            Cursor = new CursorViewModel(Children as IList<object>);

            //Cursor.Itr.AddNext(Cursor);
            Cursor.Parent = this;
            Cursor.Top.MoveNext();

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

        public void Insert(string str) => Insert(CursorPosition, str);

        public void Insert(int index, string str)
        {
            /*IEditEnumerator<object> itr;

            if (index == CursorPosition)
            {
                itr = Cursor;
            }
            else
            {
                itr = GetEditEnumerator();
                itr.Move(index - CursorPosition);
            }*/

            int diff = index - CursorPosition;
            Cursor.Move(diff);

            IList list;

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
                list = Reader<ObservableCollection<object>>.Render(str);// Crunch.Machine.StringClassification.Simple(str));
            }

            foreach (object item in list)
            {
                Cursor.AddPrev(item);
            }

            IBiEnumerator<object> itr = new FakeList.Enumerator<object>((Node)list[0]);
            while (itr.MoveNext())
            {
                Print.Log(itr.Current, itr.Current.GetType());
                ;
            }

            //Parse(list);

            Cursor.Move(-diff);

            //CursorPosition += str.Length;

            //Text = Text?.Insert(CursorPosition + n + (n < 0 ? 1 : -1), str) ?? str;
            //OnPropertyChanged(nameof(CursorPosition));
        }

        private void Parse(IList items)
        {
            foreach (object item in items)
            {
                /*if (item is Node node)
                {
                    Parse(node.Children);
                }*/
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
            for (end = Math.Min(oldText.Length, newText.Length) - 1; end >=0 && oldText[end] == newText[end]; end++) { }

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

        public void Type(string str)
        {
            if (str.Length == 0)
            {
                return;
            }

            IEditEnumerator<object> itr = Cursor.Itr;

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

            Insert(str);
            //CursorPosition += str.Length;
            object last = Cursor.Current;

            if (last is FractionViewModel fraction && fraction.Numerator.Count == 0)
            {
                itr.MovePrev();
                itr.BeginningOfPreviousMathObject();

                //fraction.Numerator.Add(LeftParenthesis);
                while (itr.Current != fraction)
                {
                    object current = itr.Current;

                    itr.MoveNext();
                    itr.RemovePrev();

                    fraction.Numerator.Add(current);
                }
                //fraction.Numerator.Add(RightParenthesis);
                itr.MoveNext();

                //fraction.Numerator.Trim();
                //fraction.Denominator.Add(LeftParenthesis);
                //fraction.Denominator.Add(RightParenthesis);
                //InternalCursorPosition += 4;
            }

            IList continuation = (last as MathLayoutViewModel)?.InputContinuation;
            if (continuation != null)
            {
                CursorPosition--;

                /*Move(-1);
                //Add(-1, "(");
                //Add(1, ")");
                CursorPosition++;
                CursorPosition++;
                Cursor.Seek(1);
                CursorPosition--;*/
                //CursorPosition-=2;
            }
        }

        public bool Delete()
        {
            Cursor.Itr.MovePrev();
            Cursor.Itr.RemoveNext();
            return true;

            if (Cursor.RemovePrev())
            {
                return true;
            }

            int count = Cursor.Parents.Count;

            do
            {
                if (Cursor.Parents.Count == 1)
                {
                    return false;
                }

                Cursor.Parents.Pop();
            }
            while (Cursor.Itr == null);

            IEditEnumerator<object> root = Cursor.Itr;
            Cursor.Itr.MoveNext();

            while (true)
            {
                Cursor.Move(-1);

                if (Cursor.Itr == root)
                {
                    break;
                }
                else if (Cursor.Itr?.Current != null && Cursor.Parents.Count <= count)
                {
                    //Itr.MovePrev();
                    object item = Cursor.Itr.Current;
                    Cursor.Itr.MoveNext();

                    if (Cursor.Itr.RemovePrev())
                    {
                        root.AddNext(item);
                    }
                }
            }

            Cursor.Itr.MoveNext();
            Cursor.Itr.RemovePrev();

            Cursor.Seek(1);

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

            Cursor.Itr.Move(-sign);
            Cursor.Itr.Remove(sign);

            /*string a = "test".ToString();
            string b = "test".ToString();
            List<object> list = new List<object> { a, b };
            Print.Log(string.IsInterned(a), string.IsInterned(b), a == b, a.Equals(b), b.Equals(a), Equals(a, b), ReferenceEquals(a, b));
            Print.Log(list[0] == list[1], list[0].Equals(list[1]), list[1].Equals(list[0]), Equals(list[0], list[1]), ReferenceEquals(list[0], list[1]));*/

            if (!Cursor.Move(sign) || !Find(Cursor, item ?? Cursor.Current, searchFromEnd, searchDirection))// !Find(item ?? Cursor.Current, Cursor, sign, Cursor, searchFromEnd, searchDirection))
            {
                sign = -sign;
                Cursor.Itr.Add(sign, Cursor);

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

        private IEditEnumerator<object> GetEditEnumerator() => new CursorViewModel(Children as IList<object>);

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
            Cursor.Itr.Move(diff);

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