using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ImagineQuest.Frontend
{
    /// <summary>
    /// Shared visual treatment for the player-facing application scenes. The gameplay
    /// scripts remain responsible for their content; this component owns only the
    /// reusable backdrop, readability treatment, and restrained interaction polish.
    /// </summary>
    internal static class PremiumFrontendPolishBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= AddPolish;
            SceneManager.sceneLoaded += AddPolish;
            AddPolish(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void AddPolish(Scene scene, LoadSceneMode mode)
        {
            if (!IsFrontendScene(scene.name))
                return;

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null && canvas.GetComponent<PremiumFrontendPolish>() == null)
                canvas.gameObject.AddComponent<PremiumFrontendPolish>();
        }

        private static bool IsFrontendScene(string name)
        {
            return name == "WelcomePage" || name == "Login" || name == "StudentAvatarSelect" ||
                   name == "StudentHub" || name == "ClassroomScene" || name == "TeacherClassSelect" ||
                   name == "TeacherClass" || name == "StudentProfile" || name == "StudentJoinClassWCode";
        }
    }

    public sealed class PremiumFrontendPolish : MonoBehaviour
    {
        private const string BackdropName = "PremiumHarbourBackdrop";
        private static readonly Color Gold = new(1f, 0.79f, 0.38f, 1f);
        private static readonly Color TextInk = new(0.98f, 0.96f, 0.87f, 1f);
        private static Sprite solidSprite;

        private void Awake()
        {
            BuildBackdrop();
            ImproveReadability();
        }

        private void BuildBackdrop()
        {
            if (transform.Find(BackdropName) != null)
                return;

            var backdrop = new GameObject(BackdropName, typeof(RectTransform), typeof(RawImage));
            backdrop.transform.SetParent(transform, false);
            backdrop.transform.SetAsFirstSibling();
            var rect = backdrop.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = backdrop.GetComponent<RawImage>();
            image.texture = Resources.Load<Texture2D>("Frontend/pirate-academy-harbour");
            image.color = new Color(1f, 1f, 1f, 0.88f);
            image.raycastTarget = false;

            var veil = CreateLayer("Moonlit Navy Veil", backdrop.transform, new Color(0.01f, 0.05f, 0.12f, 0.44f));
            veil.transform.SetAsLastSibling();
            var horizon = CreateLayer("Harbour Horizon Glow", backdrop.transform, new Color(0.03f, 0.17f, 0.24f, 0.14f));
            var horizonRect = horizon.GetComponent<RectTransform>();
            horizonRect.anchorMin = new Vector2(0f, 0f);
            horizonRect.anchorMax = new Vector2(1f, 0.38f);
            horizonRect.offsetMin = Vector2.zero;
            horizonRect.offsetMax = Vector2.zero;
            horizon.transform.SetAsLastSibling();
        }

        private static GameObject CreateLayer(string name, Transform parent, Color color)
        {
            var layer = new GameObject(name, typeof(RectTransform), typeof(Image));
            layer.transform.SetParent(parent, false);
            var rect = layer.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = layer.GetComponent<Image>();
            image.sprite = GetSolidSprite();
            image.color = color;
            image.raycastTarget = false;
            return layer;
        }

        private static Sprite GetSolidSprite()
        {
            if (solidSprite != null)
                return solidSprite;

            solidSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            return solidSprite;
        }

        private void ImproveReadability()
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                var colours = button.colors;
                colours.fadeDuration = 0.12f;
                button.colors = colours;

                var image = button.targetGraphic as Image;
                if (image != null && image.GetComponent<Outline>() == null)
                {
                    var outline = image.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.34f);
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                }

                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.color = TextInk;
                    AddTextShadow(label, new Color(0f, 0.04f, 0.09f, 0.8f), new Vector2(1.3f, -1.3f));
                }
            }

            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.fontSize >= 28f)
                    AddTextShadow(text, new Color(0f, 0.02f, 0.07f, 0.72f), new Vector2(2f, -2f));
            }
        }

        private static void AddTextShadow(TMP_Text text, Color color, Vector2 distance)
        {
            if (text.GetComponent<Shadow>() != null)
                return;
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }
    }
}
