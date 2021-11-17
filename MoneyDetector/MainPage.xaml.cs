using System;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace MoneyDetector {
    public partial class MainPage : ContentPage {
        public MainPage() {
            InitializeComponent();

            //CapturePhoto();
        }

        public async void CapturePhoto() {
            try {
                var photo = await MediaPicker.CaptureVideoAsync();
            } catch (FeatureNotEnabledException fnsEx) {
                await DisplayAlert("Feature unable", fnsEx.Message, "OK");
            } catch (PermissionException pEx) {
                await DisplayAlert("Permission not granted", pEx.Message, "OK");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
