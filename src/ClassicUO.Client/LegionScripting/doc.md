This is automatically generated documentation for the Python API scripting.  
All methods, properties, enums, etc need to pre prefaced with `API.` for example: `API.Msg("An example")`.  
If you download the [API.py](API.py) file, put it in the same folder as your python scripts and add `import API` to your script, that will enable some mild form of autocomplete in an editor like VS Code.  


This was generated on `4/4/2025`.
# API  

## Class Description
Python scripting access point

## Properties
- **JournalEntries** (*ConcurrentQueue<JournalEntry>*)
- **Backpack** (*Item*)
  - Get the players backpack
- **Player** (*PlayerMobile*)
  - Returns the player character
- **Random** (*Random*)
  - Can be used for random numbers.
         `API.Random.Next(1, 100)` will return a number between 1 and 100.
         `API.Random.Next(100)` will return a number between 0 and 100.

## Enums
### ScanType

**Values:**
- Hostile
- Party
- Followers
- Objects
- Mobiles

### Notoriety

**Values:**
- Unknown
- Innocent
- Ally
- Gray
- Criminal
- Enemy
- Murderer
- Invulnerable


## Methods

<details>
<summary><h3>Attack(serial)</h3></summary>

Attack a mobile  
 Example:  
 ```py
 enemy = API.NearestEntity([API.Notoriety.Gray, API.Notoriety.Criminal], 7)
 if enemy:
   API.Attack(enemy)
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
#### Does not return anything

</details>

***


<details>
<summary><h3>BandageSelf()</h3></summary>

Attempt to bandage yourself. Older clients this will not work, you will need to find a bandage, use it, and target yourself.  
 Example:  
 ```py
 if player.HitsMax - player.Hits > 10 or player.IsPoisoned:
   if API.BandageSelf():
     API.CreateCooldownBar(delay, "Bandaging...", 21)
     API.Pause(8)
   else:
     API.SysMsg("WARNING: No bandages!", 32)
     break
 ```

#### Return Type: *bool*

</details>

***


<details>
<summary><h3>ClearLeftHand()</h3></summary>

If you have an item in your left hand, move it to your backpack  
 Example:  
 ```py
 leftHand = API.ClearLeftHand()
 if leftHand:
   API.SysMsg("Cleared left hand: " + leftHand.Name)
 ```

#### Return Type: *Item*

</details>

***


<details>
<summary><h3>ClearRightHand()</h3></summary>

If you have an item in your right hand, move it to your backpack  
 Example:  
 ```py  
 rightHand = API.ClearRightHand()
 if rightHand:
   API.SysMsg("Cleared right hand: " + rightHand.Name)
  ```

#### Return Type: *Item*

</details>

***


<details>
<summary><h3>ClickObject(serial)</h3></summary>

Single click an object  
 Example:  
 ```py
 API.ClickObject(API.Player)
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No | Serial, or item/mobile reference |
#### Does not return anything

</details>

***


<details>
<summary><h3>UseObject(serial, skipQueue)</h3></summary>

Attempt to use(double click) an object.  
 Example:  
 ```py
 API.UseObject(API.Backpack)
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No | The serial |
| skipQueue | bool | Yes | Defaults true, set to false to use a double click queue |
#### Does not return anything

</details>

***


<details>
<summary><h3>Contents(serial)</h3></summary>

Get an item count for the contents of a container  
 Example:  
 ```py
 count = API.Contents(API.Backpack)
 if count > 0:
   API.SysMsg(f"You have {count} items in your backpack")
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
#### Return Type: *int*

</details>

***


<details>
<summary><h3>ContextMenu(serial, entry)</h3></summary>

Send a context menu(right click menu) response.  
 This does not open the menu, you do not need to open the menu first. This handles both in one action.  
 Example:  
 ```py
 API.ContextMenu(API.Player, 1)
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
| entry | ushort | No | Entries start at 0, the top entry will be 0, then 1, 2, etc. (Usually) |
#### Does not return anything

</details>

***


<details>
<summary><h3>EquipItem(serial)</h3></summary>

Attempt to equip an item. Layer is automatically detected.
 Example:  
 ```py
 lefthand = API.ClearLeftHand()
 API.Pause(2)
 API.EquipItem(lefthand)
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
#### Does not return anything

</details>

***


<details>
<summary><h3>MoveItem(serial, destination, amt, x, y)</h3></summary>

Move an item to another container.  
 Use x, and y if you don't want items stacking in the desination container.  
 Example:  
 ```py
 items = API.ItemsInContainer(API.Backpack)

 API.SysMsg("Target your fish barrel", 32)
 barrel = API.RequestTarget()


 if len(items) > 0 and barrel:
     for item in items:
         data = API.ItemNameAndProps(item)
         if data and "An Exotic Fish" in data:
             API.MoveItem(item, barrel)
             API.Pause(0.75)
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
| destination | uint | No |  |
| amt | int | Yes | Amount to move |
| x | int | Yes | X coordinate inside a container |
| y | int | Yes | Y coordinate inside a container |
#### Does not return anything

</details>

***


<details>
<summary><h3>MoveItemOffset(serial, amt, x, y, z)</h3></summary>

Move an item to the ground near you.  
 Example:  
 ```py
 items = API.ItemsInContainer(API.Backpack)
 for item in items:
   API.MoveItemOffset(item, 0, 1, 0, 0)
   API.Pause(0.75)
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
| amt | int | Yes | 0 to grab entire stack |
| x | int | Yes | Offset from your location |
| y | int | Yes | Offset from your location |
| z | int | Yes | Offset from your location |
#### Does not return anything

</details>

***


<details>
<summary><h3>UseSkill(skillName)</h3></summary>

Use a skill.  
 Example:  
 ```py
 API.UseSkill("Hiding")
 API.Pause(11)
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| skillName | string | No | Can be a partial match. Will match the first skill containing this text. |
#### Does not return anything

</details>

***


<details>
<summary><h3>CastSpell(spellName)</h3></summary>

Attempt to cast a spell by its name.  
 Example:  
 ```py
 API.CastSpell("Fireball")
 API.WaitForTarget()
 API.Target(API.Player)
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| spellName | string | No | This can be a partial match. Fireba will cast Fireball. |
#### Does not return anything

</details>

***


<details>
<summary><h3>BuffExists(buffName)</h3></summary>

Check if a buff is active.  
 Example:  
 ```py
 if API.BuffExists("Bless"):
   API.SysMsg("You are blessed!")
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| buffName | string | No | The name/title of the buff |
#### Return Type: *bool*

</details>

***


<details>
<summary><h3>SysMsg(message, hue)</h3></summary>

Show a system message(Left side of screen).  
 Example:  
 ```py
 API.SysMsg("Script started!")
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| message | string | No | Message |
| hue | ushort | Yes | Color of the message |
#### Does not return anything

</details>

***


<details>
<summary><h3>Msg(message)</h3></summary>

Say a message outloud.  
 Example:  
 ```py
 API.Say("Hello friend!")
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| message | string | No | The message to say |
#### Does not return anything

</details>

***


<details>
<summary><h3>HeadMsg(message, serial)</h3></summary>

Show a message above a mobile or item, this is only visible to you.  
 Example:  
 ```py
 API.HeadMsg("Only I can see this!", API.Player)
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| message | string | No | The message |
| serial | uint | No | The item or mobile |
#### Does not return anything

</details>

***


<details>
<summary><h3>PartyMsg(message)</h3></summary>

Send a message to your party.  
 Example:  
 ```py
 API.PartyMsg("The raid begins in 30 second! Wait... we don't have raids, wrong game..")
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| message | string | No | The message |
#### Does not return anything

</details>

***


<details>
<summary><h3>GuildMsg(message)</h3></summary>

Send your guild a message.  
 Example:  
 ```py
 API.GuildMsg("Hey guildies, just restocked my vendor, fresh valorite suits available!")
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| message | string | No |  |
#### Does not return anything

</details>

***


<details>
<summary><h3>AllyMsg(message)</h3></summary>

Send a message to your alliance.  
 Example:  
 ```py
 API.AllyMsg("Hey allies, just restocked my vendor, fresh valorite suits available!")
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| message | string | No |  |
#### Does not return anything

</details>

***


<details>
<summary><h3>WhisperMsg(message)</h3></summary>

Whisper a message.  
 Example:  
 ```py
 API.WhisperMsg("Psst, bet you didn't see me here..")
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| message | string | No |  |
#### Does not return anything

</details>

***


<details>
<summary><h3>YellMsg(message)</h3></summary>

Yell a message.  
 Example:  
 ```py
 API.YellMsg("Vendor restocked, get your fresh feathers!")
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| message | string | No |  |
#### Does not return anything

</details>

***


<details>
<summary><h3>EmoteMsg(message)</h3></summary>

Emote a message.  
 Example:  
 ```py
 API.EmoteMsg("laughing")
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| message | string | No |  |
#### Does not return anything

</details>

***


<details>
<summary><h3>FindItem(serial)</h3></summary>

Try to get an item by its serial.  
 Example:  
 ```py
 donkey = API.RequestTarget()
 item = API.FindItem(donkey)
 if item:
   API.SysMsg("Found the donkey!")
   API.UseObject(item)
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No | The serial |
#### Return Type: *Item*

</details>

***


<details>
<summary><h3>FindType(graphic, container, range, hue, minamount)</h3></summary>

Attempt to find an item by type(graphic).  
 Example:  
 ```py
 item = API.FindType(0x0EED, API.Backpack)
 if item:
   API.SysMsg("Found the item!")
   API.UseObject(item)
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| graphic | uint | No | Graphic/Type of item to find |
| container | uint | Yes | Container to search |
| range | ushort | Yes | Max range of item |
| hue | ushort | Yes | Hue of item |
| minamount | ushort | Yes | Only match if item stack is at least this much |
#### Return Type: *Item*

</details>

***


<details>
<summary><h3>FindTypeAll(graphic, container, range, hue, minamount)</h3></summary>

Return a list of items matching the parameters set.  
 Example:  
 ```py
 items = API.FindTypeAll(0x0EED, API.Backpack)
 if items:
   API.SysMsg("Found " + str(len(items)) + " items!")
 ```

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| graphic | uint | No | Graphic/Type of item to find |
| container | uint | Yes | Container to search |
| range | ushort | Yes | Max range of item(if on ground) |
| hue | ushort | Yes | Hue of item |
| minamount | ushort | Yes | Only match if item stack is at least this much |
#### Return Type: *Item[]*

</details>

***


<details>
<summary><h3>FindLayer(layer, serial)</h3></summary>

Attempt to find an item on a layer

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| layer | string | No | The layer to check, see https://github.com/bittiez/TazUO/blob/main/src/ClassicUO.Client/Game/Data/Layers.cs |
| serial | uint | Yes | Optional, if not set it will check yourself, otherwise it will check the mobile requested |
#### Return Type: *Item*

</details>

***


<details>
<summary><h3>ItemsInContainer(container)</h3></summary>

Get all items in a container

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| container | uint | No |  |
#### Return Type: *Item[]*

</details>

***


<details>
<summary><h3>UseType(graphic, hue, container, skipQueue)</h3></summary>

Attempt to use the first item found by graphic(type)

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| graphic | uint | No | Graphic/Type |
| hue | ushort | Yes | Hue of item |
| container | uint | Yes | Parent container |
| skipQueue | bool | Yes | Defaults to true, set to false to queue the double click |
#### Does not return anything

</details>

***


<details>
<summary><h3>CreateCooldownBar(seconds, text, hue)</h3></summary>

Create a cooldown bar

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| seconds | double | No | Duration in seconds for the cooldown bar |
| text | string | No | Text on the cooldown bar |
| hue | ushort | No | Hue to color the cooldown bar |
#### Does not return anything

</details>

***


<details>
<summary><h3>IgnoreObject(serial)</h3></summary>

Adds an item or mobile to your ignore list.

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No | The item/mobile serial |
#### Does not return anything

</details>

***


<details>
<summary><h3>ClearIgnoreList()</h3></summary>

Clears the ignore list

#### Does not return anything

</details>

***


<details>
<summary><h3>OnIgnoreList(serial)</h3></summary>

Check if a serial is on the ignore list

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
#### Return Type: *bool*

</details>

***


<details>
<summary><h3>Pathfind(x, y, z, distance)</h3></summary>

Attempt to pathfind to a location

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| x | int | No |  |
| y | int | No |  |
| z | int | Yes |  |
| distance | int | Yes | Distance away from goal to stop. |
#### Does not return anything

</details>

***


<details>
<summary><h3>Pathfind(entity, distance)</h3></summary>

Attempt to pathfind to a mobile or item

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| entity | uint | No | The mobile or item |
| distance | int | Yes | Distance to stop from goal |
#### Does not return anything

</details>

***


<details>
<summary><h3>Pathfinding()</h3></summary>

Check if you are already pathfinding.

#### Return Type: *bool*

</details>

***


<details>
<summary><h3>CancelPathfinding()</h3></summary>

Cancel pathfinding.

#### Does not return anything

</details>

***


<details>
<summary><h3>AutoFollow(mobile)</h3></summary>

Automatically follow a mobile

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| mobile | uint | No | The mobile |
#### Does not return anything

</details>

***


<details>
<summary><h3>CancelAutoFollow()</h3></summary>

Cancel auto follow mode

#### Does not return anything

</details>

***


<details>
<summary><h3>Run(direction)</h3></summary>

Run in a direction

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| direction | string | No | north/northeast/south/west/etc |
#### Does not return anything

</details>

***


<details>
<summary><h3>Walk(direction)</h3></summary>

Walk in a direction

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| direction | string | No | north/northeast/south/west/etc |
#### Does not return anything

</details>

***


<details>
<summary><h3>Turn(direction)</h3></summary>

Turn your character a specific direction

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| direction | string | No | north, northeast, etc |
#### Does not return anything

</details>

***


<details>
<summary><h3>Rename(serial, name)</h3></summary>

Attempt to rename something like a pet

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No | Serial of the mobile to rename |
| name | string | No | The new name |
#### Does not return anything

</details>

***


<details>
<summary><h3>Dismount()</h3></summary>

Attempt to dismount if mounted

#### Return Type: *uint*

</details>

***


<details>
<summary><h3>Mount(serial)</h3></summary>

Attempt to mount(double click)

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
#### Does not return anything

</details>

***


<details>
<summary><h3>WaitForTarget(targetType, timeout)</h3></summary>

Wait for a target cursor

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| targetType | string | Yes | Neutral/Harmful/Beneficial |
| timeout | double | Yes | Duration in seconds to wait |
#### Return Type: *bool*

</details>

***


<details>
<summary><h3>Target(serial)</h3></summary>

Target an item or mobile

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No | Serial of the item/mobile to target |
#### Does not return anything

</details>

***


<details>
<summary><h3>Target(x, y, z, graphic)</h3></summary>

Target a location. Include graphic if targeting a static.

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| x | ushort | No |  |
| y | ushort | No |  |
| z | short | No |  |
| graphic | ushort | Yes |  |
#### Does not return anything

</details>

***


<details>
<summary><h3>RequestTarget(timeout)</h3></summary>

Request the player to target something

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| timeout | double | Yes |  |
#### Return Type: *uint*

</details>

***


<details>
<summary><h3>TargetSelf()</h3></summary>

Target yourself

#### Does not return anything

</details>

***


<details>
<summary><h3>TargetLandRel(xOffset, yOffset)</h3></summary>

Target a land tile

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| xOffset | int | No | X from your position |
| yOffset | int | No | Y from your position |
#### Does not return anything

</details>

***


<details>
<summary><h3>TargetTileRel(xOffset, yOffset, graphic)</h3></summary>

Target a tile relative to your location

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| xOffset | int | No | X Offset from your position |
| yOffset | int | No | Y Offset from your position |
| graphic | uint | Yes | Optional graphic, will only target if tile matches this |
#### Does not return anything

</details>

***


<details>
<summary><h3>CancelTarget()</h3></summary>

Cancel targeting

#### Does not return anything

</details>

***


<details>
<summary><h3>SetSkillLock(skill, up_down_locked)</h3></summary>

Set a skills lock status

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| skill | string | No | The skill name, can be partia; |
| up_down_locked | string | No | up/down/locked |
#### Does not return anything

</details>

***


<details>
<summary><h3>Logout()</h3></summary>

Logout of the game

#### Does not return anything

</details>

***


<details>
<summary><h3>ItemNameAndProps(serial, wait, timeout)</h3></summary>

Gets item name and properties

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
| wait | bool | Yes | True or false to wait for name and props |
| timeout | int | Yes | Timeout in seconds |
#### Return Type: *string*

</details>

***


<details>
<summary><h3>HasGump(ID)</h3></summary>

Check if a player has a server gump

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| ID | uint | Yes | Skip to check if player has any gump from server. |
#### Return Type: *uint*

</details>

***


<details>
<summary><h3>ReplyGump(button, gump)</h3></summary>

Reply to a gump

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| button | int | No | Button ID |
| gump | uint | Yes | Gump ID, leave blank to reply to last gump |
#### Return Type: *bool*

</details>

***


<details>
<summary><h3>CloseGump(ID)</h3></summary>

Close the last gump open, or a specific gump

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| ID | uint | Yes | Gump ID |
#### Does not return anything

</details>

***


<details>
<summary><h3>GumpContains(text, ID)</h3></summary>

Check if a gump contains a specific text.

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| text | string | No | Can be regex if you start with $, otherwise it's just regular search. Case Sensitive. |
| ID | uint | Yes | Gump ID, blank to use the last gump. |
#### Return Type: *bool*

</details>

***


<details>
<summary><h3>ToggleFly()</h3></summary>

Toggle flying if you are a gargoyle

#### Does not return anything

</details>

***


<details>
<summary><h3>ToggleAbility(ability)</h3></summary>

Toggle an ability

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| ability | string | No | primary/secondary/stun/disarm |
#### Does not return anything

</details>

***


<details>
<summary><h3>PrimaryAbilityActive()</h3></summary>

Check if your primary ability is active

#### Return Type: *bool*

</details>

***


<details>
<summary><h3>SecondaryAbilityActive()</h3></summary>

Check if your secondary ability is active

#### Return Type: *bool*

</details>

***


<details>
<summary><h3>InJournal(msg)</h3></summary>

Check if your journal contains a message

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| msg | string | No | The message to check for |
#### Return Type: *bool*

</details>

***


<details>
<summary><h3>InJournalAny(msgs)</h3></summary>

Check if the journal contains *any* of the strings in this list

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| msgs | string[] | No |  |
#### Return Type: *bool*

</details>

***


<details>
<summary><h3>ClearJournal()</h3></summary>

Clear your journal(This is specific for each script)

#### Does not return anything

</details>

***


<details>
<summary><h3>Pause(seconds)</h3></summary>

Pause the script

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| seconds | double | No |  |
#### Does not return anything

</details>

***


<details>
<summary><h3>Stop()</h3></summary>

Stops the current script

#### Does not return anything

</details>

***


<details>
<summary><h3>ToggleAutoLoot()</h3></summary>

Toggle autolooting on or off

#### Does not return anything

</details>

***


<details>
<summary><h3>Virtue(virtue)</h3></summary>

Use a virtue

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| virtue | string | No | honor/sacrifice/valor |
#### Does not return anything

</details>

***


<details>
<summary><h3>NearestEntity(scanType, maxDistance)</h3></summary>

Find the nearest item/mobile based on scan type

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| scanType | ScanType | No |  |
| maxDistance | int | Yes |  |
#### Return Type: *Entity*

</details>

***


<details>
<summary><h3>NearestMobile(notoriety, maxDistance)</h3></summary>

Get the nearest mobile by Notoriety

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| notoriety | Notoriety[] | No | List of notorieties |
| maxDistance | int | Yes |  |
#### Return Type: *Mobile*

</details>

***


<details>
<summary><h3>NearestCorpse(distance)</h3></summary>

Get the nearest corpse within a distance

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| distance | int | Yes |  |
#### Return Type: *Item*

</details>

***


<details>
<summary><h3>FindMobile(serial)</h3></summary>

Get a mobile from its serial

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| serial | uint | No |  |
#### Return Type: *Mobile*

</details>

***


<details>
<summary><h3>GetAllMobiles()</h3></summary>

Return a list of all mobiles the client is aware of.

#### Return Type: *Mobile[]*

</details>

***


<details>
<summary><h3>GetTile(x, y)</h3></summary>

Get the tile at a location

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| x | int | No |  |
| y | int | No |  |
#### Return Type: *GameObject*

</details>

***


<details>
<summary><h3>CreateGump(acceptMouseInput, canMove)</h3></summary>

Get a blank gump

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| acceptMouseInput | bool | Yes | Allow clicking the gump |
| canMove | bool | Yes | Allow the play to move this gump |
#### Return Type: *Gump*

</details>

***


<details>
<summary><h3>AddGump(g)</h3></summary>

Add a gump to the players screen

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| g | Gump | No | The gump to add |
#### Does not return anything

</details>

***


<details>
<summary><h3>CreateGumpCheckbox(text, hue)</h3></summary>

Create a checkbox for gumps

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| text | string | Yes | Optional text label |
| hue | ushort | Yes | Optional hue |
#### Return Type: *Checkbox*

</details>

***


<details>
<summary><h3>CreateGumpLabel(text, hue)</h3></summary>

Create a label for a gump

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| text | string | No | The text |
| hue | ushort | Yes | The hue of the text |
#### Return Type: *Label*

</details>

***


<details>
<summary><h3>CreateGumpColorBox(opacity, color)</h3></summary>

Get a transparent color box for gumps

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| opacity | float | Yes | 0.5 = 50% |
| color | string | Yes | Html color code like #000000 |
#### Return Type: *AlphaBlendControl*

</details>

***


<details>
<summary><h3>CreateGumpItemPic(graphic, width, height)</h3></summary>

Create a picture of an item

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| graphic | uint | No |  |
| width | int | No |  |
| height | int | No |  |
#### Return Type: *ResizableStaticPic*

</details>

***


<details>
<summary><h3>CreateGumpButton(text, hue, normal, pressed, hover)</h3></summary>

Create a button for gumps

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| text | string | Yes |  |
| hue | ushort | Yes |  |
| normal | ushort | Yes | Graphic when not clicked or hovering |
| pressed | ushort | Yes | Graphic when pressed |
| hover | ushort | Yes | Graphic on hover |
#### Return Type: *Button*

</details>

***


<details>
<summary><h3>CreateGumpRadioButton(text, group, inactive, active, hue)</h3></summary>

Create a radio button for gumps, use group numbers to only allow one item to be checked at a time

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| text | string | Yes | Optional text |
| group | int | Yes | Group ID |
| inactive | ushort | Yes | Unchecked graphic |
| active | ushort | Yes | Checked graphic |
| hue | ushort | Yes | Text color |
#### Return Type: *RadioButton*

</details>

***


<details>
<summary><h3>GetSkill(skill)</h3></summary>

Get a skill from the player. See the Skill class for what properties are available: https://github.com/bittiez/TazUO/blob/main/src/ClassicUO.Client/Game/Data/Skill.cs

**Parameters**  
| Name | Type | Optional | Description |
| --- | --- | --- | --- |
| skill | string | No | Skill name, case sensitive |
#### Return Type: *Skill*

</details>

***

