using FileHelpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoanPortfolioCore.Models
{
    class Loan
    {
        public int LoanId { get; set; }
        public double Principal { get; set; }
        public double Rate { get; set; } //annual
        public int TermInMonths { get; set; }
        public string LoanName { get; set; }
        public double MinPayment
        {
            get
            {
                ////these are the minimum values required to do the calculation without errors (e.g. NaN)
                //Principal = principal;
                //Rate = rate;
                //TermInMonths = termInMonths;

                // got the details of how to calculate here:
                // https://brownmath.com/bsci/loan.htm#LoanPayment

                //first convert data types and match names to reference doc to keep the formula cleaner
                double A = (double)Principal;
                double i = Rate / 12; //convert from annual to monthly
                int N = TermInMonths;
                double P;

                //then actually calculate
                P = (i * A) /
                    (1 - Math.Pow((1 + i), -N));

                return P;
            }
        }
    }
}
