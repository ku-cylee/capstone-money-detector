using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Http;

namespace MoneyDetector {
    public class TextToSpeech {
        private readonly Uri API_URI = new Uri("https://kakaoi-newtone-openapi.kakao.com/v1/synthesize");
        private readonly int TIMEOUT = 10 * 1000;
        private readonly string apiToken;

        public TextToSpeech(string apiToken) {
            this.apiToken = $"KakaoAK {apiToken}";
        }

        public void GetSpeech(string text) {
            var request = (HttpWebRequest)WebRequest.Create(API_URI);
            request.Method = "POST";
            request.Timeout = TIMEOUT;
            request.ContentType = "application/xml";
            request.Headers.Add("Authorization", apiToken);

            byte[] bytes = Encoding.UTF8.GetBytes($"<speak>{text}</speak>");
            using (var reqStream = request.GetRequestStream()) reqStream.Write(bytes, 0, bytes.Length);

            var resp = request.GetResponse();
        }
    }
}
