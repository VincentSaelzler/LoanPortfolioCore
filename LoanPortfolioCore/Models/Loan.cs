using System;
using System.Collections.Generic;
using System.Text;

namespace LoanPortfolioCore.Models
{
    class Loan
    {
        public int LoanId { get; set; }

        public decimal MinPayment { get; set; }
        public decimal Principal { get; set; }
        public double Rate { get; set; }
        public int TermInMonths { get; set; }

        public string LoanName { get; set; }
    }
}
