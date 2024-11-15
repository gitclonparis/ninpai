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
    public class VolumeProfileIndicatorV15 : Indicator
    {
        private Dictionary<double, double> volumeProfile;
        private double vah, val, poc;
        private List<HistoricalLevel> historicalLevels;

        private class HistoricalLevel
        {
            public DateTime Date { get; set; }
            public double VAH { get; set; }
            public double VAL { get; set; }
            public double POC { get; set; }
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Volume Profile avec VAH, VAL, et POC";
                Name = "Volume Profile Indicator V15";
                PeriodeCalcul = PeriodeType.Journalier;
                HistoriqueSessions = 5;
                VAHCouleur = Brushes.Red;
                VALCouleur = Brushes.Green;
                POCCouleur = Brushes.Blue;
                EpaisseurLignes = 2;
                Transparence = 50;
                IsOverlay = true;
                Calculate = Calculate.OnBarClose;
            }
            else if (State == State.DataLoaded)
            {
                volumeProfile = new Dictionary<double, double>();
                historicalLevels = new List<HistoricalLevel>();
            }
        }

        private int GetBarsForPeriod()
        {
            if (CurrentBar == 0) return 1;

            switch (PeriodeCalcul)
            {
                case PeriodeType.Journalier:
                    int bars = 0;
                    DateTime currentDate = Time[0].Date;
                    while (bars <= CurrentBar && Time[bars].Date == currentDate)
                        bars++;
                    return bars;
                case PeriodeType.Hebdomadaire:
                    bars = 0;
                    currentDate = Time[0].Date;
                    while (bars <= CurrentBar && Time[bars].Date >= currentDate.AddDays(-7))
                        bars++;
                    return bars;
                default:
                    return Math.Min(100, CurrentBar + 1);
            }
        }

        private void StoreHistoricalLevels()
        {
            if (historicalLevels == null)
                historicalLevels = new List<HistoricalLevel>();

            var newLevel = new HistoricalLevel
            {
                Date = Time[0].Date,
                VAH = vah,
                VAL = val,
                POC = poc
            };

            // Vérifier si une entrée pour cette date existe déjà
            int existingIndex = historicalLevels.FindIndex(x => x.Date == Time[0].Date);
            if (existingIndex >= 0)
                historicalLevels[existingIndex] = newLevel;
            else
            {
                historicalLevels.Insert(0, newLevel);
                if (historicalLevels.Count > HistoriqueSessions)
                    historicalLevels.RemoveAt(historicalLevels.Count - 1);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1 || volumeProfile == null) return;

            try
            {
                if (IsNewPeriod())
                {
                    CalculateVolumeProfile();
                    if (volumeProfile.Count > 0)
                    {
                        CalculateValueArea();
                        StoreHistoricalLevels();
                    }
                }

                if (volumeProfile.Count > 0)
                    DrawLevels();
            }
            catch (Exception ex)
            {
                Print("Erreur dans OnBarUpdate: " + ex.Message);
            }
        }

        private bool IsNewPeriod()
        {
            if (CurrentBar < 1) return false;

            switch (PeriodeCalcul)
            {
                case PeriodeType.Journalier:
                    return Time[0].Date != Time[1].Date;
                case PeriodeType.Hebdomadaire:
                    return Time[0].Date.DayOfWeek < Time[1].Date.DayOfWeek;
                default:
                    return false;
            }
        }

        private void CalculateVolumeProfile()
        {
            volumeProfile.Clear();
            int barsToCalculate = GetBarsForPeriod();

            for (int i = 0; i < barsToCalculate && i <= CurrentBar; i++)
            {
                double price = Math.Round(Close[i], 2);
                if (!volumeProfile.ContainsKey(price))
                    volumeProfile[price] = 0;
                volumeProfile[price] += Volume[i];
            }
        }

        private void CalculateValueArea()
        {
            if (volumeProfile.Count == 0) return;

            poc = volumeProfile.OrderByDescending(x => x.Value).First().Key;

            double totalVolume = volumeProfile.Values.Sum();
            double targetVolume = totalVolume * 0.7;
            double currentVolume = 0;

            var sortedPrices = volumeProfile.OrderByDescending(x => x.Value);
            var includedPrices = new List<double>();

            foreach (var kvp in sortedPrices)
            {
                includedPrices.Add(kvp.Key);
                currentVolume += kvp.Value;
                if (currentVolume >= targetVolume)
                    break;
            }

            vah = includedPrices.Max();
            val = includedPrices.Min();
        }

        private void DrawLevels()
        {
            // Effacer les dessins précédents
            ClearOutputWindow();

            // Dessiner les niveaux actuels
            Draw.Line(this, "VAH", false, 0, vah, Math.Min(10, CurrentBar), vah, 
                VAHCouleur, DashStyleHelper.Solid, EpaisseurLignes);
            Draw.Line(this, "VAL", false, 0, val, Math.Min(10, CurrentBar), val, 
                VALCouleur, DashStyleHelper.Solid, EpaisseurLignes);
            Draw.Line(this, "POC", false, 0, poc, Math.Min(10, CurrentBar), poc, 
                POCCouleur, DashStyleHelper.Solid, EpaisseurLignes);

            // Dessiner le profile de volume
            foreach (var kvp in volumeProfile)
            {
                string barName = "VP_" + kvp.Key.ToString();
                Draw.Rectangle(this, barName, false, 0, kvp.Key, 
                    Math.Min((int)(kvp.Value / 100), 20), 
                    kvp.Key + TickSize,
                    Brushes.Gray.Clone(), Brushes.Gray.Clone(), Transparence);
            }

            DrawHistoricalLevels();
        }

        private void DrawHistoricalLevels()
        {
            if (historicalLevels == null) return;

            for (int i = 0; i < Math.Min(historicalLevels.Count, HistoriqueSessions); i++)
            {
                var level = historicalLevels[i];
                string suffix = "_H" + i;
                
                Draw.Line(this, "VAH" + suffix, false, 0, level.VAH, 
                    Math.Min(10, CurrentBar), level.VAH,
                    VAHCouleur.Clone(), DashStyleHelper.Dash, EpaisseurLignes);
                Draw.Line(this, "VAL" + suffix, false, 0, level.VAL, 
                    Math.Min(10, CurrentBar), level.VAL,
                    VALCouleur.Clone(), DashStyleHelper.Dash, EpaisseurLignes);
                Draw.Line(this, "POC" + suffix, false, 0, level.POC, 
                    Math.Min(10, CurrentBar), level.POC,
                    POCCouleur.Clone(), DashStyleHelper.Dash, EpaisseurLignes);
            }
        }

        #region Properties
        [NinjaScriptProperty]
        [Display(Name = "Période de calcul", Order = 1)]
        public PeriodeType PeriodeCalcul { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Nombre de sessions historiques", Order = 2)]
        public int HistoriqueSessions { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur VAH", Order = 3)]
        public Brush VAHCouleur { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur VAL", Order = 4)]
        public Brush VALCouleur { get; set; }

        [NinjaScriptProperty]
        [XmlIgnore]
        [Display(Name = "Couleur POC", Order = 5)]
        public Brush POCCouleur { get; set; }

        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name = "Épaisseur des lignes", Order = 6)]
        public int EpaisseurLignes { get; set; }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name = "Transparence", Order = 7)]
        public int Transparence { get; set; }
        #endregion

        public enum PeriodeType
        {
            Journalier,
            Hebdomadaire,
            Personnalise
        }
    }
}
