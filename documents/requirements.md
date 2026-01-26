# Requirements Workup

## Elicitation

### 1. Is the goal or outcome well defined? Does it make sense?

**Yes, reasonably well defined.** The vision statement is clear: a platform for game discovery, collection management, and social coordination. The differentiator (social planning over pure cataloging) is stated.

However, the *success criteria* are undefined:
- What makes this "done" for the two-term scope?
- What's the MVP vs. nice-to-have?

### 2. What is not clear from the given description?

| Area | Unclear Aspect | Why It Matters |
|------|----------------|----------------|
| Friends vs. Playgroups | Are these separate systems? Can you have friends outside playgroups? | Affects data model significantly |
| Game Data Source | External API (which one?) or manual entry? | Major architectural decision. **LM:** I just need to ask what the name of that one board game site is and we need to decide if its autopulling from their api, or if we just get their whole catalog. |
| Game Night Flow | Who creates it? How do selections work? Is there voting? | Core feature needs detail |
| Friend Discovery | How do users find each other? Username? Email? | Impacts user table and search. **LM:** I think Username. |
| Messaging | Mindmap shows "Message Friends" - is this in scope? | Could be huge scope creep |
| Trading | Mindmap shows trading feature - in scope or cut? | Needs explicit decision |
| Purchase Links | How do affiliate links work? Manual or automated? | Stretch goal needs definition |

### 3. How about scope? Is it clear what is included and what isn't?

**Partially clear.** The Features doc separates Public/Registered/Playgroup/Stretch features, which helps. But there are inconsistencies:

**In Mindmap but NOT in Features doc:**
- Message Friends
- Feature to Trade games
- Contact Us page
- "Where to purchase" (shown as core, but listed as stretch in Features)

**In Features doc but NOT in Mindmap:**
- Password recovery
- Session management details

**Needs explicit scope decision:**
- Is this web-only or also mobile?
- Real-time features (live updates when friends add games)?
- Notifications (email? in-app? none?)

### 4. What do you not understand?
    * Technical domain knowledge
    - What does "recommendation logic based on reviews" actually mean algorithmically?

    * Business domain knowledge
    - How do board game complexity ratings work?
    - What metadata is essential for a game entry vs. nice-to-have?
    - How do real game nigh coordination flows work in practice? 

### 5. Is there something missing?

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

### 6. Get answers to these questions.

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

This section refines and formalizes the information gathered during the elicitation phase. Each major concept in the system is analyzed to determine its bounds, constraints, behaviors, and interactions with other components. Potential conflicts, missing requirements, and unresolved questions are also addressed.

### 1. Bounds, limitations, types, and constraints

**Users and Accounts**  
A User represents a registered individual with an authenticated account. Each user has a unique username used for discovery and social interactions, as well as credentials used for authentication. Authentication is required for all non-public features, and password recovery is supported. Users may participate in multiple social and organizational structures simultaneously.

**Friends (1–1 social connections)**  
Friendships represent mutual, one-to-one social connections between users. A friendship must be accepted by both parties and is required for direct (non-playgroup) messaging. Duplicate or one-sided friendships are not allowed, and a user may have many friends.

**Playgroups**  
Playgroups are multi-user entities created by a user (the owner). Owners can invite and manage members. Users may belong to multiple playgroups at the same time. Playgroups are invite-only by default, with an optional public visibility toggle controlled by the owner.

**Game Catalog**  
Games are stored in a global catalog sourced from an external board game database and stored locally. The catalog is initially imported in full and periodically updated. Users cannot delete games from the global catalog; they can only associate games with their personal collections.

**User Collections**  
A user’s collection is represented as a relationship between a user and a game. Each game can appear at most once per user. Collection entries include a status (such as owned or wishlist) and optional metadata like notes or play count. Users can control collection visibility, including fully hiding their collection from all other users.

**Reviews and Ratings**  
Users may leave reviews and ratings for games. Reviews are tied to both the user and the game and may be constrained to one review per user per game. Ratings are numeric and bounded by application rules. Moderation mechanisms (such as reporting inappropriate reviews) may be required.

**Game Nights**  
Game nights are events created within a playgroup by a single organizer. Playgroup members can RSVP to game nights. Game selection follows a hybrid approach: games may be suggested, optionally voted on, and finalized by the organizer. Game nights have defined states (open, finalized, canceled).

**Messaging**  
Messaging exists in two forms:
- Direct messaging between users, which requires an accepted friendship.
- Playgroup messaging, which allows members to communicate within a playgroup.
Messaging is text-only in scope for the initial implementation to limit complexity.

**Trading**  
Trading is supported as a lightweight coordination feature. Trade requests occur between users and do not involve payments, escrow, or enforcement. Trades rely on an honor system and may optionally support post-trade ratings. This feature is intentionally constrained to avoid excessive complexity.

---

### 2. Conflicts and inconsistencies

Several inconsistencies identified during elicitation have been resolved:

- Messaging and trading, which appeared in earlier brainstorming artifacts, are confirmed to be in scope but intentionally limited.
- Purchase links remain a stretch feature and are not required for the core system.
- Administrative and moderation features are acknowledged but deferred to later iterations unless required for basic safety.

No remaining requirements directly conflict with one another.

---

### 3. Missing or implicit requirements

The analysis phase revealed several areas requiring explicit consideration:

- Privacy controls must be consistently enforced across collections, playgroups, and messaging.
- Edge cases such as users leaving playgroups mid-planning or games becoming unavailable in the external catalog must be handled gracefully.
- Moderation and reporting mechanisms should exist at a basic level, even if tooling is minimal.

---

### 4. Return to elicitation

All core elicitation questions have been answered sufficiently to proceed with design and modeling. Remaining questions (such as the exact voting algorithm or recommendation logic) can be addressed during later design or implementation iterations.

---

## Design and Modeling

The goal of this section is to define an initial data model that supports the analyzed requirements and behaviors. The design focuses on clarity, extensibility, and alignment with the system’s scope.

### 1. Identified entities and attributes

The primary entities in the system include:

- **User**: represents an account holder with authentication credentials and profile information.
- **Friendship**: represents a mutual social connection between two users.
- **Playgroup**: represents a group of users organized for planning gameplay.
- **PlaygroupMember**: represents membership and role information for users within a playgroup.
- **Game**: represents a board game in the global catalog.
- **UserGame**: represents a user’s relationship to a game (collection entry).
- **Review**: represents a user’s rating and written feedback for a game.
- **GameNight**: represents a scheduled gameplay event within a playgroup.
- **GameNightRSVP**: represents a member’s attendance response to a game night.
- **GameNightCandidate**: represents games proposed for a specific game night.
- **GameNightVote**: represents a user’s vote for a proposed game.
- **DirectMessage**: represents private messages between friends.
- **PlaygroupMessage**: represents messages within a playgroup.
- **TradeRequest**: represents a proposed trade between users.
- **TradeItem**: represents individual games involved in a trade.

Each entity includes appropriate identifiers, timestamps, and foreign keys to enforce referential integrity.

---

### 2. Relationships between entities

- A user may own many games through collection entries; each collection entry links one user to one game.
- Users may be connected to other users through friendships.
- Users may create and belong to multiple playgroups.
- Playgroups contain members and host game nights.
- Game nights belong to a playgroup and are created by a user.
- Game nights have RSVPs, candidate games, and optional votes from playgroup members.
- Users may send direct messages to friends and messages within playgroups.
- Trade requests occur between users and reference specific games in their collections.

---

### 3. Informal Entity–Relationship Diagram (textual)

```text
User ──< UserGame >── Game ──< Review >── User

User ──< Friendship >── User

User (owner) ──< Playgroup ──< PlaygroupMember >── User

Playgroup ──< GameNight ──< GameNightRSVP >── User
                     └──< GameNightCandidate >── Game
                     └──< GameNightVote >── (User, Game)

User ──< DirectMessage >── User
Playgroup ──< PlaygroupMessage >── User

User ──< TradeRequest >── User
TradeRequest ──< TradeItem >── UserGame

---

## Analysis of the Design

The next step is to determine how well this design meets the requirements _and_ fits into the existing system.

1. Does it support all requirements/features/behaviors?
    * For each requirement, go through the steps to fulfill it. Can it be done? Correctly? Easily?

2. Does it meet all non-functional requirements?
    * May need to look up specifications of systems, components, etc. to evaluate this.
