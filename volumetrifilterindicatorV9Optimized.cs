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
    public class volumetrifilterindicatorV9Optimized : Indicator
    {
        private Brush upArrowColor;
        private Brush downArrowColor;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicateur optimisé qui affiche des flèches en fonction de divers paramètres volumétriques.";
                Name = "volumetrifilterindicatorV9Optimized";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                PaintPriceMarkers = false;
                IsSuspendedWhileInactive = true;

                // Paramètres par défaut
                SetDefaultParameters();

                UpArrowColor = Brushes.Green;
                DownArrowColor = Brushes.Red;
            }
        }

        private void SetDefaultParameters()
        {
            // Paramètres UP
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
            DeltaChangeUPEnabled = false;
            MinDeltaChangeUP = 100;
            MaxDeltaChangeUP = 1000;
            TotalBuyingVolumeUPEnabled = false;
            MinTotalBuyingVolumeUP = 1000;
            MaxTotalBuyingVolumeUP = 10000;
            TotalSellingVolumeUPEnabled = false;
            MinTotalSellingVolumeUP = 0;
            MaxTotalSellingVolumeUP = 5000;
            TradesUPEnabled = false;
            MinTradesUP = 100;
            MaxTradesUP = 1000;
            TotalVolumeUPEnabled = false;
            MinTotalVolumeUP = 2000;
            MaxTotalVolumeUP = 20000;

            // Paramètres DOWN
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
            DeltaChangeDownEnabled = false;
            MinDeltaChangeDown = -1000;
            MaxDeltaChangeDown = -100;
            TotalBuyingVolumeDownEnabled = false;
            MinTotalBuyingVolumeDown = 0;
            MaxTotalBuyingVolumeDown = 5000;
            TotalSellingVolumeDownEnabled = false;
            MinTotalSellingVolumeDown = 1000;
            MaxTotalSellingVolumeDown = 10000;
            TradesDownEnabled = false;
            MinTradesDown = 100;
            MaxTradesDown = 1000;
            TotalVolumeDownEnabled = false;
            MinTotalVolumeDown = 2000;
            MaxTotalVolumeDown = 20000;
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 2)
                return;

            // if (!(Bars.BarsSeries.BarsType is VolumetricBarsType barsType))
                // return;
			 if (!(Bars.BarsSeries.BarsType is NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType barsType))
                return;

            var currentBarVolumes = barsType.Volumes[CurrentBar];
            var previousBarVolumes = barsType.Volumes[CurrentBar - 1];

            // Variables volumétriques
            double VmetricbarDelta0 = currentBarVolumes.BarDelta;
            double VmetricdeltaPercent0 = currentBarVolumes.GetDeltaPercent();
            double VmetricMaxPositiveDelta0 = currentBarVolumes.GetMaximumPositiveDelta();
            double VmetricMaxNegativeDelta0 = currentBarVolumes.GetMaximumNegativeDelta();
            double VmetricDeltaChange = VmetricbarDelta0 - previousBarVolumes.BarDelta;
            double VmetricTotalBuyingVolume = currentBarVolumes.TotalBuyingVolume;
            double VmetricTotalSellingVolume = currentBarVolumes.TotalSellingVolume;
            double VmetricTrades = currentBarVolumes.Trades;
            double VmetricTotalVolume = currentBarVolumes.TotalVolume;

            // Conditions UP
            bool upBarDeltaCondition = CheckCondition(UpBarDeltaEnabled, VmetricbarDelta0, MinBarDeltaUPThreshold, MaxBarDeltaUPThreshold);
            bool upDeltaPercentCondition = CheckCondition(DeltaPercentUPFilterEnabled, VmetricdeltaPercent0, MinDeltaPercentUP, MaxDeltaPercentUP);
            bool upMaxPositiveDeltaCondition = CheckCondition(MaxPositiveDeltaUPEnabled, VmetricMaxPositiveDelta0, MinMaxPositiveDeltaUP, MaxMaxPositiveDeltaUP);
            bool upMaxNegativeDeltaCondition = CheckCondition(MaxNegativeDeltaUPEnabled, VmetricMaxNegativeDelta0, MinMaxNegativeDeltaUP, MaxMaxNegativeDeltaUP);
            bool upDeltaChangeCondition = CheckCondition(DeltaChangeUPEnabled, VmetricDeltaChange, MinDeltaChangeUP, MaxDeltaChangeUP);
            bool upTotalBuyingVolumeCondition = CheckCondition(TotalBuyingVolumeUPEnabled, VmetricTotalBuyingVolume, MinTotalBuyingVolumeUP, MaxTotalBuyingVolumeUP);
            bool upTotalSellingVolumeCondition = CheckCondition(TotalSellingVolumeUPEnabled, VmetricTotalSellingVolume, MinTotalSellingVolumeUP, MaxTotalSellingVolumeUP);
            bool upTradesCondition = CheckCondition(TradesUPEnabled, VmetricTrades, MinTradesUP, MaxTradesUP);
            bool upTotalVolumeCondition = CheckCondition(TotalVolumeUPEnabled, VmetricTotalVolume, MinTotalVolumeUP, MaxTotalVolumeUP);

            // Conditions DOWN
            bool downBarDeltaCondition = CheckCondition(DownBarDeltaEnabled, VmetricbarDelta0, MinBarDeltaDownThreshold, MaxBarDeltaDownThreshold);
            bool downDeltaPercentCondition = CheckCondition(DeltaPercentDownFilterEnabled, VmetricdeltaPercent0, MinDeltaPercentDown, MaxDeltaPercentDown);
            bool downMaxPositiveDeltaCondition = CheckCondition(MaxPositiveDeltaDownEnabled, VmetricMaxPositiveDelta0, MinMaxPositiveDeltaDown, MaxMaxPositiveDeltaDown);
            bool downMaxNegativeDeltaCondition = CheckCondition(MaxNegativeDeltaDownEnabled, VmetricMaxNegativeDelta0, MinMaxNegativeDeltaDown, MaxMaxNegativeDeltaDown);
            bool downDeltaChangeCondition = CheckCondition(DeltaChangeDownEnabled, VmetricDeltaChange, MinDeltaChangeDown, MaxDeltaChangeDown);
            bool downTotalBuyingVolumeCondition = CheckCondition(TotalBuyingVolumeDownEnabled, VmetricTotalBuyingVolume, MinTotalBuyingVolumeDown, MaxTotalBuyingVolumeDown);
            bool downTotalSellingVolumeCondition = CheckCondition(TotalSellingVolumeDownEnabled, VmetricTotalSellingVolume, MinTotalSellingVolumeDown, MaxTotalSellingVolumeDown);
            bool downTradesCondition = CheckCondition(TradesDownEnabled, VmetricTrades, MinTradesDown, MaxTradesDown);
            bool downTotalVolumeCondition = CheckCondition(TotalVolumeDownEnabled, VmetricTotalVolume, MinTotalVolumeDown, MaxTotalVolumeDown);

            // Vérifier si au moins une condition est activée pour UP et DOWN
            bool anyUpConditionEnabled = AnyConditionEnabledForUp();
            bool anyDownConditionEnabled = AnyConditionEnabledForDown();

            bool showUpArrow = anyUpConditionEnabled && upBarDeltaCondition && upDeltaPercentCondition && upMaxPositiveDeltaCondition &&
                               upMaxNegativeDeltaCondition && upDeltaChangeCondition && upTotalBuyingVolumeCondition && upTotalSellingVolumeCondition &&
                               upTradesCondition && upTotalVolumeCondition;

            bool showDownArrow = anyDownConditionEnabled && downBarDeltaCondition && downDeltaPercentCondition && downMaxPositiveDeltaCondition &&
                                 downMaxNegativeDeltaCondition && downDeltaChangeCondition && downTotalBuyingVolumeCondition && downTotalSellingVolumeCondition &&
                                 downTradesCondition && downTotalVolumeCondition;

            if (showUpArrow)
            {
                Draw.ArrowUp(this, "UpArrow" + CurrentBar, false, 0, Low[0] - TickSize, UpArrowColor);
            }

            if (showDownArrow)
            {
                Draw.ArrowDown(this, "DownArrow" + CurrentBar, false, 0, High[0] + TickSize, DownArrowColor);
            }
        }

        private bool CheckCondition(bool enabled, double value, double min, double max)
        {
            return !enabled || (value >= min && value <= max);
        }

        private bool AnyConditionEnabledForUp()
        {
            return UpBarDeltaEnabled || DeltaPercentUPFilterEnabled || MaxPositiveDeltaUPEnabled || MaxNegativeDeltaUPEnabled ||
                   DeltaChangeUPEnabled || TotalBuyingVolumeUPEnabled || TotalSellingVolumeUPEnabled || TradesUPEnabled || TotalVolumeUPEnabled;
        }

        private bool AnyConditionEnabledForDown()
        {
            return DownBarDeltaEnabled || DeltaPercentDownFilterEnabled || MaxPositiveDeltaDownEnabled || MaxNegativeDeltaDownEnabled ||
                   DeltaChangeDownEnabled || TotalBuyingVolumeDownEnabled || TotalSellingVolumeDownEnabled || TradesDownEnabled || TotalVolumeDownEnabled;
        }

        #region Properties

        // Propriétés pour les conditions UP
        [NinjaScriptProperty]
        [Display(Name = "UP Bar Delta Enabled", Order = 1, GroupName = "01_VmetricbarDelta0UP")]
        public bool UpBarDeltaEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Bar Delta UP Threshold", Order = 2, GroupName = "01_VmetricbarDelta0UP")]
        public int MinBarDeltaUPThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Bar Delta UP Threshold", Order = 3, GroupName = "01_VmetricbarDelta0UP")]
        public int MaxBarDeltaUPThreshold { get; set; }

        // Ajoutez ici les autres propriétés UP en suivant le même modèle...

        // Propriétés pour les conditions DOWN
        [NinjaScriptProperty]
        [Display(Name = "DOWN Bar Delta Enabled", Order = 1, GroupName = "02_VmetricbarDelta0Down")]
        public bool DownBarDeltaEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Bar Delta Down Threshold", Order = 2, GroupName = "02_VmetricbarDelta0Down")]
        public int MinBarDeltaDownThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max Bar Delta Down Threshold", Order = 3, GroupName = "02_VmetricbarDelta0Down")]
        public int MaxBarDeltaDownThreshold { get; set; }

        // Ajoutez ici les autres propriétés DOWN en suivant le même modèle...
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

        // Propriétés pour les couleurs des flèches
        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Up Arrow Color", Order = 1, GroupName = "Visuals")]
        public Brush UpArrowColor
        {
            get { return upArrowColor; }
            set { upArrowColor = value; }
        }

        [Browsable(false)]
        public string UpArrowColorSerializable
        {
            get { return Serialize.BrushToString(UpArrowColor); }
            set { UpArrowColor = Serialize.StringToBrush(value); }
        }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Down Arrow Color", Order = 2, GroupName = "Visuals")]
        public Brush DownArrowColor
        {
            get { return downArrowColor; }
            set { downArrowColor = value; }
        }

        [Browsable(false)]
        public string DownArrowColorSerializable
        {
            get { return Serialize.BrushToString(DownArrowColor); }
            set { DownArrowColor = Serialize.StringToBrush(value); }
        }

        #endregion
    }
}
