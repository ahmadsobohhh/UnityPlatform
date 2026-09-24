# Imagine Quest Polished Game Master Plan

## Purpose and decision requested

This document is the build blueprint for turning Imagine Quest into a polished, classroom-ready educational adventure game. It is intentionally a plan, not an implementation commitment. The next implementation work should begin only after the product decisions in the **Approval Gate** section are confirmed.

The proposed direction is clear:

> Build **one exceptional, complete, teacher-controlled quest pipeline** before trying to build many worlds, games, currencies, or dashboards.

The first proof should be a 10–15 minute pirate adventure called **The Riddle of the Celestial Clock**. It starts aboard the Captain's ship, moves into a small explorable destination space, contains a teaching portal and an Arena practice activity, ends with a satisfying story payoff, and reports meaningful progress to the teacher. Every later world should be created through the same framework rather than requiring a new custom architecture.

## What we are making

Imagine Quest is not simply a pirate-themed math game. It is a reusable, story-led learning platform in which:

- The **ship and Captain** are the familiar narrative home for every quest.
- Each quest transports a student to a destination world: pirate waters, an ancient city, a forest, underwater ruins, a space station, a future city, or a storybook realm.
- Students learn through exploration, meaningful interaction, puzzle solving, practice games, feedback, and rewards.
- Teachers control the pace, concepts, assignments, accessibility settings, and classroom-facing rewards without needing to understand Unity.
- The platform remembers each student's work securely and shows the teacher evidence of progress and misconceptions.

The pirate setting is therefore the first high-quality chapter, not the entire product. The system must preserve the magic of a story while being data-driven enough to support future content.

## Product north star

At the end of a normal class session, a student should be able to say:

> “I helped the crew solve the storm, and I learned how to use fractions and angles to do it.”

At the same time, the teacher should be able to say:

> “I assigned the right activity in less than a minute, could see who needed help, and did not have to become a game designer to use it.”

The game is successful only when both statements are true. Beautiful art without a reliable teacher loop is not enough. Correct backend data without joy, story, and approachable feedback is not enough.

## Design principles

### 1. Story gives learning a purpose

Every challenge must have a believable place in the world. Fractions are not a detached worksheet; they are sail positions, cargo shares, a map divided into islands, or pieces of a star chart. Angles are not a menu prompt; they are compass bearings, cannon trajectories, telescope alignment, or clock hands. Algebra is not a final screen; it is the code that repairs a mechanism or opens the Captain's chest.

### 2. One coherent loop beats many disconnected features

The first release must make one loop delightful from beginning to end:

1. Teacher publishes an assignment.
2. Student enters through a cinematic story hook.
3. Student explores, learns, practices, and makes progress.
4. The game saves reliable evidence of what happened.
5. The teacher can understand the outcome and decide what to do next.

Only then should we add a second world, a second game island, multiple currencies, or complex customization.

### 3. Teachers control pacing; students retain agency

Teachers decide which concepts are available, when an assignment opens, and which students receive it. Students should still see future islands and locked doors as promises of upcoming adventures, not as empty or punitive restrictions. The interface should always explain what will unlock the content and what the student can do now.

### 4. Feedback teaches before it judges

Wrong answers should produce a useful next step: a visual hint, an explanation, a smaller example, an invitation to try a teaching portal, or a chance to reason aloud. The game should not rush to penalties, loss states, or public comparison. Attempts and misconceptions are teacher information, not student shame.

### 5. Security and privacy are features

Student performance, classroom behavior, rewards, and teacher actions are sensitive. A student client must never be trusted to grant itself XP, unlock content, alter Hearts, or impersonate teacher actions. Trusted backend operations, audit history, clear consent, and minimal student data are part of the product quality bar.

### 6. Make content reusable

Writers, teachers, and designers should eventually be able to create a new quest by assembling approved content blocks: cinematic, world, portal plan, lesson, question bank, practice game, reward policy, and debrief. A new quest should not require changing the core C# quest controller.

### 7. Accessibility is designed in, not added later

Every important action needs visual and non-visual feedback. Every cinematic needs captions. Every puzzle must work with the supported input modes. Players need readable text, reduced motion, volume controls, color-independent cues, pause/settings, and a safe way to recover if they get stuck.

## Current starting point

The current UnityPlatform already has a useful foundation:

- Firebase Authentication and Firestore are integrated.
- Students and teachers have distinct sign-in and routing flows.
- Students can create a character, join a class with a code, view classes, and enter a classroom lobby.
- Teachers can create classes, see a roster, and select a class.
- The current Pirate Quest route is class-aware: teachers can unlock it and students can launch it from a selected class.
- A first procedural pirate scene has a captain, an ocean/deck, three navigation math questions, a quest session, direct-video URL support, and a minimal saved progress record.
- The imported prototype offers reusable ideas for interaction prompts, puzzle locking, puzzle UI, chest/door/NPC interactions, pause/settings, XP feedback, sound, and visual effects.
- The frontend already has a unified pirate academy visual layer.

This is a prototype foundation, not yet a production game. The current quest is a fixed sequence of hard-coded questions and a binary unlock. The full vision still needs content-driven quests, portal types, a real world scene, reliable assignments, teacher reporting, authoritative rewards, an Arena, accessibility, and a security boundary.

## First polished vertical slice

### Working title

**Chronicles of the Mystic Marauder: The Riddle of the Celestial Clock**

### Student promise

A time storm is twisting the sky above the ship. The Captain needs the student to restore the Celestial Clock before the crew is lost between worlds. The student uses fractions to set the sails, angles to align a celestial compass, and algebra to repair the final clockwork lock.

### Learning scope

The exact grade band and curriculum must be approved before implementation. Until then, the default prototype scope is:

| Challenge | Story role | Math concept | Evidence captured |
|---|---|---|---|
| Sail chart | Direct the ship through the storm | Fractions and equivalent fractions | attempts, selected answer, hint use, completion |
| Compass observatory | Align the star route | Angles and bearings | attempts, answer reasoning, completion |
| Celestial Clock | Repair the storm engine | One-step algebraic reasoning | attempts, hint use, completion |

The final learning design must define the learning objective, acceptable solution strategies, hint sequence, misconception labels, difficulty range, and teacher-facing interpretation for every challenge.

### Session flow

1. **Class entry:** A student opens an active assignment from the Home Port or class lobby.
2. **Ship briefing:** A short captioned cinematic introduces the Captain, the storm, the destination, and the mission.
3. **Arrival:** The student enters a compact, beautiful exploration space. For the first vertical slice this can be a modular ship deck plus a storm-touched observatory or island outpost; it does not need to be a huge open world.
4. **Mission board:** The player sees a concise goal, current key count, accessible settings, and an optional recap.
5. **Teaching portal:** The player finds a portal or interactive object that explains the first idea through a short video, physical demonstration, or guided interaction.
6. **Knowledge check:** The student answers a low-stakes formative question to earn a key. Wrong answers reveal scaffolded help rather than a punishment.
7. **Practice portal:** The player enters an Arena island activity that practices the same concept with a clear game mechanic.
8. **Final sequence:** The player returns to the main world, uses earned keys/knowledge to solve the Celestial Clock, and receives a satisfying audiovisual payoff.
9. **Debrief:** The game celebrates effort, explains what was learned, shows any earned cosmetic or progress milestone, and provides a safe return to the Home Port.
10. **Teacher view:** The teacher can see completion, challenge status, attempts, hint use, and whether a student may need follow-up support.

### Definition of done for the first quest

The first polished quest is done only when a test student can start, pause, resume, complete, and safely return from it; a test teacher can assign and observe it; all state survives scene reload; the same behavior is secure under Firestore rules; and classroom playtesting confirms that students understand both the story and the math task.

## The world structure

### Home Port

The Home Port is the student's persistent landing space. It should replace an app-like collection of disconnected pages with a legible adventure home.

Core zones:

- **Captain's Quarters:** Active assignment, latest mission, story recap, and direct Continue button.
- **Quest Map:** Available destination worlds and completed voyages.
- **Arena Archipelago:** Visible islands for games and concepts. Locked islands show a friendly explanation such as “Your teacher will chart this island soon.”
- **Crew Profile:** Avatar, chosen character look, level, journal, accessibility/settings, and positive accomplishments.
- **Treasure Cabin:** Cosmetics and non-essential collections. Do not introduce paid mechanics or reward pressure.

The Home Port should prioritize the next useful action. A student should never wonder which of ten buttons matters today.

### Destination worlds

A destination world is a small, focused exploration environment, not an uncontrolled open world. Each contains:

- a visible mission goal;
- a short traversal path with optional discovery moments;
- one to three key interactables;
- teaching and practice portal entry points;
- checkpoint/save locations;
- readable landmarks that help students orient themselves;
- an end-state visual change after the quest is resolved.

Future world examples:

| World | Learning-friendly mechanical possibilities |
|---|---|
| Stormbound ship and observatory | fractions, angles, ratios, coordinate navigation |
| Mathwright Forge | measurement, integers, equations, patterns |
| Sunken archive | decimals, proportions, data interpretation |
| Ancient city of mirrors | geometry, symmetry, spatial reasoning |
| Clockwork future station | algebra, coding logic, rates of change |
| Storybook forest | number sense, probability, narrative word problems |

### Arena Archipelago

The Arena is a destination made of small islands, each hosting a reusable practice game. It should feel playful and explorable without making students navigate too far to reach an assigned activity.

Rules:

- An assignment portal launches the intended game directly.
- A student can revisit unlocked islands voluntarily.
- Locked islands remain visible but do not expose answer content.
- Every island has a clear concept, session length, learning objective, and feedback model.
- An island game reports structured events to the same progress pipeline as a quest challenge.

Initial island candidates:

| Island | Core loop | First concept fit |
|---|---|---|
| Compass Cay | Rotate a compass and choose route cards | angles and bearings |
| Fraction Fleet | Split cargo and set sail proportions | fractions and equivalence |
| Clockwork Cove | Repair gears with equation tiles | one-step algebra |
| Treasure Ledger | Balance trade crates | decimals and arithmetic |

## Player experience and interaction design

### Avatar and character identity

The current character selection is a good starting point. The polished version must carry that identity into gameplay through a coherent player avatar, portrait, color palette, or captain badge. A student should recognize their character in the Home Port, quest HUD, journal, and rewards.

Do not mix incompatible art styles by accident. The imported project contains detailed pirate characters, low-poly character assets, stylized props, and fantasy village assets. Choose one art direction for the player and nearby NPCs, then use other packs only where the style transition is deliberate.

### Interaction model

Use one Input System action map for interaction. The player approaches an object, gets a contextual prompt, and chooses to interact. The prototype's strongest reusable concept is a generic interaction layer:

- `IQuestInteractable` describes an object the student can use.
- `IChallenge` describes a learning interaction with a start, attempts, hints, completion, and cancel state.
- `PortalTrigger` routes a student to a teaching unit, practice game, or story event.
- `QuestNode` represents any step in a quest graph.

Avoid duplicate legacy input checks and global scene singletons. The game should have one modal/game-state service so pause, dialogue, video, puzzle UI, and input focus cannot fight one another.

### Challenge UX

Every challenge needs:

- a story reason for existing;
- concise spoken and written instructions;
- a visual model when appropriate;
- no trick wording;
- unlimited or teacher-configured retries in the learning mode;
- hints that gradually become more concrete;
- feedback that explains why rather than only saying correct/incorrect;
- a clear Exit/Back option when safe;
- cursor/input focus handling;
- a saved checkpoint before and after important progress.

### Companion system

The fairy companion is a later phase, but its hook belongs in the event model now. The game should emit attempt, inactivity, hint, pause, and completion events. A later `StruggleDetector` can inspect these signals and request an appropriate companion response.

Initial companion behavior should be gentle and optional:

- “Want a clue?” rather than automatic interruption.
- Give a strategy or visual cue, not the answer.
- Never label a student as struggling or at-risk on-screen.
- Let teachers disable, adjust, or review automated hints.

## Teacher experience

### Teacher workflow goal

A teacher should publish a useful assignment in under one minute once the content library exists.

### Class setup workflow

1. Teacher creates a class.
2. The system generates a short, unambiguous join code.
3. Teacher shares the code; students join.
4. Teacher sees a privacy-appropriate roster with status, not a full sensitive profile dump.
5. Teacher can archive a class at the end of a term without destroying historical records prematurely.

### Assignment workflow

Replace the current single `isUnlocked` toggle with a true assignment object.

Teacher selects:

- quest/template;
- concept or curriculum outcome;
- target class or selected group of students;
- release time and optional due time;
- teaching portals and practice islands enabled for the assignment;
- hint/retry policy;
- reward policy;
- draft, publish, close, or archive state.

The teacher interface should show a mission card preview, not raw JSON or technical IDs.

### Teacher reporting workflow

The first dashboard should show only information that is actionable:

- student completion state;
- last activity time;
- challenge completion;
- attempt count and hint use;
- a simple “may need follow-up” signal that is explainable;
- a way to open a student detail view;
- a way to republish, extend, or close an assignment.

Avoid leaderboards, public failure indicators, or simplistic mastery labels. The teacher needs evidence, context, and control.

### Hearts and Powers policy

The vision includes Hearts, XP, Crystals, Gold, levels, Powers, and real-world privileges. These systems are motivating but high-risk in a classroom context. They must not enter the MVP as irreversible or automatic punishment.

Recommended staged approach:

| Stage | Include | Do not include yet |
|---|---|---|
| First quest | celebratory XP display and purely cosmetic milestones | Hearts, currency spending, real-world privileges |
| Pilot | teacher-approved XP and small cosmetic rewards with audit history | full resets, public comparisons, automatic deductions |
| Validated rollout | optional Hearts and Powers behind school/teacher policy settings | irreversible loss of all progress |

If Hearts are implemented, use a restorative model: reason required, teacher confirmation, audit log, private student view, recovery opportunities, and configurable limits. Do not silently wipe a student's work or privileges at zero Hearts.

## Content architecture

### The three content layers

The platform needs a distinction between reusable content, teacher assignments, and each student's actual run.

1. **Quest definitions** are versioned authored content: story, world, node graph, video URLs, portal setup, question bank, reward policy defaults, and presentation assets.
2. **Class assignments** select a specific quest definition/version and configure its release, audience, portal plan, and teacher settings.
3. **Learner runs** record what one student did in one assignment: state, checkpoints, challenge events, hints, attempts, outcomes, and server-approved rewards.

This separation lets a teacher assign the same quest to two classes without overwriting their results, lets content improve through versions, and lets a student's later attempt remain traceable to what they actually saw.

### Suggested content model

```text
questDefinitions/{questId}
  title, summary, status, currentVersionId, tags

questDefinitions/{questId}/versions/{versionId}
  story, worldSceneKey, nodeGraph, portalDefinitions,
  videoSequence, curriculumMetadata, rewardPolicy, contentVersion

classes/{classId}/assignments/{assignmentId}
  questId, versionId, state, targetAudience, releaseAt, dueAt,
  portalPlan, teacherSettings, createdBy, createdAt

users/{uid}/classProgress/{classId}/assignments/{assignmentId}
  runState, currentNodeId, checkpoints, completedNodes,
  challengeSummary, lastActivityAt, contentVersion

users/{uid}/classProgress/{classId}/assignments/{assignmentId}/events/{eventId}
  eventType, nodeId, attemptIndex, answerMetadata, hintLevel, occurredAt
```

Direct client writes should be limited to safe events or request documents. Server-side code validates events, updates rollups, and grants rewards. Avoid using only `questId` as a progress document ID; one quest can be assigned to more than one class or more than once.

### Question/content format

Each authored question should include more than text and a correct index:

```text
questionId
learningObjective
gradeBand
conceptTags
prompt
visualAssetKey
answerFormat
correctAnswer or acceptedStrategies
distractorMetadata
explanation
hintSequence
misconceptionTags
difficulty
accessibilityText
contentVersion
```

Content authors need a preview/test mode. Teachers should use approved templates rather than editing complex questions live in the first release.

## Technical architecture

### Unity application layers

Use explicit interfaces and services rather than a large global manager.

| Layer | Responsibility |
|---|---|
| Presentation | UI, HUD, animations, VFX, audio, camera, accessibility preferences |
| Quest flow | state machine, node transitions, checkpoint/resume, portal routing |
| Gameplay modules | exploration, interactables, mini-games, teaching units, question UI |
| Content | local cache, content definitions, version resolution, validation |
| Assignments | class-specific availability, release/close state, audience filtering |
| Progress | event capture, local resilience queue, run summaries, resume state |
| Rewards | requests to trusted backend, wallet display, reward feedback |
| Platform | Firebase Auth, Firestore, Storage, Cloud Functions, telemetry |

Core services to introduce:

- `QuestFlowController`
- `QuestSessionService`
- `ContentRepository`
- `AssignmentRepository`
- `LearnerProgressRepository`
- `PortalRouter`
- `VideoSequenceService`
- `ChallengeEventService`
- `RewardService`
- `TelemetryService`
- `AccessibilitySettingsService`

Services should communicate with typed events and state objects. A mini-game should report “challenge completed with these events,” not reach into Firestore, a HUD, and an XP balance directly.

### Scene architecture

Keep scenes small and purposeful:

| Scene category | Examples | Notes |
|---|---|---|
| Shell scenes | Welcome, Login, Home Port, Teacher Dashboard | persistent UI and navigation shell |
| Quest shell | Ship briefing, quest hub/world | loads content and owns the quest state machine |
| Modular world chunks | ship deck, observatory, arena island | use additive loading where it improves memory/performance |
| Reusable mini-game scenes/prefabs | Fraction Fleet, Compass Cay | launched with assignment/node context |
| Cinematic overlay | Video/caption sequence | returns cleanly to the previous quest state |

Do not generate production worlds, UI, and questions entirely in a single bootstrap script. Procedural bootstrapping was useful for proving the pirate route; the next step should be prefab-based scenes and data-bound content.

### Video strategy

Use externally hosted, directly streamable HTTPS media with stable URLs. Firestore stores metadata and the current content version; Firebase Storage or a suitable CDN provides actual media delivery. An ordinary YouTube watch URL is not a reliable Unity VideoPlayer source.

Every video sequence needs:

- direct stream URL;
- poster/fallback image;
- captions/subtitles;
- locale/version metadata;
- retry policy and clear failure behavior;
- skip/replay policy;
- audio level handling;
- event telemetry for started, completed, skipped, and failed;
- a clean handoff back to the quest state machine.

### Backend and security

Firebase can serve as the platform backend, but not through unrestricted client writes. The production design should include:

- Firebase Authentication with protected roles and teacher onboarding;
- Firestore with least-privilege Security Rules;
- Firebase Storage/CDN for media;
- callable Cloud Functions or a trusted server for protected operations;
- Firebase App Check;
- Emulator Suite tests for Auth, Firestore rules, Functions, and Storage;
- audit events for teacher actions and reward changes;
- monitoring, backups, and data-retention decisions.

Protected operations include:

- create/join/archive a class;
- publish, close, or revoke an assignment;
- validate a completion event;
- grant XP, Crystals, Gold, or cosmetic rewards;
- adjust Hearts;
- redeem or approve a Power;
- create prize-draw rollups;
- recursively archive/delete class data;
- write teacher-facing summaries.

Students may submit an event such as “I answered option B.” They must not directly set “I earned 500 XP,” “I completed this assignment,” or “this island is unlocked.”

### Reward ledger

Rewards should be append-only, idempotent ledger events. A current balance is a server-maintained rollup, not the only source of truth.

```text
walletLedger/{ledgerEventId}
  userId, classId, assignmentId, sourceEventId, currency,
  amount, reason, createdAt, createdBy, idempotencyKey
```

This makes bugs recoverable, reduces duplicate rewards, provides a teacher audit trail, and allows balancing changes to be evaluated fairly.

## Art direction and asset plan

### Visual direction

The intended mood is **warm cinematic pirate fantasy for a classroom**, not grim piracy and not a generic mobile-game skin. Use moonlit navy, teal ocean, parchment gold, warm lantern amber, hand-painted texture, clear silhouettes, and readable interaction cues.

Visual priorities:

- strong landmark readability;
- high contrast for interactables and objective markers;
- welcoming character animation and facial/body language;
- clear visual state changes after a puzzle is solved;
- menu/UI art that feels like the same world as the 3D scenes;
- limited visual noise around learning prompts.

### Existing asset use

Reuse selectively:

- pirate characters as Captain/NPC candidates;
- colonial ship and props as a starting point for a modular deck;
- sea chest/open-close animation for a reward interaction;
- existing water/VFX/audio as raw material after compatibility and licensing review;
- DOTween for tasteful movement and reward feedback.

Do not blindly merge the old prototype. It contains style collisions, duplicated input systems, duplicate reward paths, old Supabase code, and large raw art assets.

### Performance and content delivery

Asset imports are very large. Before building worlds, establish:

- target hardware and minimum frame rate;
- texture resolution limits and compression presets;
- LODs for ship/world assets;
- baked lighting/light-probe strategy;
- occlusion and scene chunking plan;
- audio compression/loading policy;
- Addressables/content-pack plan for future worlds;
- automated build-size report;
- asset license inventory.

## Audio, music, and feedback

Audio should reinforce learning and make the world feel alive:

- captain voice/voiceover with captions;
- a calm Home Port music bed;
- low-intensity exploration music;
- a subtle change when a portal is nearby;
- satisfying but non-overwhelming puzzle, key, chest, and reward sounds;
- gentle error sounds that do not shame;
- separate music, SFX, voice, and master sliders;
- no essential information delivered only through sound.

Avoid importing the prototype's old global `AudioManager` directly because its name conflicts with the platform's existing global manager and its mixer parameter needs correction. Establish one audio service and a single naming convention.

## Accessibility and safety requirements

### Accessibility baseline

- Captions for every cinematic and voiced instructional moment.
- Text scaling and readable font sizing.
- High-contrast mode or validated contrast ratios.
- Reduced-motion option for camera shakes, parallax, transitions, and VFX.
- Screen-space/visual cues plus audio cues for important events.
- Keyboard support and clear focus states.
- Controller support if controller is a target input method.
- Pause, settings, restart/checkpoint, and unstuck capabilities.
- No required precision movement for solving the learning objective.
- Color-independent symbols and labels.
- Time limits disabled by default or accommodated appropriately.

### Student wellbeing and classroom safety

- Never publicly rank students by failure, Hearts, or reward balance.
- Avoid loss mechanics that erase work or create humiliation.
- Let teachers use neutral, restorative wording for any classroom behavior tracking.
- Make hints and adaptive support private and optional where possible.
- Create an audit/appeal/recovery path for teacher-managed privileges.
- Confirm school/district privacy and parental-consent requirements before collecting detailed activity telemetry.

## Testing and quality plan

### Test layers

| Layer | What to verify |
|---|---|
| Unit tests | reward math, state transitions, content validation, score/hint rules |
| Edit-mode tests | data assets, quest graphs, scene references, migration/schema validity |
| Play-mode tests | enter/exit quest, portal interactions, save/resume, UI input, pause/settings |
| Firebase Emulator tests | Auth roles, Firestore Rules, Function authorization, idempotency |
| Integration tests | create class, join class, publish assignment, student completion, teacher rollup |
| Device tests | memory, frame rate, input, video playback, network failure paths |
| Playtests | comprehension, engagement, teacher workload, accessibility, age appropriateness |

The inherited prototype's test coverage is not enough for the merged platform. The highest priority missing tests are class transactions, assignment authorization, quest gate behavior, learner progress persistence, reward idempotency, and resume after interruption.

### Quality gates

No new quest should ship unless it has:

- curriculum review;
- content version and author/owner;
- interaction and question validation;
- captions and accessibility text;
- a happy path and hint/failure-path test;
- teacher preview;
- performance budget check;
- security rules/function test;
- analytics event schema;
- student playtest feedback.

## Delivery roadmap

The order below protects the core loop and prevents expensive rework.

### Stage 0 — Product alignment and safety decisions

**Goal:** Decide what the first real learning experience is before building more systems.

Deliverables:

- approved grade band and curriculum outcomes;
- target platforms and device constraints;
- 10–15 minute session target;
- definition of student success and teacher success;
- first quest story outline and storyboard;
- reward/Hearts/Powers policy decision;
- student data, privacy, and school-policy review;
- art direction board and asset license inventory.

Exit criteria: one written vertical-slice brief, one approved content outline, and no unresolved decision that could invalidate the data model.

### Stage 1 — Platform hardening

**Goal:** Make the existing platform safe and consistent enough to support real users.

Deliverables:

- unified student/teacher/class membership schema;
- removal or quarantine of legacy duplicate character/class routes;
- normalized class-code input;
- Firebase Rules, indexes, emulator configuration, and test fixtures;
- protected role model and teacher onboarding;
- Cloud Functions for class lifecycle and protected writes;
- reliable class archive/delete behavior;
- structured error, loading, retry, and offline UI states.

Exit criteria: two test accounts can create/join/view a class without client-side privilege escalation or orphaned records.

### Stage 2 — Assignment and progress foundation

**Goal:** Replace the binary pirate unlock with a reusable assignment engine.

Deliverables:

- quest definition/version schema;
- teacher assignment workflow with draft/publish/close;
- student assignment list and availability states;
- assignment-scoped learner run and checkpoint model;
- event collection and server-owned rollups;
- base teacher progress view;
- direct-media/caption metadata contract.

Exit criteria: a teacher can publish a specific version of a quest to one class and a student can resume their own run without overwriting another class's progress.

### Stage 3 — Gold-standard pirate quest

**Goal:** Make the first full student experience genuinely memorable.

Deliverables:

- cinematic ship briefing with captions and fallback;
- modular pirate deck/destination scene;
- mission journal, objective HUD, pause/settings, and accessibility options;
- one teaching portal, one formative check, one practice island, and one finale;
- challenge feedback/hint model;
- high-quality audio/VFX/reward feedback;
- save/resume checkpoints;
- post-quest debrief.

Exit criteria: student and teacher playtesters can complete the intended experience without a developer present and can explain what was learned.

### Stage 4 — Teacher dashboard and governance

**Goal:** Give teachers useful control without increasing workload.

Deliverables:

- roster/assignment dashboard;
- challenge-level completion and attempt summary;
- teacher action audit history;
- safe manual reward review tools;
- assignment templates and bulk actions;
- archive/term rollover workflow.

Exit criteria: a teacher can run a classroom session, interpret progress, and act on it in a few minutes.

### Stage 5 — Arena and reusable content tooling

**Goal:** Turn one quest into a platform.

Deliverables:

- Home Port and Arena Archipelago;
- two additional game islands;
- reusable portal and mini-game prefabs;
- content authoring templates/validation;
- preview mode for quest content;
- content publishing/versioning workflow;
- Addressables/content-delivery plan.

Exit criteria: a second quest or concept island can be created by configuring content rather than modifying the core quest code.

### Stage 6 — Progression, companion, and school-year features

**Goal:** Add long-term motivation only after core learning is validated.

Deliverables:

- server-owned XP ledger and cosmetic milestone system;
- carefully piloted Crystals/Gold if their use is approved;
- companion event hooks and optional adaptive hints;
- teacher-managed Powers pilot with audit/recovery;
- optional Hearts pilot only after policy/user research;
- prize-draw reporting if legally/ethically approved.

Exit criteria: economy and behavior systems have clear safeguards, teacher controls, recovery paths, and student-test evidence of positive impact.

### Stage 7 — Pilot, polish, and release readiness

**Goal:** Prove the product in real classroom conditions.

Deliverables:

- teacher/student pilot plan;
- onboarding and support materials;
- telemetry dashboard and incident handling;
- performance and build-size pass;
- accessibility review;
- content/video reliability testing;
- privacy, retention, and backup plan;
- launch checklist and rollback plan.

Exit criteria: a pilot class can use the experience repeatedly with acceptable performance, no critical security gaps, and documented improvements from playtest results.

## Prioritized implementation backlog

### P0 — Must exist before a real student pilot

- Firebase Rules and emulator tests.
- Cloud Function/transaction for class create, class join, and assignment publish.
- Assignment-scoped progress model.
- Server-owned reward/event validation.
- One content-driven quest graph.
- One teaching portal, one practice portal, and one mini-game.
- Save/resume and error recovery.
- Captions, pause/settings, readable UI, and basic accessibility support.
- Teacher assignment and progress view.
- Real student/teacher test accounts and test data cleanup process.

### P1 — Needed for a strong public demonstration

- Home Port overhaul.
- Arena Archipelago first island set.
- refined player avatar representation.
- polished audio/VFX/cinematic presentation.
- content authoring preview/validation.
- teacher templates, groups, and bulk assignment actions.
- structured learning analytics and misconception summaries.
- mobile/controller decision and support if required.

### P2 — Expand only after pilot evidence

- more worlds and game islands;
- adaptive fairy video overlays;
- cosmetics/store;
- Crystals/Gold economy;
- Powers/Hearts governed pilot;
- end-of-year prize draw;
- localization and broader curriculum bands.

## Risks and mitigations

| Risk | Why it matters | Mitigation |
|---|---|---|
| Scope explodes into many worlds before one works | Leaves a beautiful but incomplete demo | Lock the first 10–15 minute quest scope and use a stage gate |
| Client-side Firebase writes are trusted | Students can alter rewards/access | Rules, App Check, Functions, audit logs, emulator tests |
| Current progress key uses only quest ID | Results can overwrite across classes/assignments | assignment- and class-scoped runs with immutable IDs |
| Old Supabase code is merged | Conflicting auth/state/rewards and security risk | Do not import it; port concepts only into Firebase design |
| Multiple reward systems remain | Duplicate XP and confusing state | One event and server-owned ledger pipeline |
| Art packs clash and inflate build | Game feels inconsistent or performs poorly | art direction lock, license audit, LOD/texture budget, Addressables |
| Video source is unreliable | Cinematics fail in class | direct-stream URLs, captions, poster/fallback, retries, telemetry |
| Teacher tools become too complex | Teachers abandon the platform | default templates, limited first dashboard, user testing |
| Reward/Heart mechanics harm wellbeing | Equity and classroom-policy issue | delay, teacher controls, audit/recovery, restorative design research |
| Accessibility is deferred | Students are excluded and retrofits become costly | build requirements into every content template and acceptance test |

## Approval gate

Before implementation begins, confirm these decisions:

1. What grade band and exact math outcomes does the first quest teach?
2. Is **The Riddle of the Celestial Clock** the approved first quest title/theme, or should it remain **The Pirate Voyage**?
3. Which target platforms matter first: desktop Windows only, WebGL, mobile, or controller support?
4. What is the acceptable first-session duration: 10, 15, or 20 minutes?
5. Are real-world Powers and the Hearts system in scope for this capstone/pilot, or explicitly deferred?
6. Who will create/provide the first cinematic videos and captions, and where will direct-streaming media be hosted?
7. Does the teacher need a full curriculum authoring interface now, or only a curated assignment picker?
8. What school/privacy requirements apply to student accounts, activity analytics, and teacher behavior records?
9. Which art direction should the avatar follow: stylized low-poly, detailed pirate, or a new coherent character set?
10. What does success look like in the first pilot: engagement, completion, learning gain, teacher time saved, or a combination?

## Documentation reviewed

This plan is based on the project-owned product and technical documentation, the current UnityPlatform implementation, and the prototype systems:

- `Vision Document.docx` — full platform vision, ship/portal/Arena/economy goals.
- `UnityPlatform/README.md` — project setup context.
- `UnityPlatform/Assets/Scripts/Gameplay/README.md` — current Pirate Quest vertical slice and Firebase flow.
- `capstone-project/README.md` — MathQuest Escape mission.
- Prototype puzzle architecture and reusable puzzle guides — interaction/puzzle composition model.
- Prototype pause/settings and sound setup guides — 3D usability and feedback requirements.
- Prototype scene/script inventory and coverage report — reusable assets/systems and technical debt.

## Recommended next action

Approve or revise the ten decisions in the **Approval Gate**. Once approved, implementation should begin with **Stage 0 and Stage 1**, followed by a short technical design document for the assignment/progress/security model. The team should not start building additional worlds or currencies before the first end-to-end quest loop and trusted backend are proven.
