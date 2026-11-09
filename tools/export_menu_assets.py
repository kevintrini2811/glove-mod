"""Compose native glove and bright lawn for BloomEngine without running Unity."""
import hashlib
import json
from pathlib import Path
import UnityPy
from PIL import Image, ImageOps

ROOT = Path(__file__).resolve().parents[1]
GAME = Path('/mnt/FastGaming1/SteamLibrary/steamapps/common/PVZ Replanted')
BUNDLE = GAME / 'Replanted_Data/StreamingAssets/aa/StandaloneWindows64/uiatlasshared_assets_all.bundle'
GLOVE_ID = -2474307231636651119
LAWN = ROOT.parent / 'plant-library/assets/lawn.png'


def export():
    environment = UnityPy.load(str(BUNDLE))
    glove = next(obj.read() for obj in environment.objects if obj.type.name == 'Sprite' and obj.path_id == GLOVE_ID)
    if glove.m_Name != 'Zen_GardenGlove':
        raise ValueError('Unexpected native glove sprite')
    foreground = glove.image.convert('RGBA')
    bounds = foreground.getbbox()
    if bounds is None:
        raise ValueError('Native glove sprite is empty')
    foreground = ImageOps.contain(foreground.crop(bounds), (440, 440), method=Image.Resampling.LANCZOS)
    with Image.open(LAWN) as source:
        icon = ImageOps.fit(source.convert('RGBA'), (512, 512), method=Image.Resampling.LANCZOS)
    icon.putalpha(255)
    icon.alpha_composite(foreground, ((512 - foreground.width) // 2, 512 - foreground.height - 28))
    output = ROOT / 'assets/menu-icon.png'
    icon.save(output)
    proof = dict(bundle=str(BUNDLE), bundle_sha256=hashlib.sha256(BUNDLE.read_bytes()).hexdigest(),
                 sprite=glove.m_Name, path_id=GLOVE_ID, lawn_source=str(LAWN),
                 lawn_sha256=hashlib.sha256(LAWN.read_bytes()).hexdigest(),
                 icon_size=[512, 512], foreground_bounds=[440, 440], foreground_bottom=28,
                 sha256=hashlib.sha256(output.read_bytes()).hexdigest(), game_started=False)
    (ROOT / 'assets/menu-icon-source.json').write_text(json.dumps(proof, indent=2) + '\n')


if __name__ == '__main__':
    export()
