# Debugging Recorders

The Recorder includes tools that can help you diagnose problems you may encounter during captures.

## Recorder GameObjects

The Recorder adds a **Unity-RecorderSessions** GameObject to Scenes to help manage capture sessions. The **Unity-RecorderSessions** GameObject has components that bind Recorders to GameObjects in your scene, and store the progress of the current recording session.

By default, Unity hides the **Unity-RecorderSessions** GameObject, but you can make it visible in the [Hierarchy window](https://docs.unity3d.com/Manual/Hierarchy.html). This is useful for debugging. For example, if a Recorder is not working properly, you can toggle the **Unity-RecorderSessions** GameObject on to make sure that Unity creates it properly when you launch the recording session.

To toggle the **Unity-RecorderSessions** GameObject's visibility:

- From Unity's main menu, select **Window > General > Recorder > Options > Show Recorder GameObject**.

## Recording in Verbose mode

If your Recorders are not working as expected, you can activate Verbose mode to get diagnostic information about the recording (for example, the recording's start and end time).

Verbose mode logs information to the [Console window](https://docs.unity3d.com/Manual/Console.html), which is  useful when you want to troubleshoot.

To toggle Verbose mode:

- From Unity's main menu, select **Window > General > Recorder > Options > Verbose Mode**.
