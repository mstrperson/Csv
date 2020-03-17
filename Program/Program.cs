using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Csv;

namespace Program
{
    class Program
    {
        static void Main(string[] args)
        {
            for(int i = 1; i < 1000; i++)
            {
                decimal r = map(i, 1000, 1.0M, 3.8M);
                Dictionary<int, decimal> data = findFixedPoints(r);
            }

            Console.WriteLine("Done!");
            Console.ReadKey();
        }

        static decimal iterate(decimal x, decimal r)
        {
            return r * x * (1 - x);
        }

        static decimal map(int a, int amax, decimal min, decimal max)
        {
            decimal spread = max - min;
            decimal percentage = (decimal)a / (decimal)amax;
            return min + percentage * spread;
        }

        static Dictionary<int, decimal> findFixedPoints(decimal r, int iterations = 1000, decimal init = 0.4M)
        {
            Dictionary<int, decimal> data = new Dictionary<int, decimal>();
            decimal[] values = new decimal[iterations];
            values[0] = init;
            for (int i = 1; i < iterations; i++)
            {

                values[i] = iterate(values[i - 1], r);
            }

            for (int i = 0; i < iterations; i++)
            {
                data.Add(i, values[i]);
            }

            return data;
        }

        static bool isClose(double x1, double x2, double epsilon)
        {
            return Math.Abs(x1 - x2) < epsilon;
        }
    }
}
