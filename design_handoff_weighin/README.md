# Handoff: WeighIn — mobile weight tracking app

## Overview

WeighIn is a mobile weight-tracking app. A user logs their weight — once or several times a day — and the app shows where that sits against a goal, how consistent they've been, and how the trend is moving. The prototype in this bundle covers the whole product: a PIN-locked entry, first-run onboarding, five main screens (Home, Calendar, Trends, More, History), and three setup flows (goal, reminders, export/import). All state persists locally, so the prototype behaves like the real app across reloads.

## About the design files

`WeighIn.dc.html` is a **design reference created in HTML** — a working prototype showing intended look and behavior. It is not production code to copy. The task is to recreate these screens in the target codebase's environment (React Native, SwiftUI, Flutter, Kotlin/Compose, etc.) using its established patterns, navigation, and component library. If no environment exists yet, pick the framework appropriate to the project and implement the designs there.

The prototype is a single-file HTML component: markup at the top, a JavaScript class at the bottom holding all state and derived values. The JS class is the best reference for behavior and math (streaks, moving average, BMI, goal pace, day averaging) — read it, port the logic, discard the rendering approach.

`nocturne-styles.css` is the design system token sheet the prototype pulls colors, spacing, radii, and shadows from. Every `var(--color-*)`, `var(--space-*)`, `var(--radius-*)` reference in the prototype resolves there.

## Fidelity

**High-fidelity.** Final colors, typography, spacing, copy, and interactions. Recreate the UI faithfully using the codebase's existing primitives. The one thing not to take literally is the phone bezel and status bar wrapping the screen in the prototype — that is presentation chrome, not part of the app.

Design canvas is **364 × 768 px** — the app's own viewport. Everything inside is authored against that width.

## Design tokens

From the Nocturne design system (dark, compact, low-chroma, accent used as a line and a glow rather than a flood).

### Color

| Token | Value | Use |
| --- | --- | --- |
| `--color-bg` | `#161826` | App ground |
| `--color-surface` | `#1d1f2e` | Cards, sheets, bars |
| `--color-text` | `#e9e9ed` | Primary text |
| `--color-accent` | `#9184d9` | Active tab, primary action, gauge fill, logged days |
| `--color-accent-200` | light blurple | Text on accent tints |
| `--color-accent-900` | deep blurple | Accent-tinted fills, chips |
| `--color-border` | `#2c2f3d` | 1px edges |

Literal values used alongside the tokens:

- `#232532` — empty contribution-grid cell, inactive track
- `rgba(35,37,50,.35)` — future (not-yet-reachable) grid cell
- `#8878d6` — logged contribution-grid cell
- `rgba(233,233,237,.6)` — secondary text
- `rgba(233,233,237,.45)` — inactive tab icon/label
- `rgba(233,233,237,.5)` — tertiary/meta text
- `rgba(233,233,237,.4)` — faintest meta, chip dismiss glyph
- `#3f424d` — device bezel ring (chrome only)
- Loss/gain semantics: loss reads accent/positive, gain reads warm — see `deltaColor` in the JS class for the exact pairs.

Never introduce a new hue. Contrast rule from the system: accent-on-ground is ~3:1, fine for icons, chrome, and large type but **not** for body copy — use `--color-accent-300` or lighter for paragraph text in accent.

### Type

Inter throughout (`--font-heading` and `--font-body` are both Inter; headings stay at weight 500 — do not bolden past it, hierarchy comes from size and space).

| Role | Size | Notes |
| --- | --- | --- |
| Gauge weight readout | 44px | Tabular figures, weight-500 |
| Screen title | 20px | Flush left |
| Card title | 14–15px | |
| Body / list row | 12–13px | |
| Meta / label | 10–11px | Often uppercase, `letter-spacing: .16em` for eyebrow labels |

Use tabular/monospaced figures for every number that changes in place (gauge readout, chart labels, history values) so digits don't jitter.

### Spacing, radius, shadow

Nocturne's scale is baked at density 0.70× — the system is dense on purpose. Take spacing from `--space-*`. Radius: `--radius-md` = 8px for cards and chips; the sheet uses a larger top radius; the contribution grid cells are 2px. Elevation comes from `--shadow-sm/md/lg`; on a dark ground elevation is an edge plus ambient darkness, never stacked heavy shadows.

## Data model

```
Entry (keyed by ISO date "YYYY-MM-DD")
  kg        number   the day's AVERAGE, rounded to 0.1
  time      string   "HH:MM" of the latest reading that day
  note       string   the latest non-empty note
  readings  Reading[] sorted ascending by time

Reading
  kg    number   0.1 precision, always stored in kilograms
  time  string   "HH:MM"
  note  string
```

**Weight is always stored in kilograms.** The unit setting (kg / lb) is a display-and-input concern only: multiply by `2.20462` on the way out, divide on the way in. Every readout, chart axis, stat, goal figure, and input field goes through that one conversion.

**Multiple weigh-ins per day.** A day holds an array of readings; `kg`, `time`, and `note` are always recomputed from that array (see `rollUp()` in the prototype). Logging on a day that already has entries appends a reading and re-averages — it does not overwrite. Everything downstream (gauge, chart, BMI, streak, calendar, history, export) reads the day's average, so a day with three weigh-ins is one point on the chart.

Other persisted state: `profile` (height, sex, birth year), `goal` (target weight, target date), `reminders` (enabled, time, days), `unit`, `std` (BMI standard: WHO or Asian), `pin`, `lock`, `onboarded`, `entries`.

All of it persists to local storage in the prototype and is restored on launch, with legacy single-reading entries migrated forward into the readings array on read. In the real app this is the local database layer.

## Screens

### 1. Splash

Entry point on every launch. Radial gradient from a lifted indigo at 38% height down to the app ground. Logo mark (112px square) fades and rises in over 0.7s on a `cubic-bezier(.2,.8,.2,1)` curve; the wordmark (186px wide) follows with a 0.14s delay. Tagline "One weigh-in a day" pinned 34px from the bottom, 10px uppercase with `.16em` tracking at 32% opacity.

Holds for **1.8s**, then advances to the PIN lock — or to onboarding if the user has never completed it, or straight to Home if the lock is off. Assets: `assets/weighin_logo.png`, `assets/weighin_logoname.png`.

### 2. PIN lock

Purpose: gate the app. Centered column: logo mark (54px), trimmed wordmark below it (76px wide, `assets/weighin_logoname_trim.png`), four PIN dots, then a 3×4 numeric keypad with a delete key.

Dots fill with the accent as digits are entered. A correct PIN unlocks to Home; a wrong one shakes the dot row and clears the buffer. **Prototype PIN is `1234`** — that is demo data, not a default to ship. In the real app this is the OS biometric/passcode layer where available.

### 3. Onboarding

Six steps, replayable from More. Collects height, sex, birth year, starting weight, unit preference, and goal. Each step is one question with a large input and a single forward action; back is available on every step but the first. Completing it writes the profile and sets `onboarded`.

### 4. Home

The default screen and the one that matters most.

- **Header** — greeting, today's long date ("Mon 14 Sep"), current time.
- **Gauge** — a semicircular arc showing the current weight's position within the healthy-BMI band for the user's height and standard. Current weight sits at the center in 44px figures with the unit beside it; the BMI marker sits below, clear of the readout (this clearance was a fix — keep the two from colliding at long values). The arc fill is the accent; the track is `#232532`. Band bounds shift with the unit setting and the WHO/Asian standard.
- **Delta card** — change since the last entry and since the start, each with a direction-coded color.
- **Contribution grid** — 26 weeks × 7 days, cells 11px tall in 7 rows with column auto-flow and a 2px gap. **Binary, not graded**: a day is either logged (`#8878d6`) or missed (`#232532`); future days are `rgba(35,37,50,.35)`. Legend reads "missed / logged" with one swatch each. (This replaced an earlier four-level intensity scale — do not reintroduce intensity.)
- **Streak card** — consecutive logged days, counting today only if logged.
- **Trend line** — compact sparkline of the recent average with a 7-day moving average overlay.
- **FAB** — bottom-right, opens the log sheet for today.

### 5. Log sheet (modal, over any screen)

Slides up from the bottom (`translateY(100%)` → `0`). Contents top to bottom:

1. Date label and a close action.
2. Large numeric input with the unit suffix and a numeric keypad; the field is **blank on open** even when the day already has readings, because the action is adding a weigh-in, not editing one.
3. Delta line under the readout — change vs. the previous day, direction-colored.
4. **Reading chips** — one per existing weigh-in that day, showing `weight · time`, each with a `✕`. Tapping a chip removes that reading and the day's average recalculates immediately. Row min-height 26px so the layout doesn't jump on the first reading. Chips use `--color-accent-900` fill with `--color-accent-200` text at 11px.
5. **Day summary** — "3 today · avg 71.9 kg", shown only when more than one reading exists. 10px, 40% opacity, min-height 13px.
6. Quick-adjust chips (±0.1, ±0.5 etc.) and an optional note field.
7. Primary action: **"Save"** on an empty day, **"Add"** when readings exist. Sheet title likewise **"Log weight"** → **"Add weigh-in"**.
8. **"Delete day"** removes the whole day, all readings.

Toast on save: single reading → "Logged 71.9 kg. 5 days in a row."; subsequent readings → "Added. 3 weigh-ins today, average 71.9 kg."

### 6. Calendar

Month grid. Logged days carry an accent dot; tapping a day selects it and shows its weight below the grid with its time — or "avg of 3 weigh-ins" when the day has several. Tapping the log action opens the sheet for the selected date, so past days can be filled in. Month navigation left and right; future days are not selectable.

### 7. Trends

Line chart of weight over a selectable range (week / month / quarter / all) with a 7-day moving-average overlay, axis labels in the active unit. Below it: stat tiles — average, lowest, highest, total change, logging rate — all recomputed on unit change. When a goal is set, a pace check states whether the current trajectory reaches the target by the target date.

### 8. History

Reverse-chronological list of every logged day: date, weight, delta from the previous entry, and either the time or "avg of 3" for multi-reading days. Tapping a row opens that day in the sheet.

### 9. More

Settings index: unit toggle (kg/lb), BMI standard (WHO/Asian), profile fields, PIN on/off, replay onboarding, and entries into the three setup flows below. Active-tab treatment: the More tab stays accent-colored while any of `more`, `history`, `goal`, `reminder`, `export` is the current screen.

### 10. Goal setup

Target weight and target date, with live feedback on the implied weekly rate and whether it is reasonable. Writes `goal`, which the gauge, trends pace check, and Home ETA all read.

### 11. Reminder setup

Enable toggle, time picker, and day-of-week selection. Purely local state in the prototype; in the real app this schedules local notifications.

### 12. Export / import

Export shows a live preview of the CSV with a real row count from current data. Import accepts CSV; because it clears existing entries it requires an explicit second confirmation tap (the confirm state auto-expires after 3.5s). Keep that two-step guard.

## Interactions & behavior

- **Navigation** — five-item bottom tab bar (Home, Trends, FAB, Calendar, More). Active item is `--color-accent`, inactive `rgba(233,233,237,.45)`. History, Goal, Reminder, and Export are pushed views under More, not tabs.
- **Transitions** — sheet slide-up from the bottom; toast fades in, holds, fades out; splash logo fade-and-rise as described. Keep them short (under 400ms except the splash).
- **Live propagation** — every state change updates all dependent views at once: logging a weight moves the gauge, extends the chart, recomputes the streak, fills a grid cell, marks the calendar day, and inserts a history row in the same frame. Unit changes re-label the gauge band, chart axis, stats, and goal figures together. Build this off one source of truth, not per-screen copies.
- **Validation** — weight must fall between 20 and 400 kg after unit conversion; out-of-range input is rejected silently rather than throwing an error state. Future dates cannot be logged.
- **Interaction states** — every tappable element needs a hover/press tint from the accent ramp and a 2px accent `:focus-visible` equivalent; disabled controls drop to 45% opacity. Hit targets never below 44px.
- **Empty states** — a fresh install has no entries: the gauge shows the band with no marker, the grid is all-missed, the chart and history are empty with a single prompt to log the first weigh-in.

## State summary

Derived-on-read (do not persist): streak, deltas, moving average, BMI and its band position, goal pace and ETA, CSV preview, day averages. Persisted: `entries`, `profile`, `goal`, `reminders`, `unit`, `std`, `pin`, `lock`, `onboarded`. Transient per-session: current tab, sheet open/closed, input buffer, selected date, toast text, confirm-wipe flag, splash/lock/onboard gate.

## Assets

- `assets/weighin_logo.png` — logo mark, user-supplied.
- `assets/weighin_logoname.png` — full wordmark, used on the splash.
- `assets/weighin_logoname_trim.png` — tight-cropped wordmark, used on the lock screen.

Icons in the prototype are Phosphor (https://phosphoricons.com), per the design system. Substitute the codebase's icon set at matching weights if it has one.

## Files

- `WeighIn.dc.html` — the complete prototype: all 12 screens, all logic. Open it in a browser to interact with it. PIN is `1234`.
- `nocturne-styles.css` — the design system token sheet every `var(--*)` in the prototype resolves against.
- `assets/` — the three logo files.
