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

            MathView.Content.Children.Add(Cursor);
            //entry.CursorMoved += CursorPositionChanged;
            //CursorPositionChanged(entry, new ChangedEventArgs<int>(0, 0));
        }

        private void CursorPositionChanged(object sender, ChangedEventArgs<int> e)
        {
            /*if (e.PropertyName != MathEntryViewModel.CursorPositionProperty.PropertyName)
            {
                return;
            }*/

            if (MathEntryModel.Text.Length == 0 || MathEntryModel.IndexOf(CursorModel) != e.NewValue)
            //if (MathEntryModel.Current != CursorModel)
            {
                MathEntryModel.CursorMoved -= CursorPositionChanged;

                MathEntryModel.CursorPosition = e.OldValue + 1;
                MathEntryModel.Delete();
                MathEntryModel.CursorPosition = e.NewValue;
                MathEntryModel.Insert(CursorModel);
                MathEntryModel.CursorPosition--;

                MathEntryModel.CursorMoved += CursorPositionChanged;
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
                entry.CursorMoved += (sender, e) =>
                {

                };

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