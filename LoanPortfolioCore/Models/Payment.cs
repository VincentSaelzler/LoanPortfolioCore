using FileHelpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoanPortfolioCore.Models
{
    [DelimitedRecord(",")]
    class Payment
    {
        public int PaymentId { get; set; }
        public int LoanId { get; set; }
        public int MonthId { get; set; }
        public int StrategyId { get; set; }

        public double AdditionalPrincipal { get; set; }
        public double Interest { get; set; }
        public double Principal { get; set; }
        public double PrincipalBalance { get; set; }
        public double Insurance { get; set; }
    }
}
