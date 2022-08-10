# TODOs

### Voice

- [ ] Push-to-talk / mic toggle
- [ ] Spatial playback: gameplay avatar heads
- [ ] Activity/mute indicator - self
- [ ] Activity indicator - connected player head
- [ ] Voice optimization: packet / buffer pooling

### Etc

- [ ] Text chat roles / dev colors

### Polish / minor bugs

- [ ] Leaving MpEx settings flow coordinator will break the chat title button (activation event never triggers for the lobby setup view?)
- [ ] Chat view will lag if *left open only* with lots of messages piling up; older game objects might need to be removed (cap to messages buffer size).
- [ ] Allow players that don't send caps to be muted anyway? Or if they send voice: automatically assume they have voice caps?

### Future

- [ ] Per-player volume settings