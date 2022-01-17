﻿using Bannerlord.BUTR.Shared.Helpers;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Dropdown;
using MCM.Abstractions.Settings.Base.PerSave;
using System.Collections.Generic;
using System.ComponentModel;
using TaleWorlds.Localization;

namespace MarryAnyone.Settings
{
    // Instance is null for some reason...
    // Seems to be that setting fields are null on new game creation
    // Have to reload save in order for it to work.
    internal class MCMSettings : AttributePerSaveSettings<MCMSettings>, ISettingsProvider
    {

        public override string Id { get; } = "MarryAnyone_v2";

        public override string DisplayName => TextObjectHelper.Create("{=marryanyone}Marry Anyone {VERSION}", new Dictionary<string, TextObject?>
        {
            { "VERSION", new TextObject(Helper.VersionGet.ToString(3)) }
        })?.ToString() ?? "ERROR";

        [SettingPropertyDropdown("{=difficulty}Difficulty", Order = 0, RequireRestart = false, HintText = "{=difficulty_desc}Very Easy - no mini-game | Easy - mini-game nobles only | Realistic - mini-game all")]
        [SettingPropertyGroup("{=general}General")]
        public DropdownDefault<string> DifficultyDropdown { get; set; } = new DropdownDefault<string>(new string[]
        {
            "Very Easy",
            "Easy",
            "Realistic"
        }, 1);

        [SettingPropertyDropdown("{=orientation}Sexual Orientation", Order = 1, RequireRestart = false, HintText = "{=orientation_desc}Player character can choose what gender the player can marry")]
        [SettingPropertyGroup("{=general}General")]
        public DropdownDefault<string> SexualOrientationDropdown { get; set; } = new DropdownDefault<string>(new string[]
        {
            "Heterosexual",
            "Homosexual",
            "Bisexual"
        }, 0);

        [SettingPropertyBool("{=cheating}Cheating", RequireRestart = false, HintText = "{=cheating_desc}Player character can marry characters that are already married")]
        [SettingPropertyGroup("{=relationship}Relationship Options")]
        public bool Cheating { get; set; } = false;

        [SettingPropertyBool("{=polygamy}Polygamy", Order = 1, RequireRestart = false, HintText = "{=polygamy_desc}Player character can have polygamous relationships")]
        [SettingPropertyGroup("{=relationship}Relationship Options")]
        public bool Polygamy { get; set; } = false;

        [SettingPropertyBool("{=polyamory}Polyamory", Order = 2, RequireRestart = false, HintText = "{=polyamory_desc}Player character's spouses can have relationships with each other")]
        [SettingPropertyGroup("{=relationship}Relationship Options")]
        public bool Polyamory { get; set; } = false;

        [SettingPropertyBool("{=incest}Incest", Order = 3, RequireRestart = false, HintText = "{=incest_desc}Player character can have incestuous relationships")]
        [SettingPropertyGroup("{=relationship}Relationship Options")]
        public bool Incest { get; set; } = false;

        [SettingPropertyBool("{=notable}With notable", Order = 4, RequireRestart = false, HintText = "{=notable_desc}Player character can marry notable")]
        [SettingPropertyGroup("{=relationship}Relationship Options")]
        public bool Notable { get; set; } = false;

        [SettingPropertyBool("{=ImproveRelation}Improve relation", Order = 5, RequireRestart = false, HintText = "{=ImproveRelation_desc}Improve relation when heroes have sexual relation")]
        [SettingPropertyGroup("{=relationship}Relationship Options")]
        public bool ImproveRelation { get; set; } = false;

        [SettingPropertyBool("{=CanJoinUpperClanThroughMAPath}Can join upper clan through MA Path", Order = 6, RequireRestart = false, HintText = "{=CanJoinUpperClanThroughMAPath_desc}Can join upper clan through MA Path (Not compatible with Calradia Expanded)")]
        [SettingPropertyGroup("{=relationship}Relationship Options")]
        public bool CanJoinUpperClanThroughMAPath { get; set; } = false;

        [SettingPropertyInteger("{=RelationLevelMinForRomance}Relation needed for romance", -1, 100, Order = 10, RequireRestart = false, HintText = "{=RelationLevelMinForRomance_desc}Relation needed for begin a romance (-1 desabled the control)")]
        [SettingPropertyGroup("{=relationship}Relationship Options")]
        public int RelationLevelMinForRomance { get; set; } = 5;

        [SettingPropertyInteger("{=RelationLevelMinForCheating}Relation needed for cheating relation", -1, 100, Order = 11, RequireRestart = false, HintText = "{=RelationLevelMinForRomance_desc}Relation needed for begin a cheating romance (-1 desabled the control)")]
        [SettingPropertyGroup("{=relationship}Relationship Options")]
        public int RelationLevelMinForCheating { get; set; } = 10;

        [SettingPropertyInteger("{=RelationLevelMinForSex}Relation needed for sexual relation", -1, 100, Order = 12, RequireRestart = false, HintText = "{=RelationLevelMinForSex_desc}Relation needed for sexual relation (-1 desabled the control)")]
        [SettingPropertyGroup("{=relationship}Relationship Options")]
        public int RelationLevelMinForSex { get; set; } = 10;

        [SettingPropertyBool("{=retry_courtship}Retry Courtship", RequireRestart = false, HintText = "{=retry_courtship_desc}Player can retry courtship after failure")]
        [SettingPropertyGroup("{=courtship}Courtship", GroupOrder = 1)]
        public bool RetryCourtship { get; set; } = false;

        public string Difficulty { get => DifficultyDropdown.SelectedValue; set => DifficultyDropdown.SelectedValue = value; }
        public string SexualOrientation { get => SexualOrientationDropdown.SelectedValue; set => SexualOrientationDropdown.SelectedValue = value; }

        [SettingPropertyBool("{=spousejoinarena}Spouse(s) join arena", Order = 1, RequireRestart = false, HintText = "{=spousejoinarena_desc}Spouse join arena with you")]
        [SettingPropertyGroup("{=Side}Side Options")]
        public bool SpouseJoinArena { get; set; } = false;

        [SettingPropertyBool("{=adoption}Adoption", RequireRestart = false, HintText = "{=adoption_desc}Player can adopt children in towns and villages", IsToggle = true)]
        [SettingPropertyGroup("{=adoption}Adoption", GroupOrder = 2)]
        public bool Adoption { get; set; } = false;

        [SettingPropertyFloatingInteger("{=adoption_chance}Adoption Chance", 0f, 1f, "#0%", RequireRestart = false, HintText = "{=adoption_chance_desc}Chance that a child is up for adoption")]
        [SettingPropertyGroup("{=adoption}Adoption", GroupOrder = 2)]
        public float AdoptionChance { get; set; } = 0.05f;

        [SettingPropertyBool("{=adoption_titles}Adoption Titles", RequireRestart = false, HintText = "{=adoption_titles_desc}Encyclopedia displays children without a parent as adopted")]
        [SettingPropertyGroup("{=adoption}Adoption", GroupOrder = 2)]
        public bool AdoptionTitles { get; set; } = false;

        [SettingPropertyBool("{=NotifyRelationImprovementWithinFamily}Notify relation improvement in your family", Order = 1, RequireRestart = false, HintText = "{=NotifyRelationImprovementWithinFamily_desc}Display relation improvement in your family in the game's message log")]
        [SettingPropertyGroup("{=Notification}Notification", GroupOrder = 3)]
        public bool NotifyRelationImprovementWithinFamily { get; set; } = false;

        [SettingPropertyBool("{=debug}Debug", Order = 2, RequireRestart = false, HintText = "{=debug_desc}Displays mod developer debug information in the game's message log")]
        [SettingPropertyGroup("{=Notification}Notification", GroupOrder = 3)]
        public bool Debug { get; set; } = false;

        [SettingPropertyBool("{=patch}Patch", Order = 2, RequireRestart = false, HintText = "{=patch_desc}Save and lod the save to apply the patch again")]
        public bool Patch { get; set; } = false;


    }
}