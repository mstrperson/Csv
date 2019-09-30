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
            CSV temp = new CSV();
            MACAddressNormalizationRule rule = new MACAddressNormalizationRule() { Separator = MACAddressNormalizationRule.MacSeparator.None, Capitalize = false };

            foreach (Row row in ipads)
            {
                Row aeRow = new Row()
                {
                    { "User Name", row["Wi-Fi MAC Address"] },
                    { "User Type", "1" },
                    { "User Group", "IPad Student" },
                    { "Password", row["Wi-Fi MAC Address"] },
                    { "Email Address", "" },
                    { "Description", row["Display Name"] }
                };
                Console.WriteLine(row);
                temp.Add(aeRow);
            }

            ExtendedCSV aerohiveOutput = new ExtendedCSV(temp, new List<string>() { "Password" });
            aerohiveOutput.NormalizeColumns(rule, new List<string>() { "User Name", "Password" });

            aerohiveOutput.Save("C:\\Users\\jcox\\Documents\\LowerSchool iPad Assignments\\aerohiveImport.csv");
            Console.WriteLine("Done!");
            Console.ReadKey();
        }
    }
}
