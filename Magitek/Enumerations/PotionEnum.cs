namespace Magitek.Enumerations
{
    /// <summary>
    /// Enumeration of FFXIV stat potion (tincture/gemdraught) item IDs.
    /// Item names retrieved from Garland Tools (https://www.garlandtools.org/db/).
    /// 
    /// IMPORTANT: This enum is for BATTLE potions (Tinctures and Gemdraughts), NOT crafting potions (Gemsaps).
    /// 
    /// HOW TO FIND ITEM IDs AND NAMES FOR FUTURE UPDATES:
    /// 
    /// 1. Navigate to Garland Tools: https://www.garlandtools.org/db/
    /// 
    /// 2. Search for the item by exact name in the search box (e.g., "Grade 4 Gemdraught of Dexterity")
    ///    - Type the full item name and press Enter
    ///    - Garland Tools will navigate to the item page
    /// 
    /// 3. Get the Item ID from the URL:
    ///    - The URL format is: https://www.garlandtools.org/db/#item/[ITEM_ID]
    ///    - Example: https://www.garlandtools.org/db/#item/49218
    ///    - The number after "#item/" is the Item ID (49218 in this example)
    /// 
    /// 4. Alternative method - Use the Gear icon:
    ///    - When an item is highlighted/selected in search results
    ///    - Click the Gear/Settings icon on the highlighted item
    ///    - This will display the item ID
    /// 
    /// 5. Verify the item name (CRITICAL - ALWAYS DO THIS):
    ///    - The item name is displayed on the item page
    ///    - Use this exact name in the XML documentation comment
    ///    - IMPORTANT: Verify it's a Gemdraught (battle) not Gemsap (crafting)
    ///    - CRITICAL: Always verify the item name matches what you searched for - don't trust the URL alone!
    ///      The URL might navigate to a different item if the search doesn't find an exact match.
    /// 
    /// NOTE: Grade numbering follows FFXIV's tier system:
    ///    - Grade 12 = Grade 4 Gemdraught (newest, Dawntrail 100+)
    ///    - Grade 11 = Grade 3 Gemdraught (Dawntrail 100)
    ///    - Grade 10 = Grade 2 Gemdraught (Endwalker 90)
    ///    - Grade 9 = Grade 1 Gemdraught (Shadowbringers 80)
    ///    - Grade 8 = Grade 8 Tincture (Stormblood 70)
    ///    - Grade 7 = Grade 7 Tincture (Heavensward 60)
    ///    - Grade 6 = Grade 6 Tincture (A Realm Reborn 50)
    ///    - Grade 5 = Grade 5 Tincture (Lower level)
    /// </summary>
    public enum PotionEnum : int
    {
        None = 0,

        /// <summary>Grade 4 Gemdraught of Dexterity (Item ID: 49235) - Dawntrail (Level 100+)</summary>
        Dex_Grade_12 = 49235,
        /// <summary>Grade 3 Gemdraught of Dexterity (Item ID: 45996) - Dawntrail (Level 100)</summary>
        Dex_Grade_11 = 45996,
        /// <summary>Grade 2 Gemdraught of Dexterity (Item ID: 44163) - Endwalker (Level 90)</summary>
        Dex_Grade_10 = 44163,
        /// <summary>Grade 1 Gemdraught of Dexterity (Item ID: 44158) - Shadowbringers (Level 80)</summary>
        Dex_Grade_9 = 44158,
        /// <summary>Grade 8 Tincture of Dexterity (Item ID: 39728) - Stormblood (Level 70)</summary>
        Dex_Grade_8 = 39728,
        /// <summary>Grade 7 Tincture of Dexterity (Item ID: 37841) - Heavensward (Level 60)</summary>
        Dex_Grade_7 = 37841,
        /// <summary>Grade 6 Tincture of Dexterity (Item ID: 36110) - A Realm Reborn (Level 50)</summary>
        Dex_Grade_6 = 36110,
        /// <summary>Grade 5 Tincture of Dexterity (Item ID: 36105) - Lower level</summary>
        Dex_Grade_5 = 36105,

        /// <summary>Grade 4 Gemdraught of Intelligence (Item ID: 49237) - Dawntrail (Level 100+)</summary>
        Intel_Grade_12 = 49237,
        /// <summary>Grade 3 Gemdraught of Intelligence (Item ID: 45998) - Dawntrail (Level 100)</summary>
        Intel_Grade_11 = 45998,
        /// <summary>Grade 2 Gemdraught of Intelligence (Item ID: 44165) - Endwalker (Level 90)</summary>
        Intel_Grade_10 = 44165,
        /// <summary>Grade 1 Gemdraught of Intelligence (Item ID: 44160) - Shadowbringers (Level 80)</summary>
        Intel_Grade_9 = 44160,
        /// <summary>Grade 8 Tincture of Intelligence (Item ID: 39730) - Stormblood (Level 70)</summary>
        Intel_Grade_8 = 39730,
        /// <summary>Grade 7 Tincture of Intelligence (Item ID: 37843) - Heavensward (Level 60)</summary>
        Intel_Grade_7 = 37843,
        /// <summary>Grade 6 Tincture of Intelligence (Item ID: 36112) - A Realm Reborn (Level 50)</summary>
        Intel_Grade_6 = 36112,
        /// <summary>Grade 5 Tincture of Intelligence (Item ID: 36107) - Lower level</summary>
        Intel_Grade_5 = 36107,

        /// <summary>Grade 4 Gemdraught of Mind (Item ID: 49238) - Dawntrail (Level 100+)</summary>
        Mind_Grade_12 = 49238,
        /// <summary>Grade 3 Gemdraught of Mind (Item ID: 45999) - Dawntrail (Level 100)</summary>
        Mind_Grade_11 = 45999,
        /// <summary>Grade 2 Gemdraught of Mind (Item ID: 44166) - Endwalker (Level 90)</summary>
        Mind_Grade_10 = 44166,
        /// <summary>Grade 1 Gemdraught of Mind (Item ID: 44161) - Shadowbringers (Level 80)</summary>
        Mind_Grade_9 = 44161,
        /// <summary>Grade 8 Tincture of Mind (Item ID: 39731) - Stormblood (Level 70)</summary>
        Mind_Grade_8 = 39731,
        /// <summary>Grade 7 Tincture of Mind (Item ID: 37844) - Heavensward (Level 60)</summary>
        Mind_Grade_7 = 37844,
        /// <summary>Grade 6 Tincture of Mind (Item ID: 36113) - A Realm Reborn (Level 50)</summary>
        Mind_Grade_6 = 36113,
        /// <summary>Grade 5 Tincture of Mind (Item ID: 36108) - Lower level</summary>
        Mind_Grade_5 = 36108,

        /// <summary>Grade 4 Gemdraught of Strength (Item ID: 49234) - Dawntrail (Level 100+)</summary>
        Strength_Grade_12 = 49234,
        /// <summary>Grade 3 Gemdraught of Strength (Item ID: 45995) - Dawntrail (Level 100)</summary>
        Strength_Grade_11 = 45995,
        /// <summary>Grade 2 Gemdraught of Strength (Item ID: 44162) - Endwalker (Level 90)</summary>
        Strength_Grade_10 = 44162,
        /// <summary>Grade 1 Gemdraught of Strength (Item ID: 44157) - Shadowbringers (Level 80)</summary>
        Strength_Grade_9 = 44157,
        /// <summary>Grade 8 Tincture of Strength (Item ID: 39727) - Stormblood (Level 70)</summary>
        Strength_Grade_8 = 39727,
        /// <summary>Grade 7 Tincture of Strength (Item ID: 37840) - Heavensward (Level 60)</summary>
        Strength_Grade_7 = 37840,
        /// <summary>Grade 6 Tincture of Strength (Item ID: 36109) - A Realm Reborn (Level 50)</summary>
        Strength_Grade_6 = 36109,
        /// <summary>Grade 5 Tincture of Strength (Item ID: 36104) - Lower level</summary>
        Strength_Grade_5 = 36104
    }
}
