# Before / after — product-shell baseline vs final polish

**Before:** `docs/audits/product-shell-screenshots/` (2026-09-01 baseline, 26 PNGs)
**After:** `docs/audits/final-polish-screenshots/` (this pack, 27 PNGs)

Both sets were inspected image-by-image; every claim below was verified
against the actual pixels of both captures. Competitor principles reference
`docs/product/COMPETITIVE_VISUAL_BENCHMARK.md` — no competitor layout, wording
or asset was copied.

| Screen | Before | After | What changed & why | Principle | Remaining limitation |
| --- | --- | --- | --- | --- | --- |
| Splash | `00-splash.png` | `00-splash.png` | Baseline frame showed simulator "Downloading 100%…" chrome; the after capture is a clean ink launch field from a warm relaunch. No product change — capture correctness. | A launch state must not show tooling chrome | Wordmark still displays sub-400 ms in dev builds; not frame-capturable |
| Onboarding | `01…03b` | `01…04` | Baseline showed page 1 four times (pager width bug) with text-only slides. Fixed `useWindowDimensions` pager; each page now has a distinct icon + 3-step flow diagram (Patient→Decision→Response, Assess→Investigate→Treat, 00:00→Action→State change, Timeline→Why→Evidence); page 4 CTA is "Get started". | Body Interact: distinct, stable first-run states; Qaniva's own decision→time→response→evidence grammar | Diagrams are code-native and simple by design |
| Home (first run) | `04` | `05` | Passive "Choose your first case" empty state replaced by one explicit "Start first case" action card; library preview compacted (2 indexed rows + View all); progress hint is one quiet line instead of an empty-state card. | Full Code next-patient clarity | — |
| Home (returning) | `05`, `24` | `21` | Continue/Replay card is now the single dominant module; case rows compact (index + badge + Replay), no duplicated same-case weight. | One unmistakable next action in <3 s | — |
| Cases | `06` | `06` | Cards keep full metadata but gain a teal case index (01/02/03), tighter meta line, "New case"/"Best N pts" badges and a footer CTA row — differentiated without new chrome. | Scalable scenario library | No search/filters (deliberate at 3 cases) |
| Briefing | `07`, `08` | `07`, `08`, `26` | Baseline was one dash-bullet wall (anaphylaxis clipped at the left edge). Now: labeled Case information rows (Role / Setting / Resources / Triage note) + a teal-railed "Your task" block; clipping fixed; replay state shows recent attempts + sticky Play again. | Body Interact separates scenario details from goals/Start | — |
| Simulation | `09`, `10` | `15`, `22`, `23` | Unity UI re-tokened from generic blue to Qaniva ink/teal: teal selected tab with dark-on-teal text, ink panels, semantic amber/red for warnings/exit; camera tightened (FOV 60→56, lower target) so the patient reads larger; result overlays capped in height. | RN→Unity should feel like one product entering simulation mode | Patient remains improved-EARLY-MVP art (purchase decision pending) |
| ECG viewer | `11` | `16` | Baseline title collided with the status bar and the asset floated on a white full-height canvas above competing controls. Now a dark contained viewer: safe header inset, ECG fit to viewport on a white card inside an ink frame, compact controls, teal Close; watermark/provenance note retained. | Let the clinical asset dominate; restrained controls | ECG asset is still the schematic placeholder pending clinical verification |
| Results | `12`, `13`, `14`, `15` | `17`–`20`, `24` | Baseline was a stack of same-weight cards with a raw score-breakdown text line and a clipped references capture. Now: Outcome hero (outcome + big score + score-domain grid), "Case focus" summary, criterion rows with semantic accent markers + evidence chips, causal "What happened to the patient" time rail, connected clinical timeline, quiet left-railed references. No scoring/engine change. | Full Code direct score; Body Interact staged feedback; Qaniva timing/causality identity | Long content by nature; progressive hierarchy mitigates |
| Progress | `16`, `17` | `09`, `25` | Two bordered metric cards + card-per-row replaced by one borderless metric panel with divider, grouped case rows with chevrons, and a score-column recent list. | Small-data-appropriate progress | — |
| Settings | `18` | `10` | Baseline had horizontal clipping (tab bar cut). Now grouped rows with dividers inside single surfaces; clipping fixed; destructive Reset remains distinct. | Modest, honest Settings | — |
| About / Disclaimer | `19`, `20` | `11`, `12` | Card-per-paragraph replaced with left-rail callouts and short labeled sections; disclaimer's back/title collision fixed. | Controlled line length for legal/educational copy | — |
| Error state | `22` | `14` | Baseline wordmark collided with the status clock and raw detail was prominent. Now "Simulation unavailable" eyebrow + friendly copy first, technical detail muted below, safe area respected. | Recovery-first error surfaces | — |
| Loading | `21` | `13` | Unchanged product behavior; the Unity license splash sits on an ink-matched background and RN's branded "Preparing" state precedes it (<500 ms on this machine). | Minimize brand interruption | License splash cannot be removed on the current Unity license |

## Strongest improvements

1. **Onboarding** — from a broken repeated page to four distinct pages carrying
   the decision→time→response→evidence story.
2. **Results** — from an equal-weight card stack to a progressive
   outcome→critical→causality→timeline→evidence hierarchy; now the hero screen.
3. **RN↔Unity coherence** — the simulation now uses the shell's ink/teal
   language; the transition reads as Qaniva entering simulation mode.
4. **Geometry defects closed** — every baseline safe-area collision and
   horizontal clipping (briefing, settings, disclaimer, error, ECG title) is
   gone in the final captures.
