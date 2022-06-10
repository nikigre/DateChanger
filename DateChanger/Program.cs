using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DateChanger
{
    internal class Program
    {
        static string[] formats = {
            "yyyy-MM-dd HH-mm-ss", //2001-01-01 13:55:33
            "yyyy-MM-dd HH.mm.ss", //2018-04-04 14.10.34 1750063386420681684_751130549
            "yyyy-MM-dd HH-mm-ss", //2018-09-21 20-31-26
            "yyyyMMdd-HHmmss",     //20210422-161921
            "yyyyMMdd_HHmmss",     //20210422_161921
            "yyyy_MM_dd_HH_mm_ss", //2017_12_26_18_01_39
        };

        static Dictionary<string, int> problemFiles = new Dictionary<string, int> { };
        static bool verbose = false;
        static bool skip = true;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("First argument must be a directory!");
                Environment.Exit(1);
            }

            foreach (var item in args)
            {
                if (item == "-v")
                    verbose = true;
                if (item == "-s")
                    skip = false;
                if (item == "-h")
                {
                    Console.WriteLine("Help: ");
                    Console.WriteLine("-v - Verbose mode. Print log.");
                    Console.WriteLine("-s - Do NOT skip files whose name does not include a date.");
                    Environment.Exit(0);
                }
            }

            if (Directory.Exists(args[0]))
            {
                ProcessDirectory(args[0]);
                PrintErrors();
            }
            else
            {
                Console.WriteLine("Directory does not exists or is not correct!");
                Environment.Exit(1);
            }

        }

        /// <summary>
        /// Returns only the date from the file name.
        /// </summary>
        /// <param name="fileName">FileName of a file</param>
        /// <returns>Only date</returns>
        private static string GetDate(string fileName)
        {
            Match a = Regex.Match(fileName, "[0-9]{4}-[0-9]{2}-[0-9]{2} [0-9]{2}.[0-9]{2}.[0-9]{2}");
            if (a.Success)
                return a.Value;

            a = Regex.Match(fileName, "[0-9]{4}[0-9]{2}[0-9]{2}.[0-9]{2}[0-9]{2}[0-9]{2}");
            if (a.Success)
                return a.Value;

            a = Regex.Match(fileName, "[0-9]{4}_[0-9]{2}_[0-9]{2}_[0-9]{2}_[0-9]{2}_[0-9]{2}");
            if (a.Success)
                return a.Value;

            return null;
        }

        /// <summary>
        /// The method that processes the directory.
        /// </summary>
        /// <param name="path">Path to the directory</param>
        /// <param name="depth">Depth of recursion</param>
        private static void ProcessDirectory(string path, int depth = 0)
        {
            for (int i = 0; i < depth; i++)
                Console.Write("\t");

            Console.WriteLine("Processing directory: " + path);

            //Processes all directories in the current directory
            foreach (var item in Directory.GetDirectories(path))
            {
                ProcessDirectory(item, depth + 1);
            }

            //Processes all files in the current directory
            foreach (var item in Directory.GetFiles(path))
            {
                ChangeTheDate(item, depth + 1);
            }
        }

        /// <summary>
        /// The method prepares date and sets it to a file.
        /// </summary>
        /// <param name="path">Path to file</param>
        /// <param name="depth">Depth of recursion</param>
        private static void ChangeTheDate(string path, int depth = 0)
        {
            string date = GetDate(Path.GetFileNameWithoutExtension(path));

            //We try to get the date from astring to the actual date.
            DateTime DateTimeDate;
            bool success = DateTime.TryParseExact(date, formats, null, System.Globalization.DateTimeStyles.None, out DateTimeDate);

            if (success)
            {
                //If date is in the future, we skip the file
                if (DateTimeDate > DateTime.Now)
                {
                    for (int i = 0; i < depth; i++)
                        Console.Write("\t");
                    Console.WriteLine("Date for file {0} is in the future ({1}), skipping!", Path.GetFileName(path), DateTimeDate);

                    problemFiles.Add(path, 1);

                    return;
                }

                //We cahnge the date
                ChangeTheActualDate(path, DateTimeDate, depth);

            }
            else
            {
                if (!skip)
                {
                    //We are asking user untill we get correct date
                    while (true)
                    {
                        for (int i = 0; i < depth; i++)
                            Console.Write("\t");

                        Console.Write("File {0} does not contain date. To which date shoud I set it to?(To skip, press Enter): ", Path.GetFileName(path));

                        string rez = Console.ReadLine();
                        if (rez != "")
                        {
                            date = GetDate(Path.GetFileNameWithoutExtension(rez));

                            success = DateTime.TryParseExact(date, formats, null, System.Globalization.DateTimeStyles.None, out DateTimeDate);

                            if (success)
                            {
                                ChangeTheActualDate(path, DateTimeDate, depth);
                                break;
                            }
                        }
                        else
                        {
                            problemFiles.Add(path, 2);
                            break;
                        }
                    }
                }
                else
                {
                    problemFiles.Add(path, 4);
                }
            }
        }

        /// <summary>
        /// The Method changes the date to a file.
        /// </summary>
        /// <param name="path">Path to a file</param>
        /// <param name="DateTimeDate">Date to set it to</param>
        /// <param name="depth">Depth of the recursion</param>
        private static void ChangeTheActualDate(string path, DateTime DateTimeDate, int depth = 0)
        {
            //If verbose is enabled, we print to the console
            if (verbose)
            {
                for (int i = 0; i < depth; i++)
                    Console.Write("\t");

                Console.WriteLine("File: {0}, setting date to: {1}", Path.GetFileName(path), DateTimeDate);
            }

            //We try to chage the date
            try
            {
                File.SetCreationTime(path, DateTimeDate);
                File.SetLastAccessTime(path, DateTimeDate);
                File.SetLastWriteTime(path, DateTimeDate);
            }
            catch (Exception)
            {
                for (int i = 0; i < depth; i++)
                    Console.Write("\t");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error setting date for file {0} ({1})", Path.GetFileName(path), DateTimeDate);
                Console.ResetColor();
                problemFiles.Add(path, 3);
            }
        }

        /// <summary>
        /// Prints files that were skipped for some reason
        /// </summary>
        private static void PrintErrors()
        {
            Console.WriteLine("\nFiles that were skipped:");
            foreach (var item in problemFiles)
            {
                switch (item.Value)
                {
                    case 1: Console.WriteLine("File {0} is in the future, skipped!", item.Key); break;
                    case 2: Console.WriteLine("File {0} does not contain date, user skipped it!", item.Key); break;
                    case 3: Console.WriteLine("Error setting date for file {0}, skipped!", item.Key); break;
                    case 4: Console.WriteLine("File {0} does not contain date, skipped!", item.Key); break;
                    default:
                        break;
                }
            }
        }
    }
}
