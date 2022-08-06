# Beat Saber Multiplayer Chat (PC)

💬 **Adds chat functionality to Beat Saber multiplayer lobbies.**

## Installation

### Requirements
- Beat Saber 1.24 or compatible
- [MultiplayerCore](https://github.com/Goobwabber/MultiplayerCore#readme) mod and its dependencies

### Download
[**👉 Download the latest release here**](https://github.com/roydejong/BeatSaberMultiplayerChat/releases/latest)

## Features

### Text chat
Users who have this mod installed can send and receive text messages in multiplayer lobbies.

Press the <kbd>💬</kbd> button in the lobby to open the chat box where you can see and type messages.

### Chat bubbles
Whenever someone sends a chat message in the lobby, a chat bubble will appear over their heads. You'll also see a smaller notification above the menu in the center.

### Notification sound
A notification sound is played whenever a chat message is received. You can customize the notification sound by placing your own `.ogg` files in the `Beat Saber\UserData\MultiplayerChat` directory.

Some default sounds come with the mod.

### Muting players
You can press the <kbd>🔇</kbd> mute button next someone in the players list, and you won't see any of their messages anymore.

Muted players are saved to the configuration file, so they'll stay muted between different lobbies.

You can also mute yourself here which, for now... does nothing. :)

## For modders

### Integrating chat features
If you use Zenject, you can depend on this mod and request the `ChatManager` instance (installed in the App container) to send messages and subscribe to events. 

### For server developers
You can send text chat messages using the `MpcTextChatPacket`. If the message originates from the connection owner, the mod will display it as coming from the server.

This can be useful for custom game modes, informing about entitlement issues, debugging, etc.

### Packet structure

#### Base packet (`MpcBasePacket`)
Each chat packet inherits from `MpcBasePacket`.

| Field             | Type      | Comment                                                                                                                                  |
|-------------------|-----------|------------------------------------------------------------------------------------------------------------------------------------------|
| `ProtocolVersion` | `VarUInt` | Protocol version. Currently always set to 1. Will be incremented if chat features change in a breaking way. See `MpcVersionInfo` class.  |

#### Capabilities packet (`MpcCapabilitiesPacket`)
This packet is sent to each player indicating that they have the mod installed, and which features they have enabled.

| Field                  | Type   | Comment                                                                                           |
|------------------------|--------|---------------------------------------------------------------------------------------------------|
| `CanTextChat`          | `Bool` | Indicates whether text chat is supported and enabled.                                             |
| `CanReceiveVoiceChat`  | `Bool` | Indicates whether voice chat* is supported and enabled.                                           |
| `CanTransmitVoiceChat` | `Bool` | Indicates whether voice chat* is supported and enabled, and a valid recording device is selected. |

[*] Voice chat coming soon. Maybe.

#### Text message packet  (`MpcTextChatPacket`)

| Field             | Type     | Comment                                                                                                                             |
|-------------------|----------|-------------------------------------------------------------------------------------------------------------------------------------|
| `Text`            | `String` | Raw chat message. Note: any HTML-style `<tags>` will be stripped from the message before it is displayed, to avoid rich text chaos. |