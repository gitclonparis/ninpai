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
    public class DiagonalVolumeAbsortionV01 : Indicator
    {
        private double tickSize;
        private SolidColorBrush transRed;
        private SolidColorBrush transGreen;

        #region Paramètres
        [NinjaScriptProperty]
        [Display(Name = "Use Ratio Mode", 
                 Description = "Si activé, utilise le ratio pour calculer les imbalances. Sinon, utilise la différence directe de volume", 
                 Order = 1, GroupName = "Mode de calcul")]
        public bool UseRatioMode { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Imbalance Ratio", 
                 Description = "Ratio minimal entre le volume dominant et le volume faible (utilisé uniquement en mode ratio)", 
                 Order = 2, GroupName = "Mode de calcul")]
        public double ImbalanceRatio { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Volume Difference", 
                 Description = "Différence minimale de volume requise en nombre de contrats (utilisé uniquement en mode différence)", 
                 Order = 3, GroupName = "Mode de calcul")]
        public long VolumeDifference { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Trapped Sellers For Buy", 
                 Description = "Activer la détection des vendeurs piégés pour signal d'achat", 
                 Order = 1, GroupName = "Détection Absorption")]
        public bool UseTrapedSellersForBuy { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Negative Delta", 
                 Description = "Delta négatif minimum requis pour considérer une absorption (ex: -100)", 
                 Order = 2, GroupName = "Détection Absorption")]
        public long MinNegativeDelta { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Trapped Levels", 
                 Description = "Nombre minimum de niveaux avec vendeurs piégés requis", 
                 Order = 3, GroupName = "Détection Absorption")]
        public int MinTrappedLevels { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Bullish Imbalance Count", 
                 Description = "Nombre minimal d'imbalances acheteuses requis pour afficher la flèche haussière", 
                 Order = 1, GroupName = "Paramètres Imbalance")]
        public int MinBullishImbalanceCount { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Bearish Imbalance Count", 
                 Description = "Nombre minimal d'imbalances vendeuses requis pour afficher la flèche baissière", 
                 Order = 2, GroupName = "Paramètres Imbalance")]
        public int MinBearishImbalanceCount { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Imbalance UP", 
                 Description = "Si activé, affiche la flèche haussière (UP) lorsque la condition est remplie", 
                 Order = 3, GroupName = "Paramètres Imbalance")]
        public bool UseImbalanceUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Imbalance Down", 
                 Description = "Si activé, affiche la flèche baissière (DOWN) lorsque la condition est remplie", 
                 Order = 4, GroupName = "Paramètres Imbalance")]
        public bool UseImbalanceDown { get; set; }
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicateur de Diagonal Volume Imbalance avec détection des vendeurs piégés et absorption.";
                Name = "DiagonalVolumeAbsortionV01";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;

                // Valeurs par défaut
                UseRatioMode = true;
                ImbalanceRatio = 2.0;
                VolumeDifference = 100;
                MinBullishImbalanceCount = 3;
                MinBearishImbalanceCount = 3;
                UseImbalanceUP = true;
                UseImbalanceDown = true;
                
                // Nouveaux paramètres par défaut
                UseTrapedSellersForBuy = false;
                MinNegativeDelta = -100;
                MinTrappedLevels = 3;

                AddPlot(Brushes.Transparent, "DummyPlot");
            }
            else if (State == State.DataLoaded)
            {
                tickSize = Instrument.MasterInstrument.TickSize;
                transRed = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
                transRed.Freeze();
                transGreen = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
                transGreen.Freeze();
            }
        }

        protected override void OnBarUpdate()
        {
            int bullishCount = 0, bearishCount = 0;
            List<double> trappedSellerLevels = new List<double>();

            // Évaluation standard des imbalances
            if (!UseTrapedSellersForBuy || !CheckTrappedSellers(out trappedSellerLevels))
            {
                EvaluateImbalances(out bullishCount, out bearishCount);

                if (UseImbalanceUP && bullishCount >= MinBullishImbalanceCount)
                {
                    Draw.ArrowUp(this, "BullishArrow_" + CurrentBar, true, 0, Low[0] - (2 * tickSize), Brushes.Green);
                }

                if (UseImbalanceDown && bearishCount >= MinBearishImbalanceCount)
                {
                    Draw.ArrowDown(this, "BearishArrow_" + CurrentBar, true, 0, High[0] + (2 * tickSize), Brushes.Red);
                }
            }
            
            // Si des vendeurs sont piégés et que le signal est validé
            if (UseTrapedSellersForBuy && trappedSellerLevels.Count >= MinTrappedLevels)
            {
                // Dessiner une flèche verte spéciale pour l'absorption
                Draw.ArrowUp(this, "AbsorptionArrow_" + CurrentBar, true, 0, Low[0] - (3 * tickSize), Brushes.LimeGreen);
                
                // Optionnel : Marquer les niveaux de prix où les vendeurs sont piégés
                foreach (double level in trappedSellerLevels)
                {
                    Draw.Dot(this, "TrappedLevel_" + CurrentBar + "_" + level, true, 0, level, transRed);
                }
            }
        }

        private bool CheckTrappedSellers(out List<double> trappedLevels)
        {
            trappedLevels = new List<double>();
            
            var volBarType = Bars.BarsSeries.BarsType as NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType;
            if (volBarType == null)
                return false;

            double closePrice = Close[0];

            // Parcourir tous les niveaux de prix de la barre
            for (double price = Low[0]; price <= High[0]; price += tickSize)
            {
                double askLevel = price + tickSize;
                long bidVol = volBarType.Volumes[CurrentBar].GetBidVolumeForPrice(price);
                long askVol = volBarType.Volumes[CurrentBar].GetAskVolumeForPrice(askLevel);

                if (bidVol == 0 && askVol == 0)
                    continue;

                // Calculer le delta
                long delta = askVol - bidVol;

                // Vérifier si le delta est suffisamment négatif (beaucoup de vendeurs)
                // et si le prix de clôture est au-dessus de ce niveau (vendeurs piégés)
                if (delta <= MinNegativeDelta && closePrice > price)
                {
                    trappedLevels.Add(price);
                }
            }

            return trappedLevels.Count >= MinTrappedLevels;
        }

        private void EvaluateImbalances(out int bullishCount, out int bearishCount)
        {
            bullishCount = 0;
            bearishCount = 0;

            var volBarType = Bars.BarsSeries.BarsType as NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType;
            if (volBarType == null)
                return;

            for (double price = Low[0]; price <= High[0]; price += tickSize)
            {
                double askLevel = price + tickSize;
                long bidVol = volBarType.Volumes[CurrentBar].GetBidVolumeForPrice(price);
                long askVol = volBarType.Volumes[CurrentBar].GetAskVolumeForPrice(askLevel);

                if (bidVol == 0 && askVol == 0)
                    continue;

                if (UseRatioMode)
                {
                    if (bidVol > 0)
                    {
                        double ratioAskBid = (double)askVol / bidVol;
                        if (ratioAskBid >= ImbalanceRatio)
                        {
                            bullishCount++;
                        }
                    }

                    if (askVol > 0)
                    {
                        double ratioBidAsk = (double)bidVol / askVol;
                        if (ratioBidAsk >= ImbalanceRatio)
                        {
                            bearishCount++;
                        }
                    }
                }
                else
                {
                    long deltaUp = askVol - bidVol;
                    if (deltaUp >= VolumeDifference)
                    {
                        bullishCount++;
                    }

                    long deltaDown = bidVol - askVol;
                    if (deltaDown >= VolumeDifference)
                    {
                        bearishCount++;
                    }
                }
            }
        }
    }
}
