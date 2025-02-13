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

// Changement du namespace pour éviter les conflits
namespace NinjaTrader.NinjaScript.Indicators.ninpai
{
    public class DiagonalVolumeImbalanceV4 : Indicator
    {
        private double tickSize;
        private SolidColorBrush transRed;
        private SolidColorBrush transGreen;

        #region Paramètres
        [NinjaScriptProperty]
        [Display(Name = "Imbalance Ratio", 
                 Description = "Ratio minimal entre le volume dominant et le volume faible (ex. 2 signifie qu’un côté doit être au moins 2 fois supérieur à l’autre)", 
                 Order = 1, GroupName = "Paramètres")]
        public double ImbalanceRatio { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Minimum Delta", 
                 Description = "Delta minimum requis (différence entre les volumes) pour déclencher le signal", 
                 Order = 2, GroupName = "Paramètres")]
        public long MinDelta { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Bullish Imbalance Count", 
                 Description = "Nombre minimal d'imbalances acheteuses requis pour afficher la flèche haussière", 
                 Order = 3, GroupName = "Paramètres")]
        public int MinBullishImbalanceCount { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Bearish Imbalance Count", 
                 Description = "Nombre minimal d'imbalances vendeuses requis pour afficher la flèche baissière", 
                 Order = 4, GroupName = "Paramètres")]
        public int MinBearishImbalanceCount { get; set; }
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicateur de Diagonal Volume Imbalance avec filtrage par nombre d'imbalances. " +
                              "Une flèche haussière est affichée si le nombre d'imbalances acheteuses dépasse un seuil défini, " +
                              "et une flèche baissière si le nombre d'imbalances vendeuses dépasse un seuil.";
                Name = "DiagonalVolumeImbalanceV4";
                Calculate = Calculate.OnBarClose; // On utilise la clôture de la barre pour le calcul
                IsOverlay = true;                 // L'indicateur s'affiche sur le graphique principal

                // Valeurs par défaut
                ImbalanceRatio = 2.0;
                MinDelta = 100;
                MinBullishImbalanceCount = 3;
                MinBearishImbalanceCount = 3;

                AddPlot(Brushes.Transparent, "DummyPlot"); // Plot fictif requis
            }
            else if (State == State.Configure)
            {
                // Vous pouvez ajouter ici des vérifications (ex. s'assurer que la DataSeries est en Volumetric Bars)
            }
            else if (State == State.DataLoaded)
            {
                tickSize = Instrument.MasterInstrument.TickSize;

                // Création de pinceaux semi-transparents (alpha = 128 sur 255, environ 50% de transparence)
                transRed = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
                transRed.Freeze();
                transGreen = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
                transGreen.Freeze();
            }
        }

        protected override void OnBarUpdate()
        {
            // Évaluation des imbalances sur la barre actuelle à l'aide de la méthode dédiée
            int bullishCount, bearishCount;
            EvaluateImbalances(out bullishCount, out bearishCount);

            // Si le nombre d'imbalances acheteuses est supérieur ou égal au seuil défini,
            // tracer une flèche haussière (arrow up) en dessous du Low de la barre
            if (bullishCount >= MinBullishImbalanceCount)
            {
                Draw.ArrowUp(this, "BullishArrow_" + CurrentBar, true, 0, Low[0] - (2 * tickSize), Brushes.Green);
            }

            // Si le nombre d'imbalances vendeuses est supérieur ou égal au seuil défini,
            // tracer une flèche baissière (arrow down) au-dessus du High de la barre
            if (bearishCount >= MinBearishImbalanceCount)
            {
                Draw.ArrowDown(this, "BearishArrow_" + CurrentBar, true, 0, High[0] + (2 * tickSize), Brushes.Red);
            }
        }

        /// <summary>
        /// Parcourt tous les niveaux de la barre actuelle pour compter les imbalances.
        /// Pour l'imbalance acheteuse : 
        ///     deltaup = askVol - bidVol et ratioAskBid = askVol / bidVol.
        /// Pour l'imbalance vendeuse : 
        ///     deltadown = bidVol - askVol et ratioBidAsk = bidVol / askVol.
        /// </summary>
        /// <param name="bullishCount">Nombre d'imbalances acheteuses trouvées</param>
        /// <param name="bearishCount">Nombre d'imbalances vendeuses trouvées</param>
        private void EvaluateImbalances(out int bullishCount, out int bearishCount)
        {
            bullishCount = 0;
            bearishCount = 0;

            var volBarType = Bars.BarsSeries.BarsType as NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType;
            if (volBarType == null)
                return;

            // Parcourt tous les niveaux de la barre (de Low à High, par incréments de tickSize)
            for (double price = Low[0]; price <= High[0]; price += tickSize)
            {
                double askLevel = price + tickSize;  // Comparaison en diagonale : volume Ask au niveau "price + tickSize"
                long bidVol = volBarType.Volumes[CurrentBar].GetBidVolumeForPrice(price);
                long askVol = volBarType.Volumes[CurrentBar].GetAskVolumeForPrice(askLevel);

                // Si aucun volume n'est présent, passer à l'itération suivante
                if (bidVol == 0 && askVol == 0)
                    continue;

                // Imbalance acheteuse : on vérifie si le volume Ask est suffisamment supérieur au volume Bid
                // Formule : deltaup = askVol - bidVol et ratioAskBid = askVol / bidVol
                if (bidVol > 0)
                {
                    long deltaup = askVol - bidVol;
                    double ratioAskBid = (double)askVol / bidVol;
                    if (ratioAskBid >= ImbalanceRatio && deltaup >= MinDelta)
                    {
                        bullishCount++;
                    }
                }

                // Imbalance vendeuse : on vérifie si le volume Bid est suffisamment supérieur au volume Ask
                // Formule : deltadown = bidVol - askVol et ratioBidAsk = bidVol / askVol
                if (askVol > 0)
                {
                    long deltadown = bidVol - askVol;
                    double ratioBidAsk = (double)bidVol / askVol;
                    if (ratioBidAsk >= ImbalanceRatio && deltadown >= MinDelta)
                    {
                        bearishCount++;
                    }
                }
            }
        }
    }
}
