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

        public decimal AdditionalPrincipal { get; set; }
        public decimal Interest { get; set; }
        public decimal Principal { get; set; }
        public decimal PrincipalBalance { get; set; }
    }
}
