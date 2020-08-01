using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Xamarin.Forms.MathDisplay
{
    [ContentProperty(nameof(Templates))]
    public class DictionaryTemplateSelector : DataTemplateSelector
    {
        public IDictionary<Type, DataTemplate> Templates { get; set; } = new Dictionary<Type, DataTemplate>();

        public DataTemplate DefaultTemplate { get; set; } = new DataTemplate(() =>
        {
            Label label = new Label();
            label.SetBinding(Label.TextProperty, ".");
            return label;
        });

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            DataTemplate template;
            if (Templates.TryGetValue(item.GetType(), out template))
            {
                return template;
            }

            return DefaultTemplate;
        }
    }

    public class MathElementTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate ImageTextTemplate { get; set; }

        public DataTemplate ExpressionTemplate { get; set; }
        public DataTemplate FractionTemplate { get; set; }
        public DataTemplate RadicalTemplate { get; set; }
        private DataTemplate CursorTemplate { get; set; }

        private readonly CursorView Cursor;

        public MathElementTemplateSelector()
        {
            Cursor = new CursorView();
            Cursor.SetDynamicResource(VisualElement.BackgroundColorProperty, "DetailColor");
            Cursor.MeasureInvalidated += (sender, e) => Cursor.HeightRequest = Text.MaxTextHeight * ((Cursor as View).Parent as Expression).FontSize / Text.MaxFontSize;

            CursorTemplate = new DataTemplate(() => Cursor);
        }

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