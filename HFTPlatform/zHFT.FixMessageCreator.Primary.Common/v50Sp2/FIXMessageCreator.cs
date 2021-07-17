using QuickFix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.Main.BusinessEntities.Orders;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Interfaces;
using zHFT.Main.Common.Util;
using MarketDepth = QuickFix.MarketDepth;

namespace zHFT.FixMessageCreator.Primary.Common.v50Sp2
{
    public class FIXMessageCreator : IFIXMessageCreator
    {
        #region Private Consts

        private char _CASH_VOLUME = 'w';

        private char _NOMINAL_VOLUME = 'x';

        #endregion

        #region Protected Methods

        protected  string GetUnixTimeStamp(DateTime baseDateTime)
        {
            var dtOffset = new DateTimeOffset(baseDateTime);
            return dtOffset.ToUnixTimeMilliseconds().ToString();
        }

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
                    if (entry.isSetTradeCondition())
                    {
                        string cond = entry.getField(TradeCondition.FIELD);
                        //int trdType = entry.getInt(TrdType.FIELD);

                        if (cond == TradeCondition.EXCHANGE_LAST.ToString())
                        {//we want the Trade field to only have the last and regular trade
                            sec.MarketData.Trade = price;
                            sec.MarketData.MDTradeSize = qty;
                        }
                    }

                    if (entry.isSetMDEntryDate())
                    {
                        MDEntryDate mdDate = entry.getMDEntryDate();
                        MDEntryTime mdTime = entry.getMDEntryTime();
                        DateTime time = mdTime.getValue();
                        DateTime date = mdDate.getValue();
                        DateTime datetime = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond);

                        sec.MarketData.LastTradeDateTime = datetime;
                    
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

        protected void ValidateNewOrderSingleFields(string account, string clOrderId, zHFT.Main.Common.Enums.OrdType ordType,
                                                    double? price,double? stopPx)
        {

            if (string.IsNullOrEmpty(account))
                throw new Exception("Must specify an account for a new order");

            if (string.IsNullOrEmpty(clOrderId))
                throw new Exception("Must specify an order id for a new order");

            if (ordType == zHFT.Main.Common.Enums.OrdType.Limit
              || ordType == zHFT.Main.Common.Enums.OrdType.LimitOnClose
             )
            {
                if (!price.HasValue)
                    throw new Exception("Must specify a price for a limit order");
            }

            if (ordType == zHFT.Main.Common.Enums.OrdType.StopLimit)
            {
                if (!stopPx.HasValue)
                    throw new Exception("Must specify a price for a stop limit order");
            }
        
        }

        protected void ValidateOrderCancelReplaceRequestFields(string account, string clOrderId,string origClOrderId,string orderId,
                                                               zHFT.Main.Common.Enums.OrdType ordType, double? price, double? stopPx,
                                                               string symbol, double? orderQty)
        {
            if (string.IsNullOrEmpty(account))
                throw new Exception("Must specify an account for a updated order");

            if (string.IsNullOrEmpty(clOrderId))
                throw new Exception("Must specify an order id for a updated order");

            if (string.IsNullOrEmpty(symbol))
                throw new Exception("Must specify a symbol a updated order");

            if (!orderQty.HasValue)
                throw new Exception("Must specify an order quantity for an updated order");



            if (string.IsNullOrEmpty(origClOrderId) && string.IsNullOrEmpty(orderId))
                throw new Exception("Must specify an OrigClOrderId or OrderId for an updated order");

            if (ordType == zHFT.Main.Common.Enums.OrdType.Limit
              || ordType == zHFT.Main.Common.Enums.OrdType.LimitOnClose
             )
            {
                if (!price.HasValue)
                    throw new Exception("Must specify a price for a limit order");
            }

            if (ordType == zHFT.Main.Common.Enums.OrdType.StopLimit)
            {
                if (!stopPx.HasValue)
                    throw new Exception("Must specify a price for a stop limit order");
            }

        
        }

        protected void ValidateOrderCancelRequestFields(string account, string clOrderId,string origClOrdId , 
                                                        string orderId)
        {

            if (string.IsNullOrEmpty(account))
                throw new Exception("Must specify an account for a new order");

            if (string.IsNullOrEmpty(clOrderId))
                throw new Exception("Must specify an order id for a new order");

            if (string.IsNullOrEmpty(origClOrdId) && string.IsNullOrEmpty(orderId))
                throw new Exception("Must specify a client order id or an exchange order id");

        }

        protected char GetSuscriptionRequestType(zHFT.Main.Common.Enums.SubscriptionRequestType pSubscriptionRequestType)
        {
            if (pSubscriptionRequestType == zHFT.Main.Common.Enums.SubscriptionRequestType.Snapshot)
                return QuickFix.SubscriptionRequestType.SNAPSHOT;
            else if (pSubscriptionRequestType == zHFT.Main.Common.Enums.SubscriptionRequestType.SnapshotAndUpdates)
                return QuickFix.SubscriptionRequestType.SNAPSHOT_PLUS_UPDATES;
            if (pSubscriptionRequestType == zHFT.Main.Common.Enums.SubscriptionRequestType.Unsuscribe)
                return QuickFix.SubscriptionRequestType.DISABLE_PREVIOUS_SNAPSHOT_PLUS_UPDATE_REQUEST;
            else throw new Exception(string.Format("Could not recognize subscription request type {0}", pSubscriptionRequestType.ToString()));
        }

        #endregion

        #region Public Methods

        public QuickFix.Message RequestMarketData(int id, string symbol, zHFT.Main.Common.Enums.SubscriptionRequestType pSubscriptionRequestType)
        {
              
            QuickFix50Sp2.MarketDataRequest mdRequest = new QuickFix50Sp2.MarketDataRequest();

            mdRequest.setString(MDReqID.FIELD,id.ToString());
            mdRequest.setChar(QuickFix.SubscriptionRequestType.FIELD, GetSuscriptionRequestType(pSubscriptionRequestType));
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

            //symbolBlock.setField(new QuickFix.Currency("USD"));


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


        public QuickFix.Message CreateNewOrderSingle(string clOrderId, string symbol,
                                                     zHFT.Main.Common.Enums.Side side,
                                                     zHFT.Main.Common.Enums.OrdType ordType,
                                                     zHFT.Main.Common.Enums.SettlType? settlType,
                                                     zHFT.Main.Common.Enums.TimeInForce? timeInForce,
                                                     DateTime effectiveTime,
                                                     double ordQty,double? price,double? stopPx,string account)
        {


            ValidateNewOrderSingleFields(account, clOrderId, ordType, price, stopPx);

            QuickFix50Sp2.NewOrderSingle nos = new QuickFix50Sp2.NewOrderSingle();

            nos.setField(Account.FIELD, account);
            nos.setField(ClOrdID.FIELD, clOrderId);
            nos.setUtcTimeStamp(TransactTime.FIELD, effectiveTime);
            nos.setField(Symbol.FIELD, symbol);
            nos.setDouble(OrderQty.FIELD, ordQty);
            nos.setChar(QuickFix.OrdType.FIELD, Convert.ToChar(ordType));

            if (ordType == zHFT.Main.Common.Enums.OrdType.Limit || ordType == zHFT.Main.Common.Enums.OrdType.LimitOnClose)
            {
                nos.setDouble(QuickFix.Price.FIELD, price.Value);
            }

            if (ordType == zHFT.Main.Common.Enums.OrdType.StopLimit)
            {
                nos.setDouble(QuickFix.Price.FIELD, stopPx.Value);
            }

            nos.setChar(QuickFix.Side.FIELD, Convert.ToChar(side));

            //TODO: Completar parte de BlockParties si tiene sentido

            if(timeInForce.HasValue)
                nos.setChar(QuickFix.TimeInForce.FIELD, Convert.ToChar(timeInForce.Value));

            return nos;

        }

        public QuickFix.Message CreateOrderMassCancelRequest()
        {
            QuickFix50Sp2.OrderMassCancelRequest msg = new QuickFix50Sp2.OrderMassCancelRequest();

            msg.setChar(MassCancelRequestType.FIELD, MassCancelRequestType.CANCEL_ALL_ORDERS);

            string clOrdId = GetUnixTimeStamp(DateTime.Now);

            msg.setString(ClOrdID.FIELD, clOrdId);

            return msg;
        }


        public QuickFix.Message CreateOrderCancelReplaceRequest(string clOrderId,string orderId,string origClOrdId, 
                                                             string symbol,
                                                             zHFT.Main.Common.Enums.Side side,
                                                             zHFT.Main.Common.Enums.OrdType ordType,
                                                             zHFT.Main.Common.Enums.SettlType? settlType,
                                                             zHFT.Main.Common.Enums.TimeInForce? timeInForce,
                                                             DateTime effectiveTime,
                                                             double? ordQty, double? price, double? stopPx, string account)
        {


            ValidateOrderCancelReplaceRequestFields(account, clOrderId, origClOrdId, orderId, ordType, price, stopPx, symbol, ordQty);

            QuickFix50Sp2.OrderCancelReplaceRequest ocr = new QuickFix50Sp2.OrderCancelReplaceRequest();

            //ocr.setField(DeliverToCompID.FIELD, "ROFX");
            ocr.setField(Account.FIELD, account);
            ocr.setField(ClOrdID.FIELD, clOrderId);
            ocr.setField(ExecInst.FIELD, "x");
            ocr.setField(OrderID.FIELD, orderId);

            if (ordQty.HasValue)
                ocr.setDouble(OrderQty.FIELD, ordQty.Value);

            ocr.setChar(QuickFix.OrdType.FIELD, Convert.ToChar(ordType));

            ocr.setField(OrigClOrdID.FIELD, origClOrdId);

            if (ordType == zHFT.Main.Common.Enums.OrdType.Limit || ordType == zHFT.Main.Common.Enums.OrdType.LimitOnClose)
                ocr.setDouble(QuickFix.Price.FIELD, price.Value);
            if (ordType == zHFT.Main.Common.Enums.OrdType.StopLimit)
                ocr.setDouble(QuickFix.Price.FIELD, stopPx.Value);

            ocr.setChar(QuickFix.Side.FIELD, Convert.ToChar(side));

            ocr.setField(Symbol.FIELD, symbol);

            if (timeInForce.HasValue)
                ocr.setChar(QuickFix.TimeInForce.FIELD, Convert.ToChar(timeInForce.Value));

            ocr.setUtcTimeStamp(TransactTime.FIELD, effectiveTime);

            ocr.setField(SecurityExchange.FIELD, "ROFX");


            QuickFix50Sp2.OrderCancelReplaceRequest.NoPartyIDs partiesBlock = new QuickFix50Sp2.OrderCancelReplaceRequest.NoPartyIDs();
            partiesBlock.set(new PartyRole(PartyRole.INITIATINGTRADER));
            partiesBlock.set(new PartyID("fmosca"));
            partiesBlock.set(new PartyIDSource(PartyIDSource.PROPRIETARY_CUSTOM_CODE)); //Valor Fijo obligatorio
            ocr.addGroup(partiesBlock);

            return ocr;

        }

        public QuickFix.Message CreateOrderCancelRequest(string clOrderId,string origClOrderId, string orderId, string symbol,
                                                          zHFT.Main.Common.Enums.Side side, DateTime effectiveTime,
                                                          double? ordQty, string account, string mainExchange)
        {

            ValidateOrderCancelRequestFields(account, clOrderId, origClOrderId, orderId);

            QuickFix50Sp2.OrderCancelRequest ocr = new QuickFix50Sp2.OrderCancelRequest();

            ocr.setField(Account.FIELD, account);
            ocr.setField(ClOrdID.FIELD, clOrderId);
            
            ocr.setField(OrderID.FIELD, orderId);
            ocr.setField(OrigClOrdID.FIELD, origClOrderId);
            ocr.setChar(QuickFix.Side.FIELD, Convert.ToChar(side));
            ocr.setUtcTimeStamp(TransactTime.FIELD, effectiveTime);

            if (ordQty.HasValue)
                ocr.setDouble(OrderQty.FIELD, ordQty.Value);

            ocr.setField(Symbol.FIELD, symbol);

            ocr.setField(SecurityExchange.FIELD, mainExchange);

            return ocr;
        }

        public QuickFix.Message CreateOrderMassStatusRequest(string reqId)
        {
            QuickFix50Sp2.OrderMassStatusRequest omsr = new QuickFix50Sp2.OrderMassStatusRequest();
            
            omsr.setField(MassStatusReqID.FIELD, reqId);
            
            omsr.setInt(MassStatusReqType.FIELD, MassStatusReqType.STATUS_FOR_ALL_ORDERS);

            return omsr;
        }


        #endregion
    }
}
