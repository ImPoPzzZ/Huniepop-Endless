[WIP Mod] Huniepop Endless — a roguelite run mode for HuniePop 2
I've been building a BepInEx mod that turns HuniePop 2's dating puzzle into a roguelite endless run. One button on the title screen, no save file, escalating difficulty, and a draft of random power-ups before every date. Wanted to share where it's at.

The pitch
You hit ENDLESS on the title screen and drop straight into a date. Clear it and you go to the next one — new girls, new location, higher affection goal, and you keep your leftover moves. Before each date's puzzle you draft one of three random power-ups (Slay-the-Spire style). Fail a single date and the run is over.

Nothing is saved. The whole run lives in memory — your real save files are never touched.

The run loop
Straight into a date. No apartment, no hub, no story. A throwaway max-nothing profile is built in memory and you're dropped onto a real date location.
2 girls per date, pulled at random from the full roster. Compatibility is ignored — you might get two girls who'd never be a "pair" in the base game.
The goal climbs every date: 115 → 190 → 271 → 358 → 451 → 550 → 655 … — gentle at first, then it ramps and genuinely threatens you.
Moves carry over between dates (HuniePop 2's native Non-Stop behaviour), plus a small per-date bonus if you drafted move cards.
Time of day cycles Morning → Afternoon → Evening → Night → repeat, with the location and background music changing each date to match.
Fail one date → run over. A summary screen shows how many dates you cleared (and your best of the session), then back to the title.
The power-up draft
After the girls finish their intro banter, the screen dims and three cards appear. Pick one. Every pick needs an "Are you sure?" confirm so you can't fat-finger it.

Effects tagged (this date) last one date. (run) effects stick for the whole run. Most cards are a boon + a drawback.

Moves
Second Wind — +5 moves this date
Steady Pace — +2 moves every future date (capped at +5 total) · drawback: both girls gain 1 permanent baggage
Emotional Baggage — +3 moves every future date · drawback: both girls gain 1 permanent baggage
Openers
Head Start — begin with the affection meter 20% filled · drawback: −4 moves this date
Running Start — begin with the meter 30% filled
Open Up — both girls start with +5 sentiment
Warm Welcome — both girls start with +25 passion
Caffeine — +3 stamina to both girls now
Tokens & scoring
Power tokens (the sparkly high-value ones):
Sparks Fly — power tokens form from matches more often (run) · drawback: permanent baggage
Big Feelings — power tokens score more (run) · drawback: every future goal +10%
(capped — after 3 power-token cards they stop appearing)
Scented candles — for each affection type (Talent / Flirtation / Romance / Sexuality):
X Candle — lights the real in-game X candle: +50% X tokens this date
X Bias — lights the X candle (+50%) and blocks a random other type's candle + −50% on that type, this date
Both show up as candle icons in the effects strip — one lit, one "Blocked"
Permanent progression
Talent / Flirtation / Romance / Sexuality Mastery — +1 permanent affection level (every match of that type scores more, for good — up to level 4 = ×5) · drawback: −6 moves this date
Second Nature — exhausted girls recover faster (run) · drawback: start every date at 3 stamina instead of 4
Date gifts
X Gift — a random gift of that affection type, permanently attached to a girl you pick — it's in her tray every date she appears on
Care Package — same, any gift type
Gift Basket — 3 gifts, assign each to a girl, permanently
Generous — every date gift costs 2 less sentiment to give (run)
Baggage
Clean Slate — clears both girls' baggage for this date only
Systems under the hood
Permanent baggage — when a card inflicts baggage, it hits the two girls actually on that date, and it's remembered against those specific girls. Every time one of them shows up again, her baggage comes back. It's drawn from the whole game-wide baggage pool (not just her own two), capped at 3 per girl — past that it converts to a permanent −10 passion-meter penalty for her.

Permanent date gifts — same idea: a gift you attach to a girl is in her tray every single date she's on, forever.

Run HUD — a top-right panel (toggle with H) that lists the current date, goal, move count, and every permanent effect you've picked up — buffs and debuffs — plus per-girl baggage and gift counts. Nothing invisible.

No-save — every GamePersistence write is blocked while a run is active. The synthetic profile only ever exists in memory slot 3 and evaporates when you return to the title.

Technical
BepInEx 5 + HarmonyX, standalone plugin (no dependency on Hp2BaseMod).
Reuses HuniePop 2's native Non-Stop Double Date engine for the escalating-round loop, then patches the bits that don't fit (the goal curve, the move economy, and the fact that Non-Stop normally sends you back to the hub instead of the title).
Menu button and launch screen are IMGUI — the game's own uGUI overlay handling turned out to be unreliable for injected canvases.
Status
Very much a work in progress. It was built by decompiling Assembly-CSharp, reading the real mechanics, and iterating against playtests. Balance is still being tuned, a couple of the fancier power-up ideas aren't in yet.
