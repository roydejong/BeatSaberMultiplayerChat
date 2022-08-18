using System;
using Libraries.HM.HMLib.VR;
using MultiplayerChat.Audio;
using MultiplayerChat.Config;
using MultiplayerChat.Models;
using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.XR;
using Zenject;

namespace MultiplayerChat.Core;

public class InputManager : MonoBehaviour, IInitializable, IDisposable
{
    [Inject] private readonly SiraLog _log = null!;
    [Inject] private readonly PluginConfig _config = null!;
    [Inject] private readonly VoiceManager _voiceManager = null!;
    [Inject] private readonly HapticFeedbackController _hapticFeedback = null!;
    [Inject] private readonly SoundNotifier _soundNotifier = null!;

    private static InputDevice? _leftController;
    private static InputDevice? _rightController;

    private bool _triggerConditionActive;
    private bool _debugKeyIsDown;

    private readonly HapticPresetSO _hapticPulsePreset;

    /// <summary>
    /// If enabled, the trigger will activate loopback/test mode rather than regular voice transmission.
    /// </summary>
    public bool TestMode;

    public event Action? OnActivation;
    public event Action? OnDeactivation;

    public InputManager()
    {
        _hapticPulsePreset = CreateHapticPreset();
    }

    public void Initialize()
    {
        _leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        _rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        InputDevices.deviceConnected += HandleInputDeviceConnected;
        InputDevices.deviceDisconnected += HandleInputDeviceDisconnected;

        PreloadSounds();

        _triggerConditionActive = false;
        _debugKeyIsDown = false;

        TestMode = false;
        gameObject.SetActive(false); // we'll be activated on session start
    }

    public void Dispose()
    {
        InputDevices.deviceConnected -= HandleInputDeviceConnected;
        InputDevices.deviceDisconnected -= HandleInputDeviceDisconnected;

        _triggerConditionActive = false;
        _debugKeyIsDown = false;

        TestMode = false;
    }

    public void OnEnable()
    {
        _triggerConditionActive = false;
        _debugKeyIsDown = false;
    }

    public void OnDisable()
    {
        if (TriggerIsActive)
            TriggerDeactivate();
        
        _debugKeyIsDown = false;
    }

    public void Update()
    {
        if (_config.DebugKeyboardMicActivation)
        {
            // KeyDown/KeyUp is on the frame it is pressed/released
            if (Input.GetKeyDown(KeyCode.V))
                _debugKeyIsDown = true;
            if (Input.GetKeyUp(KeyCode.V))
                _debugKeyIsDown = false;
        }
        
        var wasActive = _triggerConditionActive;
        var isActive = CheckTriggerCondition();

        if (wasActive && !isActive)
        {
            // Trigger release
            _triggerConditionActive = false;
            HandleKeybindUp();
        }
        else if (!wasActive && isActive)
        {
            // Trigger down
            _triggerConditionActive = true;
            HandleKeybindDown();
        }
    }

    #region Activation

    private bool TriggerIsActive => _voiceManager.IsTransmitting || _voiceManager.IsLoopbackTesting;

    private void TriggerActivate()
    {
        if (!_config.EnableVoiceChat)
            return;

        if (_config.MicrophoneDevice == "None")
            return;

        if (TestMode)
            _voiceManager.StartLoopbackTest();
        else if (!_voiceManager.StartVoiceTransmission())
            return;

        PlayActivationEffect();

        OnActivation?.Invoke();
    }

    private void TriggerDeactivate()
    {
        if (!TriggerIsActive)
            return;

        _voiceManager.StopLoopbackTest();
        _voiceManager.StopVoiceTransmission();

        PlayDeactivationEffect();

        OnDeactivation?.Invoke();
    }

    #endregion

    #region Keybind activation

    private void HandleKeybindDown()
    {
        switch (_config.VoiceActivationMode)
        {
            case VoiceActivationMode.Toggle:
            {
                // Toggle 
                if (TriggerIsActive)
                    TriggerDeactivate();
                else
                    TriggerActivate();
                break;
            }
            case VoiceActivationMode.Hold:
            {
                // Hold - start
                if (!TriggerIsActive)
                    TriggerActivate();
                break;
            }
        }
    }

    private void HandleKeybindUp()
    {
        switch (_config.VoiceActivationMode)
        {
            case VoiceActivationMode.Hold:
            {
                // Hold - release
                if (TriggerIsActive)
                    TriggerDeactivate();
                break;
            }
        }
    }

    private void PlayActivationEffect()
    {
        PlayMicOnSound();

        if (_config.VoiceKeybindController is VoiceKeybindController.Either or VoiceKeybindController.Left)
            HapticPulse(XRNode.LeftHand);

        if (_config.VoiceKeybindController is VoiceKeybindController.Either or VoiceKeybindController.Right)
            HapticPulse(XRNode.RightHand);
    }

    private void PlayDeactivationEffect()
    {
        PlayMicOffSound();

        if (_config.VoiceKeybindController is VoiceKeybindController.Either or VoiceKeybindController.Left)
            HapticPulse(XRNode.LeftHand);

        if (_config.VoiceKeybindController is VoiceKeybindController.Either or VoiceKeybindController.Right)
            HapticPulse(XRNode.RightHand);
    }

    #endregion

    #region Device management

    private void HandleInputDeviceConnected(InputDevice device)
    {
        if (!device.isValid)
            return;
        
        _log.Debug($"Input device connected: {device.name}");

        if ((device.characteristics & InputDeviceCharacteristics.HeldInHand) == 0 ||
            (device.characteristics & InputDeviceCharacteristics.Controller) == 0)
            // Not a handheld controller
            return;

        if ((device.characteristics & InputDeviceCharacteristics.Left) != 0)
            _leftController = device;

        if ((device.characteristics & InputDeviceCharacteristics.Right) != 0)
            _rightController = device;
    }

    private void HandleInputDeviceDisconnected(InputDevice device)
    {
        _log.Debug($"Input device disconnected: {device.name}");

        if (device == _leftController)
            _leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        if (device == _rightController)
            _rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    #endregion

    #region Device states

    private bool CheckTriggerCondition()
    {
        if (_config.DebugKeyboardMicActivation && _debugKeyIsDown)
            return true;

        switch (_config.VoiceKeybindController)
        {
            case VoiceKeybindController.Either:
                return CheckTriggerConditionOnDevice(_leftController) ||
                       CheckTriggerConditionOnDevice(_rightController);
            case VoiceKeybindController.Left:
                return CheckTriggerConditionOnDevice(_leftController);
            case VoiceKeybindController.Right:
                return CheckTriggerConditionOnDevice(_rightController);
            default:
                return false;
        }
    }

    private bool CheckTriggerConditionOnDevice(InputDevice? device)
    {
        switch (_config.VoiceKeybind)
        {
            case VoiceKeybind.PrimaryButton:
                return GetInputButtonIsDown(device, CommonUsages.primaryButton);
            case VoiceKeybind.SecondaryButton:
                return GetInputButtonIsDown(device, CommonUsages.secondaryButton);
            case VoiceKeybind.Trigger:
                return GetInputValueThreshold(device, CommonUsages.trigger, .85f);
            case VoiceKeybind.StickPress:
                return GetInputButtonIsDown(device, CommonUsages.primary2DAxisClick);
            default:
                return false;
        }
    }

    private static bool GetInputButtonIsDown(InputDevice? inputDevice, InputFeatureUsage<bool> usage)
    {
        if (inputDevice is null || !inputDevice.Value.isValid)
            return false;

        return inputDevice.Value.TryGetFeatureValue(usage, out var value) && value;
    }

    private static bool GetInputValueThreshold(InputDevice? inputDevice, InputFeatureUsage<float> usage,
        float threshold)
    {
        if (inputDevice is null || !inputDevice.Value.isValid)
            return false;

        return inputDevice.Value.TryGetFeatureValue(usage, out var value) && value >= threshold;
    }

    private static bool GetInputValueVector(InputDevice? inputDevice, InputFeatureUsage<Vector2> usage,
        float? minX = null, float? minY = null, float? maxX = null, float? maxY = null)
    {
        if (inputDevice is null || !inputDevice.Value.isValid)
            return false;

        if (!inputDevice.Value.TryGetFeatureValue(usage, out var value))
            return false;

        Console.WriteLine($"vector2 = {value.x}, {value.y}");
        
        if (minX.HasValue && value.x < minX.Value)
            return false;
        if (minY.HasValue && value.y < minY.Value)
            return false;
        if (maxX.HasValue && value.x > maxX.Value)
            return false;
        if (maxY.HasValue && value.y > maxY.Value)
            return false;

        return true;
    }

    #endregion

    #region Sound effects

    private void PreloadSounds()
    {
        _soundNotifier.LoadClipIfNeeded("MicOn");
        _soundNotifier.LoadClipIfNeeded("MicOff");
    }

    private void PlayMicOnSound() => _soundNotifier.LoadAndPlayPreview("MicOn");

    private void PlayMicOffSound() => _soundNotifier.LoadAndPlayPreview("MicOff");

    #endregion

    #region Haptics

    private HapticPresetSO CreateHapticPreset()
    {
        var hapticPulsePreset = ScriptableObject.CreateInstance<HapticPresetSO>();
        hapticPulsePreset._continuous = false;
        hapticPulsePreset._duration = .1f;
        hapticPulsePreset._frequency = .25f;
        hapticPulsePreset._strength = .5f;
        return hapticPulsePreset;
    }

    public void HapticPulse(XRNode node) =>
        _hapticFeedback.PlayHapticFeedback(node, _hapticPulsePreset);

    #endregion

    #region Settings

    public string DescribeKeybindConfig()
    {
        if (!_config.EnableVoiceChat)
            return "Voice chat is disabled";

        string keybindVerb;
        switch (_config.VoiceActivationMode)
        {
            case VoiceActivationMode.Hold:
                keybindVerb = "Hold";
                break;
            case VoiceActivationMode.Toggle:
            default:
                keybindVerb = "Press";
                break;
        }

        string keybindDescr;
        switch (_config.VoiceKeybind)
        {
            case VoiceKeybind.PrimaryButton:
                keybindDescr = "the primary button";
                break;
            case VoiceKeybind.SecondaryButton:
                keybindDescr = "the secondary button";
                break;
            case VoiceKeybind.Trigger:
                keybindDescr = "the trigger";
                break;
            case VoiceKeybind.StickPress:
                keybindDescr = "the joystick down";
                break;
            default:
                keybindDescr = "(Unknown button)";
                break;
        }

        string controllerText;
        switch (_config.VoiceKeybindController)
        {
            default:
            case VoiceKeybindController.Either:
                controllerText = "on either controller";
                break;
            case VoiceKeybindController.Left:
                controllerText = "on the left controller";
                break;
            case VoiceKeybindController.Right:
                controllerText = "on the right controller";
                break;
        }

        switch (_config.VoiceActivationMode)
        {
            case VoiceActivationMode.Hold:
                return $"{keybindVerb} {keybindDescr} {controllerText} to speak";
            case VoiceActivationMode.Toggle:
                return $"{keybindVerb} {keybindDescr} {controllerText} to unmute/mute";
            default:
                return "(Unknown keybind config)";
        }
    }

    #endregion
}