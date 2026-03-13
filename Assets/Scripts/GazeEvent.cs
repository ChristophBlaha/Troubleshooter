using UnityEngine;
using UnityEngine.Events;

public class GazeEventSphere : GazeInteractable
{

    [Header("Gaze Events")]
    public UnityEvent onGazeEnter;
    public UnityEvent onGazeFocused;
    public UnityEvent onGazeExit;

    protected override void OnGazeEnterCallback()
    {
        onGazeEnter?.Invoke();
    }

    protected override void OnGazeFocusedCallback()
    {
        onGazeFocused?.Invoke();
    }

    protected override void OnGazeExitCallback()
    {
        onGazeExit?.Invoke();
    }
}