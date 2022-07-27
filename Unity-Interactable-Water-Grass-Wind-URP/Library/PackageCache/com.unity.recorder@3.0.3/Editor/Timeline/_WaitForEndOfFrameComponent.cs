using System;
using UnityEngine;

namespace UnityEditor.Recorder.Timeline
{
    // Hack to make new a new MonoBehaviour discovery feature in the engine.
    // This is because there is a mismatched between the MB name and the file name, all inside an Editor assembly (double nono).
    class _WaitForEndOfFrameComponent{}

    [ExecuteInEditMode]
    class WaitForEndOfFrameComponent : _FrameRequestComponent
    {
        [NonSerialized]
        public RecorderPlayableBehaviour m_playable;

        public void LateUpdate()
        {
            RequestNewFrame();
        }

        protected override void FrameReady()
        {
            if (m_playable != null)
                m_playable.FrameEnded();
        }
    }
}
