using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;
using JoystickPack;

public class OnScreenSlider : OnScreenControl
{
    public VariableJoystick variableJoystick;

    private void FixedUpdate()
    {
        Vector2 direction = new Vector2(variableJoystick.Horizontal, variableJoystick.Vertical);
        Debug.Log(direction);
        SendValueToControl(direction);
    }

    [InputControl(layout = "Stick")]
    [SerializeField]
    private string _controlPath = "<Gamepad>/leftStick";

    protected override string controlPathInternal
    {
        get => _controlPath;
        set => _controlPath = value;
    }
}