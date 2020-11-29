using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Extensions;
using System.Linq;
using System.Windows.Input;
using Parse;
using Xamarin.Forms;
using Xamarin.Forms.MathDisplay;

namespace Xamarin.Forms.MathDisplay
{
    using ExpressionList = PartialObservableLinkedList<MathViewModel>;

    public class PartialObservableLinkedList<T> : IEnumerable<T>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

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
        }

        public void AddFirst(T value) => AddBefore(First, value);
        public void AddFirst(LinkedListNode<T> node) => AddBefore(First, node);
        public void AddLast(T value) => AddAfter(Last, value);
        public void AddLast(LinkedListNode<T> node) => AddAfter(Last, node);

        public void AddBefore(LinkedListNode<T> node, T value)
        {
            FullList.AddBefore(node, value);
            OnAdded(node, value, -1);
        }

        public void AddBefore(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            FullList.AddBefore(node, newNode);
            OnAdded(node, newNode.Value, -1);
        }

        public void AddAfter(LinkedListNode<T> node, T value)
        {
            FullList.AddAfter(node, value);
            OnAdded(node, value, 1);
        }

        public void AddAfter(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            FullList.AddAfter(node, newNode);
            OnAdded(node, newNode.Value, 1);
        }

        private void OnAdded(LinkedListNode<T> node, T item, int offset)
        {
            int index = IndexOf(node);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index + offset));
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

        private void OnRemoved(T value)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, value, IndexOf(value)));
        }

        public static void Move(LinkedListNode<T> node, PartialObservableLinkedList<T> oldList, PartialObservableLinkedList<T> newList)
        {
            oldList.OnRemoved(node.Value);
            newList.OnAdded(node, node.Value, 0);
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

                first = first.Next;
                index++;
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
            LinkedListNode<T> node = First ?? FullList.First;
            LinkedListNode<T> last = Last ?? FullList.Last;

            while (node != null)
            {
                yield return node.Value;

                if (node == last)
                {
                    break;
                }

                node = node.Next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public abstract class MathViewModel : BindableObject
    {
        public ExpressionViewModel Parent { get; internal set; }
    }

    public abstract class MathLayoutViewModel : MathViewModel, IEnumerable<TextViewModel>
    {
        public virtual ExpressionViewModel InputContinuation => null;

        public abstract void Lyse();
        protected void Lyse(params ExpressionViewModel[] sources)
        {
            int index = Parent.Children.IndexOf(this);
            for (int i = sources.Length - 1; i >= 0; i--)
            {
                for (int j = sources[i].Children.Count - 1; j >= 0; j--)
                {
                    Parent.Children.Insert(index + 1, sources[i].Children[j]);
                }
            }
            Parent.Children.RemoveAt(index);
        }

        public abstract IEnumerator<TextViewModel> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class TextViewModel : MathViewModel
    {
        public string Text { get; set; }

        public static implicit operator TextViewModel(string str) => new TextViewModel { Text = str };

        public override string ToString() => Text;
    }

    public class CursorViewModel : TextViewModel
    {
        public CursorViewModel() => Text = "|";
    }

    public class ImageTextViewModel : TextViewModel
    {

    }

    public class ExpressionViewModel : MathLayoutViewModel
    {
        public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(Expression));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public override ExpressionViewModel InputContinuation => TextFormat != TextFormatting.Subscript && Children.Count == 0 ? this : null;

        public TextFormatting TextFormat { get; set; }

        public ObservableCollection<MathViewModel> Children { get; private set; } = new ObservableCollection<MathViewModel>();

        public ExpressionViewModel(params MathViewModel[] children)
        {
            foreach (MathViewModel mvm in children)
            {
                Children.Add(mvm);
            }

            Children.CollectionChanged += ChildrenChanged;
        }

        public ExpressionViewModel(TextFormatting textFormat, params MathViewModel[] children) : this(children)
        {
            TextFormat = textFormat;
        }

        public override void Lyse() => Lyse(this);

        private void ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (MathViewModel mvm in e.NewItems)
                {
                    mvm.Parent = this;
                }
            }
        }

        public override IEnumerator<TextViewModel> GetEnumerator()
        {
            foreach (MathViewModel mvm in this)
            {
                if (mvm is TextViewModel text)
                {
                    yield return text;
                }
                else if (mvm is MathLayoutViewModel layout)
                {
                    foreach (TextViewModel text1 in layout)
                    {
                        yield return text1;
                    }
                }
            }
        }
    }

    public class RadicalViewModel : MathLayoutViewModel
    {
        public ExpressionViewModel Root { get; set; }
        public ExpressionViewModel Radicand { get; set; }

        public override ExpressionViewModel InputContinuation => Radicand;

        public override void Lyse() => Lyse(Root, Radicand);

        public override IEnumerator<TextViewModel> GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public class FractionViewModel : MathLayoutViewModel
    {
        public ExpressionList Numerator { get; set; }
        public ExpressionList Denominator { get; set; }

        public override ExpressionViewModel InputContinuation => null;

        public override void Lyse() { }// => Lyse(Numerator, Denominator);

        public override IEnumerator<TextViewModel> GetEnumerator()
        {
            throw new NotImplementedException();
        }
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

        public PartialObservableLinkedList<MathViewModel> Children { get; }

        private LinkedList<MathViewModel> List;
        private LinkedListNode<MathViewModel> CursorNode;

        //private ExpressionViewModel Expression;
        private MathViewModel Cursor;

        public MathEntryViewModel()
        {
            Children = new PartialObservableLinkedList<MathViewModel>(List = new LinkedList<MathViewModel>());
            Cursor = new CursorViewModel();

            List.AddFirst(CursorNode = new LinkedListNode<MathViewModel>(Cursor));

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
                    SoftKeyboard.Left();
                    //MathField.CursorPosition--;
                }
            });
        }

        public static MathViewModel LeftParenthesis() => new ImageTextViewModel { Text = "(" };
        public static MathViewModel RightParenthesis() => new ImageTextViewModel { Text = ")" };

        private ExpressionViewModel Expression => Cursor.Parent;

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

            MathViewModel[] list;
            if (str == "(")
            {
                list = new MathViewModel[] { LeftParenthesis() };
            }
            else if (str == ")")
            {
                list = new MathViewModel[] { RightParenthesis() };
            }
            else
            {
                list = Reader.Render(Crunch.Machine.StringClassification.Simple(str));
            }

            /*if (list[0] is FractionViewModel fraction && fraction.Numerator.Children.Count == 0)
            {
                fraction.Numerator.Fill(Expression.Children, Index - 1);
                fraction.Numerator.Trim();
                Index -= fraction.Numerator.Children.Count;
            }*/

            foreach (MathViewModel mvm in list)
            {
                LinkedListNode<MathViewModel> node = new LinkedListNode<MathViewModel>(mvm);

                if (mvm is FractionViewModel fraction)// && fraction.Numerator.Children.Count == 0)
                {
                    LinkedListNode<MathViewModel> open = new LinkedListNode<MathViewModel>(new TextViewModel
                    {
                        Text = "("
                    });

                    fraction.Numerator = new ExpressionList(List, open, node);

                    LinkedListNode<MathViewModel> start = SoftKeyboard.BeginningOfPreviousMathObject(List, CursorNode);
                    List.AddBefore(start, open);

                    Children.Remove(start);
                }

                //Expression.Children.Insert(Index++, mvm);
                Children.AddBefore(CursorNode, node);
            }

            //LinkedListNode<MathViewModel> node = CursorNode.Previous;
            for (int i = 0; i < list.Length; i++)
            {
                
            }
            Index += list.Length;

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
            if (Index == 0)
            {
                if (Cursor.Parent.Parent != null)
                {
                    Cursor.Parent.Lyse();
                }
                //Index = Cursor.Index();
            }
            //Otherwise just delete the thing before
            else
            {
                Index--;
                //deleted = Expression.Children[Index].ToString();
                Expression.Children.RemoveAt(Index);
            }

            //Deleted?.Invoke(deleted);

            //Cursor.Parent.OnInputChanged();

            return true;
        }

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