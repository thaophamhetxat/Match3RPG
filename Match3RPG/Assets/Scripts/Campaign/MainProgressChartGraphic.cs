using System;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Campaign
{
    public sealed class MainProgressChartGraphic : MaskableGraphic, IPointerClickHandler
    {
        const int BranchCount = 7;
        public const int StarsPerBranch = 10;
        const int CircleSegments = 32;

        static readonly Color[] BranchColors =
        {
            new Color32(242, 111, 75, 255),
            new Color32(94, 167, 229, 255),
            new Color32(177, 123, 221, 255),
            new Color32(233, 199, 115, 255),
            new Color32(92, 207, 151, 255),
            new Color32(156, 207, 226, 255),
            new Color32(218, 222, 231, 255)
        };

        static readonly Color LockedColor = new Color32(82, 91, 104, 210);
        static readonly float[] BranchRootXs =
        {
            .085f, .22f, .35f, .49f, .63f, .77f, .91f
        };
        static readonly Vector2[][] BranchStarLayouts =
        {
            new[]
            {
                new Vector2(.085f, .74f), new Vector2(.085f, .66f), new Vector2(.065f, .56f),
                new Vector2(.045f, .43f), new Vector2(.025f, .30f), new Vector2(.105f, .56f),
                new Vector2(.125f, .45f), new Vector2(.145f, .34f), new Vector2(.155f, .23f),
                new Vector2(.165f, .12f)
            },
            new[]
            {
                new Vector2(.22f, .74f), new Vector2(.22f, .66f), new Vector2(.22f, .57f),
                new Vector2(.22f, .49f), new Vector2(.19f, .40f), new Vector2(.17f, .31f),
                new Vector2(.25f, .40f), new Vector2(.27f, .31f), new Vector2(.29f, .22f),
                new Vector2(.31f, .13f)
            },
            new[]
            {
                new Vector2(.35f, .74f), new Vector2(.35f, .66f), new Vector2(.35f, .57f),
                new Vector2(.32f, .48f), new Vector2(.30f, .39f), new Vector2(.28f, .30f),
                new Vector2(.38f, .48f), new Vector2(.40f, .37f), new Vector2(.42f, .26f),
                new Vector2(.44f, .15f)
            },
            new[]
            {
                new Vector2(.49f, .74f), new Vector2(.49f, .66f), new Vector2(.49f, .57f),
                new Vector2(.49f, .49f), new Vector2(.49f, .40f), new Vector2(.46f, .31f),
                new Vector2(.43f, .22f), new Vector2(.53f, .31f), new Vector2(.56f, .20f),
                new Vector2(.59f, .09f)
            },
            new[]
            {
                new Vector2(.63f, .74f), new Vector2(.61f, .65f), new Vector2(.59f, .55f),
                new Vector2(.57f, .45f), new Vector2(.66f, .65f), new Vector2(.69f, .56f),
                new Vector2(.72f, .47f), new Vector2(.75f, .38f), new Vector2(.78f, .29f),
                new Vector2(.81f, .20f)
            },
            new[]
            {
                new Vector2(.77f, .74f), new Vector2(.77f, .66f), new Vector2(.77f, .57f),
                new Vector2(.74f, .48f), new Vector2(.72f, .38f), new Vector2(.81f, .48f),
                new Vector2(.84f, .38f), new Vector2(.87f, .29f), new Vector2(.90f, .20f),
                new Vector2(.93f, .11f)
            },
            new[]
            {
                new Vector2(.91f, .74f), new Vector2(.91f, .66f), new Vector2(.87f, .56f),
                new Vector2(.85f, .45f), new Vector2(.91f, .56f), new Vector2(.91f, .45f),
                new Vector2(.94f, .56f), new Vector2(.95f, .45f), new Vector2(.96f, .34f),
                new Vector2(.96f, .23f)
            }
        };
        static readonly int[] BranchForkStarIndices = { 1, 3, 2, 4, 0, 2, 1 };
        static readonly int[] BranchSecondArmStartIndices = { 5, 6, 6, 7, 4, 5, 6 };
        static readonly bool[][] TrigramPatterns =
        {
            new[] { true, true, true },
            new[] { true, true, false },
            new[] { true, false, true },
            new[] { true, false, false },
            new[] { false, true, true },
            new[] { false, true, false },
            new[] { false, false, true },
            new[] { false, false, false }
        };

        int[] litStarCounts = new int[BranchCount];
        int selectedBranch = -1;
        int selectedStar = -1;

        public event Action<int, int> StarClicked;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = true;
        }

        public void SetLitStarCounts(int[] counts)
        {
            if (counts == null || counts.Length != BranchCount)
                throw new ArgumentException(
                    $"Star chart requires exactly {BranchCount} branch star counts.",
                    nameof(counts));

            litStarCounts = (int[])counts.Clone();
            for (int branch = 0; branch < litStarCounts.Length; branch++)
                litStarCounts[branch] = Mathf.Clamp(litStarCounts[branch], 0,
                    StarsPerBranch);
            SetVerticesDirty();
        }

        public void SetSelectedStar(int branchIndex, int starIndex)
        {
            selectedBranch = branchIndex;
            selectedStar = starIndex;
            SetVerticesDirty();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, eventData.position, eventData.pressEventCamera,
                out Vector2 localPosition))
                return;

            Rect rect = rectTransform.rect;
            float hitRadius = Mathf.Min(rect.width, rect.height) * .055f;
            float closestDistance = hitRadius;
            int hitBranch = -1;
            int hitStar = -1;
            for (int branch = 0; branch < BranchCount; branch++)
                for (int star = 0; star < StarsPerBranch; star++)
                {
                    float distance = Vector2.Distance(localPosition,
                        GetStarPosition(rect, branch, star));
                    if (distance > closestDistance)
                        continue;
                    closestDistance = distance;
                    hitBranch = branch;
                    hitStar = star;
                }

            if (hitBranch >= 0)
                StarClicked?.Invoke(hitBranch, hitStar);
            else
                StarClicked?.Invoke(-1, -1);
        }

        public static Vector2 GetStarPosition(Rect rect, int branchIndex, int starIndex)
        {
            if (branchIndex < 0 || branchIndex >= BranchStarLayouts.Length)
                throw new ArgumentOutOfRangeException(nameof(branchIndex));
            if (starIndex < 0 || starIndex >= BranchStarLayouts[branchIndex].Length)
                throw new ArgumentOutOfRangeException(nameof(starIndex));

            Vector2 relative = BranchStarLayouts[branchIndex][starIndex];
            return new Vector2(
                rect.xMin + rect.width * relative.x,
                rect.yMin + rect.height * (relative.y + .05f));
        }

        static Vector2 GetBranchRootPosition(Rect rect, int branchIndex)
        {
            float rootX = rect.xMin + rect.width * BranchRootXs[branchIndex];
            float rootY = rect.yMin + rect.height * .83f;
            return new Vector2(rootX, rootY);
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = rectTransform.rect;
            float unit = Mathf.Min(rect.width, rect.height);
            if (unit <= 0f)
                return;

            for (int branch = 0; branch < BranchCount; branch++)
            {
                int litCount = litStarCounts[branch];
                Vector2 firstPosition = GetStarPosition(rect, branch, 0);
                Vector2 rootPosition = GetBranchRootPosition(rect, branch);
                Color firstSegmentColor = litCount > 0 ? BranchColors[branch] : LockedColor;
                firstSegmentColor.a *= litCount > 0 ? .82f : .62f;
                AddLine(vertexHelper, rootPosition, firstPosition, unit * .008f,
                    firstSegmentColor);

                for (int star = 0; star < StarsPerBranch; star++)
                {
                    Vector2 position = GetStarPosition(rect, branch, star);
                    if (star > 0)
                    {
                        Vector2 previous = GetStarPosition(rect, branch, star - 1);
                        bool segmentLit = star < litCount;
                        Color lineColor = segmentLit ? BranchColors[branch] : LockedColor;
                        lineColor.a *= segmentLit ? .82f : .62f;
                        int secondArmStart = BranchSecondArmStartIndices[branch];
                        if (star == secondArmStart)
                        {
                            Vector2 fork = GetStarPosition(rect, branch,
                                BranchForkStarIndices[branch]);
                            AddLine(vertexHelper, fork, position, unit * .008f,
                                lineColor);
                        }
                        else
                        {
                            AddLine(vertexHelper, previous, position, unit * .008f,
                                lineColor);
                        }
                    }

                    bool lit = star < litCount;
                    Color starColor = lit ? BranchColors[branch]
                        : star == 0
                            ? new Color32(255, 230, 160, 255)
                            : LockedColor;
                    if (star == 0)
                    {
                        AddDisc(vertexHelper, position, unit * .030f,
                            new Color32(255, 215, 120, 55));
                        AddRing(vertexHelper, position, unit * .026f,
                            unit * .004f, new Color32(255, 215, 120, 235));
                    }
                    AddStar(vertexHelper, position, unit * .019f,
                        starColor * color);
                    if (branch == selectedBranch && star == selectedStar)
                        AddRing(vertexHelper, position, unit * .032f,
                            unit * .004f, BranchColors[branch] * color);
                }
            }

            DrawBagua(vertexHelper, rect);
        }

        static void AddRing(VertexHelper vertexHelper, Vector2 center,
            float radius, float thickness, Color ringColor)
        {
            for (int segment = 0; segment < 12; segment++)
            {
                float startAngle = segment * Mathf.PI * 2f / 12f;
                float endAngle = (segment + .78f) * Mathf.PI * 2f / 12f;
                Vector2 start = center + new Vector2(Mathf.Cos(startAngle),
                    Mathf.Sin(startAngle)) * radius;
                Vector2 end = center + new Vector2(Mathf.Cos(endAngle),
                    Mathf.Sin(endAngle)) * radius;
                AddLine(vertexHelper, start, end, thickness, ringColor);
            }
        }

        static void DrawBagua(VertexHelper vertexHelper, Rect rect)
        {
            float unit = Mathf.Min(rect.width, rect.height);
            Vector2 center = new Vector2(rect.center.x, rect.yMin + rect.height * .93f);
            float outerRadius = unit * .048f;
            float innerRadius = unit * .040f;
            AddDisc(vertexHelper, center, outerRadius, new Color32(225, 190, 111, 255));
            AddDisc(vertexHelper, center, innerRadius, new Color32(16, 20, 28, 255));

            for (int trigram = 0; trigram < TrigramPatterns.Length; trigram++)
            {
                float angle = Mathf.PI * .5f + trigram * Mathf.PI * 2f
                    / TrigramPatterns.Length;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 tangent = new Vector2(-direction.y, direction.x);
                Vector2 trigramCenter = center + direction * unit * .032f;
                for (int bar = 0; bar < TrigramPatterns[trigram].Length; bar++)
                {
                    Vector2 barCenter = trigramCenter
                        + direction * ((bar - 1) * unit * .0065f);
                    if (TrigramPatterns[trigram][bar])
                        AddLine(vertexHelper, barCenter - tangent * unit * .010f,
                            barCenter + tangent * unit * .010f, unit * .004f,
                            new Color32(235, 219, 174, 255));
                    else
                    {
                        AddLine(vertexHelper, barCenter - tangent * unit * .010f,
                            barCenter - tangent * unit * .002f, unit * .004f,
                            new Color32(235, 219, 174, 255));
                        AddLine(vertexHelper, barCenter + tangent * unit * .002f,
                            barCenter + tangent * unit * .010f, unit * .004f,
                            new Color32(235, 219, 174, 255));
                    }
                }
            }

            float yinYangRadius = unit * .018f;
            AddDisc(vertexHelper, center, yinYangRadius, new Color32(232, 226, 205, 255));
            DrawHalfDisc(vertexHelper, center, yinYangRadius, true,
                new Color32(24, 29, 38, 255));
            DrawHalfDisc(vertexHelper, center, yinYangRadius, false,
                new Color32(232, 226, 205, 255));
            AddDisc(vertexHelper, center + Vector2.up * yinYangRadius * .48f,
                yinYangRadius * .16f, new Color32(232, 226, 205, 255));
            AddDisc(vertexHelper, center + Vector2.down * yinYangRadius * .48f,
                yinYangRadius * .16f, new Color32(24, 29, 38, 255));

            float barY = rect.yMin + rect.height * .83f;
            float leftX = rect.xMin + rect.width * .08f;
            float rightX = rect.xMin + rect.width * .92f;
            float centerX = rect.center.x;
            AddLine(vertexHelper, new Vector2(leftX, barY),
                new Vector2(rightX, barY), unit * .006f,
                new Color32(232, 195, 104, 255));
            AddLine(vertexHelper, new Vector2(centerX, center.y),
                new Vector2(centerX, barY), unit * .006f,
                new Color32(232, 195, 104, 255));
        }

        static void AddLine(VertexHelper vertexHelper, Vector2 start, Vector2 end,
            float thickness, Color lineColor)
        {
            Vector2 direction = (end - start).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x)
                * thickness * .5f;
            int first = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, start - perpendicular, lineColor);
            AddVertex(vertexHelper, start + perpendicular, lineColor);
            AddVertex(vertexHelper, end + perpendicular, lineColor);
            AddVertex(vertexHelper, end - perpendicular, lineColor);
            vertexHelper.AddTriangle(first, first + 1, first + 2);
            vertexHelper.AddTriangle(first, first + 2, first + 3);
        }

        static void AddDisc(VertexHelper vertexHelper, Vector2 center, float radius,
            Color discColor)
        {
            int centerIndex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, center, discColor);
            for (int segment = 0; segment <= CircleSegments; segment++)
            {
                float angle = segment * Mathf.PI * 2f / CircleSegments;
                AddVertex(vertexHelper, center + new Vector2(Mathf.Cos(angle),
                    Mathf.Sin(angle)) * radius, discColor);
            }

            for (int segment = 0; segment < CircleSegments; segment++)
                vertexHelper.AddTriangle(centerIndex, centerIndex + segment + 1,
                    centerIndex + segment + 2);
        }

        static void DrawHalfDisc(VertexHelper vertexHelper, Vector2 center, float radius,
            bool rightHalf, Color discColor)
        {
            int centerIndex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, center, discColor);
            float startAngle = rightHalf ? -Mathf.PI * .5f : Mathf.PI * .5f;
            for (int segment = 0; segment <= CircleSegments / 2; segment++)
            {
                float angle = startAngle + segment * Mathf.PI / CircleSegments;
                AddVertex(vertexHelper, center + new Vector2(Mathf.Cos(angle),
                    Mathf.Sin(angle)) * radius, discColor);
            }

            for (int segment = 0; segment < CircleSegments / 2; segment++)
                vertexHelper.AddTriangle(centerIndex, centerIndex + segment + 1,
                    centerIndex + segment + 2);
        }

        static void AddStar(VertexHelper vertexHelper, Vector2 center, float radius,
            Color starColor)
        {
            int centerIndex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, center, new Color(16f / 255f, 20f / 255f,
                28f / 255f, starColor.a));
            for (int point = 0; point <= 10; point++)
            {
                float pointRadius = point % 2 == 0 ? radius : radius * .43f;
                float angle = -Mathf.PI * .5f + point * Mathf.PI / 5f;
                AddVertex(vertexHelper, center + new Vector2(Mathf.Cos(angle),
                    Mathf.Sin(angle)) * pointRadius, starColor);
            }

            for (int point = 0; point < 10; point++)
                vertexHelper.AddTriangle(centerIndex, centerIndex + point + 1,
                    centerIndex + point + 2);
        }

        static void AddVertex(VertexHelper vertexHelper, Vector2 position, Color vertexColor)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = vertexColor;
            vertexHelper.AddVert(vertex);
        }
    }
}
