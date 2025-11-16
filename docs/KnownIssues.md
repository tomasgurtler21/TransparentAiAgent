# Known Issues

**Last Updated**: 2025-11-09

This document tracks known issues and limitations in the TransparentAiAgent project.

---

## LLM response streaming
- Streaming text is not rendered correctly in UI, it seems like start of each chunk is displayed, maybe triggered by their arrivals?

## Built-in tool available in Normal mode
-- Tools to control UI are meant for Teaching mode only, should not spam Normal mode context

## OpenAi LLM Response missing in Transparent events
-- Anthropic LLM responses are show n correctly in events, but OpenAi are not shown at all.

---

## Future Issues

Additional known issues will be documented here as they are discovered.

---

**See Also**:
- [UI Architecture](04-components/ui/architecture.md#known-limitations)
- [Troubleshooting Guide](05-guides/deployment/troubleshooting.md)
