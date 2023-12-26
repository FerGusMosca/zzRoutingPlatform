using System;
using System.Collections.Generic;
using System.Linq;
using tph.DayTurtles.BusinessEntities;
using tph.TrendlineTurtles.BusinessEntities;
using tph.TrendlineTurtles.LogicLayer;
using zHFT.Main.BusinessEntities.Market_Data;
using zHFT.Main.Common.Enums;
using zHFT.Main.Common.Util;
using zHFT.StrategyHandler.BusinessEntities;

namespace tph.BOBDayTurtles.BusinessEntities
{
    public class MonBOBTurtlePosition : MonTrendlineTurtlesPosition
    {
        
        #region Constructors

        public MonBOBTurtlePosition(TurtlesCustomConfig pTurtlesCustomWindow, double stopLossForOpenPositionPct,
            int outerSignalSpan,string candleReferencePrice) : base(pTurtlesCustomWindow, stopLossForOpenPositionPct,candleReferencePrice)
        {
            Resistances = new List<Trendline>();
            Supports = new List<Trendline>();
            OuterSignalSpan = outerSignalSpan;
        }

        #endregion

        #region Public Methods

        public override bool LongSignalTriggered()
        {
            return EvalResistanceBroken() && IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, false);
            
        }
        
        public override bool ShortSignalTriggered()
        {
            return EvalSupportBroken() && !IsHigherThanMMov(TurtlesCustomConfig.CloseWindow, false);
        }
        
        #endregion
    }
}