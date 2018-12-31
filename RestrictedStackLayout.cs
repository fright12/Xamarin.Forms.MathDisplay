using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms.Extensions;

namespace Xamarin.Forms.MathDisplay
{
    public abstract class ImmutableStackLayout
    {
        protected TouchableStackLayout InternalLayout;
        public IReadOnlyList<View> Children => (IReadOnlyList<View>)InternalLayout.Children;

        public void InsertIn(int index, ref IList<View> list) => list.Insert(index, InternalLayout);

        protected void Initialize(TouchableStackLayout layout)
        {
            if (InternalLayout == null)
            {
                InternalLayout = layout;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MathLayout can only be initialized once");
            }
        }
    }
}
