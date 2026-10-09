#!/usr/bin/env python3
"""Gera o PDF do Game Design Document — Aetherion: Voidfall / Apocalipse."""
from __future__ import annotations

from pathlib import Path

from PIL import Image as PILImage
from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_JUSTIFY, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import cm, mm
from reportlab.platypus import (
    Image,
    KeepTogether,
    ListFlowable,
    ListItem,
    PageBreak,
    Paragraph,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)

ROOT = Path(__file__).resolve().parent.parent
MEDIA = ROOT / "trailer" / "media"
ARTES = ROOT / "artes" / "stitch_aetherion_voidfall_ui_concept"
OUT = ROOT / "docs" / "Aetherion-Voidfall-GDD.pdf"
TMP = ROOT / "docs" / "_gdd_img"
FONT_TITLE = "Helvetica-Bold"
FONT_BODY = "Helvetica"

# Artes oficiais do zip (personagens + naves)
SRC_MAGO = ARTES / "3.png" / "mago.png"
SRC_ARQUEIRO = ARTES / "edit_the_character_sprite_of_the_celestial_archer_from_data_image_image_2_to" / "arqueiro.png"
SRC_GUERREIRO = ROOT / "Assets" / "Art" / "guerreiro_ref.png"
SRC_NAVE_ARQUEIRO = ARTES / "4.png" / "nave1.png"
SRC_NAVE_GUERREIRO = ARTES / "5.png" / "nave 2.png"
SRC_NAVE_MAGO = ARTES / "6.png" / "nave 3.png"


def ensure_dirs():
    OUT.parent.mkdir(parents=True, exist_ok=True)
    TMP.mkdir(parents=True, exist_ok=True)


def prepare_img(src: Path, name: str, max_w: int = 1400, max_h: int = 900) -> Path | None:
    if not src.exists():
        return None
    dst = TMP / name
    im = PILImage.open(src).convert("RGB")
    im.thumbnail((max_w, max_h), PILImage.Resampling.LANCZOS)
    im.save(dst, "JPEG", quality=88)
    return dst


def prepare_portrait(src: Path, name: str, canvas: tuple[int, int] = (720, 960), pad: float = 0.08) -> Path | None:
    """Sprite limpo em cartão escuro — sem colagem feia de cenário."""
    if not src.exists():
        return None
    dst = TMP / name
    im = PILImage.open(src).convert("RGBA")
    # fundo escuro do documento
    bg = PILImage.new("RGBA", canvas, (12, 6, 20, 255))
    # escala mantendo proporção
    max_w = int(canvas[0] * (1 - 2 * pad))
    max_h = int(canvas[1] * (1 - 2 * pad))
    ratio = min(max_w / im.width, max_h / im.height)
    nw, nh = max(1, int(im.width * ratio)), max(1, int(im.height * ratio))
    sp = im.resize((nw, nh), PILImage.Resampling.LANCZOS)
    x = (canvas[0] - nw) // 2
    y = (canvas[1] - nh) // 2
    bg.alpha_composite(sp, (x, y))
    bg.convert("RGB").save(dst, "JPEG", quality=92)
    return dst


def prepare_ship(src: Path, name: str, canvas: tuple[int, int] = (900, 520)) -> Path | None:
    """Nave em cartão horizontal escuro."""
    if not src.exists():
        return None
    dst = TMP / name
    im = PILImage.open(src).convert("RGBA")
    bg = PILImage.new("RGBA", canvas, (8, 4, 16, 255))
    # naves do zip vêm em canvas alto; corta conteúdo útil
    bbox = im.getbbox()
    if bbox:
        im = im.crop(bbox)
    max_w, max_h = int(canvas[0] * 0.9), int(canvas[1] * 0.88)
    ratio = min(max_w / im.width, max_h / im.height)
    nw, nh = max(1, int(im.width * ratio)), max(1, int(im.height * ratio))
    sp = im.resize((nw, nh), PILImage.Resampling.LANCZOS)
    x = (canvas[0] - nw) // 2
    y = (canvas[1] - nh) // 2
    bg.alpha_composite(sp, (x, y))
    bg.convert("RGB").save(dst, "JPEG", quality=92)
    return dst


def art_row(paths_labels: list[tuple[Path | None, str]], col_w_cm: float, img_h_cm: float, s) -> Table:
    cells = []
    for path, label in paths_labels:
        if path and path.exists():
            im = PILImage.open(path)
            w, h = im.size
            th = img_h_cm * cm
            tw = th * (w / float(h))
            max_w = col_w_cm * cm * 0.90
            if tw > max_w:
                tw = max_w
                th = tw * (h / float(w))
            # Nested table evita KeepTogether quebrar o layout do reportlab
            inner = Table(
                [
                    [Image(str(path), width=tw, height=th)],
                    [Paragraph(label, s["art_label"])],
                ],
                colWidths=[col_w_cm * cm * 0.96],
            )
            inner.setStyle(TableStyle([
                ("ALIGN", (0, 0), (-1, -1), "CENTER"),
                ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
                ("LEFTPADDING", (0, 0), (-1, -1), 2),
                ("RIGHTPADDING", (0, 0), (-1, -1), 2),
                ("TOPPADDING", (0, 0), (-1, -1), 2),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 2),
            ]))
            cells.append(inner)
        else:
            cells.append(Paragraph(label + " (arte ausente)", s["art_label"]))
    t = Table([cells], colWidths=[col_w_cm * cm] * len(cells))
    t.setStyle(TableStyle([
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("ALIGN", (0, 0), (-1, -1), "CENTER"),
        ("LEFTPADDING", (0, 0), (-1, -1), 3),
        ("RIGHTPADDING", (0, 0), (-1, -1), 3),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
        ("BACKGROUND", (0, 0), (-1, -1), colors.HexColor("#140a1c")),
        ("BOX", (0, 0), (-1, -1), 1, colors.HexColor("#3a2250")),
        ("INNERGRID", (0, 0), (-1, -1), 0.5, colors.HexColor("#2a1840")),
    ]))
    return t


def styles():
    base = getSampleStyleSheet()
    s = {
        "cover_title": ParagraphStyle(
            "cover_title",
            parent=base["Title"],
            fontName=FONT_TITLE,
            fontSize=26,
            leading=32,
            textColor=colors.HexColor("#efc75f"),
            alignment=TA_CENTER,
            spaceAfter=8,
        ),
        "cover_sub": ParagraphStyle(
            "cover_sub",
            parent=base["Normal"],
            fontName=FONT_BODY,
            fontSize=12,
            leading=16,
            textColor=colors.HexColor("#d8c9a8"),
            alignment=TA_CENTER,
            spaceAfter=6,
        ),
        "h1": ParagraphStyle(
            "h1",
            parent=base["Heading1"],
            fontName=FONT_TITLE,
            fontSize=16,
            leading=20,
            textColor=colors.HexColor("#2a1840"),
            spaceBefore=16,
            spaceAfter=8,
            borderPadding=3,
        ),
        "h2": ParagraphStyle(
            "h2",
            parent=base["Heading2"],
            fontName=FONT_TITLE,
            fontSize=12,
            leading=16,
            textColor=colors.HexColor("#5a2a78"),
            spaceBefore=10,
            spaceAfter=4,
        ),
        "body": ParagraphStyle(
            "body",
            parent=base["Normal"],
            fontName=FONT_BODY,
            fontSize=10,
            leading=14,
            alignment=TA_JUSTIFY,
            textColor=colors.HexColor("#1a1220"),
            spaceAfter=6,
        ),
        "caption": ParagraphStyle(
            "caption",
            parent=base["Normal"],
            fontName=FONT_BODY,
            fontSize=8,
            leading=11,
            alignment=TA_CENTER,
            textColor=colors.HexColor("#555"),
            spaceBefore=2,
            spaceAfter=10,
        ),
        "art_label": ParagraphStyle(
            "art_label",
            parent=base["Normal"],
            fontName=FONT_TITLE,
            fontSize=8,
            leading=11,
            alignment=TA_CENTER,
            textColor=colors.HexColor("#efc75f"),
            spaceBefore=4,
            spaceAfter=6,
        ),
        "bullet": ParagraphStyle(
            "bullet",
            parent=base["Normal"],
            fontName=FONT_BODY,
            fontSize=10,
            leading=13,
            textColor=colors.HexColor("#1a1220"),
        ),
        "footer": ParagraphStyle(
            "footer",
            parent=base["Normal"],
            fontName=FONT_BODY,
            fontSize=8,
            textColor=colors.HexColor("#777"),
            alignment=TA_CENTER,
        ),
    }
    return s


def add_image(story, path: Path | None, width_cm: float, caption: str, s):
    if not path or not path.exists():
        return
    im = PILImage.open(path)
    w, h = im.size
    tw = width_cm * cm
    th = tw * (h / w)
    max_h = 9.5 * cm
    if th > max_h:
        th = max_h
        tw = th * (w / h)
    story.append(KeepTogether([Image(str(path), width=tw, height=th), Paragraph(caption, s["caption"])]))


def bullets(items: list[str], s) -> ListFlowable:
    return ListFlowable(
        [ListItem(Paragraph(i, s["bullet"]), leftIndent=8, bulletColor=colors.HexColor("#5a2a78")) for i in items],
        bulletType="bullet",
        start="•",
        leftIndent=12,
        spaceAfter=8,
    )


def header_footer(canvas, doc):
    canvas.saveState()
    canvas.setFillColor(colors.HexColor("#1a0a22"))
    canvas.rect(0, A4[1] - 1.2 * cm, A4[0], 1.2 * cm, fill=1, stroke=0)
    canvas.setFillColor(colors.HexColor("#efc75f"))
    canvas.setFont(FONT_TITLE, 8)
    canvas.drawString(1.8 * cm, A4[1] - 0.75 * cm, "AETHERION: VOIDFALL — Game Design Document")
    canvas.setFillColor(colors.HexColor("#f0e8d8"))
    canvas.rect(0, 0, A4[0], 1.0 * cm, fill=1, stroke=0)
    canvas.setFillColor(colors.HexColor("#555"))
    canvas.setFont(FONT_BODY, 8)
    canvas.drawCentredString(A4[0] / 2, 0.4 * cm, f"Página {doc.page}")
    canvas.restoreState()


def cover_page(story, s, imgs):
    story.append(Spacer(1, 1.5 * cm))
    story.append(Paragraph("AETHERION: VOIDFALL", s["cover_title"]))
    story.append(Paragraph("(também apresentado como Aetherion: Apocalipse)", s["cover_sub"]))
    story.append(Spacer(1, 0.3 * cm))
    story.append(Paragraph("Game Design Document — <b>Demo / Parte 1</b>", s["cover_sub"]))
    story.append(Paragraph("Dark Fantasy 16-bit · Metal Slug na terra · Shoot'em up no espaço", s["cover_sub"]))
    story.append(Spacer(1, 0.35 * cm))
    story.append(Paragraph(
        "<b>Importante:</b> este documento descreve a <b>primeira parte (demo)</b> do jogo. "
        "Arte, balanceamento, fases, inimigos e sistemas ainda vão sofrer vários ajustes "
        "ao longo do desenvolvimento. O que está aqui é a visão atual — não o produto final.",
        s["body"],
    ))
    story.append(Spacer(1, 0.35 * cm))
    add_image(story, imgs.get("titulo"), 14, "Arte conceitual — título e atmosfera do Véu", s)
    story.append(Paragraph(
        "Documento de concepção da demo: história, objetivos, mecânicas, imagens, "
        "funcionamento e próximos passos antes da construção completa.",
        s["body"],
    ))
    story.append(PageBreak())


def section_historia(story, s, imgs):
    story.append(Paragraph("1. História do Jogo", s["h1"]))

    story.append(Paragraph("1.1 Contexto", s["h2"]))
    story.append(Paragraph(
        "O jogo se desenrola em <b>Aetherion</b>, um mundo protegido por milênios por uma "
        "barreira cósmica chamada <b>Véu</b>. Quando algo desperta além das estrelas, o Véu "
        "é destruído e a escuridão — o <b>Vazio</b> — invade o planeta. Cidades viram ruínas, "
        "os mortos não descansam e a guerra se espalha da superfície até a órbita. "
        "O jogador atravessa seis fases: três terrestres (ruínas, cidades sumidas, coração "
        "do Véu) e três espaciais (órbita quebrada, bloqueio da frota, fenda do Vazio).",
        s["body"],
    ))
    add_image(story, imgs.get("ruinas"), 14, "Cenário — Aetherion em ruínas sob a chuva e o Véu partido", s)

    story.append(Paragraph("1.2 Protagonistas", s["h2"]))
    story.append(Paragraph(
        "Não há um único herói: o jogador escolhe entre <b>três resistentes</b>, cada um com "
        "kit e nave próprios. Todos compartilham a mesma missão — impedir que o Vazio consuma "
        "Aetherion — mas jogam de forma distinta.",
        s["body"],
    ))
    story.append(bullets([
        "<b>Guerreiro</b> — tank corpo a corpo: vida e defesa altas, Corte de Energia e escudo. Nave pesada, tiro lento e forte.",
        "<b>Mago</b> — dano à distância: Orbe Arcano, vida baixa e poder alto. Nave frágil com projéteis em leque.",
        "<b>Arqueiro</b> — mobilidade: Pena Celestial, dash e equilíbrio de atributos. Nave rápida com tiro frequente.",
    ], s))
    story.append(Paragraph("Conceito oficial dos três heróis (artes individuais) — demo", s["caption"]))
    story.append(art_row([
        (imgs.get("card_guerreiro"), "<b>Guerreiro</b>"),
        (imgs.get("card_mago"), "<b>Mago</b>"),
        (imgs.get("card_arqueiro"), "<b>Arqueiro</b>"),
    ], 5.2, 7.2, s))
    story.append(Spacer(1, 0.35 * cm))
    story.append(Paragraph("Naves por classe — shoot'em up no espaço (conceito da demo)", s["caption"]))
    story.append(art_row([
        (imgs.get("ship_guerreiro"), "<b>Nave do Guerreiro</b>"),
        (imgs.get("ship_mago"), "<b>Nave do Mago</b>"),
        (imgs.get("ship_arqueiro"), "<b>Nave do Arqueiro</b>"),
    ], 5.2, 3.4, s))
    story.append(Spacer(1, 0.4 * cm))
    story.append(Paragraph(
        "Nomes, kits e artes podem mudar até a versão final; nesta demo o foco é provar "
        "combate terrestre, identidade dos três heróis e a transição para o espaço.",
        s["body"],
    ))

    story.append(Paragraph("1.3 Antagonistas", s["h2"]))
    story.append(Paragraph(
        "Na terra, o jogador enfrenta hordas corrompidas pelo Vazio — esqueletos, ghouls e "
        "zumbis — e o chefe <b>Vorthak, o Olhar Eterno</b>, nas ruínas. No espaço, a frota "
        "do Vazio e o chefe final <b>Morveth, o Encapuzado</b> aguardam na fenda entre as estrelas. "
        "O desafio não é só sobreviver às ondas: é carregar a build da terra até o duelo espacial.",
        s["body"],
    ))
    add_image(story, imgs.get("boss_terra"), 12, "Boss terrestre — Vorthak", s)
    add_image(story, imgs.get("boss_espaco"), 12, "Boss espacial — Morveth", s)
    story.append(PageBreak())


def section_objetivo(story, s):
    story.append(Paragraph("2. Objetivo do Jogo", s["h1"]))

    story.append(Paragraph("2.1 Meta principal", s["h2"]))
    story.append(Paragraph(
        "Guiar o herói escolhido pelas fases disponíveis nesta <b>demo (Parte 1)</b>, "
        "com o arco completo planejado em <b>seis fases</b>: derrotar o boss terrestre em T3, "
        "embarcar na nave e vencer o boss espacial em E3. A vitória completa só acontece após "
        "derrotar <b>Morveth</b> na versão final. Nesta demo, a run não persiste entre sessões: "
        "game over volta ao menu. Conteúdo, dificuldade e sistemas ainda serão ajustados.",
        s["body"],
    ))

    story.append(Paragraph("2.2 Sub-metas", s["h2"]))
    story.append(bullets([
        "Sobreviver a ondas crescentes de inimigos (esqueleto → ghoul → zumbi e além).",
        "Ganhar XP, subir de nível e escolher cartas de upgrade (dano, vida, velocidade, kits).",
        "Converter a build da terra em atributos da nave (força→dano, defesa→escudo, agilidade→velocidade, poder→especial).",
        "Colecionáveis / cartas raras e de classe (camadas futuras de progressão).",
        "Dominar plataformas one-way, pulos e padrões de ataque distintos por inimigo.",
    ], s))
    story.append(PageBreak())


def section_desafios(story, s, imgs):
    story.append(Paragraph("3. Desafios e Mecânicas de Jogo", s["h1"]))

    story.append(Paragraph("3.1 Níveis e progressão", s["h2"]))
    data = [
        [Paragraph("<b>Fase</b>", s["bullet"]), Paragraph("<b>Nome</b>", s["bullet"]), Paragraph("<b>Foco</b>", s["bullet"])],
        ["T1", "Ruínas da Borda", "Tutorial de movimento, ataque e bioma"],
        ["T2", "Cidades Sumidas", "Mais densidade e inimigos novos"],
        ["T3", "Coração do Véu", "Boss terrestre Vorthak"],
        ["E1", "Órbita Quebrada", "Troca para nave / shoot'em up"],
        ["E2", "Bloqueio da Frota", "Formações e build convertida"],
        ["E3", "Fenda do Vazio", "Boss espacial Morveth"],
    ]
    t = Table(data, colWidths=[2.2 * cm, 4.5 * cm, 9.5 * cm])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#2a1840")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.HexColor("#efc75f")),
        ("BACKGROUND", (0, 1), (-1, -1), colors.HexColor("#f7f1e6")),
        ("GRID", (0, 0), (-1, -1), 0.4, colors.HexColor("#c9b89a")),
        ("FONTNAME", (0, 0), (-1, -1), FONT_BODY),
        ("FONTSIZE", (0, 0), (-1, -1), 9),
        ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
        ("LEFTPADDING", (0, 0), (-1, -1), 6),
        ("TOPPADDING", (0, 0), (-1, -1), 5),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.HexColor("#f7f1e6"), colors.HexColor("#efe6d4")]),
    ]))
    story.append(t)
    story.append(Spacer(1, 0.35 * cm))
    story.append(Paragraph(
        "A dificuldade sobe por ondas, novos padrões de ataque e bosses. A progressão "
        "(XP, cartas, raridade, conversão terra→espaço) nasce magra e engorda por sprint — "
        "nunca tudo de uma vez. <b>Na demo atual</b>, várias dessas camadas ainda estão "
        "parciais ou em construção e devem mudar com playtests.",
        s["body"],
    ))

    story.append(Paragraph("3.2 Inimigos e obstáculos", s["h2"]))
    story.append(bullets([
        "<b>Esqueleto Guerreiro</b> — arremessa machado; resistência normal.",
        "<b>Ghoul Veloz</b> — cospe ácido em arco; frágil, mas rápido.",
        "<b>Zumbi Corrompido</b> — tanque kamikaze com explosão em área.",
        "Plataformas one-way, parallax de ruínas, chuva e neblina como leitura de fase.",
        "No espaço (planejado): drones, caças, naves pesadas e formações.",
    ], s))
    add_image(story, imgs.get("inimigos"), 13, "Relance de inimigos terrestres nas ruínas", s)

    story.append(Paragraph("3.3 Poderes e habilidades", s["h2"]))
    story.append(bullets([
        "Guerreiro: Corte de Energia + bloqueio com escudo.",
        "Mago: Orbe Arcano (automático em inimigo próximo); no espaço, leque de projéteis.",
        "Arqueiro: Pena Celestial + dash (Shift); nave ágil.",
        "Level up oferece 3 cartas; upgrades de kit (I→II→III→IV) e cartas de classe.",
        "Atributos da terra se convertem na nave após T3.",
        "Valores de dano, vida e raridade de cartas serão rebalanceados ao longo da demo.",
    ], s))
    story.append(Paragraph("Artes oficiais usadas na progressão de kits e naves (sujeitas a revisão):", s["body"]))
    story.append(art_row([
        (imgs.get("card_mago"), "Mago — Orbe Arcano"),
        (imgs.get("card_arqueiro"), "Arqueiro — Pena Celestial"),
    ], 7.8, 8.0, s))
    story.append(Spacer(1, 0.25 * cm))
    story.append(art_row([
        (imgs.get("ship_guerreiro"), "Nave Guerreiro"),
        (imgs.get("ship_mago"), "Nave Mago"),
        (imgs.get("ship_arqueiro"), "Nave Arqueiro"),
    ], 5.2, 3.2, s))
    story.append(PageBreak())


def section_imagens(story, s, imgs):
    story.append(Paragraph("4. Imagens e Mockups", s["h1"]))

    story.append(Paragraph("4.1 Conceito de personagem e cenário", s["h2"]))
    story.append(Paragraph(
        "A direção de arte é <b>pixel art dark fantasy 16-bit</b> na terra e shoot'em up "
        "espacial após a transição. Abaixo, as artes oficiais dos heróis e das naves "
        "(fonte: pasta <b>artes/</b>), seguidas dos conceitos de cenário e bosses.",
        s["body"],
    ))
    story.append(art_row([
        (imgs.get("card_guerreiro"), "Guerreiro"),
        (imgs.get("card_mago"), "Mago"),
        (imgs.get("card_arqueiro"), "Arqueiro"),
    ], 5.2, 7.0, s))
    story.append(Spacer(1, 0.3 * cm))
    story.append(art_row([
        (imgs.get("ship_guerreiro"), "Nave Guerreiro — pesada"),
        (imgs.get("ship_mago"), "Nave Mago — leque"),
        (imgs.get("ship_arqueiro"), "Nave Arqueiro — rápida"),
    ], 5.2, 3.3, s))
    story.append(Spacer(1, 0.35 * cm))
    add_image(story, imgs.get("gameplay"), 12, "Conceito de cenário — combate terrestre nas ruínas", s)
    add_image(story, imgs.get("espaco"), 12, "Conceito — transição terra → espaço", s)

    story.append(Paragraph("4.2 Protótipo de interface (HUD)", s["h2"]))
    story.append(Paragraph(
        "A HUD planejada (e parcialmente em jogo) inclui: barra de vida, barra de defesa/"
        "escudo, XP e nível, banner de onda (“ONDA N”), contador de inimigos e pausa (ESC). "
        "No espaço, os mesmos atributos viram dano, escudo, velocidade e especial da nave. "
        "Cartas de upgrade aparecem em tela de escolha no level up (3 opções).",
        s["body"],
    ))
    hud_data = [
        [Paragraph("<b>Elemento</b>", s["bullet"]), Paragraph("<b>Função</b>", s["bullet"])],
        ["Vida / Defesa", "Sobrevivência e bloqueio (Guerreiro)"],
        ["XP / Nível", "Pausa e escolha de cartas"],
        ["Onda", "Feedback de progressão da fase"],
        ["Especial / Dash", "Habilidade de classe (Arqueiro) ou especial da nave"],
    ]
    ht = Table(hud_data, colWidths=[4.5 * cm, 11.5 * cm])
    ht.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#2a1840")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.HexColor("#efc75f")),
        ("GRID", (0, 0), (-1, -1), 0.4, colors.HexColor("#c9b89a")),
        ("FONTSIZE", (0, 0), (-1, -1), 9),
        ("BACKGROUND", (0, 1), (-1, -1), colors.HexColor("#f7f1e6")),
        ("LEFTPADDING", (0, 0), (-1, -1), 6),
        ("TOPPADDING", (0, 0), (-1, -1), 5),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
    ]))
    story.append(ht)
    story.append(Spacer(1, 0.4 * cm))

    story.append(Paragraph("4.3 Storyboard de uma cena", s["h2"]))
    sb = [
        [Paragraph("<b>Beat</b>", s["bullet"]), Paragraph("<b>O que acontece</b>", s["bullet"])],
        ["1", "Herói nasce nas Ruínas da Borda; aprende A/D, pulo e ataque."],
        ["2", "Onda 1 de esqueletos; feedback de dano e XP."],
        ["3", "Ondas 2–3 com ghoul e zumbi; plataformas e precisão."],
        ["4", "Level up → escolha de carta (+dano / +vida / kit)."],
        ["5", "Avanço até T3 → duelo com Vorthak."],
        ["6", "Embarque na nave → combate espacial até Morveth."],
    ]
    st = Table(sb, colWidths=[1.8 * cm, 14.2 * cm])
    st.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#2a1840")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.HexColor("#efc75f")),
        ("GRID", (0, 0), (-1, -1), 0.4, colors.HexColor("#c9b89a")),
        ("FONTSIZE", (0, 0), (-1, -1), 9),
        ("BACKGROUND", (0, 1), (-1, -1), colors.HexColor("#f7f1e6")),
        ("LEFTPADDING", (0, 0), (-1, -1), 6),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
    ]))
    story.append(st)
    story.append(PageBreak())


def section_funcionamento(story, s):
    story.append(Paragraph("5. Funcionamento do Jogo", s["h1"]))

    story.append(Paragraph("5.1 Controles básicos", s["h2"]))
    story.append(Paragraph("<b>Terra (visão lateral tipo Metal Slug)</b>", s["body"]))
    story.append(bullets([
        "A/D ou setas — andar no eixo X",
        "Espaço / W — pular",
        "Clique / J — atacar (Guerreiro corta na frente)",
        "S / K / botão direito — bloquear com escudo",
        "Shift — dash do Arqueiro",
        "ESC — pausa",
    ], s))
    story.append(Paragraph(
        "No espaço, o mesmo herói controla a nave em shoot'em up: movimento livre no plano "
        "da tela, tiro automático/especial conforme a classe e uso do escudo/velocidade "
        "convertidos da build terrestre.",
        s["body"],
    ))

    story.append(Paragraph("5.2 Interatividade e feedback", s["h2"]))
    story.append(bullets([
        "Hit, morte e ondas com feedback visual (pisca de arming, banners de onda).",
        "XP preenche a barra; level up pausa e oferece 3 cartas.",
        "Inimigos têm IA por fases: perseguir → armar → golpe → recuperar.",
        "Dano respeita resistências por classe via EnemyData.",
        "Game over → menu; vitória só após E3.",
    ], s))

    story.append(Paragraph("5.3 Cenários dinâmicos", s["h2"]))
    story.append(Paragraph(
        "O ambiente terrestre usa <b>parallax em camadas</b> (céu, serras, cidade em ruínas, "
        "props, plano jogável e silhuetas de primeiro plano), chuva, neblina e luzes "
        "pulsantes. Plataformas one-way permitem atravessar por baixo e pousar em cima. "
        "A transição pós-T3 troca o corpo pela nave e o bioma pelo espaço — a build "
        "continua, o gênero muda.",
        s["body"],
    ))
    story.append(PageBreak())


def section_conclusao(story, s):
    story.append(Paragraph("6. Conclusão", s["h1"]))

    story.append(Paragraph("6.1 Visão geral", s["h2"]))
    story.append(Paragraph(
        "<b>Aetherion: Voidfall</b> une dark fantasy de corrida lateral com um segundo ato "
        "espacial, três heróis (Guerreiro, Mago e Arqueiro) e progressão por cartas. "
        "Este GDD cobre a <b>demo / Parte 1</b>: a visão atual para provar o conceito — "
        "não o jogo fechado. Arte, números, fases e sistemas vão mudar várias vezes até "
        "a versão final.",
        s["body"],
    ))

    story.append(Paragraph("6.2 Próximos passos (ainda em aberto na demo)", s["h2"]))
    story.append(bullets([
        "Fechar kits jogáveis do Mago e do Arqueiro com a mesma qualidade do Guerreiro.",
        "Completar arte de naves, inimigos espaciais e bosses (T3/E3).",
        "Implementar camadas restantes de progressão (raridade, cartas de classe, conversão, kits III/IV).",
        "Fases T2–T3 e E1–E3 com fundos e ondas finais.",
        "Polimento de UI/HUD, testes de balanceamento e build WebGL publicada.",
        "Rebalancear e ajustar design a partir de playtests — mudanças contínuas ao longo do processo.",
    ], s))

    story.append(Spacer(1, 0.4 * cm))
    story.append(Paragraph(
        "<b>Lembrete:</b> tudo o que está neste documento é a base da primeira parte. "
        "Vários ajustes serão feitos ao decorrer do desenvolvimento.",
        s["body"],
    ))

    story.append(Spacer(1, 0.8 * cm))
    story.append(Paragraph(
        "“O Véu caiu. O Vazio avança. Ainda existem aqueles dispostos a lutar.”",
        s["cover_sub"],
    ))
    story.append(Paragraph("— Aetherion: Voidfall (Demo / Parte 1)", s["caption"]))


def main():
    ensure_dirs()
    s = styles()
    imgs = {
        "titulo": prepare_img(MEDIA / "trailer-titulo.png", "titulo.jpg"),
        "ruinas": prepare_img(MEDIA / "trailer-ruinas.png", "ruinas.jpg"),
        "boss_terra": prepare_img(MEDIA / "trailer-boss-terra.png", "boss_terra.jpg"),
        "boss_espaco": prepare_img(MEDIA / "trailer-boss-espaco.png", "boss_espaco.jpg"),
        "inimigos": prepare_img(MEDIA / "trailer-inimigos.png", "inimigos.jpg"),
        "gameplay": prepare_img(MEDIA / "trailer-gameplay.png", "gameplay.jpg"),
        "espaco": prepare_img(MEDIA / "trailer-espaco.png", "espaco.jpg"),
        # Artes limpas do zip / ref oficial
        "card_guerreiro": prepare_portrait(SRC_GUERREIRO, "card_guerreiro.jpg"),
        "card_mago": prepare_portrait(SRC_MAGO, "card_mago.jpg"),
        "card_arqueiro": prepare_portrait(SRC_ARQUEIRO, "card_arqueiro.jpg"),
        "ship_arqueiro": prepare_ship(SRC_NAVE_ARQUEIRO, "ship_arqueiro.jpg"),
        "ship_guerreiro": prepare_ship(SRC_NAVE_GUERREIRO, "ship_guerreiro.jpg"),
        "ship_mago": prepare_ship(SRC_NAVE_MAGO, "ship_mago.jpg"),
    }
    missing = [k for k, v in imgs.items() if v is None and k.startswith(("card_", "ship_"))]
    if missing:
        print("AVISO — artes ausentes:", ", ".join(missing))
        for p in [SRC_MAGO, SRC_ARQUEIRO, SRC_GUERREIRO, SRC_NAVE_ARQUEIRO, SRC_NAVE_GUERREIRO, SRC_NAVE_MAGO]:
            print(" ", p, "→", "OK" if p.exists() else "FALTA")


    doc = SimpleDocTemplate(
        str(OUT),
        pagesize=A4,
        leftMargin=1.8 * cm,
        rightMargin=1.8 * cm,
        topMargin=1.8 * cm,
        bottomMargin=1.5 * cm,
        title="Aetherion: Voidfall — Game Design Document",
        author="Equipe Aetherion",
    )
    story = []
    cover_page(story, s, imgs)
    section_historia(story, s, imgs)
    section_objetivo(story, s)
    section_desafios(story, s, imgs)
    section_imagens(story, s, imgs)
    section_funcionamento(story, s)
    section_conclusao(story, s)

    doc.build(story, onFirstPage=header_footer, onLaterPages=header_footer)
    print(f"PDF gerado: {OUT}")
    print(f"Tamanho: {OUT.stat().st_size / 1024:.0f} KB")


if __name__ == "__main__":
    main()
