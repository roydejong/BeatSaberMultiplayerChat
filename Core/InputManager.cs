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
    [Inject] private readonly PluginConfig _pluginConfig = null!;
    [Inject] private readonly VoiceManager _voiceManager = null!;
    [Inject] private readonly HapticFeedbackController _hapticFeedback = null!;
    [Inject] private readonly SoundNotifier _soundNotifier = null!;

    private static InputDevice? _leftController;
    private static InputDevice? _rightController;

    private bool _triggerConditionActive;

    private readonly HapticPresetSO _hapticPulsePreset;

    public InputManager()
    {
        _hapticPulsePreset = ScriptableObject.CreateInstance<HapticPresetSO>();
        _hapticPulsePreset._continuous = false;
        _hapticPulsePreset._duration = .1f;
        _hapticPulsePreset._frequency = .25f;
        _hapticPulsePreset._strength = .5f;
    }
    
    public void Initialize()
    {
        _leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        _rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        InputDevices.deviceConnected += HandleInputDeviceConnected;
        InputDevices.deviceDisconnected += HandleInputDeviceDisconnected;

        PreloadSounds();

        _triggerConditionActive = false;
        
        gameObject.SetActive(false); // we'll be activated on session start
    }

    public void Dispose()
    {
        InputDevices.deviceConnected -= HandleInputDeviceConnected;
        InputDevices.deviceDisconnected -= HandleInputDeviceDisconnected;

        _triggerConditionActive = false;
    }

    public void OnEnable()
    {
        _triggerConditionActive = false;
    }

    public void Update()
    {
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

    #region Keybind activation

    private void HandleKeybindDown()
    {
        switch (_pluginConfig.VoiceActivationMode)
        {
            case VoiceActivationMode.Toggle:
            {
                // Toggle 
                if (_voiceManager.IsTransmitting)
                {
                    if (_voiceManager.StopVoiceTransmission())
                        PlayDeactivationEffect();
                }
                else
                {
                    if (_voiceManager.StartVoiceTransmission())
                        PlayActivationEffect();
                }
                break;
            }
            case VoiceActivationMode.Hold:
            {
                // Hold - start
                if (!_voiceManager.IsTransmitting)
                {
                    if (_voiceManager.StartVoiceTransmission())
                        PlayActivationEffect();
                }
                break;
            }
        }
    }

    private void HandleKeybindUp()
    {
        switch (_pluginConfig.VoiceActivationMode)
        {
            case VoiceActivationMode.Hold:
            {
                // Hold - release
                if (_voiceManager.IsTransmitting)
                {
                    if (_voiceManager.StopVoiceTransmission())
                        PlayDeactivationEffect();
                }
                break;
            }
        }
    }

    private void PlayActivationEffect()
    {
        PlayMicOnSound();
        
        if (_pluginConfig.VoiceKeybindController is VoiceKeybindController.Either or VoiceKeybindController.Left)
            HapticPulse(XRNode.LeftHand);
        
        if (_pluginConfig.VoiceKeybindController is VoiceKeybindController.Either or VoiceKeybindController.Right)
            HapticPulse(XRNode.RightHand);
    }

    private void PlayDeactivationEffect()
    {
        PlayMicOffSound();
        
        if (_pluginConfig.VoiceKeybindController is VoiceKeybindController.Either or VoiceKeybindController.Left)
            HapticPulse(XRNode.LeftHand);
        
        if (_pluginConfig.VoiceKeybindController is VoiceKeybindController.Either or VoiceKeybindController.Right)
            HapticPulse(XRNode.RightHand);
    }

    #endregion

    #region Device management

    private void HandleInputDeviceConnected(InputDevice device)
    {
        if (!device.isValid)
            return;

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
        _log.Info($"Input device disconnected: {device.name}");

        if (device == _leftController)
            _leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        if (device == _rightController)
            _rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    #endregion

    #region Device states
    
    private bool CheckTriggerCondition()
    {
        switch (_pluginConfig.VoiceKeybindController)
        {
            case VoiceKeybindController.Either:
                return CheckTriggerCondition(_leftController) || CheckTriggerCondition(_rightController);
            case VoiceKeybindController.Left:
                return CheckTriggerCondition(_leftController);
            case VoiceKeybindController.Right:
                return CheckTriggerCondition(_rightController);
            default:
                return false;
        }
    }

    private bool CheckTriggerCondition(InputDevice? device)
    {
        switch (_pluginConfig.VoiceKeybind)
        {
            case VoiceKeybind.PrimaryButton:
                return GetInputButtonIsDown(device, CommonUsages.primaryButton);
            case VoiceKeybind.SecondaryButton:
                return GetInputButtonIsDown(device, CommonUsages.secondaryButton);
            case VoiceKeybind.StickPress:
                return GetInputButtonIsDown(device, CommonUsages.primary2DAxisClick);
            case VoiceKeybind.Trigger:
                return GetInputValueThreshold(device, CommonUsages.trigger, .85f);
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
    
    private static bool GetInputValueThreshold(InputDevice? inputDevice, InputFeatureUsage<float> usage, float threshold)
    {
        if (inputDevice is null || !inputDevice.Value.isValid)
            return false;

        return inputDevice.Value.TryGetFeatureValue(usage, out var value) && value >= threshold;
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

    public void HapticPulse(XRNode node)
    {
        _hapticFeedback.PlayHapticFeedback(node, _hapticPulsePreset);
    }

    #endregion
}