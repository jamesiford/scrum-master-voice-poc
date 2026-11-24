# Teams rehearsal checklist

A person ticks this after the automated gate run (`tools/GateRunner`). It rehearses the live Teams path that the Simulated Room cannot cover. The brief called this the 10-step rehearsal from DEMO.md; DEMO.md's "Real Teams meeting" section has six numbered steps and a list of things worth showing, so the ten steps below are those, in order, with the live asks made explicit. Record the date, build id and who ran it at the bottom.

- [ ] 1. Create a Teams meeting in the demo tenant and copy the join link.
- [ ] 2. Live console: pick a prepared session, paste the link, press **Start in Teams**. Admit "Zava Scrum Agent" from the lobby. Status reaches Joining, Lobby, Connected, Live.
- [ ] 3. Roster members who join are matched by name (a Teams user called "System Administrator" matches the roster member "Admin"). The standup begins about 8 seconds after the first match.
- [ ] 4. The agent shares the standup board as its screen. Every participant sees it without doing anything. Share status shows as Bridge events.
- [ ] 5. Mid-update, ask: "Scrum Agent, is that one overdue? What was the due date?" The agent answers from the sprint data and returns to the speaker.
- [ ] 6. On your turn ask: "What am I supposed to report on?" The agent lists your items in priority order.
- [ ] 7. A colleague says "Daniel stepped out, he'll be back in a few minutes". Daniel is skipped and circled back to at the end.
- [ ] 8. Stay silent when called. The agent records present-but-no-response, not absent.
- [ ] 9. Talk over the agent while it is speaking. It stops within about half a second, does not finish the sentence, and acknowledges you on its next turn. Then correct yourself ("scratch that, I meant ...") and confirm the removed blocker is missing from the follow-up.
- [ ] 10. When the standup ends, approve one follow-up and reject another in the portal. Azure DevOps shows the "Approved by ..." comment on the approved item and nothing on the rejected one. Someone who said "I'll give Admin's update" still needed Admin's approval.

| Date | Build id | Run by | All ten ticked |
| --- | --- | --- | --- |
|  |  |  |  |
