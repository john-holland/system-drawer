# Eyes + Webtop Gaze

`EyesGazeController`: modes Mouse / TypingIndex / WebtopWindow.

`WebtopEyesBehaviorNode`: while gate open, gaze at `webtopWindowCentroid` or monitor anchor. Publish window frame centroids from telecom JS into `SetWebtopCentroid`.

`MousePeripheralDriver` moves `mouseAnchor` and drives mouse gaze.
