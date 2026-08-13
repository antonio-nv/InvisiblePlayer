namespace InvisiblePlayer.Core
{
    public static class AudioMeter
    {
        /// <summary>
        /// Calculates logarithmic dB level from linear sample amplitude.
        /// Range: -120 dB to 0 dB.
        /// </summary>
        public static double LinearToDecibels(float peak)
        {
            if (peak <= 0.000001f) return -120.0; // Dynamic range floor

            double db = 20.0 * Math.Log10(peak);
            return Math.Max(-120.0, Math.Min(0.0, db));
        }

        /// <summary>
        /// Generates an ASCII meter bar representation for -120dB to 0dB.
        /// </summary>
        public static string RenderBar(double db, int width = 40)
        {
            // Normalize -120dB..0dB to 0.0..1.0
            double normalized = (db + 120.0) / 120.0;
            int filledWidth = (int)Math.Round(normalized * width);

            filledWidth = Math.Clamp(filledWidth, 0, width);

            return new string('█', filledWidth) + new string('-', width - filledWidth);
        }
    }
}