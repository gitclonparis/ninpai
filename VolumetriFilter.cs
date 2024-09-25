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
    public class VolumetriFilter : Indicator
    {
        #region Variables
        private Brush upArrowColor;
        private Brush downArrowColor;
        #endregion

        #region Properties
        // Paramètres pour la flèche UP
        [NinjaScriptProperty]
        [Display(Name = "Activer la condition UP", Order = 1, GroupName = "Paramètres UP")]
        public bool UpConditionEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Seuil inférieur UP (BarDelta)", Order = 2, GroupName = "Paramètres UP")]
        public double UpLowerThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Seuil supérieur UP (BarDelta)", Order = 3, GroupName = "Paramètres UP")]
        public double UpUpperThreshold { get; set; }

        // Paramètres pour la flèche DOWN
        [NinjaScriptProperty]
        [Display(Name = "Activer la condition DOWN", Order = 1, GroupName = "Paramètres DOWN")]
        public bool DownConditionEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Seuil inférieur DOWN (BarDelta)", Order = 2, GroupName = "Paramètres DOWN")]
        public double DownLowerThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Seuil supérieur DOWN (BarDelta)", Order = 3, GroupName = "Paramètres DOWN")]
        public double DownUpperThreshold { get; set; }

        // Couleurs des flèches
        [XmlIgnore]
        [Display(Name = "Couleur flèche UP", Order = 1, GroupName = "Couleurs")]
        public Brush UpArrowColor
        {
            get { return upArrowColor; }
            set { upArrowColor = value; }
        }

        [Browsable(false)]
        public string UpArrowColorSerializable
        {
            get { return Serialize.BrushToString(upArrowColor); }
            set { upArrowColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Couleur flèche DOWN", Order = 2, GroupName = "Couleurs")]
        public Brush DownArrowColor
        {
            get { return downArrowColor; }
            set { downArrowColor = value; }
        }

        [Browsable(false)]
        public string DownArrowColorSerializable
        {
            get { return Serialize.BrushToString(downArrowColor); }
            set { downArrowColor = Serialize.StringToBrush(value); }
        }
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                                    = "Indicateur personnalisé qui affiche des flèches en fonction du BarDelta dans une plage définie.";
                Name                                           = "VolumetriFilter";
                Calculate                                      = Calculate.OnBarClose;
                IsOverlay                                      = true;
                DisplayInDataBox                               = false;
                PaintPriceMarkers                              = false;
                IsSuspendedWhileInactive                       = true;

                // Paramètres par défaut pour UP
                UpConditionEnabled                             = true;
                UpLowerThreshold                               = 500;
                UpUpperThreshold                               = 2000;

                // Paramètres par défaut pour DOWN
                DownConditionEnabled                           = true;
                DownLowerThreshold                             = -2000;
                DownUpperThreshold                             = -500;

                // Couleurs par défaut
                UpArrowColor                                   = Brushes.Green;
                DownArrowColor                                 = Brushes.Red;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1)
                return;

            // Vérifier si le type de barres est Volumetric
            if (!(Bars.BarsSeries.BarsType is NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType))
                return;

            var barsType = Bars.BarsSeries.BarsType as NinjaTrader.NinjaScript.BarsTypes.VolumetricBarsType;
            if (barsType == null)
                return;

            var barDelta = barsType.Volumes[CurrentBar].BarDelta;

            // Condition pour flèche UP
            if (UpConditionEnabled)
            {
                if (barDelta > UpLowerThreshold && barDelta < UpUpperThreshold)
                {
                    Draw.ArrowUp(this, "UpArrow" + CurrentBar, false, 0, Low[0] - TickSize, UpArrowColor);
                }
            }

            // Condition pour flèche DOWN
            if (DownConditionEnabled)
            {
                if (barDelta > DownLowerThreshold && barDelta < DownUpperThreshold)
                {
                    Draw.ArrowDown(this, "DownArrow" + CurrentBar, false, 0, High[0] + TickSize, DownArrowColor);
                }
            }
        }
    }
}