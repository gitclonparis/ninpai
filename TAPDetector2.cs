#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.ninpai
{
    public class TAPDetector2 : Indicator
    {
        private const int ARROW_SIZE = 12;
        private bool isTickReplayEnabled = false;
        private double lastBid = 0;
        private double lastAsk = 0;
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Taille minimum du lot", Description = "Taille minimum du lot à détecter", Order = 1, GroupName = "Paramètres")]
        public int MinLotSize { get; set; }
    
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Détecte les transactions TAP d'une taille spécifique";
                Name = "TAPDetector2";
                Calculate = Calculate.OnEachTick;
                IsOverlay = true;
                MinLotSize = 50;
            }
        }
    
        protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
        {
            // Mise à jour des prix bid/ask
            if (marketDataUpdate.MarketDataType == MarketDataType.Ask)
            {
                lastAsk = marketDataUpdate.Price;
                return;
            }
            else if (marketDataUpdate.MarketDataType == MarketDataType.Bid)
            {
                lastBid = marketDataUpdate.Price;
                return;
            }
            
            // Traitement uniquement des transactions (Last)
            if (marketDataUpdate.MarketDataType != MarketDataType.Last)
                return;

            // Vérification de la taille minimum du lot
            if (marketDataUpdate.Volume < MinLotSize)
                return;

            string timeKey = Time[0].Ticks.ToString();
            
            // TAP à l'achat : transaction au prix ask ou au-dessus
            if (lastAsk > 0 && marketDataUpdate.Price >= lastAsk)
            {
                Draw.ArrowUp(this, "Up_" + timeKey, false, 0, 
                    Low[0] - TickSize * 2, 
                    Brushes.Green, false);
            }
            // TAP à la vente : transaction au prix bid ou en-dessous
            else if (lastBid > 0 && marketDataUpdate.Price <= lastBid)
            {
                Draw.ArrowDown(this, "Down_" + timeKey, false, 0,
                    High[0] + TickSize * 2, 
                    Brushes.Red, false);
            }
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.TAPDetector2[] cacheTAPDetector2;
		public ninpai.TAPDetector2 TAPDetector2(int minLotSize)
		{
			return TAPDetector2(Input, minLotSize);
		}

		public ninpai.TAPDetector2 TAPDetector2(ISeries<double> input, int minLotSize)
		{
			if (cacheTAPDetector2 != null)
				for (int idx = 0; idx < cacheTAPDetector2.Length; idx++)
					if (cacheTAPDetector2[idx] != null && cacheTAPDetector2[idx].MinLotSize == minLotSize && cacheTAPDetector2[idx].EqualsInput(input))
						return cacheTAPDetector2[idx];
			return CacheIndicator<ninpai.TAPDetector2>(new ninpai.TAPDetector2(){ MinLotSize = minLotSize }, input, ref cacheTAPDetector2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.TAPDetector2 TAPDetector2(int minLotSize)
		{
			return indicator.TAPDetector2(Input, minLotSize);
		}

		public Indicators.ninpai.TAPDetector2 TAPDetector2(ISeries<double> input , int minLotSize)
		{
			return indicator.TAPDetector2(input, minLotSize);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.TAPDetector2 TAPDetector2(int minLotSize)
		{
			return indicator.TAPDetector2(Input, minLotSize);
		}

		public Indicators.ninpai.TAPDetector2 TAPDetector2(ISeries<double> input , int minLotSize)
		{
			return indicator.TAPDetector2(input, minLotSize);
		}
	}
}

#endregion
