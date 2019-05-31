using AutoMapper;
using FileHelpers;
using LoanPortfolioCore.FlatFileModels;
using LoanPortfolioCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LoanPortfolioCore
{
    class Program
    {
        private static IEnumerable<Loan> Loans { get; set; }
        private static IList<Payment> Payments { get; set; }
        private static IList<Month> Months { get; set; }
        private static IEnumerable<Strategy> Strategies { get; set; }

        static void Main(string[] args)
        {
            //initialize stuff
            Mapper.Initialize(cfg => cfg.CreateMap<Loan, LoanOutput>());
            PopulateDimensions(new DateTime(2019, 6, 1));

            //start the main for loop
            foreach (Strategy s in Strategies)
            {
                foreach (Month m in Months)
                {
                    decimal extraThisMonth = s.ExtraPerMonth;

                    IOrderedEnumerable<Loan> orderedLoans;

                    switch (s.SortOrder)
                    {
                        case "Highest Rate First":
                            orderedLoans = Loans.OrderByDescending(ul => ul.Rate);
                            break;
                        case "Lowest Balance First":
                            orderedLoans = Loans.OrderBy(ul => ul.Principal);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    foreach (Loan l in orderedLoans)
                    {
                        //get all the principal payments up to this point
                        var pastPayments = Payments.Where(p => p.MonthId < m.MonthId && p.LoanId == l.LoanId && p.StrategyId == s.StrategyId);

                        var totalPaid = pastPayments.Sum(p => p.Principal + p.AdditionalPrincipal);

                        var loanBalance = l.Principal - totalPaid;


                        if (loanBalance > 0)
                        {
                            //determine payment id
                            var maxPaymentId = Payments.DefaultIfEmpty().Max(p => p?.PaymentId ?? 0);
                            var currPaymentId = maxPaymentId + 1;

                            //calculate "standard" payment
                            var currInterest = loanBalance * ((decimal)l.Rate / 12);
                            var currPrincipal = l.MinPayment - currInterest;

                            //pay the minumum principal (or the remaining balance)
                            if (loanBalance <= currPrincipal)
                            {
                                currPrincipal = loanBalance;
                            }
                            loanBalance -= currPrincipal;

                            //10 is just an arbitrary number to cover rounding
                            //so if after the minumum payment is done, there is less than a $10 balance
                            //just pay the remainder off.
                            //it's a pretty high fudge factor. In calculations so far, only have been off a few pennies.
                            int fudgeFactor = 10;
                            decimal currAdditionalPrincipal = 0;

                            //(optionally) add a bit of additional principal to cover finishing off the loan
                            //DO NOT reset the extra per month to 0
                            if (loanBalance < fudgeFactor)
                            {
                                currAdditionalPrincipal = loanBalance;
                            }
                            loanBalance -= currAdditionalPrincipal;

                            //(optionally) add additional principal
                            if (extraThisMonth > 0 && loanBalance > 0)
                            {
                                if (extraThisMonth < loanBalance)
                                {
                                    currAdditionalPrincipal = extraThisMonth;
                                }
                                else
                                {
                                    currAdditionalPrincipal = loanBalance;
                                }
                                loanBalance -= currAdditionalPrincipal;

                                extraThisMonth = 0;
                            }

                            var payment = new Payment()
                            {
                                PaymentId = currPaymentId,
                                Interest = currInterest,
                                LoanId = l.LoanId,
                                AdditionalPrincipal = currAdditionalPrincipal,
                                MonthId = m.MonthId,
                                Principal = currPrincipal,
                                StrategyId = s.StrategyId
                            };

                            Payments.Add(payment);
                            //Console.ReadLine();
                        }
                    }
                }
            }

            foreach (Loan l in Loans)
            {
                Console.WriteLine("Loan Info");
                Console.WriteLine(l.MinPayment);
            }

            Console.WriteLine("Interest");
            for (int i = 1; i <= 6; i++)
            {
                Console.WriteLine(i.ToString() + " " + Payments.Where(p => p.StrategyId == i).Sum(p => p.Interest).ToString());
            }
            Console.WriteLine("Principal");
            for (int i = 1; i <= 6; i++)
            {
                Console.WriteLine(i.ToString() + " " + Payments.Where(p => p.StrategyId == i).Sum(p => p.Principal).ToString());
            }
            Console.WriteLine("Additional Principal");
            for (int i = 1; i <= 6; i++)
            {
                Console.WriteLine(i.ToString() + " " + Payments.Where(p => p.StrategyId == i).Sum(p => p.AdditionalPrincipal).ToString());
            }
            Console.WriteLine("Total Principal");
            for (int i = 1; i <= 6; i++)
            {
                Console.WriteLine(i.ToString() + " " + Payments.Where(p => p.StrategyId == i).Sum(p => p.AdditionalPrincipal + p.Principal).ToString());
            }
            Console.WriteLine("Payment Count");
            for (int i = 1; i <= 6; i++)
            {
                Console.WriteLine(i.ToString() + " " + Payments.Where(p => p.StrategyId == i).Count().ToString());
            }


            WriteOutputFiles();

            Console.ReadLine();
        }

        private static void PopulateDimensions(DateTime beginDate)
        {
            Loans = new Loan[] {
                new Loan() { LoanId = 1, LoanName = "Sample 10 Year", Principal = 20000, Rate = 0.06, TermInMonths = 120 },
                new Loan() { LoanId = 2, LoanName = "Sample 5 Year", Principal = 10000, Rate = 0.04, TermInMonths = 60 }
                };

            Strategies = new Strategy[] {
                new Strategy() { StrategyId = 1, ExtraPerMonth = 0, SortOrder = "Highest Rate First", StrategyName = "HR 0" },
                new Strategy() { StrategyId = 2, ExtraPerMonth = 100, SortOrder = "Highest Rate First", StrategyName = "HR 100" },
                new Strategy() { StrategyId = 3, ExtraPerMonth = 200, SortOrder = "Highest Rate First", StrategyName = "HR 200" },
                new Strategy() { StrategyId = 4, ExtraPerMonth = 300, SortOrder = "Highest Rate First", StrategyName = "HR 300" },
                new Strategy() { StrategyId = 5, ExtraPerMonth = 400, SortOrder = "Highest Rate First", StrategyName = "HR 400" },
                new Strategy() { StrategyId = 6, ExtraPerMonth = 500, SortOrder = "Highest Rate First", StrategyName = "HR 500" },
                new Strategy() { StrategyId = 7, ExtraPerMonth = 600, SortOrder = "Highest Rate First", StrategyName = "HR 600" },

                new Strategy() { StrategyId = 8, ExtraPerMonth = 0, SortOrder = "Lowest Balance First", StrategyName = "LB 0" },
                new Strategy() { StrategyId = 9, ExtraPerMonth = 100, SortOrder = "Lowest Balance First", StrategyName = "LB 100" },
                new Strategy() { StrategyId = 10, ExtraPerMonth = 200, SortOrder = "Lowest Balance First", StrategyName = "LB 200" },
                new Strategy() { StrategyId = 11, ExtraPerMonth = 300, SortOrder = "Lowest Balance First", StrategyName = "LB 300" },
                new Strategy() { StrategyId = 12, ExtraPerMonth = 400, SortOrder = "Lowest Balance First", StrategyName = "LB 400" },
                new Strategy() { StrategyId = 13, ExtraPerMonth = 500, SortOrder = "Lowest Balance First", StrategyName = "LB 500" },
                new Strategy() { StrategyId = 14, ExtraPerMonth = 600, SortOrder = "Lowest Balance First", StrategyName = "LB 600" },
                };

            Months = new List<Month>();
            for (int i = 0; i < 360; i++)
            {
                Months.Add(new Month() { MonthId = i + 1, Date = beginDate.AddMonths(i) });
            }

            Payments = new List<Payment>();
        }
        private static void WriteOutputFiles()
        {
            const string filePath = @"C:\Users\vince\Downloads\Power BI\Data\";
            string outFileNamePmt = $"{filePath}Loan Payments v04.csv";
            string outFileNameLoan = $"{filePath}Loan Loans v04.csv";
            string outFileNameStrat = $"{filePath}Loan Strategies v04.csv";
            string outFileNameMonth = $"{filePath}Loan Months v04.csv";

            //payments
            var outEnginePmt = new FileHelperEngine<Payment>();
            outEnginePmt.HeaderText = outEnginePmt.GetFileHeader();
            outEnginePmt.WriteFile(outFileNamePmt, Payments);

            //loans
            IEnumerable<LoanOutput> loanOutputs = Mapper.Map<IEnumerable<Loan>, IEnumerable<LoanOutput>>(Loans);

            var outEngineLoan = new FileHelperEngine<LoanOutput>();
            outEngineLoan.HeaderText = outEngineLoan.GetFileHeader();
            outEngineLoan.WriteFile(outFileNameLoan, loanOutputs);

            //strategies
            var outEngineStrat = new FileHelperEngine<Strategy>();
            outEngineStrat.HeaderText = outEngineStrat.GetFileHeader();
            outEngineStrat.WriteFile(outFileNameStrat, Strategies);

            //months
            var outEngineMonth = new FileHelperEngine<Month>();
            outEngineMonth.HeaderText = outEngineMonth.GetFileHeader();
            outEngineMonth.WriteFile(outFileNameMonth, Months);
        }
    }
}
