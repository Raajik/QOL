# QOL

A combined ACEmulator server mod providing a suite of convenience and balance enhancements. Each feature is independently toggleable via the `Patches` list in `Settings.json`.

---

## Enabling Features

Add or remove entries from the `Patches` array to control which features are active. Any feature not listed here will make no changes to the server.

```json
"Patches": [
  "animations",
  "augmentations",
  "defaults",
  "fellowships",
  "permanentobjects",
  "recklessness",
  "tailoring"
]
```

---

## Features

### Animations

Controls animation playback speed for certain actions. When a motion has an entry in `AnimationSpeeds`, that speed is used instead of the value from the motion table.

```json
"Animations": {
  "DieSeconds": 0.0,
  "AnimationSpeeds": {
    "AllegianceHometownRecall": 0.0,
    "HouseRecall": 0.0,
    "LifestoneRecall": 0.0,
    "MarketplaceRecall": 0.0,
    "PKArenaRecall": 0.0
  }
}
```

| Setting | Type | Default | Description |
|---|---|---|---|
| `DieSeconds` | float | `0.0` | Delay in seconds between each stage of the `/die` suicide sequence |
| `AnimationSpeeds` | Dictionary | (see above) | Map of [MotionCommand](https://github.com/ACEmulator/ACE/blob/master/Source/ACE.Entity/Enum/MotionCommand.cs) → speed override. Set to `0.0` to skip the animation entirely |

> **Note:** Only animations that transition from `null` to a set value are intercepted. Things like `/mp` (magic prayer) and attack frame timing are not currently covered.

---

### Augmentations

Overrides augmentation caps and optionally removes shared-group restrictions for attributes and resists.

```json
"Augmentation": {
  "IgnoreSharedAttribute": false,
  "IgnoreSharedResist": false,
  "MaxAugs": {
    "Strength": 10,
    "Endurance": 10,
    ...
  }
}
```

| Setting | Type | Default | Description |
|---|---|---|---|
| `IgnoreSharedAttribute` | bool | `false` | When `true`, attribute augmentations no longer count toward the shared attribute group limit |
| `IgnoreSharedResist` | bool | `false` | When `true`, resist augmentations no longer count toward the shared resist group limit |
| `MaxAugs` | Dictionary | (see below) | Per-type augmentation caps, applied on server startup |

**Default Augmentation Caps**

| Category | Types | Cap |
|---|---|---|
| Attributes | Strength, Endurance, Coordination, Quickness, Focus, Self | 10 each |
| Tinkering | Salvage, Item, Armor, MagicItem, Weapon | 1 each |
| Inventory | PackSlot | 1 |
| Burden | BurdenLimit | 5 |
| Death Penalties | DeathItemLoss | 3 |
| Death Penalties | DeathSpellLoss | 1 |
| Protection / XP | CritProtect, BonusXP | 1 each |
| Salvage | BonusSalvage | 4 |
| Spells | ImbueChance | 1 |
| Regeneration | RegenBonus | 2 |
| Spell Effects | SpellDuration | 5 |
| Resistances | Slash, Pierce, Bludgeon, Acid, Fire, Cold, Electric | 2 each |
| Focus Augs | Creature, Item, Life, War, Void | 1 each |
| Combat | CritChance, CritDamage, Melee, Missile, Magic, Damage, DamageResist, AllStats | 1 each |

---

### Credits

Controls the effective specialization credit cap used by the Skill Alteration Device (Gem of Enlightenment / Gem of Forgetfulness).

```json
"MaxSpecCredits": 9999
```

| Setting | Type | Default | Description |
|---|---|---|---|
| `MaxSpecCredits` | int | `9999` | Effective specialization credit cap. The patch offsets the reported total by `70 − MaxSpecCredits`, so values above ~400 (more than all skills combined) remove the cap entirely. Set to `70` to restore vanilla behavior. |

> **How it works:** ACE hardcodes a 70-credit limit checked against `GetTotalSpecializedCredits()`. This patch postfixes that method and subtracts `MaxSpecCredits − 70` from the reported total. With the default of `9999`, the reported total is always driven to `0`, so the check always passes regardless of how many skills are specialized.

---

### Defaults

Applies default property values to objects when a property transitions from `null` to being set for the first time. Useful for ensuring new characters start with specific properties.

```json
"Defaults": {
  "Int64Defaults": {
    "MaximumLuminance": 1000000000
  },
  "IntDefaults": {},
  "FloatDefaults": {},
  "BoolDefaults": {}
}
```

| Dictionary | Property Type | Description |
|---|---|---|
| `Int64Defaults` | `PropertyInt64` | Long integer property defaults |
| `IntDefaults` | `PropertyInt` | Integer property defaults |
| `FloatDefaults` | `PropertyFloat` | Float property defaults |
| `BoolDefaults` | `PropertyBool` | Boolean property defaults |

The default `MaximumLuminance: 1,000,000,000` ensures new characters can store luminance up to 1 billion. Only applies when the property has not been previously set on that character.

---

### Fellowships

Expands and customizes the fellowship system, including member cap, XP sharing, and bulk-invite tools.

```json
"Fellowship": {
  "SendDetails": true,
  "MaxMembers": 30,
  "IgnoreBusy": false,
  "OverrideSharePercent": true,
  "FlatSharePercent": 1.0,
  "SharePercent": {
    "1": 1.0,
    "2": 0.75,
    "3": 0.6,
    "4": 0.55,
    "5": 0.5,
    "6": 0.45,
    "7": 0.4,
    "8": 0.35,
    "9": 0.3
  },
  "DefaultShare": 0.0
}
```

| Setting | Type | Default | Description |
|---|---|---|---|
| `SendDetails` | bool | `true` | When enabled, both the inviter and the recruit receive a chat message on invite showing the fellowship name and resulting XP share percentage |
| `MaxMembers` | int | `30` | Maximum number of members allowed in a fellowship (vanilla cap is 9) |
| `IgnoreBusy` | bool | `false` | When `true`, bypasses the `IsBusy` check and force-accepts invites without a confirmation popup — useful for server-side bulk recruitment |
| `OverrideSharePercent` | bool | `true` | When `true`, all members always receive `FlatSharePercent` regardless of fellowship size, ignoring the `SharePercent` table |
| `FlatSharePercent` | double | `1.0` | The flat XP share multiplier used when `OverrideSharePercent` is enabled (`1.0` = 100%) |
| `SharePercent` | Dictionary | (see above) | Per-member-count XP share table, used only when `OverrideSharePercent` is `false` |
| `DefaultShare` | double | `0.0` | Fallback share value when `OverrideSharePercent` is `false` and the member count has no entry in `SharePercent` |

#### `/fship` Command

Bulk-invites players into the fellowship. Requires the invoking player to already be in a fellowship.

| Usage | Behavior |
|---|---|
| `/fship` | Invites all fellowship-less players on the current landblock |
| `/fship <name>` | Invites all online players whose name contains `<name>` (case-insensitive) |

> `IgnoreBusy` applies to `/fship` invites as well — when enabled, targeted players are added immediately without a popup.

---

### Permanent Objects

Allows specific world objects to survive landblock resets. Objects must be marked with the `Permanent` fake property to be preserved.

Mark an object as permanent using the `FakeBool.Permanent` property. Marked objects are excluded from the normal landblock cleanup sweep and their inventory is not cleared on container reset.

---

### Recklessness

Rewrites the Recklessness skill modifier to tie bonus damage to where the attack bar is held, rather than the vanilla behavior.

```json
"Recklessness": {
  "PowerLow": 0.2,
  "PowerHigh": 0.8,
  "RatingTrained": 10,
  "RatingSpecialized": 20
}
```

| Setting | Type | Default | Description |
|---|---|---|---|
| `PowerLow` | float | `0.2` | Minimum attack bar percentage (0.0–1.0) required to trigger Recklessness |
| `PowerHigh` | float | `0.8` | Maximum attack bar percentage at which Recklessness still applies |
| `RatingTrained` | int | `10` | Damage rating bonus when Recklessness is trained |
| `RatingSpecialized` | int | `20` | Damage rating bonus when Recklessness is specialized |

**How it works:**

- Only active in Melee or Missile combat mode
- Attack bar must be between `PowerLow` (20%) and `PowerHigh` (80%)
- If the player's Recklessness skill is lower than their current attack skill, the rating bonus scales down proportionally: `scale = recklessness_skill / attack_skill`
- Final modifier is computed via `GetDamageRating()` using the scaled rating

---

### Tailoring

Removes the society armor restriction from the tailoring system, allowing society armor pieces to be used as tailoring targets or sources.

No configuration required — enabling the feature is sufficient.

---

## Commands

| Command | Access | Description |
|---|---|---|
| `/fship [name]` | Player | Bulk-invites players to your fellowship. Omit `name` to invite the current landblock; provide a name fragment to search all online players |
| `/setlum` | Player | Sets your luminance to the maximum value (`MaximumLuminance`). Only works if that property has been set on your character |
| `runas <player> <command>` | Admin | Runs a command as the named online player, bypassing normal access checks. *Experimental.* |

> Example: `runas alttest faction ch 5`

---

## Settings.json Reference

Full example with all defaults:

```json
{
  "Patches": [
    "animations",
    "augmentations",
    "defaults",
    "fellowships",
    "permanentobjects",
    "recklessness",
    "tailoring"
  ],
  "MaxSpecCredits": 9999,
  "Animations": {
    "DieSeconds": 0.0,
    "AnimationSpeeds": {
      "AllegianceHometownRecall": 0.0,
      "HouseRecall": 0.0,
      "LifestoneRecall": 0.0,
      "MarketplaceRecall": 0.0,
      "PKArenaRecall": 0.0
    }
  },
  "Defaults": {
    "Int64Defaults": { "MaximumLuminance": 1000000000 },
    "IntDefaults": {},
    "FloatDefaults": {},
    "BoolDefaults": {}
  },
  "Fellowship": {
    "SendDetails": true,
    "MaxMembers": 30,
    "IgnoreBusy": false,
    "OverrideSharePercent": true,
    "FlatSharePercent": 1.0,
    "SharePercent": {
      "1": 1.0, "2": 0.75, "3": 0.6, "4": 0.55, "5": 0.5,
      "6": 0.45, "7": 0.4, "8": 0.35, "9": 0.3
    },
    "DefaultShare": 0.0
  },
  "Recklessness": {
    "PowerLow": 0.2,
    "PowerHigh": 0.8,
    "RatingTrained": 10,
    "RatingSpecialized": 20
  },
  "Augmentation": {
    "IgnoreSharedAttribute": false,
    "IgnoreSharedResist": false,
    "MaxAugs": {
      "Strength": 10, "Endurance": 10, "Coordination": 10,
      "Quickness": 10, "Focus": 10, "Self": 10,
      "PackSlot": 1, "BurdenLimit": 5,
      "DeathItemLoss": 3, "DeathSpellLoss": 1,
      "CritProtect": 1, "BonusXP": 1, "BonusSalvage": 4,
      "ImbueChance": 1, "RegenBonus": 2, "SpellDuration": 5,
      "Salvage": 1, "ItemTinkering": 1, "ArmorTinkering": 1, "MagicItemTinkering": 1, "WeaponTinkering": 1,
      "ResistSlash": 2, "ResistPierce": 2, "ResistBludgeon": 2, "ResistAcid": 2,
      "ResistFire": 2, "ResistCold": 2, "ResistElectric": 2,
      "FociCreature": 1, "FociItem": 1, "FociLife": 1, "FociWar": 1, "FociVoid": 1,
      "CritChance": 1, "CritDamage": 1,
      "Melee": 1, "Missile": 1, "Magic": 1,
      "Damage": 1, "DamageResist": 1, "AllStats": 1
    }
  }
}
```
