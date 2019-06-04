using FileHelpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoanPortfolioCore.FlatFileModels
{
    [DelimitedRecord(",")]
    class LoanOutput
    {
            public int LoanId { get; set; }
            public double Principal { get; set; }
            public double Rate { get; set; } //annual
            public int TermInMonths { get; set; }
            public string LoanName { get; set; }
            public double MinPayment { get; set; }
    }
}
