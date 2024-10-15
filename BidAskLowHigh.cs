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
	public class BidAskLowHigh : Indicator
	{
		private NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType barsType;
	
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Indicator based on Order Flow Volumetric Bars";
				Name = "BidAskLowHigh";
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
				barsType = Bars.BarsSeries.BarsType as NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType;
				if (barsType == null)
					throw new Exception("This indicator requires Volumetric Bars.");
			}
		}
	
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 3) return;
	
			// Check for UP condition
			if (Close[0] > Open[0]) // Green bar
			{
				double lowPrice = Low[0];
				double level1 = barsType.Volumes[0].GetAskVolumeForPrice(lowPrice);
				double level2 = barsType.Volumes[0].GetAskVolumeForPrice(lowPrice + (1 * TickSize));
				double level3 = barsType.Volumes[0].GetAskVolumeForPrice(lowPrice + (2 * TickSize));
	
				if (level1 < level2 && level2 < level3)
				{
					Draw.ArrowUp(this, "UpArrow" + CurrentBar, true, 0, Low[0] - (2 * TickSize), Brushes.LimeGreen);
				}
			}
	
			// TODO: Implement DOWN condition
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.BidAskLowHigh[] cacheBidAskLowHigh;
		public ninpai.BidAskLowHigh BidAskLowHigh()
		{
			return BidAskLowHigh(Input);
		}

		public ninpai.BidAskLowHigh BidAskLowHigh(ISeries<double> input)
		{
			if (cacheBidAskLowHigh != null)
				for (int idx = 0; idx < cacheBidAskLowHigh.Length; idx++)
					if (cacheBidAskLowHigh[idx] != null &&  cacheBidAskLowHigh[idx].EqualsInput(input))
						return cacheBidAskLowHigh[idx];
			return CacheIndicator<ninpai.BidAskLowHigh>(new ninpai.BidAskLowHigh(), input, ref cacheBidAskLowHigh);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.BidAskLowHigh BidAskLowHigh()
		{
			return indicator.BidAskLowHigh(Input);
		}

		public Indicators.ninpai.BidAskLowHigh BidAskLowHigh(ISeries<double> input )
		{
			return indicator.BidAskLowHigh(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.BidAskLowHigh BidAskLowHigh()
		{
			return indicator.BidAskLowHigh(Input);
		}

		public Indicators.ninpai.BidAskLowHigh BidAskLowHigh(ISeries<double> input )
		{
			return indicator.BidAskLowHigh(input);
		}
	}
}

#endregion
