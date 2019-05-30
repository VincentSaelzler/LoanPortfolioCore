using System;
using System.Collections.Generic;
using System.Text;

namespace LoanPortfolioCore.Models
{
    class Loan
    {
        public int LoanId { get; set; }
        public decimal Principal { get; set; }
        public double Rate { get; set; }
        public int TermInMonths { get; set; }

        public string LoanName { get; set; }
        //https://brownmath.com/bsci/loan.htm#LoanPayment
        public decimal MinPayment
        {
            get
            {
                double A = (double)Principal;
                double i = Rate / 12;
                int N = TermInMonths;
                double P;

                P = (i * A) /
                    (1 - Math.Pow((1 + i), -N));

                return (decimal)P;
            }
        }

    }
}
