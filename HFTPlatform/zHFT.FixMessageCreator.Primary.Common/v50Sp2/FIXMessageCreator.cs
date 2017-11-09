using QuickFix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;

namespace zHFT.FixMessageCreator.Primary.Common.v50Sp2
{
    public class FIXMessageCreator : IFIXMessageCreator
    {
        #region Private Consts

        private char _CASH_VOLUME = 'w';

        private char _NOMINAL_VOLUME = 'x';

        #endregion

        #region Protected Methods

        protected void LoadCurrentEntry(Security sec, QuickFix50.MarketDataSnapshotFullRefresh.NoMDEntries entry)
        {
            char type = ' ';
            try
            {
                type = entry.getChar(MDEntryType.FIELD);

                if (type == MDEntryType.BID)
                {
                    double price = entry.getDouble(MDEntryPx.FIELD);
                    double qty = entry.getDouble(MDEntrySize.FIELD);
                    sec.MarketData.BestBidPrice = price;
                    sec.MarketData.BestBidSize = Convert.ToInt64(qty);
                }
                else if (type == MDEntryType.OFFER)
                {
                    double price = entry.getDouble(MDEntryPx.FIELD);
                    double qty = entry.getDouble(MDEntrySize.FIELD);
                    sec.MarketData.BestAskPrice = price;
                    sec.MarketData.BestAskSize = Convert.ToInt64(qty);
                }
                else if (type == MDEntryType.TRADE)
                {
                    double price = entry.getDouble(MDEntryPx.FIELD);
                    double qty = entry.getDouble(MDEntrySize.FIELD);

                    string cond = entry.getField(TradeCondition.FIELD);
                    int trdType = entry.getInt(TrdType.FIELD);

                    if (cond == TradeCondition.EXCHANGE_LAST.ToString()
                        && trdType == TrdType.REGULAR_TRADE)
                    {//we want the Trade field to only have the last and regular trade
                        sec.MarketData.Trade = price;
                        sec.MarketData.MDTradeSize = qty;
                    }
                }
                else if (type == MDEntryType.OPENING_PRICE)
                {
                    double price = entry.getDouble(MDEntryPx.FIELD);
                    sec.MarketData.OpeningPrice = price;
                }
                else if (type == MDEntryType.CLOSING_PRICE)
                {
                    double price = entry.getDouble(MDEntryPx.FIELD);
                    sec.MarketData.ClosingPrice = price;
                }
                else if (type == MDEntryType.SETTLEMENT_PRICE)
                {
                    double price = entry.getDouble(MDEntryPx.FIELD);
                    sec.MarketData.SettlementPrice = price;
                }
                else if (type == MDEntryType.TRADING_SESSION_HIGH_PRICE)
                {
                    double price = entry.getDouble(MDEntryPx.FIELD);
                    sec.MarketData.TradingSessionHighPrice = price;
                }
                else if (type == MDEntryType.TRADING_SESSION_LOW_PRICE)
                {
                    double price = entry.getDouble(MDEntryPx.FIELD);
                    sec.MarketData.TradingSessionLowPrice = price;
                }
                else if (type == MDEntryType.TRADEVOLUME)
                {
                    double qty = entry.getDouble(MDEntrySize.FIELD);
                    sec.MarketData.TradeVolume = qty;
                }
                else if (type == MDEntryType.OPENINTEREST)
                {
                    double qty = entry.getDouble(MDEntrySize.FIELD);
                    sec.MarketData.OpenInterest = qty;
                }
                else if (type == _CASH_VOLUME)//Primary Custom Field
                {
                    double price = entry.getDouble(MDEntryPx.FIELD);
                    sec.MarketData.CashVolume = price;
                }
                else if (type == _NOMINAL_VOLUME)
                {
                    double qty = entry.getDouble(MDEntrySize.FIELD);
                    sec.MarketData.NominalVolume = qty;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error recovering market data for type {0}:{1} ", type, ex.Message));
            }
        }

        #endregion

        #region Public Methods

        public QuickFix.Message RequestMarketData(int id,string symbol)
        {
              
            QuickFix50Sp2.MarketDataRequest mdRequest = new QuickFix50Sp2.MarketDataRequest();

            mdRequest.setString(MDReqID.FIELD,id.ToString());
            mdRequest.setChar(SubscriptionRequestType.FIELD, SubscriptionRequestType.SNAPSHOT_PLUS_UPDATES);
            mdRequest.setInt(MarketDepth.FIELD, 1);
            mdRequest.setInt(MDUpdateType.FIELD, MDUpdateType.FULL);
            mdRequest.setChar(AggregatedBook.FIELD, AggregatedBook.YES);


            QuickFix50Sp2.MarketDataRequest.NoMDEntryTypes entriesBlock = new QuickFix50Sp2.MarketDataRequest.NoMDEntryTypes();
            entriesBlock.setField(new MDEntryType(MDEntryType.BID));
            mdRequest.addGroup(entriesBlock);
            entriesBlock.setField(new MDEntryType(MDEntryType.OFFER));
            mdRequest.addGroup(entriesBlock);
            entriesBlock.setField(new MDEntryType(MDEntryType.TRADE));
            mdRequest.addGroup(entriesBlock);
            entriesBlock.setField(new MDEntryType(MDEntryType.OPENING_PRICE));
            mdRequest.addGroup(entriesBlock);
            entriesBlock.setField(new MDEntryType(MDEntryType.CLOSING_PRICE));
            mdRequest.addGroup(entriesBlock);
            entriesBlock.setField(new MDEntryType(MDEntryType.SETTLEMENT_PRICE));
            mdRequest.addGroup(entriesBlock);
            entriesBlock.setField(new MDEntryType(MDEntryType.TRADING_SESSION_HIGH_PRICE));
            mdRequest.addGroup(entriesBlock);
            entriesBlock.setField(new MDEntryType(MDEntryType.TRADING_SESSION_LOW_PRICE));
            mdRequest.addGroup(entriesBlock);
            entriesBlock.setField(new MDEntryType(MDEntryType.TRADE_VOLUME));
            mdRequest.addGroup(entriesBlock);

            QuickFix50Sp2.MarketDataRequest.NoRelatedSym symbolBlock = new QuickFix50Sp2.MarketDataRequest.NoRelatedSym();
            symbolBlock.setField(new Symbol(symbol));
            mdRequest.addGroup(symbolBlock);

            return mdRequest;
        }

        public QuickFix.Message RequestSecurityList(int secType,string security)
        {
            QuickFix50Sp2.SecurityListRequest rq = new QuickFix50Sp2.SecurityListRequest();

            rq.setInt(QuickFix.SecurityListRequestType.FIELD, secType);
            rq.setString(SecurityReqID.FIELD, security);

            return rq;
        }

        public void ProcessMarketData(QuickFix.Message snapshot, object security, OnLogMessage pOnLogMsg)
        {
            Security sec = (Security)security;
            QuickFix50.MarketDataSnapshotFullRefresh message = (QuickFix50.MarketDataSnapshotFullRefresh )snapshot;
            if (message.isSetNoMDEntries())
            {
                QuickFix50.MarketDataSnapshotFullRefresh.NoMDEntries entry = new QuickFix50.MarketDataSnapshotFullRefresh.NoMDEntries();
                int noEntries = message.getField(new NoMDEntries()).getValue();
                sec.MarketData.MDUpdateAction = UpdateAction.New;
                sec.MarketData.MDEntryDate = DateTime.Now;
                sec.MarketData.MDLocalEntryDate = DateTime.Now;
                sec.MarketData.Security = sec;

                for (uint i = 1; i <= noEntries; i++)
                {
                    message.getGroup(i, entry);
                    try
                    {
                        LoadCurrentEntry(sec, entry);
                    }
                    catch (Exception ex)
                    {
                        pOnLogMsg(string.Format("@{0}:{1}", "ProcessMarketData", ex.Message), Constants.MessageType.Error);
                    }
                }
            }
        }

        #endregion
    }
}
