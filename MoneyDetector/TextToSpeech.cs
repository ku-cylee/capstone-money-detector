using System;
using System.IO;
using System.Text;
using System.Net;

namespace MoneyDetector {
    public class TextToSpeech {
        private readonly Uri API_URI = new Uri("https://kakaoi-newtone-openapi.kakao.com/v1/synthesize");
        private readonly string apiToken;

        private const int TIMEOUT = 10 * 1000;

        public TextToSpeech(string apiToken) {
            this.apiToken = $"KakaoAK {apiToken}";
        }

        public byte[] GetSpeech(string text) {
            var request = (HttpWebRequest)WebRequest.Create(API_URI);
            request.Method = "POST";
            request.Timeout = TIMEOUT;
            request.ContentType = "application/xml";
            request.Headers.Add("Authorization", apiToken);

            byte[] bytes = Encoding.UTF8.GetBytes($"<speak>{text}</speak>");
            using (var reqStream = request.GetRequestStream()) reqStream.Write(bytes, 0, bytes.Length);

            var resp = request.GetResponse();
            using (var audioStream = new MemoryStream()) {
                resp.GetResponseStream().CopyTo(audioStream);
                return audioStream.ToArray();
            }
        }
    }
}
