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
    public class PriorSessionBreakout : Indicator
    {
        private PriorDayOHLC PriorDayOHLC1;
        private double sessionOpenPrice;
        private bool isFirstBarOfSession = true;
        private double priorHigh;
        private double priorLow;
        
        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name="Breakout Offset", Description="Distance from prior high/low for breakout signal", Order=1, GroupName="Parameters")]
        public double Offset { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Prior Session Breakout Detection Indicator";
                Name = "PriorSessionBreakout";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;
                
                Offset = 1;
            }
            else if (State == State.DataLoaded)
            {
                PriorDayOHLC1 = PriorDayOHLC(Close);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBars[0] < 1) return;

            // Vérifier si c'est un nouveau jour
            if (Time[0].Date != Time[1].Date)
            {
                sessionOpenPrice = Open[0];
                isFirstBarOfSession = true;
            }

            // Mettre à jour les niveaux du jour précédent
            UpdatePriorLevels();

            // Vérifier les conditions UP et DOWN
            CheckUpCondition();
            CheckDownCondition();

            isFirstBarOfSession = false;
        }

        private void UpdatePriorLevels()
        {
            priorHigh = PriorDayOHLC1.PriorHigh[0];
            priorLow = PriorDayOHLC1.PriorLow[0];
        }

        private bool IsOpenInRange()
        {
            // Vérifie si les deux conditions d'ouverture sont remplies
            bool isSessionOpenInRange = sessionOpenPrice >= priorLow && sessionOpenPrice <= priorHigh;
            bool isCurrentBarOpenInRange = Open[0] >= priorLow && Open[0] <= priorHigh;
            
            return isSessionOpenInRange && isCurrentBarOpenInRange;
        }

        private void CheckUpCondition()
        {
            if (IsOpenInRange() && Close[0] > priorHigh + Offset)
            {
                DrawUpSignal();
            }
        }

        private void CheckDownCondition()
        {
            if (IsOpenInRange() && Close[0] < priorLow - Offset)
            {
                DrawDownSignal();
            }
        }

        private void DrawUpSignal()
        {
            string signalId = CurrentBar.ToString();
            Draw.ArrowUp(this, "UpBreakout" + signalId, true, 0, Low[0] - TickSize, Brushes.Lime);
            Draw.Text(this, "UpBreakoutText" + signalId, "UP", 0, Low[0] - 2 * TickSize, Brushes.Lime);
        }

        private void DrawDownSignal()
        {
            string signalId = CurrentBar.ToString();
            Draw.ArrowDown(this, "DownBreakout" + signalId, true, 0, High[0] + TickSize, Brushes.Red);
            Draw.Text(this, "DownBreakoutText" + signalId, "DOWN", 0, High[0] + 2 * TickSize, Brushes.Red);
        }

        #region Properties
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> PriorHigh
        {
            get { return Values[0]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> PriorLow
        {
            get { return Values[1]; }
        }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.PriorSessionBreakout[] cachePriorSessionBreakout;
		public ninpai.PriorSessionBreakout PriorSessionBreakout(double offset)
		{
			return PriorSessionBreakout(Input, offset);
		}

		public ninpai.PriorSessionBreakout PriorSessionBreakout(ISeries<double> input, double offset)
		{
			if (cachePriorSessionBreakout != null)
				for (int idx = 0; idx < cachePriorSessionBreakout.Length; idx++)
					if (cachePriorSessionBreakout[idx] != null && cachePriorSessionBreakout[idx].Offset == offset && cachePriorSessionBreakout[idx].EqualsInput(input))
						return cachePriorSessionBreakout[idx];
			return CacheIndicator<ninpai.PriorSessionBreakout>(new ninpai.PriorSessionBreakout(){ Offset = offset }, input, ref cachePriorSessionBreakout);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.PriorSessionBreakout PriorSessionBreakout(double offset)
		{
			return indicator.PriorSessionBreakout(Input, offset);
		}

		public Indicators.ninpai.PriorSessionBreakout PriorSessionBreakout(ISeries<double> input , double offset)
		{
			return indicator.PriorSessionBreakout(input, offset);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.PriorSessionBreakout PriorSessionBreakout(double offset)
		{
			return indicator.PriorSessionBreakout(Input, offset);
		}

		public Indicators.ninpai.PriorSessionBreakout PriorSessionBreakout(ISeries<double> input , double offset)
		{
			return indicator.PriorSessionBreakout(input, offset);
		}
	}
}

#endregion
