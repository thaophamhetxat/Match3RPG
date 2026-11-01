using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Board
{
    public sealed class BoardRageFireVFX : MaskableGraphic
    {
        const int FlameCount = 40;
        const int EmberCount = 12;
        const float FadeInDuration = .22f;
        const float FadeOutDuration = .30f;
        const float EmberTravelAllowance = 18f;

        struct Flame
        {
            public Vector2 Normal;
            public Vector2 Tangent;
            public float Along;
            public float WidthScale;
            public float HeightScale;
            public float Flicker;
            public float TargetFlicker;
            public float Rotation;
            public float TargetRotation;
            public float NextFlicker;
            public float Phase;
        }

        struct Ember
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Age;
            public float Lifetime;
            public float Size;
            public bool Active;
        }

        readonly Flame[] flames = new Flame[FlameCount];
        readonly Ember[] embers = new Ember[EmberCount];
        System.Random random;
        Vector2 frameSize;
        float cellSize;
        float flameEnvelope;
        float opacity;
        float targetOpacity;
        float transitionDuration;
        bool isRageReady;
        float readyTime;

        public bool IsRageReady => isRageReady;
        public float Opacity => opacity;
        public int ActiveEmberCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < embers.Length; index++)
                    if (embers[index].Active) count++;
                return count;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            maskable = false;
            random = new System.Random(unchecked(Environment.TickCount * 397
                ^ Interlocked.Increment(ref nextSeed)));
            enabled = false;
        }

        static int nextSeed;

        public void Configure(RectTransform frame, float referenceCellSize)
        {
            if (frame == null)
            {
                Debug.LogError("BoardRageFireVFX requires the BoardFrame RectTransform.", this);
                return;
            }

            cellSize = Mathf.Max(1f, referenceCellSize);
            frameSize = frame.rect.size;
            flameEnvelope = Mathf.Clamp(cellSize * .18f, 8f, 16f);
            RectTransform ownRect = rectTransform;
            ownRect.anchorMin = ownRect.anchorMax = new Vector2(.5f, .5f);
            ownRect.pivot = new Vector2(.5f, .5f);
            ownRect.anchoredPosition = frame.anchoredPosition;
            ownRect.sizeDelta = frameSize
                + Vector2.one * (flameEnvelope + EmberTravelAllowance) * 2f;
            InitializeFlames();
            if (isRageReady)
                InitializeEmbers();
            raycastTarget = false;
            SetVerticesDirty();
        }

        public void SetRageReady(bool ready)
        {
            if (isRageReady == ready)
                return;

            isRageReady = ready;
            targetOpacity = ready ? 1f : 0f;
            transitionDuration = ready ? FadeInDuration : FadeOutDuration;
            if (ready)
            {
                readyTime = Time.unscaledTime;
                InitializeEmbers();
            }

            enabled = true;
            SetVerticesDirty();
        }

        void Update()
        {
            if (random == null)
                return;

            float deltaTime = Time.unscaledDeltaTime;
            opacity = Mathf.MoveTowards(opacity, targetOpacity,
                deltaTime / Mathf.Max(.01f, transitionDuration));

            float now = Time.unscaledTime;
            for (int index = 0; index < flames.Length; index++)
            {
                Flame flame = flames[index];
                flame.Phase += deltaTime * RandomPhaseSpeed(index);
                if (now >= flame.NextFlicker)
                {
                    flame.TargetFlicker = RandomRange(.40f, .80f);
                    flame.TargetRotation = RandomRange(-5f, 5f);
                    flame.NextFlicker = now + RandomRange(.06f, .14f);
                }
                flame.Flicker = Mathf.Lerp(flame.Flicker, flame.TargetFlicker,
                    1f - Mathf.Exp(-deltaTime * 18f));
                flame.Rotation = Mathf.Lerp(flame.Rotation, flame.TargetRotation,
                    1f - Mathf.Exp(-deltaTime * 12f));
                flames[index] = flame;
            }

            for (int index = 0; index < embers.Length; index++)
                UpdateEmber(index, deltaTime);

            if (opacity <= 0f && targetOpacity <= 0f)
            {
                opacity = 0f;
                enabled = false;
                for (int index = 0; index < embers.Length; index++)
                    embers[index].Active = false;
            }
            SetVerticesDirty();
        }

        void InitializeFlames()
        {
            float now = Time.unscaledTime;
            for (int index = 0; index < flames.Length; index++)
            {
                int side;
                int sideIndex;
                int sideCount;
                if (index < 10)
                {
                    side = 0;
                    sideIndex = index;
                    sideCount = 10;
                }
                else if (index < 20)
                {
                    side = 1;
                    sideIndex = index - 10;
                    sideCount = 10;
                }
                else if (index < 28)
                {
                    side = 2;
                    sideIndex = index - 20;
                    sideCount = 8;
                }
                else if (index < 36)
                {
                    side = 3;
                    sideIndex = index - 28;
                    sideCount = 8;
                }
                else
                {
                    side = 4;
                    sideIndex = index - 36;
                    sideCount = 4;
                }

                Vector2 normal;
                Vector2 tangent;
                float along = sideCount == 1 ? 0f
                    : Mathf.Lerp(-.88f, .88f, (float)sideIndex / (sideCount - 1));
                switch (side)
                {
                    case 0: normal = Vector2.up; tangent = Vector2.right; break;
                    case 1: normal = Vector2.down; tangent = Vector2.right; break;
                    case 2: normal = Vector2.left; tangent = Vector2.up; break;
                    case 3: normal = Vector2.right; tangent = Vector2.up; break;
                    default:
                        Vector2 corner = new Vector2((sideIndex == 0 || sideIndex == 3) ? -1f : 1f,
                            sideIndex < 2 ? 1f : -1f);
                        normal = corner.normalized;
                        tangent = new Vector2(-normal.y, normal.x);
                        along = 1f;
                        break;
                }

                float flicker = RandomRange(.40f, .80f);
                flames[index] = new Flame
                {
                    Normal = normal,
                    Tangent = tangent,
                    Along = along,
                    WidthScale = RandomRange(.82f, 1.18f),
                    HeightScale = RandomRange(.68f, .98f),
                    Flicker = flicker,
                    TargetFlicker = flicker,
                    Rotation = RandomRange(-5f, 5f),
                    TargetRotation = RandomRange(-5f, 5f),
                    NextFlicker = now + RandomRange(.06f, .14f),
                    Phase = RandomRange(0f, Mathf.PI * 2f)
                };
            }
        }

        void InitializeEmbers()
        {
            for (int index = 0; index < embers.Length; index++)
            {
                SpawnEmber(index);
                Ember ember = embers[index];
                ember.Age = RandomRange(0f, ember.Lifetime * .8f);
                embers[index] = ember;
            }
        }

        void UpdateEmber(int index, float deltaTime)
        {
            Ember ember = embers[index];
            if (!ember.Active)
            {
                if (isRageReady)
                    SpawnEmber(index);
                return;
            }

            ember.Age += deltaTime;
            ember.Position += ember.Velocity * deltaTime;
            if (ember.Age >= ember.Lifetime)
            {
                ember.Active = false;
                if (isRageReady)
                    SpawnEmber(index);
                else
                    embers[index] = ember;
                return;
            }
            embers[index] = ember;
        }

        void SpawnEmber(int index)
        {
            float halfWidth = frameSize.x * .5f;
            float halfHeight = frameSize.y * .5f;
            int side = index % 4;
            Vector2 position;
            Vector2 velocity;
            switch (side)
            {
                case 0:
                    position = new Vector2(RandomRange(-halfWidth, halfWidth), halfHeight);
                    velocity = new Vector2(RandomRange(-8f, 8f), RandomRange(12f, 22f));
                    break;
                case 1:
                    position = new Vector2(RandomRange(-halfWidth, halfWidth), -halfHeight);
                    velocity = new Vector2(RandomRange(-8f, 8f), RandomRange(12f, 22f));
                    break;
                case 2:
                    position = new Vector2(-halfWidth, RandomRange(-halfHeight, halfHeight));
                    velocity = new Vector2(RandomRange(-6f, 6f), RandomRange(12f, 22f));
                    break;
                default:
                    position = new Vector2(halfWidth, RandomRange(-halfHeight, halfHeight));
                    velocity = new Vector2(RandomRange(-6f, 6f), RandomRange(12f, 22f));
                    break;
            }

            embers[index] = new Ember
            {
                Position = position,
                Velocity = velocity,
                Age = 0f,
                Lifetime = RandomRange(.45f, .75f),
                Size = RandomRange(1.5f, 3f),
                Active = true
            };
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (opacity <= 0f || frameSize.x <= 0f || frameSize.y <= 0f)
                return;

            AddFrameGlow(vertexHelper);
            for (int index = 0; index < flames.Length; index++)
                AddFlame(vertexHelper, flames[index]);
            for (int index = 0; index < embers.Length; index++)
                if (embers[index].Active)
                    AddEmber(vertexHelper, embers[index]);
        }

        void AddFrameGlow(VertexHelper vertexHelper)
        {
            float halfWidth = frameSize.x * .5f;
            float halfHeight = frameSize.y * .5f;
            float width = Mathf.Clamp(cellSize * .025f, 1.5f, 3f);
            float glowAlpha = (.35f + .10f * Mathf.Sin(
                (Time.unscaledTime - readyTime) * Mathf.PI * 2f / .82f)) * opacity;
            Color inner = new Color(1f, .46f, .10f, glowAlpha);
            Color outer = new Color(1f, .16f, .035f, 0f);

            AddQuad(vertexHelper,
                new Vector2(-halfWidth, halfHeight),
                new Vector2(halfWidth, halfHeight),
                new Vector2(halfWidth, halfHeight + width),
                new Vector2(-halfWidth, halfHeight + width), inner, outer);
            AddQuad(vertexHelper,
                new Vector2(-halfWidth, -halfHeight - width),
                new Vector2(halfWidth, -halfHeight - width),
                new Vector2(halfWidth, -halfHeight),
                new Vector2(-halfWidth, -halfHeight), outer, inner);
            AddQuad(vertexHelper,
                new Vector2(-halfWidth - width, -halfHeight),
                new Vector2(-halfWidth, -halfHeight),
                new Vector2(-halfWidth, halfHeight),
                new Vector2(-halfWidth - width, halfHeight), outer, inner);
            AddQuad(vertexHelper,
                new Vector2(halfWidth, -halfHeight),
                new Vector2(halfWidth + width, -halfHeight),
                new Vector2(halfWidth + width, halfHeight),
                new Vector2(halfWidth, halfHeight), inner, outer);
        }

        void AddFlame(VertexHelper vertexHelper, Flame flame)
        {
            Vector2 halfSize = frameSize * .5f;
            Vector2 basePoint;
            if (flame.Normal == Vector2.up || flame.Normal == Vector2.down)
                basePoint = new Vector2(flame.Along * halfSize.x, flame.Normal.y * halfSize.y);
            else if (flame.Normal == Vector2.left || flame.Normal == Vector2.right)
                basePoint = new Vector2(flame.Normal.x * halfSize.x, flame.Along * halfSize.y);
            else
                basePoint = new Vector2(flame.Normal.x * halfSize.x * 1.4142f,
                    flame.Normal.y * halfSize.y * 1.4142f);

            float flicker = flame.Flicker * opacity;
            float width = cellSize * .085f * flame.WidthScale;
            float height = flameEnvelope * flame.HeightScale
                * Mathf.Lerp(.90f, 1.15f, flame.Flicker);
            float radians = flame.Rotation * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            Vector2 tangent = Rotate(flame.Tangent, sin, cos);
            Vector2 normal = Rotate(flame.Normal, sin, cos);
            float sway = Mathf.Sin(flame.Phase) * width * .18f;
            Vector2 center = basePoint + tangent * sway;

            AddFlameShape(vertexHelper, center, tangent, normal,
                width, height, new Color32(235, 60, 20, 255), flicker);
            AddFlameShape(vertexHelper, center, tangent, normal,
                width * .58f, height * .69f, new Color32(255, 139, 35, 255), flicker);
        }

        static void AddFlameShape(VertexHelper vertexHelper, Vector2 center,
            Vector2 tangent, Vector2 normal, float width, float height, Color body, float alpha)
        {
            Vector2 p0 = center - tangent * width * .50f;
            Vector2 p1 = center - tangent * width * .43f + normal * height * .34f;
            Vector2 p2 = center - tangent * width * .25f + normal * height * .72f;
            Vector2 p3 = center + normal * height;
            Vector2 p4 = center + tangent * width * .25f + normal * height * .72f;
            Vector2 p5 = center + tangent * width * .43f + normal * height * .34f;
            Vector2 p6 = center + tangent * width * .50f;
            int start = vertexHelper.currentVertCount;
            Color core = Color.Lerp(body, new Color32(255, 243, 170, 255), .78f);
            AddVertex(vertexHelper, center + normal * height * .34f,
                WithAlpha(body, alpha * .78f));
            AddVertex(vertexHelper, p0, WithAlpha(body, alpha * .88f));
            AddVertex(vertexHelper, p1, WithAlpha(body, alpha * .88f));
            AddVertex(vertexHelper, p2, WithAlpha(body, alpha * .88f));
            AddVertex(vertexHelper, p3, WithAlpha(core, alpha));
            AddVertex(vertexHelper, p4, WithAlpha(body, alpha * .88f));
            AddVertex(vertexHelper, p5, WithAlpha(body, alpha * .88f));
            AddVertex(vertexHelper, p6, WithAlpha(body, alpha * .88f));
            for (int index = 0; index < 7; index++)
                vertexHelper.AddTriangle(start, start + index + 1, start + (index + 1) % 7 + 1);
        }

        void AddEmber(VertexHelper vertexHelper, Ember ember)
        {
            float progress = Mathf.Clamp01(ember.Age / ember.Lifetime);
            float alpha = opacity * Mathf.Sin(progress * Mathf.PI) * .8f;
            Color color = new Color(1f, Mathf.Lerp(.28f, .78f, progress), .10f, alpha);
            float size = ember.Size * (1f - progress * .35f);
            Vector2 center = ember.Position;
            int start = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, center + Vector2.up * size, color);
            AddVertex(vertexHelper, center + Vector2.right * size, WithAlpha(color, alpha * .55f));
            AddVertex(vertexHelper, center + Vector2.down * size, color);
            AddVertex(vertexHelper, center + Vector2.left * size, WithAlpha(color, alpha * .55f));
            vertexHelper.AddTriangle(start, start + 1, start + 2);
            vertexHelper.AddTriangle(start, start + 2, start + 3);
        }

        static void AddQuad(VertexHelper vertexHelper, Vector2 bottomLeft, Vector2 bottomRight,
            Vector2 topRight, Vector2 topLeft, Color inner, Color outer)
        {
            int start = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, bottomLeft, inner);
            AddVertex(vertexHelper, bottomRight, inner);
            AddVertex(vertexHelper, topRight, outer);
            AddVertex(vertexHelper, topLeft, outer);
            vertexHelper.AddTriangle(start, start + 1, start + 2);
            vertexHelper.AddTriangle(start, start + 2, start + 3);
        }

        static void AddVertex(VertexHelper vertexHelper, Vector2 position, Color color)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vertexHelper.AddVert(vertex);
        }

        static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        static Vector2 Rotate(Vector2 vector, float sin, float cos) =>
            new Vector2(vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos);

        float RandomRange(float minimum, float maximum) =>
            minimum + (float)random.NextDouble() * (maximum - minimum);

        float RandomPhaseSpeed(int index) => 2.2f + (index % 7) * .25f;
    }
}
