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
    public class PHL2 : Indicator
    {
        private PriorDayOHLC PriorDayOHLC1;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Indicateur BVA-Limusine combiné";
                Name = "PHL2";
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
				AddDataSeries("ES ETH", BarsPeriodType.Minute, 5, MarketDataType.Last);
                // AddDataSeries(BarsPeriodType.Minute, 5);
            }
            else if (State == State.DataLoaded)
            {
                 PriorDayOHLC1 = PriorDayOHLC(BarsArray[1]);
            }
        }

        protected override void OnBarUpdate()
        {
			// Vérifier si nous avons assez de barres
			if (CurrentBars[0] < 0 || CurrentBars[1] < 0)
				return;
	
			// Ne mettre à jour que lorsque la série principale (1 min RTH) est mise à jour
			if (BarsInProgress == 0)
			{
				// Utiliser les données de la série ETH (BarsInProgress 1) pour la comparaison
				if (Closes[1][0] > PriorDayOHLC1.PriorHigh[0])
				{
					Draw.ArrowUp(this, "PHL Arrow up" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Lime);
				}
				else if (Closes[1][0] < PriorDayOHLC1.PriorLow[0])
				{
					Draw.ArrowDown(this, "PHL Arrow down" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
				}
				else if (Closes[1][0] < PriorDayOHLC1.PriorHigh[0] && Closes[1][0] > PriorDayOHLC1.PriorLow[0])
				{
					Draw.Dot(this, "PHL Dot" + CurrentBar, true, 0, Close[0], Brushes.CornflowerBlue);
				}
			}
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.PHL2[] cachePHL2;
		public ninpai.PHL2 PHL2()
		{
			return PHL2(Input);
		}

		public ninpai.PHL2 PHL2(ISeries<double> input)
		{
			if (cachePHL2 != null)
				for (int idx = 0; idx < cachePHL2.Length; idx++)
					if (cachePHL2[idx] != null &&  cachePHL2[idx].EqualsInput(input))
						return cachePHL2[idx];
			return CacheIndicator<ninpai.PHL2>(new ninpai.PHL2(), input, ref cachePHL2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.PHL2 PHL2()
		{
			return indicator.PHL2(Input);
		}

		public Indicators.ninpai.PHL2 PHL2(ISeries<double> input )
		{
			return indicator.PHL2(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.PHL2 PHL2()
		{
			return indicator.PHL2(Input);
		}

		public Indicators.ninpai.PHL2 PHL2(ISeries<double> input )
		{
			return indicator.PHL2(input);
		}
	}
}

#endregion
