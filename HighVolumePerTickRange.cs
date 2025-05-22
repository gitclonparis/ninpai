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

namespace NinjaTrader.NinjaScript.Indicators
{
    public class HighVolumePerTickRange : Indicator
    {
        private Series<bool> highVolumeBars;
        private double volumePerTickThreshold = 1000;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Marks bars with unusually high volume per tick range.";
                Name = "HighVolumePerTickRange";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                IsSuspendedWhileInactive = true;
                VolumePerTickThreshold = 1000;
            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.DataLoaded)
            {
                highVolumeBars = new Series<bool>(this, MaximumBarsLookBack.Infinite);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1)
                return;

            double barSize = Close[0] >= Open[0] ? Close[0] - Open[0] : Open[0] - Close[0];
            bool isHigh = false;
            if (barSize > 0)
            {
                double volumeRatio = Volume[0] / barSize;
                if (volumeRatio >= VolumePerTickThreshold)
                {
                    isHigh = true;
                    // Draw.Ellipse(this, "HighVol" + CurrentBar, false, 0, High[0], 0, High[0] + TickSize, Brushes.Red);
					// Draw.Ellipse(this, "HighVol" + CurrentBar, 0, High[0] + TickSize, 0, High[0], Brushes.Red, 50);
					Draw.Dot(this, "HighVol" + CurrentBar, false, 0, High[0] + TickSize, Brushes.Red);
                }
            }
            highVolumeBars[0] = isHigh;
        }

        public bool IsHighVolumeBar(int barIndex)
        {
            if (barIndex < 0 || barIndex >= highVolumeBars.Count)
                return false;
            return highVolumeBars[barIndex];
        }

        #region Properties
        [NinjaScriptProperty]
        [Range(0.0, double.MaxValue)]
        [Display(Name = "Volume Per Tick Threshold", Order = 1, GroupName = "Parameters")]
        public double VolumePerTickThreshold
        {
            get { return volumePerTickThreshold; }
            set { volumePerTickThreshold = value; }
        }
        #endregion
    }
}
