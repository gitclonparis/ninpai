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
    //
	public class VWAPValueAreaSignalsV2 : Indicator
	{
		private OrderFlowVWAP vwap;
		private double priorSessionUpperBand;
		private double priorSessionLowerBand;
		private bool newSession;
		
		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Upper Offset Ticks", Description = "Number of ticks above upper band")]
		public int UpperOffsetTicks { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Lower Offset Ticks", Description = "Number of ticks below lower band")]
		public int LowerOffsetTicks { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name = "Use Prior SVA UP", Description = "Only up arrows when price above prior session VA")]
		public bool UsePriorSvaUP { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name = "Use Prior SVA Down", Description = "Only down arrows when price below prior session VA")]
		public bool UsePriorSvaDown { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name = "Block In Prior SVA", Description = "Block arrows inside prior session Value Area")]
		public bool BlockInPriorSVA { get; set; }
	
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "VWAP Value Area Signals";
				Name = "VWAPValueAreaSignalsV2";
				UpperOffsetTicks = 5;
				LowerOffsetTicks = 5;
				UsePriorSvaUP = false;
				UsePriorSvaDown = false;
				BlockInPriorSVA = false;
				Calculate = Calculate.OnBarClose;
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
				newSession = true;
			}
			else if (State == State.DataLoaded)
			{
				vwap = OrderFlowVWAP(VWAPResolution.Standard, Bars.TradingHours, 
					VWAPStandardDeviations.Three, 1, 2, 3);
			}
		}
	
		private void UpdatePriorSessionBands()
		{
			if (Bars.IsFirstBarOfSession)
			{
				newSession = true;
				priorSessionUpperBand = vwap.StdDev1Upper[1] + (TickSize * UpperOffsetTicks);
				priorSessionLowerBand = vwap.StdDev1Lower[1] - (TickSize * LowerOffsetTicks);
			}
		}
	
		private bool IsPriceWithinValueArea(double price)
		{
			return price >= priorSessionLowerBand && price <= priorSessionUpperBand;
		}
	
		private void DrawSignals(double price)
		{
			bool isWithinVA = IsPriceWithinValueArea(price);
			
			if (isWithinVA && BlockInPriorSVA) return;
	
			if (price > priorSessionUpperBand && UsePriorSvaUP)
			{
				Draw.ArrowUp(this, "Up" + CurrentBar, true, 0, Low[0] - (2 * TickSize), Brushes.Green);
			}
			else if (price < priorSessionLowerBand && UsePriorSvaDown)
			{
				Draw.ArrowDown(this, "Down" + CurrentBar, true, 0, High[0] + (2 * TickSize), Brushes.Red);
			}
			else
			{
				Draw.ArrowUp(this, "Up" + CurrentBar, true, 0, Low[0] - (2 * TickSize), Brushes.Green);
				Draw.ArrowDown(this, "Down" + CurrentBar, true, 0, High[0] + (2 * TickSize), Brushes.Red);
			}
		}
	
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 1) return;
			
			UpdatePriorSessionBands();
			if (!newSession) return;
			
			DrawSignals(Close[0]);
		}
	}
}