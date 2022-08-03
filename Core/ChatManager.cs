using System;
using BeatSaberMultiplayerChat.Network;
using MultiplayerCore.Networking;
using SiraUtil.Logging;
using Zenject;

namespace BeatSaberMultiplayerChat.Core;

// ReSharper disable once ClassNeverInstantiated.Global
public class ChatManager : IInitializable, IDisposable
{
    [Inject] private readonly SiraLog _log = null!;
    [Inject] private readonly PluginConfig _config = null!;
    [Inject] private readonly MpPacketSerializer _packetSerializer = null!;
    
    public void Initialize()
    {
        _packetSerializer.RegisterCallback<MpcCapabilitiesPacket>(HandleCapabilitiesPacket);
        _packetSerializer.RegisterCallback<MpcTextChatPacket>(HandleTextChat);
    }

    public void Dispose()
    {
        _packetSerializer.UnregisterCallback<MpcCapabilitiesPacket>();
        _packetSerializer.UnregisterCallback<MpcTextChatPacket>();
    }

    #region Packet handlers

    private void HandleTextChat(MpcTextChatPacket arg1, IConnectedPlayer arg2)
    {
        throw new NotImplementedException();
    }

    private void HandleCapabilitiesPacket(MpcCapabilitiesPacket arg1, IConnectedPlayer arg2)
    {
        throw new NotImplementedException();
    }
    
    #endregion
}