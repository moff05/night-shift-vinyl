# Blender asset scripts

Run headless, e.g. `/Applications/Blender.app/Contents/MacOS/Blender -b -P a_room.py`.
`lib2.py` models in Unity coordinates and writes FBX into `Assets/Models/Chillhop`; `tex.py` writes the generated
textures into `Assets/Textures/Chillhop`. Edit the `sys.path` line at the top of each script to point at this folder
and `OUT` in `lib2.py` to your project. Then run Chillhop > Build Scene in Unity.
