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
    public class PHL : Indicator
    {
        private PriorDayOHLC PriorDayOHLC1;
        private enum TradingPermission
        {
            None,
            BuyOnly,
            SellOnly,
            Both
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Indicateur BVA-Limusine combiné";
                Name = "PHL";
                Calculate = Calculate.OnEachTick;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;
            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.DataLoaded)
            {
                PriorDayOHLC1 = PriorDayOHLC(Close);
            }
        }
		
		private TradingPermission GetTradingPermission()
        {
            if (Close[0] > PriorDayOHLC1.PriorHigh[0])
            {
                return TradingPermission.BuyOnly;
            }
            else if (Close[0] < PriorDayOHLC1.PriorLow[0])
            {
                return TradingPermission.SellOnly;
            }
            else if (Close[0] < PriorDayOHLC1.PriorHigh[0] && Close[0] > PriorDayOHLC1.PriorLow[0])
            {
                return TradingPermission.Both;
            }
            return TradingPermission.None;
        }

        protected override void OnBarUpdate()
        {
			// Vérifier si nous avons assez de barres
            if (CurrentBars[0] < 0)
                return;

            // Obtenir la permission de trading
            TradingPermission permission = GetTradingPermission();

            // Dessiner les indicateurs appropriés
            switch (permission)
            {
                case TradingPermission.BuyOnly:
                    Draw.ArrowUp(this, "PHL Arrow up" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Lime);
                    break;

                case TradingPermission.SellOnly:
                    Draw.ArrowDown(this, "PHL Arrow down" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
                    break;

                case TradingPermission.Both:
                    Draw.Dot(this, "PHL Dot" + CurrentBar, true, 0, Close[0], Brushes.CornflowerBlue);
                    break;
            }
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.PHL[] cachePHL;
		public ninpai.PHL PHL()
		{
			return PHL(Input);
		}

		public ninpai.PHL PHL(ISeries<double> input)
		{
			if (cachePHL != null)
				for (int idx = 0; idx < cachePHL.Length; idx++)
					if (cachePHL[idx] != null &&  cachePHL[idx].EqualsInput(input))
						return cachePHL[idx];
			return CacheIndicator<ninpai.PHL>(new ninpai.PHL(), input, ref cachePHL);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.PHL PHL()
		{
			return indicator.PHL(Input);
		}

		public Indicators.ninpai.PHL PHL(ISeries<double> input )
		{
			return indicator.PHL(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.PHL PHL()
		{
			return indicator.PHL(Input);
		}

		public Indicators.ninpai.PHL PHL(ISeries<double> input )
		{
			return indicator.PHL(input);
		}
	}
}

#endregion
