# Known Issues

**Last Updated**: 2025-11-09

This document tracks known issues and limitations in the TransparentAiAgent project.

---

## Knowledge library tool topic unknown to LLM
- LLM does not know which topic are avaialable to querry. Available topic should be dynamically added to tool description

## UI Built-in tools available in Normal mode
- Tools to control UI (or to get its state) are meant for Teaching mode only, should not spam Normal mode context

## OpenAi LLM Response missing in Transparent events
- Anthropic LLM responses are shown correctly in events, but OpenAi are not shown at all.

## Configuration overlay in Teaching mode incorrect
- It shows system prompt from Normal mode, not Teaching mode. Most likely liked directly to appsettings json, but it should use services instead.

## Transparency overlay
- It is a mess. Way too many event types, should be reviewed and reduced to used ones only.
- Needs multi-filter, to show entries of several types at once, while filtering others.

## Tools
- tools overlay UI needs refactor
- especially window with tool details (after click on tool) is really bad. Could it be pushed outside overlay to have more space?
- enable/disable tool should be available in Tools overlay. User preference should be saved to appsettings.
- should respect Mode - no UI tools for Normal mode

## System message refactor
- Again a mess. Should have clean responsibility, no tools description, that belongs to tools.
- Maybe does not reflect latest state of UI, should be updated.

## Missing export of logs
- Transparent events shoudl have button to save it to json.
- Do not forget to add warning to user that messages content are part of logs, to respect privacy/security
---

## Future Issues

Additional known issues will be documented here as they are discovered.

---

**See Also**:
- [UI Architecture](04-components/ui/architecture.md#known-limitations)
- [Troubleshooting Guide](05-guides/deployment/troubleshooting.md)
