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
            ExtendedCSV deviceAudit = new ExtendedCSV(new CSV(new FileStream(@"C:\Users\admin\Downloads\studentDeviceAudit.csv", FileMode.Open)), new List<string>() { "Email" });
            CSV deviceRecords = new CSV(new FileStream(@"C:\Users\admin\Downloads\studentDeviceRecords.csv", FileMode.Open));

            CSV responses = new CSV(new FileStream(@"C:\Users\admin\Downloads\responses.csv", FileMode.Open));

            Regex serialNumberExp = new Regex("[A-Z0-9]{12}");

            foreach(Row response in responses)
            {
                if (!serialNumberExp.IsMatch(response["iPad Serial Number"].ToUpperInvariant()))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("{0} is not a valid Serial Number", response["iPad Serial Number"]);
                    Console.ForegroundColor = ConsoleColor.White;
                    continue;
                }

                response["iPad Serial Number"] = serialNumberExp.Match(response["iPad Serial Number"].ToUpperInvariant()).Value;

                if (!deviceAudit.Any(row => row["Email"].ToLowerInvariant().Equals(response["Email"].ToLowerInvariant())))
                {

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("{0} does not appear in the Audit report?", response["Full Name"]);
                    Console.ForegroundColor = ConsoleColor.White;

                    deviceRecords.Add(new Row()
                    {
                        { "User Name", response["Email"].Split('@')[0] },
                        { "Full Name", response["Full Name"] },
                        { "Serial Number", response["iPad Serial Number"] },
                        { "Asset Tag", "" },
                        { "Device Type", "iPad" }
                    });

                    continue;
                }

                bool found = true;
                Row auditRecord = deviceAudit.Where(row => row["Email"].ToLowerInvariant().Equals(response["Email"].ToLowerInvariant())).Single();
                if (string.IsNullOrEmpty(auditRecord["iPad Serial Number"]))
                {
                    auditRecord["iPad Serial Number"] = response["iPad Serial Number"];
                    auditRecord["Has iPad"] = "TRUE";
                    deviceRecords.Add(new Row()
                    {
                        { "User Name", auditRecord["Username"] },
                        { "Full Name", auditRecord["Full Name"] },
                        { "Serial Number", auditRecord["iPad Serial Number"] },
                        { "Asset Tag", "" },
                        { "Device Type", "iPad" }
                    });

                    Console.WriteLine("Identified missing student record:  {0} has {1}", auditRecord["Full Name"], auditRecord["iPad Serial Number"]);
                }
                else if (response["iPad Serial Number"].Equals(auditRecord["iPad Serial Number"]))
                {
                    auditRecord["Confirmed iPad"] = "TRUE";
                    Console.WriteLine("Confirmed iPad {0} for {1}", auditRecord["iPad Serial Number"], auditRecord["Full Name"]);
                }
                else
                    found = false;
                
                if(!found && !string.IsNullOrEmpty(auditRecord["iPad 2 Serial Number"]))
                {
                    Console.WriteLine("Student records indicate more than one iPad is assigned");

                    if (response["iPad Serial Number"].Equals(auditRecord["iPad 2 Serial Number"]))
                    {
                        auditRecord["Confirmed iPad"] = "TRUE";
                        Console.WriteLine("Confirmed iPad {0} for {1}", auditRecord["iPad 2 Serial Number"], auditRecord["Full Name"]);

                        string stemp = auditRecord["iPad Serial Number"];
                        string atemp = auditRecord["iPad Asset Tag"];

                        auditRecord["iPad Serial Number"] = auditRecord["iPad 2 Serial Number"];
                        auditRecord["iPad Asset Tag"] = auditRecord["iPad 2 Asset Tag"];

                        auditRecord["iPad 2 Serial Number"] = stemp;
                        auditRecord["iPad 2 Asset Tag"] = atemp;

                        auditRecord["Second iPad Extra"] = "TRUE";
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine("Neither iPad record matches the student's reported Serial Number");
                        Console.ForegroundColor = ConsoleColor.White;
                        auditRecord["Reported iPad Serial Number"] = response["iPad Serial Number"].ToUpperInvariant();
                        deviceRecords.Add(new Row()
                        {
                            { "User Name", auditRecord["Username"] },
                            { "Full Name", auditRecord["Full Name"] },
                            { "Serial Number", response["iPad Serial Number"] },
                            { "Asset Tag", "" },
                            { "Device Type", "iPad" }
                        });
                    }
                }
                else if(!found)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("iPad record does not match the student's reported Serial Number");
                    Console.ForegroundColor = ConsoleColor.White;
                    auditRecord["Reported iPad Serial Number"] = response["iPad Serial Number"];
                    deviceRecords.Add(new Row()
                    {
                        { "User Name", auditRecord["Username"] },
                        { "Full Name", auditRecord["Full Name"] },
                        { "Serial Number", response["iPad Serial Number"] },
                        { "Asset Tag", "" },
                        { "Device Type", "iPad" }
                    });
                }
            }

            deviceAudit.Save(@"C:\Users\admin\Downloads\deviceAuditMerged.csv");
            deviceRecords.Save(@"C:\Users\admin\Downloads\deviceRecordsMerged.csv");

            Console.WriteLine("Done!");
            Console.ReadKey();
        }



        protected static void StudentDeviceAudit()
        {
            CSV jamfData = new CSV(new FileStream(@"C:\Users\admin\Downloads\jamfData.csv", FileMode.Open));
            CSV waspData = new CSV(new FileStream(@"C:\Users\admin\Downloads\waspData.csv", FileMode.Open));

            Regex classDept = new Regex("Class (I){1,3}");

            ExtendedCSV studentDeviceAudit = new ExtendedCSV(new CSV(), new List<string>() { "Username" });

            CSV studentDeviceRecords = new CSV();

            foreach (Row jamfRow in jamfData)
            {
                if (!classDept.IsMatch(jamfRow["Department"]) || string.IsNullOrEmpty(jamfRow["Asset Tag"]))
                    continue;
                if (!studentDeviceRecords.Any(dev => dev["Asset Tag"].Equals(jamfRow["Asset Tag"])))
                {
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

                Console.WriteLine("Found {0} assigned to {1}", jamfRow["Asset Tag"], jamfRow["Full Name"]);

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