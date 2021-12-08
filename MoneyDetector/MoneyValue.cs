using System.Linq;

namespace MoneyDetector {
    public class MoneyValue {
        private readonly int count10 = 0;
        private readonly int count50 = 0;
        private readonly int count100 = 0;
        private readonly int count500 = 0;
        private readonly int count1000 = 0;
        private readonly int count5000 = 0;
        private readonly int count10000 = 0;
        private readonly int count50000 = 0;
        private readonly bool isDetected;
        private const float DETECTION_THRESHOLD = .5F;

        public bool IsDetected {
            get => isDetected;
        }

        public MoneyValue(float[] modelResult) {
            float maxValue = modelResult.Max();
            isDetected = maxValue > DETECTION_THRESHOLD;
            if (!isDetected) return;

            int maxIndex = modelResult.ToList().IndexOf(maxValue);

            count10 = maxIndex == 0 ? 1 : 0;
            count100 = maxIndex == 1 ? 1 : 0;
            count1000 = maxIndex == 2 ? 1 : 0;
            count10000 = maxIndex == 3 ? 1 : 0;
            count50 = maxIndex == 4 ? 1 : 0;
            count500 = maxIndex == 5 ? 1 : 0;
            count5000 = maxIndex == 6 ? 1 : 0;
            count50000 = maxIndex == 7 ? 1 : 0;
        }

        private int GetTotalValue() =>
            10 * count10 + 50 * count50 + 100 * count100 + 500 * count500
            + 1000 * count1000 + 5000 * count5000 + 10000 * count10000 + 50000 * count50000;

        public override string ToString() => $"{GetTotalValue()}원입니다.";
    }
}
