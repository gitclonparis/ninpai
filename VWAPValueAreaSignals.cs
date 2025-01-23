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
    public class VWAPValueAreaSignals : Indicator
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

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "VWAP Value Area Signals";
                Name = "VWAPValueAreaSignals";
                UpperOffsetTicks = 5;
                LowerOffsetTicks = 5;
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

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1) return;
            
            // Check for new session
            if (Bars.IsFirstBarOfSession)
            {
                newSession = true;
                // Store prior session bands
                priorSessionUpperBand = vwap.StdDev1Upper[1];
                priorSessionLowerBand = vwap.StdDev1Lower[1];
            }

            if (!newSession) return;

            double upperThreshold = priorSessionUpperBand + (TickSize * UpperOffsetTicks);
            double lowerThreshold = priorSessionLowerBand - (TickSize * LowerOffsetTicks);
            
            // Signal generation logic
            if (Close[0] >= priorSessionLowerBand && Close[0] <= priorSessionUpperBand)
            {
                // Price within Value Area - allow both signals
                if (Close[0] > Open[0] && Close[0] > Close[1])
                    Draw.ArrowUp(this, "Up" + CurrentBar, true, 0, Low[0] - (2 * TickSize), Brushes.Green);
                else if (Close[0] < Open[0] && Close[0] < Close[1])
                    Draw.ArrowDown(this, "Down" + CurrentBar, true, 0, High[0] + (2 * TickSize), Brushes.Red);
            }
            else if (Close[0] > upperThreshold)
            {
                // Price above upper threshold - only up signals
                if (Close[0] > Open[0] && Close[0] > Close[1])
                    Draw.ArrowUp(this, "Up" + CurrentBar, true, 0, Low[0] - (2 * TickSize), Brushes.Green);
            }
            else if (Close[0] < lowerThreshold)
            {
                // Price below lower threshold - only down signals
                if (Close[0] < Open[0] && Close[0] < Close[1])
                    Draw.ArrowDown(this, "Down" + CurrentBar, true, 0, High[0] + (2 * TickSize), Brushes.Red);
            }
        }
    }
}
