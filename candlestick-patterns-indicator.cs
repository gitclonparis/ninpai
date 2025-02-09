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
    public class CandlestickPatternArrows : Indicator
    {
        // Déclaration des variables
        private int trendStrength = 0; // Par défaut, aucune exigence de tendance

        // Paramètres utilisateur pour chaque motif haussier
        [NinjaScriptProperty]
        [Display(Name = "Bullish Belt Hold", Order = 1, GroupName = "Motifs Haussiers")]
        public bool BullishBeltHold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Bullish Engulfing", Order = 2, GroupName = "Motifs Haussiers")]
        public bool BullishEngulfing { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Bullish Harami", Order = 3, GroupName = "Motifs Haussiers")]
        public bool BullishHarami { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Bullish Harami Cross", Order = 4, GroupName = "Motifs Haussiers")]
        public bool BullishHaramiCross { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Hammer", Order = 5, GroupName = "Motifs Haussiers")]
        public bool Hammer { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Inverted Hammer", Order = 6, GroupName = "Motifs Haussiers")]
        public bool InvertedHammer { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Morning Star", Order = 7, GroupName = "Motifs Haussiers")]
        public bool MorningStar { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Piercing Line", Order = 8, GroupName = "Motifs Haussiers")]
        public bool PiercingLine { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Rising Three Methods", Order = 9, GroupName = "Motifs Haussiers")]
        public bool RisingThreeMethods { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Three White Soldiers", Order = 10, GroupName = "Motifs Haussiers")]
        public bool ThreeWhiteSoldiers { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Doji", Order = 11, GroupName = "Motifs Haussiers")]
		public bool Doji { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Stick Sandwich", Order = 12, GroupName = "Motifs Haussiers")]
		public bool StickSandwich { get; set; }

        // Paramètres utilisateur pour chaque motif baissier
        [NinjaScriptProperty]
        [Display(Name = "Bearish Belt Hold", Order = 1, GroupName = "Motifs Baissiers")]
        public bool BearishBeltHold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Bearish Engulfing", Order = 2, GroupName = "Motifs Baissiers")]
        public bool BearishEngulfing { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Bearish Harami", Order = 3, GroupName = "Motifs Baissiers")]
        public bool BearishHarami { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Bearish Harami Cross", Order = 4, GroupName = "Motifs Baissiers")]
        public bool BearishHaramiCross { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Dark Cloud Cover", Order = 5, GroupName = "Motifs Baissiers")]
        public bool DarkCloudCover { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Downside Tasuki Gap", Order = 6, GroupName = "Motifs Baissiers")]
        public bool DownsideTasukiGap { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Evening Star", Order = 7, GroupName = "Motifs Baissiers")]
        public bool EveningStar { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Falling Three Methods", Order = 8, GroupName = "Motifs Baissiers")]
        public bool FallingThreeMethods { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Hanging Man", Order = 9, GroupName = "Motifs Baissiers")]
        public bool HangingMan { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Shooting Star", Order = 10, GroupName = "Motifs Baissiers")]
        public bool ShootingStar { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Three Black Crows", Order = 11, GroupName = "Motifs Baissiers")]
        public bool ThreeBlackCrows { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Upside Gap Two Crows", Order = 12, GroupName = "Motifs Baissiers")]
        public bool UpsideGapTwoCrows { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Upside Tasuki Gap", Order = 13, GroupName = "Motifs Baissiers")]
        public bool UpsideTasukiGap { get; set; }

        // Paramètre pour la force de la tendance
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "Force de Tendance", Order = 0, GroupName = "Paramètres")]
        public int TrendStrength { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicateur qui détecte des motifs de chandeliers spécifiques et place des flèches sur le graphique.";
                Name = "CandlestickPatternArrows";
                IsOverlay = true;
                IsSuspendedWhileInactive = true;

                // Valeurs par défaut des propriétés
                TrendStrength = 0;

                // Motifs haussiers par défaut (désactivés)
                BullishBeltHold = false;
                BullishEngulfing = false;
                BullishHarami = false;
                BullishHaramiCross = false;
                Hammer = false;
                InvertedHammer = false;
                MorningStar = false;
                PiercingLine = false;
                RisingThreeMethods = false;
                ThreeWhiteSoldiers = false;
				Doji = false;
				StickSandwich = false;

                // Motifs baissiers par défaut (désactivés)
                BearishBeltHold = false;
                BearishEngulfing = false;
                BearishHarami = false;
                BearishHaramiCross = false;
                DarkCloudCover = false;
                DownsideTasukiGap = false;
                EveningStar = false;
                FallingThreeMethods = false;
                HangingMan = false;
                ShootingStar = false;
                ThreeBlackCrows = false;
                UpsideGapTwoCrows = false;
                UpsideTasukiGap = false;
            }
            else if (State == State.Configure)
            {
                // Configuration supplémentaire si nécessaire
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 20) // S'assurer qu'il y a suffisamment de barres pour la détection
                return;

            // Détection des motifs haussiers
            if (BullishBeltHold && CandlestickPattern(ChartPattern.BullishBeltHold, TrendStrength)[0] == 1)
            {
                Draw.ArrowUp(this, "BullishBeltHold" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
            }
            if (BullishEngulfing && CandlestickPattern(ChartPattern.BullishEngulfing, TrendStrength)[0] == 1)
            {
                Draw.ArrowUp(this, "BullishEngulfing" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
            }
            if (BullishHarami && CandlestickPattern(ChartPattern.BullishHarami, TrendStrength)[0] == 1)
            {
                Draw.ArrowUp(this, "BullishHarami" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
            }
            if (BullishHaramiCross && CandlestickPattern(ChartPattern.BullishHaramiCross, TrendStrength)[0] == 1)
            {
                Draw.ArrowUp(this, "BullishHaramiCross" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
            }
            if (Hammer && CandlestickPattern(ChartPattern.Hammer, TrendStrength)[0] == 1)
            {
                Draw.ArrowUp(this, "Hammer" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
            }
            if (InvertedHammer && CandlestickPattern(ChartPattern.InvertedHammer, TrendStrength)[0] == 1)
            {
                Draw.ArrowUp(this, "InvertedHammer" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
            }
            if (MorningStar && CandlestickPattern(ChartPattern.MorningStar, TrendStrength)[0] == 1)
            {
                Draw.ArrowUp(this, "MorningStar" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
            }
            if (PiercingLine && CandlestickPattern(ChartPattern.PiercingLine, TrendStrength)[0] == 1)
            {
                Draw.ArrowUp(this, "PiercingLine" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
            }
            if (RisingThreeMethods && CandlestickPattern(ChartPattern.RisingThreeMethods, TrendStrength)[0] == 1)
            {
                Draw.ArrowUp(this, "RisingThreeMethods" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
            }
            if (ThreeWhiteSoldiers && CandlestickPattern(ChartPattern.ThreeWhiteSoldiers, TrendStrength)[0] == 1)
            {
                Draw.ArrowUp(this, "ThreeWhiteSoldiers" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
            }
			
			if (Doji && CandlestickPattern(ChartPattern.Doji, TrendStrength)[0] == 1)
			{
				Draw.ArrowUp(this, "Doji" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
			}
			
			if (StickSandwich && CandlestickPattern(ChartPattern.StickSandwich, TrendStrength)[0] == 1)
			{
				Draw.ArrowUp(this, "StickSandwich" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
			}

            // Détection des motifs baissiers
            if (BearishBeltHold && CandlestickPattern(ChartPattern.BearishBeltHold, TrendStrength)[0] == 1)
            {
                Draw.ArrowDown(this, "BearishBeltHold" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
            }
            if (BearishEngulfing && CandlestickPattern(ChartPattern.BearishEngulfing, TrendStrength)[0] == 1)
            {
                Draw.ArrowDown(this, "BearishEngulfing" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
            }
            if (BearishHarami && CandlestickPattern(ChartPattern.BearishHarami, TrendStrength)[0] == 1)
            {
                Draw.ArrowDown(this, "BearishHarami" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
            }
            if (BearishHaramiCross && CandlestickPattern(ChartPattern.BearishHaramiCross, TrendStrength)[0] == 1)
            {
                Draw.ArrowDown(this, "BearishHaramiCross" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
            }
            if (DarkCloudCover && CandlestickPattern(ChartPattern.DarkCloudCover, TrendStrength)[0] == 1)
            {
                Draw.ArrowDown(this, "DarkCloudCover" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
            }
            if (DownsideTasukiGap && CandlestickPattern(ChartPattern.DownsideTasukiGap, TrendStrength)[0] == 1)
            {
                Draw.ArrowDown(this, "DownsideTasukiGap" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
            }
            if (EveningStar && CandlestickPattern(ChartPattern.EveningStar, TrendStrength)[0] == 1)
            {
                Draw.ArrowDown(this, "EveningStar" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
            }
            if (FallingThreeMethods && CandlestickPattern(ChartPattern.FallingThreeMethods, TrendStrength)[0] == 1)
            {
                Draw.ArrowDown(this, "FallingThreeMethods" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
            }
            if (HangingMan && CandlestickPattern(ChartPattern.HangingMan, TrendStrength)[0] == 1)
            {
                Draw.ArrowDown(this, "HangingMan" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
            }
            if (ShootingStar && CandlestickPattern(ChartPattern.ShootingStar, TrendStrength)[0] == 1)
            {
                Draw.ArrowDown(this, "ShootingStar" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
            }
            if (ThreeBlackCrows && CandlestickPattern(ChartPattern.ThreeBlackCrows, TrendStrength)[0] == 1)
            {
                Draw.ArrowDown(this, "ThreeBlackCrows" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
            }
            if (UpsideGapTwoCrows && CandlestickPattern(ChartPattern.UpsideGapTwoCrows, TrendStrength)[0] == 1)
            {
                Draw.ArrowDown(this, "UpsideGapTwoCrows" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
            }
            if (UpsideTasukiGap && CandlestickPattern(ChartPattern.UpsideTasukiGap, TrendStrength)[0] == 1)
            {
                Draw.ArrowDown(this, "UpsideTasukiGap" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
            }
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ninpai.CandlestickPatternArrows[] cacheCandlestickPatternArrows;
		public ninpai.CandlestickPatternArrows CandlestickPatternArrows(bool bullishBeltHold, bool bullishEngulfing, bool bullishHarami, bool bullishHaramiCross, bool hammer, bool invertedHammer, bool morningStar, bool piercingLine, bool risingThreeMethods, bool threeWhiteSoldiers, bool doji, bool stickSandwich, bool bearishBeltHold, bool bearishEngulfing, bool bearishHarami, bool bearishHaramiCross, bool darkCloudCover, bool downsideTasukiGap, bool eveningStar, bool fallingThreeMethods, bool hangingMan, bool shootingStar, bool threeBlackCrows, bool upsideGapTwoCrows, bool upsideTasukiGap, int trendStrength)
		{
			return CandlestickPatternArrows(Input, bullishBeltHold, bullishEngulfing, bullishHarami, bullishHaramiCross, hammer, invertedHammer, morningStar, piercingLine, risingThreeMethods, threeWhiteSoldiers, doji, stickSandwich, bearishBeltHold, bearishEngulfing, bearishHarami, bearishHaramiCross, darkCloudCover, downsideTasukiGap, eveningStar, fallingThreeMethods, hangingMan, shootingStar, threeBlackCrows, upsideGapTwoCrows, upsideTasukiGap, trendStrength);
		}

		public ninpai.CandlestickPatternArrows CandlestickPatternArrows(ISeries<double> input, bool bullishBeltHold, bool bullishEngulfing, bool bullishHarami, bool bullishHaramiCross, bool hammer, bool invertedHammer, bool morningStar, bool piercingLine, bool risingThreeMethods, bool threeWhiteSoldiers, bool doji, bool stickSandwich, bool bearishBeltHold, bool bearishEngulfing, bool bearishHarami, bool bearishHaramiCross, bool darkCloudCover, bool downsideTasukiGap, bool eveningStar, bool fallingThreeMethods, bool hangingMan, bool shootingStar, bool threeBlackCrows, bool upsideGapTwoCrows, bool upsideTasukiGap, int trendStrength)
		{
			if (cacheCandlestickPatternArrows != null)
				for (int idx = 0; idx < cacheCandlestickPatternArrows.Length; idx++)
					if (cacheCandlestickPatternArrows[idx] != null && cacheCandlestickPatternArrows[idx].BullishBeltHold == bullishBeltHold && cacheCandlestickPatternArrows[idx].BullishEngulfing == bullishEngulfing && cacheCandlestickPatternArrows[idx].BullishHarami == bullishHarami && cacheCandlestickPatternArrows[idx].BullishHaramiCross == bullishHaramiCross && cacheCandlestickPatternArrows[idx].Hammer == hammer && cacheCandlestickPatternArrows[idx].InvertedHammer == invertedHammer && cacheCandlestickPatternArrows[idx].MorningStar == morningStar && cacheCandlestickPatternArrows[idx].PiercingLine == piercingLine && cacheCandlestickPatternArrows[idx].RisingThreeMethods == risingThreeMethods && cacheCandlestickPatternArrows[idx].ThreeWhiteSoldiers == threeWhiteSoldiers && cacheCandlestickPatternArrows[idx].Doji == doji && cacheCandlestickPatternArrows[idx].StickSandwich == stickSandwich && cacheCandlestickPatternArrows[idx].BearishBeltHold == bearishBeltHold && cacheCandlestickPatternArrows[idx].BearishEngulfing == bearishEngulfing && cacheCandlestickPatternArrows[idx].BearishHarami == bearishHarami && cacheCandlestickPatternArrows[idx].BearishHaramiCross == bearishHaramiCross && cacheCandlestickPatternArrows[idx].DarkCloudCover == darkCloudCover && cacheCandlestickPatternArrows[idx].DownsideTasukiGap == downsideTasukiGap && cacheCandlestickPatternArrows[idx].EveningStar == eveningStar && cacheCandlestickPatternArrows[idx].FallingThreeMethods == fallingThreeMethods && cacheCandlestickPatternArrows[idx].HangingMan == hangingMan && cacheCandlestickPatternArrows[idx].ShootingStar == shootingStar && cacheCandlestickPatternArrows[idx].ThreeBlackCrows == threeBlackCrows && cacheCandlestickPatternArrows[idx].UpsideGapTwoCrows == upsideGapTwoCrows && cacheCandlestickPatternArrows[idx].UpsideTasukiGap == upsideTasukiGap && cacheCandlestickPatternArrows[idx].TrendStrength == trendStrength && cacheCandlestickPatternArrows[idx].EqualsInput(input))
						return cacheCandlestickPatternArrows[idx];
			return CacheIndicator<ninpai.CandlestickPatternArrows>(new ninpai.CandlestickPatternArrows(){ BullishBeltHold = bullishBeltHold, BullishEngulfing = bullishEngulfing, BullishHarami = bullishHarami, BullishHaramiCross = bullishHaramiCross, Hammer = hammer, InvertedHammer = invertedHammer, MorningStar = morningStar, PiercingLine = piercingLine, RisingThreeMethods = risingThreeMethods, ThreeWhiteSoldiers = threeWhiteSoldiers, Doji = doji, StickSandwich = stickSandwich, BearishBeltHold = bearishBeltHold, BearishEngulfing = bearishEngulfing, BearishHarami = bearishHarami, BearishHaramiCross = bearishHaramiCross, DarkCloudCover = darkCloudCover, DownsideTasukiGap = downsideTasukiGap, EveningStar = eveningStar, FallingThreeMethods = fallingThreeMethods, HangingMan = hangingMan, ShootingStar = shootingStar, ThreeBlackCrows = threeBlackCrows, UpsideGapTwoCrows = upsideGapTwoCrows, UpsideTasukiGap = upsideTasukiGap, TrendStrength = trendStrength }, input, ref cacheCandlestickPatternArrows);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ninpai.CandlestickPatternArrows CandlestickPatternArrows(bool bullishBeltHold, bool bullishEngulfing, bool bullishHarami, bool bullishHaramiCross, bool hammer, bool invertedHammer, bool morningStar, bool piercingLine, bool risingThreeMethods, bool threeWhiteSoldiers, bool doji, bool stickSandwich, bool bearishBeltHold, bool bearishEngulfing, bool bearishHarami, bool bearishHaramiCross, bool darkCloudCover, bool downsideTasukiGap, bool eveningStar, bool fallingThreeMethods, bool hangingMan, bool shootingStar, bool threeBlackCrows, bool upsideGapTwoCrows, bool upsideTasukiGap, int trendStrength)
		{
			return indicator.CandlestickPatternArrows(Input, bullishBeltHold, bullishEngulfing, bullishHarami, bullishHaramiCross, hammer, invertedHammer, morningStar, piercingLine, risingThreeMethods, threeWhiteSoldiers, doji, stickSandwich, bearishBeltHold, bearishEngulfing, bearishHarami, bearishHaramiCross, darkCloudCover, downsideTasukiGap, eveningStar, fallingThreeMethods, hangingMan, shootingStar, threeBlackCrows, upsideGapTwoCrows, upsideTasukiGap, trendStrength);
		}

		public Indicators.ninpai.CandlestickPatternArrows CandlestickPatternArrows(ISeries<double> input , bool bullishBeltHold, bool bullishEngulfing, bool bullishHarami, bool bullishHaramiCross, bool hammer, bool invertedHammer, bool morningStar, bool piercingLine, bool risingThreeMethods, bool threeWhiteSoldiers, bool doji, bool stickSandwich, bool bearishBeltHold, bool bearishEngulfing, bool bearishHarami, bool bearishHaramiCross, bool darkCloudCover, bool downsideTasukiGap, bool eveningStar, bool fallingThreeMethods, bool hangingMan, bool shootingStar, bool threeBlackCrows, bool upsideGapTwoCrows, bool upsideTasukiGap, int trendStrength)
		{
			return indicator.CandlestickPatternArrows(input, bullishBeltHold, bullishEngulfing, bullishHarami, bullishHaramiCross, hammer, invertedHammer, morningStar, piercingLine, risingThreeMethods, threeWhiteSoldiers, doji, stickSandwich, bearishBeltHold, bearishEngulfing, bearishHarami, bearishHaramiCross, darkCloudCover, downsideTasukiGap, eveningStar, fallingThreeMethods, hangingMan, shootingStar, threeBlackCrows, upsideGapTwoCrows, upsideTasukiGap, trendStrength);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ninpai.CandlestickPatternArrows CandlestickPatternArrows(bool bullishBeltHold, bool bullishEngulfing, bool bullishHarami, bool bullishHaramiCross, bool hammer, bool invertedHammer, bool morningStar, bool piercingLine, bool risingThreeMethods, bool threeWhiteSoldiers, bool doji, bool stickSandwich, bool bearishBeltHold, bool bearishEngulfing, bool bearishHarami, bool bearishHaramiCross, bool darkCloudCover, bool downsideTasukiGap, bool eveningStar, bool fallingThreeMethods, bool hangingMan, bool shootingStar, bool threeBlackCrows, bool upsideGapTwoCrows, bool upsideTasukiGap, int trendStrength)
		{
			return indicator.CandlestickPatternArrows(Input, bullishBeltHold, bullishEngulfing, bullishHarami, bullishHaramiCross, hammer, invertedHammer, morningStar, piercingLine, risingThreeMethods, threeWhiteSoldiers, doji, stickSandwich, bearishBeltHold, bearishEngulfing, bearishHarami, bearishHaramiCross, darkCloudCover, downsideTasukiGap, eveningStar, fallingThreeMethods, hangingMan, shootingStar, threeBlackCrows, upsideGapTwoCrows, upsideTasukiGap, trendStrength);
		}

		public Indicators.ninpai.CandlestickPatternArrows CandlestickPatternArrows(ISeries<double> input , bool bullishBeltHold, bool bullishEngulfing, bool bullishHarami, bool bullishHaramiCross, bool hammer, bool invertedHammer, bool morningStar, bool piercingLine, bool risingThreeMethods, bool threeWhiteSoldiers, bool doji, bool stickSandwich, bool bearishBeltHold, bool bearishEngulfing, bool bearishHarami, bool bearishHaramiCross, bool darkCloudCover, bool downsideTasukiGap, bool eveningStar, bool fallingThreeMethods, bool hangingMan, bool shootingStar, bool threeBlackCrows, bool upsideGapTwoCrows, bool upsideTasukiGap, int trendStrength)
		{
			return indicator.CandlestickPatternArrows(input, bullishBeltHold, bullishEngulfing, bullishHarami, bullishHaramiCross, hammer, invertedHammer, morningStar, piercingLine, risingThreeMethods, threeWhiteSoldiers, doji, stickSandwich, bearishBeltHold, bearishEngulfing, bearishHarami, bearishHaramiCross, darkCloudCover, downsideTasukiGap, eveningStar, fallingThreeMethods, hangingMan, shootingStar, threeBlackCrows, upsideGapTwoCrows, upsideTasukiGap, trendStrength);
		}
	}
}

#endregion
