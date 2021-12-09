namespace MoneyDetector.Droid.Recognizers {
    public class BinaryRecognizer : ImageRecognizer {
        public BinaryRecognizer() : base("binary-model-1208.tflite") { }

        public bool IsMoney(Android.Graphics.Bitmap image) {
            var output = GetRecognitionResult(image);
            return output[0] < output[1];
        }
    }
}
