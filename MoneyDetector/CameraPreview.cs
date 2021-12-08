using System;
using Xamarin.Forms;

namespace MoneyDetector {
    public class CameraPreview : View {
        private DateTime captureAfter = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        private readonly TimeSpan CAPTURE_TIME_INTERVAL = TimeSpan.FromMilliseconds(1000);
        private readonly TimeSpan AUDIO_PLAY_INTERVAL = TimeSpan.FromMilliseconds(5 * 1000);

        public readonly TextToSpeech tts = new TextToSpeech(App.Config.TTS_API_KEY);

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
            var doCapture = DateTime.UtcNow >= captureAfter;
            if (doCapture) captureAfter = DateTime.UtcNow + CAPTURE_TIME_INTERVAL;
            return doCapture;
        }

        public void UpdateNextCaptureOnRecognition() {
            captureAfter = DateTime.UtcNow + AUDIO_PLAY_INTERVAL;
        }
    }
}
