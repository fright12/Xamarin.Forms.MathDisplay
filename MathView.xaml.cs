using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Xamarin.Forms.MathDisplay
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MathView : ContentView
    {
        public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(Expression));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public double FontSize
        {
            get => Content.FontSize;
            set => Content.FontSize = value;
        }

        new public Expression Content => base.Content as Expression;

        public MathView()
        {
            InitializeComponent();

            this.SetBinding(TextProperty, Content.BindingContext, "Text", mode: BindingMode.TwoWay);
        }

        public override string ToString() => Content?.ToString() ?? base.ToString();
    }

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

    public class MathSymbolConverter : IValueConverter
    {
        private readonly Dictionary<string, string> Map = new Dictionary<string, string>
        {
            ["*"] = " × ",
            ["+"] = " + ",
            ["="] = " = ",
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string converted;
            if (value is string str && Map.TryGetValue(str, out converted))
            {
                return converted;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
    }

    public class MathTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultTemplate { get; set; }

        public DataTemplate TextTemplate { get; set; }
        public DataTemplate ImageTextTemplate { get; set; }

        public DataTemplate ExpressionTemplate { get; set; }
        public DataTemplate FractionTemplate { get; set; }
        public DataTemplate RadicalTemplate { get; set; }

        public CursorView Cursor { get; set; }
        private DataTemplate CursorTemplate;

        public MathTemplateSelector()
        {
            CursorTemplate = new DataTemplate(() => Cursor);
        }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if (item.ToString() == "|")
            {
                return CursorTemplate;
            }

            if (item is TextViewModel text)
            {
                if (text.Text == "(" || text.Text == ")" || text.Text == "sqrt")
                {
                    return ImageTextTemplate;
                }
                else
                {
                    return TextTemplate;
                }
            }
            else if (item is ExpressionViewModel)
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
            else
            {
                return DefaultTemplate;
            }
        }
    }
}