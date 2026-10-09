"""
Болванки в Blender: низкополигональные модели по договорённостям игры и экспорт в glb.

Запуск:
  • через Blender MCP — попросите Claude «выполни tools/blender/make_placeholders.py в Blender»
    (инструмент execute_blender_code получит содержимое файла);
  • без интерфейса: blender -b -P tools/blender/make_placeholders.py -- --out tools/blender/out --only hero

Договорённости (см. навык asset-pipeline-3d):
  • 1 единица = 1 метр, начало координат у ног, персонаж смотрит по −Y Blender (вид спереди, = +Z Unity после экспорта glTF);
  • корневой объект называется как id ассета; дочерние узлы Body, BrushPivot, BrushTip — как у болванок из кода;
  • плоские цвета через материалы Principled BSDF, без текстур — стиль и вес остаются под контролем.

Готовые glb кладите в Assets/_Project/Resources/Models/<id>.glb — игра подхватит их вместо болванок кода.
"""
import math
import os
import sys

import bpy

PALETTE = {
    "coat": (0.29, 0.18, 0.51), "skirt": (0.23, 0.13, 0.4), "apron": (0.95, 0.9, 0.82),
    "skin": (0.96, 0.79, 0.66), "hair": (1.0, 0.48, 0.23), "beret": (0.91, 0.16, 0.23),
    "wood": (0.54, 0.35, 0.16), "metal": (0.75, 0.75, 0.78), "paint": (1.0, 0.23, 0.29),
    "grey": (0.56, 0.54, 0.59), "greyLight": (0.65, 0.64, 0.68), "eye": (1.0, 0.48, 0.23), "dark": (0.1, 0.08, 0.13),
}


def srgb_to_linear(c):
    return tuple(((x + 0.055) / 1.055) ** 2.4 if x > 0.04045 else x / 12.92 for x in c)


def material(name):
    mat = bpy.data.materials.get("AC_" + name)
    if mat is None:
        mat = bpy.data.materials.new("AC_" + name)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value = (*srgb_to_linear(PALETTE[name]), 1.0)
        bsdf.inputs["Roughness"].default_value = 0.9
    return mat


def front(loc):
    """Координаты в функциях-строителях записаны как в Unity (вперёд = +Y); отражаем в «вперёд = −Y» Blender."""
    return (loc[0], -loc[1], loc[2])


def empty(name, parent=None, loc=(0, 0, 0)):
    obj = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    obj.location = front(loc)
    return obj


def part(kind, parent, loc, scale, mat, rot=(0, 0, 0), name=None, segments=12):
    if kind == "sphere":
        bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=max(6, segments // 2), radius=0.5)
    elif kind == "cylinder":
        bpy.ops.mesh.primitive_cylinder_add(vertices=segments, radius=0.5, depth=1.0)
    else:
        bpy.ops.mesh.primitive_cube_add(size=1.0)
    obj = bpy.context.active_object
    if name:
        obj.name = name
    obj.parent = parent
    obj.location = front(loc)
    obj.scale = scale
    # Отражение по Y меняет знак поворотов вокруг X и Z.
    obj.rotation_euler = (math.radians(-rot[0]), math.radians(rot[1]), math.radians(-rot[2]))
    obj.data.materials.append(material(mat))
    bpy.ops.object.shade_flat()
    return obj


# Координаты: Blender Z — вверх, -Y — к камере; glTF-экспорт сам переводит в Y-up.
def hero():
    root = empty("hero")
    body = empty("Body", root)
    part("sphere", body, (0, 0, 0.85), (0.62, 0.5, 1.24), "coat")
    part("cylinder", body, (0, 0, 0.45), (0.8, 0.7, 0.6), "skirt")
    part("cube", body, (0, 0.22, 0.95), (0.42, 0.06, 0.5), "apron")
    part("sphere", body, (0, 0, 1.52), (0.46, 0.46, 0.46), "skin", segments=16)
    part("sphere", body, (0, -0.1, 1.58), (0.52, 0.5, 0.5), "hair")
    part("sphere", body, (0.16, -0.26, 1.38), (0.22, 0.2, 0.32), "hair")
    part("cylinder", body, (0.05, 0, 1.78), (0.56, 0.56, 0.1), "beret", rot=(0, 12, 0), segments=16)
    pivot = empty("BrushPivot", body, (0.38, 0.1, 1.0))
    part("cylinder", pivot, (0, 0.45, 0.15), (0.08, 0.08, 1.2), "wood", rot=(70, 0, 0))
    part("sphere", pivot, (0, 1.22, 0.42), (0.2, 0.36, 0.2), "paint", rot=(70, 0, 0), name="BrushTip")
    return root


def husk():
    root = empty("enemy_husk")
    body = empty("Body", root)
    part("sphere", body, (0, 0.05, 0.85), (0.6, 0.5, 1.5), "grey", rot=(12, 0, 0))
    part("sphere", body, (0, 0.2, 1.55), (0.45, 0.45, 0.42), "greyLight")
    for side in (1, -1):
        part("sphere", body, (0.4 * side, 0.25, 0.8), (0.18, 0.18, 1.0), "grey", rot=(30, 10 * side, 0))
        part("sphere", body, (0.1 * side, 0.4, 1.58), (0.12, 0.12, 0.12), "eye", segments=8)
    return root


BUILDERS = {"hero": hero, "enemy_husk": husk}


def export(root, out_dir):
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for child in root.children_recursive:
        child.select_set(True)
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, root.name + ".glb")
    bpy.ops.export_scene.gltf(filepath=path, export_format="GLB", use_selection=True, export_yup=True)
    print("экспорт:", path)


def main(argv):
    out_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "out")
    only = None
    if "--" in argv:
        args = argv[argv.index("--") + 1:]
        if "--out" in args:
            out_dir = args[args.index("--out") + 1]
        if "--only" in args:
            only = args[args.index("--only") + 1].split(",")
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for asset_id, build in BUILDERS.items():
        if only and asset_id not in only:
            continue
        root = build()
        export(root, out_dir)


main(sys.argv)
