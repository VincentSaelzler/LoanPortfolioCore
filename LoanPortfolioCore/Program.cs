using LoanPortfolioCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LoanPortfolioCore
{
    class Program
    {
        private static IEnumerable<Loan> loans { get; set; }
        private static IEnumerable<Month> months { get; set; }
        private static IEnumerable<Strategy> strategies { get; set; }

        static void Main(string[] args)
        {

            PopulateDimensions(new DateTime(2019,6,1));


            Console.ReadLine();
        }

        private static void PopulateDimensions(DateTime beginDate)
        {

            loans =  new Loan[] {
                new Loan() { LoanId = 1, LoanName = "Sample 10 Year", MinPayment = 222.04M, Principal = 20000, Rate = 0.06, TermInMonths = 120 },
                new Loan() { LoanId = 2, LoanName = "Sample 5 Year", MinPayment = 184.17M, Principal = 10000, Rate = 0.04, TermInMonths = 60 }
                };
            strategies = new Strategy[] {
                new Strategy() { StrategyId = 1, ExtraPerMonth = 0, SortOrder = "Highest Rate First", StrategyName = "HR 0" },
                new Strategy() { StrategyId = 2, ExtraPerMonth = 100, SortOrder = "Highest Rate First", StrategyName = "HR 100" },
                new Strategy() { StrategyId = 3, ExtraPerMonth = 200, SortOrder = "Highest Rate First", StrategyName = "HR 200" },
                new Strategy() { StrategyId = 4, ExtraPerMonth = 0, SortOrder = "Lowest Balance First", StrategyName = "LB 0" },
                new Strategy() { StrategyId = 5, ExtraPerMonth = 100, SortOrder = "Lowest Balance First", StrategyName = "LB 100" },
                new Strategy() { StrategyId = 6, ExtraPerMonth = 200, SortOrder = "Lowest Balance First", StrategyName = "LB 200" }
                };

            IList<Month> monthList = new List<Month>();
            for (int i = 0; i < 360; i++)
            {
                monthList.Add(new Month() { MonthId = i + 1, Date = beginDate.AddMonths(i) });
            }
            months = monthList;
        }

    }
}
