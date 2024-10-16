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
//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators.ninpai
{
    public class InitialBalanceArrowsV6 : Indicator
    {
        #region Variables
        private double ibHigh = double.MinValue;
        private double ibLow = double.MaxValue;
        private bool ibPeriod = true;
        private SessionIterator sessionIterator;
        private DateTime currentDate = Core.Globals.MinDate;
        #endregion

        #region Propriétés
        [NinjaScriptProperty]
        [Display(Name = "Activer IB", Order = 1, GroupName = "Paramètres")]
        public bool ActivateIB { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Heure début IB", Order = 2, GroupName = "Paramètres")]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        public DateTime IBStartTime { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Heure fin IB", Order = 3, GroupName = "Paramètres")]
        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        public DateTime IBEndTime { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Ticks de décalage", Order = 4, GroupName = "Paramètres")]
        public int OffsetTicks { get; set; }
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                                    = @"Affiche des flèches basées sur l'Initial Balance.";
                Name                                           = "InitialBalanceArrowsV6";
                Calculate                                      = Calculate.OnBarClose;
                IsOverlay                                      = true;
                DisplayInDataBox                               = false;
                DrawOnPricePanel                               = true;
                PaintPriceMarkers                              = false;
                IsSuspendedWhileInactive                       = true;
                ActivateIB                                     = true;
                IBStartTime                                    = DateTime.Parse("15:30", System.Globalization.CultureInfo.InvariantCulture);
                IBEndTime                                      = DateTime.Parse("16:30", System.Globalization.CultureInfo.InvariantCulture);
                OffsetTicks                                    = 0;
            }
            else if (State == State.Configure)
            {
                ibHigh = double.MinValue;
                ibLow = double.MaxValue;
                ibPeriod = true;
            }
            else if (State == State.DataLoaded)
            {
                sessionIterator = new SessionIterator(Bars);
            }
            else if (State == State.Historical)
            {
                if (!Bars.BarsType.IsIntraday)
                {
                    Draw.TextFixed(this, "NinjaScriptInfo", "L'indicateur InitialBalanceArrows nécessite des données intrajournalières.", TextPosition.BottomRight);
                    Log("L'indicateur InitialBalanceArrows nécessite des données intrajournalières.", LogLevel.Error);
                }
            }
        }

        protected override void OnBarUpdate()
        {
            if (!Bars.BarsType.IsIntraday || CurrentBar < BarsRequiredToPlot || !ActivateIB)
                return;

            DateTime barTime = Time[0];
            DateTime tradingDay = sessionIterator.GetTradingDay(barTime);

            // Détection du début de session
            if (currentDate != tradingDay)
            {
                currentDate = tradingDay;
                ibHigh = double.MinValue;
                ibLow = double.MaxValue;
                ibPeriod = true;
            }

            // Déterminer l'heure de début et de fin de l'IB pour la session actuelle
            DateTime ibStart = tradingDay.AddHours(IBStartTime.Hour).AddMinutes(IBStartTime.Minute);
            DateTime ibEnd = tradingDay.AddHours(IBEndTime.Hour).AddMinutes(IBEndTime.Minute);

            // Gérer le cas où l'heure de fin est inférieure à l'heure de début (IB traversant minuit)
            if (ibEnd <= ibStart)
                ibEnd = ibEnd.AddDays(1);

            // Pendant la période IB
            if (barTime >= ibStart && barTime <= ibEnd)
            {
                ibHigh = Math.Max(ibHigh, High[0]);
                ibLow = Math.Min(ibLow, Low[0]);

                // Afficher les flèches haut et bas
                Draw.ArrowUp(this, "UpArrow" + CurrentBar, false, 0, Low[0] - TickSize, Brushes.Green);
                Draw.ArrowDown(this, "DownArrow" + CurrentBar, false, 0, High[0] + TickSize, Brushes.Red);
            }
            // Après la période IB
            else if (barTime > ibEnd && ibPeriod)
            {
                ibPeriod = false;
            }

            // Logique pour les flèches après la période IB
            if (!ibPeriod && ibHigh != double.MinValue && ibLow != double.MaxValue)
            {
                double upperBreak = ibHigh + OffsetTicks * TickSize;
                double lowerBreak = ibLow - OffsetTicks * TickSize;

                // Prix dans le range IB
                if (Close[0] <= upperBreak && Close[0] >= lowerBreak)
                {
                    // Afficher les flèches haut et bas
                    Draw.ArrowUp(this, "UpArrow" + CurrentBar, false, 0, Low[0] - TickSize, Brushes.Green);
                    Draw.ArrowDown(this, "DownArrow" + CurrentBar, false, 0, High[0] + TickSize, Brushes.Red);
                }
                // Prix casse au-dessus de IBH + décalage
                else if (Close[0] > upperBreak)
                {
                    // Afficher uniquement les flèches haut
                    Draw.ArrowUp(this, "UpArrow" + CurrentBar, false, 0, Low[0] - TickSize, Brushes.Green);
                }
                // Prix casse en dessous de IBL - décalage
                else if (Close[0] < lowerBreak)
                {
                    // Afficher uniquement les flèches bas
                    Draw.ArrowDown(this, "DownArrow" + CurrentBar, false, 0, High[0] + TickSize, Brushes.Red);
                }
            }
        }
    }
}