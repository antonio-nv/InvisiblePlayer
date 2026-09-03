using System;

namespace PS150.Core.Generators
{
    /// <summary>
    /// Jeden vzorek rozdělený do tří kmitočtových pásem - hloubky / středy / výšky.
    /// Nahrazuje dřívější jediné číslo (double) vraceného vzorku: místo aby se
    /// nástroje namixovaly do jednoho signálu a ten se pak dodatečně filtroval,
    /// každý hlas rovnou VYRÁBÍ svůj zvuk zvlášť pro každé pásmo. Tři pásma pak
    /// putují (zatím) na jednu PC zvukovku sečtená dohromady, v budoucnu každé
    /// na svůj vlastní D/A převodník (Raspberry Pi + HiFiBerry DAC8x).
    /// </summary>
    public struct BandSample
    {
        public double Bass;
        public double Mid;
        public double Treble;

        public static BandSample operator +(BandSample a, BandSample b) => new BandSample
        {
            Bass = a.Bass + b.Bass,
            Mid = a.Mid + b.Mid,
            Treble = a.Treble + b.Treble
        };

        public static BandSample operator *(BandSample a, double scalar) => new BandSample
        {
            Bass = a.Bass * scalar,
            Mid = a.Mid * scalar,
            Treble = a.Treble * scalar
        };
    }

    public abstract class SynthVoice
    {
        protected readonly double SampleRate;
        protected readonly AdsrEnvelope NoteEnvelope = new AdsrEnvelope();
        protected bool HasStarted = false;

        // Dělící kmitočty tříspásmové výhybky. Zatím napevno (viz zadání) -
        // 500 Hz mezi hloubkami/středy a 2000 Hz mezi středy/výškami. Sdílené
        // jedno místo pro VŠECHNY nástroje i pro ToneEngine/AudioEngine, aby
        // všude platilo stejné dělení.
        public const double LowCrossoverHz = 500.0;
        public const double HighCrossoverHz = 2000.0;

        // Konec tónu: Až po stisku (HasStarted) a doznění obálky (Idle).
        // VIRTUAL - nástroje s vlastním fyzikálním modelem dozvuku (Bell, Cembalo)
        // si tohle mohou rozšířit o vlastní podmínku (pokles pod práh v dB),
        // místo aby konec tónu určovala jen sdílená ADSR obálka.
        public virtual bool IsFinished => HasStarted && !NoteEnvelope.IsActive;

        protected SynthVoice(double sampleRate)
        {
            SampleRate = sampleRate;
        }

        public virtual void NoteOn()
        {
            HasStarted = true;
            NoteEnvelope.TriggerGate(true);
        }

        public virtual void NoteOff()
        {
            NoteEnvelope.TriggerGate(false);
        }

        /// <summary>
        /// Pomocná jednotná metoda pro posun fáze oscilátoru (0.0 až 1.0)
        /// </summary>
        protected double AdvancePhase(ref double phase, double frequencyMult, double baseFreq)
        {
            phase += (baseFreq * frequencyMult) / SampleRate;
            if (phase >= 1.0) phase -= Math.Floor(phase);
            return phase;
        }

        /// <summary>
        /// Zařadí jednu HODNOTU ZE ZNÁMÉHO KMITOČTU (typicky jedna sinusová
        /// harmonická/partiál) do správného pásma podle LowCrossoverHz/HighCrossoverHz.
        /// Funguje jen pro čisté tóny s přesně daným kmitočtem - u čistého sinu
        /// není žádné "rozostření" spektra, takže netřeba filtrovat, stačí
        /// rozhodnout a přičíst. Pro širokopásmový obsah (šum) tohle NEPOUŽÍVAT -
        /// tam je potřeba doopravdy filtrovat (viz ThreeBandCrossover ve Filters).
        /// </summary>
        protected static void AddToBand(ref BandSample bands, double frequencyHz, double value)
        {
            if (frequencyHz < LowCrossoverHz)
                bands.Bass += value;
            else if (frequencyHz < HighCrossoverHz)
                bands.Mid += value;
            else
                bands.Treble += value;
        }

        /// <summary>
        /// Jednotné generování vzorku pro libovolný nástroj - vrací tři pásma
        /// najednou (viz BandSample).
        /// </summary>
        public BandSample GenerateSample(double frequency)
        {
            // Obálka se spočítá automaticky pro VŠECHNY nástroje stejně!
            double envelopeValue = NoteEnvelope.Process((int)SampleRate);

            // Pokud tón nehraje nebo dozněl, šetříme procesor
            if (envelopeValue <= 0.00001 && HasStarted) return default;

            // Zavolá specifický tvar vlny pro daný nástroj (už rozdělený do pásem)
            BandSample rawWave = CalculateWaveform(frequency);

            return rawWave * envelopeValue;
        }

        /// <summary>
        /// Každý nástroj zde definuje POUZE svůj jedinečný tvar vlny/zvuku,
        /// rozdělený do tří pásem (hloubky/středy/výšky).
        /// </summary>
        protected abstract BandSample CalculateWaveform(double frequency);
    }
}
