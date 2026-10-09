#!/usr/bin/env python3
"""Compara os 3 sheets atuais (debug de proporção). Não altera sprites."""
from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(Path(__file__).resolve().parent))
from measure_hero import fmt, measure_sheet  # noqa: E402

OUT = ROOT / "Assets" / "Documentation" / "CharacterPixelArtComparison.md"

SHEETS = [
    ("Guerreiro", ROOT / "Assets/Resources/Guerreiro/sheet.png"),
    ("Mago", ROOT / "Assets/Resources/Mago/sheet.png"),
    ("Arqueiro", ROOT / "Assets/Resources/Arqueiro/sheet.png"),
]


def main():
    rows = []
    lines = [
        "# Comparison de proporção — heróis (Etapa 8–9)",
        "",
        "Gerado por `Assets/Art/_pipeline/compare_heroes.py`. **Não altera sprites.**",
        "",
        "| Herói | Grade | Idle y | Altura | Pad below | Baseline | Altura no molde |",
        "|-------|-------|--------|--------|-----------|----------|-----------------|",
    ]
    for name, path in SHEETS:
        if not path.exists():
            lines.append(f"| {name} | — | — | — | — | FALTA | — |")
            continue
        m = measure_sheet(path)
        print(fmt(m))
        base = "ok" if m.get("baseline_ok") else "FLUTUA"
        hok = "ok" if m.get("height_ok") else "FORA"
        lines.append(
            f"| {name} | {m['grid']} | {m['y0']}–{m['y1']} | {m['h']} | "
            f"{m['pad_below']} | {base} | {hok} |"
        )
        rows.append(m)
    lines += [
        "",
        "Molde: altura 24–30 px, última linha opaca em y=59–61 (Guerreiro = y=60).",
        "",
    ]
    OUT.write_text("\n".join(lines), encoding="utf-8")
    print("wrote", OUT)


if __name__ == "__main__":
    main()
