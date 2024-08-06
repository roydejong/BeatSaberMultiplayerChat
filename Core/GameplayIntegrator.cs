using System;
using BeatSaber.AvatarCore;
using MultiplayerChat.Audio;
using SiraUtil.Affinity;
using Zenject;

namespace MultiplayerChat.Core;

// ReSharper disable once ClassNeverInstantiated.Global
public class GameplayIntegrator : IInitializable, IDisposable, IAffinity
{
    [Inject] private readonly IMultiplayerSessionManager _sessionManager = null!;
    [Inject] private readonly MultiplayerPlayersManager _playersManager = null!;
    [Inject] private readonly VoiceManager _voiceManager = null!;
    
    public void Initialize()
    {
        _playersManager.playerSpawningDidFinishEvent += HandlePlayersSpawned;
        
        if (_playersManager.playerSpawningFinished)
            HandlePlayersSpawned();
    }

    public void Dispose()
    {
        _playersManager.playerSpawningDidFinishEvent -= HandlePlayersSpawned;
    }

    private void HandlePlayersSpawned()
    {
        foreach (var player in _sessionManager.connectedPlayers)
        {
            if (!_playersManager.TryGetConnectedPlayerController(player.userId, out var playerController))
                continue;

            var multiplayerGameAvatar = playerController.transform.Find("MultiplayerGameAvatar");
            _voiceManager.ProvideAvatarAudio(multiplayerGameAvatar.GetComponent<MultiplayerAvatarAudioController>());
        }
    }
}