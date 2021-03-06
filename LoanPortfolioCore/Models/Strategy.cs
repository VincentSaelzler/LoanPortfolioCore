﻿using FileHelpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoanPortfolioCore.Models
{
    [DelimitedRecord(",")]
    class Strategy
    {
        public int StrategyId { get; set; }
        public string StrategyName { get; set; }
        public SortOrders SortOrder { get; set; }
        public double ExtraPerMonth { get; set; }
        public int MonthsDelay { get; set; }
        public ExtraPerMonthCalcMethods ExtraPerMonthCalcMethod { get; set; }
        public UseSortOrderGroups UseSortOrderGroup { get; set; }
    }
}
