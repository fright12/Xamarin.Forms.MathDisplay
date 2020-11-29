using System;
using System.Collections;
using System.Extensions;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Xamarin.Forms.MathDisplay
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MathEntry : AbsoluteLayout
    {
        public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(Expression));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly BindableProperty SymbolTemplateSelectorProperty = BindableProperty.Create(nameof(SymbolTemplateSelector), typeof(MathTemplateSelector), typeof(MathEntry), defaultValueCreator: bindable => new MathTemplateSelector());

        public MathTemplateSelector SymbolTemplateSelector
        {
            get => (MathTemplateSelector)GetValue(SymbolTemplateSelectorProperty);
            set => SetValue(SymbolTemplateSelectorProperty, value);
        }

        public Expression Expression => MathView.Content;

        private CursorView Cursor => (CursorView)Resources["Cursor"];
        private CursorViewModel CursorModel;
        private int LocalIndex = 0;

        public MathEntry()
        {
            InitializeComponent();

            ((MathTemplateSelector)MathView.Resources["MathDataTemplateSelector"]).Cursor = Cursor;
            //(BindingContext as MathEntryViewModel).Children.Add(CursorObject);
            //(BindingContext as MathEntryViewModel).CursorPosition--;
            
            Cursor.MeasureInvalidated += (sender, e) => Cursor.HeightRequest = MathDisplay.Text.MaxTextHeight * ((Cursor as View).Parent as Expression).FontSize / MathDisplay.Text.MaxFontSize;
            this.SetBinding(TextProperty, "Text", BindingMode.TwoWay);
            //Cursor.SetBinding(IsVisibleProperty, "Focused");

            MathEntryViewModel entry = (MathEntryViewModel)BindingContext;
            CursorModel = new CursorViewModel(new Node());

            //CursorPositionChanged(entry, new System.ComponentModel.PropertyChangedEventArgs(MathEntryViewModel.CursorPositionProperty.PropertyName));
            //entry.PropertyChanged += CursorPositionChanged;
            MathView.Content.Children.Add(Cursor);
            //entry.CursorMoved += CursorPositionChanged;
            //CursorPositionChanged(entry, new ChangedEventArgs<int>(0, 0));

            //entry.RichTextChanged += RichTextChanged;
        }

        private int LastIndex = -1;

        private void CursorParentChanged(int index)
        {
            Cursor.Parent.Children[index].BindingContext = Cursor;
        }

        private void RichTextChanged(object sender, MathEntryViewModel.CharCollectionChangedEventArgs e)
        {
            MathEntryViewModel entry = (MathEntryViewModel)sender;
            
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                if (e.Index == entry.CursorPosition)
                {
                    MathView view = new MathView { BindingContext = e.Tree };
                    Cursor.Parent.Children.Insert(Cursor.Index(), view.Content.Children[0]);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                throw new NotImplementedException();
            }
        }

        private void CursorPositionChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != MathEntryViewModel.CursorPositionProperty.PropertyName)
            {
                return;
            }

            //if (MathEntryModel.Text.Length == 0 || MathEntryModel.IndexOf(CursorModel) != e.NewValue)
            //if (MathEntryModel.Current != CursorModel)
            if (MathEntryModel.Count == 0 || MathEntryModel[MathEntryModel.CursorPosition] != CursorModel)
            {
                MathEntryModel.PropertyChanged -= CursorPositionChanged;

                //MathEntryModel.CursorPosition = e.OldValue + 1;
                int index = LastIndex;
                if (index != -1)
                {
                    MathEntryModel.RemoveAt(index);
                }
                //MathEntryModel.CursorPosition = e.NewValue;
                MathEntryModel.Insert(MathEntryModel.CursorPosition, "", CursorModel);
                MathEntryModel.CursorPosition--;

                MathEntryModel.PropertyChanged += CursorPositionChanged;
                LastIndex = MathEntryModel.CursorPosition;
            }
        }

        /*private void CursorPositionChanging(object sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName != MathEntryViewModel.CursorPositionProperty.PropertyName || MathEntryModel.Current == CursorModel)
            {
                return;
            }

            //MathEntryModel.Remove(CursorModel, MathEntryModel.CursorPosition);
            //MathEntryModel.Delete();
            MathEntryModel.PropertyChanged += CursorPositionChanged;
        }

        private void CursorPositionChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            MathEntryModel.PropertyChanging -= CursorPositionChanging;
            MathEntryModel.PropertyChanged -= CursorPositionChanged;

            MathEntryModel.Insert(CursorModel);
            MathEntryModel.CursorPosition--;

            MathEntryModel.PropertyChanging += CursorPositionChanging;
        }*/

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            return;
            if (BindingContext is MathEntryViewModel entry)
            {
                //entry.Children.Add(CursorObject);

                entry.WhenPropertyChanged("CursorPosition", (sender, e) =>
                {
                    return;
                    /*if (!(Cursor.Parent?.BindingContext is ExpressionViewModel expression))
                    {
                        return;
                    }

                    int index = Cursor.Index();
                    expression.Children.RemoveAt(index);*/

                    //entry.Insert(CursorObject);
                    //entry.CursorPosition--;

                    /*LocalIndex += entry.CursorPosition - LastCursorPosition;

                    if (LocalIndex >= 0 && LocalIndex < Cursor.Parent.Children.Count)
                    {
                        if (Cursor.Parent.Children[LocalIndex] == Cursor)
                        {
                            return;
                        }

                        Cursor.Remove();
                        Cursor.Parent.Children.Insert(LocalIndex, Cursor);
                    }

                    LastCursorPosition = entry.CursorPosition;*/
                });
            }
        }

        public void Test()
        {
            Entry.Focus();
        }

        public bool IsEmpty() => Entry.Text == null || Entry.Text.Length == 0;

        private async void Blink()
        {
            while (true)
            {
                await System.Threading.Tasks.Task.Delay(500);
                //Cursor.Opacity = 1 - Cursor.Opacity;
                //Device.BeginInvokeOnMainThread(() => Cursor.IsVisible = false);
            }
        }
    }
}