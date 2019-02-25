using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace zHFT.OrderRouters.Bitmex.Common.DTO
{
    public class ExecutionReport
    {
        #region Public Attributes

        /// <summary>
        /// Gets or Sets ExecID
        /// </summary>
        [DataMember(Name = "execID", EmitDefaultValue = false)]
        public string ExecID { get; set; }

        /// <summary>
        /// Gets or Sets OrderID
        /// </summary>
        [DataMember(Name = "orderID", EmitDefaultValue = false)]
        public string OrderID { get; set; }

        /// <summary>
        /// Gets or Sets ClOrdID
        /// </summary>
        [DataMember(Name = "clOrdID", EmitDefaultValue = false)]
        public string ClOrdID { get; set; }

        /// <summary>
        /// Gets or Sets ClOrdLinkID
        /// </summary>
        [DataMember(Name = "clOrdLinkID", EmitDefaultValue = false)]
        public string ClOrdLinkID { get; set; }

        /// <summary>
        /// Gets or Sets Account
        /// </summary>
        [DataMember(Name = "account", EmitDefaultValue = false)]
        public decimal? Account { get; set; }

        /// <summary>
        /// Gets or Sets Symbol
        /// </summary>
        [DataMember(Name = "symbol", EmitDefaultValue = false)]
        public string Symbol { get; set; }

        /// <summary>
        /// Gets or Sets Side
        /// </summary>
        [DataMember(Name = "side", EmitDefaultValue = false)]
        public string Side { get; set; }

        /// <summary>
        /// Gets or Sets LastQty
        /// </summary>
        [DataMember(Name = "lastQty", EmitDefaultValue = false)]
        public double? LastQty { get; set; }

        /// <summary>
        /// Gets or Sets LastPx
        /// </summary>
        [DataMember(Name = "lastPx", EmitDefaultValue = false)]
        public double? LastPx { get; set; }

        /// <summary>
        /// Gets or Sets UnderlyingLastPx
        /// </summary>
        [DataMember(Name = "underlyingLastPx", EmitDefaultValue = false)]
        public double? UnderlyingLastPx { get; set; }

        /// <summary>
        /// Gets or Sets LastMkt
        /// </summary>
        [DataMember(Name = "lastMkt", EmitDefaultValue = false)]
        public string LastMkt { get; set; }

        /// <summary>
        /// Gets or Sets LastLiquidityInd
        /// </summary>
        [DataMember(Name = "lastLiquidityInd", EmitDefaultValue = false)]
        public string LastLiquidityInd { get; set; }

        /// <summary>
        /// Gets or Sets SimpleOrderQty
        /// </summary>
        [DataMember(Name = "simpleOrderQty", EmitDefaultValue = false)]
        public double? SimpleOrderQty { get; set; }

        /// <summary>
        /// Gets or Sets OrderQty
        /// </summary>
        [DataMember(Name = "orderQty", EmitDefaultValue = false)]
        public decimal? OrderQty { get; set; }

        /// <summary>
        /// Gets or Sets Price
        /// </summary>
        [DataMember(Name = "price", EmitDefaultValue = false)]
        public double? Price { get; set; }

        /// <summary>
        /// Gets or Sets DisplayQty
        /// </summary>
        [DataMember(Name = "displayQty", EmitDefaultValue = false)]
        public decimal? DisplayQty { get; set; }

        /// <summary>
        /// Gets or Sets StopPx
        /// </summary>
        [DataMember(Name = "stopPx", EmitDefaultValue = false)]
        public double? StopPx { get; set; }

        /// <summary>
        /// Gets or Sets PegOffsetValue
        /// </summary>
        [DataMember(Name = "pegOffsetValue", EmitDefaultValue = false)]
        public double? PegOffsetValue { get; set; }

        /// <summary>
        /// Gets or Sets PegPriceType
        /// </summary>
        [DataMember(Name = "pegPriceType", EmitDefaultValue = false)]
        public string PegPriceType { get; set; }

        /// <summary>
        /// Gets or Sets Currency
        /// </summary>
        [DataMember(Name = "currency", EmitDefaultValue = false)]
        public string Currency { get; set; }

        /// <summary>
        /// Gets or Sets SettlCurrency
        /// </summary>
        [DataMember(Name = "settlCurrency", EmitDefaultValue = false)]
        public string SettlCurrency { get; set; }

        /// <summary>
        /// Gets or Sets ExecType
        /// </summary>
        [DataMember(Name = "execType", EmitDefaultValue = false)]
        public string ExecType { get; set; }

        /// <summary>
        /// Gets or Sets OrdType
        /// </summary>
        [DataMember(Name = "ordType", EmitDefaultValue = false)]
        public string OrdType { get; set; }

        /// <summary>
        /// Gets or Sets TimeInForce
        /// </summary>
        [DataMember(Name = "timeInForce", EmitDefaultValue = false)]
        public string TimeInForce { get; set; }

        /// <summary>
        /// Gets or Sets ExecInst
        /// </summary>
        [DataMember(Name = "execInst", EmitDefaultValue = false)]
        public string ExecInst { get; set; }

        /// <summary>
        /// Gets or Sets ContingencyType
        /// </summary>
        [DataMember(Name = "contingencyType", EmitDefaultValue = false)]
        public string ContingencyType { get; set; }

        /// <summary>
        /// Gets or Sets ExDestination
        /// </summary>
        [DataMember(Name = "exDestination", EmitDefaultValue = false)]
        public string ExDestination { get; set; }

        /// <summary>
        /// Gets or Sets OrdStatus
        /// </summary>
        [DataMember(Name = "ordStatus", EmitDefaultValue = false)]
        public string OrdStatus { get; set; }

        /// <summary>
        /// Gets or Sets Triggered
        /// </summary>
        [DataMember(Name = "triggered", EmitDefaultValue = false)]
        public string Triggered { get; set; }

        /// <summary>
        /// Gets or Sets WorkingIndicator
        /// </summary>
        [DataMember(Name = "workingIndicator", EmitDefaultValue = false)]
        public bool? WorkingIndicator { get; set; }

        /// <summary>
        /// Gets or Sets OrdRejReason
        /// </summary>
        [DataMember(Name = "ordRejReason", EmitDefaultValue = false)]
        public string OrdRejReason { get; set; }

        /// <summary>
        /// Gets or Sets SimpleLeavesQty
        /// </summary>
        [DataMember(Name = "simpleLeavesQty", EmitDefaultValue = false)]
        public double? SimpleLeavesQty { get; set; }

        /// <summary>
        /// Gets or Sets LeavesQty
        /// </summary>
        [DataMember(Name = "leavesQty", EmitDefaultValue = false)]
        public decimal? LeavesQty { get; set; }

        /// <summary>
        /// Gets or Sets SimpleCumQty
        /// </summary>
        [DataMember(Name = "simpleCumQty", EmitDefaultValue = false)]
        public double? SimpleCumQty { get; set; }

        /// <summary>
        /// Gets or Sets CumQty
        /// </summary>
        [DataMember(Name = "cumQty", EmitDefaultValue = false)]
        public double? CumQty { get; set; }

        /// <summary>
        /// Gets or Sets AvgPx
        /// </summary>
        [DataMember(Name = "avgPx", EmitDefaultValue = false)]
        public double? AvgPx { get; set; }

        /// <summary>
        /// Gets or Sets Commission
        /// </summary>
        [DataMember(Name = "commission", EmitDefaultValue = false)]
        public double? Commission { get; set; }

        /// <summary>
        /// Gets or Sets TradePublishIndicator
        /// </summary>
        [DataMember(Name = "tradePublishIndicator", EmitDefaultValue = false)]
        public string TradePublishIndicator { get; set; }

        /// <summary>
        /// Gets or Sets MultiLegReportingType
        /// </summary>
        [DataMember(Name = "multiLegReportingType", EmitDefaultValue = false)]
        public string MultiLegReportingType { get; set; }

        /// <summary>
        /// Gets or Sets Text
        /// </summary>
        [DataMember(Name = "text", EmitDefaultValue = false)]
        public string Text { get; set; }

        /// <summary>
        /// Gets or Sets TrdMatchID
        /// </summary>
        [DataMember(Name = "trdMatchID", EmitDefaultValue = false)]
        public string TrdMatchID { get; set; }

        /// <summary>
        /// Gets or Sets ExecCost
        /// </summary>
        [DataMember(Name = "execCost", EmitDefaultValue = false)]
        public decimal? ExecCost { get; set; }

        /// <summary>
        /// Gets or Sets ExecComm
        /// </summary>
        [DataMember(Name = "execComm", EmitDefaultValue = false)]
        public decimal? ExecComm { get; set; }

        /// <summary>
        /// Gets or Sets HomeNotional
        /// </summary>
        [DataMember(Name = "homeNotional", EmitDefaultValue = false)]
        public double? HomeNotional { get; set; }

        /// <summary>
        /// Gets or Sets ForeignNotional
        /// </summary>
        [DataMember(Name = "foreignNotional", EmitDefaultValue = false)]
        public double? ForeignNotional { get; set; }

        /// <summary>
        /// Gets or Sets TransactTime
        /// </summary>
        [DataMember(Name = "transactTime", EmitDefaultValue = false)]
        public DateTime? TransactTime { get; set; }

        /// <summary>
        /// Gets or Sets Timestamp
        /// </summary>
        [DataMember(Name = "timestamp", EmitDefaultValue = false)]
        public DateTime? Timestamp { get; set; }




        #endregion
    }
}
