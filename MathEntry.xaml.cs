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

        private readonly object CursorObject = "|";
        private int LocalIndex = 0;

        public MathEntry()
        {
            InitializeComponent();

            CursorView cursor = (CursorView)Resources["Cursor"];
            ((MathTemplateSelector)MathView.Resources["MathDataTemplateSelector"]).Cursor = cursor;
            //(BindingContext as MathEntryViewModel).Children.Add(CursorObject);
            //(BindingContext as MathEntryViewModel).CursorPosition--;

            cursor.MeasureInvalidated += (sender, e) => cursor.HeightRequest = MathDisplay.Text.MaxTextHeight * ((cursor as View).Parent as Expression).FontSize / MathDisplay.Text.MaxFontSize;
            this.SetBinding(TextProperty, "Text", BindingMode.TwoWay);
            //Cursor.SetBinding(IsVisibleProperty, "Focused");
        }

        private int LastCursorPosition = 0;

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            if (BindingContext is MathEntryViewModel entry)
            {
                //entry.Children.Add(CursorObject);
                entry.CursorPositionChanging += (sender, e) =>
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