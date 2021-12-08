using Android.App;
using Android.Graphics;
using Java.IO;
using Java.Nio;
using Java.Nio.Channels;
using Xamarin.TensorFlow.Lite;

namespace MoneyDetector.Droid {
    public class MoneyRecognizer {
        private const int FLOAT_SIZE = 1;
        private const int PIXEL_SIZE = 3;
        private const int LABELS_COUNT = 8;
        private readonly Interpreter model;

        public MoneyRecognizer() {
            var fd = Application.Context.Assets.OpenFd("1208_128.tflite");
            var inputStream = new FileInputStream(fd.FileDescriptor);
            model = new Interpreter(inputStream.Channel.Map(FileChannel.MapMode.ReadOnly, fd.StartOffset, fd.DeclaredLength));
        }

        public MoneyValue GetMoneyValue(Bitmap image) {
            var tensorShape = model.GetInputTensor(0).Shape();
            var width = tensorShape[1];
            var height = tensorShape[2];

            var input = GetModelInput(image, width, height);
            var output = Java.Lang.Object.FromArray(new float[1][] { new float[LABELS_COUNT] });

            model.Run(input, output);

            return new MoneyValue(output.ToArray<float[]>()[0]);
        }

        private ByteBuffer GetModelInput(Bitmap image, int width, int height) {
            var resized = Bitmap.CreateScaledBitmap(image, width, height, true);

            var modelInputSize = FLOAT_SIZE * width * height * PIXEL_SIZE;
            var byteBuffer = ByteBuffer.AllocateDirect(modelInputSize);
            byteBuffer.Order(ByteOrder.NativeOrder());

            var pixels = new int[width * height];
            resized.GetPixels(pixels, 0, resized.Width, 0, 0, resized.Width, resized.Height);

            foreach (var pixel in pixels) {
                byteBuffer.PutFloat((pixel >> 16 & 0xFF) / 255);
                byteBuffer.PutFloat((pixel >> 8 & 0xFF) / 255);
                byteBuffer.PutFloat((pixel & 0xFF) / 255);
            }

            image.Recycle();
            return byteBuffer;
        }
    }
}
