# Software Requirements Specification (SRS)
## Personal Weight & BMI Tracker — Offline Android App
**Version:** 1.2 (Updated)
**Author:** Hannah
**Date:** August 2026
**Working Title:** WeighIn

---

## 1. Introduction

### 1.1 Purpose
This document defines the requirements for a personal, offline Android application that lets a single user log their body weight over time, automatically calculates BMI from a stored height, and visualizes trends. Built with .NET MAUI in JetBrains Rider.

### 1.2 Scope
- Single-user, single-device app (no accounts, no cloud sync, no internet required).
- Core purpose: log weight → auto-compute BMI → show trends over time.
- All data stored locally on the Android device.

### 1.3 Definitions
| Term | Meaning |
|---|---|
| BMI | Body Mass Index = weight(kg) / height(m)² |
| Entry | A single weight log record (date + value) |
| Trend | Visual/statistical pattern of entries over a time range |

### 1.4 Learning Goal
This build is also a hands-on way to learn C#. Implementation steps will include short explanations of the language features/patterns used as they come up (classes, async/await, data binding, LINQ, MVVM, etc.) rather than just handing over finished code.

---

## 2. Overall Description

### 2.1 Product Perspective
Standalone mobile app. No backend server. No login. Runs entirely offline.

### 2.2 User Characteristics
- One user (you), tracking personal health data.
- Wants simplicity — quick daily entry, minimal taps.

### 2.3 Operating Environment
- Android phone (target Android 8.0+ recommended for MAUI support).
- Built with .NET MAUI, developed in Rider.

### 2.4 Constraints
- Must work fully without internet.
- Must persist data across app restarts/phone reboots.
- Should not require Google Play Services–dependent features (keep it self-contained).

### 2.5 Assumptions
- You are the only user of the data (no multi-user isolation needed for v1).
- Height changes rarely (adults), so it's a settings-level value, not logged daily.
- **V1 prioritizes working functionality over visual polish.** UI/UX refinement is a deliberate later pass once core features work.

---

## 3. Functional Requirements

### 3.1 Profile & Settings (Core)
- FR-1: Set/edit height (cm or ft+in, user's choice of unit).
- FR-2: Set preferred weight unit (kg or lb) — convertible at display time.
- FR-2a: Choose preferred BMI classification standard — General/WHO or Asian/Asia-Pacific (Asian thresholds are lower and more clinically relevant for most users in the region). Defaults to Asian.
- FR-3: Optionally set a target/goal weight.
- FR-4: Optionally record date of birth/sex — not required for standard BMI, not a v1 must-have.
- FR-5: Ability to update height later; historical BMI stays accurate to the height on record at that time (see FR-14).

### 3.2 Weight Logging (Core)
- FR-6: Add a new weight entry: value + date (defaults to today, editable) + optional time.
- FR-7: Optional short note per entry (e.g., "after workout", "post-meal").
- FR-8: Edit or delete any past entry.
- FR-9: Allow multiple entries per day; the daily trend line and statistics use the **average** of all entries logged that day.
- FR-10: Quick-add shortcut (e.g., a "+" floating button, minimal-tap entry flow).

### 3.3 BMI Calculation (Core)
- FR-11: Auto-calculate BMI = weight(kg) / height(m)² on every entry, using the height on record.
- FR-12: Display BMI category using the standard chosen in FR-2a:
  - **General/WHO:** Underweight (<18.5), Normal (18.5–24.9), Overweight (25–29.9), Obese (≥30)
  - **Asian/Asia-Pacific:** Underweight (<18.5), Normal (18.5–22.9), Overweight (23–24.9), Obese (≥25)
- FR-13: Color-coded category indicator (e.g., blue/green/orange/red).
- FR-14: Store the height-at-time-of-entry alongside each weight entry, so BMI history stays accurate even if height is edited later.

### 3.4 Trends & Visualization (Core)
- FR-15: Line chart of weight over time.
- FR-16: Line chart of BMI over time (or overlay both on toggle).
- FR-17: Selectable time ranges: 7 days / 30 days / 90 days / 1 year / All time.
- FR-18: Tap a data point to see exact value + date + note.
- FR-19: Trend direction indicator (↑ gaining / ↓ losing / → stable) based on recent slope.
- FR-20: **Calendar view** — month grid showing which days have a logged entry (marked/dot) vs. missed days. Tapping a date opens that day's entry, or lets you add one if missing.

### 3.5 Statistics & Insights (Core)
- FR-21: Show current weight, starting weight, and net change over the selected period.
- FR-22: Show highest/lowest recorded weight and dates.
- FR-23: Show average weight over the selected period.
- FR-24: Simple moving average line overlaid on the chart (smooths daily noise).
- FR-25: Weekly/monthly rate of change (e.g., "-0.4 kg/week").

### 3.6 Goals (Core)
- FR-26: Set a target weight and target date.
- FR-27: Progress bar/percentage toward goal.
- FR-28: Simple linear projection: "At your current rate, you'll hit your goal around [date]" — clearly labeled as an estimate, not medical advice.

### 3.7 History Management (Core)
- FR-29: Scrollable/searchable list of all past entries (date, weight, BMI, note).
- FR-30: Filter history by date range.
- FR-31: Bulk delete (e.g., "clear all data" with confirmation).

### 3.8 Reminders / Notifications (Core)
- FR-32: Local daily reminder notification ("Log your weight today") at a user-set time.
- FR-33: Streak tracker (consecutive days logged) as a light motivator — pairs well with the calendar view (FR-20).
- Note: local notifications only — no server push needed; .NET MAUI supports this via a community plugin (`Plugin.LocalNotification`).

### 3.9 Data Backup / Export / Import (Core)
- FR-34: Export all data to a CSV file (saved to phone storage) — protects you if the app is ever uninstalled/reinstalled.
- FR-35: Import from a previously exported CSV to restore data.
- FR-36: Optional: export a chart/summary as an image or PDF for sharing later.

### 3.10 Security & Privacy (Core)
- FR-37: Optional app lock (PIN or device biometric) before opening the app.
- FR-38: All data stays on-device — explicitly no network calls, no analytics/tracking.

### 3.11 Stretch / Optional Features (later, after v1 is stable)
- FR-39: Track additional body measurements — **waist and hips** — manual entry only, no smart scale integration needed.
- FR-40: Dark mode / theme choice.
- FR-41: Multiple profiles (e.g., track a family member too).
- FR-42: Photo progress log (optional monthly photo attached to an entry).

---

## 4. Non-Functional Requirements
| Category | Requirement |
|---|---|
| Performance | App should launch in <2s; chart rendering smooth even with 1000+ entries |
| Usability | Logging a weight entry should take ≤3 taps from launch |
| Reliability | No data loss on app crash/force close — writes should be immediate/transactional |
| Portability | Should run on any Android device Android 8.0+ |
| Storage | Local SQLite database; data footprint stays small even for years of daily entries |
| Offline | 100% offline — zero network permissions requested |

---

## 5. System / Technical Requirements
- **IDE:** JetBrains Rider
- **Framework:** .NET MAUI (C#) — targeting Android
- **Local Database:** SQLite via `sqlite-net-pcl`
- **Architecture:** MVVM (`CommunityToolkit.Mvvm` for ViewModels/commands)
- **Charting:** MAUI-compatible chart library (`LiveChartsCore.SkiaSharpView.Maui`) — free/open-source, no license needed
- **Local Notifications:** `Plugin.LocalNotification`
- **Calendar UI:** custom grid built from MAUI's `CollectionView`/`Grid` (no heavy external dependency needed for a simple month view)

### 5.1 Draft Data Model
```
Profile
- Id
- HeightCm (float)
- WeightUnitPreference (kg/lb)
- TargetWeightKg (nullable)
- TargetDate (nullable)

WeightEntry
- Id
- DateTime
- WeightKg (float, canonical storage unit; convert for display)
- HeightCmAtEntry (float, snapshot for accurate historical BMI)
- Note (string, nullable)

BodyMeasurement (stretch, FR-39)
- Id
- DateTime
- WaistCm (nullable)
- HipsCm (nullable)
```

---

## 6. Out of Scope (v1)
- Cloud sync / multi-device access
- Social features / sharing to other apps by default
- Medical diagnosis or health advice beyond standard BMI category labeling
- iOS build (MAUI supports it later if wanted, not the current target)
- Home screen widget

---

## 7. Build Priority
**V1 (build now):** Sections 3.1–3.10 in full — FR-1 to FR-38, including calendar view, reminders, backup/export, and app lock. UI kept plain/functional; visual polish comes after.

**Stretch (after v1 is stable):** Section 3.11 — FR-39 to FR-42 (waist/hip measurements, dark mode, multiple profiles, photo log).
