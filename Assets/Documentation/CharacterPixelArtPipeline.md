# Pipeline de consistência visual — heróis

Guerreiro = **molde estrutural**. Mago/Arqueiro = **identidade própria** dentro da mesma caixa.

| Etapa | Artefato | Status |
|-------|----------|--------|
| 1 | `CharacterPixelArtAudit.md` | feito (PR #8 / `92831ac`) |
| 2 | `CharacterPixelArtStyleGuide.md` | feito |
| 3 | `Assets/Art/_pipeline/HeroCell_64.md` + `guia_baseline.png` | feito |
| 4–5 | `import_hero_sheet.py` | feito (empacota, não redesenha) |
| 6–7 | `validate_hero_sheet.py` + `measure_hero.py` | feito |
| 8–9 | `compare_heroes.py` → `CharacterPixelArtComparison.md` | feito |
| 10 | `CharacterPixelArtMagoSpec.md` | feito |
| 11 | Redraw do Mago no molde (`draw_mago_etapa11.py`) | feito — validar no Play |
| 12 | Cut-ins de especial (`Assets/Art/CutIns/build_cutins.py` + `SpecialCutIn.cs`) | feito — ver `Assets/Art/CutIns/FRAME_MAP.md` |

**Regra:** etapas 1–10 **não sobrescrevem** sprites de gameplay. Import só roda à mão.
