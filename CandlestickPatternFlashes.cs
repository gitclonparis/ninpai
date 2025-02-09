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

	public class CandlestickPatternFlashes : Indicator
	{
		[NinjaScriptProperty]
        [Display(Name = "Bullish Engulfing", Order = 2, GroupName = "Motifs Haussiers")]
        public bool BullishEngulfing { get; set; }
		
		[NinjaScriptProperty]
        [Display(Name = "Bearish Engulfing", Order = 2, GroupName = "Motifs Baissiers")]
        public bool BearishEngulfing { get; set; }
		
		// Paramètre pour la force de la tendance
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Force de Tendance", Order = 0, GroupName = "Paramètres")]
        public int TrendStrength { get; set; }
	
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = "Custom Pattern Indicator";
				Name = "CandlestickPatternFlashes";
				Calculate = Calculate.OnBarClose;
				IsOverlay = true;
				DisplayInDataBox = true;
				DrawOnPricePanel = true;
				DrawHorizontalGridLines = true;
				DrawVerticalGridLines = true;
				PaintPriceMarkers = true;
				ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
				
				TrendStrength = 0;
				BullishEngulfing = false;
				BearishEngulfing = false;
			}
			else if (State == State.DataLoaded)
			{
			}
		}
	
		protected override void OnBarUpdate()
		{
			
			if (CurrentBar < 20) // S'assurer qu'il y a suffisamment de barres pour la détection
                return;
			
			if (BullishEngulfing && CandlestickPattern(ChartPattern.BullishEngulfing, TrendStrength)[0] == 1)
            {
                Draw.ArrowUp(this, "BullishEngulfing" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
            }
			
			if (BearishEngulfing && CandlestickPattern(ChartPattern.BearishEngulfing, TrendStrength)[0] == 1)
            {
                Draw.ArrowDown(this, "BearishEngulfing" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
            }
			
		}
	}
}