# Image Sequence Recorder properties

The **Image Sequence Recorder** generates a sequence of image files in the JPEG, PNG, or EXR (OpenEXR) file format.

This page covers all properties specific to the Image Sequence Recorder type.

> **Note:** To fully configure any Recorder, you must also set the general recording properties according to the recording interface you are using: the [Recorder window](RecorderWindowRecordingProperties.md) or a [Recorder Clip](RecordingTimelineTrack.md#recorder-clip-properties).

![](Images/RecorderImage.png)

The Image Sequence Recorder properties fall into three main categories:
* [Capture](#capture)
* [Format](#format)
* [Output File](#output-file)

## Capture

Use this section to define the source and the content of your recording.

|Property||Function|
|:---|:---|:---|
| **Source** ||Specifies the input for the recording.|
|| Game View |Records frames rendered in the Game View.<br/><br/>Selecting this option displays the [Game View source properties](#game-view-source-properties). |
|| Targeted Camera |Records frames captured by a specific camera, even if the Game View does not use that camera.<br/><br/>Selecting this option displays the [Targeted Camera source properties](#targeted-camera-source-properties).|
|| 360 View |Records a 360-degree image sequence.<br/><br/>Selecting this option displays the [360 View source properties](#360-view-source-properties).|
|| Render Texture Asset |Records frames rendered in a Render Texture.<br/><br/>Selecting this option displays the [Render Texture Asset source properties](#render-texture-asset-source-properties).|
|| Texture Sampling |Supersamples the source camera during the capture to generate anti-aliased images in the recording.<br/><br/>Selecting this option displays the [Texture Sampling source properties](#texture-sampling-source-properties).|
| **Flip Vertical** ||When you enable this option, the Recorder flips the output image vertically.<br />This is useful to correct for systems that output video upside down.<br /><br />This option is not available when you record the Game View.|
| **Accumulation** || Enable this feature to render multiple sub-frames for accumulation purposes. See the [Accumulation properties](#accumulation-properties) for more details on this feature availability, use cases, and setup.<br /><br />**Note:** Enabling the **Accumulation** feature might considerably slow down your recording process as it involves a higher amount of rendering steps.|

### Game View source properties
[!include[](InclCaptureOptionsGameview.md)]

### Targeted Camera source properties
[!include[](InclCaptureOptionsTargetedCamera.md)]

### 360 View source properties
[!include[](InclCaptureOptions360View.md)]

### Render Texture Asset source properties
[!include[](InclCaptureOptionsRenderTextureAsset.md)]

### Texture Sampling source properties
[!include[](InclCaptureOptionsTextureSampling.md)]

### Accumulation properties
[!include[](InclCaptureOptionsAccumulation.md)]

## Format

Use this section to set up the media format you need to save the recorded images in.

|Property||Function|
|:---|:---|:---|
| **Media File Format** || The file encoding format.<br/><br/>Choose **PNG**, **JPEG**, or **EXR** ([OpenEXR](https://en.wikipedia.org/wiki/OpenEXR)). The Recorder encodes EXR in 16 bits. |
| **Include Alpha** || Enable this property to include the alpha channel in the recording. Disable it to only record the RGB channels.<br/><br/>This property is not available when the selected **Media File Format** doesn't support transparency, or when **Capture** is set to **Game View**. |
| **Color Space** | | The color space (gamma curve and gamut) to use in the output images. |
|  | sRGB, sRGB | Uses sRGB curve and sRGB primaries. |
|  | Linear, sRGB (unclamped) | Uses linear curve and sRGB primaries.<br/>This option is only available when you set the **Format** to **EXR**.<br/><br/>**Important:** To get the expected unclamped values in the output images, you must:<br/><br/>• Disable any Tonemapping post-processing effects in your Scene (menu: **Edit > Project Settings > HDRP Default Settings** and deselect **Tonemapping**) and in any Volume that includes a Tonemapping override (select the Volume, navigate in the Inspector and deselect **Tonemapping** if present).<br/><br/>• Disable **Dithering** on the Camera selected for the capture (in the Inspector, navigate to **General** and deselect **Dithering**). |
| **Compression** | | The compression method to apply when saving the data. <br/>This property is only available when you set the **Media File Format** to **EXR**. |
|  | None | Disables all compression. |
|  | Zip | Applies deflate compression to blocks of 16 scanlines at a time.<br/>This is the default selection. |
|  | RLE | Applies [Run-length encoding](https://en.wikipedia.org/wiki/Run-length_encoding) compression.  |

## Output File

Use this section to specify the output **Path** and **File Name** pattern to save the recorded image files.

> **Note:** [Output File properties](OutputFileProperties.md) work the same for all types of recorders.
