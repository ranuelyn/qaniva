#!/usr/bin/env python3
"""Generates the CLINICAL ECG PLACEHOLDER for stemi_anterior_001.

This is a SCHEMATIC, code-generated 12-lead tracing (standard 3x4 layout +
lead-II rhythm strip, 25 mm/s, 10 mm/mV grid) whose morphology follows the
blueprint's intent (anterior ST elevation V1-V4, reciprocal inferior
depression) — but it is NOT a diagnostically valid ECG and is watermarked as
such on the image itself. Provenance: first-party, zero external content
(docs/clinical/cases/stemi/ECG_ASSET_SPEC.md, acceptable-source route
"generated placeholder"). Replacement with a clinician-verified tracing is
REQUIRED before any clinical claim (resultAssets provenance.clinicalStatus =
placeholder_replacement_required).

Output is deterministic (no randomness): re-running reproduces the same PNG.

Usage: python3 scripts/generate-ecg-placeholder.py
"""

import math
from pathlib import Path

from PIL import Image, ImageDraw

# --- geometry: 4 px per mm; 25 mm/s; 10 mm/mV ------------------------------
PX_PER_MM = 7
MM_PER_SEC = 25
MM_PER_MV = 10
SEC_W = MM_PER_SEC * PX_PER_MM  # px per second

MARGIN = 30 * PX_PER_MM
COL_SEC = 2.5  # seconds of tracing per column cell
ROWS, COLS = 3, 4
ROW_MM = 42  # vertical mm per row band
STRIP_SEC = 10.0

WIDTH = int(2 * MARGIN + COLS * COL_SEC * SEC_W)
HEIGHT = int(2 * MARGIN + (ROWS + 1) * ROW_MM * PX_PER_MM + 26 * PX_PER_MM)

GRID_SMALL = (245, 200, 200)
GRID_BOLD = (235, 160, 160)
INK = (30, 30, 40)
WATERMARK = (200, 60, 60)

HR = 96
RR_SEC = 60.0 / HR

# Schematic per-lead morphology (mV): (r_amp, s_amp, st_offset, t_amp).
# Anterior-STEMI intent per EV-STEMI-014: ST elevation V1-V4, reciprocal
# inferior depression. SCHEMATIC values — not clinically calibrated.
LEADS = {
    "I": (0.7, 0.10, 0.00, 0.25),
    "II": (0.9, 0.10, -0.10, 0.20),
    "III": (0.5, 0.10, -0.15, -0.10),
    "aVR": (-0.6, 0.05, 0.00, -0.20),
    "aVL": (0.4, 0.10, 0.08, 0.15),
    "aVF": (0.7, 0.10, -0.12, 0.10),
    "V1": (0.30, 0.60, 0.30, 0.40),
    "V2": (0.40, 0.50, 0.45, 0.55),
    "V3": (0.60, 0.35, 0.40, 0.50),
    "V4": (0.85, 0.20, 0.30, 0.40),
    "V5": (0.95, 0.15, 0.10, 0.30),
    "V6": (0.80, 0.10, 0.05, 0.25),
}
LAYOUT = [["I", "aVR", "V1", "V4"], ["II", "aVL", "V2", "V5"], ["III", "aVF", "V3", "V6"]]


def gauss(t: float, mu: float, sigma: float) -> float:
    return math.exp(-((t - mu) ** 2) / (2 * sigma * sigma))


def beat_mv(t: float, r: float, s: float, st: float, t_amp: float) -> float:
    """One schematic P-QRS-ST-T complex, t in [0, RR_SEC)."""
    v = 0.0
    v += 0.12 * gauss(t, 0.10, 0.020)  # P
    v += -0.08 * abs(r) / max(abs(r), 0.3) * gauss(t, 0.212, 0.006)  # Q
    v += r * gauss(t, 0.230, 0.008)  # R
    v += -s * gauss(t, 0.248, 0.007)  # S
    # ST segment: plateau blending into the T wave
    st_ramp = 0.5 * (1 + math.tanh((t - 0.27) / 0.012)) * 0.5 * (1 + math.tanh((0.46 - t) / 0.05))
    v += st * st_ramp
    v += t_amp * gauss(t, 0.44, 0.045)  # T
    return v


def draw_grid(d: ImageDraw.ImageDraw) -> None:
    for x in range(MARGIN, WIDTH - MARGIN + 1, PX_PER_MM):
        mm = (x - MARGIN) // PX_PER_MM
        d.line([(x, MARGIN), (x, HEIGHT - MARGIN)], fill=GRID_BOLD if mm % 5 == 0 else GRID_SMALL)
    for y in range(MARGIN, HEIGHT - MARGIN + 1, PX_PER_MM):
        mm = (y - MARGIN) // PX_PER_MM
        d.line([(MARGIN, y), (WIDTH - MARGIN, y)], fill=GRID_BOLD if mm % 5 == 0 else GRID_SMALL)


def draw_trace(d, x0: int, baseline_y: int, seconds: float, lead: str, t_start: float) -> None:
    r, s, st, t_amp = LEADS[lead]
    points = []
    steps = int(seconds * SEC_W)
    for i in range(steps):
        t_abs = t_start + i / SEC_W
        t_in_beat = t_abs % RR_SEC
        mv = beat_mv(t_in_beat, r, s, st, t_amp)
        points.append((x0 + i, baseline_y - int(mv * MM_PER_MV * PX_PER_MM)))
    d.line(points, fill=INK, width=3)
    d.text((x0 + 8, baseline_y - 22 * PX_PER_MM // 2), lead, fill=INK)


def draw_cal_pulse(d, x0: int, baseline_y: int) -> int:
    """Standard 1 mV / 0.2 s calibration pulse; returns consumed px."""
    h = MM_PER_MV * PX_PER_MM
    w = int(0.2 * SEC_W)
    d.line(
        [
            (x0, baseline_y),
            (x0 + 6, baseline_y),
            (x0 + 6, baseline_y - h),
            (x0 + 6 + w, baseline_y - h),
            (x0 + 6 + w, baseline_y),
            (x0 + 12 + w, baseline_y),
        ],
        fill=INK,
        width=3,
    )
    return 18 + w


def main() -> None:
    img = Image.new("RGB", (WIDTH, HEIGHT), (255, 255, 255))
    d = ImageDraw.Draw(img)
    draw_grid(d)

    # Machine-style header: rate + calibration only — NO interpretation text
    # (the blueprint grants no diagnosis assistance at core difficulty).
    d.text((MARGIN, 8 * PX_PER_MM), f"Rate {HR}/min    25 mm/s    10 mm/mV", fill=INK)
    d.text(
        (MARGIN, 15 * PX_PER_MM),
        "TRAINING PLACEHOLDER - SCHEMATIC TRACING - NOT A DIAGNOSTIC ECG - REPLACEMENT REQUIRED",
        fill=WATERMARK,
    )

    row_h = ROW_MM * PX_PER_MM
    top = MARGIN + 10 * PX_PER_MM
    for row_i, row in enumerate(LAYOUT):
        baseline = top + row_i * row_h + row_h * 2 // 3
        x = MARGIN
        x += draw_cal_pulse(d, x, baseline)
        for col_i, lead in enumerate(row):
            t_start = col_i * COL_SEC
            draw_trace(d, x, baseline, COL_SEC, lead, t_start)
            x += int(COL_SEC * SEC_W)

    # Rhythm strip: lead II, 10 s
    strip_baseline = top + ROWS * row_h + row_h * 2 // 3
    x = MARGIN
    x += draw_cal_pulse(d, x, strip_baseline)
    draw_trace(d, x, strip_baseline, STRIP_SEC - 0.3, "II", 0.0)

    repo = Path(__file__).resolve().parent.parent
    outputs = [
        repo / "packages/case-schema/fixtures/stemi_anterior_001/v1/assets/ecg_stemi_anterior_v1.png",
        repo / "unity/QanivaSimulation/Assets/Qaniva/Resources/Qaniva/CaseAssets/ecg_stemi_anterior_v1.png",
    ]
    for out in outputs:
        out.parent.mkdir(parents=True, exist_ok=True)
        img.save(out, optimize=True)
        print(f"wrote {out} ({out.stat().st_size / 1024:.0f} KB, {WIDTH}x{HEIGHT})")


if __name__ == "__main__":
    main()
