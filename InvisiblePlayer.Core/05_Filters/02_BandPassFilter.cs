namespace InvisiblePlayer.Core.Filters
{
    public class BandPassFilter
    {
        private double _b0, _b1, _b2, _a1, _a2;
        private double _x1, _x2, _y1, _y2;

        public void SetParams(double centerFreqHz, double q, double sampleRate = 44100.0)
        {
            double w0 = 2.0 * System.Math.PI * centerFreqHz / sampleRate;
            double alpha = System.Math.Sin(w0) / (2.0 * q);

            double a0 = 1.0 + alpha;
            _b0 = alpha / a0;
            _b1 = 0.0;
            _b2 = -alpha / a0;
            _a1 = (-2.0 * System.Math.Cos(w0)) / a0;
            _a2 = (1.0 - alpha) / a0;
        }

        public double Process(double input)
        {
            double y = _b0 * input + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
            _x2 = _x1; _x1 = input;
            _y2 = _y1; _y1 = y;
            return y;
        }
    }
}