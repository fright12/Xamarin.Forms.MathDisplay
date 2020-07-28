using System;
using System.Extensions;
using Xamarin.Forms.Xaml;

namespace Xamarin.Forms.MathDisplay
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MathEntry : AbsoluteLayout
    {
        public MathEntry()
        {
            InitializeComponent();
        }

        private void CursorMoved(object sender, ChangedEventArgs<int> e)
        {
            Action move = e.NewValue > e.OldValue ? SoftKeyboard.Right : (Action)SoftKeyboard.Left;

            for (int i = 0; i < Math.Abs(e.NewValue - e.OldValue); i++)
            {
                move();
            }
        }
    }
}