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
    public class DiagonalVolumeImbalanceV6 : Indicator
    {
        private double tickSize;
        private SolidColorBrush transRed;
        private SolidColorBrush transGreen;

        #region Paramètres
        [NinjaScriptProperty]
        [Display(Name = "Use Ratio Mode", 
                 Description = "Si activé, utilise le ratio pour calculer les imbalances. Sinon, utilise la différence directe de volume", 
                 Order = 1, GroupName = "Paramètres")]
        public bool UseRatioMode { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Imbalance Ratio", 
                 Description = "Ratio minimal entre le volume dominant et le volume faible (utilisé uniquement en mode ratio)", 
                 Order = 2, GroupName = "Paramètres")]
        public double ImbalanceRatio { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Volume Difference", 
                 Description = "Différence minimale de volume requise en nombre de contrats (utilisé uniquement en mode différence)", 
                 Order = 3, GroupName = "Paramètres")]
        public long VolumeDifference { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Bullish Imbalance Count", 
                 Description = "Nombre minimal d'imbalances acheteuses requis pour afficher la flèche haussière", 
                 Order = 4, GroupName = "Paramètres")]
        public int MinBullishImbalanceCount { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Min Bearish Imbalance Count", 
                 Description = "Nombre minimal d'imbalances vendeuses requis pour afficher la flèche baissière", 
                 Order = 5, GroupName = "Paramètres")]
        public int MinBearishImbalanceCount { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Imbalance UP", 
                 Description = "Si activé, affiche la flèche haussière (UP) lorsque la condition est remplie", 
                 Order = 6, GroupName = "Paramètres")]
        public bool UseImbalanceUP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Imbalance Down", 
                 Description = "Si activé, affiche la flèche baissière (DOWN) lorsque la condition est remplie", 
                 Order = 7, GroupName = "Paramètres")]
        public bool UseImbalanceDown { get; set; }
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicateur de Diagonal Volume Imbalance avec choix entre mode ratio ou différence directe de volume.";
                Name = "DiagonalVolumeImbalanceV6";
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
            int bullishCount, bearishCount;
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

                // Calcul des imbalances en fonction du mode choisi
                if (UseRatioMode)
                {
                    // Mode ratio (original)
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
                    // Mode différence directe de volume
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
