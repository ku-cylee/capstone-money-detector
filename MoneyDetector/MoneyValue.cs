using System;

namespace MoneyDetector {
    public class MoneyValue {
        private int count10 = 0;
        private int count50 = 0;
        private int count100 = 0;
        private int count500 = 0;
        private int count1000 = 0;
        private int count5000 = 0;
        private int count10000 = 0;
        private int count50000 = 0;
        private bool isDetected;

        public bool IsDetected {
            get => isDetected;
        }

        public MoneyValue(byte[] imageBytes) {
            // Get detection result from TF model
            isDetected = GetRandomBool(10);

            if (!isDetected) return;

            count10 = GetRandomInt();
            count50 = GetRandomInt();
            count100 = GetRandomInt();
            count500 = GetRandomInt();
            count1000 = GetRandomInt();
            count5000 = GetRandomInt();
            count10000 = GetRandomInt();
            count50000 = GetRandomInt();
        }

        private int GetTotalValue() =>
            10 * count10 + 50 * count50 + 100 * count100 + 500 * count500
            + 1000 * count1000 + 5000 * count5000 + 10000 * count10000 + 50000 * count50000;

        public override string ToString() => $"{GetTotalValue()}원입니다.";

        #region Random
        // Region for test without actual model
        // MUST be deleted after model added
        private Random random = new Random();

        private bool GetRandomBool(int truePermill) => random.Next(0, 1000) < truePermill;

        private int GetRandomInt() => random.Next(0, 20);
        #endregion
    }
}
