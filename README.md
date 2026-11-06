# Night Shift Vinyl: Lo-Fi Vinyl Lounge

Stationary Meta Quest hand-tracking VR sim (Unity 6000.5, XRI 3.6, OpenXR, XR Hands).
Open `Assets/Scenes/ChillhopCrate.unity`. Everything is already wired.

Scripts: `Assets/Scripts/Chillhop`. Audio (synthesized): `Assets/Audio/Chillhop`.
Scene builder (rebuilds the scene from code): menu **Chillhop > Build Scene**.
APK: **Chillhop > Build APK** (writes `Builds/nightshiftvinyl.apk`).
Editor-only simulated playthrough: `Assets/Scripts/Chillhop/Dev/SimulatedPlaythrough.cs`.

Build log, walkthrough video and APK download: https://cim423-3d-world.vercel.app/night-shift-vinyl/

## Credits
Room props were modeled in Blender (scripts in `Tools/Blender`), textures and audio are generated in code. Hand-tracking rig and interaction samples come from Unity's XR Interaction Toolkit and XR Hands packages.

Part of CIM 423 at the University of Miami. Nicholas Moffett, 2026.
