# Overview

**Real-Time Procedural Level Generation in a Platform Game: An Emotion-Driven Approach — Research Prototype**

This is a research prototype designed to assess the impact of ‘emotion-driven procedural level generation’ on player experience. It currently includes two modes:

*   **Mode 1:** Use the camera for local inference and adapt the difficulty based on the player's emotions.

*   **Mode 2:** Emotion recognition is not used, and the difficulty gradually increases over time.

Both modes generate infinite levels.  
For research and privacy details, see the Participant Information Statement (PIS): [ParticipantInformationSheet.pdf](https://www.admiralspee.site/wp-content/uploads/2025/08/ParticipantInformationSheet.pdf)

# Before play

*   18+ only.

*   **Camera permission** is required for Mode 1; no audio is captured.

*   **Privacy and data:** Camera images are only used for local inference and are not saved or uploaded. The data displayed on the results page is only for on-site viewing and is not automatically saved. You can voluntarily enter these values manually into the survey.

*   **Known limitation**: emotion recognition accuracy is limited (research prototype). You don’t need to report generic accuracy concerns (open comments are still welcome).

*   **Well-being:** If the camera makes you feel uncomfortable, please use the Mode 2 or turn off the camera and exit.

# Controls

**Move:** A / D

**Jump:** Space

**High jump:** Press and hold the Space

# Enemies

**Spikes** will rise and fall, and touching them will result in game over. 

**Slimes** will move back and forth within a certain area, and touching them from the side will result in game over. stepping on slimes from above will kill them. 

 **Turrets** remain in fixed positions. When players enter their range, they will shoot shells at them.  

# Troubleshooting

**No Result (ORT\_INIT\_FAILED):** Check if the latest version of Visual C++ x64 Redistributable is installed. ([Latest supported Visual C++ Redistributable downloads | Microsoft Learn](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170))

**No Result (CAM\_PERMISSION\_DENIED):** Check whether the camera permissions have been denied.

**No Result (CAM\_NO\_DEVICE):** Check the camera connection, driver, and whether it is being used exclusively by other programmes.

**No Result:** You have manually stopped the camera. Please re-enter the level to enable the camera.

**Low FPS/high latency**: Close background tasks. Try Mode 2 first.

**Poor recognition**: face the camera, improve lighting, avoid backlight. Limitations are expected in a research prototype.

# Data & compliance

On-device inference, no saved or upload. Microphone off by default. No third-party sharing.

**Survey data** (if you opt in) are stored by Microsoft Forms under its privacy policy. We use anonymous settings and do not collect identifiers.

# Thanks

Thanks for participating and sharing your feedback! This prototype is for academic evaluation at goldsmiths, University of London.

# License

## Main Project
The source code of this project is licensed under the **MIT License**.

The game assets (original art/audio) are licensed under **CC-BY 4.0** (or your choice).

## Third-Party Notices
This project makes use of the following third-party assets and libraries. Full license texts can be found in the `licenses/` directory.

###  Art & Assets
| Asset Name | Author | License | File |
| :--- | :--- | :--- | :--- |
| **Platformer Art: Deluxe** | Kenney | CC0 1.0 | `CC0-1.0-Platformer-Art-Deluxe.txt` |
| **Platformer Art: More animations** | Kenney | CC0 1.0 | `CC0-1.0-Platformer-Art-More-Animations-and-Enemies.txt` |

###  AI & Frameworks
| Library | Version | License | File |
| :--- | :--- | :--- | :--- |
| **micro-expression-recognition** | - | MIT | `MIT-micro-expression-recognition.txt` |
| **ONNX Runtime** | v1.22 | MIT | `MIT-ONNX-Runtime.txt` |
| **ONNX** | - | Apache-2.0 | `Apache-2.0-ONNX.txt` |

###  Plugins
| Plugin | License | Notice |
| :--- | :--- | :--- |
| **OpenCV for Unity** | Asset Store EULA | **Not included in repo.** Please acquire from Unity Asset Store. |
| *(Underlying OpenCV)* | Apache 2.0 / BSD | See `OpenCV-Apache-2.0.txt` |

# References

_Building a procedurally generated platformer_ (2017) _dylanwolf.com_. Available at: [https://www.dylanwolf.com/2016/05/09/building-a-procedurally-generated-platforme...](https://www.dylanwolf.com/2016/05/09/building-a-procedurally-generated-platformer-in-unity/?utm_source=chatgpt.com) (Accessed: 11 August 2025). 

Reddy, S.P.T. _et al._ (2019) _Spontaneous facial micro-expression recognition using 3D spatiotemporal convolutional neural networks_, _arXiv.org_. Available at: [https://arxiv.org/abs/1904.01390](https://arxiv.org/abs/1904.01390) (Accessed: 11 August 2025).
