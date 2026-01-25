# Requirements Workup

## Elicitation

1. Is the goal or outcome well defined? Does it make sense?

2. What is not clear from the given description?

| Area | Unclear Aspect | Why It Matters |
|------|----------------|----------------|
| Friends vs. Playgroups | Are these separate systems? Can you have friends outside playgroups? | Affects data model significantly |
| Game Data Source | External API (which one?) or manual entry? | Major architectural decision. **LM:** I just need to ask what the name of that one board game site is and we need to decide if its autopulling from their api, or if we just get their whole catalog. |
| Game Night Flow | Who creates it? How do selections work? Is there voting? | Core feature needs detail |
| Friend Discovery | How do users find each other? Username? Email? | Impacts user table and search. **LM:** I think Username. |
| Messaging | Mindmap shows "Message Friends" - is this in scope? | Could be huge scope creep |
| Trading | Mindmap shows trading feature - in scope or cut? | Needs explicit decision |
| Purchase Links | How do affiliate links work? Manual or automated? | Stretch goal needs definition |

3. How about scope? Is it clear what is included and what isn't?

4. What do you not understand?
    * Technical domain knowledge
    * Business domain knowledge

5. Is there something missing?

1. **Error states and edge cases**
   - What if a user tries to join a full game night?
   - What if someone leaves a playgroup mid-planning?
   - What if a game is removed from the catalog that users own?

2. **Moderation**
   - Can users report inappropriate reviews?
   - **LM:** Would be good, along with messages or even groups.
   - Who manages the game catalog?
   - **LM:** This goes back to how we pull from the API. I am personally leaning towards pull the whole database once and then update it every once in awhile?

3. **Privacy controls**
   - Can users hide their collection from non-friends?
   - **LM:** I think users should be able to hide their collection from EVERYONE, if they want.
   - Are playgroups public or invite-only?
   - **LM:** Preferably this is a toggle to allow each owner to decide.

4. **Data limits**
   - Max games in a collection?
   - **LM:** This probably isn't really an issue, but would be good to set it as SOMETHING, say 10,000.
   - Max members in a playgroup?
   - **LM:** 10? 20? Paywall it above a certain number? lol, or just set it at 500.
   - Max game nights per group?
   - **LM:** Another one I'm not sure is needed. Could be set at 7 per week, or even 14 per week, if people want multiple a day? I would want to ask what the value is in limiting it as opposed to just letting people have a lot, maybe they have a reason.

6. Get answers to these questions.

| # | Question | Impact if Unanswered |
|---|----------|---------------------|
| 1 | Are Friends and Playgroups separate systems? | Can't finalize data model. **LM:** Also would be good to settle on what to call these. |
| 2 | ~~Which game data API (if any)?~~ | ~~Blocks game catalog implementation~~ **LM:** Using the API from that one boardgame site, right? |
| 3 | Is messaging in scope for MVP? | Major feature creep risk. **LM:** I think this is totally doable, but shouldn't be the highest priority. |
| 4 | Is trading in scope for MVP? | Adds complexity to UserGames. **LM:** Trading does potentially get complex if we are talking about payment, or confirming people trade properly. But we could maybe just leave the risk up to the users and the honor system, and then allow people to review people they've traded with, and since its trading, not involve money or a payment system. |
| 5 | Web-only or responsive/mobile? | Affects UI framework choice |
| 6 | How do users find/add friends? | Affects User entity and search |
| 7 | What's the game night voting mechanism? | Core feature needs specification. **LM:** Also we may be talking about quite a few voting systems now. Rating/reviewing games, Rating traders, voting on game nights, possibly AI powered recommendations. |

---

## Analysis

Go through all the information gathered during the previous round of elicitation.

1. For each attribute, term, entity, relationship, activity... precisely determine its bounds, limitations, types and constraints in both form and function. Write them down.

2. Do they work together or are there some conflicting requirements, specifications or behaviors?

3. Have you discovered if something is missing?

4. Return to Elicitation activities if unanswered questions remain.

---

## Design and Modeling

Our first goal is to create a **data model** that will support the initial requirements.

1. Identify all entities; for each entity, label its attributes; include concrete types

2. Identify relationships between entities. Write them out in English descriptions.

3. Draw these entities and relationships in an _informal_ Entity-Relation Diagram.

4. If you have questions about something, return to elicitation and analysis before returning here.

---

## Analysis of the Design

The next step is to determine how well this design meets the requirements _and_ fits into the existing system.

1. Does it support all requirements/features/behaviors?
    * For each requirement, go through the steps to fulfill it. Can it be done? Correctly? Easily?

2. Does it meet all non-functional requirements?
    * May need to look up specifications of systems, components, etc. to evaluate this.
