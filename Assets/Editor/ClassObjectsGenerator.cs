// Generates a complete set of sample RPG assets under Assets/ClassObjects
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ClassSystem.Core;
using ClassSystem.Core.Utilities;
using ClassSystem.Combat;
using ClassSystem.Items;
using ClassSystem.Classes;
using ClassSystem.Spells;

public static class ClassObjectsGenerator
{
    const string Root = "Assets/ClassObjects";

    [MenuItem("Tools/RPG/Generate Sample ClassObjects")]
    public static void Generate()
    {
        EnsureFolders(
            Root,
            Root + "/DamageTypes",
            Root + "/WeaponTypes",
            Root + "/Weapons",
            Root + "/Armors",
            Root + "/Spells",
            Root + "/StatBlocks",
            Root + "/PlayerClasses",
            Root + "/LevelingProfiles"
        );

        // Damage Types
        var fire = CreateOrLoad<DamageType>(Root + "/DamageTypes/Fire.asset", a => a.color = new Color(1f, 0.3f, 0.08f));
        var ice = CreateOrLoad<DamageType>(Root + "/DamageTypes/Ice.asset", a => a.color = new Color(0.6f, 0.9f, 1f));
        var lightning = CreateOrLoad<DamageType>(Root + "/DamageTypes/Lightning.asset", a => a.color = new Color(1f, 1f, 0.2f));
        var poison = CreateOrLoad<DamageType>(Root + "/DamageTypes/Poison.asset", a => a.color = new Color(0.3f, 0.8f, 0.2f));

        // Weapon Types
        var sword = CreateOrLoad<WeaponType>(Root + "/WeaponTypes/Sword.asset");
        var dagger = CreateOrLoad<WeaponType>(Root + "/WeaponTypes/Dagger.asset");
        var wand = CreateOrLoad<WeaponType>(Root + "/WeaponTypes/Wand.asset");
        var staff = CreateOrLoad<WeaponType>(Root + "/WeaponTypes/Staff.asset");

        // Leveling Profile (shared)
        var lvl = CreateOrLoad<LevelingProfile>(Root + "/LevelingProfiles/PlayerLvlProfile.asset", a =>
        {
            a.xpToNextLevel = new List<int> { 100, 150, 200, 250, 300, 400 };
            a.extrapolationCurve = AnimationCurve.EaseInOut(1, 100, 20, 5000);
        });

        // Stat Blocks
        var knightStats = CreateOrLoad<StatBlock>(Root + "/StatBlocks/KnightStatBlock.asset", a =>
        {
            SetStat(a, StatType.Health, 20, 10);
            SetStat(a, StatType.Mana, 10, 3);
            SetStat(a, StatType.Strength, 8, 4);
            SetStat(a, StatType.Intelligence, 3, 1);
            SetStat(a, StatType.Dexterity, 6, 3);
            SetStat(a, StatType.Defense, 5, 2);
            SetStat(a, StatType.ManaRegen, 1, 0.3f);
        });
        var wizardStats = CreateOrLoad<StatBlock>(Root + "/StatBlocks/WizardStatBlock.asset", a =>
        {
            SetStat(a, StatType.Health, 12, 6);
            SetStat(a, StatType.Mana, 30, 8);
            SetStat(a, StatType.Strength, 3, 1);
            SetStat(a, StatType.Intelligence, 8, 4);
            SetStat(a, StatType.Dexterity, 6, 3);
            SetStat(a, StatType.Defense, 2, 1);
            SetStat(a, StatType.ManaRegen, 2, 0.6f);
        });

        // Spells
        var fireball = CreateOrLoad<Spell>(Root + "/Spells/Fireball.asset", a =>
        {
            a.displayName = "Fireball";
            a.element = fire;
            a.manaCost = 5f;
            a.cooldown = 0.5f;
            a.effect = SpellEffectKind.Damage;
            a.baseAmount = 4; a.dice.Clear(); a.dice.Add(new Dice { count = 2, sides = 6 });
            a.continuous = false; a.duration = 0f; a.tickInterval = 0.5f;
            a.targeting = SpellTargetingMode.Projectile;
            a.projectileSpeed = 8f; a.projectileMaxDistance = 10f;
        });
        var poisonSpray = CreateOrLoad<Spell>(Root + "/Spells/Poison Spray.asset", a =>
        {
            a.displayName = "Poison Spray";
            a.element = poison;
            a.manaCost = 4f;
            a.cooldown = 0.7f;
            a.effect = SpellEffectKind.Damage;
            a.baseAmount = 2; a.dice.Clear(); a.dice.Add(new Dice { count = 1, sides = 4 });
            a.continuous = true; a.duration = 4f; a.tickInterval = 0.5f;
            a.targeting = SpellTargetingMode.Projectile;
            a.projectileSpeed = 6f; a.projectileMaxDistance = 6f;
        });
        var healSelf = CreateOrLoad<Spell>(Root + "/Spells/Heal Self.asset", a =>
        {
            a.displayName = "Heal Self";
            a.element = null;
            a.manaCost = 5f;
            a.cooldown = 1.0f;
            a.effect = SpellEffectKind.Heal;
            a.baseAmount = 6; a.dice.Clear(); a.dice.Add(new Dice { count = 1, sides = 6 });
            a.continuous = false; a.duration = 0f; a.tickInterval = 0.5f;
            a.targeting = SpellTargetingMode.Self;
        });
        var regenerate = CreateOrLoad<Spell>(Root + "/Spells/Regenerate Health.asset", a =>
        {
            a.displayName = "Regenerate Health";
            a.element = null;
            a.manaCost = 6f;
            a.cooldown = 2.0f;
            a.effect = SpellEffectKind.Heal;
            a.baseAmount = 2; a.dice.Clear(); a.dice.Add(new Dice { count = 1, sides = 4 });
            a.continuous = true; a.duration = 5f; a.tickInterval = 0.5f;
            a.targeting = SpellTargetingMode.Self;
        });

        // Weapons
        var flaming = CreateOrLoad<Weapon>(Root + "/Weapons/FlamingLaserSword.asset", a =>
        {
            a.weaponType = sword;
            a.attackCooldown = 0.6f;
            a.damage.Clear();
            a.damage.Add(new WeaponDamageEntry { type = fire, baseAmount = 4, dice = new List<Dice>{ new Dice{count=2, sides=6} } });
            a.damage.Add(new WeaponDamageEntry { type = lightning, baseAmount = 1, dice = new List<Dice>{ new Dice{count=1, sides=4} } });
        });
        var daggerWpn = CreateOrLoad<Weapon>(Root + "/Weapons/Dagger.asset", a =>
        {
            a.weaponType = dagger;
            a.attackCooldown = 0.4f;
            a.damage.Clear();
            a.damage.Add(new WeaponDamageEntry { type = poison, baseAmount = 1, dice = new List<Dice>{ new Dice{count=1, sides=6} } });
        });
        var basicWand = CreateOrLoad<Weapon>(Root + "/Weapons/BasicWand.asset", a =>
        {
            a.weaponType = wand;
            a.attackCooldown = 0.7f;
            a.damage.Clear();
            a.damage.Add(new WeaponDamageEntry { type = lightning, baseAmount = 1, dice = new List<Dice>{ new Dice{count=1, sides=6} } });
        });

        // Armor
        var basicArmor = CreateOrLoad<Armor>(Root + "/Armors/BasicArmor.asset", a =>
        {
            a.armorClass = 2;
            a.resistances.Clear();
            a.resistances.Add(new ResistanceEntry { type = ice, reduction = 0.1f });
        });
        var leatherArmor = CreateOrLoad<Armor>(Root + "/Armors/LeatherArmor.asset", a =>
        {
            a.armorClass = 3;
            a.resistances.Clear();
            a.resistances.Add(new ResistanceEntry { type = poison, reduction = 0.05f });
        });

        // Player Classes
        var wizardClass = CreateOrLoad<PlayerClass>(Root + "/PlayerClasses/WizardClass.asset", a =>
        {
            a.displayName = "Wizard";
            a.levelingProfile = lvl;
            a.statBlock = wizardStats;
            a.allowedWeaponTypes.Clear();
            a.allowedWeaponTypes.Add(wand);
            a.allowedWeaponTypes.Add(staff);
            a.knownSpells.Clear();
            a.knownSpells.Add(fireball);
            a.knownSpells.Add(healSelf);
            a.knownSpells.Add(regenerate);
        });
        var knightClass = CreateOrLoad<PlayerClass>(Root + "/PlayerClasses/KnightClass.asset", a =>
        {
            a.displayName = "Knight";
            a.levelingProfile = lvl;
            a.statBlock = knightStats;
            a.allowedWeaponTypes.Clear();
            a.allowedWeaponTypes.Add(sword);
            a.allowedWeaponTypes.Add(dagger);
            a.knownSpells.Clear();
            a.knownSpells.Add(regenerate); // simple self-heal over time
        });

        AssetDatabase.SaveAssets();
        Debug.Log("Sample ClassObjects generated under Assets/ClassObjects. You can run this once to bootstrap a project.");
    }

    // Helpers
    static void EnsureFolders(params string[] folders)
    {
        foreach (var path in folders)
        {
            if (AssetDatabase.IsValidFolder(path)) continue;
            var parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }

    static T CreateOrLoad<T>(string path, System.Action<T> init = null) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            init?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
        }
        else if (init != null)
        {
            init(asset);
            EditorUtility.SetDirty(asset);
        }
        return asset;
    }

    static void SetStat(StatBlock block, StatType type, float baseValue, float perLevel)
    {
        var so = new SerializedObject(block);
        var entries = so.FindProperty("entries");
        // find existing
        int found = -1;
        for (int i = 0; i < entries.arraySize; i++)
        {
            var elem = entries.GetArrayElementAtIndex(i);
            if ((StatType)elem.FindPropertyRelative("stat").enumValueIndex == type)
            {
                found = i; break;
            }
        }
        if (found < 0)
        {
            entries.InsertArrayElementAtIndex(entries.arraySize);
            found = entries.arraySize - 1;
        }
        var target = entries.GetArrayElementAtIndex(found);
        target.FindPropertyRelative("stat").enumValueIndex = (int)type;
        target.FindPropertyRelative("baseValue").floatValue = baseValue;
        target.FindPropertyRelative("perLevel").floatValue = perLevel;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif

