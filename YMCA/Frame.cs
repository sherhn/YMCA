using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YMCA
{
    internal class Frame
    {
        public int Index { get; set; }
        public List<List<int>> Pixels { get; set; }

        public Frame(int index, List<List<int>> pixels)
        {
            Index = index;
            Pixels = pixels;
        }
    }
}
