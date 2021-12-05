using System;
using Xamarin.Forms;

namespace MoneyDetector {
    public class CameraPreview : View {
        private DateTime lastCapturedAt = DateTime.UtcNow;
        private readonly TimeSpan CAPTURE_TIME_INTERVAL = TimeSpan.FromMilliseconds(200);

        private DateTime lastPlayedAt = DateTime.UtcNow;
        private readonly TimeSpan AUDIO_PLAY_INTERVAL = TimeSpan.FromMilliseconds(10 * 1000);

        public readonly TextToSpeech tts = new TextToSpeech(App.Config.TtsApiKey);

        public static readonly BindableProperty CameraProperty = BindableProperty.Create(
            propertyName: "Camera",
            returnType: typeof(CameraOptions),
            declaringType: typeof(CameraPreview),
            defaultValue: CameraOptions.Rear);

        public CameraOptions Camera {
            get { return (CameraOptions)GetValue(CameraProperty); }
            set { SetValue(CameraProperty, value); }
        }

        public bool DoCapture() {
            var currentTime = DateTime.UtcNow;
            var sinceLastCapture = currentTime - lastCapturedAt;
            if (sinceLastCapture < CAPTURE_TIME_INTERVAL) return false;

            lastCapturedAt = currentTime;
            return true;
        }

        public bool DoPlay() {
            var currentTime = DateTime.UtcNow;
            var sinceLastPlay = currentTime - lastPlayedAt;
            return sinceLastPlay >= AUDIO_PLAY_INTERVAL;
        }

        public void UpdatePlayedTime() {
            lastPlayedAt = DateTime.UtcNow;
        }
    }
}
