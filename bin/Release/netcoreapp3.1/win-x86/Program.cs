using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PlattsParser
{
    class Program
    {
        static void Main(string[] args)
        {
            var currentDir = Directory.GetCurrentDirectory();

            string[] paths = Directory.GetFiles($@"{currentDir}\RawFiles", "*.pdf");
            if (paths.Length == 0)
            {
                throw new FileNotFoundException();
            }

            List<string> neededPlatts = GetNeededPlatts(currentDir);
            var filteredTokens = new List<List<string>>();
            var BuletinDate = "";

            foreach (string path in paths)
            {
                string fileContent = PDFConvert(path);

                if (BuletinDate == "")
                {
                    BuletinDate = GetBuletinDate(fileContent);
                    filteredTokens.Add(new List<string>() { BuletinDate });
                }

                MatchCollection matches = Regex.Matches(fileContent, "([A-Z]+[0]{2})[0-9\\.\\-\\–\\s\\+]+");

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
        }

        private static Exception FileNotFoundException()
        {
            throw new NotImplementedException();
        }

        private static string GetBuletinDate(string fileContent)
        {
            var result = "";
            result = Regex.Matches(fileContent.Split('/')[2], @"[\w]+\s[0-9]+,\s[0-9]+").First().Value;
            var resultArray = result.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).ToArray();

            return result = resultArray[1] + "." + GetMonthAsNumber(resultArray[0]) + "." + resultArray[2];

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
