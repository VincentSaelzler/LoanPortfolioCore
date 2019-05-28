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

            PopulateDimensions(new DateTime(2019, 6, 1));

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
                            //calculate interest
                            var currInterest = loanBalance * ((decimal)l.Rate / 12);

                            //create a minumum payment
                            var maxPaymentId = Payments.DefaultIfEmpty().Max(p => p?.PaymentId ?? 0);

                            var payment = new Payment() { PaymentId = maxPaymentId + 1, Interest = currInterest, LoanId = l.LoanId, AdditionalPrincipal = 0, MonthId = m.MonthId, Principal = l.MinPayment - currInterest, StrategyId = s.StrategyId};

                            loanBalance -= payment.Principal;

                            if (loanBalance < 10) //the 10 is just an arbitrary amount that should cover rounding errors
                            {
                                payment.Principal += loanBalance;
                                loanBalance = 0;
                            }

                            //(optionally) add additional principal
                            if (extraThisMonth > 0)
                            {
                                if (loanBalance > extraThisMonth)
                                {
                                    payment.AdditionalPrincipal = extraThisMonth;
                                }
                                else
                                {
                                    payment.AdditionalPrincipal = loanBalance;
                                }
                                extraThisMonth = 0;
                            }

                            Payments.Add(payment);
                            //Console.ReadLine();
                        }
                    }
                }
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

            Console.ReadLine();
        }

        private static void PopulateDimensions(DateTime beginDate)
        {

            Loans = new Loan[] {
                new Loan() { LoanId = 1, LoanName = "Sample 10 Year", MinPayment = 222.04M, Principal = 20000, Rate = 0.06, TermInMonths = 120 },
                new Loan() { LoanId = 2, LoanName = "Sample 5 Year", MinPayment = 184.17M, Principal = 10000, Rate = 0.04, TermInMonths = 60 }
                };

            Strategies = new Strategy[] {
                new Strategy() { StrategyId = 1, ExtraPerMonth = 0, SortOrder = "Highest Rate First", StrategyName = "HR 0" },
                new Strategy() { StrategyId = 2, ExtraPerMonth = 100, SortOrder = "Highest Rate First", StrategyName = "HR 100" },
                new Strategy() { StrategyId = 3, ExtraPerMonth = 200, SortOrder = "Highest Rate First", StrategyName = "HR 200" },
                new Strategy() { StrategyId = 4, ExtraPerMonth = 0, SortOrder = "Lowest Balance First", StrategyName = "LB 0" },
                new Strategy() { StrategyId = 5, ExtraPerMonth = 100, SortOrder = "Lowest Balance First", StrategyName = "LB 100" },
                new Strategy() { StrategyId = 6, ExtraPerMonth = 200, SortOrder = "Lowest Balance First", StrategyName = "LB 200" }
                };

            Months = new List<Month>();
            for (int i = 0; i < 360; i++)
            {
                Months.Add(new Month() { MonthId = i + 1, Date = beginDate.AddMonths(i) });
            }

            Payments = new List<Payment>();
        }
    }
}
