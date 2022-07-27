using System;
using UnityEngine;

/// <summary>
/// A class that represents the settings when capturing accumulations.
/// </summary>
[Serializable]
public class AccumulationSettings
{
    /// <summary>
    /// The type of Shutter Profile to use for the accumulation.
    /// </summary>
    public enum ShutterProfileType
    {
        /// <summary>
        /// Specifies the Shutter Profile as a trapezoid. The range indicates when the shutter remains fully open between linear opening and linear closing.
        /// </summary>
        Range = 0,
        /// <summary>
        /// Specifies the Shutter Profile as a curve through the selection of an animation curve.
        /// </summary>
        Curve
    }

    /// <summary>
    /// Indicates whether this Recorder is set to capture and accumulate multiple sub-frames or not.
    /// </summary>
    public bool CaptureAccumulation
    {
        get { return captureAccumulation; }
        set { captureAccumulation = value; }
    }

    [SerializeField] private bool captureAccumulation;


    /// <summary>
    /// The number of sub-frames to capture and accumulate on each final recorded frame.
    /// </summary>
    public int Samples
    {
        get { return samples; }
        set { samples = value; }
    }

    [SerializeField] private int samples = 1;

    /// <summary>
    /// The portion of the interval between two subsequent frames in which the shutter actually opens and closes according to the specified shutter profile. The value 1.0f applies the shutter profile to the whole interval between the two frames, while the value 0.0f disables the shutter and the accumulation. Any value in between proportionally rescales the shutter profile and makes the shutter remain closed for the rest of the interval to the next frame.
    /// </summary>
    public float ShutterInterval
    {
        get { return shutterInterval; }
        set { shutterInterval = value; }
    }

    [SerializeField] private float shutterInterval = 1.0f;

    /// <summary>
    /// The type of response profile that simulates a physical motion of camera shutter: either a range or an animation curve.
    /// </summary>
    public ShutterProfileType ShutterType
    {
        get { return shutterProfileType; }
        set { shutterProfileType = value; }
    }

    [SerializeField] private ShutterProfileType shutterProfileType;

    /// <summary>
    /// Stores an animation curve to use as the shutter profile.
    /// </summary>
    public AnimationCurve ShutterProfileCurve
    {
        get { return shutterProfileCurve; }
        set { shutterProfileCurve = value; }
    }

    [SerializeField] private AnimationCurve shutterProfileCurve = AnimationCurve.Constant(0, 1, 1);

    /// <summary>
    /// The time when the full open state of the shutter starts, normalized to the full shutter profile length. This automatically defines the slope of the linear opening from the profile start time.
    /// </summary>
    public float ShutterFullyOpen
    {
        get { return shutterFullyOpen; }
        set { shutterFullyOpen = value; }
    }

    [SerializeField] private float shutterFullyOpen = 0.25f;

    /// <summary>
    /// The time when the full open state of the shutter ends, normalized to the full shutter profile length. This automatically defines the slope of the linear closing to the profile end time.
    /// </summary>
    public float ShutterBeginsClosing
    {
        get { return shutterBeginsClosing; }
        set { shutterBeginsClosing = value; }
    }

    [SerializeField] private float shutterBeginsClosing = 0.75f;
}
