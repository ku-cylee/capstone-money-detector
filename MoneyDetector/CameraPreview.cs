using System;
using Xamarin.Forms;

namespace MoneyDetector {
    public class CameraPreview : View {
        private DateTime lastCapturedAt = DateTime.UtcNow;
        private readonly TimeSpan CAPTURE_TIME_INTERVAL = TimeSpan.FromMilliseconds(200);
        private readonly TextToSpeech tts = new TextToSpeech(App.Config.TtsApiKey);

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

        public void SpeakMoneyValueFromImage(byte[] imageBytes) {
            var moneyValue = new MoneyValue(imageBytes);
            if (!moneyValue.IsDetected) return;

            tts.GetSpeech(moneyValue.ToString());
        }
    }
}
