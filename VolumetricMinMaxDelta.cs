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
	public class VolumetricMinMaxDelta : Indicator
	{
		private NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType barsType;
	
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Min Positive Delta Up", Order=1, GroupName="Parameters")]
		public int MinPositiveDeltaUp { get; set; }
	
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Max Positive Delta Up", Order=2, GroupName="Parameters")]
		public int MaxPositiveDeltaUp { get; set; }
	
		[NinjaScriptProperty]
		[Range(int.MinValue, 0)]
		[Display(Name="Min Negative Delta Up", Order=3, GroupName="Parameters")]
		public int MinNegativeDeltaUp { get; set; }
	
		[NinjaScriptProperty]
		[Range(int.MinValue, 0)]
		[Display(Name="Max Negative Delta Up", Order=4, GroupName="Parameters")]
		public int MaxNegativeDeltaUp { get; set; }
	
		[NinjaScriptProperty]
		[Range(int.MinValue, 0)]
		[Display(Name="Min Negative Delta Down", Order=5, GroupName="Parameters")]
		public int MinNegativeDeltaDown { get; set; }
	
		[NinjaScriptProperty]
		[Range(int.MinValue, 0)]
		[Display(Name="Max Negative Delta Down", Order=6, GroupName="Parameters")]
		public int MaxNegativeDeltaDown { get; set; }
	
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Min Positive Delta Down", Order=7, GroupName="Parameters")]
		public int MinPositiveDeltaDown { get; set; }
	
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Max Positive Delta Down", Order=8, GroupName="Parameters")]
		public int MaxPositiveDeltaDown { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name="Show Up Arrows", Order=9, GroupName="Filters")]
		public bool ShowUpArrows { get; set; }
	
		[NinjaScriptProperty]
		[Display(Name="Show Down Arrows", Order=10, GroupName="Filters")]
		public bool ShowDownArrows { get; set; }
	
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Volumetric Bars Indicator";
				Name = "VolumetricMinMaxDelta";
				Calculate = Calculate.OnBarClose;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
				
				// Set default values
				MinPositiveDeltaUp = 200;
				MaxPositiveDeltaUp = 3000;
				MinNegativeDeltaUp = -20;
				MaxNegativeDeltaUp = 20;
				MinNegativeDeltaDown = -3000;
				MaxNegativeDeltaDown = -200;
				MinPositiveDeltaDown = -20;
				MaxPositiveDeltaDown = 20;
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
			if (barsType == null)
				return;
	
			long maxPositiveDelta = barsType.Volumes[CurrentBar].GetMaximumPositiveDelta();
			long maxNegativeDelta = barsType.Volumes[CurrentBar].GetMaximumNegativeDelta();
	
			if (ShowUpArrows &&
				maxPositiveDelta >= MinPositiveDeltaUp && maxPositiveDelta <= MaxPositiveDeltaUp &&
				maxNegativeDelta >= MinNegativeDeltaUp && maxNegativeDelta <= MaxNegativeDeltaUp)
			{
				Draw.ArrowUp(this, "UpArrow" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
			}
	
			if (ShowDownArrows &&
				maxNegativeDelta >= MinNegativeDeltaDown && maxNegativeDelta <= MaxNegativeDeltaDown &&
				maxPositiveDelta >= MinPositiveDeltaDown && maxPositiveDelta <= MaxPositiveDeltaDown)
			{
				Draw.ArrowDown(this, "DownArrow" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
			}
		}
	}
}