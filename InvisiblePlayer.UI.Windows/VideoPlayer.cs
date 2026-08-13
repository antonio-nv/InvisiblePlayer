using System;
using System.Windows.Controls;

namespace InvisiblePlayer.UI.Windows
{
    public class VideoPlayer
    {
        private readonly MediaElement _mediaElement;

        public event Action? PlaybackEnded;

        public VideoPlayer(MediaElement mediaElement)
        {
            _mediaElement = mediaElement;
            _mediaElement.MediaEnded += (s, e) => PlaybackEnded?.Invoke();
        }

        public bool IsPlaying { get; private set; }

        public TimeSpan CurrentTime => _mediaElement.Position;
        public TimeSpan TotalTime => _mediaElement.NaturalDuration.HasTimeSpan ? _mediaElement.NaturalDuration.TimeSpan : TimeSpan.Zero;

        public double Volume
        {
            get => _mediaElement.Volume;
            set => _mediaElement.Volume = Math.Clamp(value, 0.0, 1.0);
        }

        public void LoadAndPlay(string filePath)
        {
            _mediaElement.Source = new Uri(filePath);
            _mediaElement.Visibility = System.Windows.Visibility.Visible;
            Play();
        }

        public void Play()
        {
            _mediaElement.Play();
            IsPlaying = true;
        }

        public void Pause()
        {
            _mediaElement.Pause();
            IsPlaying = false;
        }

        public void Stop()
        {
            _mediaElement.Stop();
            _mediaElement.Visibility = System.Windows.Visibility.Collapsed;
            IsPlaying = false;
        }
    }
}
