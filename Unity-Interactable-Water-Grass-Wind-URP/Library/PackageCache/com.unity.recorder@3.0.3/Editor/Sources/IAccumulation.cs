using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Interface to implement to allow the use of the Accumulation feature.
/// </summary>
public interface IAccumulation
{
    /// <summary>
    /// Returns the settings of the Accumulation feature.
    /// </summary>
    /// <returns>AccumulationSettings</returns>
    AccumulationSettings GetAccumulationSettings();
}
