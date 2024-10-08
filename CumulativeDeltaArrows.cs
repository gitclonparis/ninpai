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
	public class CumulativeDeltaArrows : Indicator
	{
		private NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType barsType;
	
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Displays arrows based on Cumulative Delta conditions";
				Name = "Cumulative Delta Arrows";
				Calculate = Calculate.OnBarClose;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
				
				// Default settings
				BarRange = 3;
				DeltaJump = 1000;
				ShowUpArrows = true;
				ShowDownArrows = true;
			}
			else if (State == State.Configure)
			{
				barsType = Bars.BarsSeries.BarsType as NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType;
			}
		}
	
		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarRange)
				return;
	
			if (barsType == null)
				return;
	
			if (ShowUpArrows && CheckUpCondition())
			{
				Draw.ArrowUp(this, "UpArrow" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
			}
	
			if (ShowDownArrows && CheckDownCondition())
			{
				Draw.ArrowDown(this, "DownArrow" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
			}
		}
	
		private bool CheckUpCondition()
		{
			for (int i = 0; i < BarRange - 1; i++)
			{
				if (barsType.Volumes[CurrentBar - i].CumulativeDelta <= 
					barsType.Volumes[CurrentBar - i - 1].CumulativeDelta + DeltaJump)
				{
					return false;
				}
			}
			return true;
		}
	
		private bool CheckDownCondition()
		{
			for (int i = 0; i < BarRange - 1; i++)
			{
				if (barsType.Volumes[CurrentBar - i].CumulativeDelta >= 
					barsType.Volumes[CurrentBar - i - 1].CumulativeDelta - DeltaJump)
				{
					return false;
				}
			}
			return true;
		}
	
		#region Properties
		[Range(2, 5)]
		[NinjaScriptProperty]
		[Display(Name="Bar Range", Description="Number of bars to check (2-5)", Order=1, GroupName="Parameters")]
		public int BarRange { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name="Delta Jump", Description="Minimum jump in Cumulative Delta", Order=2, GroupName="Parameters")]
		public int DeltaJump { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name="Show Up Arrows", Description="Display up arrows", Order=3, GroupName="Display")]
		public bool ShowUpArrows { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name="Show Down Arrows", Description="Display down arrows", Order=4, GroupName="Display")]
		public bool ShowDownArrows { get; set; }
		#endregion
	}
}
