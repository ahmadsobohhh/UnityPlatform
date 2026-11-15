using UnityEngine;

namespace ImagineQuest.Characters
{
    public sealed class StudentCharacterCatalog : ScriptableObject
    {
        public GameObject[] Bodies;
        public RuntimeAnimatorController Locomotion;
        public static StudentCharacterCatalog Load() => Resources.Load<StudentCharacterCatalog>("StudentCharacters/Catalog");
    }
}
