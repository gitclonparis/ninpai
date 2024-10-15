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
	public class InitialBalanceIndicatorCaudeAi : Indicator
	{
		private DateTime ibStartTime;
		private DateTime ibEndTime;
		private double ibHigh;
		private double ibLow;
		private bool ibCompleted;
		private Dictionary<DateTime, Tuple<double, double>> ibLevels;
	
		[NinjaScriptProperty]
		[Display(Name="Activer IB", Description="Activer l'indicateur Initial Balance", Order=1, GroupName="Paramètres")]
		public bool EnableIB { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name="Heure de début IB", Description="Heure de début de l'Initial Balance (HH:mm)", Order=2, GroupName="Paramètres")]
		public string IBStartTime { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name="Heure de fin IB", Description="Heure de fin de l'Initial Balance (HH:mm)", Order=3, GroupName="Paramètres")]
		public string IBEndTime { get; set; }
	
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Ticks de breakout", Description="Nombre de ticks pour confirmer un breakout", Order=4, GroupName="Paramètres")]
		public int BreakoutTicks { get; set; }
	
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Indicateur Initial Balance amélioré";
				Name = "Claude AI Initial Balance Indicator";
				Calculate = Calculate.OnBarClose;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
				
				EnableIB = true;
				IBStartTime = "15:30";
				IBEndTime = "16:00";
				BreakoutTicks = 2;
	
				ibLevels = new Dictionary<DateTime, Tuple<double, double>>();
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Day, 1);
			}
		}
	
		protected override void OnBarUpdate()
		{
			if (!EnableIB || Bars == null || CurrentBar < 1) return;
	
			DateTime currentBarTime = Time[0];
			DateTime currentDate = currentBarTime.Date;
	
			// Vérifier si c'est un nouveau jour de trading
			if (Bars.IsFirstBarOfSession && ibLevels.ContainsKey(currentDate))
			{
				ibLevels.Remove(currentDate);
			}
	
			TimeSpan ibStart = TimeSpan.Parse(IBStartTime);
			TimeSpan ibEnd = TimeSpan.Parse(IBEndTime);
	
			// Vérifier si c'est le début de l'IB
			if (currentBarTime.TimeOfDay == ibStart)
			{
				ibStartTime = currentBarTime;
				ibEndTime = currentDate.Add(ibEnd);
				ibHigh = High[0];
				ibLow = Low[0];
				ibCompleted = false;
			}
	
			// Pendant l'IB
			if (currentBarTime >= ibStartTime && currentBarTime <= ibEndTime)
			{
				ibHigh = Math.Max(ibHigh, High[0]);
				ibLow = Math.Min(ibLow, Low[0]);
	
				Draw.ArrowUp(this, "Up" + CurrentBar, false, 0, Low[0] - TickSize, Brushes.Green);
				Draw.ArrowDown(this, "Down" + CurrentBar, false, 0, High[0] + TickSize, Brushes.Red);
			}
	
			// Après l'IB
			if (currentBarTime > ibEndTime && !ibCompleted)
			{
				ibCompleted = true;
				ibLevels[currentDate] = new Tuple<double, double>(ibHigh, ibLow);
	
				// Dessiner les lignes IBH et IBL
				if (ibEndTime > DateTime.MinValue && ibEndTime <= currentBarTime)
				{
					Draw.Line(this, "IBH_" + currentDate.ToShortDateString(), false, ibEndTime, ibHigh, currentBarTime, ibHigh, Brushes.Blue, DashStyleHelper.Solid, 2);
					Draw.Line(this, "IBL_" + currentDate.ToShortDateString(), false, ibEndTime, ibLow, currentBarTime, ibLow, Brushes.Red, DashStyleHelper.Solid, 2);
				}
			}
	
			if (ibCompleted && ibLevels.ContainsKey(currentDate))
			{
				double ibHigh = ibLevels[currentDate].Item1;
				double ibLow = ibLevels[currentDate].Item2;
				double breakoutHighLevel = ibHigh + (BreakoutTicks * TickSize);
				double breakoutLowLevel = ibLow - (BreakoutTicks * TickSize);
	
				if (Close[0] > breakoutHighLevel)
				{
					Draw.ArrowUp(this, "BreakoutUp" + CurrentBar, false, 0, Low[0] - TickSize, Brushes.Blue);
				}
				else if (Close[0] < breakoutLowLevel)
				{
					Draw.ArrowDown(this, "BreakoutDown" + CurrentBar, false, 0, High[0] + TickSize, Brushes.Purple);
				}
				else
				{
					Draw.ArrowUp(this, "Up" + CurrentBar, false, 0, Low[0] - TickSize, Brushes.Green);
					Draw.ArrowDown(this, "Down" + CurrentBar, false, 0, High[0] + TickSize, Brushes.Red);
				}
			}
	
			// Dessiner les lignes IB pour les jours précédents
			if (BarsInProgress == 1)
			{
				foreach (var kvp in ibLevels)
				{
					if (kvp.Key < currentDate)
					{
						DateTime startTime = kvp.Key.Add(ibStart);
						if (startTime > DateTime.MinValue && startTime <= currentBarTime)
						{
							Draw.Line(this, "IBH_" + kvp.Key.ToShortDateString(), false, startTime, kvp.Value.Item1, currentBarTime, kvp.Value.Item1, Brushes.Blue, DashStyleHelper.Dash, 1);
							Draw.Line(this, "IBL_" + kvp.Key.ToShortDateString(), false, startTime, kvp.Value.Item2, currentBarTime, kvp.Value.Item2, Brushes.Red, DashStyleHelper.Dash, 1);
						}
					}
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
		private ninpai.InitialBalanceIndicatorCaudeAi[] cacheInitialBalanceIndicatorCaudeAi;
		public ninpai.InitialBalanceIndicatorCaudeAi InitialBalanceIndicatorCaudeAi(bool enableIB, string iBStartTime, string iBEndTime, int breakoutTicks)
		{
			return InitialBalanceIndicatorCaudeAi(Input, enableIB, iBStartTime, iBEndTime, breakoutTicks);
		}

		public ninpai.InitialBalanceIndicatorCaudeAi InitialBalanceIndicatorCaudeAi(ISeries<double> input, bool enableIB, string iBStartTime, string iBEndTime, int breakoutTicks)
		{
			if (cacheInitialBalanceIndicatorCaudeAi != null)
				for (int idx = 0; idx < cacheInitialBalanceIndicatorCaudeAi.Length; idx++)
					if (cacheInitialBalanceIndicatorCaudeAi[idx] != null && cacheInitialBalanceIndicatorCaudeAi[idx].EnableIB == enableIB && cacheInitialBalanceIndicatorCaudeAi[idx].IBStartTime == iBStartTime && cacheInitialBalanceIndicatorCaudeAi[idx].IBEndTime == iBEndTime && cacheInitialBalanceIndicatorCaudeAi[idx].BreakoutTicks == breakoutTicks && cacheInitialBalanceIndicatorCaudeAi[idx].EqualsInput(input))
						return cacheInitialBalanceIndicatorCaudeAi[idx];
			return CacheIndicator<ninpai.InitialBalanceIndicatorCaudeAi>(new ninpai.InitialBalanceIndicatorCaudeAi(){ EnableIB = enableIB, IBStartTime = iBStartTime, IBEndTime = iBEndTime, BreakoutTicks = breakoutTicks }, input, ref cacheInitialBalanceIndicatorCaudeAi);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.InitialBalanceIndicatorCaudeAi InitialBalanceIndicatorCaudeAi(bool enableIB, string iBStartTime, string iBEndTime, int breakoutTicks)
		{
			return indicator.InitialBalanceIndicatorCaudeAi(Input, enableIB, iBStartTime, iBEndTime, breakoutTicks);
		}

		public Indicators.ninpai.InitialBalanceIndicatorCaudeAi InitialBalanceIndicatorCaudeAi(ISeries<double> input , bool enableIB, string iBStartTime, string iBEndTime, int breakoutTicks)
		{
			return indicator.InitialBalanceIndicatorCaudeAi(input, enableIB, iBStartTime, iBEndTime, breakoutTicks);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.InitialBalanceIndicatorCaudeAi InitialBalanceIndicatorCaudeAi(bool enableIB, string iBStartTime, string iBEndTime, int breakoutTicks)
		{
			return indicator.InitialBalanceIndicatorCaudeAi(Input, enableIB, iBStartTime, iBEndTime, breakoutTicks);
		}

		public Indicators.ninpai.InitialBalanceIndicatorCaudeAi InitialBalanceIndicatorCaudeAi(ISeries<double> input , bool enableIB, string iBStartTime, string iBEndTime, int breakoutTicks)
		{
			return indicator.InitialBalanceIndicatorCaudeAi(input, enableIB, iBStartTime, iBEndTime, breakoutTicks);
		}
	}
}

#endregion
