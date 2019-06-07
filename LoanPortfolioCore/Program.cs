using AutoMapper;
using FileHelpers;
using LoanPortfolioCore.FlatFileModels;
using LoanPortfolioCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace LoanPortfolioCore
{
    class Program
    {
        private static IEnumerable<Loan> Loans { get; set; }
        private static IEnumerable<Payment> Payments { get; set; }
        private static IList<Month> Months { get; set; }
        private static IList<Strategy> Strategies { get; set; }

        static void Main(string[] args)
        {
            //start timer
            var watch = Stopwatch.StartNew();

            //10 is just an arbitrary number to cover rounding
            //so if after the minumum payment is done, there is less than a $10 balance
            //just pay the remainder off.
            //it's a pretty high fudge factor. In calculations so far, only have been off a few pennies.
            const int fudgeFactor = 10;

            //initialize stuff
            Mapper.Initialize(cfg => cfg.CreateMap<Loan, LoanOutput>());
            PopulateDimensions(new DateTime(2019, 6, 1));

            //container
            IList<IList<Payment>> paymentLists = new List<IList<Payment>>();

            //start the main for loop
            foreach (Strategy s in Strategies)
            {
                //container
                IList<Payment> payments = new List<Payment>();

                //calc the max extra per month (stays constant month-over-month)
                var totalSpendPerMonth = s.ExtraPerMonth + Loans.Sum(l => l.MinPayment);

                foreach (Month m in Months)
                {
                    //pay min
                    foreach (Loan l in Loans)
                    {
                        //get all the principal payments up to this point
                        var pastPayments = payments.Where(p =>
                            p.StrategyId == s.StrategyId &&
                            p.LoanId == l.LoanId &&
                            p.MonthId < m.MonthId);

                        //calc balance (EXPENSIVE)
                        var totalPaid = pastPayments.Sum(p => p.Principal + p.AdditionalPrincipal);
                        var loanBalance = l.Principal - totalPaid;

                        if (loanBalance > 0)
                        {
                            //determine payment id
                            var maxPaymentId = Payments.DefaultIfEmpty().Max(p => p?.PaymentId ?? 0);
                            var currPaymentId = maxPaymentId + 1;

                            //calculate "standard" payment
                            var currInterest = loanBalance * (l.Rate / 12);
                            var currPrincipal = l.MinPayment - currInterest;

                            //pay the minumum principal (or the remaining balance)
                            if (loanBalance <= currPrincipal + fudgeFactor)
                            {
                                currPrincipal = loanBalance;
                            }
                            loanBalance -= currPrincipal;

                            //create and add payment
                            var payment = new Payment()
                            {
                                PaymentId = currPaymentId,
                                LoanId = l.LoanId,
                                StrategyId = s.StrategyId,
                                MonthId = m.MonthId,
                                Interest = currInterest,
                                Principal = currPrincipal,
                                PrincipalBalance = loanBalance,
                                AdditionalPrincipal = 0, //this will be added in the next step
                            };
                            payments.Add(payment);
                        }
                    }

                    //add exta principal if not the base case
                    if (s.StrategyName != "Base" && m.MonthId > s.MonthsDelay)
                    {
                        //order the loans
                        //MSDN https://docs.microsoft.com/en-us/dotnet/api/system.linq.enumerable.orderbydescending?view=netframework-4.8
                        //you must use .ThenBy(). If you call .OrderBy on something that was already ordered, it introduces a NEW primary ordering.
                        IOrderedEnumerable<Loan> orderedLoans;
                        switch (s.SortOrder)
                        {
                            case SortOrders.HighestRateFirst:
                                if (s.UseSortOrderGroup == UseSortOrderGroups.Use)
                                {
                                    orderedLoans = Loans.OrderBy(l => l.SortGroup).ThenByDescending(l => l.Rate);
                                }
                                else
                                {
                                    orderedLoans = Loans.OrderByDescending(l => l.Rate);
                                }
                                break;
                            case SortOrders.LowestBalanceFirst:
                                if (s.UseSortOrderGroup == UseSortOrderGroups.Use)
                                {
                                    orderedLoans = Loans.OrderBy(l => l.SortGroup).ThenBy(l => l.Principal);
                                }
                                else
                                {
                                    orderedLoans = Loans.OrderBy(l => l.Principal);
                                }
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        //get the sum of the mimimum payments this month
                        var minPaymentSumThisMonth = payments
                            .Where(p => p.MonthId == m.MonthId && p.StrategyId == s.StrategyId)
                            .Sum(p => p.Principal + p.Interest);

                        //get the extra to work with this month
                        double extraThisMonth = 0;
                        switch (s.ExtraPerMonthCalcMethod)
                        {
                            case ExtraPerMonthCalcMethods.MinPaymentPlusExtra:
                                extraThisMonth = s.ExtraPerMonth;
                                break;
                            case ExtraPerMonthCalcMethods.Contant:
                                extraThisMonth = totalSpendPerMonth - minPaymentSumThisMonth;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        //adjust existing payments - add additional principal
                        foreach (Loan l in orderedLoans)
                        {
                            double extraThisMonthThisLoan = 0;

                            //get all the principal payments up to this point
                            var pastPayments = payments.Where(p =>
                                p.StrategyId == s.StrategyId &&
                                p.LoanId == l.LoanId &&
                                p.MonthId <= m.MonthId);

                            //calc balance (EXPENSIVE)
                            var totalPaid = pastPayments.Sum(p => p.Principal + p.AdditionalPrincipal);
                            var loanBalance = l.Principal - totalPaid;

                            if (loanBalance > 0 && extraThisMonth > 0)
                            {
                                //figure out the extra amount to pay
                                if (loanBalance > extraThisMonth)
                                {
                                    extraThisMonthThisLoan = extraThisMonth;
                                }
                                else
                                {
                                    extraThisMonthThisLoan = loanBalance;
                                }

                                //adjust running totals
                                loanBalance -= extraThisMonthThisLoan;
                                extraThisMonth -= extraThisMonthThisLoan;

                                //determine latest payment id
                                var latestPmtId = pastPayments.Select(p => p.PaymentId).Max();

                                //adjust the latest payment
                                foreach (var p in Payments)
                                {
                                    if (p.PaymentId == latestPmtId)
                                    {
                                        p.AdditionalPrincipal = extraThisMonthThisLoan;
                                        p.PrincipalBalance = loanBalance;
                                    }
                                }
                            }
                        }
                    }
                }

                //add to out-of-loop container
                paymentLists.Add(payments);

                //show progress
                Console.WriteLine($"{s.StrategyId} of {Strategies.Count}");
            }

            //get all the payments in one enumerable
            Payments = paymentLists.SelectMany(plist => plist.AsEnumerable());

            //add (arbitrary) payment ids
            int i = 0;
            foreach (var p in Payments)
            {
                i++;
                p.PaymentId = i;
            }

            WriteOutputFiles();
            //WriteDebugInfo();

            //show timer results
            watch.Stop();
            Console.WriteLine(watch.ElapsedMilliseconds);
            Console.ReadLine();
        }
        private static void PopulateDimensions(DateTime beginDate)
        {
            //loans
            Loans = new Loan[] {
                new Loan() { LoanId = 1, LoanName = "Sample 10 Year", Principal = 20000, Rate = 0.06, TermInMonths = 120, SortGroup = 2 },
                new Loan() { LoanId = 2, LoanName = "Sample 5 Year", Principal = 10000, Rate = 0.04, TermInMonths = 60, SortGroup = 1 },
                };

            //months
            var maxTerm = Loans.Select(l => l.TermInMonths).Max(); //max of the base strat loan terms
            Months = new List<Month>();
            for (int i = 0; i < maxTerm; i++)
            {
                Months.Add(new Month() { MonthId = i + 1, Date = beginDate.AddMonths(i) });
            }

            //payments (empty container)
            Payments = new List<Payment>();

            //strategies
            CreateStrategies();
        }
        private static void WriteOutputFiles()
        {
            const string filePath = @"C:\Users\vince\Downloads\Power BI\Data\";
            string outFileNamePmt = $"{filePath}Loan Payments v06.csv";
            string outFileNameLoan = $"{filePath}Loan Loans v06.csv";
            string outFileNameStrat = $"{filePath}Loan Strategies v06.csv";
            string outFileNameMonth = $"{filePath}Loan Months v06.csv";

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
        private static void CreateStrategies()
        {
            //sort orderr groups
            var useSortOrderGroups = new UseSortOrderGroups[] { UseSortOrderGroups.Use, UseSortOrderGroups.DoNotUse };

            //sort orders
            var sortOrders = new SortOrders[] { SortOrders.HighestRateFirst, SortOrders.LowestBalanceFirst };

            //extra per month calc methods
            var extraPerMonthCalcMethods = new ExtraPerMonthCalcMethods[] { ExtraPerMonthCalcMethods.Contant, ExtraPerMonthCalcMethods.MinPaymentPlusExtra };

            //months/years of delay
            var maxTerm = Loans.Select(l => l.TermInMonths).Max(); //max of the base strat loan terms
            const int monthStep = 12;
            var m = 0;

            IList<int> monthsDelay = new List<int>();
            do
            {
                monthsDelay.Add(m * monthStep);
                m++;
            } while (m * monthStep < maxTerm);

            //extra payment amounts per month
            const int amountStep = 100;
            const int numAmountSteps = 20;

            IList<int> extraAmounts = new List<int>();
            for (int a = 1; a <= numAmountSteps; a++)
            {
                extraAmounts.Add(a * amountStep);
            }

            //create the "special" base strategy
            int id = 1;
            Strategies = new List<Strategy>
            {
                new Strategy() {
                    StrategyId = id,
                    ExtraPerMonth = 0,
                    SortOrder = SortOrders.NotApplicable,
                    StrategyName = "Base",
                    MonthsDelay = 0,
                    ExtraPerMonthCalcMethod = ExtraPerMonthCalcMethods.NotApplicable,
                    UseSortOrderGroup = UseSortOrderGroups.NotApplicable,
                }
            };

            //create the rest of the strategies
            foreach (UseSortOrderGroups useSortOrderGroup in useSortOrderGroups)
            {
                foreach (ExtraPerMonthCalcMethods extraPerMonthCalcMethod in extraPerMonthCalcMethods)
                {
                    foreach (SortOrders sortOrder in sortOrders)
                    {
                        foreach (var extraAmount in extraAmounts)
                        {
                            foreach (var monthDelay in monthsDelay)
                            {
                                id++;
                                Strategies.Add(new Strategy
                                {
                                    StrategyId = id,
                                    ExtraPerMonth = extraAmount,
                                    SortOrder = sortOrder,
                                    StrategyName = $"{useSortOrderGroup} {extraPerMonthCalcMethod} {sortOrder} {extraAmount} {monthDelay}",
                                    MonthsDelay = monthDelay,
                                    ExtraPerMonthCalcMethod = extraPerMonthCalcMethod,
                                    UseSortOrderGroup = useSortOrderGroup,
                                });
                            }
                        }
                    }
                }
            }
        }
    }
}
