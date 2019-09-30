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
            CSV ipads = new CSV(new FileStream(@"C:\Users\jcox\Documents\LowerSchool iPad Assignments\wifiMac.csv", FileMode.Open));
            ipads.Heading = "WiFi MAC Addresses";
            string tableHtml = ipads.HtmlTable();
            string json = ipads.JsonString;

            StreamWriter output = new StreamWriter(new FileStream("C:\\Users\\jcox\\Documents\\test2.html", FileMode.CreateNew));
            output.AutoFlush = true;

            output.WriteLine("<html><head><script>{1}</script></head><body>{0}</body></html>", tableHtml, json);
            output.Close();

            Console.WriteLine("Done!");
            Console.ReadKey();
        }
    }
}
