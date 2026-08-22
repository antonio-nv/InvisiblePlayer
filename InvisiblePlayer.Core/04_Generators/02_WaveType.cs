namespace InvisiblePlayer.Core.Generators
{
    public enum WaveType
    {
        Sine,       // Čistý sinus (základní tón)
        Sawtooth,   // Pila (bohatá na sudé i liché harmonické - smyčce, žestě)
        Square,     // Čtverec (liché harmonické - klarinet, 8-bit zvuky)
        Triangle,   // Trojúhelník (jemný tón, flétna)
        WhiteNoise  // Bílý šum (perkuse, fuk varhan)
    }

    // Který "zvukový engine" má preset použít. Výchozí je Organ, takže všechny
    // dosavadní presety (Bombard, Piano...) zůstávají beze změny funkční.
    public enum InstrumentType
    {
        Organ,
        Piano,
        Cembalo,
        Bell
    }

    public class VoicePreset
    {
        public string Name { get; set; } = "Default";

        // Číslo rejstříku odpovídající fyzickému číslování na hracím stole
        // (viz štítky na klapkách skutečného nástroje, např. "68 - Dolce 8'").
        // 0 = zatím nepřiřazeno.
        public int Number { get; set; } = 0;

        // Který nástroj/engine preset používá.
        public InstrumentType Instrument { get; set; } = InstrumentType.Organ;

        // Tabulka alikvót: (poměr frekvence, hlasitost). Používá OrganVoice,
        // a volitelně i PianoVoice/CembaloVoice (viz jejich preset-konstruktor)
        // pro přepsání výchozí barvy zvuku.
        public (double FrequencyMultiplier, double Amplitude)[] Harmonics { get; set; }

        // Parametry Šumu / Chiffu / Úderu (Organ)
        public double ChiffNoiseGain { get; set; } = 0.2;
        public double ChiffFilterFreqHz { get; set; } = 800.0;
        public double ChiffFilterQ { get; set; } = 1.0;
        public double ChiffDurationSec { get; set; } = 0.030; // 30ms

        // Modulace (Vibrato / Tremolo)
        public ModulationType ModType { get; set; } = ModulationType.None;
        public double ModSpeedHz { get; set; } = 5.5;
        public double ModDepth { get; set; } = 0.08;

        // Volitelné přepsání ADSR obálky (útok/pokles/sustain/dozvuk) - funguje
        // pro JAKÝKOLIV Instrument (Organ/Piano/Cembalo/Bell). Když necháš null,
        // každý hlas použije své vestavěné výchozí hodnoty (jako dosud).
        public AdsrEnvelope Envelope { get; set; } = null;

        // --- Parametry pro Instrument == Piano/Cembalo (fyzikální model struny,
        // Karplus-Strong - viz KarplusStrongString.cs). Organ/Bell tato pole
        // nepoužívají. ---

        // Za kolik sekund doznívá ZÁKLADNÍ tón o -60 dB. Vyšší harmonické
        // doznívají samy rychleji, netřeba nastavovat zvlášť.
        public double StringDecaySeconds { get; set; } = 8.0;

        // 0.0-0.98: síla dolní propusti ve zpožďovací smyčce struny.
        // Nižší = jasnější/ostřejší zvuk (cembalo), vyšší = tmavší/plnější (klavír).
        public double StringBrightness { get; set; } = 0.5;

        // 0.02-0.5: poloha úderu/drnknutí jako podíl délky struny od kraje
        // (viz KarplusStrongString.Excite). Blíž kraji = jasnější/"kovovější"
        // tón (cembalo), blíž středu (0.5) = měkčí, temnější tón (klavír).
        public double PickPosition { get; set; } = 0.125;

        // 0.0-1.0: kolik šumu se přimíchá k deterministickému tvaru výchylky
        // (textura úderu/drnknutí). Základní tón vždy nese ten deterministický
        // tvar, ne šum - tohle je jen na "dech"/špínu zvuku navrch.
        public double ExcitationNoiseAmount { get; set; } = 0.15;

        // --- Partiály pro Instrument == Bell (volitelné) ---
        // Pokud necháš null, BellVoice použije svůj vestavěný výchozí model.
        // Všechny tři pole musí mít stejnou délku (počet partiálů).
        public double[] PartialRatios { get; set; } = null;
        public double[] PartialAmplitudes { get; set; } = null;
        public double[] PartialDecayRates { get; set; } = null;
    }

    public enum ModulationType
    {
        None,
        AM, // Amplitudová modulace (Tremolo)
        FM  // Frekvenční modulace (Vibrato)
    }

}
