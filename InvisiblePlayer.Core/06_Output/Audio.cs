using System;
using System.Collections.Generic;
using InvisiblePlayer.Core.Generators;
using InvisiblePlayer.Core.ToneEngine;
using NAudio.Wave;

namespace InvisiblePlayer.Core.Output

{
    public class AudioEngine : IWaveProvider, IDisposable
    {
        private WaveOutEvent? _waveOut;
        private readonly InvisiblePlayer.Core.ToneEngine.ToneEngine _synth;

        // Špičkové úrovně od posledního čtení - zvlášť pro každé ze tří pásem
        // (hloubky/středy/výšky), navíc jedna společná pro dnešní sloučený
        // mono výstup. Stejný princip jako MaxLeftPeak/MaxRightPeak v AudioPlayer.cs.
        private float _peakSinceLastRead;
        private float _peakBass;
        private float _peakMid;
        private float _peakTreble;

        public WaveFormat WaveFormat { get; }

        public AudioEngine(InvisiblePlayer.Core.ToneEngine.ToneEngine synth, int sampleRate = 44100)
        {
            _synth = synth;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1); // 44.1 kHz, Mono, 32-bit Float
        }

        /// <summary>
        /// Spustí audio výstup na zvukovou kartu.
        /// </summary>
        public void Start()
        {
            if (_waveOut != null) return;

            _waveOut = new WaveOutEvent
            {
                DesiredLatency = 100, // Nízká latence (50 ms) pro živé hraní bez zpoždění
                NumberOfBuffers = 4
            };

            _waveOut.Init(this);
            _waveOut.Play();
        }

        /// <summary>
        /// Zastaví audio výstup.
        /// </summary>
        public void Stop()
        {
            if (_waveOut != null)
            {
                _waveOut.Stop();
                _waveOut.Dispose();
                _waveOut = null;
            }
        }

        /// <summary>
        /// Callback od NAudio – plníme zvukový buffer vzorky z našeho PolySynthu.
        /// ToneEngine teď vrací rovnou tři hotová pásma (hloubky/středy/výšky) -
        /// každý hlas si je vyrábí sám (viz SynthVoice.CalculateWaveform), takže
        /// se tu už nic dodatečně nefiltruje. Dokud nemáme HW se třemi výstupy
        /// (Raspberry Pi + HiFiBerry DAC8x), pásma se tu jen sečtou zpátky do
        /// jednoho mono signálu pro současnou PC zvukovku.
        /// </summary>
        public int Read(byte[] buffer, int offset, int count)
        {
            int sampleCount = count / 4;
            var waveBuffer = new WaveBuffer(buffer);

            for (int i = 0; i < sampleCount; i++)
            {
                BandSample bands = _synth.GenerateNextBandSample();

                TrackPeak(ref _peakBass, bands.Bass);
                TrackPeak(ref _peakMid, bands.Mid);
                TrackPeak(ref _peakTreble, bands.Treble);

                float sample = (float)(bands.Bass + bands.Mid + bands.Treble);

                TrackPeak(ref _peakSinceLastRead, sample);

                waveBuffer.FloatBuffer[offset / 4 + i] = sample;
            }

            return count;
        }

        private static void TrackPeak(ref float peakStore, double sample)
        {
            float abs = (float)Math.Abs(sample);
            if (abs > peakStore) peakStore = abs;
        }

        /// <summary>
        /// Vrátí nejvyšší absolutní úroveň vzorku od posledního volání a vynuluje počítadlo.
        /// Určeno pro VU metr (stejný princip jako AudioPlayer.ReadPeakLevels() u MP3 přehrávače).
        /// Toto je úroveň CELKOVÉHO sloučeného signálu - pro jednotlivá pásma
        /// použij ReadBandPeaks().
        /// </summary>
        public float ReadPeak()
        {
            float peak = _peakSinceLastRead;
            _peakSinceLastRead = 0f;
            return peak;
        }

        /// <summary>
        /// Vrátí špičkové úrovně všech tří kmitočtových pásem (hloubky/středy/výšky)
        /// od posledního volání a vynuluje je. Určeno pro tři samostatné VU metry -
        /// volej ve stejném rytmu jako ReadPeak(), např. z UI časovače.
        /// </summary>
        public (float Bass, float Mid, float Treble) ReadBandPeaks()
        {
            var result = (_peakBass, _peakMid, _peakTreble);
            _peakBass = 0f;
            _peakMid = 0f;
            _peakTreble = 0f;
            return result;
        }

        /// <summary>Dělící kmitočet hloubky/středy v Hz (zatím pevný, viz SynthVoice).</summary>
        public double LowCrossoverHz => SynthVoice.LowCrossoverHz;

        /// <summary>Dělící kmitočet středy/výšky v Hz (zatím pevný, viz SynthVoice).</summary>
        public double HighCrossoverHz => SynthVoice.HighCrossoverHz;

        /// <summary>
        /// True, pokud poslední vygenerovaný vzorek v NĚKTERÉM z pásem přesáhl
        /// rozsah -1.0..1.0 (tedy došlo k oříznutí). Průchozí hodnota ze ToneEngine.
        /// </summary>
        public bool ClipDetected => _synth.ClipDetected;

        // --- Průchozí ovládání rejstříků (pro VGA panel / budoucí UI) ---

        /// <summary>Přepne rejstřík ON/OFF podle čísla. Vrací nový stav (true = ON).</summary>
        public bool ToggleRegister(int number) => _synth.ToggleRegister(number);

        public bool IsRegisterActive(int number) => _synth.IsRegisterActive(number);

        public IReadOnlyCollection<int> ActiveRegisters => _synth.ActiveRegisters;

        public void Dispose()
        {
            Stop();
        }
    }
}
