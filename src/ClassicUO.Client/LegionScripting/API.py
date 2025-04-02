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
def Attack(serial):
    pass

def BandageSelf():
    pass

def ClearLeftHand():
    pass

def ClearRightHand():
    pass

def ClickObject(serial):
    pass

def UseObject(serial, skipQueue):
    pass

def Contents(serial):
    pass

def ContextMenu(serial, entry):
    pass

def EquipItem(serial):
    pass

def MoveItem(serial, destination, amt, x, y):
    pass

def MoveItemOffset(serial, amt, x, y, z):
    pass

def UseSkill(skillName):
    pass

def CastSpell(spellName):
    pass

def BuffExists(buffName):
    pass

def SysMsg(message, hue):
    pass

def Msg(message):
    pass

def HeadMsg(message, serial):
    pass

def PartyMsg(message):
    pass

def GuildMsg(message):
    pass

def AllyMsg(message):
    pass

def WhisperMsg(message):
    pass

def YellMsg(message):
    pass

def EmoteMsg(message):
    pass

def FindItem(serial):
    pass

def FindType(graphic, container, range, hue, minamount):
    pass

def FindTypeAll(graphic, container, range, hue, minamount):
    pass

def FindLayer(layer, serial):
    pass

def ItemsInContainer(container):
    pass

def UseType(graphic, hue, container, skipQueue):
    pass

def CreateCooldownBar(seconds, text, hue):
    pass

def IgnoreObject(serial):
    pass

def ClearIgnoreList():
    pass

def OnIgnoreList(serial):
    pass

def Pathfind(x, y, z, distance):
    pass

def Pathfind(entity, distance):
    pass

def Pathfinding():
    pass

def CancelPathfinding():
    pass

def AutoFollow(mobile):
    pass

def CancelAutoFollow():
    pass

def Run(direction):
    pass

def Walk(direction):
    pass

def Turn(direction):
    pass

def Rename(serial, name):
    pass

def Dismount():
    pass

def Mount(serial):
    pass

def WaitForTarget(targetType, timeout):
    pass

def Target(serial):
    pass

def Target(x, y, z, graphic):
    pass

def RequestTarget(timeout):
    pass

def TargetSelf():
    pass

def TargetLandRel(xOffset, yOffset):
    pass

def TargetTileRel(xOffset, yOffset, graphic):
    pass

def CancelTarget():
    pass

def SetSkillLock(skill, up_down_locked):
    pass

def Logout():
    pass

def ItemNameAndProps(serial, wait, timeout):
    pass

def HasGump(ID):
    pass

def ReplyGump(button, gump):
    pass

def CloseGump(ID):
    pass

def GumpContains(text, ID):
    pass

def ToggleFly():
    pass

def ToggleAbility(ability):
    pass

def PrimaryAbilityActive():
    pass

def SecondaryAbilityActive():
    pass

def InJournal(msg):
    pass

def InJournalAny(msgs):
    pass

def ClearJournal():
    pass

def Pause(seconds):
    pass

def Stop():
    pass

def ToggleAutoLoot():
    pass

def Virtue(virtue):
    pass

def NearestEntity(scanType, maxDistance):
    pass

def NearestMobile(notoriety, maxDistance):
    pass

def NearestCorpse(distance):
    pass

def FindMobile(serial):
    pass

def GetAllMobiles():
    pass

def GetTile(x, y):
    pass

def CreateGump(acceptMouseInput, canMove):
    pass

def AddGump(g):
    pass

def CreateGumpCheckbox(text, hue):
    pass

def CreateGumpLabel(text, hue):
    pass

def CreateGumpColorBox(opacity, color):
    pass

def CreateGumpItemPic(graphic, width, height):
    pass

def CreateGumpButton(text, hue, normal, pressed, hover):
    pass

def CreateGumpRadioButton(text, group, inactive, active, hue):
    pass

def GetSkill(skill):
    pass

