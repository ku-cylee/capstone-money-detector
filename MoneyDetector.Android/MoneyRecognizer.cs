using Android.App;
using Android.Graphics;
using Java.IO;
using Java.Nio;
using Java.Nio.Channels;
using Xamarin.TensorFlow.Lite;

namespace MoneyDetector.Droid {
    public class MoneyRecognizer {
        private const int FLOAT_SIZE = 4;
        private const int PIXEL_SIZE = 3;
        private const int LABELS_COUNT = 8;
        private readonly Interpreter model;

        public MoneyRecognizer() {
            var fd = Application.Context.Assets.OpenFd("moneyvalue-detection-model.tflite");
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
            var resizedBitmap = Bitmap.CreateScaledBitmap(image, width, height, true);
            var modelInputSize = FLOAT_SIZE * height * width * PIXEL_SIZE;
            var byteBuffer = ByteBuffer.AllocateDirect(modelInputSize);
            byteBuffer.Order(ByteOrder.NativeOrder());

            var pixels = new int[width * height];
            resizedBitmap.GetPixels(pixels, 0, resizedBitmap.Width, 0, 0, resizedBitmap.Width, resizedBitmap.Height);

            var pixelIdx = 0;

            for (var i = 0; i < width; i++) {
                for (var j = 0; j < height; j++) {
                    var pixel = pixels[pixelIdx++];
                    byteBuffer.PutFloat(pixel >> 16 & 0xFF);
                    byteBuffer.PutFloat(pixel >> 8 & 0xFF);
                    byteBuffer.PutFloat(pixel & 0xFF);
                }
            }

            image.Recycle();
            return byteBuffer;
        }
    }
}
