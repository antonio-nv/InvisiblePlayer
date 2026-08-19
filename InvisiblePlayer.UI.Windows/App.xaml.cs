using InvisiblePlayer.Core;            // Pro AudioEngine
using InvisiblePlayer.Core.Generators;
using InvisiblePlayer.Core.Input;      // Pro InputManager
using InvisiblePlayer.Core.Output;
using InvisiblePlayer.Core.ToneEngine; // TONEENGINE
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace InvisiblePlayer.UI.Windows
{
    public static class MediaLauncher
    {
        // Seznam přípon, které považujeme za video
        private static readonly string[] VideoExtensions = new[]
        {
            ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v"
        };

        public static void Launch(string filePath, InputManager inputManager)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            if (VideoExtensions.Contains(ext))
            {
                // VIDEO -> Spustíme WPF okno s LibVLC
                Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

                var window = new MainWindow();
                Application.Current.MainWindow = window;
                window.Show();
                window.PlayFile(filePath);
            }
            else if (ext == ".mid" || ext == ".midi")
            {
                // MIDI -> Spustíme přehrávání souboru přes náš nový InputManager v Core
                _ = inputManager.PlayMidiFileAsync(filePath);

                // Spustíme VGA konzoli pro vizualizaci
                VgaEngine.Run(filePath);
            }
            else
            {
                // AUDIO / OSTATNÍ -> Spustíme původní VGA konzoli
                VgaEngine.Run(filePath);

                // Po skončení konzole aplikaci ukončíme
                Application.Current.Shutdown();
            }
        }
    }

    public partial class App : Application
    {
        private InputManager? _inputManager;
        private ToneEngine? _toneEngine;
        private AudioEngine? _audioEngine;

        // Statická reference, aby k aktuálnímu AudioEngine (varhany) mohly
        // přistoupit i jiné statické třídy jako VgaEngine (pro VU metr).
        public static AudioEngine? OrganEngine { get; private set; }


        protected override void OnStartup(StartupEventArgs e)
        {
            // --- KROK 0: UKONČENÍ PŘEDCHOZÍCH INSTANCÍ PROHLÍŽEČE ---
            KillPreviousInstances();

            base.OnStartup(e);

            // 1. INICIALIZACE NOVÉHO TONEENGINE (Rejstříky / Varhany)
            _toneEngine = new ToneEngine();

            _audioEngine = new AudioEngine(_toneEngine);
            _audioEngine.Start();

            OrganEngine = _audioEngine;

            // 2. INICIALIZACE CORE INPUTU (Živé piano z USB / Casio)
            _inputManager = new InputManager();

            _inputManager.OnInputEvent += evt =>
            {
                if (evt.Type == InputEventType.NoteOn && evt.Velocity > 0)
                {
                    _toneEngine?.NoteOn(evt.Note.Number);
                }
                else
                {
                    _toneEngine?.NoteOff(evt.Note.Number);
                }

                Debug.WriteLine($"[{evt.Source}] {evt.Type} | Nota: {evt.Note.Number} ({evt.Note.FrequencyHz:F1} Hz)");
            };

            _inputManager.StartLiveDevice("USB MIDI");

            // 3. NAČTENÍ NASTAVENÍ A KONTROLA ARGUMENTŮ
            AppSettings settings = AppSettings.Load();
            string? filePathToPlay = null;

            if (e.Args.Length == 0)
            {
                // Spuštěno bez parametrů (F5 / Debug / Start) -> načteme naposledy přehrávaný
                filePathToPlay = settings.LastFilePath;
            }
            else
            {
                // Spuštěno s parametrem -> načteme cestu ze souboru
                string rawPath = string.Join(" ", e.Args).Trim('"');
                try { filePathToPlay = Path.GetFullPath(rawPath); }
                catch { filePathToPlay = rawPath; }
            }

            // 4. SPUŠTĚNÍ PŘEHRÁVÁNÍ NEBO VGA KONZOLE
            if (!string.IsNullOrEmpty(filePathToPlay) && File.Exists(filePathToPlay))
            {
                settings.LastFilePath = filePathToPlay;
                settings.LastFolderPath = Path.GetDirectoryName(filePathToPlay);
                settings.Save();

                MediaLauncher.Launch(filePathToPlay, _inputManager);
            }
            else
            {
                // Pokud soubor neexistuje, otevře se konzole naprázdno pro hraní na piano
                VgaEngine.Run("");
                Shutdown();
            }
        }


        private void KillPreviousInstances()
        {
            try
            {
                Process currentProcess = Process.GetCurrentProcess();

                var previousProcesses = Process.GetProcessesByName(currentProcess.ProcessName)
                                               .Where(p => p.Id != currentProcess.Id);

                foreach (var process in previousProcesses)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(1000);
                    }
                    catch { /* ignorujeme chybové stavy pri zavírání */ }
                }
            }
            catch { /* ignorujeme chyby přístupu */ }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Úklid zdrojů při vypnutí
            _inputManager?.Dispose();
            _audioEngine?.Stop();
            _audioEngine?.Dispose();

            base.OnExit(e);
        }
    }
}