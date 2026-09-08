import math
from pathlib import Path

import bpy

ROOT = Path("/Users/gabriellucas/Desktop/trabalho jogo sexta/Assets/Art")
BLEND = ROOT / "Anjo8bit.blend"
GLB = ROOT / "Anjo8bit.glb"
REF = ROOT / "anjo_ref.png"
PREVIEW = ROOT / "anjo_preview.png"


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


def set_material_preview():
    for window in bpy.context.window_manager.windows:
        for area in window.screen.areas:
            if area.type != "VIEW_3D":
                continue
            for space in area.spaces:
                if space.type != "VIEW_3D":
                    continue
                space.shading.type = "MATERIAL"
                space.shading.use_scene_lights = True
                space.shading.use_scene_world = True


def build():
    reset_scene()

    white = material("PanoBranco", (0.9, 0.9, 0.94))
    silver = material("Prata", (0.7, 0.74, 0.8))
    steel = material("Aco", (0.4, 0.44, 0.5))
    gold = material("Ouro", (0.86, 0.66, 0.2))
    gold_g = material("OuroGlow", (1.0, 0.84, 0.28), emission=4.2)
    cape_b = material("CapaSombra", (0.58, 0.68, 0.8))
    skin = material("Pele", (0.84, 0.66, 0.54))
    dark = material("Sombra", (0.18, 0.17, 0.2))
    cyan = material("Ciano", (0.42, 0.82, 0.95), emission=2.2)
    feather = material("Pena", (0.97, 0.97, 1.0))

    root = bpy.data.objects.new("Anjo8bit", None)
    bpy.context.collection.objects.link(root)

    box("Bota_E", (-1.25, 0.15, 0.7), (1.6, 2.2, 1.4), steel, root)
    box("Bota_D", (1.25, 0.15, 0.7), (1.6, 2.2, 1.4), steel, root)
    box("OuroPe_E", (-1.25, 0.95, 0.85), (1.1, 0.22, 0.55), gold, root)
    box("OuroPe_D", (1.25, 0.95, 0.85), (1.1, 0.22, 0.55), gold, root)

    box("Perna_E", (-1.2, 0.05, 3.25), (1.55, 1.9, 3.6), silver, root)
    box("Perna_D", (1.2, 0.05, 3.25), (1.55, 1.9, 3.6), silver, root)
    box("Joelho_E", (-1.2, 0.25, 4.55), (1.75, 2.1, 1.15), gold, root)
    box("Joelho_D", (1.2, 0.25, 4.55), (1.75, 2.1, 1.15), gold, root)

    box("Tunica", (0.0, 0.08, 6.15), (4.4, 2.35, 2.0), white, root)
    box("Cintura", (0.0, 0.1, 5.35), (4.6, 2.45, 0.7), gold, root)
    box("Torso", (0.0, 0.12, 8.35), (4.5, 2.55, 3.2), silver, root)
    box("PeitoOuro", (0.0, 1.38, 8.55), (2.4, 0.22, 2.0), gold, root)
    box("V_Peito", (0.0, 1.42, 7.7), (1.3, 0.2, 0.45), gold_g, root)

    box("Ombro_E", (-3.15, 0.15, 10.45), (2.15, 2.4, 2.0), silver, root)
    box("Ombro_D", (3.15, 0.15, 10.45), (2.15, 2.4, 2.0), silver, root)
    box("OmbroOuro_E", (-3.15, 1.3, 10.45), (1.5, 0.2, 1.1), gold, root)
    box("OmbroOuro_D", (3.15, 1.3, 10.45), (1.5, 0.2, 1.1), gold, root)

    box("Braco_E", (-3.25, 0.2, 8.0), (1.45, 1.75, 2.9), silver, root)
    box("Braco_D", (3.25, 0.2, 8.0), (1.45, 1.75, 2.9), silver, root)
    box("Luva_E", (-3.3, 0.35, 6.2), (1.6, 1.9, 1.35), steel, root)
    box("Luva_D", (3.3, 0.35, 6.2), (1.6, 1.9, 1.35), steel, root)
    box("LuvaOuro_E", (-3.3, 1.2, 6.35), (1.1, 0.18, 0.55), gold, root)
    box("LuvaOuro_D", (3.3, 1.2, 6.35), (1.1, 0.18, 0.55), gold, root)

    box("Capa", (0.0, -1.55, 8.4), (5.2, 0.7, 6.4), white, root)
    box("CapaForro", (0.0, -1.15, 7.6), (4.4, 0.25, 4.6), cape_b, root)
    box("AsaCapa_E", (-2.6, -1.85, 10.6), (1.8, 0.45, 2.2), white, root)
    box("AsaCapa_D", (2.6, -1.85, 10.6), (1.8, 0.45, 2.2), white, root)

    box("Aljava", (1.35, -1.35, 9.7), (1.1, 0.85, 2.8), steel, root)
    box("AljavaOuro", (1.35, -0.9, 9.7), (0.9, 0.18, 2.4), gold, root)
    box("PenaAljavaA", (1.15, -1.55, 11.35), (0.22, 0.22, 0.7), cyan, root)
    box("PenaAljavaB", (1.45, -1.7, 11.45), (0.22, 0.22, 0.75), cyan, root)
    box("PenaAljavaC", (1.7, -1.5, 11.3), (0.2, 0.2, 0.6), cyan, root)

    box("Pescoco", (0.0, 0.2, 11.35), (1.6, 1.7, 0.7), skin, root)
    box("Cabeca", (0.0, 0.25, 12.85), (2.6, 2.7, 2.5), skin, root)
    box("Capuz", (0.0, 0.05, 13.15), (3.3, 3.15, 2.9), white, root)
    box("CapuzAba", (0.0, 1.55, 12.35), (2.8, 0.45, 0.7), white, root)
    box("Rosto", (0.0, 1.55, 12.7), (1.7, 0.28, 1.15), skin, root)
    box("Coroa", (0.0, 1.2, 13.85), (2.3, 2.2, 0.28), gold, root)
    box("AsaCapuz_E", (-2.15, 0.15, 14.35), (1.3, 0.7, 0.85), feather, root)
    box("AsaCapuz_D", (2.15, 0.15, 14.35), (1.3, 0.7, 0.85), feather, root)

    grip = box("ArcoPunho", (-3.45, 1.25, 8.05), (0.45, 0.55, 1.3), gold, root)
    upper = box("ArcoAlto", (-4.15, 1.25, 10.9), (0.4, 0.4, 3.4), silver, root)
    top = box("ArcoTopo", (-4.85, 1.25, 13.35), (0.55, 0.4, 1.7), gold, root)
    lower = box("ArcoBaixo", (-4.15, 1.25, 5.15), (0.4, 0.4, 3.4), silver, root)
    bot = box("ArcoPonta", (-4.85, 1.25, 2.75), (0.55, 0.4, 1.7), gold, root)
    upper.rotation_euler = (0.0, math.radians(14), 0.0)
    top.rotation_euler = (0.0, math.radians(28), 0.0)
    lower.rotation_euler = (0.0, math.radians(-14), 0.0)
    bot.rotation_euler = (0.0, math.radians(-28), 0.0)
    box("Corda", (-3.15, 1.25, 8.05), (0.08, 0.08, 10.4), cyan, root)
    box("Flecha", (-2.35, 1.25, 8.05), (2.8, 0.18, 0.18), gold_g, root)
    box("PontaFlecha", (-0.75, 1.25, 8.05), (0.55, 0.28, 0.28), gold_g, root)
    box("PenaFlecha", (-3.55, 1.25, 8.05), (0.35, 0.35, 0.35), cyan, root)

    box("PenaSoltaA", (-2.2, 2.4, 14.2), (0.35, 0.18, 0.7), feather, root)
    box("PenaSoltaB", (2.6, 2.1, 12.6), (0.3, 0.16, 0.6), feather, root)
    box("PenaSoltaC", (-4.6, 2.0, 9.4), (0.28, 0.14, 0.55), feather, root)
    box("PenaSoltaD", (3.8, 1.8, 7.2), (0.26, 0.14, 0.5), feather, root)
    bpy.data.objects["PenaSoltaA"].rotation_euler = (0.0, 0.0, math.radians(22))
    bpy.data.objects["PenaSoltaB"].rotation_euler = (0.0, 0.0, math.radians(-18))
    bpy.data.objects["PenaSoltaC"].rotation_euler = (0.0, math.radians(15), math.radians(10))
    bpy.data.objects["AsaCapuz_E"].rotation_euler = (0.0, math.radians(18), math.radians(12))
    bpy.data.objects["AsaCapuz_D"].rotation_euler = (0.0, math.radians(-18), math.radians(-12))
    bpy.data.objects["AsaCapa_E"].rotation_euler = (math.radians(12), math.radians(16), 0.0)
    bpy.data.objects["AsaCapa_D"].rotation_euler = (math.radians(12), math.radians(-16), 0.0)

    if REF.exists():
        img = bpy.data.images.load(str(REF))
        bpy.ops.mesh.primitive_plane_add(size=12, location=(12.8, 0, 8.2))
        plane = bpy.context.active_object
        plane.name = "Referencia"
        plane.scale = (7.4, 12.6, 1)
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
    fill.data.energy = 380
    fill.data.size = 10
    fill.location = (-7, -6, 10)
    bpy.context.collection.objects.link(fill)

    rim = bpy.data.objects.new("Rim", bpy.data.lights.new("Rim", "AREA"))
    rim.data.energy = 450
    rim.data.size = 6
    rim.data.color = (1.0, 0.88, 0.55)
    rim.location = (-3, 6, 13)
    bpy.context.collection.objects.link(rim)

    cam_data = bpy.data.cameras.new("Camera")
    cam = bpy.data.objects.new("Camera", cam_data)
    cam.location = (5.2, 20.5, 8.6)
    cam.rotation_euler = (math.radians(90), 0, math.radians(180))
    cam_data.lens = 50
    bpy.context.collection.objects.link(cam)
    bpy.context.scene.camera = cam
    target = bpy.data.objects.new("LookAt", None)
    target.location = (0.0, 1.1, 8.2)
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
        bg.inputs[0].default_value = (0.02, 0.02, 0.03, 1)
        bg.inputs[1].default_value = 0.14

    scene = bpy.context.scene
    scene.render.resolution_x = 720
    scene.render.resolution_y = 900
    scene.render.filepath = str(PREVIEW)
    scene.render.film_transparent = False
    try:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    except Exception:
        scene.render.engine = "BLENDER_EEVEE"

    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    bpy.context.view_layer.objects.active = root

    set_material_preview()
    ROOT.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))
    print("SAVED", BLEND)

    try:
        bpy.ops.render.render(write_still=True)
        print("PREVIEW", PREVIEW)
    except Exception as exc:
        print("PREVIEW_SKIP", exc)

    set_material_preview()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))

    try:
        bpy.ops.object.select_all(action="DESELECT")
        root.select_set(True)
        for child in root.children:
            child.select_set(True)
        bpy.context.view_layer.objects.active = root
        bpy.ops.export_scene.gltf(
            filepath=str(GLB),
            export_format="GLB",
            export_apply=True,
            use_selection=True,
        )
        print("EXPORTED", GLB)
    except Exception as exc:
        print("GLB_SKIP", exc)


build()
