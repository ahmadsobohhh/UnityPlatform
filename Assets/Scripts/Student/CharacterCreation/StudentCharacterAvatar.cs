using System;
using UnityEngine;

namespace ImagineQuest.Characters
{
    /// <summary>The same clothed prefab assembly is used in the creator, portraits and on deck.</summary>
    public sealed class StudentCharacterAvatar : MonoBehaviour
    {
        public Renderer[] SkinRenderers, HairRenderers;
        public int[] SkinMaterialIndices;
        public GameObject[] Hairstyles;
        public int Body;
        public StudentAppearance Appearance {get;private set;}
        public static StudentCharacterAvatar Create(Transform parent, StudentAppearance appearance, float scale=1f)
        {
            if (!appearance.Valid) appearance=StudentAppearance.Default;
            var catalog=StudentCharacterCatalog.Load();
            if (catalog==null || catalog.Bodies==null || catalog.Bodies.Length!=2 || catalog.Bodies[appearance.Body]==null)
                throw new InvalidOperationException("The character models are unavailable. Please reopen the game.");
            var root=Instantiate(catalog.Bodies[appearance.Body],parent,false);
            root.name="Student Pirate 3D"; root.transform.localScale=Vector3.one*scale;
            var avatar=root.GetComponent<StudentCharacterAvatar>(); avatar.Apply(appearance);
            var animator=root.GetComponent<Animator>();
            animator.runtimeAnimatorController=catalog.Locomotion; animator.applyRootMotion=false;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind(); animator.Update(0);
            return avatar;
        }
        public void Apply(StudentAppearance appearance)
        {
            if (!appearance.Valid || appearance.Body!=Body) throw new ArgumentException("Appearance does not match this model.");
            Appearance=appearance;
            for (int i=0;i<Hairstyles.Length;i++) Hairstyles[i].SetActive(i==appearance.Hair);
            Tint(SkinRenderers,StudentAppearance.SkinColors[appearance.Skin],SkinMaterialIndices);
            Tint(HairRenderers,StudentAppearance.HairColors[appearance.HairColor],null);
        }
        private static void Tint(Renderer[] renderers, Color color, int[] materialIndices)
        {
            var block=new MaterialPropertyBlock();
            for(int i=0;i<renderers.Length;i++)
            {
                var renderer=renderers[i];
                int slot=materialIndices!=null && i<materialIndices.Length?materialIndices[i]:0;
                block.Clear();renderer.GetPropertyBlock(block,slot);
                block.SetColor("_BaseColor",color); block.SetColor("_Color",color);
                renderer.SetPropertyBlock(block,slot);
            }
        }
    }
}
