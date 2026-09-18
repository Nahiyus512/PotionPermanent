# Potion Eternity (PotionPermanent)

Keeps a potion's or food's buff active permanently once you stock the required amount, and adds a buff controller.

## Features

- Stacks of the same potion or food in your inventory, piggy bank, safe, defender's forge or void vault keep their buff active permanently once the threshold is reached
- Sustained buffs show no countdown, just like the campfire and heart lantern
- Food buffs keep only the highest tier (exquisitely stuffed > well fed > fed)
- Ale and sake keep their Tipsy buff too; genuinely harmful debuffs are never sustained
- Threshold defaults to 30 and can be changed to 1-999 in the in-game F6 -> config page
- Drop below the threshold and the buff simply expires when its time runs out

## Buff controller

Left click any buff icon in the top-left buff bar to open the buff controller:

- The panel lists every active buff in order, with its description on hover
- **Left click** turns a buff on / off. A buff you turn off disappears at once and is kept suppressed
  (including buffs granted by other mods such as Portable Stations), drawn dimmed with a red frame
- **Alt + left click** favourites a buff and moves it to the front (gold frame)
- The "Enable all" button at the bottom clears every disabled buff at once
- The panel is draggable, closes with Esc or the X button, and reopens where you left it
- State is stored in `TerrariaModder\mods\potion-permanent\buff-controller.json`

Candle furniture: the water candle buff is driven by scene metrics, and portable-storage mods only add the
count after the scan has already finished, so the buff never lights up. With "Portable candle buffs" enabled
this mod applies that last step, so a carried water candle really does light up and can be toggled here.

## Configuration

Edit it in the in-game mod menu -> config page (applies instantly). The file lives at
`core/configs/potion-permanent.client.json`:

| Option | Description |
| --- | --- |
| Enabled | Master switch for potion sustain |
| Required amount | Minimum stack needed to sustain a buff (default 30) |
| Buff controller | Open the controller by clicking a buff icon in the top-left corner |
| Portable candle buffs | Make carried candle buffs such as the water candle actually apply |
| Debug logging | Write debug output |

## Notes

- Any storage location counts (inventory or portable containers)
- Below the threshold a sustained buff simply expires when its timer runs out
- The controller leaves every other buff untouched; disabled debuffs are removed too
