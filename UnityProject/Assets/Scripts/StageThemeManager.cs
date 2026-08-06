using System;
using System.Collections.Generic;
using UnityEngine;

namespace TikTokLiveGame
{
    public sealed class StageThemeData
    {
        public string Id { get; }
        public string Name { get; }
        public string ResourcePath { get; }
        public Color AmbientColor { get; }
        public Color FogColor { get; }
        public Color PrimaryLightColor { get; }
        public Color SecondaryLightColor { get; }
        public Color CameraBgColor { get; }

        public StageThemeData(
            string id,
            string name,
            string resourcePath,
            Color ambientColor,
            Color fogColor,
            Color primaryLightColor,
            Color secondaryLightColor,
            Color cameraBgColor)
        {
            Id = id;
            Name = name;
            ResourcePath = resourcePath;
            AmbientColor = ambientColor;
            FogColor = fogColor;
            PrimaryLightColor = primaryLightColor;
            SecondaryLightColor = secondaryLightColor;
            CameraBgColor = cameraBgColor;
        }
    }

    public sealed class StageThemeManager : MonoBehaviour
    {
        public static StageThemeManager Instance { get; private set; }

        private const string PrefKey = "SelectedStageThemeIndex";

        private readonly List<StageThemeData> themes = new()
        {
            new StageThemeData(
                "premium-stage-v2",
                "Cyber Disco",
                "Stage/premium-stage-v2",
                new Color(0.09f, 0.06f, 0.18f),
                new Color(0.015f, 0.008f, 0.04f),
                new Color(0.1f, 0.9f, 1f),
                new Color(0.58f, 0.16f, 1f),
                new Color(0.01f, 0.005f, 0.025f)
            ),
            new StageThemeData(
                "stage-gold-palace",
                "Cung Điện Vàng",
                "Stage/stage-gold-palace",
                new Color(0.24f, 0.18f, 0.06f),
                new Color(0.05f, 0.035f, 0.01f),
                new Color(1f, 0.85f, 0.3f),
                new Color(1f, 0.6f, 0.12f),
                new Color(0.04f, 0.025f, 0.005f)
            ),
            new StageThemeData(
                "stage-heaven-angel",
                "Thiên Đường",
                "Stage/stage-heaven-angel",
                new Color(0.25f, 0.28f, 0.35f),
                new Color(0.08f, 0.1f, 0.14f),
                new Color(1f, 0.96f, 0.85f),
                new Color(0.4f, 0.8f, 1f),
                new Color(0.03f, 0.04f, 0.06f)
            ),
            new StageThemeData(
                "stage-inferno-hell",
                "Địa Ngục Red",
                "Stage/stage-inferno-hell",
                new Color(0.2f, 0.02f, 0.02f),
                new Color(0.05f, 0.005f, 0.005f),
                new Color(1f, 0.12f, 0.05f),
                new Color(1f, 0.42f, 0f),
                new Color(0.02f, 0.002f, 0.002f)
            )
        };

        private int currentIndex;
        private GameObject backdropObject;
        private Renderer backdropRenderer;
        private Material backdropMaterial;
        private Light cyanLight;
        private Light violetLight;

        public IReadOnlyList<StageThemeData> Themes => themes;
        public int CurrentIndex => currentIndex;
        public StageThemeData CurrentTheme => themes[Mathf.Clamp(currentIndex, 0, themes.Count - 1)];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Initialize()
        {
            FindStageLights();
            EnsureBackdropPlane();
            int savedIndex = PlayerPrefs.GetInt(PrefKey, 0);
            SetTheme(savedIndex, false);
        }

        private void FindStageLights()
        {
            GameObject cyanObj = GameObject.Find("Cyan Light");
            if (cyanObj != null) cyanLight = cyanObj.GetComponent<Light>();

            GameObject violetObj = GameObject.Find("Violet Light");
            if (violetObj != null) violetLight = violetObj.GetComponent<Light>();
        }

        private void EnsureBackdropPlane()
        {
            if (backdropObject != null) return;

            backdropObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backdropObject.name = "Stage Theme Backdrop";
            backdropObject.transform.position = new Vector3(0f, 5.1f, -10.28f);
            backdropObject.transform.localScale = new Vector3(18.5f, 10.8f, 1f);

            // Remove collider for background quad
            Collider col = backdropObject.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Shader shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
            backdropMaterial = new Material(shader)
            {
                name = "Stage Theme Material"
            };

            backdropRenderer = backdropObject.GetComponent<Renderer>();
            backdropRenderer.sharedMaterial = backdropMaterial;
        }

        public void SetTheme(int index, bool savePref = true)
        {
            if (themes.Count == 0) return;
            currentIndex = (index % themes.Count + themes.Count) % themes.Count;
            StageThemeData theme = CurrentTheme;

            // Load backdrop texture
            Texture2D texture = Resources.Load<Texture2D>(theme.ResourcePath);
            if (texture != null && backdropMaterial != null)
            {
                backdropMaterial.mainTexture = texture;
            }
            else
            {
                Debug.LogWarning($"[StageThemeManager] Could not load texture at: {theme.ResourcePath}");
            }

            // Apply lighting & fog
            RenderSettings.ambientLight = theme.AmbientColor;
            RenderSettings.fogColor = theme.FogColor;

            if (Camera.main != null)
            {
                Camera.main.backgroundColor = theme.CameraBgColor;
            }

            if (cyanLight != null) cyanLight.color = theme.PrimaryLightColor;
            if (violetLight != null) violetLight.color = theme.SecondaryLightColor;

            if (savePref)
            {
                PlayerPrefs.SetInt(PrefKey, currentIndex);
                PlayerPrefs.Save();
            }

            Debug.Log($"[StageThemeManager] Switched to theme: {theme.Name} ({theme.Id})");
        }

        public void NextTheme()
        {
            SetTheme(currentIndex + 1);
        }

        public void PreviousTheme()
        {
            SetTheme(currentIndex - 1);
        }
    }
}
