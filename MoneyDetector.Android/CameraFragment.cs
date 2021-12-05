﻿using Android;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
using Java.Lang;
using Java.Util.Concurrent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms.Platform.Android;

namespace MoneyDetector.Droid {
    public class CameraFragment : Fragment, TextureView.ISurfaceTextureListener {
        CameraDevice device;
        CaptureRequest.Builder sessionBuilder;
        CameraCaptureSession session;
        CameraTemplate cameraTemplate;
        CameraManager manager;

        bool cameraPermissionGranted;
        bool busy;
        bool repeatingIsRunning;
        int sensorOrientation;
        string cameraId;
        LensFacing cameraType;

        Android.Util.Size previewSize;

        HandlerThread backgroundThread;
        Handler backgroundHandler = null;

        Semaphore captureSessionOpenCloseLock = new Semaphore(1);

        AutoFitTextureView texture;

        TaskCompletionSource<CameraDevice> initTaskSource;
        TaskCompletionSource<bool> permissionsRequested;

        CameraManager Manager => manager ??= (CameraManager)Context.GetSystemService(Context.CameraService);

        bool IsBusy {
            get => device == null || busy;
            set { busy = value; }
        }

        bool Available;

        public CameraPreview Element { get; set; }

        #region Constructors

        public CameraFragment() { }

        public CameraFragment(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

        #endregion

        #region Overrides

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
            => inflater.Inflate(Resource.Layout.CameraFragment, null);
        public override void OnViewCreated(View view, Bundle savedInstanceState)
            => texture = view.FindViewById<AutoFitTextureView>(Resource.Id.cameratexture);

        public override void OnPause() {
            CloseSession();
            StopBackgroundThread();
            base.OnPause();
        }

        public override async void OnResume() {
            base.OnResume();

            StartBackgroundThread();

            if (texture is null) return;

            if (texture.IsAvailable) {
                View?.SetBackgroundColor(Element.BackgroundColor.ToAndroid());
                cameraTemplate = CameraTemplate.Preview;
                await RetrieveCameraDevice(force: true);
            } else {
                texture.SurfaceTextureListener = this;
            }
        }

        protected override void Dispose(bool disposing) {
            CloseDevice();
            base.Dispose(disposing);
        }

        #endregion

        #region Public methods

        public async Task RetrieveCameraDevice(bool force = false) {
            if (Context == null || (!force && initTaskSource != null)) return;

            if (device != null) CloseDevice();

            await RequestCameraPermissions();
            if (!cameraPermissionGranted) return;

            if (!captureSessionOpenCloseLock.TryAcquire(2500, TimeUnit.Milliseconds)) {
                throw new RuntimeException("Timeout waiting to lock camera opening");
            }

            IsBusy = true;
            cameraId = GetCameraId();

            if (string.IsNullOrEmpty(cameraId)) {
                IsBusy = false;
                captureSessionOpenCloseLock.Release();
                Console.WriteLine("No camera found");
            } else {
                try {
                    CameraCharacteristics characteristics = Manager.GetCameraCharacteristics(cameraId);
                    StreamConfigurationMap map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                    previewSize = ChooseOptimalSize(map.GetOutputSizes(Class.FromType(typeof(SurfaceTexture))),
                        texture.Width, texture.Height, GetMaxSize(map.GetOutputSizes((int)ImageFormatType.Jpeg)));
                    sensorOrientation = (int)characteristics.Get(CameraCharacteristics.LensFacing);
                    cameraType = (LensFacing)(int)characteristics.Get(CameraCharacteristics.LensFacing);

                    if (Resources.Configuration.Orientation == Android.Content.Res.Orientation.Landscape) {
                        texture.SetAspectRatio(previewSize.Width, previewSize.Height);
                    } else {
                        texture.SetAspectRatio(previewSize.Height, previewSize.Width);
                    }

                    initTaskSource = new TaskCompletionSource<CameraDevice>();
                    Manager.OpenCamera(cameraId, new CameraStateListener {
                        OnOpenedAction = device => initTaskSource?.TrySetResult(device),
                        OnDisconnectedAction = device => {
                            initTaskSource?.TrySetResult(null);
                            CloseDevice(device);
                        },
                        OnErrorAction = (device, error) => {
                            initTaskSource?.TrySetResult(device);
                            Console.WriteLine($"Camera device error: {error}");
                            CloseDevice(device);
                        },
                        OnClosedAction = device => {
                            initTaskSource?.TrySetResult(null);
                            CloseDevice(device);
                        },
                    }, backgroundHandler);

                    captureSessionOpenCloseLock.Release();
                    device = await initTaskSource.Task;
                    initTaskSource = null;
                    if (device != null) await PrepareSession();
                } catch (Java.Lang.Exception ex) {
                    Console.WriteLine("Failed to open camera", ex);
                    Available = false;
                } finally {
                    IsBusy = false;
                }
            }
        }

        public void UpdateRepeatingRequest() {
            if (session == null || sessionBuilder == null) return;

            IsBusy = true;

            try {
                if (repeatingIsRunning) session.StopRepeating();

                sessionBuilder.Set(CaptureRequest.ControlMode, (int)ControlMode.Auto);
                sessionBuilder.Set(CaptureRequest.ControlAeMode, (int)ControlAEMode.On);
                session.SetRepeatingRequest(sessionBuilder.Build(), listener: null, backgroundHandler);
                repeatingIsRunning = true;
            } catch (Java.Lang.Exception ex) {
                Console.WriteLine("Update preview exception", ex);
            } finally {
                IsBusy = false;
            }
        }

        #endregion

        void StartBackgroundThread() {
            backgroundThread = new HandlerThread("CameraBackground");
            backgroundThread.Start();
            backgroundHandler = new Handler(backgroundThread.Looper);
        }

        void StopBackgroundThread() {
            if (backgroundHandler == null) return;

            backgroundThread.QuitSafely();
            try {
                backgroundThread.Join();
                backgroundThread = null;
                backgroundHandler = null;
            } catch (InterruptedException ex) {
                Console.WriteLine("Error stopping background thread", ex);
            }
        }

        Android.Util.Size GetMaxSize(Android.Util.Size[] imageSizes) {
            Android.Util.Size maxSize = null;
            long maxPixels = 0;
            for (int i = 0; i < imageSizes.Length; i++) {
                long currentPixels = imageSizes[i].Width * imageSizes[i].Height;
                if (currentPixels > maxPixels) {
                    maxSize = imageSizes[i];
                    maxPixels = currentPixels;
                }
            }
            return maxSize;
        }

        Android.Util.Size ChooseOptimalSize(Android.Util.Size[] choices, int width, int height, Android.Util.Size aspectRatio) {
            List<Android.Util.Size> bigEnough = new List<Android.Util.Size>();
            int w = aspectRatio.Width;
            int h = aspectRatio.Height;

            foreach (Android.Util.Size option in choices) {
                if (option.Height == option.Width * h / w && option.Width >= width && option.Height >= height) {
                    bigEnough.Add(option);
                }
            }

            if (bigEnough.Count > 0) {
                int minArea = bigEnough.Min(s => s.Width * s.Height);
                return bigEnough.First(s => s.Width * s.Height == minArea);
            } else {
                Console.WriteLine("Couldn't find any suitable preview size");
                return choices[0];
            }
        }

        string GetCameraId() {
            string[] cameraIdList = Manager.GetCameraIdList();
            if (cameraIdList.Length == 0) return null;

            string FilterCameraByLens(LensFacing lensFacing) {
                foreach (string id in cameraIdList) {
                    CameraCharacteristics characteristics = Manager.GetCameraCharacteristics(id);
                    if (lensFacing == (LensFacing)(int)characteristics.Get(CameraCharacteristics.LensFacing)) return id;
                }
                return null;
            }

            return (Element.Camera == CameraOptions.Front) ? FilterCameraByLens(LensFacing.Front) : FilterCameraByLens(LensFacing.Back);
        }

        async Task PrepareSession() {
            IsBusy = true;

            try {
                CloseSession();
                sessionBuilder = device.CreateCaptureRequest(cameraTemplate);

                List<Surface> surfaces = new List<Surface>();
                if (texture.IsAvailable && previewSize != null) {
                    var texture = this.texture.SurfaceTexture;
                    texture.SetDefaultBufferSize(previewSize.Width, previewSize.Height);
                    Surface previewSurface = new Surface(texture);
                    surfaces.Add(previewSurface);
                    sessionBuilder.AddTarget(previewSurface);
                }

                TaskCompletionSource<CameraCaptureSession> tcs = new TaskCompletionSource<CameraCaptureSession>();
                device.CreateCaptureSession(surfaces, new CameraCaptureStateListener {
                    OnConfigureFailedAction = captureSession => {
                        tcs.SetResult(null);
                        Console.WriteLine("Failed to create capture session");
                    },
                    OnConfiguredAction = captureSession => tcs.SetResult(captureSession),
                }, null);

                session = await tcs.Task;
                if (session != null) UpdateRepeatingRequest();
            } catch (Java.Lang.Exception ex) {
                Available = false;
                Console.WriteLine("Capture error", ex);
            } finally {
                Available = session != null;
                IsBusy = false;
            }
        }

        void CloseSession() {
            repeatingIsRunning = false;
            if (session == null) return;

            try {
                session.StopRepeating();
                session.AbortCaptures();
                session.Close();
                session.Dispose();
                session = null;
            } catch (CameraAccessException ex) {
                Console.WriteLine("Camera access error", ex);
            } catch (Java.Lang.Exception ex) {
                Console.WriteLine("Error closing device", ex);
            }
        }

        void CloseDevice(CameraDevice inputDevice) {
            if (inputDevice == device) CloseDevice();
        }

        void CloseDevice() {
            CloseSession();

            try {
                if (session != null) {
                    sessionBuilder.Dispose();
                    sessionBuilder = null;
                }

                if (device != null) {
                    device.Close();
                    device = null;
                }
            } catch (Java.Lang.Exception error) {
                Console.WriteLine("Error closing device", error);
            }
        }

        void ConfigureTransform(int viewWidth, int viewHeight) {
            if (texture == null || previewSize == null || previewSize.Width == 0 || previewSize.Height == 0) return;

            var matrix = new Matrix();
            var viewRect = new RectF(0, 0, viewWidth, viewHeight);
            var bufferRect = new RectF(0, 0, previewSize.Height, previewSize.Width);
            var centerX = viewRect.CenterX();
            var centerY = viewRect.CenterY();
            bufferRect.Offset(centerX - bufferRect.CenterX(), centerY - bufferRect.CenterY());
            matrix.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);
            matrix.PostRotate(GetCaptureOrientation(), centerX, centerY);
            texture.SetTransform(matrix);
        }

        int GetCaptureOrientation() {
            int frontOffset = cameraType == LensFacing.Front ? 90 : -90;
            return (360 + sensorOrientation - GetDisplayRotationDegrees() + frontOffset) % 360;
        }

        int GetDisplayRotationDegrees() =>
            GetDisplayRotation() switch {
                SurfaceOrientation.Rotation90 => 90,
                SurfaceOrientation.Rotation180 => 180,
                SurfaceOrientation.Rotation270 => 270,
                _ => 0
            };

        SurfaceOrientation GetDisplayRotation() =>
            Android.App.Application.Context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>().DefaultDisplay.Rotation;

        #region Permissions

        async Task RequestCameraPermissions() {
            if (permissionsRequested != null) await permissionsRequested.Task;

            List<string> permissionsToRequest = new List<string>();
            cameraPermissionGranted = ContextCompat.CheckSelfPermission(Context, Manifest.Permission.Camera) == Permission.Granted;

            if (!cameraPermissionGranted) permissionsToRequest.Add(Manifest.Permission.Camera);

            if (permissionsToRequest.Count > 0) {
                permissionsRequested = new TaskCompletionSource<bool>();
                RequestPermissions(permissionsToRequest.ToArray(), requestCode: 1);
                await permissionsRequested.Task;
                permissionsRequested = null;
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults) {
            if (requestCode != 1) {
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
                return;
            }

            for (int i = 0; i < permissions.Length; i++) {
                if (permissions[i] == Manifest.Permission.Camera) {
                    cameraPermissionGranted = grantResults[i] == Permission.Granted;
                    if (!cameraPermissionGranted) Console.WriteLine("No permission to use the camera");
                }
            }
            permissionsRequested?.TrySetResult(true);
        }

        #endregion

        #region TextureView.ISurfaceTextureListener

        async void TextureView.ISurfaceTextureListener.OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height) {
            View?.SetBackgroundColor(Element.BackgroundColor.ToAndroid());
            cameraTemplate = CameraTemplate.Preview;
            await RetrieveCameraDevice();
        }

        bool TextureView.ISurfaceTextureListener.OnSurfaceTextureDestroyed(SurfaceTexture surface) {
            CloseDevice();
            return true;
        }

        void TextureView.ISurfaceTextureListener.OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
            => ConfigureTransform(width, height);

        async void TextureView.ISurfaceTextureListener.OnSurfaceTextureUpdated(SurfaceTexture surface) {
            try {
                if (!Element.DoCapture() || !Element.DoPlay()) return;

                byte[] imageBytes = null;
                var image = Bitmap.CreateBitmap(texture.Bitmap, 0, 0, texture.Bitmap.Width, texture.Bitmap.Height);
                using (var imageStream = new MemoryStream()) {
                    await image.CompressAsync(Bitmap.CompressFormat.Jpeg, 80, imageStream);
                    image.Recycle();
                }

                var moneyValue = new MoneyValue(imageBytes);
                if (!moneyValue.IsDetected) return;

                var audioBytes = Element.tts.GetSpeech(moneyValue.ToString());
                var audioBase64 = Convert.ToBase64String(audioBytes, 0, audioBytes.Length);
                var player = new MediaPlayer();
                player.SetDataSource($"data:audio/mp3;base64,{audioBase64}");
                player.Prepare();
                player.Start();

                Element.UpdatePlayedTime();
            } catch {}
        }

        #endregion
    }
}
