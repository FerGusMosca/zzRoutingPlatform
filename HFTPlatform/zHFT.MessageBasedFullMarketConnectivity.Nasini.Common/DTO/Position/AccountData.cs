using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.MessageBasedFullMarketConnectivity.Nasini.Common.DTO.Position
{
    public class AccountData
    {
        public string accountName { get; set; }
        public string marketMember { get; set; }
        public string marketMemberIdentity { get; set; }
        public double collateral { get; set; }
        public double margin { get; set; }
        public double availableToCollateral { get; set; }

        [JsonProperty("detailedAccountReports")]
        public Dictionary<string, AccountDetail> detailedAccountReports { get; set; }

        [JsonIgnore]
        public List<AccountDetail> DetailedReportsList => detailedAccountReports?.Values.ToList();

        public bool hasError { get; set; }
        public string errorDetail { get; set; }
        public long lastCalculation { get; set; }
        public double portfolio { get; set; }
        public double ordersMargin { get; set; }
        public double currentCash { get; set; }
        public double dailyDiff { get; set; }
        public double uncoveredMargin { get; set; }
        public double? repurchaseMarginARS { get; set; }
        public double? repurchaseMarginUSD { get; set; }
        public string accountDescription { get; set; }
        public object detailedCollateral { get; set; }
        public object detailedPortfolio { get; set; }
        public object detailedAvailableToCollateral { get; set; }
        public string personName { get; set; }
    }

}
