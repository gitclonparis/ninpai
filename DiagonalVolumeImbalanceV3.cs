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
    public class DiagonalVolumeImbalanceV3 : Indicator
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
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Parcourt tous les niveaux d'une barre en comparant en diagonale le volume Bid et le volume Ask et trace : " +
                              "- un point vert (imbalance acheteuse) au niveau Bid, " +
                              "- un point rouge (imbalance vendeuse) au niveau Ask.";
                Name = "DiagonalVolumeImbalanceV3";
                Calculate = Calculate.OnBarClose; // Calcul à la clôture de chaque barre
                IsOverlay = true;                 // S'affiche sur le graphique principal
                ImbalanceRatio = 2.0;             // Ratio par défaut 2:1
                MinDelta = 100;                   // Delta minimum par défaut
                AddPlot(Brushes.Transparent, "DummyPlot"); // Plot fictif
            }
            else if (State == State.Configure)
            {
                // Vous pouvez ajouter ici des vérifications (ex. s'assurer que la DataSeries est en Volumetric Bars)
            }
            else if (State == State.DataLoaded)
            {
                tickSize = Instrument.MasterInstrument.TickSize;
                
                // Création de brushes semi-transparents (alpha = 128 sur 255 => environ 50% de transparence)
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

            // Parcourir tous les niveaux de prix de la barre (de Low à High, par pas de tickSize)
            for (double price = Low[0]; price <= High[0]; price += tickSize)
            {
                double askLevel = price + tickSize; // Niveau diagonal pour le volume Ask
                long bidVol = volBarType.Volumes[CurrentBar].GetBidVolumeForPrice(price);
                long askVol = volBarType.Volumes[CurrentBar].GetAskVolumeForPrice(askLevel);

                // Si aucun volume n'est présent sur ces deux niveaux, passer au suivant
                if (bidVol == 0 && askVol == 0)
                    continue;

                // -------------------------------
                // Imbalance acheteuse
                // Formule : deltaup = askVol - bidVol et ratioAskBid = askVol / bidVol
                // On trace un point vert au niveau Bid (price)
                if (bidVol > 0)
                {
                    long deltaup = askVol - bidVol;
                    double ratioAskBid = (double)askVol / bidVol;
                    if (ratioAskBid >= ImbalanceRatio && deltaup >= MinDelta)
                    {
                        string tag = "BullishPoint_" + CurrentBar + "_" + price;
                        Draw.Dot(this, tag, true, 0, price, transGreen);
                    }
                }

                // -------------------------------
                // Imbalance vendeuse
                // Formule : deltadown = bidVol - askVol et ratioBidAsk = bidVol / askVol
                // On trace un point rouge au niveau Ask (askLevel)
                if (askVol > 0)
                {
                    long deltadown = bidVol - askVol;
                    double ratioBidAsk = (double)bidVol / askVol;
                    if (ratioBidAsk >= ImbalanceRatio && deltadown >= MinDelta)
                    {
                        string tag = "BearishPoint_" + CurrentBar + "_" + price;
                        Draw.Dot(this, tag, true, 0, askLevel, transRed);
                    }
                }
            }
        }
    }
}
