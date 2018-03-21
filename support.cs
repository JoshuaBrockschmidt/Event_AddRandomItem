//Usage:	getColorIDTableCat(COLOR ID);
//Returns:	Row Name	TAB	Color ID relative to row(starting at 1)

function getColorIDTableCat(%colorID)
{
	//Probably global variables for this, but I don't know them.

	%colorset = $GameModeArg $= "Add-Ons/GameMode_Custom/gamemode.txt" || $GameModeArg $= "" ? "config/server/colorset.txt" : filePath($GameModeArg) @ "/colorset.txt";

	if(!isFile(%colorset))
	{
		return;
	}

	%file = new fileObject();
	%file.openForRead(%colorset);

	%currColor = 0;
	%returnColor = 1;

	while(!%file.isEOF())
	{
		%line = %file.readLine();

		if(%line $= "")
		{
			continue;
		}

		if(%currColor == %colorID)
		{
			%found = true;
		}

		if(getSubStr(%line, 0, 4) $= "DIV:")
		{
			%currCat = trim(getSubStr(%line, 4, strLen(%line) - 4));

			if(%found)
			{
				break;
			}

			%returnColor = 1;

			continue;
		}

		%currColor++;

		if(!%found)
		{
			%returnColor++;
		}
	}

	%file.delete();

	if(!%found)
	{
		return -1;
	}

	return %currCat TAB %returnColor;
}

function addRandomItem_rgbToHex(%rgb)
{
	%r = addRandomItem_compToHex(255 * firstWord(%rgb));
	%g = addRandomItem_compToHex(255 * getWord(%rgb, 1));
	%b = addRandomItem_compToHex(255 * getWord(%rgb, 2));

	return %r @ %g @ %b;
}

function addRandomItem_hexToRgb(%rgb)
{
	%r = addRandomItem_hexToComp(getSubStr(%rgb, 0, 2)) / 255;
	%g = addRandomItem_hexToComp(getSubStr(%rgb, 2, 2)) / 255;
	%b = addRandomItem_hexToComp(getSubStr(%rgb, 4, 2)) / 255;

	return %r SPC %g SPC %b;
}

function addRandomItem_compToHex(%comp)
{
	%left = mFloor(%comp / 16);
	%comp = mFloor(%comp - %left * 16);
	%left = getSubStr("0123456789ABCDEF", %left, 1);
	%comp = getSubStr("0123456789ABCDEF", %comp, 1);

	return %left @ %comp;
}

function addRandomItem_hexToComp(%hex)
{
	%left = getSubStr(%hex, 0, 1);
	%comp = getSubStr(%hex, 1, 1);
	%left = striPos("0123456789ABCDEF", %left);
	%comp = striPos("0123456789ABCDEF", %comp);

	if(%left < 0 || %comp < 0)
	{
		return 0;
	}

	return %left * 16 + %comp;
}