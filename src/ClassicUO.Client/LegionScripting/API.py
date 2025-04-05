JournalEntries = None
Backpack = None
Player = None
Random = None

class ScanType:
    Hostile = 0
    Party = 1
    Followers = 2
    Objects = 3
    Mobiles = 4

class Notoriety:
    Unknown = 1
    Innocent = 1
    Ally = 1
    Gray = 1
    Criminal = 1
    Enemy = 1
    Murderer = 1
    Invulnerable = 1
def Attack(serial: int):
    pass

def BandageSelf():
    pass

def ClearLeftHand():
    pass

def ClearRightHand():
    pass

def ClickObject(serial: int):
    pass

def UseObject(serial: int, skipQueue: bool):
    pass

def Contents(serial: int):
    pass

def ContextMenu(serial: int, entry: int):
    pass

def EquipItem(serial: int):
    pass

def MoveItem(serial: int, destination: int, amt: int, x: int, y: int):
    pass

def MoveItemOffset(serial: int, amt: int, x: int, y: int, z: int):
    pass

def UseSkill(skillName: str):
    pass

def CastSpell(spellName: str):
    pass

def BuffExists(buffName: str):
    pass

def SysMsg(message: str, hue: int):
    pass

def Msg(message: str):
    pass

def HeadMsg(message: str, serial: int):
    pass

def PartyMsg(message: str):
    pass

def GuildMsg(message: str):
    pass

def AllyMsg(message: str):
    pass

def WhisperMsg(message: str):
    pass

def YellMsg(message: str):
    pass

def EmoteMsg(message: str):
    pass

def FindItem(serial: int):
    pass

def FindType(graphic: int, container: int, range: int, hue: int, minamount: int):
    pass

def FindTypeAll(graphic: int, container: int, range: int, hue: int, minamount: int):
    pass

def FindLayer(layer: str, serial: int):
    pass

def ItemsInContainer(container: int):
    pass

def UseType(graphic: int, hue: int, container: int, skipQueue: bool):
    pass

def CreateCooldownBar(seconds: float, text: str, hue: int):
    pass

def IgnoreObject(serial: int):
    pass

def ClearIgnoreList():
    pass

def OnIgnoreList(serial: int):
    pass

def Pathfind(x: int, y: int, z: int, distance: int):
    pass

def Pathfind(entity: int, distance: int):
    pass

def Pathfinding():
    pass

def CancelPathfinding():
    pass

def AutoFollow(mobile: int):
    pass

def CancelAutoFollow():
    pass

def Run(direction: str):
    pass

def Walk(direction: str):
    pass

def Turn(direction: str):
    pass

def Rename(serial: int, name: str):
    pass

def Dismount():
    pass

def Mount(serial: int):
    pass

def WaitForTarget(targetType: str, timeout: float):
    pass

def Target(serial: int):
    pass

def Target(x: int, y: int, z: Any, graphic: int):
    pass

def RequestTarget(timeout: float):
    pass

def TargetSelf():
    pass

def TargetLandRel(xOffset: int, yOffset: int):
    pass

def TargetTileRel(xOffset: int, yOffset: int, graphic: int):
    pass

def CancelTarget():
    pass

def SetSkillLock(skill: str, up_down_locked: str):
    pass

def Logout():
    pass

def ItemNameAndProps(serial: int, wait: bool, timeout: int):
    pass

def HasGump(ID: int):
    pass

def ReplyGump(button: int, gump: int):
    pass

def CloseGump(ID: int):
    pass

def GumpContains(text: str, ID: int):
    pass

def ToggleFly():
    pass

def ToggleAbility(ability: str):
    pass

def PrimaryAbilityActive():
    pass

def SecondaryAbilityActive():
    pass

def InJournal(msg: str):
    pass

def InJournalAny(msgs: Any):
    pass

def ClearJournal():
    pass

def Pause(seconds: float):
    pass

def Stop():
    pass

def ToggleAutoLoot():
    pass

def Virtue(virtue: str):
    pass

def NearestEntity(scanType: Any, maxDistance: int):
    pass

def NearestMobile(notoriety: Any, maxDistance: int):
    pass

def NearestCorpse(distance: int):
    pass

def FindMobile(serial: int):
    pass

def GetAllMobiles():
    pass

def GetTile(x: int, y: int):
    pass

def CreateGump(acceptMouseInput: bool, canMove: bool):
    pass

def AddGump(g: Any):
    pass

def CreateGumpCheckbox(text: str, hue: int):
    pass

def CreateGumpLabel(text: str, hue: int):
    pass

def CreateGumpColorBox(opacity: float, color: str):
    pass

def CreateGumpItemPic(graphic: int, width: int, height: int):
    pass

def CreateGumpButton(text: str, hue: int, normal: int, pressed: int, hover: int):
    pass

def CreateGumpRadioButton(text: str, group: int, inactive: int, active: int, hue: int):
    pass

def GetSkill(skill: str):
    pass

