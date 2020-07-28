using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Xamarin.Forms.MathDisplay
{
    public class MathElementTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate ImageTextTemplate { get; set; }

        public DataTemplate ExpressionTemplate { get; set; }
        public DataTemplate FractionTemplate { get; set; }
        public DataTemplate RadicalTemplate { get; set; }
        private DataTemplate CursorTemplate = new DataTemplate(() => SoftKeyboard.Cursor);

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if (item is ExpressionViewModel)
            {
                return ExpressionTemplate;
            }
            if (item is FractionViewModel)
            {
                return FractionTemplate;
            }
            else if (item is RadicalViewModel)
            {
                return RadicalTemplate;
            }
            else if (item is CursorViewModel)
            {
                return CursorTemplate;
            }
            else if (item is ImageTextViewModel)
            {
                return ImageTextTemplate;
            }
            else
            {
                return TextTemplate;
            }
        }
    }
}