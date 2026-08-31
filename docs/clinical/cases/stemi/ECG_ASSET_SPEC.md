# ECG asset specification — `stemi_anterior_001`

Status: **requirement spec only — no asset acquired yet.** Per the asset policy
(`docs/art/asset-manifest.md`): no import without a verified license recorded
first. **No fabricated diagnostic ECG from model memory** — whatever the source,
a clinician verifies the tracing shows exactly the intended pattern before it
ships (evidence EV-ASSET-001, EV-STEMI-014).

## Diagnostic content (what the tracing must show)

- Sinus rhythm ~95–100/min (consistent with the case's canonical HR 96).
- Unambiguous **anterior STEMI**: ST elevation V1–V4 (± I/aVL), reciprocal
  inferior ST depression. Exact elevation magnitudes: clinician-verified
  against the guideline lead/mm criteria (EV-STEMI-012 — verify V2–V3
  age/sex cutoffs against the primary text).
- No confounders: no LBBB, no pacing, no early-repolarization ambiguity, no
  artifact that invites a "repeat the ECG" response.

## Presentation requirements

- Standard 12-lead layout (3×4 + rhythm strip II), standard 25 mm/s / 10 mm/mV
  grid so learners can apply real calibration habits.
- Mobile readability: legible at ~390 pt width with pinch-zoom; source at least
  ~2000 px wide, clean grid contrast in light + dark UI, ≤2K texture budget per
  the asset policy if rendered in Unity.
- Machine-style header: rate/intervals only. Any "computer read" line is a
  difficulty-gated overlay (BLUEPRINT §ECG, [Q2]), **part of case data, not
  baked into the image**.

## Annotation policy

The shipped asset is unannotated. Debrief may show an annotated variant
(elevation arrows, territory shading) — a second derived asset, also
clinician-verified.

## Acceptable sources (in preference order)

1. **Commissioned/clinician-supplied synthetic tracing** (e.g., drawn with an
   ECG simulator tool by/with the reviewing clinician) — cleanest provenance;
   still requires written verification of the pattern.
2. **Wikimedia Commons CC BY 4.0** 12-lead anterior-STEMI tracing — commercial
   use permitted with attribution; verify the *specific file's* license page at
   acquisition time and record it in the asset manifest.
3. Licensed medical-education image banks (paid) — only with an explicit
   commercial-use license on file.

**Not acceptable:** LITFL library (CC BY-NC — non-commercial only), Dr. Smith's
ECG Blog (CC BY-NC), any real patient tracing without documented de-identified
educational-reuse rights, screenshots of unclear provenance, or an
AI-generated image presented as a diagnostic ECG without clinician sign-off.

## Provenance record (fill at acquisition)

`source file/URL · license + retrieval date · attribution text · clinician
verifier + date + statement ("this tracing shows an unambiguous anterior STEMI
suitable for teaching") · derived variants` → recorded in
`docs/art/asset-manifest.md` **before** import.
