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
           /* CSV test = new CSV(new FileStream(@"C:\Users\jcox\Downloads\nslds.csv", FileMode.Open));

            Console.WriteLine("NSLDS has {0} columns!", test.AllKeys.Count);

            CSV Identity = new CSV();
            foreach(Row row in test)
            {
                Row id = new Row()
                {
                    { "UNITID", row["UNITID"] },
                    { "OPEID", row["OPEID"] },
                    { "OPEID6", row["OPEID6"] },
                    { "INSTNM", row["INSTNM"] }
                };
                Identity.Add(id);
            }

            Identity.Save("C:\\Users\\jcox\\Downloads\\identity.csv");
            ExtendedCSV main = new ExtendedCSV(Identity, new List<string>() { "UNITID" });
            /*
            List<string> dataColumns = test.AllKeys.Where(key => !Identity.AllKeys.Contains(key)).ToList();

            List<List<string>> groupings = new List<List<string>>();

            List<string> currentGroup = new List<string>();
            
            foreach(string colName in dataColumns)
            {
                if(currentGroup.Count > 10 && !currentGroup.Last().Substring(0, 4).Equals(colName.Substring(0, 4)))
                {
                    groupings.Add(currentGroup);
                    currentGroup = new List<string>();
                }

                currentGroup.Add(colName);
            }

            groupings.Add(currentGroup);

            Console.WriteLine("There are {0} groupings", groupings.Count);
            */
            StreamWriter writer = new StreamWriter(new FileStream("C:\\Users\\jcox\\Downloads\\treasury_insert.sql", FileMode.Create));
            
            /*foreach(List<string> group in groupings)
            {
                int index = group.Last().IndexOf("_");
                string title = index > 0? group.Last().Substring(0, index): group.Last();
                if(tableNames.Contains(title))
                {
                    int count = 1;
                    while (tableNames.Contains(string.Format("{0}{1}", title, count)))
                        count++;

                    title = string.Format("{0}{1}", title, count);
                }
                tableNames.Add(title);
                Console.WriteLine("group nslds_{0} has {1} columns\n__________________________________", title, group.Count);
                //Console.ReadLine();
                CSV nslds_grp = new CSV();
                foreach(Row row in test)
                {
                    Row grpRow = new Row() { { "UNITID", row["UNITID"] } };

                    foreach(string colname in group)
                    {
                        grpRow.Add(colname, row[colname]);
                    }

                    nslds_grp.Add(grpRow);
                }

                nslds_grp.Save(string.Format("C:\\Users\\jcox\\Downloads\\nslds_{0}.csv", title));
                */
            //List<string> files = Directory.GetFiles("C:\\Users\\jcox\\Downloads").Where(f => f.Contains("nslds_")).ToList();
            //foreach (string fileName in files)
            //{

                //string title = fileName.Split('\\').Last().Split('.')[0];
                CSV nslds_grp = new CSV(new FileStream("C:\\Users\\jcox\\Downloads\\scorecard.csv", FileMode.Open));
                ExtendedCSV ext = new ExtendedCSV(nslds_grp, new List<string>() { "UNITID" });

            //string create = ext.MySQLCreateTableCommand("treasury");

                string cmd = ext.MySQLInsertCommand("scorecard");

                Console.WriteLine(cmd);
                writer.WriteLine(cmd);
                writer.Flush();
                Console.WriteLine("______________________________________");
                //Console.ReadLine();
            //}

            writer.Close();
            Console.WriteLine("Done!");
            Console.ReadKey();
        }
    }
}
