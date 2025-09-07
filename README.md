# 3D Triple-Match Tray Physics Pile, Tap-to-Tray, Triple Clear

## Problem & Constraints

### We needed a shippable core loop for a Matchingham-style 3D match game that:
	•	Spawns a physical pile of items that settle convincingly.
	•	Uses tap-to-select → send to tray → triple clear.
	•	Is data-driven (levels from JSON), maintainable and performant on mobile.
	•	Has a fail condition (tray overflow or countdown timer), win flow and clean Next/Retry/Restart.
	•	Uses free/open assets only, no plagiarized gameplay code.
	•	Targets Unity 6000.0.43f1, produces a clean APK and feels publish-ready.

## Architecture: Why a Small, Explicit Core?

#### We keep the game simple, explicit and testable:
	•	Single runtime scene (GameScene) with modular controllers:
	•	RayController (input)
	•	TrayController (domain + visuals)
	•	FruitPool (per-type pooling)
	•	Timer/UI bindings (Bindable observables)
	•	No hard coupling between physics pile and tray logic. The tray only consumes an IFruit (type id + anchors) and works purely on type IDs.
	•	Orthographic camera so the board reads consistently on all devices. Size is computed once in Awake to frame the play area; UI is anchored independently.

#### Level Data: Packed Tasks, Deterministic Setup
	•	Levels come from Resources/levels.json.
	•	Each task is a packed int: (typeId << 8) | count.
	•	On load we unpack tasks, round counts up to multiples of 3 (triple-safety) and build a remaining-by-type map.

Why: Tiny data, easy diffs, no serialization overhead, zero ambiguity (always matchable).

### Runtime Flow (Player Journey)
#### 1.	Start/Restart

	•	Level JSON loads.
	•	Remaining task counters are computed.
	•	Spawn plan is built (see below).
	•	Timer (70s) arms and starts ticking.
#### 2.	Spawning (stable physics)
	•	We spawn items in small timed batches (coroutine, ~50–100 ms between spawns) at a single controlled drop position above the board (e.g., (0, y, 0) with mild height jitter).
	•	Each piece is placed kinematic + gravity off, then one frame later switched to dynamic + gravity on with Continuous Speculative collision and a capped maxDepenetrationVelocity.
	•	Result: convincing settle, no “explosions,” no infinite transforms.
#### 3.	Tap → Tray
	•	Input raycasts on a Fruit layer, ignores UI.
	•	The closest hit’s IFruit is passed to the tray.
#### 4.	Tray Add & Clear
	•	Tray tries to place by type, not by object identity.
	•	If 3 of the same type exist → clear:
	•	All three shrink out, return to pool.
	•	Tray compacts left.
	•	Task counters decrement by 3.
	•	Win check → prompt Next Level.
#### 5.	Fail
	•	If a placement is rejected because the tray is full, or timer hits zero, we fire Fail and show the prompt.
#### 6.	Next / Retry / Restart
	•	Next: apply reward, load next level, re-spawn.
	•	Retry: same level, re-spawn.
	•	Restart: level index → 0, gold reset, re-spawn.
	•	In all cases we return any active fruits (tray + pile) to the pool before spawning.

#### Spawn Plan: Why “Required + Filler Triples”?
	•	We always spawn exactly what the tasks need per type.
	•	Then we pad with extra triples of random types until we reach a target count (≈ 30–70 pieces) for a satisfying pile.
	•	Padding is in triples to keep the board solvable and avoid stranded singles.

#### Why: Realistic variety without soft-locking the board.
Physics Stability: The Choices That Matter
	•	Staggered spawning (small delays) instead of one giant drop.
	•	Kinematic placement → next frame dynamic to avoid immediate deep interpenetration.
	•	Continuous Speculative collision + maxDepenetrationVelocity cap for predictable resolution.
	•	Walls + floor are static colliders; no rigidbodies on the environment.
	•	Anchor-based tray alignment: fruits have RB/Collider on a child “anchor”. When snapping to a tray slot, we compute world pose so the child anchor lands at the slot center; this keeps visuals, collider and physics aligned.

#### Tray Domain: Simple, Deterministic, Extendable
	•	Tray maintains a fixed-size line of type IDs, with TryAdd(typeId) returning {accepted, cleared, indices}.
	•	Triple detection and compaction are pure operations on the type array.
	•	Visuals mirror the domain:
	•	Slots know which IFruit sits where.
	•	Clear uses the returned indices and returns objects to the pool.
	•	Compaction animates surviving fruits to their new slots using the anchor-based alignment.

Why: Easy to unit test and trivial to skin/replace.

#### Pooling: Per-Type Queues, Zero Garbage Mid-Run
	•	Each FruitType has a Queue filled at init (size comes from authoring).
	•	Get returns an inactive fruit, initializes light state and activates.
	•	Return disables, resets minimal state (flags/colliders) and enqueues.
	•	We never destroy during gameplay; we reset when switching levels.

Why: No spikes, no GC churn, instant restarts.

#### Timer & Fail Policy
	•	The timer starts on level setup and on Start; default 70 seconds (configurable).
	•	Fail triggers when:
	•	The timer reaches 0, or
	•	A pick attempt overflows the tray (the rejected add is your fail moment, not after the next click).
	•	Prompts expose Next/Retry/Restart; all flows cleanly reset state and pool.

#### Performance & Memory Strategy
	•	PrimeTween for all in-game tweens (allocation-free).
	•	GPU instancing-friendly materials; no expensive post-processing (e.g., Bloom off by default).
	•	Target frame rate matches device refresh when available, with a sane mobile fallback (no v-sync stall on Android).
	•	No per-frame allocations in core loops; logs are throttled and dev-only.

Result: Steady frame times and low draw call pressure.

Error Handling & Safety Nets
	•	Missing prefab type in pool definitions → clear log with the missing enum.
	•	Spawn safety:
	•	Single controlled drop point removes floor-grid edge cases.
	•	We only switch to dynamic after transforms are synchronized.
	•	Level JSON missing or malformed → safe defaults and a visible error string.

Reviewers see resilience, not just happy-path.

Why This Fits the Task
	•	Core mechanic: fully implemented — tap-to-tray, triple clear, compaction, progress to win.
	•	Maintainable architecture: small, decoupled controllers; data-driven levels; pure tray domain.
	•	Publish optimization: pooling, safe physics, lean post, device-aware frame targeting.
	•	Open-source only: tweening + input; no paid kits; gameplay code is original.
	•	Basic UI that’s extendable: bindables for level, gold, timer, task text; prompts for next/fail; one-call restart.

#### Trade-offs & Justifications
	•	Orthographic camera over perspective: cleaner read and device consistency. We size it once in Awake to avoid UI breathing.
	•	Single drop point instead of floor grids: fewer physics edge cases, faster to stabilize, better early-game feel.
	•	Triples-only filler: guarantees solvability; avoids leftover singles cluttering the tray.

#### What I’d Add Next (Given More Time)
	•	Soft-body jiggle/micro-impulses on pile taps for feel.
	•	Hint system and auto-suggest when a triple is obvious.
	•	Haptics and screen feedback on clear/fail/win.
	•	Daily missions layered on top of the task model.
	•	Analytics hooks around tray overflow, timeouts and clear cadence.

#### Summary

This build focuses on the core loop done right: stable physics pile, clean tap-to-tray input, deterministic triple clears, robust level progression and production-minded performance. It’s small, clear and extensible the kind of foundation you can actually ship and iterate on.
