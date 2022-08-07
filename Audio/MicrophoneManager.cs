using System;
using System.Linq;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace MultiplayerChat.Audio;

public class MicrophoneManager : MonoBehaviour, IInitializable, IDisposable
{
    [Inject] private readonly SiraLog _log = null!;
    [Inject] private readonly PluginConfig _config = null!;
    
    public const int TargetFrequency = 44100;
    
    public string? SelectedDeviceName { get; private set; }
    
    private int _minFreq;
    private int _maxFreq;
    private AudioClip? _captureClip;
    private AudioSource? _loopbackSource;

    public bool IsCapturing { get; private set; }
    public bool IsLoopbackTesting { get; private set; } 
    
    public bool HaveSelectedDevice => SelectedDeviceName != null;

    public void Initialize()
    {
        TryAutoSelectDevice();
    }

    public void Dispose()
    {
        if (_loopbackSource != null)
        {
            _loopbackSource.Stop();
        }
        
        if (_captureClip != null)
        {
            Destroy(_captureClip);
            _captureClip = null;
        }
    }

    public void Awake()
    {
        _loopbackSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
    }

    #region Device selection

    public bool AnyDevicesAvailable => Microphone.devices.Length > 0;
    
    public string[] AvailableDeviceNames => Microphone.devices;
    
    public string? DefaultDeviceName => Microphone.devices.FirstOrDefault();

    public bool TryAutoSelectDevice()
    {
        if (_config.MicrophoneDevice is not null && TrySelectDevice(_config.MicrophoneDevice))
            return true;

        if (DefaultDeviceName != null && TrySelectDevice(DefaultDeviceName))
            return true;
        
        _log.Error("No valid recording devices available, will not be able to speak");
        return false;
    }

    public bool TrySelectDevice(string? deviceName)
    {
        if (IsCapturing)
            StopCapture();
        
        if (string.IsNullOrEmpty(deviceName) || deviceName == "None")
        {
            SelectedDeviceName = null;
            _minFreq = 0;
            _maxFreq = 0;
            _log.Error("Recording device selection removed, will not be able to speak");
            return true;
        }
        
        if (!AvailableDeviceNames.Contains(deviceName))
        {
            _log.Error($"Requested device is not available: {deviceName}");
            return false;
        }

        SelectedDeviceName = deviceName;
        Microphone.GetDeviceCaps(deviceName, out _minFreq, out _maxFreq);
        _log.Info($"Selected recording device: {deviceName} (frequency={GetRecordingFrequency()})");
        return true;
    }
    
    public int GetRecordingFrequency()
    {
        if (_minFreq == 0 && _maxFreq == 0)
            // If these values are zero, any frequency should be supported
            return TargetFrequency;

        return Mathf.Clamp(TargetFrequency, _minFreq, _maxFreq);
    }
    
    #endregion

    #region Capture

    public void StartCapture()
    {
        StopCapture();

        if (!HaveSelectedDevice || SelectedDeviceName == "None")
            throw new InvalidOperationException("Cannot start capture without a selected device");

        _captureClip = Microphone.Start(SelectedDeviceName, true, 10, GetRecordingFrequency());
        
        _log.Info($"Start mic capture");
        
        IsCapturing = true;
    }

    public void StopCapture()
    {
        if (IsCapturing)
            _log.Info($"Stop mic capture");
        
        IsCapturing = false;
        IsLoopbackTesting = false;

        if (_loopbackSource != null)
        {
            _loopbackSource.loop = false;
            _loopbackSource.Stop();
            _loopbackSource.clip = null;
        }

        if (_captureClip != null)
        {
            Destroy(_captureClip);
            _captureClip = null;
        }
    }

    #endregion

    #region Loopback test

    public void StartLoopbackTest()
    {
        if (_loopbackSource is null)
            return;
        
        StartCapture();
        
        _loopbackSource.clip = _captureClip;
        _loopbackSource.loop = true;
        _loopbackSource.PlayDelayed(.1f);

        IsLoopbackTesting = true;
    }

    public void StopLoopbackTest() => StopCapture();

    #endregion
}