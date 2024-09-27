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
    public class volumetrifilterindicatorV9 : Indicator
    {
        private Brush upArrowColor;
        private Brush downArrowColor;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicateur personnalisé qui affiche des flèches en fonction du BarDelta, DeltaPercent, MaximumPositiveDelta, MaximumNegativeDelta, DeltaChange, TotalBuyingVolume, TotalSellingVolume, Trades et Total Volume dans des plages définies.";
                Name = "volumetrifilterindicatorV9";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                PaintPriceMarkers = false;
                IsSuspendedWhileInactive = true;
                
                // Existing properties...
				UpBarDeltaEnabled = false;
                MinBarDeltaUPThreshold = 500;
                MaxBarDeltaUPThreshold = 2000;
                DeltaPercentUPFilterEnabled = false;
                MinDeltaPercentUP = 10;
                MaxDeltaPercentUP = 50;
                MaxPositiveDeltaUPEnabled = false;
                MinMaxPositiveDeltaUP = 1000;
                MaxMaxPositiveDeltaUP = 5000;
                MaxNegativeDeltaUPEnabled = false;
                MinMaxNegativeDeltaUP = -5000;
                MaxMaxNegativeDeltaUP = -1000;
                
                DownBarDeltaEnabled = false;
                MinBarDeltaDownThreshold = -2000;
                MaxBarDeltaDownThreshold = -500;
                DeltaPercentDownFilterEnabled = false;
                MinDeltaPercentDown = -50;
                MaxDeltaPercentDown = -10;
                MaxPositiveDeltaDownEnabled = false;
                MinMaxPositiveDeltaDown = 0;
                MaxMaxPositiveDeltaDown = 1000;
                MaxNegativeDeltaDownEnabled = false;
                MinMaxNegativeDeltaDown = -5000;
                MaxMaxNegativeDeltaDown = -1000;
                
                DeltaChangeUPEnabled = false;
                MinDeltaChangeUP = 100;
                MaxDeltaChangeUP = 1000;
                
                DeltaChangeDownEnabled = false;
                MinDeltaChangeDown = -1000;
                MaxDeltaChangeDown = -100;
                
                TotalBuyingVolumeUPEnabled = false;
                MinTotalBuyingVolumeUP = 1000;
                MaxTotalBuyingVolumeUP = 10000;
                
                TotalBuyingVolumeDownEnabled = false;
                MinTotalBuyingVolumeDown = 0;
                MaxTotalBuyingVolumeDown = 5000;
                
                TotalSellingVolumeUPEnabled = false;
                MinTotalSellingVolumeUP = 0;
                MaxTotalSellingVolumeUP = 5000;
                
                TotalSellingVolumeDownEnabled = false;
                MinTotalSellingVolumeDown = 1000;
                MaxTotalSellingVolumeDown = 10000;
                
                TradesUPEnabled = false;
                MinTradesUP = 100;
                MaxTradesUP = 1000;
                
                TradesDownEnabled = false;
                MinTradesDown = 100;
                MaxTradesDown = 1000;
                
                TotalVolumeUPEnabled = false;
                MinTotalVolumeUP = 2000;
                MaxTotalVolumeUP = 20000;
                
                TotalVolumeDownEnabled = false;
                MinTotalVolumeDown = 2000;
                MaxTotalVolumeDown = 20000;
                
                UpArrowColor = Brushes.Green;
                DownArrowColor = Brushes.Red;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 2)  // We need at least 2 bars to calculate delta change
                return;

            if (!(Bars.BarsSeries.BarsType is NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType barsType))
                return;

            var VmetricbarDelta0 = barsType.Volumes[CurrentBar].BarDelta;
            var VmetricdeltaPercent0 = barsType.Volumes[CurrentBar].GetDeltaPercent();
            var VmetricMaxPositiveDelta0 = barsType.Volumes[CurrentBar].GetMaximumPositiveDelta();
            var VmetricMaxNegativeDelta0 = barsType.Volumes[CurrentBar].GetMaximumNegativeDelta();
            
            // Calculate delta change
            var VmetricbarDelta1 = barsType.Volumes[CurrentBar - 1].BarDelta;
            var VmetricDeltaChange = VmetricbarDelta0 - VmetricbarDelta1;

            // Existing conditions...
            var VmetricTotalBuyingVolume = barsType.Volumes[CurrentBar].TotalBuyingVolume;
            var VmetricTotalSellingVolume = barsType.Volumes[CurrentBar].TotalSellingVolume;
            var VmetricTrades = barsType.Volumes[CurrentBar].Trades;

            // New condition
            var VmetricTotalVolume = barsType.Volumes[CurrentBar].TotalVolume;

            // Existing conditions...
			bool upBarDeltaCondition = !UpBarDeltaEnabled || (VmetricbarDelta0 >= MinBarDeltaUPThreshold && VmetricbarDelta0 <= MaxBarDeltaUPThreshold);
            bool upDeltaPercentCondition = !DeltaPercentUPFilterEnabled || (VmetricdeltaPercent0 >= MinDeltaPercentUP && VmetricdeltaPercent0 <= MaxDeltaPercentUP);
            bool upMaxPositiveDeltaCondition = !MaxPositiveDeltaUPEnabled || (VmetricMaxPositiveDelta0 >= MinMaxPositiveDeltaUP && VmetricMaxPositiveDelta0 <= MaxMaxPositiveDeltaUP);
            bool upMaxNegativeDeltaCondition = !MaxNegativeDeltaUPEnabled || (VmetricMaxNegativeDelta0 >= MinMaxNegativeDeltaUP && VmetricMaxNegativeDelta0 <= MaxMaxNegativeDeltaUP);

            bool downBarDeltaCondition = !DownBarDeltaEnabled || (VmetricbarDelta0 <= MinBarDeltaDownThreshold && VmetricbarDelta0 >= MaxBarDeltaDownThreshold);
            bool downDeltaPercentCondition = !DeltaPercentDownFilterEnabled || (VmetricdeltaPercent0 <= MinDeltaPercentDown && VmetricdeltaPercent0 >= MaxDeltaPercentDown);
            bool downMaxPositiveDeltaCondition = !MaxPositiveDeltaDownEnabled || (VmetricMaxPositiveDelta0 >= MinMaxPositiveDeltaDown && VmetricMaxPositiveDelta0 <= MaxMaxPositiveDeltaDown);
            bool downMaxNegativeDeltaCondition = !MaxNegativeDeltaDownEnabled || (VmetricMaxNegativeDelta0 >= MinMaxNegativeDeltaDown && VmetricMaxNegativeDelta0 <= MaxMaxNegativeDeltaDown);

            bool upDeltaChangeCondition = !DeltaChangeUPEnabled || (VmetricDeltaChange >= MinDeltaChangeUP && VmetricDeltaChange <= MaxDeltaChangeUP);
            bool downDeltaChangeCondition = !DeltaChangeDownEnabled || (VmetricDeltaChange <= MinDeltaChangeDown && VmetricDeltaChange >= MaxDeltaChangeDown);

            bool upTotalBuyingVolumeCondition = !TotalBuyingVolumeUPEnabled || (VmetricTotalBuyingVolume >= MinTotalBuyingVolumeUP && VmetricTotalBuyingVolume <= MaxTotalBuyingVolumeUP);
            bool downTotalBuyingVolumeCondition = !TotalBuyingVolumeDownEnabled || (VmetricTotalBuyingVolume >= MinTotalBuyingVolumeDown && VmetricTotalBuyingVolume <= MaxTotalBuyingVolumeDown);

            bool upTotalSellingVolumeCondition = !TotalSellingVolumeUPEnabled || (VmetricTotalSellingVolume >= MinTotalSellingVolumeUP && VmetricTotalSellingVolume <= MaxTotalSellingVolumeUP);
            bool downTotalSellingVolumeCondition = !TotalSellingVolumeDownEnabled || (VmetricTotalSellingVolume >= MinTotalSellingVolumeDown && VmetricTotalSellingVolume <= MaxTotalSellingVolumeDown);

            bool upTradesCondition = !TradesUPEnabled || (VmetricTrades >= MinTradesUP && VmetricTrades <= MaxTradesUP);
            bool downTradesCondition = !TradesDownEnabled || (VmetricTrades >= MinTradesDown && VmetricTrades <= MaxTradesDown);

            bool upTotalVolumeCondition = !TotalVolumeUPEnabled || (VmetricTotalVolume >= MinTotalVolumeUP && VmetricTotalVolume <= MaxTotalVolumeUP);
            bool downTotalVolumeCondition = !TotalVolumeDownEnabled || (VmetricTotalVolume >= MinTotalVolumeDown && VmetricTotalVolume <= MaxTotalVolumeDown);

            bool showUpArrow = (UpBarDeltaEnabled || DeltaPercentUPFilterEnabled || MaxPositiveDeltaUPEnabled || MaxNegativeDeltaUPEnabled || DeltaChangeUPEnabled || 
                                TotalBuyingVolumeUPEnabled || TotalSellingVolumeUPEnabled || TradesUPEnabled || TotalVolumeUPEnabled) 
                && upBarDeltaCondition && upDeltaPercentCondition && upMaxPositiveDeltaCondition && upMaxNegativeDeltaCondition && upDeltaChangeCondition &&
                upTotalBuyingVolumeCondition && upTotalSellingVolumeCondition && upTradesCondition && upTotalVolumeCondition;

            bool showDownArrow = (DownBarDeltaEnabled || DeltaPercentDownFilterEnabled || MaxPositiveDeltaDownEnabled || MaxNegativeDeltaDownEnabled || DeltaChangeDownEnabled || 
                                  TotalBuyingVolumeDownEnabled || TotalSellingVolumeDownEnabled || TradesDownEnabled || TotalVolumeDownEnabled) 
                && downBarDeltaCondition && downDeltaPercentCondition && downMaxPositiveDeltaCondition && downMaxNegativeDeltaCondition && downDeltaChangeCondition &&
                downTotalBuyingVolumeCondition && downTotalSellingVolumeCondition && downTradesCondition && downTotalVolumeCondition;

            if (showUpArrow)
            {
                Draw.ArrowUp(this, "UpArrow" + CurrentBar, false, 0, Low[0] - TickSize, UpArrowColor);
            }

            if (showDownArrow)
            {
                Draw.ArrowDown(this, "DownArrow" + CurrentBar, false, 0, High[0] + TickSize, DownArrowColor);
            }
        }

        #region Properties
        // Existing properties...
		[NinjaScriptProperty]
        [Display(Name = "UP Bar Delta Enabled", Order = 1, GroupName = "01_VmetricbarDelta0UP")]
        public bool UpBarDeltaEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Bar Delta UP Threshold", Order = 2, GroupName = "01_VmetricbarDelta0UP")]
        public int MinBarDeltaUPThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Bar Delta UP Threshold", Order = 3, GroupName = "01_VmetricbarDelta0UP")]
        public int MaxBarDeltaUPThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "DOWN Bar Delta Enabled", Order = 1, GroupName = "02_VmetricbarDelta0Down")]
        public bool DownBarDeltaEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Bar Delta Down Threshold", Order = 2, GroupName = "02_VmetricbarDelta0Down")]
        public int MinBarDeltaDownThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Bar Delta Down Threshold", Order = 3, GroupName = "02_VmetricbarDelta0Down")]
        public int MaxBarDeltaDownThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Delta Percent UP Filter Enabled", Order = 1, GroupName = "03_VmetricdeltaPercentUP")]
        public bool DeltaPercentUPFilterEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Delta Percent UP", Order = 2, GroupName = "03_VmetricdeltaPercentUP")]
        public double MinDeltaPercentUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Delta Percent UP", Order = 3, GroupName = "03_VmetricdeltaPercentUP")]
        public double MaxDeltaPercentUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Delta Percent DOWN Filter Enabled", Order = 1, GroupName = "04_VmetricdeltaPercentDown")]
        public bool DeltaPercentDownFilterEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Delta Percent DOWN", Order = 2, GroupName = "04_VmetricdeltaPercentDown")]
        public double MinDeltaPercentDown { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Delta Percent DOWN", Order = 3, GroupName = "04_VmetricdeltaPercentDown")]
        public double MaxDeltaPercentDown { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Positive Delta UP Enabled", Order = 1, GroupName = "05_VmetricMaxPositiveDeltaUP")]
        public bool MaxPositiveDeltaUPEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Max Positive Delta UP", Order = 2, GroupName = "05_VmetricMaxPositiveDeltaUP")]
        public int MinMaxPositiveDeltaUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Max Positive Delta UP", Order = 3, GroupName = "05_VmetricMaxPositiveDeltaUP")]
        public int MaxMaxPositiveDeltaUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Positive Delta DOWN Enabled", Order = 1, GroupName = "06_VmetricMaxPositiveDeltaDown")]
        public bool MaxPositiveDeltaDownEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Max Positive Delta DOWN", Order = 2, GroupName = "06_VmetricMaxPositiveDeltaDown")]
        public int MinMaxPositiveDeltaDown { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Max Positive Delta DOWN", Order = 3, GroupName = "06_VmetricMaxPositiveDeltaDown")]
        public int MaxMaxPositiveDeltaDown { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Negative Delta UP Enabled", Order = 1, GroupName = "07_VmetricMaxNegativeDeltaUP")]
        public bool MaxNegativeDeltaUPEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Max Negative Delta UP", Order = 2, GroupName = "07_VmetricMaxNegativeDeltaUP")]
        public int MinMaxNegativeDeltaUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Max Negative Delta UP", Order = 3, GroupName = "07_VmetricMaxNegativeDeltaUP")]
        public int MaxMaxNegativeDeltaUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Negative Delta DOWN Enabled", Order = 1, GroupName = "08_VmetricMaxNegativeDeltaDown")]
        public bool MaxNegativeDeltaDownEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Max Negative Delta DOWN", Order = 2, GroupName = "08_VmetricMaxNegativeDeltaDown")]
        public int MinMaxNegativeDeltaDown { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Max Negative Delta DOWN", Order = 3, GroupName = "08_VmetricMaxNegativeDeltaDown")]
        public int MaxMaxNegativeDeltaDown { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Delta Change UP Enabled", Order = 1, GroupName = "09_VmetricDeltaChangeUP")]
        public bool DeltaChangeUPEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Delta Change UP", Order = 2, GroupName = "09_VmetricDeltaChangeUP")]
        public int MinDeltaChangeUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Delta Change UP", Order = 3, GroupName = "09_VmetricDeltaChangeUP")]
        public int MaxDeltaChangeUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Delta Change DOWN Enabled", Order = 1, GroupName = "10_VmetricDeltaChangeDown")]
        public bool DeltaChangeDownEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Delta Change DOWN", Order = 2, GroupName = "10_VmetricDeltaChangeDown")]
        public int MinDeltaChangeDown { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Delta Change DOWN", Order = 3, GroupName = "10_VmetricDeltaChangeDown")]
        public int MaxDeltaChangeDown { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Total Buying Volume UP Enabled", Order = 1, GroupName = "11_VmetricTotalBuyingVolumeUP")]
        public bool TotalBuyingVolumeUPEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Total Buying Volume UP", Order = 2, GroupName = "11_VmetricTotalBuyingVolumeUP")]
        public int MinTotalBuyingVolumeUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Total Buying Volume UP", Order = 3, GroupName = "11_VmetricTotalBuyingVolumeUP")]
        public int MaxTotalBuyingVolumeUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Total Buying Volume DOWN Enabled", Order = 1, GroupName = "12_VmetricTotalBuyingVolumeDown")]
        public bool TotalBuyingVolumeDownEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Total Buying Volume DOWN", Order = 2, GroupName = "12_VmetricTotalBuyingVolumeDown")]
        public int MinTotalBuyingVolumeDown { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Total Buying Volume DOWN", Order = 3, GroupName = "12_VmetricTotalBuyingVolumeDown")]
        public int MaxTotalBuyingVolumeDown { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Total Selling Volume UP Enabled", Order = 1, GroupName = "13_VmetricTotalSellingVolumeUP")]
        public bool TotalSellingVolumeUPEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Total Selling Volume UP", Order = 2, GroupName = "13_VmetricTotalSellingVolumeUP")]
        public int MinTotalSellingVolumeUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Total Selling Volume UP", Order = 3, GroupName = "13_VmetricTotalSellingVolumeUP")]
        public int MaxTotalSellingVolumeUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Total Selling Volume DOWN Enabled", Order = 1, GroupName = "14_VmetricTotalSellingVolumeDown")]
        public bool TotalSellingVolumeDownEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Total Selling Volume DOWN", Order = 2, GroupName = "14_VmetricTotalSellingVolumeDown")]
        public int MinTotalSellingVolumeDown { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Total Selling Volume DOWN", Order = 3, GroupName = "14_VmetricTotalSellingVolumeDown")]
        public int MaxTotalSellingVolumeDown { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trades UP Enabled", Order = 1, GroupName = "15_VmetricTradesUP")]
        public bool TradesUPEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Trades UP", Order = 2, GroupName = "15_VmetricTradesUP")]
        public int MinTradesUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Trades UP", Order = 3, GroupName = "15_VmetricTradesUP")]
        public int MaxTradesUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Trades DOWN Enabled", Order = 1, GroupName = "16_VmetricTradesDown")]
        public bool TradesDownEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Trades DOWN", Order = 2, GroupName = "16_VmetricTradesDown")]
        public int MinTradesDown { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Trades DOWN", Order = 3, GroupName = "16_VmetricTradesDown")]
        public int MaxTradesDown { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Total Volume UP Enabled", Order = 1, GroupName = "17_VmetricTotalVolumeUP")]
        public bool TotalVolumeUPEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Total Volume UP", Order = 2, GroupName = "17_VmetricTotalVolumeUP")]
        public int MinTotalVolumeUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Total Volume UP", Order = 3, GroupName = "17_VmetricTotalVolumeUP")]
        public int MaxTotalVolumeUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Total Volume DOWN Enabled", Order = 1, GroupName = "18_VmetricTotalVolumeDown")]
        public bool TotalVolumeDownEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Total Volume DOWN", Order = 2, GroupName = "18_VmetricTotalVolumeDown")]
        public int MinTotalVolumeDown { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Total Volume DOWN", Order = 3, GroupName = "18_VmetricTotalVolumeDown")]
        public int MaxTotalVolumeDown { get; set; }

        // Existing color properties...
		[NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Up Arrow Color", Order = 1, GroupName = "Visuals")]
        public Brush UpArrowColor
        {
            get { return upArrowColor; }
            set { upArrowColor = value; }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Down Arrow Color", Order = 2, GroupName = "Visuals")]
        public Brush DownArrowColor
        {
            get { return downArrowColor; }
            set { downArrowColor = value; }
        }
        #endregion
    }
}
