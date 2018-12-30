using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace Crunch.GraphX
{
    public interface IMathView
    {
        double Middle { get; }
        double FontSize { set; }

        string ToLatex();
    }
}
