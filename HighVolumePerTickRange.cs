#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
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
                    Draw.Ellipse(this, "HighVol" + CurrentBar, false, 0, High[0], 0, High[0] + TickSize, Brushes.Red);
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

#region NinjaScript generated code. Neither change nor remove.
namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class Indicator
    {
        private HighVolumePerTickRange[] cacheHighVolumePerTickRange;
        public HighVolumePerTickRange HighVolumePerTickRange(double volumePerTickThreshold)
        {
            return HighVolumePerTickRange(Input, volumePerTickThreshold);
        }

        public HighVolumePerTickRange HighVolumePerTickRange(ISeries<double> input, double volumePerTickThreshold)
        {
            if (cacheHighVolumePerTickRange != null)
                for (int idx = 0; idx < cacheHighVolumePerTickRange.Length; idx++)
                    if (cacheHighVolumePerTickRange[idx] != null && cacheHighVolumePerTickRange[idx].VolumePerTickThreshold == volumePerTickThreshold && cacheHighVolumePerTickRange[idx].EqualsInput(input))
                        return cacheHighVolumePerTickRange[idx];
            return CacheIndicator<HighVolumePerTickRange>(new HighVolumePerTickRange(){ VolumePerTickThreshold = volumePerTickThreshold }, input, ref cacheHighVolumePerTickRange);
        }
    }
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
    public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
    {
        public Indicators.HighVolumePerTickRange HighVolumePerTickRange(double volumePerTickThreshold)
        {
            return indicator.HighVolumePerTickRange(Input, volumePerTickThreshold);
        }

        public Indicators.HighVolumePerTickRange HighVolumePerTickRange(ISeries<double> input, double volumePerTickThreshold)
        {
            return indicator.HighVolumePerTickRange(input, volumePerTickThreshold);
        }
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
    {
        public Indicators.HighVolumePerTickRange HighVolumePerTickRange(double volumePerTickThreshold)
        {
            return indicator.HighVolumePerTickRange(Input, volumePerTickThreshold);
        }

        public Indicators.HighVolumePerTickRange HighVolumePerTickRange(ISeries<double> input, double volumePerTickThreshold)
        {
            return indicator.HighVolumePerTickRange(input, volumePerTickThreshold);
        }
    }
}
#endregion
