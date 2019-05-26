using System;
using System.Collections.Generic;
using System.Text;

namespace LoanPortfolioCore.Models
{
    class Strategy
    {
        public int StrategyId { get; set; }
        public string StrategyName { get; set; }
        public string SortOrder { get; set; }
        public decimal ExtraPerMonth { get; set; }
    }
}
