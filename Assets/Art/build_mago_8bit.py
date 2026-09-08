import math
from pathlib import Path

import bpy

ROOT = Path("/Users/gabriellucas/Desktop/trabalho jogo sexta/Assets/Art")
BLEND = ROOT / "Mago8bit.blend"
GLB = ROOT / "Mago8bit.glb"
REF = ROOT / "mago_ref.png"
PREVIEW = ROOT / "mago_preview.png"


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

    void = material("VoidFace", (0.03, 0.015, 0.05))
    robe_d = material("RobeDark", (0.14, 0.05, 0.22))
    robe_m = material("RobeMid", (0.28, 0.1, 0.42))
    trim = material("TrimLilac", (0.68, 0.52, 0.82))
    highlight = material("RobeLight", (0.48, 0.28, 0.64))
    brown = material("Couro", (0.34, 0.16, 0.09))
    metal = material("Haste", (0.58, 0.55, 0.64))
    metal_d = material("HasteDark", (0.26, 0.24, 0.3))
    crystal = material("Cristal", (0.72, 0.28, 1.0), emission=3.6)
    spark = material("Fagulha", (1.0, 0.45, 0.85), emission=5.5)

    root = bpy.data.objects.new("Mago8bit", None)
    bpy.context.collection.objects.link(root)

    box("Pe_E", (-1.15, 0.25, 0.35), (1.5, 2.0, 0.7), void, root)
    box("Pe_D", (1.15, 0.25, 0.35), (1.5, 2.0, 0.7), void, root)

    box("Bainha", (0.0, 0.08, 1.55), (5.9, 2.7, 2.8), robe_d, root)
    box("BainhaTrim", (0.0, 1.4, 0.4), (5.5, 0.22, 0.38), trim, root)
    box("MantoBaixo", (0.0, 0.06, 4.0), (5.2, 2.55, 3.2), robe_m, root)
    box("MantoMeio", (0.0, 0.08, 6.55), (4.6, 2.45, 2.4), robe_m, root)
    box("DobraManto", (0.0, 1.28, 3.7), (2.2, 0.2, 2.4), highlight, root)

    box("Cinto", (0.0, 0.12, 5.55), (4.85, 2.6, 0.42), brown, root)
    box("Fivela", (0.0, 1.42, 5.55), (0.55, 0.18, 0.35), metal, root)
    box("Bolsa", (1.95, 1.35, 5.1), (0.9, 0.55, 0.95), brown, root)
    box("AbaBolsa", (1.95, 1.42, 5.55), (0.95, 0.4, 0.22), brown, root)

    box("Peito", (0.0, 0.1, 8.55), (4.4, 2.5, 2.7), robe_m, root)
    box("Gola", (0.0, 0.18, 10.35), (5.5, 2.95, 1.35), robe_d, root)
    box("GolaTrim", (0.0, 1.62, 10.05), (4.7, 0.2, 0.32), trim, root)
    box("CapaQueda_E", (-2.35, -0.55, 9.4), (1.5, 0.7, 2.6), robe_d, root)
    box("CapaQueda_D", (2.35, -0.55, 9.4), (1.5, 0.7, 2.6), robe_d, root)

    box("Manga_E", (-3.2, 0.22, 8.35), (2.35, 2.35, 3.5), robe_m, root)
    box("Manga_D", (3.2, 0.22, 8.35), (2.35, 2.35, 3.5), robe_m, root)
    box("Punho_E", (-3.25, 0.38, 6.35), (2.05, 2.15, 1.0), trim, root)
    box("Punho_D", (3.25, 0.38, 6.35), (2.05, 2.15, 1.0), trim, root)
    box("Mao_E", (-3.25, 0.45, 5.55), (1.25, 1.35, 0.7), robe_d, root)
    box("Mao_D", (3.25, 0.45, 5.55), (1.25, 1.35, 0.7), robe_d, root)

    box("Capuz", (0.0, 0.18, 12.65), (3.7, 3.35, 3.2), robe_d, root)
    box("CapuzTopo", (0.0, 0.08, 14.5), (2.9, 2.85, 1.15), robe_m, root)
    box("CapuzPonta", (0.0, -0.45, 15.3), (1.45, 1.7, 0.95), robe_d, root)
    box("Vazio", (0.0, 1.88, 12.6), (2.25, 0.32, 1.75), void, root)
    box("CapuzAba", (0.0, 1.55, 11.35), (3.1, 0.55, 0.7), robe_d, root)

    box("Haste", (-3.3, 1.2, 8.1), (0.34, 0.34, 14.4), metal, root)
    box("HasteSombra", (-3.3, 1.05, 8.1), (0.18, 0.18, 13.8), metal_d, root)
    box("AnelHaste", (-3.3, 1.2, 14.55), (0.55, 0.55, 0.35), metal, root)
    garra_e = box("Garra_E", (-3.85, 1.2, 15.55), (0.32, 0.32, 1.45), metal, root)
    garra_d = box("Garra_D", (-2.75, 1.2, 15.55), (0.32, 0.32, 1.45), metal, root)
    garra_f = box("Garra_F", (-3.3, 1.65, 15.65), (0.32, 0.32, 1.25), metal, root)
    garra_e.rotation_euler = (0.0, math.radians(16), 0.0)
    garra_d.rotation_euler = (0.0, math.radians(-16), 0.0)
    garra_f.rotation_euler = (math.radians(-14), 0.0, 0.0)

    box("Cristal", (-3.3, 1.2, 16.25), (1.15, 1.15, 1.45), crystal, root)
    box("CristalNucleo", (-3.3, 1.2, 16.25), (0.55, 0.55, 0.7), spark, root)
    box("FagulhaA", (-4.2, 1.7, 16.7), (0.28, 0.28, 0.28), spark, root)
    box("FagulhaB", (-2.45, 0.65, 16.95), (0.22, 0.22, 0.22), spark, root)
    box("FagulhaC", (-3.7, 1.95, 15.45), (0.2, 0.2, 0.2), crystal, root)
    box("FagulhaD", (-2.7, 1.85, 16.5), (0.18, 0.18, 0.18), spark, root)
    box("FagulhaE", (-3.95, 0.55, 15.85), (0.16, 0.16, 0.16), crystal, root)

    if REF.exists():
        img = bpy.data.images.load(str(REF))
        bpy.ops.mesh.primitive_plane_add(size=12, location=(12.5, 0, 8.2))
        plane = bpy.context.active_object
        plane.name = "Referencia"
        plane.scale = (7.2, 12.4, 1)
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
    light.data.energy = 850
    light.data.size = 8
    light.location = (6, -8, 14)
    light.rotation_euler = (math.radians(55), 0, math.radians(25))
    bpy.context.collection.objects.link(light)

    fill = bpy.data.objects.new("Fill", bpy.data.lights.new("Fill", "AREA"))
    fill.data.energy = 320
    fill.data.size = 10
    fill.location = (-7, -6, 10)
    bpy.context.collection.objects.link(fill)

    rim = bpy.data.objects.new("Rim", bpy.data.lights.new("Rim", "AREA"))
    rim.data.energy = 400
    rim.data.size = 6
    rim.data.color = (0.7, 0.35, 1.0)
    rim.location = (-4, 6, 13)
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
        bg.inputs[0].default_value = (0.015, 0.01, 0.03, 1)
        bg.inputs[1].default_value = 0.12

    scene = bpy.context.scene
    scene.render.resolution_x = 720
    scene.render.resolution_y = 900
    scene.render.filepath = str(PREVIEW)
    scene.render.film_transparent = False
    engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in dir(bpy.types.RenderSettings) or True else "BLENDER_EEVEE"
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
