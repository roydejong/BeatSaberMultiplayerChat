# TODOs
Core things are done, these are mostly ideas / things to explore on how to improve further.

### Bugz

- [ ] Interactable on range controls in settings BROKEN

### Text

- [ ] Text chat roles / dev colors

### Voice

- [ ] Activity indicator above heads
- [ ] Volume gain

### VoIP Improvements

- [ ] Optimization: Move encode/decode to their own threads, off Unity main?

### Polish / minor bugs

- [ ] Leaving MpEx settings flow coordinator will break the chat title button (activation event never triggers for the lobby setup view?)
- [ ] Chat view will lag if *left open only* with lots of messages piling up; older game objects might need to be removed (cap to messages buffer size).
- [ ] Allow players that don't send caps to be muted anyway? Or if they send voice: automatically assume they have voice caps?
- [ ] Can we move chat settings into mp lobby? (or at least make em openable from there?)