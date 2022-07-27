# Known issues and limitations

This page lists some known issues and limitations that you might experience with the Recorder. It also gives basic instructions to help you work around them.

#### Recording slowdown with concurrent Movie Recorders

**Limitation:** The Unity Editor playback process might substantially slow down if you perform concurrent recordings with multiple Movie Recorders, particularly with large image resolutions (full HD or higher).

**Workaround:** The recommended use case is to limit yourself to one Movie recording at a time. Ensure that you have only one active Movie Recorder in the Recorder window and no Movie Recorder Clips in Timeline, or vice-versa. If you need to keep concurrent recordings for some reason, you can still set up lower resolutions or try different encoders (for instance, the MP4 encoding step is much faster than the ProRes one).

#### ActiveCamera recording not available with SRPs

**Limitation:** The use of a Scriptable Render Pipeline ([SRP](https://docs.unity3d.com/Manual/ScriptableRenderPipeline.html)) in your project prevents you from setting ActiveCamera as the source of the recording in the [Movie Recorder](RecorderMovie.md#targeted-camera-source-properties) and the [Image Sequence Recorder](RecorderImage.md#targeted-camera-source-properties). This render pipeline limitation applies to all SRPs including Unity's High Definition Render Pipeline ([HDRP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest)) and Universal Render Pipeline ([URP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest)). For the same reason, the [AOV Recorder](RecorderAOV.md#source-camera), which requires HDRP, doesn't include the ActiveCamera option by design.

**Workaround:** You can use a Tagged Camera for the recording. In your project, add a [Tag](https://docs.unity3d.com/Manual/Tags.html) to the camera you want to record through, and then in the Recorder Settings, select TaggedCamera and specify your camera's Tag.

#### Audio recording limited support

**Limitation:** The Recorder currently supports only the recording of samples from the Unity built-in audio engine. As such, it cannot record audio from third-party audio engines such as FMOD Studio or Wwise.

**Workaround:** For a movie, you can record the third-party audio output in WAV format through another application, reimport this recorded file into the Unity Timeline, and then use the Recorder to create the final movie with audio. Alternatively, you can use any video editing software to recompose audio and video.

#### GIF Animation Recorder no longer available

**Limitation:** This version of the Recorder no longer includes the GIF Animation Recorder, although it is still available in Recorder versions 2.5 and lower.

**Workaround:** To produce a GIF animation, record your content with the [Image Sequence Recorder](RecorderImage.md) and then process the result through any external GIF animation software.

#### Limited support of AA/TAA in AOVs

**Limitation:** The Beauty AOV is the only AOV that you can currently record with Anti-Aliasing (AA) / Temporal Anti-Aliasing (TAA) enabled on your recording camera.

#### Color artifacts in AOV recordings when TAA is enabled

**Known issue:** If you record multiple AOVs while the recording camera has Temporal Anti-Aliasing (TAA) enabled, the recorded outputs might contain unexpected color artifacts. For example, some areas of a Beauty pass might include artificial colors coming from the data recorded for a Normal pass.

**Workaround:** If you need to record a Beauty pass with TAA enabled on your recording camera, you should record it through its own recording session, separately from any other AOVs.

<a name="360-view"></a>
#### 360 View recording issues and limitations

The Recorder doesn't fully support 360 View recording. Here is a list of known issues you might encounter:

* If you record a 360 View in projects that use the High Definition Render Pipeline (HDRP), the rendered cube map might have artefacts that make its boundaries visible. One way to work around this issue would be to disable post-process effects such as Shadows, Bloom, or Volumetric Fogs.

* If you record a 360 View through a Physical Camera, the rendered image is not equirectangular, which makes the output media unusable. To work around the issue, use a regular Camera for the recording.

* The Recorder doesn't support stereoscopic recording in projects that use any Scriptable Render Pipelines (SRPs). The **Stereo Separation** property has no effect on the recorded views, which makes the rendering identical for both eyes.
