using System;
using System.Collections.Generic;
using System.Text;

namespace LoanPortfolioCore
{
    enum SortOrders { HighestRateFirst, LowestBalanceFirst, NotApplicable }
    enum ExtraPerMonthCalcMethods { Contant, MinPaymentPlusExtra, NotApplicable }
    enum UseSortOrderGroups { DoNotUse, Use, NotApplicable }
}
