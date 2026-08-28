# GunsGunsGuns!

![Version](https://img.shields.io/github/v/release/Luca-Nero/GunsGunsGuns?style=flat-square)
![Game Version](https://img.shields.io/badge/Game-v0.1%2B-blue?style=flat-square)
[![Ko-fi](https://img.shields.io/badge/Ko--fi-Donate-ff5e5b?style=flat-square&logo=ko-fi&logoColor=white)](https://ko-fi.com/Luca_Nero)

Adds a bunch new guns! An AK-47, a pump shotgun and an anti-materiel rifle, cycled with the mouse wheel. Nothing here is a re-skinned game weapon: the rounds travel, ricochet, punch through Bob and throw brass, the models are held and animated in code, and the sniper's scope is a live picture-in-picture render rather than a zoom trick.

---

## Features

- **Three Weapons, One Slot:** Take the slot and cycle with the mouse wheel. The slot's name and icon change with the weapon, and each carries its own fire rate, ballistics, recoil, sights and model.
    - **AK-47:** 600 RPM full-auto, tight cone, punches through up to three surfaces.
    - **RM870:** 9-pellet pump gun. Wide spread, heavy shove per pellet, barely any penetration - and it racks *after* the shot, not during it.
    - **AS50:** 35 RPM anti-materiel rifle. 800 m/s, twelve penetrations, a brutal kick and a working scope.
- **Travelling Rounds:** No hitscan. Every round is marched through the world with its own gravity and lifetime, so range, drop and lead all matter.
    - **Ricochet:** Grazing hits skip off surfaces, losing energy and scattering a little each bounce.
    - **Penetration:** Rounds bore through cover and out the other side, weakening with each surface and with how much material they crossed.
    - **External Forces:** Rounds sample FruitLib's force field in flight, so a Singularity's gravity well will visibly bend a burst.
- **Impacts:** Surface marks stretch and fade by the angle they were struck at, and impact audio plays from a source at the hit point rather than on the player.
- **Held Models:** Body, magazine and bolt as separate parts, animated in code - no armature. The bolt or pump runs back, holds, then slams home with a hard stop at each end.
    - **Rounds Leave The Barrel:** Shots start at the muzzle and converge on the crosshair, so they come out of the gun rather than the middle of the screen and still land where you aim.
    - **Spent Cases:** The game's own shells are thrown from a real ejection port. Optionally swap them for cases built to the actual calibre - 7.62x39, 12 gauge, 12.7x99.
    - **Viewmodel Camera:** A dedicated camera layer keeps the gun from being sliced by the near plane, however close it sits.
- **Recoil That Fights Back:** The kick goes into the *camera*, not just the model, so a held burst walks your aim upward and you have to pull it back down. The gun itself only rumbles.
    - **Sight Picture Survives It:** Recoil turns the gun about your eye, so front and rear sights sweep together and stay lined up while it kicks.
- **Iron Sights & ADS:** Hold right mouse to bring the gun up. Aiming tightens the cone, steadies the sway and fades the crosshair out. Zoom is a fraction of your own field of view, so it respects whatever FOV you play at.
- **Picture-In-Picture Scope:** The AS50's scope is a second camera rendering the world at 8 degrees onto the lens. Magnification lives in the glass, so the tube stays tube-sized instead of swallowing the screen, and the image carries the rifle's own sway and recoil.
    - **Always Live:** The glass keeps updating at a reduced rate with the gun down, so it is never a frozen picture when you bring it up.
    - **Reticle:** A fine cross replaces the crosshair once you are behind the glass.
- **Hold Your Breath:** Hold **Mouse 4** while aimed to settle the drift for five seconds, then let it come back. A thin meter appears under the reticle only once you have started spending it.
- **QoL Tweaks:** Firing and the crosshair are suppressed behind the pause screen and the FruitLib menu, rounds and shells are cleared on scene load, the fire rate carries its remainder so it stays honest at any frame rate, and a debug mode draws every round's flight path.

## Requirements & Compatibility

- **Prerequisites:** MelonLoader 0.7.2+ Installation. [Check out their Tutorial!](https://melonwiki.xyz/#/) and the latest [FruitLib](https://github.com/Luca-Nero/FruitLib) in your `Mods/` folder - GunsGunsGuns will not start without it.
- **Optional:** [Singularity](https://github.com/Luca-Nero/Singularity) - its gravity wells will bend rounds in flight.
- **Compatibility:** No known Incompatabilities. Explosion effects from BombsAway may briefly override the aimed field of view.

## Installation

1. Download the latest release from the [Releases page](../../releases/latest).
2. Extract the archive.
3. Drop the contents into your game's `Mods/` directory.

## Controls (Defaults)

| Key | Action |
|-----|--------|
| 5 | Select the weapon slot |
| Left Mouse | Fire (hold for full-auto) |
| Mouse Wheel | Cycle weapon |
| Right Mouse | Aim down sights (hold) |
| Mouse 4 | Hold breath while aimed |

## Configuration

`GGGConfig.ini` is created next to the DLL on first launch, and the in-game FruitMenu mirrors it - Audio, Barrel, Breath, Debug, Forces, HUD, Impacts, Model, Scope, Shells, Sights and Wound.

Per-weapon numbers are deliberately **not** in the INI. Fire rate, ballistics, penetration, recoil, model placement and sights all live on the weapon itself, so adding a fourth gun is one entry in code and nothing else. The INI holds what is genuinely global: presentation, impact marks, shell behaviour, scope resolution and the switches above.

Delete the file to regenerate it from defaults - a stale INI will silently override a changed default after an update.

---

## Support & Feedback

Found a bug or have a suggestion? Feel free to open an issue on the [Issues page](../../issues) or catch me on Discord.

If you enjoy my work and want to support future updates, feel free to [buy me a coffee on Ko-fi](https://ko-fi.com/Luca_Nero)!

## Credits

Shoutout to @TheCLsOfCasey on Discord for the helpful feedback and testing, leading to the addition of weapon Customizability

All models were taken off of Sketchfab from [TastyTony](https://sketchfab.com/TastyTony) so huge thanks to them! 

## License

[AGPL-3.0](LICENSE) © Luca Nero / Game Community
