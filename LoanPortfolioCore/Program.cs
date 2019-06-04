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
        private static IList<Strategy> Strategies { get; set; }

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
                        case SortOrders.HighestRateFirst:
                        case SortOrders.NotApplicable: //the order really doesn't matter but we DO need an IOrderedEnumerable
                            orderedLoans = Loans.OrderByDescending(ul => ul.Rate);
                            break;
                        case SortOrders.LowestBalanceFirst:
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

                                extraThisMonth -= currAdditionalPrincipal;
                            }

                            var payment = new Payment()
                            {
                                PaymentId = currPaymentId,
                                Interest = currInterest,
                                LoanId = l.LoanId,
                                AdditionalPrincipal = currAdditionalPrincipal,
                                MonthId = m.MonthId,
                                Principal = currPrincipal,
                                StrategyId = s.StrategyId,
                                PrincipalBalance = loanBalance
                            };

                            Payments.Add(payment);
                            //Console.ReadLine();
                        }
                    }
                }
            }

            WriteOutputFiles();
            WriteDebugInfo();
            Console.ReadLine();
        }

        private static void PopulateDimensions(DateTime beginDate)
        {
            Loans = new Loan[] {
                new Loan() { LoanId = 1, LoanName = "Sample 10 Year", Principal = 20000, Rate = 0.06, TermInMonths = 120 },
                new Loan() { LoanId = 2, LoanName = "Sample 5 Year", Principal = 10000, Rate = 0.04, TermInMonths = 60 }
                };

            int id = 1;
            var sortOrdersForLoop = new SortOrders[] { SortOrders.HighestRateFirst, SortOrders.LowestBalanceFirst };
            var extraAmountsForLoop = new int[] { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000 };

            //create the "special" base strategy
            Strategies = new List<Strategy>
            {
                new Strategy() { StrategyId = id, ExtraPerMonth = 0, SortOrder = SortOrders.NotApplicable, StrategyName = "Base" }
            };

            //create the rest of the strategies
            foreach (var sortOrder in sortOrdersForLoop)
            {
                foreach (var extraAmount in extraAmountsForLoop)
                {
                    id++;
                    Strategies.Add(new Strategy
                    {
                        StrategyId = id,
                        ExtraPerMonth = extraAmount,
                        SortOrder = sortOrder,
                        StrategyName = $"{sortOrder} {extraAmount}"
                    });
                }
            }

            Months = new List<Month>();
            for (int i = 0; i < 360; i++) //nothing special about 360 - just seemed like long time!
            {
                Months.Add(new Month() { MonthId = i + 1, Date = beginDate.AddMonths(i) });
            }

            Payments = new List<Payment>();
        }
        private static void WriteOutputFiles()
        {
            const string filePath = @"C:\Users\vince\Downloads\Power BI\Data\";
            string outFileNamePmt = $"{filePath}Loan Payments v05.csv";
            string outFileNameLoan = $"{filePath}Loan Loans v05.csv";
            string outFileNameStrat = $"{filePath}Loan Strategies v05.csv";
            string outFileNameMonth = $"{filePath}Loan Months v05.csv";

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
        private static void WriteDebugInfo()
        {
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
        }
    }
}
