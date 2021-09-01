using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Csv;

namespace Program
{
    class Program
    {
        static void Main(string[] args)
        {
            // Open a CSV File that is saved on  your computer
            CSV contacts = new CSV(new FileStream("C:\\Users\\jcox\\Documents\\cheqroomContacts.csv", FileMode.Open));
            
            // go through each row in the file
            foreach (Row row in contacts)
            {
                if (row["some-column-header"].Equals("What you're looking for..."))
                {
                    row["some-other-column-header"] = "the value you want!";
                }
            }

            // save the file to a DIFFERENT file name.
            // (it will not allow you to overwrite the file you originally opened--I did that on purpose)
            contacts.Save("C:\\Users\\jcox\\Documents\\contactsImport.csv");

            Console.WriteLine("Done!");
            Console.ReadKey();
        }

        // here's some code that uses multiple CSVs and has some merge stuff going on.
        protected static void StudentDeviceAudit()
        {
            CSV jamfData = new CSV(new FileStream(@"C:\Users\admin\Downloads\jamfData.csv", FileMode.Open));
            CSV waspData = new CSV(new FileStream(@"C:\Users\admin\Downloads\waspData.csv", FileMode.Open));

            // matches "Class I", "Class II", or "Class III"
            Regex classDept = new Regex("Class (I){1,3}");

            // CSV with some extra stuff... don't worry about this for now.
            ExtendedCSV studentDeviceAudit = new ExtendedCSV(new CSV(), new List<string>() { "Username" });

            // new CSV to put info into
            CSV studentDeviceRecords = new CSV();

            foreach (Row jamfRow in jamfData)
            {
                // if it isn't a Class I, II, III computer, skip it!
                if (!classDept.IsMatch(jamfRow["Department"]) || string.IsNullOrEmpty(jamfRow["Asset Tag"]))
                    continue;
                
                // if the studentDeviceRecords does not contain a device with this asset tag...
                if (!studentDeviceRecords.Any(dev => dev["Asset Tag"].Equals(jamfRow["Asset Tag"])))
                {
                    // add a new row with that Asset Tag
                    studentDeviceRecords.Add(new Row()
                    {
                        { "User Name", jamfRow["Username"] },
                        { "Full Name", jamfRow["Full Name"] },
                        { "Serial Number", jamfRow["Serial Number"] },
                        { "Asset Tag", jamfRow["Asset Tag"] },
                        { "Device Type", jamfRow["Device Type"].Equals("Computer")?"Laptop":"iPad" }
                    });
                }
                else
                {
                    Console.WriteLine("I found the same asset tag listed twice? {0}", jamfRow["Asset Tag"]);
                }

                // print some nice debug messages to the console.
                Console.WriteLine("Found {0} assigned to {1}", jamfRow["Asset Tag"], jamfRow["Full Name"]);

                
                // don't worry about this section..... ;\
                Row studentRow = new Row();

                try
                {
                    studentRow = studentDeviceAudit.Find(new Row() { { "Username", jamfRow["Username"] } });
                }
                catch (KeyNotFoundException)
                {
                    studentRow["Username"] = jamfRow["Username"];
                    studentRow["Email"] = jamfRow["Email Address"];
                    studentRow["Full Name"] = jamfRow["Full Name"];
                    studentRow["Class"] = jamfRow["Department"];
                    studentRow["Has iPad"] = "FALSE";
                    studentRow["Has Laptop"] = "FALSE";
                    studentDeviceAudit.Add(studentRow);
                }

                string deviceHeader;

                switch (jamfRow["Device Type"])
                {
                    case "Mobile Device":
                        deviceHeader = "iPad";
                        studentRow["Has iPad"] = "TRUE";
                        break;
                    case "Computer":
                        deviceHeader = "Laptop";
                        studentRow["Has Laptop"] = "TRUE";
                        break;
                    default: deviceHeader = "ERROR"; break;
                }

                string serialHeader = string.Format("{0} Serial Number", deviceHeader);
                string assetTagHeader = string.Format("{0} Asset Tag", deviceHeader);
                int count = 1;
                while (!string.IsNullOrEmpty(studentRow[serialHeader]))
                {
                    studentRow["Too Many Devices"] = "X";
                    serialHeader = string.Format("{0} {1} Serial Number", deviceHeader, ++count);
                    assetTagHeader = string.Format("{0} {1} Asset Tag", deviceHeader, count);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Too many devices assigned to this student!!");
                    Console.ForegroundColor = ConsoleColor.White;
                }

                string serialNumber = jamfRow["Serial Number"];
                string assetTag = jamfRow["Asset Tag"];

                studentRow[serialHeader] = serialNumber;
                studentRow[assetTagHeader] = assetTag;

            }

            CSV waspConflicts = new CSV();

            foreach (Row waspRow in waspData)
            {
                if (!classDept.IsMatch(waspRow["Location"]))
                    continue;

                if (!waspRow["Asset Tag"].ToLowerInvariant().StartsWith("lapt") && !waspRow["Asset Tag"].ToLowerInvariant().StartsWith("ipad"))
                    continue;

                string assetTag = waspRow["Asset Tag"];
                string serialNumber = waspRow["Serial Number"];

                if (studentDeviceRecords.Any(record => record["Asset Tag"].Equals(assetTag)))
                {
                    Row recordRow = studentDeviceRecords.Where(record => record["Asset Tag"].Equals(assetTag)).Single();

                    if (!recordRow["Serial Number"].Equals(serialNumber))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("{0} has a Serial Number mismatch....", assetTag);
                        Console.WriteLine("Jamf:  {0}\nWasp:  {1}", recordRow["Serial Number"], serialNumber);
                        Console.ForegroundColor = ConsoleColor.White;

                        if (waspData.Any(check => check["Serial Number"].Equals(recordRow["Serial Number"])))
                        {
                            Row waspSNRow = waspData.Where(check => check["Serial Number"].Equals(recordRow["Serial Number"])).Single();

                            waspConflicts.Add(new Row()
                            {
                                { "Asset Tag", waspSNRow["Asset Tag"] },
                                { "Jamf Asset Tag", recordRow["Asset Tag"] },
                                { "Jamf Serial Number", recordRow["Serial Number"] },
                                { "Wasp Serial Number", waspSNRow["Serial Number"] }
                            });
                        }

                        if (!waspConflicts.Any(conflict => conflict["Asset Tag"].Equals(assetTag)))
                        {
                            waspConflicts.Add(new Row()
                            {
                                { "Asset Tag", assetTag },
                                { "Jamf Serial Number", recordRow["Serial Number"] },
                                { "Wasp Serial Number", serialNumber }
                            });
                        }
                    }

                    if (!string.IsNullOrEmpty(waspRow["Description"]) && !waspRow["Description"].Equals(recordRow["Full Name"]))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("{0} has a student assignment mismatch...", assetTag);
                        Console.WriteLine("Jamf:  {0}\nWasp:  {1}", recordRow["Full Name"], waspRow["Description"]);
                        Console.ForegroundColor = ConsoleColor.White;

                        Console.WriteLine("Is this Really a mistake? [y/N]");
                        bool real = Console.ReadLine().ToLowerInvariant().StartsWith("y");

                        if (real && !waspConflicts.Any(conflict => conflict["Asset Tag"].Equals(assetTag)))
                        {
                            waspConflicts.Add(new Row()
                            {
                                { "Asset Tag", assetTag },
                                { "Jamf Serial Number", recordRow["Serial Number"] },
                                { "Wasp Serial Number", serialNumber },
                                { "Jamf Assigned Student", recordRow["Full Name"] },
                                { "Wasp Description", waspRow["Description"] }
                            });
                        }
                        else if (real)
                        {
                            Row conflictRow = waspConflicts.Where(conflict => conflict["Asset Tag"].Equals(assetTag)).Single();
                            conflictRow["Jamf Assigned Student"] = recordRow["Full Name"];
                            conflictRow["Wasp Descriptoin"] = waspRow["Description"];
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Found missing student record {0} assigned to {1}", assetTag, waspRow["Description"]);

                    studentDeviceRecords.Add(new Row()
                    {
                        { "Asset Tag", assetTag },
                        { "Serial Number", serialNumber },
                        { "Full Name", waspRow["Description"] },
                        { "Device Type", waspRow["Asset Type"].Contains("iPad")?"iPad":"Laptop" }
                    });
                }
            }

            waspConflicts.Save(@"C:\Users\admin\Downloads\waspConflicts.csv");
            studentDeviceAudit.Save(@"C:\Users\admin\Downloads\studentDeviceAudit.csv");
            studentDeviceRecords.Save(@"C:\Users\admin\Downloads\studentDeviceRecords.csv");
        }
    }
}