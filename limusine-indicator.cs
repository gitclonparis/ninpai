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

// This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators.ninpai
{
    public class LimusineIndicator : Indicator
    {
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Indicateur de Limusine";
                Name = "LimusineIndicator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                //Disable this property if your indicator requires custom values that cumulate with each new market data event. 
                //See Help Guide for additional information.
                IsSuspendedWhileInactive = true;
                MinimumTicks = 20; // Paramètre configurable pour la taille minimale des limusines
            }
        }

        // [NinjaScriptProperty]
        // [Range(1, int.MaxValue)]
        // [Display(Name = "Minimum Ticks", Description = "Nombre minimum de ticks pour une limusine", Order = 1, GroupName = "Parameters")]
        // public int MinimumTicks { get; set; }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1) return;
            // Calculer les différences en ticks
            double openCloseDiff = Math.Abs(Open[0] - Close[0]) / TickSize;
            double highLowDiff = Math.Abs(High[0] - Low[0]) / TickSize;
            // Vérifier les conditions pour chaque type de limusine
            bool isLimusineOpenCloseUP = openCloseDiff >= MinimumTicks && Close[0] > Open[0];
            bool isLimusineOpenCloseDOWN = openCloseDiff >= MinimumTicks && Close[0] < Open[0];
            bool isLimusineHighLowUP = highLowDiff >= MinimumTicks && Close[0] > Open[0];
            bool isLimusineHighLowDOWN = highLowDiff >= MinimumTicks && Close[0] < Open[0];
            // Dessiner les flèches appropriées
            if (isLimusineOpenCloseUP || isLimusineHighLowUP)
            {
                Draw.ArrowUp(this, "LimusineUP_" + CurrentBar, true, 0, Low[0] - 2 * TickSize, Brushes.Green);
            }
            else if (isLimusineOpenCloseDOWN || isLimusineHighLowDOWN)
            {
                Draw.ArrowDown(this, "LimusineDown_" + CurrentBar, true, 0, High[0] + 2 * TickSize, Brushes.Red);
            }
        }
		
		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Minimum Ticks", Description = "Nombre minimum de ticks pour une limusine", Order = 1, GroupName = "Parameters")]
		public int MinimumTicks { get; set; }
		#endregion
    }	
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.LimusineIndicator[] cacheLimusineIndicator;
		public ninpai.LimusineIndicator LimusineIndicator(int minimumTicks)
		{
			return LimusineIndicator(Input, minimumTicks);
		}

		public ninpai.LimusineIndicator LimusineIndicator(ISeries<double> input, int minimumTicks)
		{
			if (cacheLimusineIndicator != null)
				for (int idx = 0; idx < cacheLimusineIndicator.Length; idx++)
					if (cacheLimusineIndicator[idx] != null && cacheLimusineIndicator[idx].MinimumTicks == minimumTicks && cacheLimusineIndicator[idx].EqualsInput(input))
						return cacheLimusineIndicator[idx];
			return CacheIndicator<ninpai.LimusineIndicator>(new ninpai.LimusineIndicator(){ MinimumTicks = minimumTicks }, input, ref cacheLimusineIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.LimusineIndicator LimusineIndicator(int minimumTicks)
		{
			return indicator.LimusineIndicator(Input, minimumTicks);
		}

		public Indicators.ninpai.LimusineIndicator LimusineIndicator(ISeries<double> input , int minimumTicks)
		{
			return indicator.LimusineIndicator(input, minimumTicks);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.LimusineIndicator LimusineIndicator(int minimumTicks)
		{
			return indicator.LimusineIndicator(Input, minimumTicks);
		}

		public Indicators.ninpai.LimusineIndicator LimusineIndicator(ISeries<double> input , int minimumTicks)
		{
			return indicator.LimusineIndicator(input, minimumTicks);
		}
	}
}

#endregion
