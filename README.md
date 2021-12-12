# Money Detector Application

This application is designed for visually impaired people that suffer difficulties recognizing face values of cashes. By showing target cash to the camera one by one, the application reads out the face value of the cash on recognition.

## Target Platforms

This application targets Android mobile devices, with version of Android 5.0 (Lollipop) to Android 12.0 (S).

## Image Recognition Model

Image recognition model was implemented using Tensorflow library. Training data was retrieved by taking 20 photos following Korean cashes, respectively.

* 50,000KRW banknote
    - '가'(1st): [front](./images/50000krw-ga-observe.jpg), [back](./images/50000krw-ga-reverse.jpg)
* 10,000KRW banknote
    - '바'(6th): [front](./images/10000krw-ba-observe.jpg), [back](./images/10000krw-ba-reverse.jpg)
* 5,000KRW banknote
    - '마'(5th): [front](./images/5000krw-ma-observe.jpg), [back](./images/5000krw-ma-reverse.jpg)
* 1,000KRW banknote
    - '다'(3rd): [front](./images/1000krw-da-observe.jpg), [back](./images/1000krw-da-reverse.jpg)
* 500KRW coin
    - ['가'(1st)](./images/500krw-ga.png)
* 100KRW coin
    - ['나'(2nd)](./images/100krw-na.png)
* 50KRW coin
    - ['나'(2nd)](./images/50krw-na.png)
* 10KRW coin
    - ['다'(3rd)](./images/10krw-da.jpeg)
    - ['라'(4th)](./images/10krw-la.png)

Two models, binary classifier and label classifier, were generated by training data. Both classifiers accepts array of `float32`s, each 3 elements describing normalized RGB values of image data. Outputs of each classifiers are

* Binary classifier
    - `0`: non-cash image
    - `1`: cash image
* Label classifier
    - `0`: 10KRW coin
    - `1`: 100KRW coin
    - `2`: 1,000KRW banknote
    - `3`: 10,00KRW banknote
    - `4`: 50KRW coin
    - `5`: 500KRW coin
    - `6`: 5,000KRW banknote
    - `7`: 50,00KRW banknote

See [noparkee/Capstone-Design](https://github.com/noparkee/Capstone-Design) for more details.

## Mobile Application

Mobile application was implemented using Xamarin for Android platform. The application uses image recognition model implemented from above, and voice synthesis service provided by [kakao developers](https://developers.kakao.com/product/voice).

Project requires an API key for voice synthesis service. Create an app, issue a REST API key `<kakao-rest-api-key>` from kakao developers. Then, provide it to the app by storing at the file "MoneyDetector/appsettings.json" as
```json
{
  "TTS_API_KEY": "<kakao-rest-api-key>"
}
```

## Demo Video

You can see a demo of the application [here](https://www.youtube.com/watch?v=Ho0NijayA1c). The application recognizes cashes well, and also recognizes the background as non-cash on 00:24.

## Limitations

The project was for COSE489 Capstone Design course, and is no longer maintained.
* Relatively low accuracy on 50,000KRW banknotes, 100KRW coins, 50KRW coins, 10KRW coins.
    - Possible solution is more and more training data sets
* Unable to recognize multiple cashes at once
    - Possible solution is to use libraries such as OpenCV.
* Unable to use on other platforms
    - The application has too many platform-dependent codes, which causes too much overhead using Xamarin framework.
* Camera preview on screen is not fullscreen, which is thought to be a minor problem since the actual users of this application has high possibility of having difficulty to see the screen.

## Contributors

Image recognition model used by the application was implemented by [@noparkee](https://github.com/noparkee)
