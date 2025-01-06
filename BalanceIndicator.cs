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

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class BalanceIndicator : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "BalanceIndicator";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				Period					= 50;
				AddPlot(Brushes.Violet, "BalanceLine");
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
			if(CurrentBar < Period)
				return; 
			
			// MAX(Period) => What is the Maximum close price in the last 50 bars?
			// MIN(Period) => What is the Minimum close price in the last 50 bars?
			
			BalanceLine[0] = (MAX(Period)[0] + MIN(Period)[0]) / 2; // Balance line
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Period", Order=1, GroupName="Parameters")]
		public int Period
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> BalanceLine // [1,2,3,3.4,5.6,..]
		{
			get { return Values[0]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BalanceIndicator[] cacheBalanceIndicator;
		public BalanceIndicator BalanceIndicator(int period)
		{
			return BalanceIndicator(Input, period);
		}

		public BalanceIndicator BalanceIndicator(ISeries<double> input, int period)
		{
			if (cacheBalanceIndicator != null)
				for (int idx = 0; idx < cacheBalanceIndicator.Length; idx++)
					if (cacheBalanceIndicator[idx] != null && cacheBalanceIndicator[idx].Period == period && cacheBalanceIndicator[idx].EqualsInput(input))
						return cacheBalanceIndicator[idx];
			return CacheIndicator<BalanceIndicator>(new BalanceIndicator(){ Period = period }, input, ref cacheBalanceIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BalanceIndicator BalanceIndicator(int period)
		{
			return indicator.BalanceIndicator(Input, period);
		}

		public Indicators.BalanceIndicator BalanceIndicator(ISeries<double> input , int period)
		{
			return indicator.BalanceIndicator(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BalanceIndicator BalanceIndicator(int period)
		{
			return indicator.BalanceIndicator(Input, period);
		}

		public Indicators.BalanceIndicator BalanceIndicator(ISeries<double> input , int period)
		{
			return indicator.BalanceIndicator(input, period);
		}
	}
}

#endregion
