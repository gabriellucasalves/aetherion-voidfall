import math
from pathlib import Path

import bpy

ROOT = Path("/Users/gabriellucas/Desktop/trabalho jogo sexta/Assets/Art")
BLEND = ROOT / "Guerreiro8bit.blend"
GLB = ROOT / "Guerreiro8bit.glb"
REF = ROOT / "guerreiro_ref.png"


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)


def material(name, color, emission=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    rgba = (*color, 1.0)
    bsdf.inputs["Base Color"].default_value = rgba
    bsdf.inputs["Roughness"].default_value = 1.0
    if "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = 0.0
    elif "Specular" in bsdf.inputs:
        bsdf.inputs["Specular"].default_value = 0.0
    if emission > 0:
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = rgba
            bsdf.inputs["Emission Strength"].default_value = emission
        elif "Emission" in bsdf.inputs:
            bsdf.inputs["Emission"].default_value = rgba
    mat.use_backface_culling = True
    return mat


def box(name, loc, size, mat, parent):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    for poly in obj.data.polygons:
        poly.use_smooth = False
    obj.parent = parent
    return obj


def build():
    reset_scene()

    dark = material("ArmorDark", (0.22, 0.04, 0.05))
    red = material("ArmorRed", (0.82, 0.09, 0.08))
    bright = material("ArmorBright", (0.95, 0.22, 0.12))
    glow_o = material("GlowOrange", (1.0, 0.38, 0.06), emission=8.0)
    glow_y = material("GlowYellow", (1.0, 0.86, 0.28), emission=12.0)
    plume = material("Plume", (0.72, 0.08, 0.1))

    root = bpy.data.objects.new("Guerreiro8bit", None)
    bpy.context.collection.objects.link(root)

    # Pés
    box("Bota_E", (-1.4, 0.1, 0.7), (1.8, 2.4, 1.4), dark, root)
    box("Bota_D", (1.4, 0.1, 0.7), (1.8, 2.4, 1.4), dark, root)
    box("BrilhoPe_E", (-1.4, 0.9, 0.7), (1.2, 0.25, 0.7), glow_o, root)
    box("BrilhoPe_D", (1.4, 0.9, 0.7), (1.2, 0.25, 0.7), glow_o, root)

    # Pernas
    box("Perna_E", (-1.35, 0.0, 3.3), (1.7, 2.0, 4.0), red, root)
    box("Perna_D", (1.35, 0.0, 3.3), (1.7, 2.0, 4.0), red, root)
    box("Joelho_E", (-1.35, 0.3, 4.4), (1.9, 2.2, 1.2), bright, root)
    box("Joelho_D", (1.35, 0.3, 4.4), (1.9, 2.2, 1.2), bright, root)
    box("VeiaPerna_E", (-1.35, 1.05, 3.1), (0.35, 0.2, 2.6), glow_o, root)
    box("VeiaPerna_D", (1.35, 1.05, 3.1), (0.35, 0.2, 2.6), glow_o, root)

    # Cintura / torso
    box("Cintura", (0.0, 0.0, 5.8), (4.4, 2.4, 1.6), dark, root)
    box("Torso", (0.0, 0.05, 8.4), (5.2, 2.8, 4.0), red, root)
    box("PeitoAlto", (0.0, 0.15, 10.2), (5.0, 2.6, 1.6), bright, root)
    box("V_E", (-0.55, 1.45, 8.9), (0.45, 0.25, 1.8), glow_y, root)
    box("V_D", (0.55, 1.45, 8.9), (0.45, 0.25, 1.8), glow_y, root)
    box("V_Base", (0.0, 1.45, 8.15), (1.4, 0.25, 0.4), glow_y, root)
    box("VeiaPeito_E", (-1.7, 1.4, 8.6), (0.25, 0.2, 2.2), glow_o, root)
    box("VeiaPeito_D", (1.7, 1.4, 8.6), (0.25, 0.2, 2.2), glow_o, root)

    # Ombros
    box("Ombro_E", (-3.3, 0.1, 10.6), (2.2, 2.6, 2.2), bright, root)
    box("Ombro_D", (3.3, 0.1, 10.6), (2.2, 2.6, 2.2), bright, root)
    box("OmbroGlow_E", (-3.3, 1.35, 10.6), (1.5, 0.2, 1.2), glow_o, root)
    box("OmbroGlow_D", (3.3, 1.35, 10.6), (1.5, 0.2, 1.2), glow_o, root)

    # Braços
    box("Braco_E", (-3.5, 0.15, 8.0), (1.5, 1.8, 3.2), red, root)
    box("Braco_D", (3.5, 0.15, 8.0), (1.5, 1.8, 3.2), red, root)
    box("Luva_E", (-3.55, 0.3, 6.1), (1.7, 2.0, 1.5), dark, root)
    box("Luva_D", (3.55, 0.3, 6.1), (1.7, 2.0, 1.5), dark, root)

    # Capacete
    box("Cabeca", (0.0, 0.1, 12.8), (3.6, 3.2, 3.2), red, root)
    box("ElmoTopo", (0.0, 0.1, 14.5), (3.2, 2.8, 1.0), bright, root)
    box("Viseira", (0.0, 1.7, 12.85), (2.2, 0.25, 0.55), glow_y, root)
    box("Pluma", (0.0, 0.0, 15.5), (0.7, 0.7, 1.2), plume, root)
    box("Chifre_E", (-2.15, 0.0, 15.3), (0.7, 0.7, 2.4), dark, root)
    box("Chifre_D", (2.15, 0.0, 15.3), (0.7, 0.7, 2.4), dark, root)
    box("PontaChifre_E", (-2.55, 0.0, 16.6), (0.55, 0.55, 0.9), red, root)
    box("PontaChifre_D", (2.55, 0.0, 16.6), (0.55, 0.55, 0.9), red, root)

    # Espada — mão direita do personagem (esquerda da câmera)
    box("Punho", (-3.7, 1.3, 6.5), (0.55, 0.55, 1.6), dark, root)
    box("Guarda", (-3.7, 1.3, 7.4), (1.6, 0.45, 0.4), bright, root)
    box("Gema", (-3.7, 1.3, 7.4), (0.45, 0.5, 0.45), glow_y, root)
    box("Lamina", (-3.7, 1.3, 10.6), (0.7, 0.28, 6.0), glow_o, root)
    box("Nucleo", (-3.7, 1.48, 10.6), (0.28, 0.12, 5.2), glow_y, root)
    box("PontaLamina", (-3.7, 1.3, 13.85), (0.45, 0.22, 0.7), glow_y, root)

    # Escudo — mão esquerda
    box("Escudo", (4.5, 0.9, 7.6), (0.45, 3.0, 4.6), red, root)
    box("BordaEscudo", (4.7, 0.9, 7.6), (0.2, 3.2, 4.8), glow_o, root)
    box("V_Escudo_E", (4.8, 0.9, 8.0), (0.18, 0.25, 1.1), glow_y, root)
    box("V_Escudo_D", (4.8, 0.9, 8.0), (0.18, 0.25, 1.1), glow_y, root)
    bpy.data.objects["V_Escudo_E"].rotation_euler = (0.0, math.radians(22), 0.0)
    bpy.data.objects["V_Escudo_D"].rotation_euler = (0.0, math.radians(-22), 0.0)
    bpy.data.objects["Chifre_E"].rotation_euler = (0.0, math.radians(18), 0.0)
    bpy.data.objects["Chifre_D"].rotation_euler = (0.0, math.radians(-18), 0.0)

    if REF.exists():
        img = bpy.data.images.load(str(REF))
        bpy.ops.mesh.primitive_plane_add(size=12, location=(12, 0, 8))
        plane = bpy.context.active_object
        plane.name = "Referencia"
        plane.scale = (8.5, 12, 1)
        bpy.ops.object.transform_apply(scale=True)
        plane.rotation_euler = (math.radians(90), 0, 0)
        plane.hide_render = True
        mat = bpy.data.materials.new("RefMat")
        mat.use_nodes = True
        nodes = mat.node_tree.nodes
        links = mat.node_tree.links
        nodes.clear()
        out = nodes.new("ShaderNodeOutputMaterial")
        emit = nodes.new("ShaderNodeEmission")
        tex = nodes.new("ShaderNodeTexImage")
        tex.image = img
        tex.interpolation = "Closest"
        links.new(tex.outputs["Color"], emit.inputs["Color"])
        links.new(emit.outputs["Emission"], out.inputs["Surface"])
        plane.data.materials.append(mat)

    light = bpy.data.objects.new("KeyLight", bpy.data.lights.new("KeyLight", "AREA"))
    light.data.energy = 900
    light.data.size = 8
    light.location = (6, -8, 14)
    light.rotation_euler = (math.radians(55), 0, math.radians(25))
    bpy.context.collection.objects.link(light)

    fill = bpy.data.objects.new("Fill", bpy.data.lights.new("Fill", "AREA"))
    fill.data.energy = 350
    fill.data.size = 10
    fill.location = (-7, -6, 10)
    bpy.context.collection.objects.link(fill)

    cam_data = bpy.data.cameras.new("Camera")
    cam = bpy.data.objects.new("Camera", cam_data)
    cam.location = (5.5, 20.0, 8.8)
    cam.rotation_euler = (math.radians(90), 0, math.radians(180))
    cam_data.lens = 50
    bpy.context.collection.objects.link(cam)
    bpy.context.scene.camera = cam
    target = bpy.data.objects.new("LookAt", None)
    target.location = (0.2, 1.2, 8.4)
    bpy.context.collection.objects.link(target)
    constraint = cam.constraints.new(type="TRACK_TO")
    constraint.target = target
    constraint.track_axis = "TRACK_NEGATIVE_Z"
    constraint.up_axis = "UP_Y"
    world = bpy.data.worlds.new("Void")
    world.use_nodes = True
    bpy.context.scene.world = world
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.02, 0.015, 0.03, 1)
        bg.inputs[1].default_value = 0.15

    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    bpy.context.view_layer.objects.active = root

    ROOT.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))
    print("SAVED", BLEND)
    try:
        bpy.ops.export_scene.gltf(filepath=str(GLB), export_format="GLB", export_apply=True)
        print("EXPORTED", GLB)
    except Exception as exc:
        print("GLB_SKIP", exc)


build()
