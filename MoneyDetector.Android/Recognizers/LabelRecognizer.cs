namespace MoneyDetector.Droid.Recognizers {
    public class LabelRecognizer : ImageRecognizer {
        public LabelRecognizer() : base("label-model-1208.tflite") { }

        public MoneyValue GetMoneyValue(Android.Graphics.Bitmap image) {
            var output = GetRecognitionResult(image);
            return new MoneyValue(output);
        }
    }
}
