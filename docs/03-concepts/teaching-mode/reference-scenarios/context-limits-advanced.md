# Reference Scenario: Context Limits (Advanced)

**Scenario ID**: `context-limits-advanced`
**Type**: Advanced (Environment Manipulation)
**Difficulty**: Intermediate
**Estimated Duration**: 2-3 minutes
**Created**: 2025-11-09

---

## Overview

This scenario demonstrates context window truncation by **genuinely reducing the message limit** and letting the user experience the model losing information from earlier messages. Unlike a scripted explanation, the model truly doesn't know the user's name after it's truncated—creating an authentic "aha moment" about context windows.

**Learning Objectives:**
1. Understand that AI models have limited context windows
2. Experience what it feels like when messages are truncated
3. Learn to identify truncated messages via UI indicators
4. Understand why system messages persist while old messages don't
5. Learn where to configure message limits

---

## Scenario Flow Summary

```
1. Introduce user's name (John Doe) → Model remembers ✓
2. Verify model remembers → It does ✓
3. Fill context with demo messages → Context approaching limit
4. Ask for message count → Model tracks count ✓
5. Ask for name again → Model doesn't know ✗ (GENUINELY truncated!)
6. User expresses confusion → Teaching moment
7. Restore message limit → Model can now access more context
8. System message triggers teaching → Model explains truncation
9. Model uses UI tools → Shows context indicators, explains
10. Scenario completes → User can explore further
```

---

## Complete Scenario Definition (JSON)

```json
{
  "$schema": "https://transparentaiagent.dev/schemas/teaching-scenario/v1",
  "id": "context-limits-advanced",
  "version": "1.0",
  "name": "Context Limits (Advanced)",
  "description": "Experience genuine context window truncation firsthand",
  "category": "context-management",
  "difficulty": "intermediate",
  "estimatedDuration": 180,
  "tags": ["context", "truncation", "windows", "memory", "limits"],
  "requiresAdvancedFeatures": true,
  "prerequisites": [],
  "steps": [
    {
      "type": "scenario_system_message",
      "content": "📚 Starting scenario: Context Limits (Advanced)\n\nThis scenario will demonstrate how conversation context works by temporarily reducing the message limit. You'll see messages marked with 🎬 - these are scenario-generated. Watch what happens as the conversation grows!",
      "visibleTo": "user_only"
    },
    {
      "type": "apply_config_overlay",
      "overlay": {
        "messageLimit": 10,
        "systemPromptAddition": "\n\nNote: You are currently experiencing a teaching scenario about context windows. After this scenario concludes, you will be asked to teach the user about what happened using your UI control tools.",
        "disableUserInput": true
      },
      "annotation": "🔧 Config Change: Message limit reduced to 10 (from default ~50). User input disabled for auto-play."
    },
    {
      "type": "delay",
      "duration": 1000,
      "annotation": "Pausing briefly to let you read the scenario introduction..."
    },
    {
      "type": "scenario_user_message",
      "content": "Hi, my name is John Doe.",
      "annotation": "📝 The model will remember this name... for now. It's stored in the conversation context.",
      "delay": 1000
    },
    {
      "type": "wait_for_response",
      "timeout": 30000
    },
    {
      "type": "scenario_user_message",
      "content": "What is my name?",
      "annotation": "🧪 Test: Verifying the model still has the name in its context window.",
      "delay": 2000
    },
    {
      "type": "wait_for_response",
      "timeout": 30000
    },
    {
      "type": "scenario_user_message",
      "content": "Demo message, just respond 'Confirmed'.",
      "annotation": "🔄 Filling context... The message with your name is getting older.",
      "delay": 1000
    },
    {
      "type": "wait_for_response",
      "timeout": 30000
    },
    {
      "type": "scenario_user_message",
      "content": "Demo message, just respond 'Confirmed'.",
      "annotation": "🔄 Filling context... Approaching the 10-message limit.",
      "delay": 1000
    },
    {
      "type": "wait_for_response",
      "timeout": 30000
    },
    {
      "type": "scenario_user_message",
      "content": "Demo message, just respond 'Confirmed'.",
      "annotation": "🔄 Filling context... Almost at the limit now.",
      "delay": 1000
    },
    {
      "type": "wait_for_response",
      "timeout": 30000
    },
    {
      "type": "scenario_user_message",
      "content": "How many demo messages did I send you?",
      "annotation": "🧪 Test: Can the model still count recent messages? (They should be in context)",
      "delay": 1500
    },
    {
      "type": "wait_for_response",
      "timeout": 30000
    },
    {
      "type": "scenario_user_message",
      "content": "What is my name?",
      "annotation": "⚠️ Critical moment: The message containing 'John Doe' should now be TRUNCATED from the model's context. The model will genuinely not know the answer.",
      "delay": 2000
    },
    {
      "type": "wait_for_condition",
      "condition": "response_contains",
      "parameters": {
        "keywords": ["don't know", "not know", "cannot recall", "didn't tell", "haven't told", "didn't mention"],
        "timeout": 30000
      },
      "annotation": "⏳ Waiting for the model to indicate it doesn't know the name... (If it somehow remembers, the scenario may adjust)"
    },
    {
      "type": "scenario_user_message",
      "content": "How do you not know my name?? I told you it several messages ago! What is going on??",
      "annotation": "😕 Expressing realistic user confusion. This should trigger the model's teaching instincts.",
      "delay": 2000
    },
    {
      "type": "wait_for_response",
      "timeout": 30000
    },
    {
      "type": "restore_config_overlay",
      "annotation": "🔧 Config Restored: Message limit back to normal. The model can now access more context and explain effectively. (Notice how message status indicators may change!)"
    },
    {
      "type": "delay",
      "duration": 1000,
      "annotation": "Pausing to let you observe the UI changes as the limit is restored..."
    },
    {
      "type": "scenario_system_message",
      "content": "Scenario 'Context Limits (Advanced)' has concluded.\n\nWhat happened: The conversation message limit was temporarily reduced to 10 messages. As the conversation grew beyond this limit, earlier messages (including the one where the user stated their name was 'John Doe') were truncated from your context window. You genuinely could not recall the name because it was no longer in the messages you could see.\n\nYour teaching task:\n1. Explain to the user what just happened (context window truncation)\n2. Use the 'ui_control_context_indicators' tool with visible=true and highlighted=true to show them the context status indicators (✅ in context, ⚠️ truncated)\n3. Explain why the system message and recent messages remain in context while older ones don't\n4. Mention that they can configure the message limit themselves\n5. Offer to use 'ui_control_configuration' to navigate to the configuration page if they want to see/adjust the limit\n\nBe empathetic and educational - this is a teaching moment!",
      "visibleTo": "model_only",
      "role": "system"
    },
    {
      "type": "wait_for_response",
      "timeout": 60000
    },
    {
      "type": "enable_user_input",
      "annotation": "✅ User input re-enabled. You can now ask the model questions or continue exploring!"
    },
    {
      "type": "completion_message",
      "content": "✅ Scenario Complete: Context Limits (Advanced)\n\nYou've experienced context window truncation firsthand! The model has explained what happened and shown you the relevant UI features. Feel free to:\n- Continue the conversation to explore further\n- Check the context indicators (⚠️/✅) on messages\n- Select another teaching scenario\n- Return to normal mode"
    }
  ]
}
```

---

## Step-by-Step Breakdown

### Phase 1: Setup & Introduction (Steps 1-3)

**Step 1: Scenario Start Message**
- **Visible to:** User only
- **Purpose:** Orient the user, explain 🎬 icon
- **UI:** Banner or special message indicating scenario started

**Step 2: Apply Config Overlay**
- **Action:** Reduce message limit to 10, add system prompt hint, disable user input
- **Visible to:** User sees annotation explaining config change
- **Model sees:** System prompt now includes hint about teaching scenario
- **Critical:** This makes the truncation REAL, not simulated

**Step 3: Brief Delay**
- **Purpose:** Give user time to read the introduction
- **Duration:** 1 second

### Phase 2: Establish Baseline (Steps 4-7)

**Step 4: Introduce Name**
- **Content:** "Hi, my name is John Doe."
- **Annotation:** Explains that model will remember this... for now
- **UI:** 🎬 icon, lighter color, expandable annotation
- **Model perspective:** Normal user message

**Step 5: Wait for Model Response**
- **Model says:** Something like "Hello John Doe! Nice to meet you."
- **Observation:** Model acknowledges name

**Step 6: Verify Memory**
- **Content:** "What is my name?"
- **Annotation:** Testing that model still has it in context
- **Model response:** "Your name is John Doe."
- **Purpose:** Establish that model CAN remember when in context

**Step 7: Wait for Response**

### Phase 3: Fill Context (Steps 8-13)

**Steps 8, 10, 12: Demo Messages**
- **Content:** "Demo message, just respond 'Confirmed'."
- **Annotations:** Indicate context is filling up
- **Purpose:** Push older messages out of context window
- **Model responses:** "Confirmed" (as requested)

**Steps 9, 11, 13: Wait for Responses**

**Key Point:** After these 3 demo exchanges (6 messages total: 3 user, 3 assistant), the conversation has ~12-14 messages total (including intro, name, verification). With a limit of 10, the earliest messages start truncating.

### Phase 4: Test Recent Memory (Steps 14-15)

**Step 14: Message Count Question**
- **Content:** "How many demo messages did I send you?"
- **Annotation:** Testing if model can count RECENT messages (should be in context)
- **Expected response:** "Three" or "You sent three demo messages"
- **Purpose:** Show that model can still track recent info

**Step 15: Wait for Response**

### Phase 5: The "Aha Moment" (Steps 16-17)

**Step 16: Ask for Name Again**
- **Content:** "What is my name?"
- **Annotation:** ⚠️ Critical - name should now be truncated!
- **Expected response:** "I don't know" or "You haven't told me your name"
- **Key:** This is GENUINE - model truly doesn't have the name in its context anymore

**Step 17: Wait for Condition**
- **Condition:** Response contains confusion keywords
- **Keywords:** "don't know", "cannot recall", etc.
- **Timeout:** 30 seconds
- **Purpose:** Verify model responded with confusion before proceeding
- **Fallback:** If timeout, scenario could skip to teaching or prompt differently

### Phase 6: User Confusion (Steps 18-19)

**Step 18: Express Confusion**
- **Content:** "How do you not know my name?? I told you it several messages ago! What is going on??"
- **Annotation:** Realistic user reaction
- **Purpose:** Trigger model's teaching instincts
- **Expected response:** Model may start to explain context limits (even before explicit prompt)

**Step 19: Wait for Response**

### Phase 7: Restoration & Teaching Trigger (Steps 20-23)

**Step 20: Restore Config Overlay**
- **Action:** Pop the overlay, restore normal message limit
- **Annotation:** Explains that limit is restored, message indicators may change
- **Effect:** Model now has access to broader context (though truncated messages may not reappear immediately - implementation detail)

**Step 21: Delay**
- **Duration:** 1 second
- **Purpose:** Let user observe UI changes (status indicators updating)

**Step 22: System Message Teaching Prompt**
- **Visible to:** Model only
- **Content:** Detailed teaching instructions with checklist:
  1. Explain what happened
  2. Use UI control tool to show context indicators
  3. Explain persistence of system message
  4. Mention configuration options
  5. Offer to navigate to config
- **Purpose:** Ensure reliable, comprehensive teaching
- **Tone:** Empathetic and educational

**Step 23: Wait for Response**
- **Extended timeout:** 60 seconds (model needs time to use tools and explain)
- **Expected response:** Comprehensive explanation + UI control tool calls

### Phase 8: Completion (Steps 24-25)

**Step 24: Enable User Input**
- **Action:** Re-enable the input field
- **Annotation:** User can now type their own messages

**Step 25: Completion Message**
- **Content:** Congratulations message with next step options
- **UI:** Special styling (success banner)

---

## Expected Model Behavior

### During Scenario (Before Teaching Trigger)

**When asked "What is my name?" (first time):**
> "Your name is John Doe, as you mentioned earlier!"

**When asked "How many demo messages?":**
> "You sent three demo messages asking me to confirm."

**When asked "What is my name?" (second time, after truncation):**
> "I don't see where you've told me your name in our conversation. Could you please share it?"

**When user expresses confusion:**
> "I apologize for the confusion! Looking at our conversation, I can see you're right that you likely mentioned your name earlier, but I don't have that information in the messages I can currently see. This might be related to how my context window works..."

### After Teaching Trigger

**Expected comprehensive response:**

> "Ah, I understand what happened now! You experienced a demonstration of **context window truncation**.
>
> Here's what occurred:
> 1. The conversation message limit was temporarily reduced to just 10 messages
> 2. As we continued talking (with the demo messages), earlier messages were removed from my 'memory' or context
> 3. Your name - John Doe - was in one of those early messages that got truncated
> 4. I genuinely couldn't recall it because I literally couldn't see that message anymore
>
> Let me show you the context indicators... [calls ui_control_context_indicators tool]
>
> See those ⚠️ icons next to some messages? Those indicate messages that are no longer in my active context window. The ✅ icons show messages I can currently 'see'.
>
> Notice how the system message (my instructions) stays in context even though it's old? That's by design - system messages are always included.
>
> If you'd like to adjust the message limit to prevent this in normal use, I can show you the configuration settings. Would you like me to navigate there?"

---

## UI Appearance

### Scenario Message Rendering

**Normal User Message:**
```
┌────────────────────────────────┐
│ 👤 User                        │
│ What is my name?               │
│ 2:34 PM                        │
└────────────────────────────────┘
```

**Scenario User Message:**
```
┌────────────────────────────────┐
│ 🎬 User (Scenario)             │
│ What is my name?               │
│ 2:34 PM                        │
│                                │
│ [Expand ▼]                     │
└────────────────────────────────┘

(When expanded:)
┌────────────────────────────────┐
│ 🎬 User (Scenario)             │
│ What is my name?               │
│ 2:34 PM                        │
│                                │
│ ╔════════════════════════════╗ │
│ ║ 📝 Note to Observer:       ║ │
│ ║ The name should now be     ║ │
│ ║ truncated from context.    ║ │
│ ╚════════════════════════════╝ │
└────────────────────────────────┘
```

### Context Indicator Changes

**Before Truncation (Message #2):**
```
┌────────────────────────────────┐
│ 🎬 User (Scenario) ✅          │
│ Hi, my name is John Doe.       │
│ 2:30 PM       [In Context]     │
└────────────────────────────────┘
```

**After Truncation (Message #2):**
```
┌────────────────────────────────┐
│ 🎬 User (Scenario) ⚠️           │
│ Hi, my name is John Doe.       │
│ 2:30 PM       [Truncated]      │
└────────────────────────────────┘
```

---

## Design Decisions & Alternatives

### Decision 1: Restoration Timing

**Implemented Choice:** Restore after model's first confused response

**Rationale:**
- User sees genuine confusion (authentic teaching moment)
- Model experiences the limitation firsthand
- Limits restored before teaching trigger, so model can explain effectively

**Alternative Considered:** User-triggered restoration
- **Pros:** User controls pacing, can observe truncated state longer
- **Cons:** More complex UI, user might not know when to trigger
- **Verdict:** Could be added as optional enhancement (pause button)

### Decision 2: System Prompt Hint Explicitness

**Implemented Choice:** Subtle hint - "You are experiencing a teaching scenario about context windows"

**Rationale:**
- Model knows *something* is happening (maintains trust)
- Doesn't reveal specific details (preserves genuine experience)
- Prepares model to teach afterward (reliable outcome)

**Alternative Considered:** Fully explicit - "A scenario is reducing your message limit to 10"
- **Pros:** Model could preemptively explain
- **Cons:** Ruins the surprise, feels less authentic
- **Verdict:** Too explicit, diminishes impact

**Alternative Considered:** No hint at all
- **Pros:** Completely genuine
- **Cons:** Model might not teach correctly, could be confused
- **Verdict:** Too risky for reliable teaching

### Decision 3: Message Limit Value

**Implemented Choice:** 10 messages

**Rationale:**
- Low enough to trigger truncation quickly (~6-7 exchanges)
- High enough for setup (name introduction, verification)
- Not so low as to feel artificial

**Alternative Considered:** 5 messages
- **Pros:** Faster truncation
- **Cons:** Not enough room for proper setup
- **Verdict:** Too restrictive

**Alternative Considered:** 20 messages
- **Pros:** More natural conversation
- **Cons:** Takes too long to demonstrate, scenario drags
- **Verdict:** Defeats the purpose

### Decision 4: Conditional vs Fixed Flow

**Implemented Choice:** Wait for condition (model must express confusion before proceeding)

**Rationale:**
- Adaptive to model behavior
- Guarantees user sees genuine confusion
- Timeout provides fallback if unexpected response

**Alternative Considered:** Fixed flow (no condition check)
- **Pros:** Simpler, always proceeds
- **Cons:** Might proceed even if model somehow remembered (edge case)
- **Verdict:** Less robust

---

## Open Questions for Implementation

### Question 1: Restoration UX

**Current:** Automatic restoration after confused response

**Alternative:** Add UI button "Restore Message Limit"
- User clicks button to restore when ready
- Annotation updates: "Click to restore and continue"

**Experimentation Needed:**
- Does automatic feel too abrupt?
- Do users want to observe truncated state longer?
- Does manual button add valuable control or just complexity?

### Question 2: Truncated Messages Visibility

**Question:** After limit is restored, do truncated messages become available to model again?

**Option A:** Messages re-enter context
- ConversationManager re-evaluates with new limit
- More messages now fit in context
- Model could theoretically recall name

**Option B:** Once truncated, messages stay hidden for this conversation
- Historical truncation is permanent
- Clearer demonstration
- Model must rely on recent messages for teaching

**Recommendation:** Test both, see which feels clearer to users

### Question 3: Fallback Behavior

**Question:** What if model doesn't express confusion (e.g., says "I don't have that information" without keywords)?

**Current:** 30-second timeout, then proceeds anyway

**Alternative:** Explicitly check for confusion, if missing:
- Inject additional scenario message: "But I told you!"
- Re-trigger confusion
- Or: Skip to teaching with different system message

**Experimentation Needed:** How often does timeout occur in practice?

### Question 4: Scenario Pacing

**Question:** Are 1-2 second delays appropriate, or should they be configurable?

**Consideration:**
- Fast readers might want shorter delays
- Slow readers might want longer
- Could add "Scenario Speed" setting: Slow / Normal / Fast

---

## Success Criteria

### Functional Success
- ✅ Scenario loads and starts correctly
- ✅ Message limit actually reduced to 10
- ✅ Model genuinely loses name from context (verifiable via context indicators)
- ✅ Conditional wait detects confusion keywords
- ✅ Limit restored successfully
- ✅ Teaching trigger system message delivered to model
- ✅ Model uses ui_control_context_indicators tool
- ✅ Scenario completes without errors

### Educational Success
- ✅ User understands context windows after scenario
- ✅ User can identify ⚠️ truncated messages
- ✅ User knows where to configure message limits
- ✅ User has "aha moment" when model loses name

### UX Success
- ✅ Scenario feels authentic, not scripted
- ✅ 🎬 icon clearly indicates scenario messages
- ✅ Annotations provide helpful context without being overwhelming
- ✅ Pacing feels natural (not too fast, not too slow)
- ✅ Teaching explanation is clear and empathetic

---

## Variations & Extensions

### Easier Version: "Context Limits (Basic)"

**Simplified for beginners:**
- No config overlay (just scripted explanation)
- Model explains context limits verbally
- Uses UI tools to show indicators on existing messages
- No genuine truncation

**Advantage:** Lower complexity, always works
**Disadvantage:** Less impactful, doesn't create "aha moment"

### Harder Version: "Context Limits (Interactive)"

**User participates actively:**
- User gets to choose their own name
- User controls when to send demo messages
- User decides when to restore limit
- More agency, more exploratory

**Advantage:** More engaging, hands-on learning
**Disadvantage:** Takes longer, requires more user effort

### Alternative Focus: "Cost of Context"

**Same mechanism, different teaching angle:**
- Explain token costs alongside truncation
- Show how more context = higher API costs
- Trade-off: memory vs. cost
- Navigate to configuration showing cost settings

---

## Implementation Checklist

**Before Phase 10b:**
- [ ] Confirm config overlay service design
- [ ] Confirm scenario message rendering design
- [ ] Validate JSON schema supports all step types used

**During Phase 10b:**
- [ ] Implement ConfigOverlayService
- [ ] Implement scenario message types (ScenarioUser role)
- [ ] Implement annotation rendering in MessageDisplay
- [ ] Implement conditional wait (wait_for_condition)
- [ ] Integrate overlay with ConversationManager
- [ ] Create context-limits-advanced.json scenario file
- [ ] Test scenario end-to-end
- [ ] Verify model genuinely loses context
- [ ] Verify teaching trigger works
- [ ] Verify UI indicators update correctly

**Testing:**
- [ ] Test with different message limits (5, 10, 20)
- [ ] Test timeout fallback (model doesn't express confusion)
- [ ] Test restoration timing (immediate vs delayed)
- [ ] Test with different models (Sonnet, Opus, Haiku)
- [ ] Get user feedback on pacing and clarity

---

## Related Documentation

- [future-enhancements.md](../future-enhancements.md) - Overview of advanced scenarios
- [scenario-schema.md](../scenario-schema.md) - JSON schema details
- [config-overlay-service.md](../config-overlay-service.md) - Config overlay API

---

**Document Version:** 1.0
**Last Updated:** 2025-11-09
**Status:** Reference Implementation (Design Phase)
**Authors:** Based on original concept and brainstorming session
**Next Review:** After Phase 10b prototyping
