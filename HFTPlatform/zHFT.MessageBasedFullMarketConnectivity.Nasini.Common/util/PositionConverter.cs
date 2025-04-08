using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zHFT.InstructionBasedFullMarketConnectivity.Common;
using zHFT.Main.BusinessEntities.Positions;
using zHFT.Main.BusinessEntities.Securities;
using zHFT.Main.Common.DTO;
using zHFT.Main.Common.Enums;
using zHFT.MarketClient.Primary.Common.Converters;
using zHFT.MessageBasedFullMarketConnectivity.Nasini.Common.DTO.Generic;
using zHFT.MessageBasedFullMarketConnectivity.Nasini.Common.DTO.Position;
using zHFT.MessageBasedFullMarketConnectivity.Nasini.Common.Generic;

namespace zHFT.MessageBasedFullMarketConnectivity.Nasini.Common.util
{

    public class PositionConverter: IPortfolioPositionConverter
    {

        #region Protected Conts

        protected string _DEF_CURRENCY = "ARS";

        protected string _DEF_N_A_VAL = "N/A";

        protected string _OK_VAL = "OK";

        protected string _MERV_EXCH = "MERV";

        #endregion

        #region Public Methods

        public IEnumerable<zHFT.Main.BusinessEntities.Positions.Position> ConvertAccountReport(GenericResponse resp, string accountNumber)
        {
            if (resp is GetAccountReportResp accResp)
            {
                List<zHFT.Main.BusinessEntities.Positions.Position> positions = new List<zHFT.Main.BusinessEntities.Positions.Position>();

                if (!resp.success || accResp.status != _OK_VAL || accResp.accountData == null)
                {
                    positions.Add(new zHFT.Main.BusinessEntities.Positions.Position
                    {
                        Security = new Security
                        {
                            Symbol = "Error: " + (resp.error != null ? resp.error.message : "Unknown error"),
                            SecType = SecurityType.CS,
                            Currency = _DEF_CURRENCY
                        },
                        Exchange = _DEF_N_A_VAL,
                        QuantityType = QuantityType.CURRENCY,
                        PriceType = Main.Common.Enums.PriceType.FixedAmount,
                        Qty = 0,
                        CumQty = 0,
                        LeavesQty = 0,
                        LastPx = 1,
                        Side = Main.Common.Enums.Side.Buy,
                        AccountId = accountNumber
                    });

                    return positions;
                }

                foreach (AccountDetail detail in accResp.accountData.DetailedReportsList)
                {
                    if (detail == null || detail.availableToOperate == null || detail.availableToOperate.cash == null || detail.availableToOperate.cash.detailedCash == null)
                        continue;

                    Dictionary<string, double> detailedCash = detail.availableToOperate.cash.detailedCash;

                    foreach (KeyValuePair<string, double> kvp in detailedCash)
                    {
                        string currency = kvp.Key;
                        double rawAmount = kvp.Value;

                        if (rawAmount <= 0 || string.IsNullOrWhiteSpace(currency))
                            continue;

                        // Convert to decimal for financial calculations
                        double amount = rawAmount;

                        // Extract clean currency code (e.g., "USD" from "USD MtR")
                        string cleanCurrency = currency.Split(' ')[0].ToUpperInvariant();

                        positions.Add(new zHFT.Main.BusinessEntities.Positions.Position
                        {
                            Security = new Security
                            {
                                Symbol = cleanCurrency,
                                SecType = SecurityType.CASH,
                                Currency = cleanCurrency
                            },
                            Exchange = _DEF_N_A_VAL, 
                            QuantityType = QuantityType.CURRENCY,
                            PriceType = Main.Common.Enums.PriceType.FixedAmount,
                            Qty = amount,
                            CumQty = amount,
                            LeavesQty = 0,
                            LastPx = 1,
                            Side = Main.Common.Enums.Side.Buy,
                            AccountId = accountNumber
                        });
                    }
                }

                return positions;
            }
            else
            {
                throw new Exception("Invalid type converting GetAccountReport response: " + resp.GetType().Name);
            }
        }

        public IEnumerable<zHFT.Main.BusinessEntities.Positions.Position> ConvertPositions(GenericResponse resp,
                                                                                           string accountNumber,
                                                                                           bool useCleanSymbol,
                                                                                           Dictionary<string, SecurityType> secTypes)
        {
            if (resp is GetPositionsResp getPosResp)
            {
                List<zHFT.Main.BusinessEntities.Positions.Position> positions = new List<zHFT.Main.BusinessEntities.Positions.Position>();

                if (!resp.success || getPosResp.status != _OK_VAL)
                {
                    positions.Add(new zHFT.Main.BusinessEntities.Positions.Position
                    {
                        Security = new Security
                        {
                            Symbol = $"Error: {resp.error?.message ?? "Unknown error"}",
                            SecType = SecurityType.CS,
                            Currency = _DEF_CURRENCY
                        },
                        Exchange = _DEF_N_A_VAL,
                        QuantityType = QuantityType.SHARES,
                        PriceType = Main.Common.Enums.PriceType.FixedAmount,
                        Qty = 0,
                        CumQty = 0,
                        LeavesQty = 0,
                        LastPx = 0,
                        Side = Main.Common.Enums.Side.Buy,
                        AccountId = accountNumber
                    });

                    return positions;
                }

                foreach (var dto in getPosResp.positions)
                {

                    string market = ExchangeConverter.GetMarketFromPrimarySymbol(dto.symbol);
                    string fullSymbol = SymbolConverter.GetFullSymbolFromPrimary(dto.symbol, market);

                    if (useCleanSymbol)
                        fullSymbol=SymbolConverter.GetCleanSymbolFromFullSymbol(fullSymbol);

                    SecurityType secType = SecurityType.OTH;
                    if (secTypes.Keys.Any(x => x.Contains(fullSymbol)))
                        secType = secTypes[fullSymbol];

                    positions.Add(new zHFT.Main.BusinessEntities.Positions.Position
                    {
                        Security = new Security
                        {
                            Symbol = fullSymbol,
                            SecType = secType,
                            Currency = _OK_VAL
                        },
                        Exchange = market,
                        QuantityType = QuantityType.SHARES,
                        PriceType = Main.Common.Enums.PriceType.FixedAmount,
                        PosStatus = PositionStatus.Filled,
                        Qty = dto.buySize,
                        CumQty = dto.buySize,
                        LeavesQty = 0,
                        AvgPx = dto.buyPrice,
                        Side = dto.buySize > 0 ? Side.Buy : Side.Sell,
                        AccountId = accountNumber
                    });
                }

                return positions;
            }
            else
            {
                throw new Exception($"Invalid type converting Nasini GetPositions response: {resp.GetType()}");
            }
        }

        #endregion
    }
}
