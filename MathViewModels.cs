using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Extensions;
using System.Windows.Input;
using Parse;

namespace Xamarin.Forms.MathDisplay
{
    public class ExpressionList : PartialObservableLinkedList<object>
    {
        public ExpressionList(LinkedList<object> fullList) : base(fullList) { }

        public ExpressionList(LinkedList<object> fullList, LinkedListNode<object> first, LinkedListNode<object> last) : base(fullList, first, last) { }

        public void ParentSet(LinkedListNode<object> node) => OnAdded(node);

        protected override void OnAdded(LinkedListNode<object> node)
        {
            if (node.Value is MathViewModel mvm)
            {
                //mvm.Parent = this;
            }

            base.OnAdded(node);
        }

        protected override void OnRemoved(object value)
        {
            if (value is MathViewModel mvm)
            {
                //mvm.Parent = this;
            }

            base.OnRemoved(value);
        }
    }

    public class PartialObservableLinkedList<T> : IEnumerable<T>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public int Count => System.Linq.Enumerable.Count(this);

        private LinkedList<T> FullList;
        private LinkedListNode<T> First => _First?.Next ?? FullList.First;
        private LinkedListNode<T> _First;
        private LinkedListNode<T> Last => _Last?.Previous ?? FullList.Last;
        private LinkedListNode<T> _Last;

        public PartialObservableLinkedList(LinkedList<T> fullList) : this(fullList, fullList.First, fullList.Last) { }

        public PartialObservableLinkedList(LinkedList<T> fullList, LinkedListNode<T> first, LinkedListNode<T> last)
        {
            FullList = fullList;
            _First = first;
            _Last = last;
            
            foreach (LinkedListNode<T> node in NodesBetween(First, Last))
            {
                if (node.Value is MathViewModel mvm)
                {
                    //mvm.Parent?.OnRemoved(node.Value);
                    //mvm.Parent = this as ExpressionList;
                }
            }
        }

        public void AddFirst(T value) => AddBefore(First, value);
        public void AddFirst(LinkedListNode<T> node) => AddBefore(First, node);
        public void AddLast(T value) => AddAfter(Last, value);
        public void AddLast(LinkedListNode<T> node) => AddAfter(Last, node);

        public void AddBefore(LinkedListNode<T> node, T value)
        {
            if (node == null)
            {
                FullList.AddFirst(value);
            }
            else
            {
                FullList.AddBefore(node, value);
            }
            OnAdded(node.Previous);
        }

        public void AddBefore(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            if (node == null)
            {
                FullList.AddFirst(newNode);
            }
            else
            {
                FullList.AddBefore(node, newNode);
            }
            OnAdded(node?.Previous ?? FullList.First);
        }

        public void AddAfter(LinkedListNode<T> node, T value)
        {
            FullList.AddAfter(node, value);
            OnAdded(node.Next);
        }

        public void AddAfter(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            FullList.AddAfter(node, newNode);
            OnAdded(node.Next);
        }

        protected virtual void OnAdded(LinkedListNode<T> node)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, node.Value, IndexOf(node)));
        }

        public void Remove(T value)
        {
            FullList.Remove(value);
            OnRemoved(value);
        }

        public void Remove(LinkedListNode<T> node)
        {
            FullList.Remove(node);
            OnRemoved(node.Value);
        }

        protected virtual void OnRemoved(T value)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, value, IndexOf(value)));
        }

        public static void Move(LinkedListNode<T> node, PartialObservableLinkedList<T> oldList, PartialObservableLinkedList<T> newList)
        {
            oldList.OnRemoved(node.Value);
            //newList.OnAdded(node);
        }

        private int IndexOf(LinkedListNode<T> node) => IndexOf(node.Value);

        private int IndexOf(T value)
        {
            LinkedListNode<T> first = First ?? FullList.First;
            LinkedListNode<T> last = Last ?? FullList.Last;

            int index = 0;
            while (!first.Value.Equals(value))
            {
                if (first == last)
                {
                    return -1;
                }

                //if (!(first.Value is MathViewModel mvm && mvm.Parent != this as ExpressionList))
                index++;
                first = first.Next;
            }

            return index;
        }

        public void Clear()
        {
            LinkedListNode<T> last = Last;
            _First = First.Previous;
            _Last = Last.Next;

            while (true)
            {
                LinkedListNode<T> next = First.Next ?? FullList.First;
                FullList.Remove(next);

                if (next == last)
                {
                    break;
                }
            }

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(T item)
        {
            foreach (T t in this)
            {
                if (t.Equals(item))
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (LinkedListNode<T> node in NodesBetween(First, Last))
            {
                //if (!(node.Value is MathViewModel mvm && mvm.Parent != this as ExpressionList))
                yield return node.Value;
            }
        }

        public static IEnumerable<LinkedListNode<T>> NodesBetween(LinkedListNode<T> first, LinkedListNode<T> last)
        {
            while (first != null)
            {
                yield return first;

                if (first == last)
                {
                    break;
                }

                first = first.Next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public abstract class MathViewModel : BindableObject
    {
        
    }

    public class TextViewModel : MathViewModel
    {
        public string Text { get; set; }

        public static implicit operator TextViewModel(string str) => new TextViewModel { Text = str };

        public override string ToString() => Text;
    }

    public class CursorViewModel : TextViewModel
    {
        public Stack<ObservableCollection<object>> Parents { get; } = new Stack<ObservableCollection<object>>();

        public CursorViewModel() => Text = "|";
    }

    public class ImageTextViewModel : TextViewModel
    {

    }

    public class ExpressionViewModel : MathViewModel
    {
        public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(Expression));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public IList Children { get; set; }

        public TextFormatting TextFormat { get; set; }
    }

    public class RadicalViewModel : MathViewModel
    {
        public IList Root { get; set; }
        public IList Radicand { get; set; }
    }

    public class FractionViewModel : MathViewModel, System.Collections.IBiEnumerable
    {
        public IList Numerator { get; set; }

        public IList Denominator { get; set; }

        public System.Collections.IBiEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class MathEntryViewModel : BindableObject // ExpressionViewModel
    {
        public enum CursorKey { Left, Right, Up, Down, End, Home };

        public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(MathEntryViewModel), string.Empty, propertyChanged: TextPropertyChanged);

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly BindableProperty CursorPositionProperty = BindableProperty.Create(nameof(CursorPosition), typeof(int), typeof(MathEntryViewModel), 0, propertyChanged: CursorPositionChanged);//, coerceValue: (bindable, value) => ((int)value).Bound(0, ((MathField)bindable).Children.Count));

        public event EventHandler<ChangedEventArgs<int>> CursorMoved;

        public int CursorPosition
        {
            get => (int)GetValue(CursorPositionProperty);
            set => SetValue(CursorPositionProperty, value);
        }

        public ICommand InputCommand { get; set; }
        public ICommand BackspaceCommand { get; set; }
        public ICommand MoveCursorCommand { get; set; }

        public ObservableCollection<object> Children { get; }

        //private LinkedList<object> List;
        //private LinkedListNode<object> CursorNode;

        //private ExpressionViewModel Expression;
        private CursorViewModel Cursor;

        public MathEntryViewModel()
        {
            //Children = new ExpressionList(List = new LinkedList<object>());
            Children = new ObservableCollection<object>
            {
                (Cursor = new CursorViewModel())
            };
            Cursor.Parents.Push(Children);

            //Children.AddFirst(CursorNode = new LinkedListNode<object>(Cursor));
            
            InputCommand = new Command<string>(value =>
            {
                /*MainPage page = SoftKeyboard.Cursor.Parent<MainPage>();
                if (page == null)
                {
                    App.Current.Home.AddCalculation();
                }*/

                //Input(value);
                Type(value);
            });

            BackspaceCommand = new Command(() =>
            {
                Delete();
            });

            MoveCursorCommand = new Command<CursorKey>(value =>
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
                    SoftKeyboard.Right();
                    //MathField.CursorPosition++;
                }
                else if (value == CursorKey.Left)
                {
                    /*LinkedListNode<object> node = CursorNode.Previous;
                    ExpressionList expression = ((MathViewModel)CursorNode.Value).Parent;
                    
                    expression.Remove(CursorNode);
                    expression.AddBefore(node, CursorNode);*/

                    //SoftKeyboard.Left();
                    //MathField.CursorPosition--;
                }
            });
        }

        public static readonly MathViewModel LeftParenthesis = new ImageTextViewModel { Text = "(" };
        public static readonly MathViewModel RightParenthesis = new ImageTextViewModel { Text = ")" };

        public void Type(string str)
        {
            if (str.Length == 0)
            {
                return;
            }

            int Index = SoftKeyboard.RealIndex;
            //Suround previous thing with parentheses if it's an exponent or a fraction
            /*if (str[0] == '^' && Index > 0 && ((Expression.Children
                [Index - 1] is ExpressionViewModel expression && expression.TextFormat == TextFormatting.Superscript) || Expression.Children[Index - 1] is FractionViewModel))
            {
                Expression.Children.Insert(Expression.Children.BeginningOfPreviousMathObject(Index++), LeftParenthesis());
                Expression.Children.Insert(Index++, RightParenthesis());
            }*/

            List<MathViewModel> list;
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
                list = Reader.Render(Crunch.Machine.StringClassification.Simple(str));
            }

            ObservableCollection<object> parent = Cursor.Parents.Peek();
            int index = parent.IndexOf(Cursor);
            
            if (list[0] is FractionViewModel fraction && fraction.Numerator.Count == 0)
            {
                fraction.Numerator = new ObservableCollection<object>();

                int start = index - 1;
                int count = start - parent.BeginningOfPreviousMathObject(start);
                for (int i = 0; i <= count; i++)
                {
                    object mvm = parent[start - count];

                    parent.Remove(mvm);
                    fraction.Numerator.Add(mvm);
                }

                fraction.Numerator.Trim();
                index -= fraction.Numerator.Count;
            }

            //LinkedListNode<object> node = CursorNode.Previous;
            //ExpressionList parent = ((MathViewModel)CursorNode.Value).Parent;

            foreach (MathViewModel mvm in list)
            {
                //LinkedListNode<MathViewModel> node = new LinkedListNode<MathViewModel>(mvm);
                //List.AddBefore(CursorNode, mvm);
                //Expression.ParentSet(node);

                parent.Insert(index++, mvm);
            }

            for (int i = 0; i < list.Count; i++)
            {
                /*node = node?.Next ?? List.First;

                ExpressionList Continuation() => new ExpressionList(List, node, CursorNode.Next);

                if (node.Value is ExpressionViewModel expression)
                {
                    expression.Children = Continuation();
                }
                else if (node.Value is FractionViewModel fraction)// && fraction.Numerator.Children.Count == 0)
                {
                    LinkedListNode<object> start = SoftKeyboard.BeginningOfPreviousMathObject(List, node.Previous);

                    fraction.Numerator = new ExpressionList(List, start.Previous, node);
                    fraction.Denominator = Continuation();
                }

                parent.ParentSet(node);*/
            }
            Index += list.Count;

            /*ExpressionViewModel continuation = (list.Last() as MathLayoutViewModel)?.InputContinuation;
            if (continuation != null)
            {
                continuation.Children.Add(Cursor);
                Index = 0;
            }*/

            //Cursor.Parent.OnInputChanged();
        }

        public bool Delete()
        {
            int Index = SoftKeyboard.RealIndex;
            //print.log(index, Cursor.Parent.Children.Count, Cursor.Parent);
            //foreach (View v in Cursor.Parent.Children)
            //print.log(v, v.GetType());

            string deleted = "";

            //Try to delete the container
            /*if (CursorNode?.Previous.Value is FractionViewModel fraction)
            {
                foreach (ExpressionList expression in fraction.Operands)
                {

                }
            }
            //Otherwise just delete the thing before
            else*/
            /*if (CursorNode.Previous != null)
            {
                Index--;
                //deleted = Expression.Children[Index].ToString();

                ExpressionList parent = (CursorNode.Previous.Value as MathViewModel).Parent;
                Children.Remove(CursorNode.Previous);

                if (parent != null && parent != (CursorNode.Value as MathViewModel)?.Parent)
                {
                    parent.ParentSet(CursorNode);
                }
                //Expression.Children.RemoveAt(Index);
            }*/

            //Infect(node => node.Previous);
            //Infect(node => node.Next);

            //Deleted?.Invoke(deleted);

            //Cursor.Parent.OnInputChanged();

            return true;
        }

        /*private void Infect(Func<LinkedListNode<object>, LinkedListNode<object>> next)
        {
            LinkedListNode<object> start = CursorNode;

            while (true)
            {
                start = next(start);

                if (!(start?.Value is TextViewModel))
                {
                    break;
                }

                if (start.Value is MathViewModel mvm)
                {
                    MathViewModel cursor = (MathViewModel)CursorNode.Value;

                    if (mvm.Parent == cursor.Parent)
                    {
                        break;
                    }

                    cursor.Parent.ParentSet(start);
                }
            }
        }*/

        private static void TextPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            MathEntryViewModel model = (MathEntryViewModel)bindable;
            string old = (string)oldValue;
            string value = (string)newValue;

            for (int i = 0; i < value.Length; i++)
            {
                if (old[i] == value[i])
                {
                    continue;
                }


            }
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

        /*private static void MathFieldPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            MathFieldViewModel viewModel = (MathFieldViewModel)bindable;

            if (oldValue is MathField oldMathField)
            {
                oldMathField.CursorMoved -= viewModel.CursorMoved;
                oldMathField.Children.CollectionChanged -= viewModel.ChildrenChanged;
            }

            if (newValue is MathField newMathField)
            {
                newMathField.CursorMoved += viewModel.CursorMoved;
                newMathField.Children.CollectionChanged += viewModel.ChildrenChanged;
            }
        }*/

        /*public void Input(string input, int? index = null)
        {
            index = index ?? CursorPosition;

            for (int i = input.Length - 1; i >= 0; i--)
            {
                MathViewModel value;

                if (input[i] == '/')
                {
                    value = new FractionViewModel();
                }
                else
                {
                    char c = input[i];
                    string pad = (c == '*' || c == '+' || c == '=') ? " " : "";
                    if (c == '*')
                    {
                        c = '×';
                    }
                    value = new TextViewModel { Text = pad + c + pad };
                }

                Cursor.Parent.Insert(index.Value, value);
            }
        }*/

        /*public void Delete(int? index = null)
        {
            index = index ?? CursorPosition;

            if (index.Value.IsBetween(1, Expression.Count))
            {
                Expression.RemoveAt(index.Value - 1);
            }
        }*/

        protected void OnCursorMoved(ChangedEventArgs<int> e)
        {
            CursorMoved?.Invoke(this, e);
        }

        private static void CursorPositionChanged(BindableObject bindable, object oldValue, object newValue)
        {
            MathEntryViewModel mathField = (MathEntryViewModel)bindable;
            mathField.OnCursorMoved(new ChangedEventArgs<int>((int)oldValue, (int)newValue));
        }
    }

#if false
    public static UpdatedReader Render = new UpdatedReader(
            new KeyValuePair<string, Operator<MathViewModel>>[]
            {
                new KeyValuePair<string, Operator<MathViewModel>>("√", UpdatedUnaryOperator((o) => new Radical(Wrap(o, false)))),
                new KeyValuePair<string, Operator<MathViewModel>>("_", UpdatedUnaryOperator((o) => new Expression(TextFormatting.Subscript, Wrap(o, false))))
            },
            new KeyValuePair<string, Operator<MathViewModel>>[]
            {
                //new KeyValuePair<string, ParseAlias.Operator<View>>("^", UpdatedBinaryOperator(exponents)),
                //new KeyValuePair<string, ParseAlias.Operator<View>>("log", new ParseAlias.Operator<View>((o) => new LinkedList<object>().Populate(new Text("log"), o[0], o[1]), NextOperand, (n) => n.Next == null ? null : NextOperand(n.Next)))
            },
            new KeyValuePair<string, Operator<Token>>[]
            {
                new KeyValuePair<string, Operator<Token>>("/", BinaryOperator((o1, o2) => new FractionViewModel
                {
                    Numerator = new MathExpression { Children = { o1 } },
                    Denominator = new MathExpression { Children = { o2 } },
                }))
            }
        );

        private static void NextOperand(IEditEnumerator<Token> itr)
        {
            if (itr.MoveNext() && itr.Current.ToString() == "-")
            {
                //Delete the negative sign
                itr.MovePrev();
                itr.Remove(1);
                itr.MoveNext();

                //Negate what's after
                //node.Next.Value = new LinkedList<object>().Populate("-", node.Next.Value);
            }
        }

        private static Operator<Token> UnaryOperator(Func<MathViewModel, MathViewModel> f) => new UnaryOperator<Token>((o) => new Token.Operand<MathViewModel>(f((MathViewModel)o.Value)), NextOperand);
        private static Operator<Token> BinaryOperator(Func<MathViewModel, MathViewModel, MathViewModel> f) => new BinaryOperator<Token>((o1, o2) => f(o1, o2), (itr) => itr.MovePrev(), NextOperand);
    }

    public class UpdatedReader
    {
        public readonly Parse.Reader Parser;
        public readonly Lexer<MathViewModel> Lexer;

        public UpdatedReader(params KeyValuePair<string, Operator<Token>>[][] operations)
        {
            var info = Parse.Extensions.MakeTrie(operations);
            Parser = new Parse.Reader(Juxtapose);
            Lexer = new Lexer<MathViewModel>(info, Tokenize);
        }

        public void Render(string input)
        {
            IEnumerable<Token> tokenStream = Lexer.TokenStream(input);
            Parser.Parse(tokenStream);
        }

        protected IEnumerable<Token.Operand<MathViewModel>> Tokenize(IEnumerable<char> operand)
        {

        }

        private Token Juxtapose(IEnumerable<Token> views) => new Expression { Children = views };

        public class Classifier : IClassifier<MathViewModel>
        {
            public readonly IDictionary<string, Tuple<Operator<Token>, int>> Operations;

            public Classifier(IDictionary<string, Tuple<Operator<Token>, int>> operations)
            {
                Operations = operations;
            }

            public Token Classify(MathViewModel input)
            {
                string text = input.ToString();

                Tuple<Operator<Token>, int> operation;
                if (text == "(")
                {
                    return new Token.Separator(text, true);
                }
                else if (text == ")")
                {
                    return new Token.Separator(text, false);
                }
                else if (Operations.TryGetValue(text, out operation))
                {
                    return new Token.Operator<Token>(text, operation.Item1, operation.Item2);
                }

                return new Token.Operand<MathViewModel>(input);
            }
        }
    }
#endif
}