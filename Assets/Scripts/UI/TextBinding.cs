using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Match3Tray.Binding;
using Match3Tray.Logging;
using TMPro;
using UnityEngine;

namespace Match3Tray.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class TextBinding : UIBinding
    {
        /// <summary>
        ///     German culture info used for number formatting with appropriate decimal and thousand separators.
        /// </summary>
        private static readonly CultureInfo sCult = CultureInfo.GetCultureInfo("de-DE");

        /// <summary>
        ///     The delimiter used to separate multiple values, or a composite format string.
        /// </summary>
        [Tooltip("Delimiter or composite format (use {0}, {1}, etc)")] [SerializeField]
        private string Delimiter = ", ";

        /// <summary>
        ///     When true, enables automatic number formatting with K/M/B suffixes for large numbers.
        /// </summary>
        public bool IsFormatted;

        /// <summary>
        ///     Indicates whether the delimiter is a composite format string.
        /// </summary>
        private bool _delimiterIsComposite;

        /// <summary>
        ///     Reference to the TextMeshProUGUI component that will display the bound values.
        /// </summary>
        private TextMeshProUGUI _txt;

        /// <summary>
        ///     Initializes the TextBinder by getting a reference to the TextMeshProUGUI component.
        /// </summary>
        private void Awake()
        {
            _txt = GetComponent<TextMeshProUGUI>();
        }

        /// <summary>
        ///     Initializes the format string segments for better performance.
        /// </summary>
        public override void Start()
        {
            _delimiterIsComposite = !string.IsNullOrEmpty(Delimiter) && Delimiter.Contains("{") && Delimiter.Contains("}");
            base.Start();
        }

        /// <summary>
        ///     Updates the text display based on the bound values.
        ///     Handles formatting, number scaling, and different display modes based on the configuration.
        /// </summary>
        /// <param name="count">Number of values in the update</param>
        /// <param name="values">Array of values to display</param>
        protected override void OnBindingUpdated(int count, object[] values)
        {
            if (count == 0)
            {
                _txt.text = string.Empty;
                return;
            }

            if (IsFormatted)
                for (var i = 0; i < count; i++)
                    if (values[i] is IConvertible convertible)
                        values[i] = Format(Convert.ToDouble(convertible));

            try
            {
                if (_delimiterIsComposite)
                {
                    _txt.text = string.Format(Delimiter, values);
                }
                else if (count == 1)
                {
                    _txt.text = values[0]?.ToString() ?? string.Empty;
                }
                else
                {
                    var stringValues = new string[count];
                    for (var i = 0; i < count; i++) stringValues[i] = values[i]?.ToString() ?? string.Empty;

                    _txt.text = string.Join(Delimiter, stringValues);
                }
            }
            catch (Exception ex)
            {
                LoggerExtra.LogError($"[{name}] TextBinder: Format error - {ex.Message}");
                _txt.text = string.Join(" / ", values);
            }
        }

        /// <summary>
        ///     Formats a number with appropriate suffixes (K, M, B).
        ///     Optimized with aggressive inlining and minimal branching.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string Format(double amt)
        {
            return amt < 1000 ? amt.ToString("N0", sCult) :
                amt < 1_000_000 ? (amt / 1000).ToString("N0", sCult) + "K" :
                amt < 1_000_000_000 ? (amt / 1_000_000).ToString("N0", sCult) + "M" :
                (amt / 1_000_000_000).ToString("N0", sCult) + "B";
        }
    }
}