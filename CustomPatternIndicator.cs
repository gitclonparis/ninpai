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

	public class CustomPatternIndicator : Indicator
	{
		private Series<double> upArrows;
		private Series<double> downArrows;
	
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Custom Pattern Indicator";
				Name = "CustomPattern";
				Calculate = Calculate.OnBarClose;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
			}
			else if (State == State.DataLoaded)
			{
				upArrows = new Series<double>(this);
				downArrows = new Series<double>(this);
			}
		}
	
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 3) return;  // Need at least 4 bars for the pattern
	
			// Buy pattern conditions
			bool c1 = High[0] > High[1];
			bool c2 = High[1] > Low[0];
			bool c3 = Low[0] > High[2];
			bool c4 = High[2] > Low[1];
			bool c5 = Low[1] > High[3];
			bool c6 = High[3] > Low[2];
			bool c7 = Low[2] > Low[3];
	
			if (c1 && c2 && c3 && c4 && c5 && c6 && c7)
			{
				Draw.ArrowUp(this, "Up" + CurrentBar, true, 0, Low[0] - TickSize * 2, Brushes.LimeGreen);
			}
	
			// Sell pattern conditions
			c1 = Low[0] < Low[1];
			c2 = Low[1] < High[0];
			c3 = High[0] < Low[2];
			c4 = Low[2] < High[1];
			c5 = High[1] < Low[3];
			c6 = Low[3] < High[2];
			c7 = High[2] < High[3];
	
			if (c1 && c2 && c3 && c4 && c5 && c6 && c7)
			{
				Draw.ArrowDown(this, "Down" + CurrentBar, true, 0, High[0] + TickSize * 2, Brushes.Red);
			}
			
			// Deuxième pattern d'achat (flèches bleues)
			c1 = High[0] > Close[0];
			c2 = Close[0] > High[2];
			c3 = High[2] > High[1];
			c4 = High[1] > Low[0];
			c5 = Low[0] > Low[2];
			c6 = Low[2] > Low[1];
	
			if (c1 && c2 && c3 && c4 && c5 && c6)
			{
				Draw.ArrowUp(this, "Up2_" + CurrentBar, true, 0, Low[0] - TickSize * 3, Brushes.Blue);
			}
	
			// Deuxième pattern de vente (flèches jaunes)
			c1 = Low[0] < Open[0];
			c2 = Open[0] < Low[2];
			c3 = Low[2] < Low[1];
			c4 = Low[1] < High[0];
			c5 = High[0] < High[2];
			c6 = High[2] < High[1];
	
			if (c1 && c2 && c3 && c4 && c5 && c6)
			{
				Draw.ArrowDown(this, "Down2_" + CurrentBar, true, 0, High[0] + TickSize * 3, Brushes.Yellow);
			}
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.CustomPatternIndicator[] cacheCustomPatternIndicator;
		public ninpai.CustomPatternIndicator CustomPatternIndicator()
		{
			return CustomPatternIndicator(Input);
		}

		public ninpai.CustomPatternIndicator CustomPatternIndicator(ISeries<double> input)
		{
			if (cacheCustomPatternIndicator != null)
				for (int idx = 0; idx < cacheCustomPatternIndicator.Length; idx++)
					if (cacheCustomPatternIndicator[idx] != null &&  cacheCustomPatternIndicator[idx].EqualsInput(input))
						return cacheCustomPatternIndicator[idx];
			return CacheIndicator<ninpai.CustomPatternIndicator>(new ninpai.CustomPatternIndicator(), input, ref cacheCustomPatternIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.CustomPatternIndicator CustomPatternIndicator()
		{
			return indicator.CustomPatternIndicator(Input);
		}

		public Indicators.ninpai.CustomPatternIndicator CustomPatternIndicator(ISeries<double> input )
		{
			return indicator.CustomPatternIndicator(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.CustomPatternIndicator CustomPatternIndicator()
		{
			return indicator.CustomPatternIndicator(Input);
		}

		public Indicators.ninpai.CustomPatternIndicator CustomPatternIndicator(ISeries<double> input )
		{
			return indicator.CustomPatternIndicator(input);
		}
	}
}

#endregion
