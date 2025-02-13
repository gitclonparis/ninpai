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
    public class DiagonalVolumeImbalanceV2 : Indicator
    {
        private double tickSize;
        private SolidColorBrush transRed;
        private SolidColorBrush transGreen;

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
                Description = "Parcourt tous les niveaux d'une barre en comparant en diagonale le volume Bid (à 'price') et le volume Ask (à 'price + tick') et trace un point rouge pour une imbalance baissière et un point vert pour une imbalance haussière.";
                Name = "DiagonalVolumeImbalanceV2";
                Calculate = Calculate.OnBarClose; // Calcul à la clôture de la barre
                IsOverlay = true;                 // L’indicateur s'affiche sur le graphique principal
                ImbalanceRatio = 2.0;             // Ratio par défaut de 2:1
                MinDelta = 100;                   // Delta minimum par défaut
                AddPlot(Brushes.Transparent, "DummyPlot"); // Plot fictif pour respecter la structure
            }
            else if (State == State.Configure)
            {
                // Optionnel : vérifier que la DataSeries utilise des Volumetric Bars.
            }
            else if (State == State.DataLoaded)
            {
                tickSize = Instrument.MasterInstrument.TickSize;

                // Création des brushes semi-transparents (alpha = 128 sur 255 => environ 50% de transparence)
                transRed = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
                transRed.Freeze();
                transGreen = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
                transGreen.Freeze();
            }
        }

        protected override void OnBarUpdate()
        {
            // Récupération de l'objet VolumetricBarsType associé à la DataSeries
            var volBarType = Bars.BarsSeries.BarsType as NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType;
            if (volBarType == null)
                return;

            // Parcourir tous les niveaux de prix de la barre
            // Pour chaque niveau, on compare le volume Bid à 'price' et le volume Ask à 'price + tickSize'
            for (double price = Low[0]; price <= High[0]; price += tickSize)
            {
                double askLevel = price + tickSize; // niveau pour le volume Ask (comparaison en diagonale)
                long bidVol = volBarType.Volumes[CurrentBar].GetBidVolumeForPrice(price);
                long askVol = volBarType.Volumes[CurrentBar].GetAskVolumeForPrice(askLevel);

                // Si aucun volume n'est présent sur ces deux niveaux, passer au suivant
                if (bidVol == 0 && askVol == 0)
                    continue;

                // -------------------------------
                // Traitement des cas où l’un des volumes est nul
                if (bidVol == 0 && askVol > MinDelta)
                {
                    if (askVol >= MinDelta)
                    {
                        
                        string tag = "BearishPoint_" + CurrentBar + "_" + price;
                        Draw.Dot(this, tag, true, 0, askLevel, transRed);
                    }
                }
                else if (askVol == 0 && bidVol > MinDelta)
                {
                    if (bidVol >= MinDelta)
                    {
                        
                        string tag = "BullishPoint_" + CurrentBar + "_" + price;
                        Draw.Dot(this, tag, true, 0, price, transGreen);
                    }
                }
                // -------------------------------
                // Traitement des cas où les deux volumes sont non nuls
                else
                {
                    long delta = askVol - bidVol;  // delta diagonal
                    double ratioAskBid = (double)askVol / bidVol;
                    double ratioBidAsk = (double)bidVol / askVol;

                    // Imbalance baissière : volume Ask dominant
                    if (ratioAskBid >= ImbalanceRatio && delta >= MinDelta)
                    {
                        string tag = "BearishPoint_" + CurrentBar + "_" + price;
                        Draw.Dot(this, tag, true, 0, askLevel, transRed);
                    }
                    // Imbalance haussière : volume Bid dominant
                    if (ratioBidAsk >= ImbalanceRatio && delta >= MinDelta)
                    {
                        string tag = "BullishPoint_" + CurrentBar + "_" + price;
                        Draw.Dot(this, tag, true, 0, price, transGreen);
                    }
                }
            }
        }
    }
}
