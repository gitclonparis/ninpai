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
    public class DiagonalVolumeImbalance : Indicator
    {
        private double tickSize;

        #region Paramètres
        [NinjaScriptProperty]
        [Display(Name = "Imbalance Ratio", Description = "Ratio minimal entre le volume dominant et le volume faible (ex. 2 signifie qu’un côté doit être au moins 2 fois supérieur à l’autre)", Order = 1, GroupName = "Paramètres")]
        public double ImbalanceRatio { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Minimum Delta", Description = "Delta minimum (différence entre le volume Ask et Bid) requis pour déclencher le signal", Order = 2, GroupName = "Paramètres")]
        public long MinDelta { get; set; }
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Parcourt tous les niveaux de prix d'une barre pour comparer en diagonale le volume Bid (à 'price') et le volume Ask (à 'price + tick') et trace une flèche selon l’imbalance détectée.";
                Name = "DiagonalVolumeImbalance";
                Calculate = Calculate.OnBarClose; // On calcule à la clôture de la barre
                IsOverlay = true;                 // L’indicateur s'affiche sur le graphique principal
                ImbalanceRatio = 2.0;             // Ratio par défaut de 2:1
                MinDelta = 100;                   // Delta minimum par défaut
                AddPlot(Brushes.Transparent, "DummyPlot"); // Plot fictif (obligatoire dans la structure)
            }
            else if (State == State.Configure)
            {
                // Vous pouvez ajouter ici des vérifications (ex. s'assurer que la DataSeries est bien en Volumetric Bars)
            }
            else if (State == State.DataLoaded)
            {
                tickSize = Instrument.MasterInstrument.TickSize;
            }
        }

        protected override void OnBarUpdate()
        {
            // Récupérer l'objet VolumetricBarsType
            var volBarType = Bars.BarsSeries.BarsType as NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType;
            if (volBarType == null)
                return;

            // Ces flags permettront de savoir si, dans la barre, on a détecté une imbalance haussière (bid dominant)
            // et/ou baissière (ask dominant)
            bool foundDown = false; // ask dominant → pression vendeuse → flèche vers le bas
            bool foundUp   = false; // bid dominant → pression acheteuse → flèche vers le haut

            // Parcourir tous les niveaux de prix de la barre
            // Pour chaque niveau, nous comparerons le volume Bid à 'price' et le volume Ask à 'price + tickSize'
            // Cela correspond à une comparaison en diagonale
            for (double price = Low[0]; price <= High[0]; price += tickSize)
            {
                double diagonalPrice = price + tickSize; // Le niveau pour le volume Ask

                // Récupération des volumes sur la barre courante
                long bidVol = volBarType.Volumes[CurrentBar].GetBidVolumeForPrice(price);
                long askVol = volBarType.Volumes[CurrentBar].GetAskVolumeForPrice(diagonalPrice);

                // Si aucun volume n'est présent aux deux niveaux, passer au suivant.
                if (bidVol == 0 && askVol == 0)
                    continue;

                // Cas où l’un des volumes est nul :
                if (bidVol == 0 && askVol > 0)
                {
                    if (askVol >= MinDelta)
                        foundDown = true;  // Imbalance baissière détectée
                }
                else if (askVol == 0 && bidVol > 0)
                {
                    if (bidVol >= MinDelta)
                        foundUp = true;    // Imbalance haussière détectée
                }
                // Cas où les deux volumes sont non nuls :
                else
                {
                    // Calcul du delta en diagonale
                    long deltaDiagonal = askVol - bidVol;

                    // Vérification de l’imbalance baissière (ask dominant)
                    if (((double)askVol / bidVol) >= ImbalanceRatio && deltaDiagonal >= MinDelta)
                    {
                        foundDown = true;
                    }
                    // Vérification de l’imbalance haussière (bid dominant)
                    if (((double)bidVol / askVol) >= ImbalanceRatio && -deltaDiagonal >= MinDelta)
                    {
                        foundUp = true;
                    }
                }
            }

            // Tracer les flèches sur la barre courante selon les signaux détectés
            // Pour l'imbalance baissière, une flèche vers le bas est tracée au-dessus du High de la barre
            if (foundDown)
            {
                Draw.ArrowDown(this, "ImbalanceDown" + CurrentBar, true, 0, High[0] + (2 * tickSize), Brushes.Red);
            }
            // Pour l'imbalance haussière, une flèche vers le haut est tracée en dessous du Low de la barre
            if (foundUp)
            {
                Draw.ArrowUp(this, "ImbalanceUp" + CurrentBar, true, 0, Low[0] - (2 * tickSize), Brushes.Green);
            }
        }
    }
}
