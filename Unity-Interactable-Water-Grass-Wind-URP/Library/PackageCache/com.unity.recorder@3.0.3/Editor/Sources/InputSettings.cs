using System;
using System.Collections.Generic;

namespace UnityEditor.Recorder
{
    /// <summary>
    /// Base class that represents a RecorderSetting Input that can be recorded from. (like a Camera, a RenderTexture...)
    /// </summary>
    [Serializable]
    public abstract class RecorderInputSettings
    {
        protected internal abstract Type InputType { get; }

        [Obsolete("Please use methods CheckForErrors() and CheckForWarnings()")]
        protected internal virtual bool ValidityCheck(List<string> errors)
        {
            return true;
        }

        protected internal virtual void CheckForWarnings(List<string> warnings) {}
        protected internal virtual void CheckForErrors(List<string> errors) {}
    }
}
