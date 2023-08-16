using iTextSharp.text;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using static iTextSharp.text.pdf.AcroFields;

namespace PlattsParser
{
    class Program
    {
        static void Main(string[] args)
        {
            //Only in production mode
            //var currentDir = AppDomain.CurrentDomain.BaseDirectory;

            //Debug mode
            var currentDir = "C:\\Users\\1\\Desktop\\errorPlats";

            string[] paths = Directory.GetFiles($@"{currentDir}\RawFiles", "*.pdf");
            if (paths.Length == 0)
            {
                throw new FileNotFoundException();
            }

            List<string> neededPlatts = GetNeededPlatts(currentDir);
            var filteredTokens = new List<List<string>>();
            var progressList = new List<string>();
            var buletinDate = "";

            try
            {
                foreach (string path in paths)
                {
                    progressList.Add(path.Split("\\").Last());

                    string fileContent = PDFConvert(path);

                    if (buletinDate == "")
                    {
                        buletinDate = GetBuletinDate(fileContent);
                        filteredTokens.Add(new List<string>() { buletinDate });
                    }
                    //Old
                    //MatchCollection matches = Regex.Matches(fileContent, "([A-Z]+[0]{2})[0-9\\.\\-\\–\\s\\+]+");
                    //new
                    MatchCollection matches = Regex.Matches(fileContent, "([A-Z]+[1]?[0]{1,2})[0-9\\.\\-–\\s\\+]+");

                    for (int i = 0; i < matches.Count; i++)
                    {
                        var matchToken = matches[i].Value.Split(null).Where(x => !string.IsNullOrEmpty(x)).ToList();

                        matchToken = RepairElementsPositions(matchToken);

                        var codes = filteredTokens.Select(x => x[0]).ToList();

                        if (neededPlatts.Contains(matchToken[0]) && !codes.Contains(matchToken[0]))
                        {
                            filteredTokens.Add(matchToken);
                        }
                    };

                    // Create and write to csv file
                    string strFilePath = $@"{currentDir}\TransformedFiles\ResultFile.csv";
                    string strSeperator = ",";

                    StringBuilder sbOutput = new StringBuilder();

                    for (int i = 0; i < filteredTokens.Count; i++)
                    {
                        sbOutput.AppendLine(string.Join(strSeperator, filteredTokens[i]));
                    }

                    File.WriteAllText(strFilePath, sbOutput.ToString());

                    //File.AppendAllText(strFilePath, sbOutput.ToString());
                }
                //throw new Exception("");
            }
            catch (Exception)
            {
                Console.WriteLine("-----------------------------------");
                Console.WriteLine("Прогрес в обработката на файлове:");
                var counter = 1;
                foreach (var item in progressList)
                {
                    Console.WriteLine(counter++ + ". " + item);
                }

                Console.WriteLine("-----------------------------------");
                Console.WriteLine("Прогрес в обработката на котировки:");
                counter = 0;
                foreach (var innerList in filteredTokens)
                {
                    if (counter > 0) //ignoring first iteration - date
                    {
                        Console.Write(counter + ". " + innerList.First());

                        Console.WriteLine();
                    }
                    counter++;
                }
                throw;
            }

            //Final check comparing results
            if (filteredTokens.Count - 1 != neededPlatts.Count)
            {
                var cuatationsCount = filteredTokens.Count - 1;
                Console.WriteLine("-----------------------------------");
                Console.WriteLine("Изведени са " + cuatationsCount + " от зададените " + neededPlatts.Count);

                var onlyCuatationsList = filteredTokens.Select(x=>x.First()).ToList();
                var diff = onlyCuatationsList.Except(neededPlatts);
                Console.WriteLine("Необработени котировки: " + String.Join(", ", diff));
            }
        }

        private static string GetBuletinDate(string fileContent)
        {
            try
            {
                Console.WriteLine(fileContent.Split('/')[2]);
                var result = fileContent.Split('/')[2];
                result = Regex.Matches(fileContent.Split('/')[2], @"[\w]+(\s+)?[0-9]+(\s+)?[,](\s+)?[0-9]+").First().Value;
                var resultArray = result.Split(new char[] { ' ', ',', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
                return result = resultArray[1] + "." + GetMonthAsNumber(resultArray[0]) + "." + resultArray[2];
            }
            catch (Exception)
            {
                Console.WriteLine("-----------------------------------");
                Console.WriteLine("Грешка при обработката на датата на бюлетина");
                throw;
            }
        }

        private static string GetMonthAsNumber(string monthAsText)
        {
            var result = monthAsText switch
            {
                "January" => "01",
                "February" => "02",
                "March" => "03",
                "April" => "04",
                "May" => "05",
                "June" => "06",
                "July" => "07",
                "August" => "08",
                "September" => "09",
                "October" => "10",
                "November" => "11",
                "December" => "12",
                _ => "Unexpected date format"
            };
            return result;
        }

        private static List<string> RepairElementsPositions(List<string> matchToken)
        {
            var repairedToken = new List<string>();
            if (matchToken.Count == 4)
            {
                var secondElement = matchToken[1].Split(new char[] { '-', '–' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                repairedToken.Add(matchToken[0]);
                repairedToken.AddRange(secondElement);
                repairedToken.Add(matchToken[2]);
                repairedToken.Add(matchToken[3]);
            }
            else if (matchToken.Count == 3)
            {
                matchToken.InsertRange(1, new List<string>() { "", "" });
                repairedToken = matchToken;
            }
            else
            {
                repairedToken = matchToken;
            }

            return repairedToken;
        }

        private static List<string> GetNeededPlatts(string currentDir)
        {
            string fileContent = File.ReadAllText($@"{currentDir}\RawFiles\AAA_CODES_TO_EXTRACT.txt");

            var lukPlatts = fileContent.Split(",").Where(x => !string.IsNullOrEmpty(x)).ToList();

            return lukPlatts;
        }

        private static string PDFConvert(string filePath)
        {
            return PDFParser.GetText(filePath);
        }

        private static string GetHTMLText(string filePath)
        {
            return PDFParser.GetHTMLText(filePath);
        }
    }
}
