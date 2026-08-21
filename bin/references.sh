#!/bin/sh
# Use path variable if set, otherwise check for passed argument
if [ -z "$RainWorldDir" ]; then
    if [ -z $1 ]; then
        echo "ERR: No valid Rain World directory supplied"
        exit
    else
        RW_DIR=$1
    fi
else
    RW_DIR=$RainWorldDir
fi

# Create References folder if it does not exist yet
OUTPUT="${PWD}/References/"
if [ ! -d "$OUTPUT" ]; then
    mkdir "$OUTPUT"
fi

# Main RW files
cp -v "${RW_DIR}/BepInEx/core/BepInEx.dll" "$OUTPUT"
cp -v "${RW_DIR}/BepInEx/utils/PUBLIC-Assembly-CSharp.dll" "$OUTPUT"
cp -v "${RW_DIR}/BepInEx/plugins/HOOKS-Assembly-CSharp.dll" "$OUTPUT"
cp -v "${RW_DIR}/BepInEx/core/Mono.Cecil.dll" "$OUTPUT"
cp -v "${RW_DIR}/RainWorld_Data/Managed/MonoMod.RuntimeDetour.dll" "$OUTPUT"
cp -v "${RW_DIR}/RainWorld_Data/Managed/MonoMod.Utils.dll" "$OUTPUT"
cp -v "${RW_DIR}/RainWorld_Data/Managed/UnityEngine.dll" "$OUTPUT"
cp -v "${RW_DIR}/RainWorld_Data/Managed/UnityEngine.CoreModule.dll" "$OUTPUT"
cp -v "${RW_DIR}/RainWorld_Data/Managed/UnityEngine.AssetBundleModule.dll" "$OUTPUT"
cp -v "${RW_DIR}/RainWorld_Data/Managed/UnityEngine.InputLegacyModule.dll" "$OUTPUT"
echo ""

# Dependency Mods
cd "$RW_DIR"
cd ..
cd ..
echo "$PWD"
WORKSHOP_DIR="${PWD}/workshop/content/312520/"
if [ ! -d "$WORKSHOP_DIR" ]; then
    echo "ERR: Failed to find Rain World workshop directory"
fi

cp -v "${WORKSHOP_DIR}/2920439476/plugins/RegionKit.dll" "$OUTPUT"
cp -v "${WORKSHOP_DIR}/3126910221/plugins/ImprovedCollectiblesTracker.dll" "$OUTPUT"
cp -v "${WORKSHOP_DIR}/3244633122/plugins/ExtendedCollectiblesTracker.dll" "$OUTPUT"

echo "\nSuccessfully copied project references"
