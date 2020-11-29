using System;
using System.Collections.Generic;
using System.Text;

using Xamarin.Forms.Extensions;

namespace Xamarin.Forms.MathDisplay
{
    public class Minus : Text
    {
        private Expression parent;

        public Minus() : base()
        {
            Text = " - ";
        }

        protected override void OnParentSet()
        {
            base.OnParentSet();

            if (parent != null)
            {
                parent.ChildAdded -= change;
                parent.ChildRemoved -= change;
            }
            
            change();

            if (Parent != null)
            {
                Parent.ChildAdded += change;
                Parent.ChildRemoved += change;
                parent = Parent;
            }
        }

        private void change(object sender, ElementEventArgs e) => change();

        private void change()
        {
            if (Parent != null)
            {
                int index = this.Index();
                object previous;
                if (Parent.IndexOf(this) == 0 || ((previous = Parent.ChildBefore(index)) != null && (Crunch.Machine.StringClassification.IsOperator(previous.ToString().Trim()) || previous.ToString().Trim() == "(" || (previous is Expression && (previous as Expression).TextFormat == TextFormatting.Subscript))))
                {
                    Text = "-";
                }
                else
                {
                    Text = " - ";
                }
            }
            else if (parent != null)
            {
                parent.ChildAdded -= change;
                parent.ChildRemoved -= change;
            }
        }

        public override string ToString() => Text;
    }
}
