using FileHelpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoanPortfolioCore.Models
{
    [DelimitedRecord(",")]
    class Month
    {
        public int MonthId { get; set; }
        [FieldConverter(ConverterKind.Date, "yyyy-MM-dd")]
        public DateTime Date { get; set; }
    }
}
