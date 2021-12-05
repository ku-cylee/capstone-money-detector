using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Xamarin.Forms;

namespace MoneyDetector {
    public partial class App : Application {
        public App() {
            InitializeComponent();

            MainPage = new MainPage();
        }

        private static Configuration config;

        public static Configuration Config {
            get {
                if (config == null) LoadConfig();
                return config;
            }
        }

        private static void LoadConfig() {
            var configResourceStream = Assembly.GetAssembly(typeof(Configuration)).GetManifestResourceStream("MoneyDetector.appsettings.json");

            if (configResourceStream == null) return;

            using (var stream = new StreamReader(configResourceStream)) {
                var jsonString = stream.ReadToEnd();
                config = JsonConvert.DeserializeObject<Configuration>(jsonString);
            }
        }

        protected override void OnStart() {}

        protected override void OnSleep() {}

        protected override void OnResume() {}
    }
}
