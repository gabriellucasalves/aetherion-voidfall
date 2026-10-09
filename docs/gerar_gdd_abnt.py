#!/usr/bin/env python3
"""GDD Aetherion: Apocalipse em papel timbrado UNICEPLAC e normas ABNT (NBR 14724)."""
from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image as PILImage
from reportlab.lib.colors import HexColor, black
from reportlab.lib.enums import TA_CENTER, TA_JUSTIFY, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import cm
from reportlab.platypus import (
    BaseDocTemplate,
    Frame,
    Image,
    KeepTogether,
    ListFlowable,
    ListItem,
    NextPageTemplate,
    PageBreak,
    PageTemplate,
    Paragraph,
    Spacer,
    Table,
    TableStyle,
)

from gerar_gdd_pdf import (
    SRC_ARQUEIRO,
    SRC_GUERREIRO,
    SRC_MAGO,
    SRC_NAVE_ARQUEIRO,
    SRC_NAVE_GUERREIRO,
    SRC_NAVE_MAGO,
    MEDIA,
    ROOT,
    prepare_img,
    prepare_portrait,
    prepare_ship,
)

TEXT = black
LINE = HexColor("#666666")
SOFT = HexColor("#F2F2F2")

NEXUS_DIR = Path(
    "/Users/gabriellucas/Developer/odoo-local/tmp/cronograma/nexus-cronograma"
)
ASSETS = Path(
    "/Users/gabriellucas/.cursor/projects/Users-gabriellucas-Developer-odoo-local/assets"
)
TIMBRADO = NEXUS_DIR / "timbrado" / "image1.png"
LOGO_UNICEPLAC = ASSETS / "Vertical-Colorida-16908678-ffd7-4ff3-8e03-3338384b33e4.png"
LOGO_TRIM = ROOT / "docs" / "_gdd_img" / "_uniceplac_logo_trim.png"

OUT = ROOT / "docs" / "Aetherion-Apocalipse-GDD.pdf"

INTEGRANTES = [
    "Bianca Xavier de Oliveira",
    "Gabriel Lucas Alves da Silva",
    "Isabela Rosa Dos Santos Gontijo",
    "Samer Osama Mohammad Taleeb",
    "Thiago Costa Renovato",
    "Wesley Thiago Matias Xavier",
]
TITULO = "AETHERION: APOCALIPSE"
SUBTITULO = "Game Design Document — Demo / Parte 1"
CURSO = "Curso de Análise e Desenvolvimento de Sistemas"
DISCIPLINA = "Desenvolvimento de Games"

MARGIN_LEFT = 3 * cm
MARGIN_RIGHT = 2 * cm
MARGIN_TOP = 4.6 * cm
MARGIN_TOP_CAPA = 2.35 * cm
MARGIN_BOTTOM = 4.2 * cm


def trim_logo(src: Path, dest: Path) -> Path:
    dest.parent.mkdir(parents=True, exist_ok=True)
    im = PILImage.open(src).convert("RGBA")
    w, h = im.size
    pixels = im.load()
    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            if r < 40 and g < 40 and b < 40:
                pixels[x, y] = (r, g, b, 0)
    bbox = im.getbbox()
    if bbox:
        im = im.crop(bbox)
    im.save(dest)
    return dest


def styles():
    base = getSampleStyleSheet()
    lead = 18
    return {
        "capa_inst": ParagraphStyle(
            "capa_inst", parent=base["Normal"], fontName="Times-Bold",
            fontSize=12, leading=16, alignment=TA_CENTER, textColor=TEXT,
            spaceBefore=4, spaceAfter=8,
        ),
        "capa_titulo": ParagraphStyle(
            "capa_titulo", parent=base["Normal"], fontName="Times-Bold",
            fontSize=14, leading=20, alignment=TA_CENTER, textColor=TEXT, spaceAfter=6,
        ),
        "capa_centro": ParagraphStyle(
            "capa_centro", parent=base["Normal"], fontName="Times-Roman",
            fontSize=12, leading=lead, alignment=TA_CENTER, textColor=TEXT, spaceAfter=2,
        ),
        "capa_local": ParagraphStyle(
            "capa_local", parent=base["Normal"], fontName="Times-Roman",
            fontSize=12, leading=lead, alignment=TA_CENTER, textColor=TEXT,
        ),
        "h1": ParagraphStyle(
            "h1", parent=base["Heading1"], fontName="Times-Bold",
            fontSize=12, leading=lead, textColor=TEXT, spaceBefore=12, spaceAfter=12,
            alignment=TA_JUSTIFY,
        ),
        "h2": ParagraphStyle(
            "h2", parent=base["Heading2"], fontName="Times-Bold",
            fontSize=12, leading=lead, textColor=TEXT, spaceBefore=12, spaceAfter=6,
            alignment=TA_JUSTIFY,
        ),
        "body": ParagraphStyle(
            "body", parent=base["Normal"], fontName="Times-Roman",
            fontSize=12, leading=lead, textColor=TEXT, alignment=TA_JUSTIFY,
            firstLineIndent=1.25 * cm, spaceAfter=0,
        ),
        "body0": ParagraphStyle(
            "body0", parent=base["Normal"], fontName="Times-Roman",
            fontSize=12, leading=lead, textColor=TEXT, alignment=TA_JUSTIFY,
            firstLineIndent=0, spaceAfter=6,
        ),
        "legenda": ParagraphStyle(
            "legenda", parent=base["Normal"], fontName="Times-Roman",
            fontSize=10, leading=12, alignment=TA_CENTER, textColor=TEXT,
            spaceBefore=4, spaceAfter=10,
        ),
        "titulo_tabela": ParagraphStyle(
            "titulo_tabela", parent=base["Normal"], fontName="Times-Roman",
            fontSize=10, leading=12, alignment=TA_CENTER, textColor=TEXT,
            spaceBefore=8, spaceAfter=4,
        ),
        "th": ParagraphStyle(
            "th", parent=base["Normal"], fontName="Times-Bold",
            fontSize=10, leading=13, alignment=TA_CENTER, textColor=TEXT,
        ),
        "td": ParagraphStyle(
            "td", parent=base["Normal"], fontName="Times-Roman",
            fontSize=10, leading=13, alignment=TA_JUSTIFY, textColor=TEXT,
        ),
        "tdc": ParagraphStyle(
            "tdc", parent=base["Normal"], fontName="Times-Roman",
            fontSize=10, leading=13, alignment=TA_CENTER, textColor=TEXT,
        ),
        "bullet": ParagraphStyle(
            "bullet", parent=base["Normal"], fontName="Times-Roman",
            fontSize=12, leading=lead, textColor=TEXT,
        ),
        "art_label": ParagraphStyle(
            "art_label", parent=base["Normal"], fontName="Times-Bold",
            fontSize=10, leading=12, alignment=TA_CENTER, textColor=TEXT,
            spaceBefore=4, spaceAfter=4,
        ),
        "ref": ParagraphStyle(
            "ref", parent=base["Normal"], fontName="Times-Roman",
            fontSize=12, leading=lead, textColor=TEXT, alignment=TA_LEFT,
            firstLineIndent=-1.25 * cm, leftIndent=1.25 * cm, spaceAfter=8,
        ),
        "nota": ParagraphStyle(
            "nota", parent=base["Normal"], fontName="Times-Roman",
            fontSize=12, leading=lead, alignment=TA_JUSTIFY, textColor=TEXT,
        ),
    }


def bullets(items, style):
    return ListFlowable(
        [ListItem(Paragraph(i, style), leftIndent=18) for i in items],
        bulletType="bullet",
        start="•",
        leftIndent=1.25 * cm,
        bulletFontName="Times-Roman",
        bulletFontSize=12,
        spaceBefore=6,
        spaceAfter=6,
    )


def art_row(paths_labels, col_w_cm, img_h_cm, s):
    """Grade de artes sem fundo decorativo — padrão acadêmico."""
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
        ("BOX", (0, 0), (-1, -1), 0.4, LINE),
        ("INNERGRID", (0, 0), (-1, -1), 0.3, LINE),
    ]))
    return t


def tabela(data, col_widths):
    t = Table(data, colWidths=col_widths, repeatRows=1)
    t.setStyle(TableStyle([
        ("FONTNAME", (0, 0), (-1, 0), "Times-Bold"),
        ("FONTNAME", (0, 1), (-1, -1), "Times-Roman"),
        ("FONTSIZE", (0, 0), (-1, -1), 10),
        ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
        ("LEFTPADDING", (0, 0), (-1, -1), 4),
        ("RIGHTPADDING", (0, 0), (-1, -1), 4),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
        ("GRID", (0, 0), (-1, -1), 0.4, LINE),
        ("BACKGROUND", (0, 0), (-1, 0), SOFT),
    ]))
    return t


def draw_letterhead(canvas, doc):
    canvas.saveState()
    if TIMBRADO.exists():
        canvas.drawImage(
            str(TIMBRADO), 0, 0, width=A4[0], height=A4[1],
            preserveAspectRatio=False, mask="auto",
        )
    if doc.page > 2:
        canvas.setFont("Times-Roman", 10)
        canvas.setFillColor(TEXT)
        canvas.drawRightString(A4[0] - MARGIN_RIGHT, A4[1] - 4.15 * cm, str(doc.page - 2))
    canvas.restoreState()


def centered_image(path: Path, height_cm: float, usable: float):
    im = PILImage.open(path)
    w, h = im.size
    height = height_cm * cm
    width = height * (w / h)
    flow = Image(str(path), width=width, height=height)
    box = Table([[flow]], colWidths=[usable])
    box.setStyle(TableStyle([
        ("ALIGN", (0, 0), (-1, -1), "CENTER"),
        ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
    ]))
    return box


def add_figure(story, path: Path | None, width_cm: float, caption: str, s):
    if not path or not path.exists():
        return
    im = PILImage.open(path)
    w, h = im.size
    tw = width_cm * cm
    th = tw * (h / w)
    max_h = 8.5 * cm
    if th > max_h:
        th = max_h
        tw = th * (w / h)
    story.append(KeepTogether([
        Image(str(path), width=tw, height=th),
        Paragraph(caption, s["legenda"]),
    ]))


def build_imgs():
    return {
        "titulo": prepare_img(MEDIA / "trailer-titulo.png", "titulo.jpg"),
        "ruinas": prepare_img(MEDIA / "trailer-ruinas.png", "ruinas.jpg"),
        "boss_terra": prepare_img(MEDIA / "trailer-boss-terra.png", "boss_terra.jpg"),
        "boss_espaco": prepare_img(MEDIA / "trailer-boss-espaco.png", "boss_espaco.jpg"),
        "inimigos": prepare_img(MEDIA / "trailer-inimigos.png", "inimigos.jpg"),
        "gameplay": prepare_img(MEDIA / "trailer-gameplay.png", "gameplay.jpg"),
        "espaco": prepare_img(MEDIA / "trailer-espaco.png", "espaco.jpg"),
        "card_guerreiro": prepare_portrait(SRC_GUERREIRO, "card_guerreiro.jpg"),
        "card_mago": prepare_portrait(SRC_MAGO, "card_mago.jpg"),
        "card_arqueiro": prepare_portrait(SRC_ARQUEIRO, "card_arqueiro.jpg"),
        "ship_arqueiro": prepare_ship(SRC_NAVE_ARQUEIRO, "ship_arqueiro.jpg"),
        "ship_guerreiro": prepare_ship(SRC_NAVE_GUERREIRO, "ship_guerreiro.jpg"),
        "ship_mago": prepare_ship(SRC_NAVE_MAGO, "ship_mago.jpg"),
    }


def capa_e_rosto(story, S, logo_uni, usable):
    story.append(NextPageTemplate("Capa"))
    story.append(centered_image(logo_uni, 3.5, usable))
    story.append(Paragraph(
        "CENTRO UNIVERSITÁRIO DO PLANALTO CENTRAL APPARECIDO DOS SANTOS",
        S["capa_inst"],
    ))
    story.append(Paragraph(CURSO, S["capa_centro"]))
    story.append(Paragraph(DISCIPLINA, S["capa_centro"]))
    story.append(Spacer(1, 1.2 * cm))
    story.append(Paragraph(TITULO, S["capa_titulo"]))
    story.append(Paragraph(SUBTITULO, S["capa_centro"]))
    story.append(Spacer(1, 0.8 * cm))
    for nome in INTEGRANTES:
        story.append(Paragraph(nome, S["capa_centro"]))
    story.append(Spacer(1, 1.6 * cm))
    story.append(Paragraph("Gama – DF", S["capa_local"]))
    story.append(Paragraph("2026", S["capa_local"]))

    story.append(NextPageTemplate("Folha"))
    story.append(PageBreak())
    for nome in INTEGRANTES:
        story.append(Paragraph(nome, S["capa_centro"]))
    story.append(Spacer(1, 1.6 * cm))
    story.append(Paragraph(TITULO, S["capa_titulo"]))
    story.append(Paragraph(SUBTITULO, S["capa_centro"]))
    story.append(Spacer(1, 1.6 * cm))
    nota = Paragraph(
        "Game Design Document apresentado à disciplina de Desenvolvimento de Games, "
        "do Curso de Análise e Desenvolvimento de Sistemas do Centro Universitário "
        "do Planalto Central Apparecido dos Santos – UNICEPLAC, como requisito da "
        "demo / Parte 1 do jogo Aetherion: Apocalipse.",
        S["nota"],
    )
    nota_tab = Table([["", nota]], colWidths=[usable - 8.7 * cm, 8.7 * cm])
    nota_tab.setStyle(TableStyle([
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), 0),
        ("RIGHTPADDING", (0, 0), (-1, -1), 0),
    ]))
    story.append(nota_tab)
    story.append(Spacer(1, 5.0 * cm))
    story.append(KeepTogether([
        Paragraph("Gama – DF", S["capa_local"]),
        Paragraph("2026", S["capa_local"]),
    ]))
    story.append(NextPageTemplate("Corpo"))
    story.append(PageBreak())


def corpo(story, S, imgs, usable):
    story.append(Paragraph("1 INTRODUÇÃO", S["h1"]))
    story.append(Paragraph(
        "Este documento descreve a primeira parte (demo) de Aetherion: Apocalipse, "
        "protótipo acadêmico em pixel art 16-bit, com combate terrestre em visão "
        "lateral e ato espacial em shoot’em up. Arte, balanceamento, fases, "
        "inimigos e sistemas ainda sofrerão ajustes ao longo do desenvolvimento. "
        "O que se lê a seguir é a visão atual da equipe — não o produto final.",
        S["body"],
    ))
    story.append(Paragraph(
        "O gênero une dark fantasy na superfície (referência de leitura: Metal Slug) "
        "e combate espacial após a transição. A demo deve provar o combate terrestre, "
        "a identidade dos três heróis e a passagem para o espaço.",
        S["body"],
    ))
    add_figure(story, imgs.get("titulo"), 14, "Figura 1 – Arte conceitual do título e da atmosfera do Véu", S)
    story.append(Paragraph("Fonte: acervo do projeto Aetherion (2026).", S["legenda"]))

    story.append(Paragraph("2 HISTÓRIA DO JOGO", S["h1"]))
    story.append(Paragraph("2.1 Contexto", S["h2"]))
    story.append(Paragraph(
        "O jogo se desenrola em Aetherion, mundo protegido por milênios por uma "
        "barreira cósmica chamada Véu. Quando algo desperta além das estrelas, o Véu "
        "é destruído e o Vazio invade o planeta. Cidades viram ruínas, os mortos não "
        "descansam e a guerra se espalha da superfície até a órbita. O jogador "
        "atravessa seis fases: três terrestres (ruínas, cidades sumidas, coração do "
        "Véu) e três espaciais (órbita quebrada, bloqueio da frota, fenda do Vazio).",
        S["body"],
    ))
    add_figure(story, imgs.get("ruinas"), 14, "Figura 2 – Aetherion em ruínas sob a chuva e o Véu partido", S)
    story.append(Paragraph("Fonte: acervo do projeto Aetherion (2026).", S["legenda"]))

    story.append(Paragraph("2.2 Protagonistas", S["h2"]))
    story.append(Paragraph(
        "Não há um único herói: o jogador escolhe entre três resistentes, cada um "
        "com kit e nave próprios. Todos compartilham a missão de impedir que o Vazio "
        "consuma Aetherion, mas jogam de forma distinta.",
        S["body"],
    ))
    story.append(bullets([
        "<b>Guerreiro</b> — tanque corpo a corpo: vida e defesa altas, Corte de Energia e escudo. Nave pesada, tiro lento e forte;",
        "<b>Mago</b> — dano à distância: Orbe Arcano, vida baixa e poder alto. Nave frágil com projéteis em leque;",
        "<b>Arqueiro</b> — mobilidade: Pena Celestial, dash e equilíbrio de atributos. Nave rápida com tiro frequente.",
    ], S["bullet"]))
    story.append(Paragraph("Figura 3 – Conceito dos três heróis da demo", S["titulo_tabela"]))
    story.append(art_row([
        (imgs.get("card_guerreiro"), "Guerreiro"),
        (imgs.get("card_mago"), "Mago"),
        (imgs.get("card_arqueiro"), "Arqueiro"),
    ], 5.0, 6.6, S))
    story.append(Paragraph("Fonte: acervo do projeto Aetherion (2026).", S["legenda"]))
    story.append(Paragraph("Figura 4 – Naves por classe no ato espacial", S["titulo_tabela"]))
    story.append(art_row([
        (imgs.get("ship_guerreiro"), "Nave do Guerreiro"),
        (imgs.get("ship_mago"), "Nave do Mago"),
        (imgs.get("ship_arqueiro"), "Nave do Arqueiro"),
    ], 5.0, 3.2, S))
    story.append(Paragraph("Fonte: acervo do projeto Aetherion (2026).", S["legenda"]))
    story.append(Paragraph(
        "Nomes, kits e artes podem mudar até a versão final. Nesta demo, o foco é "
        "provar o combate terrestre, a identidade dos três heróis e a transição para o espaço.",
        S["body"],
    ))

    story.append(Paragraph("2.3 Antagonistas", S["h2"]))
    story.append(Paragraph(
        "Na terra, o jogador enfrenta hordas corrompidas pelo Vazio — esqueletos, "
        "ghouls e zumbis — e o chefe Vorthak, o Olhar Eterno, nas ruínas. No espaço, "
        "a frota do Vazio e o chefe final Morveth, o Encapuzado, aguardam na fenda "
        "entre as estrelas. O desafio não é apenas sobreviver às ondas: é carregar a "
        "build da terra até o duelo espacial.",
        S["body"],
    ))
    add_figure(story, imgs.get("boss_terra"), 12, "Figura 5 – Boss terrestre Vorthak, o Olhar Eterno", S)
    story.append(Paragraph("Fonte: acervo do projeto Aetherion (2026).", S["legenda"]))
    add_figure(story, imgs.get("boss_espaco"), 12, "Figura 6 – Boss espacial Morveth, o Encapuzado", S)
    story.append(Paragraph("Fonte: acervo do projeto Aetherion (2026).", S["legenda"]))

    story.append(Paragraph("3 OBJETIVO DO JOGO", S["h1"]))
    story.append(Paragraph("3.1 Meta principal", S["h2"]))
    story.append(Paragraph(
        "Guiar o herói escolhido pelas fases disponíveis nesta demo (Parte 1), com o "
        "arco completo planejado em seis fases: derrotar o boss terrestre em T3, "
        "embarcar na nave e vencer o boss espacial em E3. A vitória completa só "
        "acontece após derrotar Morveth na versão final. Nesta demo, a run não "
        "persiste entre sessões: o game over retorna ao menu.",
        S["body"],
    ))
    story.append(Paragraph("3.2 Submetas", S["h2"]))
    story.append(bullets([
        "sobreviver a ondas crescentes de inimigos (esqueleto, ghoul, zumbi e demais);",
        "ganhar experiência, subir de nível e escolher cartas de aprimoramento;",
        "converter a build da terra em atributos da nave (força em dano, defesa em escudo, agilidade em velocidade, poder em especial);",
        "usar colecionáveis e cartas raras de classe nas camadas futuras de progressão;",
        "dominar plataformas unidirecionais, pulos e padrões de ataque distintos por inimigo.",
    ], S["bullet"]))

    story.append(Paragraph("4 DESAFIOS E MECÂNICAS", S["h1"]))
    story.append(Paragraph("4.1 Níveis e progressão", S["h2"]))
    story.append(Paragraph("Tabela 1 – Fases planejadas da demo e do arco completo", S["titulo_tabela"]))
    fases = [
        [Paragraph("Fase", S["th"]), Paragraph("Nome", S["th"]), Paragraph("Foco", S["th"])],
        [Paragraph("T1", S["tdc"]), Paragraph("Ruínas da Borda", S["td"]), Paragraph("Tutorial de movimento, ataque e bioma", S["td"])],
        [Paragraph("T2", S["tdc"]), Paragraph("Cidades Sumidas", S["td"]), Paragraph("Maior densidade e inimigos novos", S["td"])],
        [Paragraph("T3", S["tdc"]), Paragraph("Coração do Véu", S["td"]), Paragraph("Boss terrestre Vorthak", S["td"])],
        [Paragraph("E1", S["tdc"]), Paragraph("Órbita Quebrada", S["td"]), Paragraph("Troca para nave / shoot’em up", S["td"])],
        [Paragraph("E2", S["tdc"]), Paragraph("Bloqueio da Frota", S["td"]), Paragraph("Formações e build convertida", S["td"])],
        [Paragraph("E3", S["tdc"]), Paragraph("Fenda do Vazio", S["td"]), Paragraph("Boss espacial Morveth", S["td"])],
    ]
    story.append(tabela(fases, [usable * 0.16, usable * 0.32, usable * 0.52]))
    story.append(Paragraph("Fonte: elaborada pelos autores (2026).", S["legenda"]))
    story.append(Paragraph(
        "A dificuldade sobe por ondas, novos padrões de ataque e chefes. A progressão "
        "(experiência, cartas, raridade, conversão terra–espaço) nasce reduzida e "
        "cresce por sprint. Na demo atual, várias dessas camadas ainda estão parciais "
        "e devem mudar com testes de jogabilidade.",
        S["body"],
    ))

    story.append(Paragraph("4.2 Inimigos e obstáculos", S["h2"]))
    story.append(bullets([
        "<b>Esqueleto Guerreiro</b> — arremessa machado; resistência normal;",
        "<b>Ghoul Veloz</b> — cospe ácido em arco; frágil, porém rápido;",
        "<b>Zumbi Corrompido</b> — tanque kamikaze com explosão em área;",
        "plataformas unidirecionais, parallax de ruínas, chuva e neblina como leitura de fase;",
        "no espaço (planejado): drones, caças, naves pesadas e formações.",
    ], S["bullet"]))
    add_figure(story, imgs.get("inimigos"), 13, "Figura 7 – Inimigos terrestres nas ruínas", S)
    story.append(Paragraph("Fonte: acervo do projeto Aetherion (2026).", S["legenda"]))

    story.append(Paragraph("4.3 Poderes e habilidades", S["h2"]))
    story.append(bullets([
        "Guerreiro: Corte de Energia e bloqueio com escudo;",
        "Mago: Orbe Arcano (automático no inimigo próximo); no espaço, leque de projéteis;",
        "Arqueiro: Pena Celestial e dash (Shift); nave ágil;",
        "o aumento de nível oferece três cartas; há aprimoramento de kit (I a IV) e cartas de classe;",
        "os atributos da terra se convertem na nave após T3;",
        "valores de dano, vida e raridade serão rebalanceados ao longo da demo.",
    ], S["bullet"]))

    story.append(Paragraph("5 IMAGENS E PROTÓTIPOS", S["h1"]))
    story.append(Paragraph("5.1 Conceito de personagem e cenário", S["h2"]))
    story.append(Paragraph(
        "A direção de arte é pixel art dark fantasy 16-bit na terra e shoot’em up "
        "espacial após a transição. As artes oficiais dos heróis e das naves "
        "encontram-se na pasta artes/ do repositório.",
        S["body"],
    ))
    add_figure(story, imgs.get("gameplay"), 12, "Figura 8 – Conceito de combate terrestre nas ruínas", S)
    story.append(Paragraph("Fonte: acervo do projeto Aetherion (2026).", S["legenda"]))
    add_figure(story, imgs.get("espaco"), 12, "Figura 9 – Conceito da transição terra–espaço", S)
    story.append(Paragraph("Fonte: acervo do projeto Aetherion (2026).", S["legenda"]))

    story.append(Paragraph("5.2 Protótipo de interface", S["h2"]))
    story.append(Paragraph(
        "A interface planejada — e parcialmente implementada — inclui barra de vida, "
        "barra de defesa ou escudo, experiência e nível, faixa de onda, contador de "
        "inimigos e pausa (ESC). No espaço, os mesmos atributos tornam-se dano, "
        "escudo, velocidade e especial da nave. As cartas de aprimoramento aparecem "
        "em tela de escolha no aumento de nível (três opções).",
        S["body"],
    ))
    story.append(Paragraph("Tabela 2 – Elementos da interface e respectiva função", S["titulo_tabela"]))
    hud = [
        [Paragraph("Elemento", S["th"]), Paragraph("Função", S["th"])],
        [Paragraph("Vida / defesa", S["td"]), Paragraph("Sobrevivência e bloqueio (Guerreiro)", S["td"])],
        [Paragraph("Experiência / nível", S["td"]), Paragraph("Pausa e escolha de cartas", S["td"])],
        [Paragraph("Onda", S["td"]), Paragraph("Retorno visual da progressão da fase", S["td"])],
        [Paragraph("Especial / dash", S["td"]), Paragraph("Habilidade de classe ou especial da nave", S["td"])],
    ]
    story.append(tabela(hud, [usable * 0.34, usable * 0.66]))
    story.append(Paragraph("Fonte: elaborada pelos autores (2026).", S["legenda"]))

    story.append(Paragraph("5.3 Storyboard de uma cena", S["h2"]))
    story.append(Paragraph("Tabela 3 – Sequência de uma run da demo", S["titulo_tabela"]))
    sb = [
        [Paragraph("Beat", S["th"]), Paragraph("O que acontece", S["th"])],
        [Paragraph("1", S["tdc"]), Paragraph("O herói nasce nas Ruínas da Borda e aprende a andar, pular e atacar.", S["td"])],
        [Paragraph("2", S["tdc"]), Paragraph("Onda 1 de esqueletos; retorno de dano e experiência.", S["td"])],
        [Paragraph("3", S["tdc"]), Paragraph("Ondas 2 e 3 com ghoul e zumbi; plataformas e precisão.", S["td"])],
        [Paragraph("4", S["tdc"]), Paragraph("Aumento de nível e escolha de carta (dano, vida ou kit).", S["td"])],
        [Paragraph("5", S["tdc"]), Paragraph("Avanço até T3 e duelo com Vorthak.", S["td"])],
        [Paragraph("6", S["tdc"]), Paragraph("Embarque na nave e combate espacial até Morveth.", S["td"])],
    ]
    story.append(tabela(sb, [usable * 0.14, usable * 0.86]))
    story.append(Paragraph("Fonte: elaborada pelos autores (2026).", S["legenda"]))

    story.append(Paragraph("6 FUNCIONAMENTO DO JOGO", S["h1"]))
    story.append(Paragraph("6.1 Controles básicos", S["h2"]))
    story.append(Paragraph(
        "Na terra, a visão é lateral. Os comandos previstos são: A/D ou setas para "
        "andar; espaço ou W para pular; clique ou J para atacar; S, K ou botão direito "
        "para bloquear com escudo; Shift para o dash do Arqueiro; ESC para pausar.",
        S["body"],
    ))
    story.append(Paragraph(
        "No espaço, o mesmo herói controla a nave em shoot’em up: movimento livre no "
        "plano da tela, tiro automático ou especial conforme a classe e uso do escudo "
        "e da velocidade convertidos da build terrestre.",
        S["body"],
    ))
    story.append(Paragraph("6.2 Interatividade e retorno ao jogador", S["h2"]))
    story.append(bullets([
        "acerto, morte e ondas com retorno visual;",
        "a barra de experiência enche; o aumento de nível pausa e oferece três cartas;",
        "inimigos têm inteligência por fases: perseguir, armar, golpear e recuperar;",
        "o dano respeita resistências por classe;",
        "o game over retorna ao menu; a vitória só ocorre após E3.",
    ], S["bullet"]))
    story.append(Paragraph("6.3 Cenários dinâmicos", S["h2"]))
    story.append(Paragraph(
        "O ambiente terrestre usa parallax em camadas (céu, serras, cidade em ruínas, "
        "adereços, plano jogável e silhuetas de primeiro plano), chuva, neblina e "
        "luzes pulsantes. Plataformas unidirecionais permitem atravessar por baixo e "
        "pousar em cima. A transição após T3 troca o corpo pela nave e o bioma pelo "
        "espaço: a build continua e o gênero muda.",
        S["body"],
    ))

    story.append(Paragraph("7 CONCLUSÃO", S["h1"]))
    story.append(Paragraph(
        "Aetherion: Apocalipse une dark fantasy de corrida lateral a um segundo ato "
        "espacial, três heróis (Guerreiro, Mago e Arqueiro) e progressão por cartas. "
        "Este Game Design Document cobre a demo / Parte 1: a visão atual para provar "
        "o conceito — não o jogo fechado. Arte, números, fases e sistemas mudarão "
        "várias vezes até a versão final.",
        S["body"],
    ))
    story.append(Paragraph("7.1 Próximos passos", S["h2"]))
    story.append(bullets([
        "fechar os kits jogáveis do Mago e do Arqueiro com a mesma qualidade do Guerreiro;",
        "completar arte de naves, inimigos espaciais e chefes (T3 e E3);",
        "implementar as camadas restantes de progressão (raridade, cartas de classe, conversão, kits III e IV);",
        "concluir as fases T2–T3 e E1–E3 com fundos e ondas finais;",
        "polir a interface, testar o balanceamento e publicar a build WebGL;",
        "rebalancear o design a partir dos testes de jogabilidade.",
    ], S["bullet"]))
    story.append(Paragraph(
        "Tudo o que está neste documento é a base da primeira parte. Vários ajustes "
        "serão feitos ao longo do desenvolvimento.",
        S["body"],
    ))

    story.append(Paragraph("REFERÊNCIAS", S["h1"]))
    for ref in [
        "AETHERION: APOCALIPSE. Protótipo acadêmico em Unity. Gama: UNICEPLAC, 2026. Disponível em: https://aetherion-voidfall.vercel.app. Acesso em: 11 set. 2026.",
        "ASSOCIAÇÃO BRASILEIRA DE NORMAS TÉCNICAS. NBR 14724: informação e documentação — trabalhos acadêmicos — apresentação. Rio de Janeiro: ABNT, 2011.",
        "EQUIPE AETHERION. Aetherion: Apocalipse — repositório do projeto. GitHub, 2026. Disponível em: https://github.com/gabriellucasalves/aetherion-voidfall. Acesso em: 11 set. 2026.",
    ]:
        story.append(Paragraph(ref, S["ref"]))


def main():
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    (ROOT / "docs" / "_gdd_img").mkdir(parents=True, exist_ok=True)
    S = styles()
    usable = A4[0] - MARGIN_LEFT - MARGIN_RIGHT
    logo_uni = trim_logo(LOGO_UNICEPLAC, LOGO_TRIM) if LOGO_UNICEPLAC.exists() else None
    imgs = build_imgs()

    story = []
    if logo_uni:
        capa_e_rosto(story, S, logo_uni, usable)
    else:
        story.append(NextPageTemplate("Corpo"))
    corpo(story, S, imgs, usable)

    doc = BaseDocTemplate(
        str(OUT),
        pagesize=A4,
        title="Aetherion: Apocalipse — Game Design Document",
        author="Equipe Aetherion",
        subject="GDD ABNT — UNICEPLAC",
    )
    frame_capa = Frame(
        MARGIN_LEFT, MARGIN_BOTTOM,
        A4[0] - MARGIN_LEFT - MARGIN_RIGHT,
        A4[1] - MARGIN_TOP_CAPA - MARGIN_BOTTOM,
        id="capa",
    )
    frame_corpo = Frame(
        MARGIN_LEFT, MARGIN_BOTTOM,
        A4[0] - MARGIN_LEFT - MARGIN_RIGHT,
        A4[1] - MARGIN_TOP - MARGIN_BOTTOM,
        id="corpo",
    )

    def on_capa(c, d):
        d.page_template = "Capa"
        draw_letterhead(c, d)

    def on_folha(c, d):
        d.page_template = "Folha"
        draw_letterhead(c, d)

    def on_corpo(c, d):
        d.page_template = "Corpo"
        draw_letterhead(c, d)

    doc.addPageTemplates([
        PageTemplate(id="Capa", frames=[frame_capa], onPage=on_capa),
        PageTemplate(id="Folha", frames=[frame_capa], onPage=on_folha),
        PageTemplate(id="Corpo", frames=[frame_corpo], onPage=on_corpo),
    ])
    doc.build(story)
    print(OUT)
    print(f"{OUT.stat().st_size / 1024:.0f} KB")


if __name__ == "__main__":
    main()
