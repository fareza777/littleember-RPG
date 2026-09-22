# LITTLE EMBER — Grand Design Document

> **Genre:** Top-down Action-Adventure RPG (Zelda-like) · Single player
> **Target length:** 20–25 h main story · 28–30 h completionist
> **Engine:** Unity 6000.6.1f1, URP 2D (project `C:\Pixel Game Master\Pixel Game`)
> **Asset base:** Super Retro Collection MAIN BUNDLE (verified 18,348 assets)
> **Language of record:** English
> **One-line pitch:** *"The Prince of Dusk blew out the five Everflames. You carry the last spark."*

---

# PART I — THE STORY

## 1. World premise (simple enough for a child, deep enough for an adult)

Fire is alive in the Emberlands. Centuries ago the **First Flame** fell from the sky, and from it five **Everflames** were kindled in five Sanctuaries. As long as they burn, the land stays warm, crops grow, and the ancient cold — the **Hollow Frost** — sleeps beneath the world.

Every ten years, the Emberlands celebrate the **Kindling Festival**, when the flames are ritually renewed. The game opens on that night.

## 2. Prologue — "The Night the Lights Went Out" (0:00–1:00)

**Emberholt**, a farming village in a valley. **PIP** (12, default name — player-renameable) is the grandchild of **ODO**, the old lampkeeper. Pip's father **KAEL**, the High Flamekeeper, vanished ten years ago. Odo never speaks of it.

The Kindling Festival begins (marketplace stalls, lamps, torii-style banners — the village at its most beautiful; this beauty is deliberately staged so its loss hurts). Midway through the ceremony, a figure in a dusk-blue cloak — the **PRINCE OF DUSK** — walks through the crowd, unchallenged, and **blows out the Everflame of Emberholt like a birthday candle**.

The cold arrives in seconds. Crops frost over. Tall grass stops moving. Pale things crawl out of the tree line.

Odo shoves the family's iron lantern into Pip's arms. Inside, **one spark survived** — the Little Ember. His instruction is the whole game:

> *"Five Sanctuaries. Five Everflames. Carry her gently, little one — and whatever you see in the dark, do not let her go out."*

**Tutorial in disguise:** lantern controls, sword basics on training dummies (`dummy` prefab), pushing a crate to reach the village gate (`block_push_on_contact`), first secret behind tall grass (`tall_grass_react_on_contact`).

## 3. Act structure & the five regions

Each region follows the same emotional rhythm: **arrive in the cold → meet the people → earn the key item → conquer the Sanctuary → defeat the Warden → RELIGHT** — and the relight is the reward the player *sees*, not just a cutscene: the whole region's palette, music, and NPC behavior transform (see Art Direction, the Two-State World system).

### Region 1 — Verdant Lowlands (green) · Hours 1–4
- **Settlement: Millbrook** — farmers, windmill hill, rabbits and birds everywhere.
- **Story beat:** The harvest was days away when the cold hit. Villagers argue: flee or stay? Pip's arrival with a living flame settles the argument — hope returns before the flame does. (The story teaches: the lantern alone already changes people.)
- **Key item:** **Woodcutter's Hatchet** — earned by repairing the carpenter Mari's roof (teaches: help people, get tools, not shops).
- **Dungeon: Mossdeep Grotto** — flooded caves behind the bramble wall. Water-flow puzzles (animated water autotiles), pushable blocks, first traps.
- **Warden I: BRAMBLEMAW** (`Monsters_01`) — once the gentle moss-guardian of the grotto, gone feral in the dark. 2 phases: armored charge → soft belly vulnerability after it trips on pushed blocks.
- **Relight reward:** **Verdant Cloak** (hero color_2) — regenerates hearts slowly while standing in sunlight.
- **Companion joins:** Pip frees a young fox caught in a poacher's snare — **CINDER**, who follows for the rest of the game (fox sprite). Cinder digs up buried secrets and points at hidden paths. Pure heart, zero words.

### Region 2 — Sakura Reach (pink) · Hours 4–8
- **Settlement: Hanamura** — a shrine town under eternal-petal trees (13 torii prefabs form the processional road), lanterns on every eave.
- **Story beat:** Here the cold didn't freeze the land — it made the petals fall *and never stop*, a town buried in pink snow. The shrine maiden **SAYA** hasn't rung the festival bell since her brother vanished into the dark. Side quest to return the bell clapper = one of the game's signature quiet moments.
- **Key item:** **Ember Gloves** — lift and carry boulders/pots (`lift`, `carry_idle`, `carry_run` anims).
- **The Ember Arena:** a colosseum where fighters battle on a raised stage — framed in **front view**, spotlight on the challenger (artistic framing that turns the 6 front-only bonus monsters + battler portraits into a feature). Optional bracket, 6 elite opponents, champion title, unique Flame Art as prize.
- **Dungeon: Tideglass Cave** — reached across the **Glimmer Shore** (beach water tiles). Tide puzzles: raise/lower water level via switch chains; carry ember-braziers across floating platforms (Gloves payoff).
- **Warden II: TIDECALLER** (`Monsters_02`) — a river spirit swollen with cold rain. Arena floods in phase 2; safe ground shrinks; the Skybow-less counterplay is throwing pots (`throw` anim).
- **Foreshadow #1:** villagers whisper that a "dusk-cloaked man" carried a drowned child out of the flood last week. The villain saves children? Pip asks Odo by letter. No reply.
- **Relight reward:** **Sakura Cloak** (color_3) — wider lantern radius; petals trail behind the run.

### Region 3 — Amber Marches (autumn) · Hours 8–13
- **Settlement: Rustwick** — hunters, charcoal-burners, foxes in the woods.
- **Story beat:** This land has been autumn for a hundred years — nobody here has ever seen spring. (Plant the question: *why?*) The people are warm but the mood is melancholy; the region's music never resolves its final note.
- **Key item:** **Skybow** (`bow` anim + `arrow` asset) — shoots distant switches, fire-arrows after visiting a sanctuary brazier.
- **Dungeon: THE GRANDFATHER TREE** — the signature dungeon, built from the gigantic tree pack: a colossal dying tree, climbed *from the inside* — hollow trunk, rope bridges, vine climbs, abandoned tree-house village of a people nobody remembers. Vertical design: fall = lose progress, not hearts. Light shafts pierce the trunk — the most beautiful dungeon in the game.
- **Warden III: WYRMROOT** (`Monsters_03`) — the tree's own heart-root, worming mad with cold. Weak point opens only when fire-arrows relight the room's hanging lanterns.
- **MID-GAME TWIST (Twist 1):** At the summit, the Prince of Dusk steps out of the dark. He looks at Pip's lantern for a long moment… and says Pip's name. It is **KAEL. The father.**
  > *"You have her eyes. And her lantern, I see. Ask your grandfather what the Everflames eat — then decide whose errand you're running."*
  He doesn't fight. He leaves a trail of frost-flowers. Pip's hands shake for the rest of the climb down (idle anim becomes breath_idle in cold rooms from here on — a tiny, free storytelling device).

### Region 4 — Crystal Expanse (blue/crystal) · Hours 13–18
- **Settlement: Shiverton** — miners and astronomers; penguins on the frozen lake, cats in every windowsill.
- **Story beat:** Everything here is crystal that *sings* when struck — the region is a giant instrument. The mine is the town's life; the mine is also where the cold comes from.
- **Set-piece: GLITTERDEEP MINE kart ride** (Kart clips) — a high-speed rail sequence through glittering galleries, jumping broken track, ducking beams. The game's adrenaline peak.
- **Key item:** **Deepflame Lantern** — reveals invisible crystal paths, melts ice seals.
- **Dungeon: Palace of Frost** — mirror-and-light puzzles (rotate crystal statues — Statues prefabs — to bounce the lantern beam).
- **Warden IV: CRYSTALLIS** (`Monsters_04`) — a prism-serpent; can only be hurt by its own reflected beam.
- **Revelation (Twist 2, part 1):** Pip finds Kael's old expedition journal in the astronomers' archive (told as an illustrated **Storybook Page** using the painterly `Backgrounds` art): **the Everflames burn by drinking the land's warmth.** That's why Amber Marches never sees spring and why the Expanse froze long before tonight. Kael blew them out to *free* the land — but the flames were also the seal on the **Hollow Frost**. He was right, and he was wrong, and now the thing below is waking — and wearing his body.
- **Relight reward:** **Crystal Cloak** (color_4) — immunity to chill-slow; sprint leaves ice-sparkle.

### Region 5 — Ashen Ruins (gray) · Hours 18–23
- **No living settlement** — only **CINDRALIS**, the dead first capital: broken columns (Columns 21), statues of forgotten kings (Statues 7), and the **Library of Cinders** (Books 15) where the whole truth is finally readable, mural by mural.
- **Story beat:** The loneliest region by design — no shops, no quests, just wind. Cinder the fox stays close. The silence is the point: this is what the world looks like when neither flame nor frost wins.
- **Key item:** **Feather Mantle** — dash/glide across gaps (uses `run` + `spin` anims).
- **Dungeon: The Last Sanctuary** — all previous mechanics combined: water, light-beams, blocks, ice, brambles. The final exam.
- **Warden V: ASHEN SENTINEL** (`Monsters_05`) — the first guardian ever made, still loyal to a dead king. The most honorable fight in the game: it bows before phase 1.
- **Revelation (Twist 2, part 2):** The library's last mural + Odo's letter that finally arrives:
  **The night Kael vanished, baby Pip died of cold — for eleven minutes.** The Everflame of Emberholt gave a piece of itself to restart the child's heart. The "last spark" in the lantern did not survive the extinguishing. **It *came home*. Pip IS the Little Ember.** This is why monsters hunt the lantern, why the Hollow whispers in Pip's dreams, and why the flame gutters when Pip is afraid (mechanic: lantern dims at low HP — it was never oil).
- **Relight reward:** none. The fifth flame is lit, and the sky gets *darker*. The Hollow is loose. March on the rift.

### Finale — The Hollow Deep · Hours 23–26
- Beneath Cindralis, a wound in the world. No map, no music — only the lantern.
- **Penultimate boss: KAEL, PRINCE OF DUSK.** The fight cannot be won by damage — his frost-armor regenerates. The real mechanic: dodge until he overextends, then **hold the lantern to his chest** (a prompt, not an attack). Three times. On the third, the last frost breaks and he whispers: *"Take the shot, son."* →
- **Final boss: THE HOLLOW FROST** (built from tinted, upscaled `Monsters_02` water-serpent + crystal shard VFX from ARPG sheets — see Art Direction for making it feel bespoke). 2 phases:
  1. **The Blizzard** — bullet-hell-lite frost patterns; the lantern is the only warmth; standing in its light = stamina regen.
  2. **The Starved Dark** — it swallows the arena lights one by one. The fight trends toward total darkness. On the scripted "final hit," Pip's lantern goes out. Screen black. One heartbeat of silence.
- **Twist 3 — the ending the game was always about:** in the black, one light appears. Then ten. Then hundreds: **every villager, every quest NPC, the Arena crowd, the shrine maiden's bell-ringers, the five purified Wardens** — everyone Pip helped walks out of the dark carrying their own little lanterns. Odo speaks the last tutorial-turned-theme:
  > *"One great flame devours. A thousand little flames give. Light her, little ones."*
  The Hollow is not destroyed — it is **embraced into sleep** by shared warmth. The five Sanctuaries are relit as *shared flames* that no longer drink the land: the Amber Marches sees its first spring in a century (autumn tiles interleaved with green bloom — a unique post-game tileset state), the Expanse thaws at the edges.
- **Last beat:** the final spark returns to Pip's lantern on its own. It was never in danger. It was choosing.
- **Epilogue:** Kindling Festival, one year later. Kael, gray-haired, lights his son's lantern — the gesture inverted from the prologue. Post-credit: a green sprout on the Grandfather Tree.

## 4. Plot twist & foreshadowing ledger (so the twists feel earned)

| Twist | Planted where | Paid off |
|---|---|---|
| Kael is the Dusk Prince | "dusk-cloaked man saved a child" (R2); Odo's silence; lantern flares near the Prince (R1 random encounter) | R3 summit |
| Everflames drain the land | Eternal autumn (R3) and pre-frozen Expanse (R4) are visible *before* any explanation | R4 journal |
| Pip is the Little Ember | Lantern dims at low HP from hour 1; monsters ignore dropped loot to chase the lantern; Hollow whispers | R5 mural + Odo's letter |
| Shared flames save the world | Every quest is literally "bring warmth to someone"; Arena crowd chants Pip's name | Finale lantern walk |

---

# PART II — CHARACTERS

## Main cast

| Character | Role | Sprite plan (verified asset) | Notes |
|---|---|---|---|
| **PIP** | Hero, 12 | `Hero/color_1` start; cloaks unlock colors 2–5 | 16 anim groups × 4 dirs — full verbs: attack/bow/shield/throw/lift/carry/spin/dead |
| **CINDER** | Fox companion | `Characters/Animals/foxes` | Follower AI, digs secrets, sleeps at campfires. The emotional support fox |
| **ODO** | Grandfather, mentor | `Characters/Characters/chara_*` (elder pick) | Dies off-screen? No — lives to light the final fire. Subvert the dead-mentor cliché |
| **KAEL / Prince of Dusk** | Father, false antagonist | Hero `color_5` dark-tinted variant + dusk cloak recolor | His animations reuse hero set = deliberately "a player-shaped enemy" |
| **THE HOLLOW FROST** | True antagonist | Upscaled tinted `Monsters_02` + ARPG crystal VFX | Ancient cold; neither evil nor good — *starving* |
| **The 5 Wardens** | Region bosses | `Monsters_01–05` (4-dir walkers, 8 frames) | Corrupted guardians; purified post-fight into golden-tint gentle spirits that linger in the world |

## Key NPCs (from the 32 `chara_*` sheets — full cast of 32, these are the leads)

| NPC | Town | Function |
|---|---|---|
| **Mari** the carpenter | Millbrook | Gives Hatchet quest; upgrades house/garden |
| **Saya** the shrine maiden | Hanamura | Bell side-quest; opens Sanctuary |
| **Master Jiro** | Hanamura | Ember Arena master; sells Flame Art scrolls |
| **Flint & Petra** | Shiverton | Mine foremen; kart sequence; lantern upgrades |
| **Sage Velka** | Cindralis (ghost librarian) | Library of Cinders; the lore keeper |
| **Bram** the blacksmith | Emberholt | Sword/shield tiers |
| **Wick** the card collector | Travels all towns | Spirit Card album; trades duplicates |

Plus farmers, kids, merchants, arena fighters, astronomers — the remaining 24 sheets fill shops, quests, and festival crowds.

## The 6 Arena Elites (front-view bonus monsters — a feature, not a limitation)

`Additional_bonus_Monsters_front_anim_only_0–5` become the six exhibition champions of the Ember Arena, fought on a spotlight stage in theatrical front view: **Puffking, Baron von Sting, The Moss Baron, Lady Vex, Old Cinders, The Pale Guest**. Each is a personality, announced by Jiro like a wrestling show. Crowd = chara sheets cheering.

---

# PART III — SYSTEMS

## 1. The Kindling System (leveling)

No grind philosophy: XP = **Sparks**, from combat, quests, secrets, arena, cooking firsts. **Level cap 20**, tuned so main-path ≈ level 14, completionist ≈ 20.

Each level = **1 Kindling Point** in one of three branches:

| Branch | Stat identity | Sample nodes (pick 4–5 each tier) |
|---|---|---|
| **BLAZE** (combat) | Damage, combos, Flame Art slots | +combo finisher · charge-attack · +1 Art slot · counter-window after perfect block |
| **HEARTH** (survival) | Hearts, defense, cooking | +1 heart · armor vs burn/freeze · meals last longer · Gembul finds truffles (sellable) |
| **GLOW** (exploration) | Lantern, stamina, movement | +lantern radius · dash iframes · +card drop luck · sprint costs less |

**Hearts:** start 3 → +1 per Warden (5) → +3 from 12 hidden Heart Pieces (4 = 1 heart) = **11 max**.
**Spark Meter:** fills by dealing/taking damage; spent to cast **Flame Arts**.

## 2. Flame Arts — 12 skills (mapped 1:1 to the 12 ARPG attack sheets)

The ARPG folder (32 characters × 12 sets × 6 frames @ 96×128) is the **skill VFX library** — big, gorgeous, hand-drawn attack art overlaid on Pip's position:

| # | Flame Art | Source sheet | Effect |
|---|---|---|---|
| 1 | Crescent Arc | sword01 | Wide frontal slash |
| 2 | Brand Rush | sword02 | Dash-through strike |
| 3 | Dawnbreaker | sword03 | Charged overhead |
| 4 | Meteor Thrust | spear01 | Long lunge |
| 5 | Pinwheel Lance | spear02 | 360° sweep |
| 6 | Spark Sigil | staff01 | Place a damage rune |
| 7 | Cinder Nova | staff02 | Radial burst |
| 8 | Lantern Rain | staff03 | Falling embers AoE |
| 9 | Mirror Ward | staff04 | Projectile reflect |
| 10 | Solstice Ray | staff05 | Beam (uses lantern aim) |
| 11 | Starfall | staff06 | Screen-wide, 1/day |
| 12 | Final Flicker | slash01 | Execute under 25% HP |

Unlock sources: 3 from story, 3 from Arena ranks, 3 from hidden shrines, 3 from Spirit Card set completion.

## 3. Items

**Key items (gating, in order):**
Woodcutter's Hatchet (cut brambles) → Ember Gloves (lift/carry) → Skybow (ranged switches, fire/ice arrows) → Deepflame Lantern (reveal/melt) → Feather Mantle (dash-glide).

**Equipment:**
- **Swords:** Training Blade → Knight's Brand → **Dawnbreaker** (post-game forge from all 5 sanctuary coals).
- **Shields:** **Pot Lid** (joke item that's genuinely good early) → Oak Guard → Mirror Aegis (reflects).
- **Cloaks (the 5 hero colors):** Ashen (start), Verdant (sun regen), Sakura (radius), Crystal (chill-immune), **Dawn Cloak** (color_5, endgame — ember trail, all minor perks).
- **Lantern tiers:** radius/fuel upgrades ×3 at Shiverton.

**Consumables:** cooked meals (garden crops), lantern oil, throwable pots/stones (`throw`).

## 4. Collectibles (the 30-hour layer)

| Collectible | Count | Asset used | Why it's beautiful, not filler |
|---|---|---|---|
| **Spirit Cards** | 57 | All of `Battlers/` | Defeated enemies may drop their card — tarot-framed front-view art + 2 lines of lore in Wick's album. Turns "no back sprites" into a gallery feature. Full sets → Flame Arts |
| **Lost Sparks** | 60 | Fire/Cristal clips | Hidden in world-puzzle micro-dungeons; 10 = 1 Kindling Point |
| **Storybook Pages** | 8 | `Backgrounds/` 320×240 | Unlock painterly illustrated chapters (the land's history) on a storybook UI — SNES battle art reborn as book plates |
| **Seeds** | 22 | `Prefabs/Crops` | Garden content below |
| **Heart Pieces** | 12 | — | Classic exploration reward |

## 5. Hearthgarden (side system — the cozy core)

Pip's grandfather's field behind the house in Emberholt:
- Till (hoe) → plant → water daily (`Characters/Farm/` anims) → 4-stage growth (`SpriteSelectFrame.cs`, 22 crop prefabs) → cook meals at the hearth (heal/buff items).
- **Gembul the pig**: a Millbrook kid gifts a piglet (`Animals/pigs` **baby → grow → adult** stages). Fed scraps, it grows and finds truffles (rare sellables). Gembul is the game's mascot.
- Pure optional warmth — 0 gates, ~2 h of spread-out content, huge "home to return to" value.

## 6. Combat core

3-hit sword combo, shield block (stamina) / shield-walk, dodge-roll (`spin` anim, iframes via Glow branch), bow, throw, lift-carry-throw pots. Hit-stop 60–90 ms, knockback, white-flash, ember particle burst. Enemy roster: small mobs from `Characters/Monsters/` variants + tinted battler-inspired map walkers; every region reskins behavior + palette (an honest, art-directed reuse, not copy-paste — see Art Direction).

## 7. Save structure

Autosave at sanctuary braziers + crystal save points (`Animations/Cristal` 27 clips = the save-point identity). 3 slots. Post-game unlocks: Dawnbreaker forge, Arena EX bracket, New Game+ (Dusk Mode: inverted relit/dark states).

---

# PART IV — WORLD & PACING (20–30 h budget)

| Chapter | Content | Main hours | Side hours |
|---|---|---|---|
| Prologue — Emberholt & Kindling Festival | Tutorial, 1 mini-secret | 1.0 | 0.5 |
| R1 Verdant Lowlands | Millbrook, Hatchet questline, Mossdeep Grotto (12 rooms), Bramblemaw | 3.0 | 1.5 |
| R2 Sakura Reach | Hanamura, bell quest, Arena tiers 1–3, Glimmer Shore, Tideglass Cave (14 rooms), Tidecaller | 4.0 | 2.5 |
| R3 Amber Marches | Rustwick, Skybow trials, **Grandfather Tree** (vertical, 16 rooms), Wyrmroot, Twist 1 | 4.5 | 2.0 |
| R4 Crystal Expanse | Shiverton, **kart set-piece**, Palace of Frost (15 rooms), Crystallis, journal reveal | 4.5 | 2.0 |
| R5 Ashen Ruins | Cindralis, Library of Cinders, Last Sanctuary (14 rooms), Ashen Sentinel, Twist 2 | 4.0 | 1.0 |
| Finale — Hollow Deep | Kael fight, Hollow Frost ×2 phases, ending | 2.5 | — |
| Post-game | Dawnbreaker forge, Arena EX, remaining collectibles | — | 4.0 |
| **Total** | | **~23.5** | **~13.5 → 30 h cap via completionist path** |

Overworld: a classic **world map** (overworld tiles in the atlas) connects regions — retro touch, fast travel via crystal network after each relight.

---

# PART V — ASSET MAXIMIZATION MAP (every folder has a job)

| Asset folder (verified) | Where it lives in LITTLE EMBER |
|---|---|
| `Hero/` 5 colors × 16 anims | Pip + 5 cloaks; Kael = dark-tint color_5 ("a player-shaped enemy") |
| `ARPG/` 32×12×6 @96×128 | The 12 Flame Arts VFX; intro-splash art; boss "enrage" flashes |
| `Characters/Characters/chara_0–31` | Full 32-NPC cast across 6 settlements |
| `Characters/Animals/` | Wildlife per region identity: rabbits/birds Verdant, foxes Amber, penguins+cats Crystal, pigs farms; **Cinder** the fox; **Gembul** the pig (baby→adult) |
| `Characters/Farm/` | Hearthgarden hoe/shovel/water/walk |
| `Characters/Monsters/Monsters_01–05` | The 5 Wardens (bosses) — 4-dir, 8-frame walkers |
| `Characters/Monsters/bonus ×6` | The 6 Arena Elites (front-view stage) |
| `Battlers/` 57 | **Spirit Cards** collection (tarot frames + lore) |
| `Backgrounds/` 11 | **Storybook Pages**, chapter-title cards, title-screen art with live ember particles |
| `Environments` atlas 5 color themes | The 5 regions: green / pink / autumn / crystal / ash + post-game "first spring" blended state |
| Autotiles + animated (water/waterfall/lava) | Dungeons, shore, mine, lava below Hollow Deep |
| `beach_water_tiles` | Glimmer Shore |
| `gigantic_pack` | **The Grandfather Tree** signature dungeon |
| `Prefabs/` 571 | Set dressing per composition rules below; pots/barrels/crates = breakables; books = library; statues/columns = dead capital |
| `Prefabs_with_behavior/` | Tutorialized interactions: chests, push-blocks, secret grass |
| `Animations/` | Door/Chest/Trap/Switch = dungeon grammar; Cristal = save points; Kart = mine set-piece; Lamp/Fire = town warmth; Water/Lava = hazard identity |
| `Samples/` 8 scenes | Layout references for Emberholt (village), Millbrook (farm), Hanamura (marketplace), interiors (indoor) |
| `Scripts/` | PlayerMovement base, CharacterAppearance, SpriteSelectFrame for crops |
| FangAutoTile | All terrain painting (47-blob rules) |

---

# PART VI — ART DIRECTION ("beautiful by rule, not by luck")

## 1. The Two-State World — the game's signature art system
Every region is built and graded **twice**:
- **EXTINGUISHED:** desaturated −40%, hue shifted blue-gray, fog density up, long hard shadows from a low "dead sun," no particles but drifting ash/cold motes, music = solo instrument.
- **RELIT:** saturation +15%, warm 2D point lights on every lamp/torch/hearth, bloom on ember particles, region-specific particle weather returns (petals/leaves/snow-sparkle), NPCs outdoors, music = full ensemble.
The relight moment is a 6-second real-time grade sweep radiating from the Sanctuary — the player *watches the world heal because of them*. This is the screenshot that sells the game.

## 2. Color script (emotional arc)
| Region | Hue | Mood |
|---|---|---|
| Emberholt | warm amber | home |
| Verdant | green | hope |
| Sakura | pink | tender grief |
| Amber | orange | melancholy |
| Crystal | blue | lonely wonder |
| Cindralis | gray | silence |
| Hollow Deep | black + one warm dot | dread |
| Epilogue | gold | earned joy |

## 3. Lighting (URP 2D Light — Renderer2D already active)
Pip's lantern = flickering warm point light (radius = survival in Crystal/Hollow). Torch/lamp prefabs get light halos; crystals glow from within (Crystal clips); lava = emissive under-lighting; Grandfather Tree = vertical god-ray shafts through the trunk.

## 4. Composition laws (level-design checklist, enforced per screen)
1. **One landmark per screen** (a giant tree, a torii, a statue) — never two.
2. **Paths curve.** No straight road longer than 6 tiles; break sightlines with props.
3. **Odd clusters:** props in 1/3/5, never symmetric pairs unless it's a formal space (shrine, palace).
4. **Frame the player:** entrances framed by torii / columns / tree arches.
5. **60-30-10 color:** dominant ground 60%, secondary foliage 30%, accent (flowers, lamps) 10%.
6. **Max 3 ground textures per map**, blended only through autotile transitions.
7. **Water is a mirror:** shores always pair with something worth reflecting (blossoms, torii, lanterns).
8. **Prop storytelling:** every prop combo tells a micro-story — abandoned cart + spilled pots + child's doll = they left in a hurry; two chairs + one cold hearth = someone waits alone.

## 5. Juice spec
Hit-stop 60–90 ms · 2–4 px knockback · 1-frame white flash · ember burst on kill · screen shake only on boss hits · relight = the only full-screen effect, so it stays sacred.

## 6. UI art
Storybook style: hand-inked borders, flame motif on every frame, chapter cards use `Backgrounds` plates. Spirit Cards = tarot frames. Font: warm serif for story, clean pixel font for HUD. (All UI built from scratch — pack contains none.)

---

# PART VII — AUDIO DIRECTION (zero assets — must be produced)

- **Leitmotif:** "Little Ember theme" — music box + flute, 8 notes; appears in every track, incomplete until the credits where it resolves.
- **Per region:** Verdant = strings/guitar · Sakura = koto/bells · Amber = cello/harmonica · Crystal = celesta/wind · Cindralis = solo piano + tape hiss · Hollow Deep = sub-bass heartbeat.
- **Relight sting:** 6-second orchestral swell synced to the grade sweep.
- **SFX list:** flame crackle (3 layers by lantern tier), sword kit (8), block-push scrape, chest chime, card drop flutter, kart clatter, Warden voices (pitched animal recordings), UI chimes (4).

---

# PART VIII — PRODUCTION MILESTONES

| Milestone | Content | Exit criteria |
|---|---|---|
| **M0 Foundation** (2 wk) | Movement/combat/animator generation from sheets, input set to Both, 2D light rig | Pip walks Emberholt greybox |
| **M1 Vertical slice** (6 wk) | Emberholt + Millbrook + Mossdeep + Bramblemaw + Two-State relight demo | 30-min demo, one full relight moment |
| **M2 World half** (8 wk) | R2 + R3 complete, Arena, Skybow, Grandfather Tree | 3 regions relightable |
| **M3 World full** (8 wk) | R4 + R5, kart set-piece, all twists staged | Main path playable end-to-end |
| **M4 Finale & polish** (6 wk) | Hollow Deep, ending, cards/garden/cooking, audio pass, juice pass | 20-h main path verified by playtest |
| **M5 Ship prep** (4 wk) | Balance, save-stress, credits, store page | — |

**Immediate next step:** M0 — generate Animator controllers from the sliced sheets by script (16 groups × 4 dirs × 5 colors is not hand-work), build the Emberholt greybox, and set `Active Input Handling = Both`.
