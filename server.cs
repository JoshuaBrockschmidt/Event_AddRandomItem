registerOutputEvent("fxDTSBrick", "z_fakeEvent_addRandomItem", "int 0 9 0", 0);
registerOutputEvent("fxDTSBrick", "setRandomItem", "paintColor 0", 1);
registerOutputEvent("Player", "AddRandomItem", "paintColor 0" TAB "bool", 1);

datablock fxDTSBrickData(BrickRandomItemSpawnData : Brick2x2fData)
{
	category = "Special";
	subCategory = "Interactive";
	uiName = "Random Item Spawn";
};

// \cNUMBER colors don't work properly for the below variables. Don't do it.
$AddRandomItem::Rarity0 = "<color:969696>Ubiquitous";
$AddRandomItem::Rarity1 = "<color:D6D6Dy>Familiar";
$AddRandomItem::Rarity2 = "<color:F5F5F5>Common";
$AddRandomItem::Rarity3 = "<color:FFFC99>Usual";
$AddRandomItem::Rarity4 = "<color:FFFF00>Unusual";
$AddRandomItem::Rarity5 = "<color:824100>Uncommon";
$AddRandomItem::Rarity6 = "<color:EB7300>Rare";
$AddRandomItem::Rarity7 = "<color:00FFFF>Very Rare";
$AddRandomItem::Rarity8 = "<color:A5FFFF>Epic";
$AddRandomItem::Rarity9 = "<color:DC19D7>Legendary";

$AddRandomItem::ShapeNameDist = 25;

exec("./support.cs");

function fxDTSBrick::setRandomItem(%brick, %colorID)
{
	if(!isObject(%simSet = "addRandomItem_Cat" @ %colorID) || !%simSet.getCount())
	{
		return;
	}

	if(!isObject(%item = getRandomItemColorBased(%colorID)))
	{
		return;
	}

	%brick.setItem(%item);
}

function player::addRandomItem(%player, %colorID, %noDoMsg, %client)
{
	if(!isObject(%simSet = "addRandomItem_Cat" @ %colorID) || !%simSet.getCount())
	{
		return;
	}

	%count = %player.getDataBlock().maxTools;

	for(%i = 0; %i < %count; %i++)
	{
		if(!isObject(%player.tool[%i]))
		{
			%freeSlot = %i;
			break;
		}
	}

	if(%freeSlot $= "")
	{
		return;
	}

	%item = getRandomItemColorBased(%colorID);

	messageClient(%client, 'MsgItemPickup', "", %freeSlot, %item, 1);
	%player.tool[%freeSlot] = %item;

	if(%item.className $= "Weapon")
	{
		%player.weaponCount++;
	}

	if(!%noDoMsg)
	{
		%colorCat = $AddRandomItem::ColorName[%colorID] !$= "" ? $AddRandomItem::ColorName[%colorID] : getField(getColorIDTableCat(%colorID), 0) SPC "-" SPC getField(getColorIDTableCat(%colorID), 1);

		cancel(%client.addRandomItem_msgSched);

		%newLine = %client.addRandomItem_msg $= "" ? "" : "<br>";

		%client.addRandomItem_msg = %client.addRandomItem_msg @ %newLine @ "\c6+<color:" @ addRandomItem_rgbToHex(getColorIDTable(%colorID)) @ ">" @ %item.uiName SPC "\c6[<color:" @ addRandomItem_rgbToHex(getColorIDTable(%colorID)) @ ">" @ %colorCat @ "\c6," SPC $AddRandomItem::Rarity[%chosenRarity] @ "\c6]";

		%client.addRandomItem_msgSched = schedule(25, 0, eval, "commandToClient(" @ %client @ ", \'centerPrint\'," SPC %client @ ".addRandomItem_msg, 3);" SPC %client @ ".addRandomItem_msg = \"\";");
	}
}

function getRandomItemColorBased(%colorID)
{
	if(!isObject(%simSet = "addRandomItem_Cat" @ %colorID) || !%simSet.getCount())
	{
		return -1;
	}

	//Break the category of items up into rarities. Pick a rarity. Give the player a random item from that rarity.

	%count = %simSet.getCount();

	for(%i = 0; %i < %count; %i++)
	{
		%brick = %simSet.getObject(%i);

		if(!isObject(%item = %brick.item))
		{
			continue;
		}

		%brick.addRandomItem_rarity = mClampF(mFloor(%brick.addRandomItem_rarity), 0, 9);

		if(%itemRarityCount[%brick.addRandomItem_rarity] $= "")
		{
			%itemRarityCount[%brick.addRandomItem_rarity] = 0;
		}

		%itemRarity[%brick.addRandomItem_rarity, %itemRarityCount[%brick.addRandomItem_rarity]] = %brick;
		%itemRarityCount[%brick.addRandomItem_rarity]++;
	}

	//We've just broken them up into groups of rarities.

	for(%i = 0; %i < 10; %i++)
	{
		if(%itemRarityCount[%i])
		{
			%string = %string SPC %i;
		}
	}

	//We've not just compiled all of the rarities with items in them into a list.

	%string = trim(%string);

	if(%string $= "")
	{
		return;
	}

	%count = getWordCount(%string);

	for(%i = 0; %i < %count; %i++)
	{
		%word = getWord(%string, %i);

		%chances[%word] = 10 - %word;
		%totalChances += %chances[%word];
	}

	//We've just given each rarity a number of chances to get chosen.
	//The percentage of each rarity being chosen is (%chances[Rarity Num] / 55) * 100.

	%rand = getRandom(1, %totalChances);
	%count = getWordCount(%string);

	for(%i = 0; %i < %count; %i++)
	{
		%word = getWord(%string, %i);

		%minVal = %maxVal + 1;
		%maxVal = %minVal + (%chances[%word] - 1);

		if(%rand >= %minVal && %rand <= %maxVal)
		{
			%chosenRarity = %word;

			break;
		}
	}

	//Just chose a rarity to choose the item from.

	%chosenItem = %itemRarity[%chosenRarity, getRandom(0, %itemRarityCount[%chosenRarity] - 1)].item.getDataBlock();
	//Got our item. Now give it to them.

	return %chosenItem;
}

function fxDTSBrick::z_fakeEvent_addRandomItem(%brick, %slot)
{
	//Do nothing.
}

function fxDTSBrick::addRandomItem_onLoadPlanted(%brick)
{
	if(!isObject(%brick.item))
	{
		return;
	}

	if(!isObject(%simSet = "addRandomItem_Cat" @ %colorID))
	{
		%simSet = new simSet("addRandomItem_Cat" @ %colorID);

		missionCleanup.add(%simSet);
	}

	%simSet.add(%brick);
	%brick.addRandomItem_simSet = %simSet;

	%brick.item.canPickUp = false;

	for(%i = 0; %i < %brick.numEvents; %i++)
	{
		if(%brick.eventOutput[%i] $= "z_fakeEvent_addRandomItem")
		{
			%raritySlot = mClampF(mFloor(%brick.eventOutputParameter[%i, 1]), 0, 9);

			break;
		}
	}

	if(%raritySlot $= "")
	{
		return;
	}

	%rarity = $AddRandomItem::Rarity[%raritySlot];
	%color = addRandomItem_hexToRGB(getSubStr(%rarity, 7, strLen(%rarity) - strLen(stripMLControlChars(%rarity)) - 8));

	%brick.item.setShapeName("Rarity:" SPC stripMLControlChars(%rarity) @ "(" @ %raritySlot @ ")");
	%brick.item.setShapeNameColor(%color);
	%brick.item.setShapeNameDistance($AddRandomItem::ShapeNameDist);

	%brick.addRandomItem_rarity = %raritySlot;
}

function serverCmdSetRandItemColorName(%client, %word1, %word2, %word3, %word4, %word5, %word6, %word7, %word8, %word9, %word10, %word11)
{
	if(!%client.isAdmin)
	{
		messageClient(%client, '', "You must be an \c6Admin\c0 or higher to use this command.");

		return;
	}

	%colorID = mClampF(mFloor(%client.currentColor), 0, 64);
	%name = trim(%word1 SPC %word2 SPC %word3 SPC %word4 SPC %word5 SPC %word6 SPC %word7 SPC %word8 SPC %word9 SPC %word10 SPC %word11);
	%hex = addRandomItem_rgbToHex(getColorIDTable(%client.currentColor));

	if(strLen(%name) > 64)
	{
		%name = getSubStr(%name, 0, 64);
	}

	$AddRandomItem::ColorName[%colorID] = %name;

	commandToClient(%client, 'centerPrint', "\c6Set random item category color ID<color:" @ %hex @ ">" SPC %client.currentColor SPC "\c6to<color:" @ %hex @ ">" SPC %name @ "\c6.", 5);
}

package Event_AddRandomItem
{
	function fxDTSBrickData::onTrustCheckFinished(%data, %brick)
	{
		if(isObject(%brick.client))
		{
			%client = %brick.client;
		}

		else if(isObject(%brick.getGroup().client))
		{
			%client = %brick.getGroup().client;
		}

		else if(isObject(findClientByBL_ID(%brick.getGroup().bl_id)))
	        {
			%client = findClientByBL_ID(%brick.getGroup().bl_id);
		}

		if(isObject(%client) && %data.getName() $= "BrickRandomItemSpawnData" && !%client.isAdmin)
	        {
			%failed = true;

			brickPlantSound.setName("BrickPlantSound_Temp");
		}

		parent::onTrustCheckFinished(%data, %brick);

		if(%failed)
		{
			BrickPlantSound_Temp.setName("BrickPlantSound");

			commandToClient(%client, 'centerPrint', "You must be an \c6Admin\c0 to plant this brick.", 2);
			%brick.trustCheckFailed();
		}
	}

	function fxDTSBrick::setColor(%brick, %colorID)
	{
		parent::setColor(%brick, %colorID);

		if(%brick.getDataBlock().getName() $= "BrickRandomItemSpawnData" && isObject(%brick.item))
		{
			if(!isObject(%simSet = "addRandomItem_Cat" @ %colorID))
			{
				%simSet = new simSet("addRandomItem_Cat" @ %colorID);

				missionCleanup.add(%simSet);
			}

			if(isObject(%brick.addRandomItem_simSet) && %brick.addRandomItem_simSet.isMember(%brick))
			{
				%brick.addRandomItem_simSet.remove(%brick);
			}

			%simSet.add(%brick);
		}
	}

	function fxDTSBrick::setItem(%brick, %item)
	{
		%prevItem = isObject(%brick.item) ? %brick.item.getDataBlock() : 0;

		parent::setItem(%brick, %item);

		%colorID = %brick.getColorID();

		if(%brick.getDataBlock().getName() $= "BrickRandomItemSpawnData")
		{
			if(isObject(%item))
			{
				if(!isObject(%prevItem))
				{
					if(!isObject(%simSet = "addRandomItem_Cat" @ %colorID))
					{
						%simSet = new simSet("addRandomItem_Cat" @ %colorID);

						missionCleanup.add(%simSet);
					}

					%simSet.add(%brick);
					%brick.addRandomItem_simSet = %simSet;
				}

				%brick.item.canPickUp = false;

				%slot = mClampF(%brick.addRandomItem_rarity, 0, 9);
				%rarity = $AddRandomItem::Rarity[%slot];
				%color = addRandomItem_hexToRGB(getSubStr(%rarity, 7, strLen(%rarity) - strLen(stripMLControlChars(%rarity)) - 8));

				%brick.item.setShapeName("Rarity:" SPC stripMLControlChars(%rarity) @ "(" @ %slot @ ")");
				%brick.item.setShapeNameColor(%color);
				%brick.item.setShapeNameDistance($AddRandomItem::ShapeNameDist);
			}

			else if(isObject(%prevItem))
			{
				if(!isObject(%simSet = "addRandomItem_Cat" @ %colorID))
				{
					return;
				}

				%simSet.remove(%brick);

				if(!%simSet.getCount())
				{
					%simSet.delete();
				}
			}

			%count = clientGroup.getCount();

			for(%i = 0; %i < %count; %i++)
			{
				if(isObject(%player = clientGroup.getObject(%i).player) && isObject(%player.isSettingRandomItemRarity) && %player.isSettingRandomItemRarity == %brick)
				{
					%player.isSettingRandomItemRarity = 0;

					commandToClient(clientGroup.getObject(%i), 'clearCenterPrint');
				}
			}
		}
	}

	function fxDTSBrick::onActivate(%brick, %player, %client, %pos, %vec)
	{
		parent::onActivate(%brick, %player, %client, %pos, %vec);

		%data = %brick.getDataBlock();

		if(%data.getName() !$= "BrickRandomItemSpawnData" || !isObject(%brick.item) || !%client.isAdmin)
		{
			return;
		}

		%player.isSettingRandomItemRarity = %brick;

		commandToClient(%client, 'centerPrint', "\c6Press \c30\c6 - \c39\c6 to set the random item rarity for<color:" @ addRandomItem_rgbToHex(getColorIDTable(%brick.getColorID())) @ ">" SPC %brick.item.getDataBlock().uiName @ "\c6.", 0);
	}

	function serverCmdUseInventory(%client, %slot)
	{
		if(isObject(%player = %client.player) && isObject(%brick = %player.isSettingRandomItemRarity))
		{
			%slot = %slot >= 9 ? 0 : %slot + 1;
			%rarity = $AddRandomItem::Rarity[%slot];

			%player.currSelectedRandomItemRarity = %slot;

			commandToClient(%client, 'centerPrint', "\c6Rarity:" SPC %rarity @ "(" @ %slot @ ")\c6.\n\c6Press \c2Plant Brick\c6 to confirm. Press \c0Cancel Brick\c6 to cancel.", 0);

			return;
		}

		parent::serverCmdUseInventory(%client, %slot);
	}

	function serverCmdPlantBrick(%client)
	{
		if(isObject(%player = %client.player) && isObject(%brick = %player.isSettingRandomItemRarity))
		{
			%slot = mClampF(mFloor(%player.currSelectedRandomItemRarity), 0, 9);
			%rarity = $AddRandomItem::Rarity[%slot];
			%color = addRandomItem_hexToRGB(getSubStr(%rarity, 7, strLen(%rarity) - strLen(stripMLControlChars(%rarity)) - 8));

			commandToClient(%client, 'centerPrint', "\c3" @ %brick.item.getDataBlock().uiName SPC "\c6rarity set to" SPC %rarity @ "(" @ %slot @ ")", 3);

			%player.isSettingRandomItemRarity = 0;
			%player.currSelectedRandomItemRarity = 0;

			%brick.item.setShapeName("Rarity:" SPC stripMLControlChars(%rarity) @ "(" @ %slot @ ")");
			%brick.item.setShapeNameColor(%color);
			%brick.item.setShapeNameDistance($AddRandomItem::ShapeNameDist);

			%brick.addRandomItem_rarity = %slot;

			for(%i = 0; %i < %brick.numEvents; %i++)
			{
				if(%brick.eventOutput[%i] $= "z_fakeEvent_addRandomItem")
				{
					%brick.eventOutputParameter[%i, 1] = %slot;

					%foundEvent = true;
					break;
				}
			}

			if(!%foundEvent)
			{
				%brick.addEvent(1, 0, "onActivate", "Self", "z_fakeEvent_addRandomItem", %slot);
			}

			return;
		}

		parent::serverCmdPlantBrick(%client);
	}

	function serverCmdCancelBrick(%client)
	{
		if(isObject(%player = %client.player) && isObject(%brick = %player.isSettingRandomItemRarity))
		{
			%player.isSettingRandomItemRarity = 0;
			%player.currSelectedRandomItemRarity = 0;

			commandToClient(%client, 'clearCenterPrint');

			return;
		}

		parent::serverCmdCancelBrick(%client);
	}

	function fxDTSBrick::onLoadPlant(%brick)
	{
		parent::onLoadPlant(%brick);

		%brick.schedule(1000, addRandomItem_onLoadPlanted);
	}
};
activatePackage(Event_AddRandomItem);
