# Beat Saber Multiplayer Chat (PC)

💬 **Adds Text Chat and Voice Chat to Beat Saber multiplayer lobbies.**

## Installation

### Requirements
- Beat Saber 1.24 or compatible
- [MultiplayerCore](https://github.com/Goobwabber/MultiplayerCore#readme) mod and its dependencies

### Download
[**👉 Download the latest release here**](https://github.com/roydejong/BeatSaberMultiplayerChat/releases/latest)

## Features
The chat features work on all official and unofficial servers, but only with other players that have the mod installed as well. Have fun, and remember: be nice. 😊

### Text chat
Press the <kbd>💬</kbd> button in the lobby to open the chat box where you can see and type messages.

#### Chat bubbles
Whenever someone sends a text message in the lobby, a chat bubble will appear over their heads. You'll also see a smaller notification above the menu in the center.

#### Notification sounds
A notification sound is played whenever someone sends a chat message. 

You can change or disable the notification sound via the Mod Settings. You can add your own sounds by placing `.ogg` files in `Beat Saber\UserData\MultiplayerChat`.

### Voice chat
Players who have this mod installed can talk in multiplayer lobbies and hear others.

When other players speak, you'll hear spatial audio coming from their avatar's head.

#### Settings
You can select and test your recording device and set up your Push-to-Talk keybind in the Mod Settings. I recommend reviewing your settings there before heading into a lobby.

### Muting players
You can press the <kbd>🔇</kbd> mute button next someone in the players list. If a player is muted, you won't see their chat messages or hear their voice anymore.

Muted players are saved to the configuration file, so they'll stay muted between sessions.

### Mod Settings

You can change the mod's settings ingame (Main Menu → <kbd>⚙️</kbd> → Mod Settings → Multiplayer Chat):
- Enable/disable text chat
- Change your notification sound
- Enable/disable voice chat
- Select and test your recording device for voice

## Notice
⚠️ **Text and voice chat communications using this mod are not encrypted.**

Communication packets are not end-to-end encrypted, which means the server can always read them.

While your connection to the server itself is encrypted, a modded client will not verify server certificates which makes it vulnerable to man-in-the-middle attacks.

You should not rely on this mod for secure communications.

## For modders

### For mod developers
If you use Zenject, you can depend on this mod and request the `ChatManager` instance (installed in the App container) to send and receive text messages.

### For server developers
You can send text chat messages using the `MpcTextChatPacket`. If the message originates from the connection owner, the mod will display it as coming from the server with some special formatting.

Sending or broadcasting text messages could be useful for custom game modes, providing players with instructions, debugging, etc.

### Packet structure

#### Base packet (`MpcBasePacket`)
Each chat packet inherits from `MpcBasePacket`.

| Field             | Type      | Comment                                                                                                                                  |
|-------------------|-----------|------------------------------------------------------------------------------------------------------------------------------------------|
| `ProtocolVersion` | `VarUInt` | Protocol version. Currently always set to 1. Will be incremented if chat features change in a breaking way. See `MpcVersionInfo` class.  |

#### Capabilities packet (`MpcCapabilitiesPacket`)
Reliable packet sent to each player indicating that they have the mod installed, and specifically which features they have enabled. Could be sent as an update when already connected.

| Field                  | Type   | Comment                                                                                          |
|------------------------|--------|--------------------------------------------------------------------------------------------------|
| `CanTextChat`          | `Bool` | Indicates whether text chat is supported and enabled.                                            |
| `CanReceiveVoiceChat`  | `Bool` | Indicates whether voice chat is supported and enabled.                                           |
| `CanTransmitVoiceChat` | `Bool` | Indicates whether voice chat is supported and enabled, and a valid recording device is selected. |

#### Text message packet  (`MpcTextChatPacket`)

Reliable packet containing a simple text chat message.

| Field             | Type     | Comment                                                                                                                             |
|-------------------|----------|-------------------------------------------------------------------------------------------------------------------------------------|
| `Text`            | `String` | Raw chat message. Note: any HTML-style `<tags>` will be stripped from the message before it is displayed, to avoid rich text chaos. |

#### Voice fragment packet  (`MpcVoicePacket`)

Unreliable packet containing a Opus-encoded voice fragment.

| Field             | Type              | Comment                                                                                                                      |
|-------------------|-------------------|------------------------------------------------------------------------------------------------------------------------------|
| `Data`            | `BytesWithLength` | Opus-encoded audio fragment (48kHz, 1 channel). If the array has a length of zero, this indicates the end of a transmission. |