using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpaConsole {
    class Program {
        static void Main(string[] args) {
            while (true) {
                string tm = Console.ReadLine();
                long time = Int64.Parse(tm);
                DateTime dt = new DateTime(1970, 1, 1).AddSeconds(time).AddHours(5);
                Console.WriteLine(dt.ToString());
            }
            Console.ReadLine();
        }
    }
}
