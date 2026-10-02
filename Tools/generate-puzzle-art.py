#!/usr/bin/env python3
"""Generates the puzzle art (card icons, sliding tile faces, card back) as PNG sprites.

Simple and self-contained: one script, one dependency (Pillow), no project settings touched.
Output goes to Assets/_Project/Art/Puzzle/ together with hand-written .meta files so Unity imports
every PNG as a Sprite (2D and UI) on first import. The Editor builder
(Tools/3D Puzzle Explorer/Build Everything) re-applies the sprite import settings anyway, so the
art is correct even if you regenerate it later.

Run:  python Tools/generate-puzzle-art.py
"""

from __future__ import annotations

import hashlib
import math
import os

from PIL import Image, ImageDraw, ImageFont

# ---------------------------------------------------------------- configuration

SIZE = 256          # final sprite size (px)
SS = 4              # supersampling factor -> drawn at SIZE*SS then downscaled (anti-aliasing)
ICON_COUNT = 8      # Memory Match pairs (4x4 board)
TILE_COUNT = 8      # Sliding Puzzle tiles (3x3 board -> values 1..8)

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_ROOT = os.path.join(PROJECT, "Assets", "_Project", "Art", "Puzzle")
ICON_DIR = os.path.join(OUT_ROOT, "Icons")
TILE_DIR = os.path.join(OUT_ROOT, "Tiles")
BACK_DIR = os.path.join(OUT_ROOT, "Cards")

# Bright hues, readable on the dark card background (#293040-ish).
HUES = [
    (255, 107, 107),   # 1 red
    (255, 169, 77),    # 2 orange
    (255, 212, 59),    # 3 yellow
    ( 81, 207, 102),   # 4 green
    ( 56, 217, 169),   # 5 teal
    ( 77, 171, 247),   # 6 blue
    (151, 117, 250),   # 7 purple
    (247, 131, 172),   # 8 pink
]

OUTLINE = (12, 15, 22)      # dark outline colour: keeps shapes legible on any background
CARD_BACK_BG = (26, 31, 44)
CARD_BACK_LINE = (58, 68, 92)

_FONT_CANDIDATES = [
    r"C:\Windows\Fonts\arialbd.ttf",
    r"C:\Windows\Fonts\segoeuib.ttf",
    r"C:\Windows\Fonts\arial.ttf",
    "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
]


# ---------------------------------------------------------------- drawing helpers

def new_layer() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    """A transparent supersampled canvas."""
    image = Image.new("RGBA", (SIZE * SS, SIZE * SS), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def s(value: float) -> float:
    """Scales a coordinate from the 256px design space to the supersampled canvas."""
    return value * SS


def finish(image: Image.Image) -> Image.Image:
    """Downscales the supersampled canvas back to the sprite size."""
    return image.resize((SIZE, SIZE), Image.LANCZOS)


def load_font(size: int) -> ImageFont.FreeTypeFont:
    for path in _FONT_CANDIDATES:
        if os.path.isfile(path):
            return ImageFont.truetype(path, size)

    return ImageFont.load_default(size)


def polygon_points(count: int, radius: float, start_angle: float,
                   inner_ratio: float | None = None) -> list[tuple[float, float]]:
    """Vertices of a regular polygon (star when inner_ratio is given), centred on the canvas."""
    cx = cy = SIZE / 2
    points: list[tuple[float, float]] = []
    steps = count * 2 if inner_ratio is not None else count

    for index in range(steps):
        r = radius if (inner_ratio is None or index % 2 == 0) else radius * inner_ratio
        angle = math.radians(start_angle + index * 360.0 / steps)
        points.append((s(cx + math.cos(angle) * r), s(cy - math.sin(angle) * r)))

    return points


def heart_points() -> list[tuple[float, float]]:
    """A heart as a polygon sampled from the classic implicit curve."""
    points: list[tuple[float, float]] = []
    cx = cy = SIZE / 2
    scale = SIZE * 0.32

    for step in range(180):
        t = math.pi * 2 * step / 180
        x = 16 * math.sin(t) ** 3
        y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        points.append((s(cx + x * scale / 16), s(cy - y * scale / 16)))

    return points


# Each icon is drawn twice: once scaled up in the outline colour, then the shape itself, which
# gives a clean 6px outline that keeps the glyph readable over the card background.
def draw_icon(shape: str, colour: tuple[int, int, int]) -> Image.Image:
    layer, draw = new_layer()
    grow = 8.0  # outline thickness in design pixels

    if shape == "circle":
        draw.ellipse([s(grow), s(grow), s(SIZE - grow), s(SIZE - grow)], fill=OUTLINE + (255,))
        draw.ellipse([s(24), s(24), s(SIZE - 24), s(SIZE - 24)], fill=colour + (255,))

    elif shape == "square":
        draw.rounded_rectangle([s(6), s(6), s(SIZE - 6), s(SIZE - 6)], radius=s(40), fill=OUTLINE + (255,))
        draw.rounded_rectangle([s(20), s(20), s(SIZE - 20), s(SIZE - 20)], radius=s(28), fill=colour + (255,))

    elif shape == "diamond":
        outer = [(s(SIZE / 2), s(grow)), (s(SIZE - grow), s(SIZE / 2)),
                 (s(SIZE / 2), s(SIZE - grow)), (s(grow), s(SIZE / 2))]
        inner = [(s(SIZE / 2), s(28)), (s(SIZE - 28), s(SIZE / 2)),
                 (s(SIZE / 2), s(SIZE - 28)), (s(28), s(SIZE / 2))]
        draw.polygon(outer, fill=OUTLINE + (255,))
        draw.polygon(inner, fill=colour + (255,))

    elif shape == "triangle":
        draw.polygon(polygon_points(3, SIZE * 0.5, 90), fill=OUTLINE + (255,))
        draw.polygon(polygon_points(3, SIZE * 0.5 - 22, 90), fill=colour + (255,))

    elif shape == "hexagon":
        draw.polygon(polygon_points(6, SIZE * 0.5, 0), fill=OUTLINE + (255,))
        draw.polygon(polygon_points(6, SIZE * 0.5 - 20, 0), fill=colour + (255,))

    elif shape == "star":
        draw.polygon(polygon_points(5, SIZE * 0.5, 90, inner_ratio=0.42), fill=OUTLINE + (255,))
        draw.polygon(polygon_points(5, SIZE * 0.5 - 20, 90, inner_ratio=0.44), fill=colour + (255,))

    elif shape == "ring":
        draw.ellipse([s(grow), s(grow), s(SIZE - grow), s(SIZE - grow)], fill=OUTLINE + (255,))
        draw.ellipse([s(24), s(24), s(SIZE - 24), s(SIZE - 24)], fill=colour + (255,))
        draw.ellipse([s(84), s(84), s(SIZE - 84), s(SIZE - 84)], fill=(0, 0, 0, 0))

    elif shape == "plus":
        arm, thick = SIZE * 0.34, SIZE * 0.20
        for grow_size, fill in ((16.0, OUTLINE + (255,)), (32.0, colour + (255,))):
            cx = cy = SIZE / 2
            draw.rectangle([s(cx - arm + 8), s(cy - thick / 2 - grow_size / 4),
                            s(cx + arm - 8), s(cy + thick / 2 + grow_size / 4)], fill=fill)
            draw.rectangle([s(cx - thick / 2 - grow_size / 4), s(cy - arm + 8),
                            s(cx + thick / 2 + grow_size / 4), s(cy + arm - 8)], fill=fill)
            arm -= 12
            thick -= 8

    elif shape == "heart":
        draw.polygon(heart_points(), fill=OUTLINE + (255,))
        inner = [(x, y) for x, y in heart_points()]
        cx = cy = s(SIZE / 2)
        inner = [(cx + (x - cx) * 0.80, cy + (y - cy) * 0.80) for x, y in inner]
        draw.polygon(inner, fill=colour + (255,))

    else:
        raise ValueError(f"unknown shape: {shape}")

    return finish(layer)


def draw_tile(value: int, colour: tuple[int, int, int]) -> Image.Image:
    """A sliding tile: rounded square in the hue plus a big white number."""
    layer, draw = new_layer()

    draw.rounded_rectangle([s(4), s(4), s(SIZE - 4), s(SIZE - 4)], radius=s(26), fill=OUTLINE + (255,))
    draw.rounded_rectangle([s(10), s(10), s(SIZE - 10), s(SIZE - 10)], radius=s(22), fill=colour + (255,))

    text = str(value)
    font = load_font(int(s(150)))

    # Two passes: dark copy offset down-right for a soft shadow, then white on top.
    bounding = draw.textbbox((0, 0), text, font=font)
    width, height = bounding[2] - bounding[0], bounding[3] - bounding[1]
    position = (s(SIZE / 2) - width / 2 - bounding[0], s(SIZE / 2) - height / 2 - bounding[1])

    draw.text((position[0] + s(4), position[1] + s(5)), text, font=font, fill=(0, 0, 0, 120))
    draw.text(position, text, font=font, fill=(255, 255, 255, 255))

    return finish(layer)


def draw_card_back() -> Image.Image:
    """A quiet diagonal pattern for the face-down side of a card."""
    layer, draw = new_layer()

    draw.rounded_rectangle([s(4), s(4), s(SIZE - 4), s(SIZE - 4)], radius=s(24), fill=CARD_BACK_BG + (255,))
    draw.rounded_rectangle([s(12), s(12), s(SIZE - 12), s(SIZE - 12)], radius=s(18), outline=CARD_BACK_LINE + (255,),
                           width=int(s(4)))

    step = 40
    for offset in range(-SIZE, SIZE * 2, step):
        draw.line([(s(offset), s(0)), (s(offset + SIZE), s(SIZE))], fill=CARD_BACK_LINE + (90,), width=int(s(5)))

    # A simple centred diamond so the back reads as "the hidden side".
    cx = cy = SIZE / 2
    draw.polygon([(s(cx), s(cy - 54)), (s(cx + 46), s(cy)), (s(cx), s(cy + 54)), (s(cx - 46), s(cy))],
                 fill=(0, 0, 0, 0), outline=CARD_BACK_LINE + (220,))
    draw.polygon([(s(cx), s(cy - 46)), (s(cx + 39), s(cy)), (s(cx), s(cy + 46)), (s(cx - 39), s(cy))],
                 fill=(0, 0, 0, 0), outline=(120, 134, 168, 235))

    return finish(layer)


# ---------------------------------------------------------------- Unity .meta files

META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 512
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings: []
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  spritePackingTag: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def write_meta(path: str, asset_path_for_guid: str) -> None:
    """Writes a sprite .meta next to the PNG, with a GUID derived from the asset path.

    Deriving the GUID keeps the file stable across regenerations, so references stored in
    scenes/assets never break when you re-run this script.
    """
    guid = hashlib.md5(asset_path_for_guid.replace("\\", "/").encode("utf-8")).hexdigest()

    with open(path + ".meta", "w", encoding="utf-8", newline="\n") as handle:
        handle.write(META_TEMPLATE.format(guid=guid))


# ---------------------------------------------------------------- output

def save(image: Image.Image, directory: str, filename: str) -> str:
    os.makedirs(directory, exist_ok=True)
    path = os.path.join(directory, filename)
    image.save(path, "PNG")

    relative = os.path.relpath(path, PROJECT)
    write_meta(path, relative)

    return relative


def build_preview(images: list[tuple[str, Image.Image]]) -> Image.Image:
    """A 4x4 contact sheet, handy for reviewing the art in one look."""
    cell, pad = 128, 12
    columns = 4
    rows = math.ceil(len(images) / columns)

    sheet = Image.new("RGBA", (columns * cell + (columns + 1) * pad, rows * cell + (rows + 1) * pad),
                      (18, 21, 30, 255))

    for index, (_label, image) in enumerate(images):
        column = index % columns
        row = index // columns
        thumb = image.resize((cell, cell), Image.LANCZOS)
        sheet.alpha_composite(thumb, (pad + column * (cell + pad), pad + row * (cell + pad)))

    return sheet


def main() -> int:
    shapes = ["circle", "square", "triangle", "diamond", "star", "hexagon", "ring", "heart"]
    written: list[str] = []
    sheet_items: list[tuple[str, Image.Image]] = []

    for index in range(ICON_COUNT):
        image = draw_icon(shapes[index % len(shapes)], HUES[index % len(HUES)])
        written.append(save(image, ICON_DIR, f"icon_{index + 1:02d}.png"))
        sheet_items.append((f"icon_{index + 1:02d}", image))

    for index in range(TILE_COUNT):
        image = draw_tile(index + 1, HUES[index % len(HUES)])
        written.append(save(image, TILE_DIR, f"tile_{index + 1:02d}.png"))
        sheet_items.append((f"tile_{index + 1:02d}", image))

    back = draw_card_back()
    written.append(save(back, BACK_DIR, "card_back.png"))

    preview = build_preview(sheet_items)
    # Deliberately outside Assets/: Unity imports everything under Assets/, and a contact sheet has
    # no business being a sprite in the project.
    preview_path = os.path.join(PROJECT, "Tools", "art-preview.png")
    preview.save(preview_path, "PNG")

    print(f"wrote {len(written)} sprites into {os.path.relpath(OUT_ROOT, PROJECT)}")
    for relative in written:
        print("  " + relative)

    print(f"  {os.path.relpath(preview_path, PROJECT)}   (contact sheet for review, lives outside Assets/)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
