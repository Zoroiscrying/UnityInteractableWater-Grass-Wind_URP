using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Recorder.Input
{
    /// <inheritdoc />
    /// <summary>
    /// Optional base class for image related inputs.
    /// </summary>
    public abstract class ImageInputSettings : RecorderInputSettings
    {
        /// <summary>
        /// Stores the output image width.
        /// </summary>
        public abstract int OutputWidth { get; set; }
        /// <summary>
        /// Stores the output image height.
        /// </summary>
        public abstract int OutputHeight { get; set; }

        /// <summary>
        /// Indicates if derived classes support transparency.
        /// </summary>
        public virtual bool SupportsTransparent
        {
            get { return true; }
        }

        /// <summary>
        /// This property indicates that the alpha channel should be grabbed from the GPU.
        /// </summary>
        public bool RecordTransparency { get; set; }
    }

    /// <inheritdoc />
    /// <summary>
    /// This class regroups settings required to specify the size of an image input using a size and an aspect ratio.
    /// </summary>
    [Serializable]
    public abstract class StandardImageInputSettings : ImageInputSettings
    {
        [SerializeField]
        OutputResolution m_OutputResolution = new OutputResolution();

        internal bool forceEvenSize;

        /// <inheritdoc />
        public override int OutputWidth
        {
            get { return ForceEvenIfNecessary(m_OutputResolution.GetWidth()); }
            set { m_OutputResolution.SetWidth(ForceEvenIfNecessary(value)); }
        }

        /// <inheritdoc />
        public override int OutputHeight
        {
            get { return ForceEvenIfNecessary(m_OutputResolution.GetHeight()); }
            set { m_OutputResolution.SetHeight(ForceEvenIfNecessary(value)); }
        }

        internal ImageHeight outputImageHeight
        {
            get { return m_OutputResolution.imageHeight; }
            set { m_OutputResolution.imageHeight = value; }
        }

        internal ImageHeight maxSupportedSize
        {
            get { return m_OutputResolution.maxSupportedHeight; }
            set { m_OutputResolution.maxSupportedHeight = value; }
        }

        int ForceEvenIfNecessary(int v)
        {
            if (forceEvenSize && outputImageHeight != ImageHeight.Custom)
                return (v + 1) & ~1;

            return v;
        }

        protected internal override void CheckForWarnings(List<string> warnings)
        {
            base.CheckForWarnings(warnings);

            if (OutputHeight > (int)maxSupportedSize)
                warnings.Add($"The image size exceeds the recommended maximum height of {(int)maxSupportedSize} px: {OutputHeight}");
        }

        protected internal override void CheckForErrors(List<string> errors)
        {
            base.CheckForErrors(errors);

            var h = OutputHeight;
            var w = OutputWidth;

            if (w <= 0 || h <= 0)
                errors.Add($"Invalid source image resolution {w}x{h}");
        }
    }
}
