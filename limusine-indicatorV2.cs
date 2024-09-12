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
    public class LimusineIndicatorV2 : Indicator
    {
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Indicateur de Limusine";
                Name = "LimusineIndicatorV2";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;
                MinimumTicks = 20;
                ShowLimusineOpenCloseUP = true;
                ShowLimusineOpenCloseDOWN = true;
                ShowLimusineHighLowUP = true;
                ShowLimusineHighLowDOWN = true;
            }
        }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Minimum Ticks", Description = "Nombre minimum de ticks pour une limusine", Order = 1, GroupName = "Parameters")]
        public int MinimumTicks { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Afficher Limusine Open-Close UP", Description = "Afficher les limusines Open-Close UP", Order = 2, GroupName = "Parameters")]
        public bool ShowLimusineOpenCloseUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Afficher Limusine Open-Close DOWN", Description = "Afficher les limusines Open-Close DOWN", Order = 3, GroupName = "Parameters")]
        public bool ShowLimusineOpenCloseDOWN { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Afficher Limusine High-Low UP", Description = "Afficher les limusines High-Low UP", Order = 4, GroupName = "Parameters")]
        public bool ShowLimusineHighLowUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Afficher Limusine High-Low DOWN", Description = "Afficher les limusines High-Low DOWN", Order = 5, GroupName = "Parameters")]
        public bool ShowLimusineHighLowDOWN { get; set; }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1) return;

            // Calculer les différences en ticks
            double openCloseDiff = Math.Abs(Open[0] - Close[0]) / TickSize;
            double highLowDiff = Math.Abs(High[0] - Low[0]) / TickSize;

            // Vérifier les conditions pour chaque type de limusine
            bool isLimusineOpenCloseUP = ShowLimusineOpenCloseUP && openCloseDiff >= MinimumTicks && Close[0] > Open[0];
            bool isLimusineOpenCloseDOWN = ShowLimusineOpenCloseDOWN && openCloseDiff >= MinimumTicks && Close[0] < Open[0];
            bool isLimusineHighLowUP = ShowLimusineHighLowUP && highLowDiff >= MinimumTicks && Close[0] > Open[0];
            bool isLimusineHighLowDOWN = ShowLimusineHighLowDOWN && highLowDiff >= MinimumTicks && Close[0] < Open[0];

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
    }
}
