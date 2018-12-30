﻿using System;
using System.Collections.Generic;
using System.Text;

using Xamarin.Forms;
using Xamarin.Forms.Extensions;

namespace Crunch.GraphX
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
                parent.InputChanged -= change;
            }
            
            change();

            if (Parent != null)
            {
                Parent.InputChanged += change;
                parent = Parent;
            }
        }

        private void change()
        {
            if (Parent != null)
            {
                int index = this.Index();
                Text previous;
                if (Parent.IndexOf(this) == 0 || (previous = Parent.ChildBefore(index) as Text) != null && (Machine.StringClassification.IsOperand(previous.Text.Trim()) || previous.Text.Trim() == "("))
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
                parent.InputChanged -= change;
            }
        }
    }
}