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
    public class FairValueGapV3 : Indicator
    {
        private Brush bullishColor = Brushes.LightGreen;
        private Brush bearishColor = Brushes.LightCoral;
        
        private class CandleData
        {
            public double High1, Low1, Close1;
            public double High2, Low2, Close2, Open2;
            public double High3, Low3, Open3;
        }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Extension des rectangles (barres)", Description="Nombre de barres sur lesquelles étendre les rectangles", Order=1, GroupName="Paramètres")]
        public int RectangleExtension { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Afficher flèche FVG haussier", Description="Active l'affichage des flèches pour les FVG haussiers", Order=2, GroupName="Paramètres")]
        public bool UseFVGup { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Afficher flèche FVG baissier", Description="Active l'affichage des flèches pour les FVG baissiers", Order=3, GroupName="Paramètres")]
        public bool UseFVGdown { get; set; }
        
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicateur qui détecte des motifs de chandeliers spécifiques et place des flèches sur le graphique.";
                Name = "FairValueGapV3";
                IsOverlay = true;
                IsSuspendedWhileInactive = true;
                RectangleExtension = 5;
                UseFVGup = true;
                UseFVGdown = true;
                AddPlot(bullishColor, "Bullish FVG");
                AddPlot(bearishColor, "Bearish FVG");
            }
        }

        private CandleData GetCandleData()
        {
            return new CandleData
            {
                High1 = High[2],  // Bougie 1
                Low1 = Low[2],
                Close1 = Close[2],
                
                High2 = High[1],  // Bougie 2
                Low2 = Low[1],
                Close2 = Close[1],
                Open2 = Open[1],
                
                High3 = High[0],  // Bougie 3
                Low3 = Low[0],
                Open3 = Open[0]
            };
        }

        private void DrawBullishFVG(CandleData data)
        {
            Draw.Rectangle(this, 
                "BullishFVG" + CurrentBar.ToString(), 
                false, 
                2, data.High1,
                -RectangleExtension, data.Low3,
                bullishColor, 
                bullishColor, 
                30);
            
            if (UseFVGup)
            {
                Draw.ArrowUp(this, 
                    "BullishArrow" + CurrentBar.ToString(),
                    false,
                    0,
                    Low[0] - TickSize * 5,
                    bullishColor);
            }
        }

        private void DrawBearishFVG(CandleData data)
        {
            Draw.Rectangle(this, 
                "BearishFVG" + CurrentBar.ToString(), 
                false, 
                2, data.Low1,
                -RectangleExtension, data.High3,
                bearishColor, 
                bearishColor, 
                30);
            
            if (UseFVGdown)
            {
                Draw.ArrowDown(this, 
                    "BearishArrow" + CurrentBar.ToString(),
                    false,
                    0,
                    High[0] + TickSize * 5,
                    bearishColor);
            }
        }

        private bool IsBullishFVG(CandleData data)
        {
            return data.Close2 > data.Open2 && data.Low3 > data.High1;
        }

        private bool IsBearishFVG(CandleData data)
        {
            return data.Close2 < data.Open2 && data.High3 < data.Low1;
        }
        
        protected override void OnBarUpdate()
        {
            if (CurrentBar < 2) return;

            var candleData = GetCandleData();

            if (IsBullishFVG(candleData))
            {
                DrawBullishFVG(candleData);
            }

            if (IsBearishFVG(candleData))
            {
                DrawBearishFVG(candleData);
            }
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.FairValueGapV3[] cacheFairValueGapV3;
		public ninpai.FairValueGapV3 FairValueGapV3(int rectangleExtension, bool useFVGup, bool useFVGdown)
		{
			return FairValueGapV3(Input, rectangleExtension, useFVGup, useFVGdown);
		}

		public ninpai.FairValueGapV3 FairValueGapV3(ISeries<double> input, int rectangleExtension, bool useFVGup, bool useFVGdown)
		{
			if (cacheFairValueGapV3 != null)
				for (int idx = 0; idx < cacheFairValueGapV3.Length; idx++)
					if (cacheFairValueGapV3[idx] != null && cacheFairValueGapV3[idx].RectangleExtension == rectangleExtension && cacheFairValueGapV3[idx].UseFVGup == useFVGup && cacheFairValueGapV3[idx].UseFVGdown == useFVGdown && cacheFairValueGapV3[idx].EqualsInput(input))
						return cacheFairValueGapV3[idx];
			return CacheIndicator<ninpai.FairValueGapV3>(new ninpai.FairValueGapV3(){ RectangleExtension = rectangleExtension, UseFVGup = useFVGup, UseFVGdown = useFVGdown }, input, ref cacheFairValueGapV3);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.FairValueGapV3 FairValueGapV3(int rectangleExtension, bool useFVGup, bool useFVGdown)
		{
			return indicator.FairValueGapV3(Input, rectangleExtension, useFVGup, useFVGdown);
		}

		public Indicators.ninpai.FairValueGapV3 FairValueGapV3(ISeries<double> input , int rectangleExtension, bool useFVGup, bool useFVGdown)
		{
			return indicator.FairValueGapV3(input, rectangleExtension, useFVGup, useFVGdown);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.FairValueGapV3 FairValueGapV3(int rectangleExtension, bool useFVGup, bool useFVGdown)
		{
			return indicator.FairValueGapV3(Input, rectangleExtension, useFVGup, useFVGdown);
		}

		public Indicators.ninpai.FairValueGapV3 FairValueGapV3(ISeries<double> input , int rectangleExtension, bool useFVGup, bool useFVGdown)
		{
			return indicator.FairValueGapV3(input, rectangleExtension, useFVGup, useFVGdown);
		}
	}
}

#endregion
