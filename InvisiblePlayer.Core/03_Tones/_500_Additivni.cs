using InvisiblePlayer.Core.Generators;

namespace InvisiblePlayer.Core.Tones
{
    /// <summary>
    /// PROVIZORNÍ klavír na aditivní (alikvotové) syntéze - viz
    /// AdditiveStringVoice.cs. Žádný Karplus-Strong, jen sinusovky. Poloha
    /// úderu kladívka ~0.12 délky struny (typické pro klavír), amplitudy a
    /// rychlosti doznívání partiálů spočtené z tohohle úderu tak, aby vyšší
    /// partiály byly tišší A doznívaly rychleji - stejná fyzika, jaká by
    /// nakonec měla platit i pro Karplus-Strong verzi.
    /// </summary>
    public static class _500_Piano_Additivni
    {
        public static VoicePreset Preset => new VoicePreset
        {
            Name = "Piano (aditivní, provizorní)",
            Number = 500,
            Instrument = InstrumentType.AdditiveString,

            // Pole StringBrightness se tu zneužívá jako neharmonicita B
            // (viz komentář v AdditiveStringVoice) - 0.0003 je typická
            // hodnota pro klavírní strunu ve středních polohách.
            StringBrightness = 0.0003,

            // Krátký úder kladívka (chiff) - stejná pole jako u KS klavíru.
            ChiffFilterFreqHz = 450.0,
            ChiffFilterQ = 1.2,
            ExcitationNoiseAmount = 0.30,

            PartialRatios = new[] { 0.3359, 0.6641, 1.0000, 1.3359, 1.4531, 1.4922, 1.6641, 3.3359, 3.6641, 4.0000, 4.6641, 5.6719 },
            PartialAmplitudes = new[] { 0.8334, 0.1658, 1.0000, 0.2770, 0.0196, 0.0234, 0.0689, 0.0245, 0.0323, 0.0420, 0.0211, 0.0207 },
            PartialDecayRates = new double[] { 0.77, 1.34, 1.85, 2.33, 2.78, 3.22, 3.64, 4.05 }
        };
    }

    /// <summary>
    /// PROVIZORNÍ cembalo na aditivní syntéze. Brnknutí brčkem blízko kraje
    /// struny (~0.06 délky) => mnohem bohatší, jasnější/"kovovější" spektrum
    /// než klavír (vyšší partiály tišší jen mírně, ne prudce), kratší
    /// celkový dozvuk, menší neharmonicita (tenčí nevinuté struny).
    /// </summary>
    public static class _501_Cembalo_Additivni
    {
        public static VoicePreset Preset => new VoicePreset
        {
            Name = "Cembalo (aditivní, provizorní)",
            Number = 501,
            Instrument = InstrumentType.AdditiveString,

            StringBrightness = 0.00015, // neharmonicita B - menší než klavír

            ChiffFilterFreqHz = 3800.0, // brnknutí brčkem - ostřejší, výš než kladívko
            ChiffFilterQ = 2.0,
            ExcitationNoiseAmount = 0.15,

            PartialRatios = new double[] { 1, 2, 3, 4, 5, 6, 7, 8 },
            PartialAmplitudes = new double[]
            {
                0.40, 0.196, 0.127, 0.091, 0.069, 0.054, 0.042, 0.033
            },
            PartialDecayRates = new double[]
            {
                1.97, 2.99, 3.82, 4.54, 5.18, 5.78, 6.34, 6.87
            }
        };
    }
}
