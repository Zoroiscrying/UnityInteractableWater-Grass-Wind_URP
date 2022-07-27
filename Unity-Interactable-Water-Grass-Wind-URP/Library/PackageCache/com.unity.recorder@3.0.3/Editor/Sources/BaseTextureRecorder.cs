using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
#if HDRP_ACCUM_API
using UnityEngine.Rendering.HighDefinition;
#endif

namespace UnityEditor.Recorder
{
    /// <summary>
    /// Abstract base class for all Recorders that output images.
    /// </summary>
    /// <typeparam name="T">The class implementing the Recorder Settings.</typeparam>
    public abstract class BaseTextureRecorder<T> : GenericRecorder<T> where T : RecorderSettings
    {
        int       m_OngoingAsyncGPURequestsCount;
        bool      m_DelayedEncoderDispose;

        /// <summary>
        /// Whether or not to use asynchronous GPU commands in order to get the texture for the recorder.
        /// </summary>
        protected bool      UseAsyncGPUReadback;

        Texture2D m_ReadbackTexture;

        /// <summary>
        /// Stores the format of the texture used for the readback.
        /// </summary>
        protected abstract TextureFormat ReadbackTextureFormat { get; }

        /// <inheritdoc/>
        protected internal override bool BeginRecording(RecordingSession session)
        {
            if (!base.BeginRecording(session))
                return false;
#if HDRP_ACCUM_API
            var hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdPipeline != null)
            {
                if (settings.IsAccumulationSupported() && settings is IAccumulation accumulation)
                {
                    AccumulationSettings aSettings = accumulation.GetAccumulationSettings();

                    if (aSettings != null && aSettings.CaptureAccumulation)
                    {
                        if (aSettings != null &&
                            aSettings.ShutterType == AccumulationSettings.ShutterProfileType.Range)
                        {
                            hdPipeline.BeginRecording(
                                aSettings.Samples,
                                aSettings.ShutterInterval,
                                aSettings.ShutterFullyOpen,
                                aSettings.ShutterBeginsClosing
                            );
                        }
                        else
                        {
                            hdPipeline.BeginRecording(
                                aSettings.Samples,
                                aSettings.ShutterInterval,
                                aSettings.ShutterProfileCurve
                            );
                        }
                    }
                }
            }
#endif
            UseAsyncGPUReadback = SystemInfo.supportsAsyncGPUReadback;
            m_OngoingAsyncGPURequestsCount = 0;
            m_DelayedEncoderDispose = false;
            return true;
        }

        /// <inheritdoc/>
        protected internal override void RecordFrame(RecordingSession session)
        {
            var input = (BaseRenderTextureInput)m_Inputs[0];

            if (input.ReadbackTexture != null)
            {
                WriteFrame(input.ReadbackTexture);
                return;
            }

            var renderTexture = input.OutputRenderTexture;

            if (renderTexture == null)
            {
                Debug.LogWarning($"Ignoring the current frame because the source has been disposed");
                return;
            }

            if (UseAsyncGPUReadback)
            {
                AsyncGPUReadback.Request(
                    renderTexture, 0, ReadbackTextureFormat, ReadbackDone);
                ++m_OngoingAsyncGPURequestsCount;
                return;
            }

            var width = renderTexture.width;
            var height = renderTexture.height;

            if (m_ReadbackTexture == null)
                m_ReadbackTexture = CreateReadbackTexture(width, height);

            var backupActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            m_ReadbackTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            m_ReadbackTexture.Apply();
            RenderTexture.active = backupActive;
            WriteFrame(m_ReadbackTexture);
        }

        private void ReadbackDone(AsyncGPUReadbackRequest r)
        {
            Profiler.BeginSample("BaseTextureRecorder.ReadbackDone");
            WriteFrame(r);
            Profiler.EndSample();
            --m_OngoingAsyncGPURequestsCount;
            if (m_OngoingAsyncGPURequestsCount == 0 && m_DelayedEncoderDispose)
                DisposeEncoder();
        }

        // <summary>
        // Prepares a frame before recording it. Callback is invoked for every frame during the recording session, before RecordFrame.
        // </summary>
        // <param name="ctx">The current recording session.</param>
        protected internal override void PrepareNewFrame(RecordingSession ctx)
        {
            base.PrepareNewFrame(ctx);
#if HDRP_ACCUM_API
            if (UnityHelpers.CaptureAccumulation(settings))
            {
                if (RenderPipelineManager.currentPipeline is HDRenderPipeline hdPipeline)
                {
                    hdPipeline.PrepareNewSubFrame();
                }
            }
#endif
        }

        /// <inheritdoc/>
        protected internal override void EndRecording(RecordingSession session)
        {
#if HDRP_ACCUM_API

            if (UnityHelpers.CaptureAccumulation(settings))
            {
                if (RenderPipelineManager.currentPipeline is HDRenderPipeline hdPipeline)
                {
                    // hdPipeline.EndRecording needs to be called before base.EndRecording because
                    // it will restore the Time.captureFrameRate
                    // this would otherwise override what needs to be done by base.EndRecording
                    hdPipeline.EndRecording();
                }
            }
#endif
            base.EndRecording(session);

            if (m_OngoingAsyncGPURequestsCount > 0)
            {
                Recording = true;
                m_DelayedEncoderDispose = true;
            }
            else
                DisposeEncoder();
        }

        private Texture2D CreateReadbackTexture(int width, int height)
        {
            return new Texture2D(width, height, ReadbackTextureFormat, false);
        }

        /// <summary>
        /// Writes the frame from an asynchronous GPU read request.
        /// </summary>
        /// <param name="r">The asynchronous readback target.</param>
        protected virtual void WriteFrame(AsyncGPUReadbackRequest r)
        {
            if (r.hasError)
            {
                ConsoleLogMessage("The rendered image has errors. Skipping this frame.", LogType.Error);
                return;
            }

            if (m_ReadbackTexture == null)
                m_ReadbackTexture = CreateReadbackTexture(r.width, r.height);
            Profiler.BeginSample("BaseTextureRecorder.LoadRawTextureData");
            m_ReadbackTexture.LoadRawTextureData(r.GetData<byte>());
            Profiler.EndSample();
            WriteFrame(m_ReadbackTexture);
        }

        /// <summary>
        /// Writes the frame from a Texture2D.
        /// </summary>
        /// <param name="t">The readback target.</param>
        protected abstract void WriteFrame(Texture2D t);

        /// <summary>
        /// Releases the encoder resources.
        /// </summary>
        protected virtual void DisposeEncoder()
        {
            UnityHelpers.Destroy(m_ReadbackTexture);
            Recording = false;
        }
    }
}
