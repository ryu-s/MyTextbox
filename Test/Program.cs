using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyLibrary
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] arr = MyLibrary.MyStringTool.StringSplitter("テスト\nえ", "\r");
            Console.ReadKey();
        }
    }
}
