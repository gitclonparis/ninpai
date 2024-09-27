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
    public class volumetrifilterindicator : Indicator
    {
        #region Variables
        private Brush upArrowColor;
        private Brush downArrowColor;
        #endregion

        

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicateur personnalisé qui affiche des flèches en fonction du BarDelta et du DeltaPercent dans des plages définies.";
                Name = "volumetrifilterindicator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                PaintPriceMarkers = false;
                IsSuspendedWhileInactive = true;
                
                // Paramètres par défaut pour UP
                UpBarDeltaEnabled = true;
                MinBarDeltaUPThreshold = 500;
                MaxBarDeltaUPThreshold = 2000;
                DeltaPercentUPFilterEnabled = true;
                MinDeltaPercentUP = 10;
                MaxDeltaPercentUP = 50;
                
                // Paramètres par défaut pour DOWN
                DownBarDeltaEnabled = true;
                MinBarDeltaDownThreshold = -500;
                MaxBarDeltaDownThreshold = -2000;
                DeltaPercentDownFilterEnabled = true;
                MinDeltaPercentDown = -10;
                MaxDeltaPercentDown = -30;
                
                // Couleurs par défaut
                UpArrowColor = Brushes.Green;
                DownArrowColor = Brushes.Red;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1)
                return;

            // Vérifier si le type de barres est Volumetric
            if (!(Bars.BarsSeries.BarsType is NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType))
                return;

            var barsType = Bars.BarsSeries.BarsType as NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType;
            if (barsType == null)
                return;

            var VmetricbarDelta0 = barsType.Volumes[CurrentBar].BarDelta;
            var VmetricdeltaPercent0 = barsType.Volumes[CurrentBar].GetDeltaPercent();

            bool upBarDeltaCondition = !UpBarDeltaEnabled || (VmetricbarDelta0 >= MinBarDeltaUPThreshold && VmetricbarDelta0 <= MaxBarDeltaUPThreshold);
            bool upDeltaPercentCondition = !DeltaPercentUPFilterEnabled || (VmetricdeltaPercent0 >= MinDeltaPercentUP && VmetricdeltaPercent0 <= MaxDeltaPercentUP);

            bool downBarDeltaCondition = !DownBarDeltaEnabled || (VmetricbarDelta0 <= MinBarDeltaDownThreshold && VmetricbarDelta0 >= MaxBarDeltaDownThreshold);
            bool downDeltaPercentCondition = !DeltaPercentDownFilterEnabled || (VmetricdeltaPercent0 <= MinDeltaPercentDown && VmetricdeltaPercent0 >= MaxDeltaPercentDown);

            // Condition pour flèche UP
            if (upBarDeltaCondition && upDeltaPercentCondition)
            {
                Draw.ArrowUp(this, "UpArrow" + CurrentBar, false, 0, Low[0] - TickSize, UpArrowColor);
            }

            // Condition pour flèche DOWN
            if (downBarDeltaCondition && downDeltaPercentCondition)
            {
                Draw.ArrowDown(this, "DownArrow" + CurrentBar, false, 0, High[0] + TickSize, DownArrowColor);
            }
        }
		
		#region Properties
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
