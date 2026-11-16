using System;
using System.Globalization;
using UnityEngine;

namespace ImagineQuest.Characters
{
    /// <summary>Versioned appearance stored in the existing avatarId profile field.</summary>
    [Serializable]
    public struct StudentAppearance : IEquatable<StudentAppearance>
    {
        public int Body, Skin, Hair, HairColor;
        public const int HairChoiceCount=4;
        public static readonly string[] BoyHairNames = { "Bald", "Crew Cut", "Side Part", "Coils" };
        public static readonly string[] GirlHairNames = { "Bald", "Short Curls", "Long", "Twin Buns" };
        public static readonly string[] SkinNames = { "Porcelain", "Sand", "Honey", "Bronze", "Chestnut", "Ebony" };
        public static readonly string[] HairColorNames = { "Midnight", "Brown", "Chestnut", "Copper", "Golden", "Silver" };
        // Multipliers for the source's neutral light skin and hair textures; clothing never receives these tints.
        public static readonly Color[] SkinColors = { new Color(1f,.97f,.94f), new Color(.92f,.82f,.72f), new Color(.81f,.65f,.49f), new Color(.65f,.45f,.32f), new Color(.45f,.29f,.20f), new Color(.28f,.18f,.13f) };
        public static readonly Color[] HairColors = { new Color(.12f,.10f,.10f), new Color(.31f,.21f,.15f), new Color(.47f,.27f,.16f), new Color(.66f,.30f,.15f), new Color(.90f,.69f,.32f), new Color(.78f,.79f,.78f) };
        public static StudentAppearance Default => new StudentAppearance { Body=0, Skin=2, Hair=2, HairColor=1 };
        public string BodyName => Body == 1 ? "Girl" : "Boy";
        // 4/5 are compatibility-only styles, never offered as additional menu choices.
        public bool Valid => Body>=0 && Body<2 && Skin>=0 && Skin<SkinColors.Length && Hair>=0 && Hair<6 && HairColor>=0 && HairColor<HairColors.Length;
        public string AvatarId => string.Join(":", Hair>=4?"pirate-v1":"pirate-v2", Body.ToString(CultureInfo.InvariantCulture), Skin.ToString(CultureInfo.InvariantCulture), (Hair>=4?(Body==0?(Hair==4?2:3):(Hair==4?1:0)):Hair).ToString(CultureInfo.InvariantCulture), HairColor.ToString(CultureInfo.InvariantCulture));
        public static string HairName(int body,int style) => style>=4?(body==0?(style==4?"Long":"Twin Buns"):(style==4?"Side Part":"Crew Cut")):(body==1?GirlHairNames:BoyHairNames)[Mathf.Clamp(style,0,3)];
        public static bool TryParse(string value, out StudentAppearance appearance)
        {
            appearance=Default;
            if (string.IsNullOrEmpty(value) || value.Length>80) return false;
            var parts=value.Split(':');
            if (parts.Length!=5 || (parts[0]!="pirate-v1" && parts[0]!="pirate-v2") ||
                !int.TryParse(parts[1],NumberStyles.None,CultureInfo.InvariantCulture,out int body) ||
                !int.TryParse(parts[2],NumberStyles.None,CultureInfo.InvariantCulture,out int skin) ||
                !int.TryParse(parts[3],NumberStyles.None,CultureInfo.InvariantCulture,out int hair) ||
                !int.TryParse(parts[4],NumberStyles.None,CultureInfo.InvariantCulture,out int color)) return false;
            if(hair<0 || hair>=HairChoiceCount) return false;
            if(parts[0]=="pirate-v1") hair=body==0?new[]{1,2,4,5}[hair]:new[]{5,4,2,3}[hair];
            var parsed=new StudentAppearance { Body=body,Skin=skin,Hair=hair,HairColor=color };
            if (!parsed.Valid) return false;
            appearance=parsed; return true;
        }
        public static StudentAppearance FromAvatarId(string id) => TryParse(id,out var result) ? result : Default;
        public bool Equals(StudentAppearance other) => Body==other.Body && Skin==other.Skin && Hair==other.Hair && HairColor==other.HairColor;
        public override bool Equals(object other) => other is StudentAppearance appearance && Equals(appearance);
        public override int GetHashCode() => Body*1000+Skin*100+Hair*10+HairColor;
    }
}
