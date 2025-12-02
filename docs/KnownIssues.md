# Known Issues

**Last Updated**: 2025-11-19

This document tracks known issues and limitations in the TransparentAiAgent project.

---

## Tools
- tools overlay UI needs refactor
- especially window with tool details (after click on tool) is really bad. Could it be pushed outside overlay to have more space?
- enable/disable tool should be available in Tools overlay. User preference should be saved to appsettings.

## System message refactor
- Does not reflect latest state of UI, should be updated.

## Configuration refactor unifinished
- Configuration overlay in app still writes to main app settings when changing context limit size.
- UserSetting are actually never created.

## Logging
- Only processed stream chunks are logged. When app fails to parse chunk, root cause is almost untraceable.

## Scenarios
- To stop scenario, stop button must be clicked twice
- Provider API erorrs are not displayed during scenarios
- Real tools might conflict with mock tools. Real tools should be removed and blocked during scenario execution.

---

## Future Issues

Additional known issues will be documented here as they are discovered.

---

**See Also**:
- [UI Architecture](04-components/ui/architecture.md#known-limitations)
- [Troubleshooting Guide](05-guides/deployment/troubleshooting.md)
