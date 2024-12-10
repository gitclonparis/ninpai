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
    public class MyPredictionIndicatorGPT : Indicator
    {
        // Coefficients à insérer après entraînement en Python
        private double a0 = 0.0;
        private double a1 = 0.0;
        private double a2 = 0.0;
        private double a3 = 0.0;
        private double a4 = 0.0;
        private double a5 = 0.0;
        private double a6 = 0.0;
        private double a7 = 0.0;
        private double a8 = 0.0;
        private double a9 = 0.0;
        private double a10 = 0.0;
        
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "MyPredictionIndicatorGPT";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                // ...
            }
        }

        protected override void OnBarUpdate()
        {
            // Il faut au moins 5 barres antérieures pour calculer la moyenne
            if (CurrentBar < 5) 
                return;
            
            // Calcul des moyennes sur 5 barres
            double avgOpen5 = (Open[1] + Open[2] + Open[3] + Open[4] + Open[5]) / 5.0;
            double avgHigh5 = (High[1] + High[2] + High[3] + High[4] + High[5]) / 5.0;
            double avgLow5  = (Low[1] + Low[2] + Low[3] + Low[4] + Low[5]) / 5.0;
            double avgVol5  = (Volume[1] + Volume[2] + Volume[3] + Volume[4] + Volume[5]) / 5.0;

            int currentYear = Time[0].Year;
            int dow = (int)Time[0].DayOfWeek; 
            
            int dow0 = (dow == 0) ? 1 : 0;
            int dow1 = (dow == 1) ? 1 : 0;
            int dow2 = (dow == 2) ? 1 : 0;
            int dow3 = (dow == 3) ? 1 : 0;
            int dow4 = (dow == 4) ? 1 : 0;

            // Prédiction
            double predictedClose = a0 
                                    + a1 * avgOpen5
                                    + a2 * avgHigh5
                                    + a3 * avgLow5
                                    + a4 * avgVol5
                                    + a5 * currentYear
                                    + a6 * dow0
                                    + a7 * dow1
                                    + a8 * dow2
                                    + a9 * dow3
                                    + a10 * dow4;
            
            // Affichage sur le graphique (point rouge sur la barre actuelle)
            Draw.Dot(this, "Prediction"+CurrentBar, false, 0, predictedClose, Brushes.Red);
            
            // Si vous souhaitez l'afficher sur une barre future (ex: 5 barres dans le futur),
            // vous pouvez essayer d'utiliser une date future (Attention: il n’y aura pas de barre future encore visible)
            // Par exemple, si chaque barre est 1 minute :
            // Draw.Dot(this, "FuturePred"+CurrentBar, false, Time[0].AddMinutes(5), predictedClose, Brushes.Blue);
        }
    }
}