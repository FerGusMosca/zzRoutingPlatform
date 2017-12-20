using QuickFix;
using Shared.Bussiness.Fix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using zHFT.Main.Common.Wrappers;
using zHFT.MarketClient.Primary.Common.Converters;

namespace zHFT.OrderRouters.Primary.Common.Wrappers
{
    public class ExecutionReportWrapper : Wrapper
    {
        #region Private Attributes
        
        protected QuickFix50.ExecutionReport ExecutionReport { get; set; }

        protected IConfiguration Config { get; set; }

        #endregion

        #region Constructors

        public ExecutionReportWrapper(QuickFix50.ExecutionReport pExecReport, IConfiguration pConfig)
        {
            ExecutionReport = pExecReport;

            Config = pConfig;
        }

        #endregion

        #region Private Methods

        protected string GetOrdId(QuickFix50.Message msg, int field)
        {
            string OrdId = null;
            if (field == ClOrdID.FIELD)
                OrdId = FixHelper.GetNullFieldIfSet(msg, ClOrdID.FIELD);
            else if (field == SecondaryClOrdID.FIELD)
                OrdId = FixHelper.GetNullFieldIfSet(msg, SecondaryClOrdID.FIELD);
            else if (field == OrderID.FIELD)
                OrdId = FixHelper.GetNullFieldIfSet(msg, OrderID.FIELD);
            else if (field == OrigClOrdID.FIELD)
                OrdId = FixHelper.GetNullFieldIfSet(msg, OrigClOrdID.FIELD);

            return OrdId;
        }

        protected zHFT.Main.Common.Enums.ExecType GetExecType(char estado)
        {
            if (estado == QuickFix.ExecType.PENDING_NEW)
                return zHFT.Main.Common.Enums.ExecType.PendingNew;
            else if (estado == QuickFix.ExecType.NEW)
                return zHFT.Main.Common.Enums.ExecType.New;
            else if (estado == QuickFix.ExecType.REJECTED)
                return zHFT.Main.Common.Enums.ExecType.Rejected;
            else if (estado == QuickFix.ExecType.CANCELED)
                return zHFT.Main.Common.Enums.ExecType.Canceled;
            else if (estado == QuickFix.ExecType.EXPIRED)
                return zHFT.Main.Common.Enums.ExecType.Expired;
            else if (estado == QuickFix.ExecType.PENDING_REPLACE)
                return zHFT.Main.Common.Enums.ExecType.PendingReplace;
            else if (estado == QuickFix.ExecType.REPLACED)
                return zHFT.Main.Common.Enums.ExecType.Replaced;
            else if (estado == QuickFix.ExecType.TRADE)
                return zHFT.Main.Common.Enums.ExecType.Trade;
            else if (estado == QuickFix.ExecType.PENDING_CANCEL)
                return zHFT.Main.Common.Enums.ExecType.PendingCancel;
            else
                throw new Exception("Tipo de ejecución no soportado: " + estado.ToString());
        }

        protected zHFT.Main.Common.Enums.OrdStatus GetOrdStatus(char estado)
        {
            if (estado == QuickFix.OrdStatus.PENDING_NEW)
                return zHFT.Main.Common.Enums.OrdStatus.PendingNew;
            else if (estado == QuickFix.OrdStatus.NEW)
                return zHFT.Main.Common.Enums.OrdStatus.New;
            else if (estado == QuickFix.OrdStatus.REJECTED)
                return zHFT.Main.Common.Enums.OrdStatus.Rejected;
            else if (estado == QuickFix.OrdStatus.CANCELED)
                return zHFT.Main.Common.Enums.OrdStatus.Canceled;
            else if (estado == QuickFix.OrdStatus.EXPIRED)
                return zHFT.Main.Common.Enums.OrdStatus.Expired;
            else if (estado == QuickFix.OrdStatus.PENDING_REPLACE)
                return zHFT.Main.Common.Enums.OrdStatus.PendingReplace;
            else if (estado == QuickFix.OrdStatus.REPLACED)
                return zHFT.Main.Common.Enums.OrdStatus.Replaced;
            else if (estado == QuickFix.OrdStatus.FILLED)
                return zHFT.Main.Common.Enums.OrdStatus.Filled;
            else if (estado == QuickFix.OrdStatus.PARTIALLY_FILLED)
                return zHFT.Main.Common.Enums.OrdStatus.PartiallyFilled;
            else
                throw new Exception("OrdStatus not supported: " + estado.ToString());
        }

        protected zHFT.Main.Common.Enums.OrdType? GetOrdType(char estado)
        {
            if (estado == QuickFix.OrdType.LIMIT)
                return zHFT.Main.Common.Enums.OrdType.Limit;
            else if (estado == QuickFix.OrdType.MARKET)
                return zHFT.Main.Common.Enums.OrdType.Market;
            else if (estado == QuickFix.OrdType.STOP)
                return zHFT.Main.Common.Enums.OrdType.Stop;
            else if (estado == QuickFix.OrdType.STOPLIMIT)
                return zHFT.Main.Common.Enums.OrdType.StopLimit;
            else if (estado == QuickFix.OrdType.MARKET_ON_CLOSE)
                return zHFT.Main.Common.Enums.OrdType.MarketOnClose;
            else if (estado == QuickFix.OrdType.LIMIT_ON_CLOSE)
                return zHFT.Main.Common.Enums.OrdType.LimitOnClose;
            else
                return null;
        
        }

        protected zHFT.Main.Common.Enums.Side GetSide(char estado)
        {
            if (estado == QuickFix.Side.BUY)
                return zHFT.Main.Common.Enums.Side.Buy;
            else if (estado == QuickFix.Side.SELL)
                return zHFT.Main.Common.Enums.Side.Sell;

            else
                return zHFT.Main.Common.Enums.Side.Unknown;
        }

        #endregion


        #region Public Overriden Methods

        public override Actions GetAction()
        {
            return Actions.EXECUTION_REPORT;
        }

        public override object GetField(Fields field)
        {
            ExecutionReportFields xrField = (ExecutionReportFields)field;

            if (ExecutionReport == null)
                return ExecutionReportFields.NULL;

            if (xrField == ExecutionReportFields.OrderID)
                return GetOrdId(ExecutionReport, OrderID.FIELD);
            else if (xrField == ExecutionReportFields.ClOrdID)
                return GetOrdId(ExecutionReport, ClOrdID.FIELD);
            else if (xrField == ExecutionReportFields.OrigClOrdID)
                return GetOrdId(ExecutionReport, OrigClOrdID.FIELD);
            else if (xrField == ExecutionReportFields.ExecType)
                return GetExecType(FixHelper.GetCharFieldIfSet(ExecutionReport, QuickFix.ExecType.FIELD));
            else if (xrField == ExecutionReportFields.ExecID)
                return ExecutionReport.getString(ExecID.FIELD);
            else if (xrField == ExecutionReportFields.OrdStatus)
                return GetOrdStatus(ExecutionReport.getChar(QuickFix.OrdStatus.FIELD));
            else if (xrField == ExecutionReportFields.OrdRejReason)
                return FixHelper.GetNullIntFieldIfSet(ExecutionReport, QuickFix.OrdRejReason.FIELD);
            else if (xrField == ExecutionReportFields.LeavesQty)
                return ExecutionReport.getInt(LeavesQty.FIELD);
            else if (xrField == ExecutionReportFields.CumQty)
                return ExecutionReport.getInt(CumQty.FIELD);
            else if (xrField == ExecutionReportFields.AvgPx)
                return FixHelper.GetDoubleFieldIfSet(ExecutionReport, AvgPx.FIELD);
            else if (xrField == ExecutionReportFields.Commission)
                return FixHelper.GetDoubleFieldIfSet(ExecutionReport, Commission.FIELD);
            else if (xrField == ExecutionReportFields.Text)
                return ExecutionReport.getField(Text.FIELD);
            else if (xrField == ExecutionReportFields.TransactTime)
                return DateTime.Now;
            else if (xrField == ExecutionReportFields.LastQty)
                return FixHelper.GetNullIntFieldIfSet(ExecutionReport, LastQty.FIELD);
            else if (xrField == ExecutionReportFields.LastPx)
                return FixHelper.GetNullDoubleFieldIfSet(ExecutionReport, LastPx.FIELD);
            else if (xrField == ExecutionReportFields.LastMkt)
                return FixHelper.GetFieldIfSet(ExecutionReport,LastMkt.FIELD);


            else if (xrField == ExecutionReportFields.Symbol)
            {
                string primarySymbol = FixHelper.GetFieldIfSet(ExecutionReport, Symbol.FIELD);
                string exchange=ExchangeConverter.GetMarketFromPrimarySymbol(primarySymbol);
                return SymbolConverter.GetFullSymbolFromPrimary(primarySymbol, exchange);
            }
            else if (xrField == ExecutionReportFields.OrderQty)
                return FixHelper.GetNullDoubleFieldIfSet(ExecutionReport, OrderQty.FIELD);
            else if (xrField == ExecutionReportFields.CashOrderQty)
                return FixHelper.GetNullDoubleFieldIfSet(ExecutionReport, CashOrderQty.FIELD);
            else if (xrField == ExecutionReportFields.OrdType)
                return GetOrdType(FixHelper.GetCharFieldIfSet(ExecutionReport, QuickFix.OrdType.FIELD));
            else if (xrField == ExecutionReportFields.Price)
                return FixHelper.GetNullDoubleFieldIfSet(ExecutionReport, Price.FIELD);
            else if (xrField == ExecutionReportFields.StopPx)
                return FixHelper.GetNullDoubleFieldIfSet(ExecutionReport, StopPx.FIELD);
            else if (xrField == ExecutionReportFields.Currency)
                return FixHelper.GetNullFieldIfSet(ExecutionReport, QuickFix.Currency.FIELD);
            else if (xrField == ExecutionReportFields.ExpireDate)
                return FixHelper.GetDateTimeFieldIfSet(ExecutionReport, ExpireDate.FIELD, false);
            else if (xrField == ExecutionReportFields.MinQty)
                return FixHelper.GetNullDoubleFieldIfSet(ExecutionReport, MinQty.FIELD);
            else if (xrField == ExecutionReportFields.Side)
                GetSide(FixHelper.GetCharFieldIfSet(ExecutionReport, QuickFix.Side.FIELD));
            else if (xrField == ExecutionReportFields.QuantityType)
                return zHFT.Main.Common.Enums.QuantityType.SHARES;//In Primary v1.0 we only work with SHARE orders
            else if (xrField == ExecutionReportFields.PriceType)
                return zHFT.Main.Common.Enums.PriceType.FixedAmount;//In Primary v1.0 we only work with FIXED AMMOUNT orders
            else if (xrField == ExecutionReportFields.Account)
                return ExecutionReport.getField(QuickFix.Account.FIELD);
            else if (xrField == ExecutionReportFields.ExecInst)
                return ExecutionReport.getField(QuickFix.ExecInst.FIELD);

            return ExecutionReportFields.NULL;
        }

        #endregion
    }
}
