using Android.App;
using Android.Graphics;
using Java.IO;
using Java.Nio;
using Java.Nio.Channels;
using Xamarin.TensorFlow.Lite;

namespace MoneyDetector.Droid.Recognizers {
    public class ImageRecognizer {
        private const int INPUT_TYPE_SIZE = sizeof(float);
        private const float COLOR_NORMALIZER = 255;

        private readonly int inputWidth;
        private readonly int inputHeight;
        private readonly int inputDepth;

        private readonly int labelsCount;

        private readonly Interpreter model;

        public ImageRecognizer() { }

        public ImageRecognizer(string modelFileName) {
            var fd = Application.Context.Assets.OpenFd(modelFileName);
            var inputStream = new FileInputStream(fd.FileDescriptor);
            model = new Interpreter(inputStream.Channel.Map(FileChannel.MapMode.ReadOnly, fd.StartOffset, fd.DeclaredLength));

            var inputShape = model.GetInputTensor(0).Shape();
            inputWidth = inputShape[1];
            inputHeight = inputShape[2];
            inputDepth = inputShape[3];
            var outputShape = model.GetOutputTensor(0).Shape();
            labelsCount = outputShape[1];
        }

        private ByteBuffer GetModelInputFromImage(Bitmap image) {
            var resized = Bitmap.CreateScaledBitmap(image, inputWidth, inputHeight, true);

            var modelInputSize = INPUT_TYPE_SIZE * inputWidth * inputHeight * inputDepth;
            var byteBuffer = ByteBuffer.AllocateDirect(modelInputSize);
            byteBuffer.Order(ByteOrder.NativeOrder());

            var pixels = new int[inputWidth * inputHeight];
            resized.GetPixels(pixels, 0, resized.Width, 0, 0, resized.Width, resized.Height);

            foreach (var pixel in pixels) {
                for (var shift = 8 * (inputDepth - 1); shift >= 0; shift -= 8) {
                    byteBuffer.PutFloat((pixel >> shift & 0xFF) / COLOR_NORMALIZER);
                }
            }

            return byteBuffer;
        }

        private Java.Lang.Object GetModelOutput() => Java.Lang.Object.FromArray(new float[1][] { new float[labelsCount] });

        protected float[] GetRecognitionResult(Bitmap image) {
            var input = GetModelInputFromImage(image);
            var output = GetModelOutput();

            model.Run(input, output);

            return output.ToArray<float[]>()[0];
        }
    }
}
