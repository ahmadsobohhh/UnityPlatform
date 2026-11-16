using UnityEngine;
using TMPro;

namespace ImagineQuest.Characters
{
    public sealed class StudentCharacterCatalog : ScriptableObject
    {
        public GameObject[] Bodies;
        public RuntimeAnimatorController Locomotion;
        public Sprite CreatorBackground;
        public TMP_FontAsset MenuFont, ReadingFont;
        public static StudentCharacterCatalog Load() => Resources.Load<StudentCharacterCatalog>("StudentCharacters/Catalog");
    }
}
