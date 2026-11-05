import sys, math, random
sys.path.insert(0, "/private/tmp/claude-501/-Users-nmoff/5c2b5fe1-51bf-4bb6-bec5-edba5506ae6a/scratchpad/bl")
from lib2 import *
for n, c in {"M_Plinth": (.07,.06,.06), "M_PlinthTop": (.1,.09,.09), "M_Steel": (.75,.75,.8), "M_Platter": (.55,.55,.6), "M_Mat": (.04,.04,.045),
             "M_Dot": (.95,.95,.9), "M_LEDRed": (1,.1,.05), "M_LEDGreen": (.1,1,.3), "M_Black": (.04,.04,.05), "M_Brass": (.8,.6,.2),
             "M_Vinyl": (.03,.03,.04), "M_VinylLabel": (.92,.42,.34), "M_MixerBody": (.12,.12,.14), "M_MixerTop": (.17,.17,.2),
             "M_Wood": (.36,.2,.1), "M_WoodDark": (.18,.1,.06), "M_CrateWood": (.42,.26,.14), "M_Felt": (.1,.05,.07), "M_Label": (.85,.8,.7),
             "M_Sleeve0": (.9,.4,.5), "M_Sleeve1": (.3,.7,.8), "M_Sleeve2": (.95,.8,.3), "M_Sleeve3": (.5,.4,.8)}.items():
    M(n, c)

def build_turntable():
    """origin = world (-0.37, 0.95, 0.67). Plinth top at local y=0.06."""
    A = Asset("Turntable")
    A.box((0.62, 0.05, 0.56), (0, 0.03, 0), "M_Plinth", bevel=0.01, seg=3)
    A.box((0.61, 0.006, 0.55), (0, 0.058, 0), "M_PlinthTop", bevel=0.002, seg=1)
    A.box((0.64, 0.02, 0.02), (0, 0.045, -0.285), "M_Wood", bevel=0.004)                  # wood front lip
    A.box((0.64, 0.02, 0.02), (0, 0.045, 0.285), "M_Wood", bevel=0.004)
    for sx in (-1, 1):
        A.box((0.02, 0.058, 0.58), (sx * 0.315, 0.029, 0), "M_Wood", bevel=0.004)
        for sz in (-1, 1):
            A.cyl(0.02, 0.014, (sx * 0.27, 0.007, sz * 0.23), "M_Black", seg=12)
    # controls front right
    A.cyl(0.02, 0.014, (0.24, 0.067, -0.2), "M_Steel", seg=20, bevel=0.002)                  # start/stop
    A.cyl(0.011, 0.012, (0.20, 0.066, -0.12), "M_Black", seg=14)                              # 33
    A.cyl(0.011, 0.012, (0.25, 0.066, -0.12), "M_Black", seg=14)                              # 45
    A.cyl(0.006, 0.004, (0.26, 0.064, -0.245), "M_LEDRed", seg=8)                             # power LED
    A.box((0.04, 0.002, 0.008), (0.22, 0.062, -0.16), "M_Label")
    # pitch fader slot on the right edge
    A.box((0.018, 0.003, 0.14), (0.285, 0.061, 0.05), "M_Black")
    A.box((0.03, 0.01, 0.018), (0.285, 0.066, 0.02), "M_Steel", bevel=0.002)
    # tonearm resting post
    A.cyl(0.008, 0.045, (0.2, 0.082, 0.15), "M_Steel", seg=10)
    A.box((0.03, 0.006, 0.012), (0.2, 0.108, 0.15), "M_Steel", bevel=0.002)
    return A

def build_platter():
    """origin = platter centre, top of mat at local y ~ 0.0045 (record rests at 0.006)."""
    A = Asset("PlatterModel")
    A.cyl(0.2, 0.026, (0, -0.017, 0), "M_Platter", seg=64, bevel=0.002)
    A.cyl(0.19, 0.009, (0, 0.0, 0), "M_Mat", seg=64, bevel=0.0015)
    A.torus(0.1, 0.0015, (0, 0.0046, 0), "M_Platter", seg=48, rseg=4)                         # groove rings on the mat
    A.torus(0.14, 0.0015, (0, 0.0046, 0), "M_Platter", seg=48, rseg=4)
    # strobe dots on the rim
    n = 72
    for i in range(n):
        a = 2 * math.pi * i / n
        if i % 2 == 0:
            A.box((0.006, 0.004, 0.003), (math.cos(a) * 0.1996, -0.01, math.sin(a) * 0.1996), "M_Dot", rot=(0, -math.degrees(a) + 90, 0))
    return A

def build_armbase():
    """origin = (-0.12, 1.01, 0.82): fixed pivot housing"""
    A = Asset("ArmBase")
    A.cyl(0.042, 0.014, (0, 0.007, 0), "M_Black", seg=24)
    A.cyl(0.03, 0.05, (0, 0.04, 0), "M_Steel", seg=20, bevel=0.002)
    A.cyl(0.022, 0.012, (0, 0.07, 0), "M_Black", seg=18)
    return A

def build_tonearm():
    """origin = pivot, arm along +Z, pivot at the top of the housing"""
    A = Asset("TonearmModel")
    A.cyl(0.018, 0.03, (0, 0.0, 0), "M_Steel", seg=16)
    A.tube([(0, 0.0, -0.03), (0, 0.0, 0.12), (0.0, -0.002, 0.21)], 0.0055, "M_Steel", seg=10)
    A.cyl(0.02, 0.05, (0, 0.0, -0.065), "M_Black", rot=(90, 0, 0), seg=16)                         # counterweight
    A.cyl(0.0205, 0.004, (0, 0.0, -0.045), "M_Steel", rot=(90, 0, 0), seg=16)
    A.box((0.024, 0.006, 0.045), (0, -0.007, 0.226), "M_Black", bevel=0.001)                      # headshell
    A.box((0.016, 0.012, 0.022), (0, -0.015, 0.228), "M_Steel", bevel=0.002)                       # cartridge
    A.cyl(0.0025, 0.012, (0, -0.026, 0.237), "M_Brass", r2=0.0008, seg=6)                           # stylus
    A.tube([(0.0, -0.005, 0.245), (0.0, 0.01, 0.255), (0.0, 0.014, 0.265)], 0.0022, "M_Steel", seg=6)   # finger lift
    return A

def build_mixer():
    """origin = mixer origin (0.28,0.95,0.66). Body top at local y=0.06."""
    A = Asset("MixerModel")
    A.box((0.5, 0.058, 0.46), (0, 0.03, 0), "M_MixerBody", bevel=0.008, seg=3)
    A.box((0.47, 0.004, 0.43), (0, 0.06, 0), "M_MixerTop", bevel=0.001, seg=1)
    for sx in (-1, 1):
        A.box((0.02, 0.066, 0.47), (sx * 0.255, 0.033, 0), "M_Wood", bevel=0.004)
    for sx in (-1, 1):
        for sz in (-1, 1):
            A.cyl(0.015, 0.012, (sx * 0.21, 0.006, sz * 0.19), "M_Black", seg=10)
            A.cyl(0.005, 0.003, (sx * 0.222, 0.0635, sz * 0.205), "M_Steel", seg=6)             # screws
    # VU window frame (bars are added in Unity)
    A.box((0.24, 0.003, 0.055), (0, 0.0635, 0.04), "M_Black", bevel=0.001)
    # LEDs
    for i, m in enumerate(["M_LEDGreen", "M_LEDRed", "M_LEDGreen"]):
        A.cyl(0.005, 0.004, (-0.19 + i * 0.02, 0.064, 0.0), m, seg=8)
    # flat DJ-style fader channel on the board (cap, slot and ticks are added in Unity); plate centred at z 0.15
    A.box((0.30, 0.003, 0.085), (0, 0.0625, -0.185), "M_Black", bevel=0.001, seg=1)
    A.box((0.25, 0.0012, 0.016), (0, 0.0642, -0.185), "M_MixerBody", bevel=0.0005, seg=1)
    for x in (-0.145, 0.145):
        A.box((0.004, 0.0015, 0.07), (x, 0.0642, -0.185), "M_Label")
    return A

def build_crate():
    """origin = crate origin (0.78,0.95,0.62): top of felt at local y=0.1"""
    A = Asset("CrateModel")
    A.box((0.37, 0.04, 0.37), (0, 0.02, 0), "M_CrateWood", bevel=0.004, uv=0.5)
    A.box((0.35, 0.062, 0.35), (0, 0.07, 0), "M_Felt", bevel=0.002, seg=1)
    A.box((0.34, 0.002, 0.34), (0, 0.1, 0), "M_Felt")
    for k in range(4):                                                               # back slats
        A.box((0.38, 0.032, 0.02), (0, 0.12 + k * 0.035, 0.175), "M_CrateWood", bevel=0.003, uv=0.5)
    for sx in (-1, 1):
        for k in range(2):                                                           # side slats
            A.box((0.02, 0.04, 0.36), (sx * 0.175, 0.115 + k * 0.05, 0), "M_CrateWood", bevel=0.003, uv=0.5)
        for sz in (-1, 1):
            A.box((0.03, 0.3, 0.03), (sx * 0.175, 0.15, sz * 0.175), "M_CrateWood", bevel=0.004, uv=0.5)
    A.box((0.34, 0.045, 0.02), (0, 0.075, -0.175), "M_CrateWood", bevel=0.003, uv=0.5)   # front lip
    A.box((0.16, 0.03, 0.006), (0, 0.075, -0.186), "M_Label")                                 # stencil plate
    # record sleeves leaning at the back, behind the record
    for i, (m, a) in enumerate((("M_Sleeve0", 3), ("M_Sleeve1", -2), ("M_Sleeve3", 5))):
        A.box((0.31, 0.31, 0.004), (-0.005 * i, 0.2, 0.158 - i * 0.012), m, rot=(-4, 0, a), uv=0.3)
    return A

def build_record():
    A = Asset("RecordModel")
    A.disc(0.15, (0, 0.0015, 0), "M_Vinyl", seg=72, inner=0.052)
    A.disc(0.15, (0, -0.0015, 0), "M_Vinyl", seg=72, inner=0.052, down=True)
    A.cyl(0.15, 0.003, (0, 0, 0), "M_Vinyl", seg=72, caps=False, smooth=True)
    A.disc(0.052, (0, 0.0019, 0), "M_VinylLabel", seg=40, inner=0.0035)
    A.disc(0.052, (0, -0.0019, 0), "M_VinylLabel", seg=40, inner=0.0035, down=True)
    A.cyl(0.052, 0.0038, (0, 0, 0), "M_VinylLabel", seg=40, caps=False)
    A.cyl(0.0035, 0.004, (0, 0, 0), "M_Steel", seg=10, caps=False)
    return A

B = {"Turntable": build_turntable, "PlatterModel": build_platter, "ArmBase": build_armbase, "TonearmModel": build_tonearm,
     "MixerModel": build_mixer, "CrateModel": build_crate, "RecordModel": build_record}

if __name__ == "__main__":
    reset()
    if "--sheet" in sys.argv:
        arts = []
        off = {"Turntable": (0, 0, 0), "PlatterModel": (0, 0.1, 0.0), "ArmBase": (0.4, 0.0, 0), "TonearmModel": (0.4, 0.08, 0), "MixerModel": (1.0, 0, 0), "CrateModel": (1.7, 0, 0), "RecordModel": (2.2, 0.15, 0)}
        for n, f in B.items():
            a = f(); a.origin = Vector(off[n]); arts.append(a)
        preview(arts, "/tmp/prev_deck.png", (0.9, 0.9, -1.4), (0.9, 0.05, 0), lens=45, res=(1500, 640))
    else:
        for n, f in B.items():
            export(f(), n + ".fbx")
