"""Export the complete native hammer cursor setup pose without running Unity."""
from pathlib import Path
import hashlib
import json
import subprocess
import sys
import tempfile
import UnityPy

ROOT = Path(__file__).resolve().parents[1]
GAME = Path('/mnt/FastGaming1/SteamLibrary/steamapps/common/PVZ Replanted')
BUNDLES = GAME / 'Replanted_Data/StreamingAssets/aa/StandaloneWindows64'
BUNDLE = BUNDLES / 'spineassets_assets_assets/art/characters/spine.bundle'
TOOLS = ROOT.parent / 'fusion-mod/tools'
sys.path.insert(0, str(TOOLS))
from render_zombie_pose import render

output = ROOT / 'assets'
output.mkdir(exist_ok=True)
environment = UnityPy.load(str(BUNDLE))
objects = {obj.path_id: obj for obj in environment.objects}
# CursorController's native Hammer entry references this exact skeleton asset.
asset = objects[6400684083306646311].read_typetree()
assert asset['m_Name'] == 'Hammer_SkeletonData'
atlas = objects[asset['atlasAssets'][0]['m_PathID']].read_typetree()
metadata = dict(bundle=str(BUNDLE), skeleton_data=6400684083306646311,
                skeleton=asset['skeletonJSON']['m_PathID'], atlas=atlas['atlasFile']['m_PathID'],
                texture=3979291817390956949, pose='setup',
                sha256=hashlib.sha256(BUNDLE.read_bytes()).hexdigest())
with tempfile.TemporaryDirectory(prefix='replanted-hammer-') as temporary:
    folder = Path(temporary)
    for name, path_id in [('Hammer.skel', metadata['skeleton']), ('Hammer.atlas', metadata['atlas'])]:
        data = objects[path_id].read().m_Script
        (folder / name).write_bytes(data.encode('utf-8', 'surrogateescape') if isinstance(data, str) else bytes(data))
    objects[metadata['texture']].read().image.save(folder / 'Hammer.png')
    pose_path = folder / 'pose.json'
    subprocess.run(['node', str(TOOLS / 'decode_zombie_pose.mjs'), str(folder),
                    'Hammer', str(pose_path), '-', '-'], check=True)
    pose = json.loads(pose_path.read_text())
    # Render native attachment transforms and draw order, not an isolated atlas part.
    render(pose['attachments'], folder).save(output / 'hammer.png')
    metadata['attachments'] = [part['name'] for part in pose['attachments'] if 'locator' not in part['name'].lower()]
(output / 'hammer-source.json').write_text(json.dumps(metadata, indent=2) + '\n')
# Reuse the native, offline-exported portraits already used by Fusion debug mode.
for name in ('zombie-packets.png', 'zombie-packets.json'):
    source = ROOT.parent / 'fusion-mod/assets/debug-cards' / name
    (output / name).write_bytes(source.read_bytes())
