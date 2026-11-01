#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Match3RPG.Board;
using Match3RPG.Campaign.Data;
using Match3RPG.Core;
using Match3RPG.Persistence;
using Match3RPG.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Battle
{
    public static class TurnBattleDebugChecks
    {
        const int DebugBoardRows = 7;
        const int DebugBoardColumns = 7;

        public static void RunAll(TurnManager activeTurnManager)
        {
            if (!Application.isPlaying || activeTurnManager == null)
            {
                Debug.LogError("Run deterministic turn battle checks from a TurnManager while the game is in Play Mode.");
                return;
            }
            SpecialGemResolver resolver = new SpecialGemResolver();
            CheckMatchThreeEndsTurn(resolver);
            CheckMatchFourCreatesLinesWithoutExtraTurn(resolver);
            CheckMatchFiveCreatesRainbow(resolver);
            CheckMatchSixCreatesRainbow(resolver);
            CheckMatchPriorityAndMultipleSpecials(resolver);
            CheckTLShapeSpecialCreation(resolver);
            CheckLineClearsCross(resolver);
            CheckExtraTurnRules(resolver);
            CheckSevenBySevenBoard();
            CheckSelectionArrowDirections();
            CheckSingleColumnGravityPreservesUnaffectedColumns();
            CheckBattleGemValues();
            CheckShieldBlocksSoulDamage();
            CheckSoulLevelRules();
            CheckRageAndMultiHitCombat();
            CheckRageHud();
            CheckCascadeMatchFourDoesNotGrantExtraTurn(resolver);
            CheckLineSpecialRequiresMatch(resolver);
            CheckLightningBeamGeometry();
            CheckGemSpriteMappings();
            CheckDirectAreaBomb(resolver);
            CheckLightningAndBaguaActivation(resolver);
            CheckBaguaClickConsumesGem();
            CheckBonusSpecialRollOdds();
            CheckRainbowSwapClearsColor(resolver);
            CheckRainbowRequiresSwap(resolver);
            CheckTimerAndStoredTurns();
            CheckTimeoutConsumesTurn();
            CheckScannerDoesNotMutate();
            CheckInvalidSwapReturnsGems();
            CheckShufflePreservesGemData(activeTurnManager);
            CheckBossMoveUsesBoardPipeline();
            CheckBossDamageAndBattleEnd();
            CheckCharacterAnimationPositions();
            CheckHeroScaleTransitions();
            CheckDeathAnimationPriority();
            CheckInvalidMovePenalty();
            CheckPlayerInputStates();
            Debug.Log("All deterministic Player-vs-Boss rule checks passed.");
        }

        public static void RunBoardConservationRegression()
        {
            CheckMatchPriorityAndMultipleSpecials(new SpecialGemResolver());
            CheckSingleColumnGravityPreservesUnaffectedColumns();
            CheckSpecialGemGravityPreservesState();
            CheckGemSpriteMappings();
            CheckGemSpriteFillsSquareCell();
            CheckSelectionArrowDirections();
            CheckSpecialGemIdleVFX();
            CheckBoardRageFireBinding();
            CheckIdleLoopClips();
            CheckSharedBattleGroundLine();
            CheckBattleLayoutGridLayout();
            CheckMatchFourCreatesLinesWithoutExtraTurn(new SpecialGemResolver());
            CheckMatchFiveCreatesRainbow(new SpecialGemResolver());
            CheckMatchSixCreatesRainbow(new SpecialGemResolver());
            CheckTLShapeSpecialCreation(new SpecialGemResolver());
            CheckLineClearsCross(new SpecialGemResolver());
            CheckExtraTurnRules(new SpecialGemResolver());
            CheckCascadeMatchFourDoesNotGrantExtraTurn(new SpecialGemResolver());
            CheckCreatedSpecialVisualsRefresh();
            CheckBoardCreatedSpecialsPersist();
            CheckLightningBeamGeometry();
            Debug.Log("Deterministic board conservation and match-priority regressions passed.");
        }

        public static void RunGemFocusPresentationRegression()
        {
            CheckGemSpriteFillsSquareCell();
            CheckSelectionArrowDirections();
            Debug.Log("Gem focus visual and 7x7 arrow-direction regressions passed.");
        }

        public static void RunBattlePresentationRegression()
        {
            CheckBoardRageFireBinding();
            CheckIdleLoopClips();
            CheckSharedBattleGroundLine();
            Debug.Log("Deterministic Battle presentation regressions passed.");
        }

        static void CheckMatchThreeEndsTurn(SpecialGemResolver resolver)
        {
            BoardSnapshot board = LineBoard(3, true, GemSpecialType.None);
            MatchResult match = resolver.ResolveCurrent(board, new MoveResult(new BoardPos(0, 0), new BoardPos(0, 1)), true, 1, false);
            Require(match.LongestMatch == 3 && !match.GrantsExtraTurn, "Match-3 must not grant an extra turn.");
            Require(TurnManager.GetStateAfterMove(BattleActor.Player, false) == BattleTurnState.BossThinking,
                "A normal player move must pass control to the Boss.");
        }

        static void CheckMatchFourCreatesLinesWithoutExtraTurn(SpecialGemResolver resolver)
        {
            MatchResult match = resolver.ResolveCurrent(LineBoard(4, true, GemSpecialType.None),
                new MoveResult(new BoardPos(0, 1), new BoardPos(0, 2)), true, 1, false);
            Require(match.CreatedSpecial == GemSpecialType.Line && !match.GrantsExtraTurn,
                "Initial horizontal Match-4 must create the shared Line without granting an extra turn.");
            Require(match.CreatedLineCount == 1 && match.CreatedAreaBombCount == 0
                && match.CreatedRainbowCount == 0 && match.ExtraTurnsGranted == 0,
                "Horizontal Match-4 must create exactly one Line, no Bomb or Rainbow, and no Extra Turn.");
            Require(match.SpecialPosition.Equals(new BoardPos(0, 2)),
                "A created special must use the swap destination when it belongs to the match.");
            MoveResult action = new MoveResult(new BoardPos(0, 1), new BoardPos(0, 2));
            action.AddMatch(match);
            Require(action.ExtraTurnsGranted == 0 && action.CreatedMatch4,
                "A Player initial Match-4 must not grant a stored turn.");
            Require(TurnManager.GetStateAfterMove(BattleActor.Player, match.GrantsExtraTurn) == BattleTurnState.BossThinking,
                "Player Match-4 must pass control to the Boss.");

            MatchResult vertical = resolver.ResolveCurrent(LineBoard(4, false, GemSpecialType.None),
                new MoveResult(new BoardPos(1, 0), new BoardPos(2, 0)), true, 1, false);
            Require(vertical.CreatedSpecial == GemSpecialType.Line && !vertical.GrantsExtraTurn,
                "Initial vertical Match-4 must create the shared Line without granting an extra turn.");
            Require(vertical.CreatedLineCount == 1 && vertical.CreatedAreaBombCount == 0
                && vertical.CreatedRainbowCount == 0 && vertical.ExtraTurnsGranted == 0,
                "Vertical Match-4 must create exactly one Line, no Bomb or Rainbow, and no Extra Turn.");
        }

        static void CheckMatchFiveCreatesRainbow(SpecialGemResolver resolver)
        {
            MatchResult match = resolver.ResolveCurrent(LineBoard(5, true, GemSpecialType.None),
                new MoveResult(new BoardPos(0, 2), new BoardPos(0, 3)), true, 1, false);
            Require(match.CreatedSpecial == GemSpecialType.Rainbow
                && match.CreatedGemType == GemType.Rainbow && match.GrantsExtraTurn
                && match.CreatedAreaBombCount == 0 && match.CreatedRainbowCount == 1
                && match.CreatedLineCount == 0
                && match.ExtraTurnsGranted == 1,
                "Initial Match-5 must create a Rainbow and grant an extra turn.");
            MoveResult action = new MoveResult(new BoardPos(0, 2), new BoardPos(0, 3));
            action.AddMatch(match);
            Require(action.ExtraTurnsGranted == 1 && action.CreatedMatch5
                && match.CreatedRainbowCount == 1,
                "A Player initial Match-5 must grant exactly one stored turn.");
        }

        static void CheckMatchSixCreatesRainbow(SpecialGemResolver resolver)
        {
            MatchResult match = resolver.ResolveCurrent(LineBoard(6, true, GemSpecialType.None),
                new MoveResult(new BoardPos(0, 2), new BoardPos(0, 3)), true, 1, false);
            Require(match.CreatedSpecial == GemSpecialType.Rainbow
                && match.CreatedGemType == GemType.Rainbow
                && match.GrantsExtraTurn && match.ExtraTurnsGranted == 1
                && match.CreatedRainbowCount == 1
                && match.CreatedAreaBombCount == 0
                && match.CreatedLineCount == 0
                && !match.ClearPositions.Contains(match.SpecialPosition)
                && match.Segments.Count(segment => segment.IsHorizontal && segment.Length == 6) == 1,
                "Initial Match-6 must create one Rainbow gem and grant one Extra Turn.");

            MatchResult vertical = resolver.ResolveCurrent(LineBoard(6, false, GemSpecialType.None),
                new MoveResult(new BoardPos(2, 0), new BoardPos(3, 0)), true, 1, false);
            Require(vertical.CreatedRainbowCount == 1 && vertical.CreatedAreaBombCount == 0
                && vertical.CreatedLineCount == 0 && vertical.ExtraTurnsGranted == 1
                && vertical.Segments.Count(segment => !segment.IsHorizontal && segment.Length == 6) == 1,
                "Initial vertical Match-6 must create exactly one Rainbow and grant one Extra Turn.");

            MatchResult seven = resolver.ResolveCurrent(LineBoard(7, true, GemSpecialType.None),
                new MoveResult(default, default), false, 1, false);
            Require(seven.CreatedRainbowCount == 1 && seven.CreatedAreaBombCount == 0
                && seven.ExtraTurnsGranted == 1
                && seven.Segments.Count(segment => segment.IsHorizontal && segment.Length == 7) == 1,
                "A maximal horizontal Match-7 must create one Rainbow, not multiple Bombs.");
        }

        static void CheckMatchPriorityAndMultipleSpecials(SpecialGemResolver resolver)
        {
            GemType[,] types = BaselineTypes();
            GemSpecialType[,] specials = new GemSpecialType[DebugBoardRows, DebugBoardColumns];
            SetHorizontalRun(types, 0, 0, 6, GemType.Attack);
            SetHorizontalRun(types, 2, 0, 5, GemType.Mana);
            SetHorizontalRun(types, 4, 0, 4, GemType.Health);
            SetHorizontalRun(types, 6, 0, 3, GemType.Rage);
            types[0, 6] = GemType.Mana;
            types[6, 3] = GemType.Health;
            SetHorizontalRun(types, 5, 4, 3, GemType.Rage);
            for (int row = 4; row <= 6; row++)
                types[row, 5] = GemType.Rage;

            MatchResult match = resolver.ResolveCurrent(new BoardSnapshot(types, specials),
                new MoveResult(default, default), false, 1, false);
            Require(match.Segments.Count == 6
                && match.Segments.Select(segment => segment.Length).OrderByDescending(length => length)
                    .SequenceEqual(new[] { 6, 5, 4, 3, 3, 3 }),
                "The scanner must return each maximal run once and preserve all group lengths.");
            Require(match.CreatedSpecials.Count == 4
                && match.CreatedSpecials[0].SpecialType == GemSpecialType.Rainbow
                && match.CreatedSpecials[1].SpecialType == GemSpecialType.AreaBomb
                && match.CreatedSpecials[2].SpecialType == GemSpecialType.Rainbow
                && match.CreatedSpecials[3].SpecialType == GemSpecialType.Line
                && match.CreatedTLBombCount == 1 && match.ExtraTurnsGranted == 2,
                "Special creation priority must be straight 6+, T/L, straight 5, then 4.");
            Require(match.CreatedSpecials.All(creation =>
                    !match.ClearPositions.Contains(creation.Position))
                && match.CreatedSpecials.Select(creation => creation.Position).Distinct().Count() == 4,
                "Created specials must survive the wave at unique positions.");
            Require(match.CreatedSpecials[0].Position.Equals(new BoardPos(0, 3)),
                "A cascade-created special must use the deterministic center of its maximal run.");

            types = BaselineTypes();
            SetHorizontalRun(types, 0, 1, 6, GemType.Attack);
            for (int row = 0; row < 5; row++)
                types[row, 3] = GemType.Attack;
            match = resolver.ResolveCurrent(new BoardSnapshot(types, specials),
                new MoveResult(default, default), false, 1, false);
            Require(match.CreatedSpecials.Count == 1
                && match.CreatedSpecials[0].SpecialType == GemSpecialType.Rainbow,
                "Overlapping maximal 5+ groups must not be downgraded to Lines after positions are claimed.");

        }

        static void CheckTLShapeSpecialCreation(SpecialGemResolver resolver)
        {
            MatchResult[] rotatedShapes =
            {
                ResolveIntersection(resolver, 2, 2, 3, 2, 3),
                ResolveIntersection(resolver, 4, 2, 3, 2, 3),
                ResolveIntersection(resolver, 3, 2, 2, 2, 3),
                ResolveIntersection(resolver, 3, 2, 4, 2, 3),
                ResolveIntersection(resolver, 3, 2, 2, 1, 3),
                ResolveIntersection(resolver, 3, 2, 4, 1, 3),
                ResolveIntersection(resolver, 3, 2, 2, 3, 3),
                ResolveIntersection(resolver, 3, 2, 4, 3, 3)
            };
            BoardPos[] intersections =
            {
                new BoardPos(2, 3), new BoardPos(4, 3),
                new BoardPos(3, 2), new BoardPos(3, 4),
                new BoardPos(3, 2), new BoardPos(3, 4),
                new BoardPos(3, 2), new BoardPos(3, 4)
            };
            for (int index = 0; index < rotatedShapes.Length; index++)
                RequireTLBomb(rotatedShapes[index], intersections[index],
                    $"T/L rotation {index + 1} must create an AreaBomb at its intersection.");

            MatchResult fourRunT = ResolveIntersection(resolver, 2, 1, 3, 2, 3, 4);
            RequireTLBomb(fourRunT, new BoardPos(2, 3),
                "A four-Gem horizontal run with a stem must create a T/L AreaBomb.");
            MatchResult largeT = ResolveIntersection(resolver, 2, 1, 3, 2, 3, 5);
            Require(largeT.CreatedRainbowCount == 1 && largeT.CreatedAreaBombCount == 0
                && largeT.CreatedTLBombCount == 0 && largeT.ExtraTurnsGranted == 1,
                "A five-Gem horizontal run with a stem must create a Rainbow, not a T/L Bomb.");
            Require(largeT.LongestMatch == 5,
                "The large T must use its longest straight run rather than its seven-cell union.");

            MatchResult straightSixWithStem = ResolveIntersection(resolver, 2, 1, 4, 2, 3, 6);
            Require(straightSixWithStem.CreatedSpecials.Any(
                    creation => creation.SpecialType == GemSpecialType.Rainbow)
                && straightSixWithStem.CreatedTLBombCount == 0
                && straightSixWithStem.ExtraTurnsGranted == 1,
                "A straight Match-6 must take priority over its intersecting stem.");
        }

        static MatchResult ResolveIntersection(SpecialGemResolver resolver, int horizontalRow,
            int horizontalStart, int verticalColumn, int verticalStart, int verticalLength,
            int horizontalLength = 3, bool initialSwap = true, int comboIndex = 1)
        {
            GemType[,] types = BaselineTypes();
            GemSpecialType[,] specials = new GemSpecialType[DebugBoardRows, DebugBoardColumns];
            for (int offset = 0; offset < horizontalLength; offset++)
                types[horizontalRow, horizontalStart + offset] = GemType.Attack;
            for (int offset = 0; offset < verticalLength; offset++)
                types[verticalStart + offset, verticalColumn] = GemType.Attack;

            int intersectionRow = horizontalRow;
            int intersectionCol = verticalColumn;
            MatchResult result = resolver.ResolveCurrent(new BoardSnapshot(types, specials),
                new MoveResult(new BoardPos(intersectionRow, intersectionCol),
                    new BoardPos(intersectionRow, intersectionCol)), initialSwap, comboIndex, false);
            return result;
        }

        static void RequireTLBomb(MatchResult match, BoardPos intersection, string message)
        {
            SpecialGemCreation creation = match.CreatedSpecials.FirstOrDefault(
                candidate => candidate.IsTLBomb);
            Require(creation != null && creation.SpecialType == GemSpecialType.AreaBomb
                && creation.Position.Equals(intersection)
                && match.CreatedTLBombCount == 1
                && match.ExtraTurnsGranted == 1
                && !match.ClearPositions.Contains(intersection)
                && match.GemCounts.TryGetValue(GemType.Attack, out int clearedCount)
                && clearedCount == match.ClearPositions.Count,
                message);
        }

        static void CheckExtraTurnRules(SpecialGemResolver resolver)
        {
            MatchResult match3 = resolver.ResolveCurrent(LineBoard(3, true, GemSpecialType.None),
                new MoveResult(default, default), true, 1, false);
            MatchResult match4 = resolver.ResolveCurrent(LineBoard(4, true, GemSpecialType.None),
                new MoveResult(default, default), true, 1, false);
            MatchResult match5 = resolver.ResolveCurrent(LineBoard(5, true, GemSpecialType.None),
                new MoveResult(default, default), true, 1, false);
            MatchResult match6 = resolver.ResolveCurrent(LineBoard(6, true, GemSpecialType.None),
                new MoveResult(default, default), true, 1, false);
            Require(match3.ExtraTurnsGranted == 0 && match4.ExtraTurnsGranted == 0
                && match5.ExtraTurnsGranted == 1 && match6.ExtraTurnsGranted == 1,
                "Straight Match-3/4/5/6 rewards must be 0/0/1/1 Extra Turns.");

            MatchResult cascade3 = resolver.ResolveCurrent(LineBoard(3, true, GemSpecialType.None),
                new MoveResult(default, default), false, 2, false);
            MatchResult cascade4 = resolver.ResolveCurrent(LineBoard(4, true, GemSpecialType.None),
                new MoveResult(default, default), false, 2, false);
            MatchResult cascade5 = resolver.ResolveCurrent(LineBoard(5, true, GemSpecialType.None),
                new MoveResult(default, default), false, 2, false);
            MatchResult cascade6 = resolver.ResolveCurrent(LineBoard(6, true, GemSpecialType.None),
                new MoveResult(default, default), false, 2, false);
            MatchResult cascadeTL = ResolveIntersection(resolver, 2, 2, 3, 2, 3,
                initialSwap: false, comboIndex: 2);
            Require(cascade3.ExtraTurnsGranted == 0 && cascade4.ExtraTurnsGranted == 0
                && cascade5.ExtraTurnsGranted == 1 && cascade6.ExtraTurnsGranted == 1
                && cascadeTL.ExtraTurnsGranted == 1,
                "Cascade Match-3/4/5/6 and T/L rewards must be 0/0/1/1/1.");

            MoveResult accumulated = new MoveResult(default, default);
            accumulated.AddMatch(match3);
            accumulated.AddMatch(cascade5);
            accumulated.AddMatch(cascade6);
            Require(accumulated.ExtraTurnsGranted == 2
                && accumulated.CreatedAreaBombCount == 0
                && accumulated.CreatedRainbowCount == 2,
                "Independent cascade Match-5 and Match-6 groups must accumulate two Rainbow rewards.");
            Require(TurnManager.ConsumeTurn(1, accumulated.ExtraTurnsGranted, 5) == 2
                && TurnManager.ConsumeTurn(5, accumulated.ExtraTurnsGranted, 5) == 5,
                "Accumulated Extra Turns must consume the action and respect the stored-turn cap.");

            BoardSnapshot bombBoard = SpecialBoard(new BoardPos(3, 3), GemSpecialType.AreaBomb);
            MoveResult directBomb = resolver.EvaluateDirectActivation(bombBoard, new BoardPos(3, 3));
            Require(directBomb.InitialMatch.ExtraTurnsGranted == 0,
                "Direct AreaBomb activation must not grant an Extra Turn.");

            GemType[,] lineTypes = BaselineTypes();
            GemSpecialType[,] lineSpecials = new GemSpecialType[DebugBoardRows, DebugBoardColumns];
            for (int column = 1; column <= 3; column++)
                lineTypes[3, column] = GemType.Attack;
            lineSpecials[3, 2] = GemSpecialType.Line;
            MatchResult lineActivation = resolver.ResolveCurrent(new BoardSnapshot(lineTypes, lineSpecials),
                new MoveResult(new BoardPos(3, 2), new BoardPos(3, 3)), true, 1, false);
            Require(lineActivation.ExtraTurnsGranted == 0,
                "Activating an existing Line special must not grant an Extra Turn.");

            BoardSnapshot rainbowBoard = SpecialBoard(new BoardPos(3, 3), GemSpecialType.Rainbow);
            MoveResult rainbowColor = resolver.EvaluateSwap(rainbowBoard,
                new BoardPos(3, 3), new BoardPos(3, 4));
            Require(rainbowColor.IsValid && rainbowColor.ExtraTurnsGranted == 0,
                "Rainbow plus a normal Gem must not grant an Extra Turn.");

            GemType[,] rainbowTypes = BaselineTypes();
            GemSpecialType[,] rainbowSpecials = new GemSpecialType[DebugBoardRows, DebugBoardColumns];
            rainbowTypes[3, 3] = rainbowTypes[3, 4] = GemType.Rainbow;
            rainbowSpecials[3, 3] = rainbowSpecials[3, 4] = GemSpecialType.Rainbow;
            MoveResult rainbowPair = resolver.EvaluateSwap(new BoardSnapshot(rainbowTypes, rainbowSpecials),
                new BoardPos(3, 3), new BoardPos(3, 4));
            Require(rainbowPair.IsValid && rainbowPair.ExtraTurnsGranted == 0,
                "Rainbow plus Rainbow must not grant an Extra Turn.");

            GemSpecialType[,] bombLineSpecials = new GemSpecialType[DebugBoardRows, DebugBoardColumns];
            bombLineSpecials[3, 3] = GemSpecialType.AreaBomb;
            bombLineSpecials[3, 4] = GemSpecialType.Line;
            MoveResult bombLine = resolver.EvaluateSwap(
                new BoardSnapshot(BaselineTypes(), bombLineSpecials),
                new BoardPos(3, 3), new BoardPos(3, 4));
            HashSet<BoardPos> bombLineExpected = new HashSet<BoardPos>();
            for (int offset = -1; offset <= 1; offset++)
            {
                for (int column = 0; column < DebugBoardColumns; column++)
                    bombLineExpected.Add(new BoardPos(3 + offset, column));
                for (int row = 0; row < DebugBoardRows; row++)
                    bombLineExpected.Add(new BoardPos(row, 3 + offset));
            }
            Require(bombLine.IsValid && bombLine.ExtraTurnsGranted == 0
                && bombLine.InitialMatch.SpecialActivations.Any(activation =>
                    activation.Kind == SpecialActivationKind.BombLineCombo)
                && bombLineExpected.SetEquals(bombLine.InitialMatch.ClearPositions),
                "Bomb+Line swap must clear exactly three full rows and three full columns.");

            MoveResult bossMatch4 = new MoveResult(default, default);
            bossMatch4.AddMatch(match4);
            MoveResult bossMatch5 = new MoveResult(default, default);
            bossMatch5.AddMatch(match5);
            MoveResult bossTL = new MoveResult(default, default);
            bossTL.AddMatch(cascadeTL);
            Require(TurnManager.ConsumeTurn(1, bossMatch4.ExtraTurnsGranted, 5) == 0
                && TurnManager.ConsumeTurn(1, bossMatch5.ExtraTurnsGranted, 5) == 1
                && TurnManager.ConsumeTurn(1, bossTL.ExtraTurnsGranted, 5) == 1,
                "Boss Match-4 must grant no turn; Match-5 and T/L each grant one capped turn.");

            MoveResult bossMatch6 = new MoveResult(default, default);
            bossMatch6.AddMatch(match6);
            MoveResult bossCascade5 = new MoveResult(default, default);
            bossCascade5.AddMatch(match3);
            bossCascade5.AddMatch(cascade5);
            MoveResult bossCascade6 = new MoveResult(default, default);
            bossCascade6.AddMatch(match3);
            bossCascade6.AddMatch(cascade6);
            Require(TurnManager.ConsumeTurn(1, bossMatch6.ExtraTurnsGranted, 5) == 1
                && TurnManager.ConsumeTurn(1, bossCascade5.ExtraTurnsGranted, 5) == 1
                && TurnManager.ConsumeTurn(1, bossCascade6.ExtraTurnsGranted, 5) == 1,
                "Boss Match-6 and cascade Match-5/6 must each grant one stored turn.");

            GameObject aiObject = new GameObject("TurnDebugExtraTurnBossAI");
            try
            {
                BossAI ai = aiObject.AddComponent<BossAI>();
                MoveResult lineMove = new MoveResult(default, default) { IsValid = true };
                lineMove.AddMatch(match4);
                MoveResult bombMove = new MoveResult(default, default) { IsValid = true };
                bombMove.AddMatch(match5);
                MoveResult rainbowMove = new MoveResult(default, default) { IsValid = true };
                rainbowMove.AddMatch(match6);
                MoveResult tShapeMove = new MoveResult(default, default) { IsValid = true };
                tShapeMove.AddMatch(cascadeTL);
                Require(ai.Score(rainbowMove, .8f) > ai.Score(bombMove, .8f)
                    && ai.Score(bombMove, .8f) > ai.Score(lineMove, .8f)
                    && ai.Score(tShapeMove, .8f) > ai.Score(lineMove, .8f),
                    "BossAI must value Rainbow highest and T/L AreaBomb above a Line Match-4.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(aiObject);
            }
        }

        static void CheckCreatedSpecialVisualsRefresh()
        {
            Sprite[] baseSprites = CreateTestSprites();
            Sprite[] specialSprites = CreateTestSprites(7);
            Sprite rainbowActiveSprite = CreateTestSprites(1)[0];
            GameObject gemObject = new GameObject("TurnDebugSpecialVisualGem",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(GemView));
            try
            {
                baseSprites[(int)GemType.Rainbow].name = "Base_08_Spiral";
                specialSprites[(int)GemType.Attack].name = "Base_01_Sword_special";
                rainbowActiveSprite.name = "Base_08_Spiral";
                GemView gem = gemObject.GetComponent<GemView>();
                Button button = gemObject.GetComponent<Button>();
                button.targetGraphic = gemObject.GetComponent<Image>();
                GemVisualDatabase visuals = new GemVisualDatabase(baseSprites, specialSprites,
                    rainbowActiveSprite);
                gem.ConfigureArt(null, gemObject.GetComponent<Image>(), null, null,
                    null, null, visuals);
                gem.Initialize(null, new BoardPos(2, 3), GemType.Attack,
                    GemSpecialType.None, baseSprites[(int)GemType.Attack], visuals);

                gem.SetState(GemType.Attack, GemSpecialType.Line,
                    baseSprites[(int)GemType.Attack]);
                Require(gem.SpriteImage.sprite == specialSprites[(int)GemType.Attack],
                    "A newly created Line must immediately show its mapped special sprite.");
                Require(gem.SpecialType == GemSpecialType.Line,
                    "A Line must not retain horizontal or vertical direction.");
                gem.SetState(GemType.Attack, GemSpecialType.AreaBomb,
                    baseSprites[(int)GemType.Attack]);
                Require(gem.SpriteImage.sprite == specialSprites[(int)GemType.Attack]
                    && gem.SpecialIdleVFX.SpecialType == GemSpecialType.AreaBomb
                    && gem.SpecialIdleVFX.IntensityLevel == 2,
                    "A newly created AreaBomb must immediately use its own color's sealed special sprite.");
                gem.SetState(GemType.Rainbow, GemSpecialType.Rainbow,
                    baseSprites[(int)GemType.Rainbow]);
                Require(gem.Type == GemType.Rainbow && gem.SpecialType == GemSpecialType.Rainbow
                    && gem.SpriteImage.sprite == rainbowActiveSprite
                    && gem.SpriteImage.sprite.name == "Base_08_Spiral"
                    && gem.SpecialIdleVFX.SpecialType == GemSpecialType.Rainbow
                    && gem.SpecialIdleVFX.IntensityLevel == 3,
                    "A newly created Rainbow must immediately display Base_08_Spiral and its strongest aura.");

                for (int typeIndex = (int)GemType.Attack; typeIndex <= (int)GemType.Soul; typeIndex++)
                {
                    GemType type = (GemType)typeIndex;
                    gem.SetState(type, GemSpecialType.None, baseSprites[typeIndex]);
                    Require(gem.SpriteImage.sprite == baseSprites[typeIndex],
                        $"{type} ordinary Gems must display their own base sprite.");
                    gem.SetState(type, GemSpecialType.Line, baseSprites[typeIndex]);
                    Require(gem.SpriteImage.sprite == specialSprites[typeIndex],
                        $"{type} Line Gems must display their own sealed special sprite.");
                    gem.SetState(type, GemSpecialType.AreaBomb, baseSprites[typeIndex]);
                    Require(gem.SpriteImage.sprite == specialSprites[typeIndex],
                        $"{type} AreaBomb Gems must display their own sealed special sprite.");
                }

                gem.SetState(GemType.Lightning, GemSpecialType.Lightning, baseSprites[8]);
                Require(gem.SpriteImage.sprite == baseSprites[8],
                    "Lightning must keep its dedicated Base_09_PurpleLightning sprite.");
                gem.SetState(GemType.Bagua, GemSpecialType.Bagua, baseSprites[9]);
                Require(gem.SpriteImage.sprite == baseSprites[9],
                    "Bagua must keep its dedicated Base_10_YinYang_Black sprite.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gemObject);
                foreach (Sprite sprite in baseSprites.Concat(specialSprites)
                    .Append(rainbowActiveSprite).Distinct())
                {
                    if (sprite == null) continue;
                    Texture2D texture = sprite.texture;
                    UnityEngine.Object.DestroyImmediate(sprite);
                    if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
                }
            }

        }

        static void CheckGemSpriteFillsSquareCell()
        {
            const float cellSize = 100f;
            const int boardSize = 7;
            GameObject boardObject = new GameObject("TurnDebugGemCenterBoard",
                typeof(RectTransform));
            RectTransform boardRect = (RectTransform)boardObject.transform;
            boardRect.sizeDelta = Vector2.one * (cellSize * boardSize);
            Sprite[] baseSprites = CreateTestSprites();
            Sprite[] specialSprites = CreateTestSprites(7);
            Sprite[] ownedSprites = baseSprites.Concat(specialSprites).ToArray();
            GemType[] gemTypes =
            {
                GemType.Attack, GemType.Rainbow, GemType.ExpDrain, GemType.Mana,
                GemType.Rage, GemType.Soul, GemType.Attack
            };
            int[] rows = { 0, 3, 0, 6, 6, 1, 5 };
            int[] columns = { 0, 3, 6, 0, 6, 4, 2 };
            GameObject[] cells = new GameObject[gemTypes.Length];
            try
            {
                GemVisualDatabase visuals = new GemVisualDatabase(baseSprites, specialSprites, null);
                float maximumCenterError = 0f;
                for (int index = 0; index < gemTypes.Length; index++)
                {
                    GameObject cellObject = new GameObject($"TurnDebugGemCell_{rows[index]}_{columns[index]}",
                        typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                        typeof(Button), typeof(GemView));
                    cells[index] = cellObject;
                    cellObject.transform.SetParent(boardRect, false);
                    RectTransform cell = (RectTransform)cellObject.transform;
                    cell.anchorMin = cell.anchorMax = new Vector2(.5f, .5f);
                    cell.pivot = new Vector2(.5f, .5f);
                    cell.sizeDelta = Vector2.one * cellSize;
                    cell.anchoredPosition = new Vector2(
                        (columns[index] - 3) * cellSize, (3 - rows[index]) * cellSize);
                    cell.localScale = Vector3.one;
                    cell.localRotation = Quaternion.identity;

                    RectTransform visualRoot = new GameObject("GemVisualRoot",
                        typeof(RectTransform)).GetComponent<RectTransform>();
                    visualRoot.SetParent(cell, false);
                    visualRoot.anchorMin = Vector2.zero;
                    visualRoot.anchorMax = Vector2.one;
                    visualRoot.pivot = new Vector2(.2f, .8f);
                    visualRoot.sizeDelta = new Vector2(-12f, -18f);
                    visualRoot.anchoredPosition = new Vector2(7f, -9f);
                    visualRoot.localRotation = Quaternion.Euler(0f, 0f, 13f);
                    visualRoot.localScale = Vector3.one * .83f;

                    GameObject spriteObject = new GameObject("GemSprite",
                        typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    spriteObject.transform.SetParent(visualRoot, false);
                    Image spriteImage = spriteObject.GetComponent<Image>();
                    spriteImage.sprite = baseSprites[(int)gemTypes[index]];
                    RectTransform spriteRect = spriteImage.rectTransform;
                    spriteRect.anchorMin = new Vector2(.2f, .3f);
                    spriteRect.anchorMax = new Vector2(.8f, .7f);
                    spriteRect.pivot = new Vector2(.1f, .9f);
                    spriteRect.anchoredPosition = new Vector2(-5f, 11f);
                    spriteRect.offsetMin = new Vector2(4f, -3f);
                    spriteRect.offsetMax = new Vector2(-6f, 8f);
                    spriteRect.localRotation = Quaternion.Euler(0f, 0f, -9f);
                    spriteRect.localScale = Vector3.one * .7f;
                    Button button = cellObject.GetComponent<Button>();
                    button.targetGraphic = cellObject.GetComponent<Image>();

                    GemView gem = cellObject.GetComponent<GemView>();
                    gem.ConfigureArt(null, spriteImage, null, null, null, visualRoot, visuals);
                    GemSpecialType special = index == gemTypes.Length - 1
                        ? GemSpecialType.Line
                        : GemSpecialType.None;
                    gem.Initialize(null, new BoardPos(rows[index], columns[index]),
                        gemTypes[index], special, baseSprites[(int)gemTypes[index]], visuals);

                    if (index == gemTypes.Length - 1)
                        Require(spriteImage.sprite == specialSprites[(int)GemType.Attack],
                            "A Special Gem must preserve its artwork while sharing the same center rule.");

                    float centerError = gem.GetVisualCenterErrorIn(boardRect, cell);
                    maximumCenterError = Mathf.Max(maximumCenterError, centerError);
                    Require(centerError <= .001f
                        && visualRoot.anchorMin == new Vector2(.5f, .5f)
                        && visualRoot.anchorMax == new Vector2(.5f, .5f)
                        && visualRoot.pivot == new Vector2(.5f, .5f)
                        && visualRoot.sizeDelta == cell.rect.size
                        && visualRoot.anchoredPosition == Vector2.zero
                        && visualRoot.localRotation == Quaternion.identity
                        && Mathf.Approximately(visualRoot.localScale.x, 1.05f)
                        && Mathf.Approximately(visualRoot.localScale.y, 1.05f)
                        && spriteRect.anchorMin == Vector2.zero
                        && spriteRect.anchorMax == Vector2.one
                        && spriteRect.offsetMin == Vector2.zero
                        && spriteRect.offsetMax == Vector2.zero
                        && spriteRect.anchoredPosition == Vector2.zero
                        && spriteRect.pivot == new Vector2(.5f, .5f)
                        && spriteRect.localScale == Vector3.one
                        && spriteRect.localRotation == Quaternion.identity
                        && spriteImage.type == Image.Type.Simple
                        && spriteImage.preserveAspect && !spriteImage.raycastTarget,
                        $"{gemTypes[index]} Gem must reset to centered, aspect-preserving transforms.");

                    gem.SetVisualOffset(new Vector2(12f, -7f));
                    gem.Initialize(null, new BoardPos(rows[index], columns[index]),
                        gemTypes[index], special, baseSprites[(int)gemTypes[index]], visuals);
                    Require(visualRoot.anchoredPosition == Vector2.zero
                        && visualRoot.localRotation == Quaternion.identity
                        && Mathf.Approximately(visualRoot.localScale.x, 1.05f)
                        && gem.GetVisualCenterErrorIn(boardRect, cell) <= .001f,
                        $"{gemTypes[index]} Gem reuse must clear stale visual animation transforms.");

                    gem.SetFocused(true);
                    Image brighten = visualRoot.Find("FocusBrighten").GetComponent<Image>();
                    GemFocusOutlineGraphic outline = visualRoot.Find("FocusOutline")
                        .GetComponent<GemFocusOutlineGraphic>();
                    Require(Mathf.Approximately(visualRoot.localScale.x, 1.05f * 1.06f)
                        && brighten.enabled && outline.enabled
                        && !brighten.raycastTarget && !outline.raycastTarget,
                        $"{gemTypes[index]} focus must brighten, outline, and scale only the visual root.");
                    gem.SetFocused(false);
                    Require(Mathf.Approximately(visualRoot.localScale.x, 1.05f)
                        && !brighten.enabled && !outline.enabled,
                        $"{gemTypes[index]} unfocus must restore the exact base visual scale.");
                }

                Debug.Log($"Validated {gemTypes.Length} Gem centers across board corners and center; "
                    + $"maximum center error {maximumCenterError:F3} reference pixels.");
            }

            finally
            {
                foreach (GameObject cell in cells)
                    if (cell != null)
                        UnityEngine.Object.DestroyImmediate(cell);
                UnityEngine.Object.DestroyImmediate(boardObject);
                foreach (Sprite sprite in ownedSprites.Distinct())
                {
                    if (sprite == null) continue;
                    Texture2D texture = sprite.texture;
                    UnityEngine.Object.DestroyImmediate(sprite);
                    if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        static void CheckGemSpriteMappings()
        {
            Sprite[] baseSprites = CreateTestSprites();
            Sprite[] specialSprites = CreateTestSprites(7);
            GameObject gemObject = new GameObject("TurnDebugLightningBaguaGem",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button), typeof(GemView));
            try
            {
                GemVisualDatabase visuals = new GemVisualDatabase(baseSprites,
                    specialSprites, null);
                Require(visuals.GetSprite(GemType.Lightning, GemSpecialType.None)
                        == baseSprites[8]
                    && visuals.GetSprite(GemType.Lightning, GemSpecialType.Lightning)
                        == baseSprites[8]
                    && visuals.GetSprite(GemType.Bagua, GemSpecialType.None)
                        == baseSprites[9]
                    && visuals.GetSprite(GemType.Bagua, GemSpecialType.Bagua)
                        == baseSprites[9],
                    "Lightning and Bagua must use their explicit Other-folder base sprites, not indexed special sprites.");

                Image image = gemObject.GetComponent<Image>();
                Button button = gemObject.GetComponent<Button>();
                button.targetGraphic = image;
                GemView gem = gemObject.GetComponent<GemView>();
                gem.Initialize(null, new BoardPos(6, 4), GemType.Lightning,
                    GemSpecialType.Lightning, baseSprites[8], visuals);
                Require(gem.SpriteImage.sprite == baseSprites[8],
                    "A newly spawned or refilled Lightning Gem must display Base_09_PurpleLightning.");
                gem.SetState(GemType.Bagua, GemSpecialType.Bagua, baseSprites[9]);
                Require(gem.SpriteImage.sprite == baseSprites[9],
                    "A Gem changed to Bagua must display Base_10_YinYang_Black.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gemObject);
                foreach (Sprite sprite in baseSprites.Concat(specialSprites).Distinct())
                {
                    if (sprite == null) continue;
                    Texture2D texture = sprite.texture;
                    UnityEngine.Object.DestroyImmediate(sprite);
                    if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        static void CheckSelectionArrowDirections()
        {
            const int size = BoardManager.DefaultBoardSize;
            Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
            Require(CountAvailableDirections(new BoardPos(0, 0), size, size, directions) == 2
                && !BoardSelectionOverlay.IsDirectionAvailable(new BoardPos(0, 0), size, size, Vector2.up)
                && !BoardSelectionOverlay.IsDirectionAvailable(new BoardPos(0, 0), size, size, Vector2.left)
                && BoardSelectionOverlay.IsDirectionAvailable(new BoardPos(0, 0), size, size, Vector2.right)
                && BoardSelectionOverlay.IsDirectionAvailable(new BoardPos(0, 0), size, size, Vector2.down),
                "Top-left Gem focus must show only Right and Down arrows.");
            Require(CountAvailableDirections(new BoardPos(size - 1, size - 1), size, size, directions) == 2
                && BoardSelectionOverlay.IsDirectionAvailable(
                    new BoardPos(size - 1, size - 1), size, size, Vector2.left)
                && BoardSelectionOverlay.IsDirectionAvailable(
                    new BoardPos(size - 1, size - 1), size, size, Vector2.up)
                && !BoardSelectionOverlay.IsDirectionAvailable(
                    new BoardPos(size - 1, size - 1), size, size, Vector2.right)
                && !BoardSelectionOverlay.IsDirectionAvailable(
                    new BoardPos(size - 1, size - 1), size, size, Vector2.down),
                "Bottom-right Gem focus must show only Left and Up arrows.");
            Require(CountAvailableDirections(new BoardPos(0, 3), size, size, directions) == 3
                && CountAvailableDirections(new BoardPos(3, 0), size, size, directions) == 3
                && CountAvailableDirections(new BoardPos(3, size - 1), size, size, directions) == 3
                && CountAvailableDirections(new BoardPos(size - 1, 3), size, size, directions) == 3,
                "Each board edge must show exactly three valid direction arrows.");
            Require(CountAvailableDirections(new BoardPos(3, 3), size, size, directions) == 4,
                "A center Gem must show all four direction arrows.");
            Require(CountAvailableDirections(new BoardPos(0, size - 1), size, size, directions) == 2
                && CountAvailableDirections(new BoardPos(size - 1, 0), size, size, directions) == 2,
                "The remaining two board corners must show exactly two direction arrows.");
        }

        static int CountAvailableDirections(
            BoardPos position, int rows, int columns, Vector2[] directions)
        {
            int count = 0;
            foreach (Vector2 direction in directions)
                if (BoardSelectionOverlay.IsDirectionAvailable(
                    position, rows, columns, direction))
                    count++;
            return count;
        }

        static void CheckSpecialGemIdleVFX()
        {
            Vector2 sidePoint = SpecialGemIdleVFX.GetRoundedSquarePointForDebug(0f);
            Vector2 cornerPoint = SpecialGemIdleVFX.GetRoundedSquarePointForDebug(Mathf.PI * .25f);
            Require(Mathf.Abs(sidePoint.x - 1f) < .001f
                && Mathf.Abs(sidePoint.y) < .001f
                && cornerPoint.magnitude > 1.15f
                && Mathf.Abs(cornerPoint.x - cornerPoint.y) < .001f,
                "Special Gem aura geometry must be a rounded square rather than a circular ring.");

            GemSpecialType[] types =
            {
                GemSpecialType.Line,
                GemSpecialType.AreaBomb, GemSpecialType.Rainbow
            };
            GameObject row = new GameObject("TurnDebugSpecialIdleVfxRow", typeof(RectTransform));
            Sprite[] baseSprites = CreateTestSprites();
            Sprite[] specialSprites = CreateTestSprites(7);
            Sprite selectionSprite = CreateTestSprites(1)[0];
            Sprite[] ownedSprites = baseSprites.Concat(specialSprites)
                .Append(selectionSprite).ToArray();
            try
            {
                RectTransform rowRect = (RectTransform)row.transform;
                GemView[] gems = new GemView[types.Length];
                SpecialGemIdleVFX[] idleEffects = new SpecialGemIdleVFX[types.Length];
                GemVisualDatabase visuals = new GemVisualDatabase(baseSprites, specialSprites, null);
                for (int index = 0; index < types.Length; index++)
                {
                    GameObject gemObject = new GameObject($"SpecialGem_{types[index]}",
                        typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(GemView));
                    gemObject.transform.SetParent(rowRect, false);
                    RectTransform gemRect = (RectTransform)gemObject.transform;
                    gemRect.sizeDelta = Vector2.one * 100f;
                    gemRect.anchoredPosition = new Vector2(index * 104f, 0f);

                    RectTransform visualRoot = new GameObject("GemVisualRoot", typeof(RectTransform))
                        .GetComponent<RectTransform>();
                    visualRoot.SetParent(gemRect, false);
                    visualRoot.anchorMin = Vector2.zero;
                    visualRoot.anchorMax = Vector2.one;
                    visualRoot.offsetMin = Vector2.zero;
                    visualRoot.offsetMax = Vector2.zero;

                    GameObject spriteObject = new GameObject("GemSprite",
                        typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    spriteObject.transform.SetParent(visualRoot, false);
                    Image sprite = spriteObject.GetComponent<Image>();
                    sprite.sprite = baseSprites[index];

                    GameObject selectionObject = new GameObject("SelectionOverlay",
                        typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    selectionObject.transform.SetParent(visualRoot, false);
                    Image selection = selectionObject.GetComponent<Image>();
                    selection.sprite = selectionSprite;

                    Button button = gemObject.GetComponent<Button>();
                    button.targetGraphic = gemObject.GetComponent<Image>();
                    GemView gem = gemObject.GetComponent<GemView>();
                    gem.ConfigureArt(null, sprite, null, null, selection,
                        visualRoot, visuals);
                    gem.Initialize(null, new BoardPos(0, index), (GemType)index,
                        GemSpecialType.None, baseSprites[index], visuals);
                    gem.SetSpecialType(types[index]);
                    gems[index] = gem;
                    idleEffects[index] = gem.SpecialIdleVFX;
                }

                Require(idleEffects.All(effect => effect != null && !effect.raycastTarget)
                    && gems.All(gem => gem.SpecialIdleVFX.IntensityLevel > 0)
                    && idleEffects[0].IntensityLevel == 1
                    && idleEffects[1].IntensityLevel == 1
                    && idleEffects[2].IntensityLevel == 2
                    && idleEffects[3].IntensityLevel == 3
                    && Mathf.Approximately(SpecialGemIdleVFX.GetAuraAlphaForDebug(types[0]),
                        SpecialGemIdleVFX.GetAuraAlphaForDebug(types[1]))
                    && SpecialGemIdleVFX.GetAuraAlphaForDebug(types[0])
                        < SpecialGemIdleVFX.GetAuraAlphaForDebug(types[2])
                    && SpecialGemIdleVFX.GetAuraAlphaForDebug(types[2])
                        < SpecialGemIdleVFX.GetAuraAlphaForDebug(types[3])
                    && SpecialGemIdleVFX.GetSparkleTargetForDebug(types[0]) == 2
                    && SpecialGemIdleVFX.GetSparkleTargetForDebug(types[2]) == 3
                    && SpecialGemIdleVFX.GetSparkleTargetForDebug(types[3]) == 4,
                    "Side-by-side special VFX must increase in intensity from Line to Bomb to Rainbow.");

                for (int index = 0; index < gems.Length; index++)
                {
                    Vector2 rootPosition = ((RectTransform)gems[index].transform).anchoredPosition;
                    Vector2 spriteSize = gems[index].SpriteImage.rectTransform.rect.size;
                    SpecialGemIdleVFX effect = gems[index].SpecialIdleVFX;
                    int creationFlashCount = effect.CreationFlashCount;
                    int idleStartRequestCount = effect.IdleStartRequestCount;
                    for (int refresh = 0; refresh < 3; refresh++)
                        gems[index].RefreshSpecialVisual();
                    gems[index].SetPosition(new BoardPos(1, index));
                    gems[index].SetVisualOffset(new Vector2(0f, 12f));
                    gems[index].SetSelected(true);
                    Require(ReferenceEquals(effect, gems[index].SpecialIdleVFX)
                        && ((RectTransform)gems[index].transform).anchoredPosition == rootPosition
                        && gems[index].SpriteImage.rectTransform.rect.size == spriteSize
                        && effect.SpecialType == types[index]
                        && effect.CreationFlashCount == creationFlashCount
                        && effect.IdleStartRequestCount == idleStartRequestCount
                        && effect.ActiveSparkleCount <= 4
                        && (gems[index].SpecialType == GemSpecialType.None
                            || gems[index].SpriteImage.raycastTarget == false),
                        "Refreshing or selecting a special must reuse idle VFX and leave Gem transforms unchanged.");
                }

                gems[0].SetState(GemType.Attack, GemSpecialType.Line,
                    baseSprites[(int)GemType.Attack], playCreationFlash: true);
                Require(gems[0].SpecialIdleVFX.SpecialType == GemSpecialType.Line
                    && gems[0].SpecialIdleVFX.CreationFlashCount == 1,
                    "New special creation must request one flash, while movement and refresh do not replay it.");

                foreach (GemView gem in gems)
                    gem.StopSpecialVFX();
                Require(idleEffects.All(effect => !effect.IsIdleEffectRunning
                    && !effect.raycastTarget),
                    "Stopping special effects must stop each reused effect without raycasts.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(row);
                foreach (Sprite sprite in ownedSprites.Distinct())
                {
                    if (sprite == null) continue;
                    Texture2D texture = sprite.texture;
                    UnityEngine.Object.DestroyImmediate(sprite);
                    if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        static void CheckBoardRageFireBinding()
        {
            GameObject root = new GameObject("TurnDebugBoardRageFire",
                typeof(RectTransform));
            GameObject battleObject = new GameObject("TurnDebugBoardRageBattle");
            try
            {
                RectTransform frame = new GameObject("BoardFrame", typeof(RectTransform))
                    .GetComponent<RectTransform>();
                frame.SetParent(root.transform, false);
                frame.sizeDelta = Vector2.one * 820f;
                frame.anchoredPosition = new Vector2(3f, -2f);
                Vector2 originalSize = frame.sizeDelta;
                Vector2 originalPosition = frame.anchoredPosition;

                RectTransform fireRect = new GameObject("RageFrameVFX", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(BoardRageFireVFX)).GetComponent<RectTransform>();
                fireRect.SetParent(root.transform, false);
                BoardRageFireVFX fire = fireRect.GetComponent<BoardRageFireVFX>();
                fire.Configure(frame, 100f);
                Require(fireRect.parent == root.transform && !fire.maskable
                    && !fire.raycastTarget && fireRect.rect.width > frame.rect.width,
                    "Board fire must be an active, unmasked, non-interactive overlay around BoardFrame.");

                BattleController battle = battleObject.AddComponent<BattleController>();
                battle.Configure(null, null, null, boardRageFire: fire);
                Require(!fire.IsRageReady,
                    "Battle initialization must synchronize the current Hero Rage state immediately.");
                battle.SetRageForDebug(BattleActor.Player, 99);
                Require(!fire.IsRageReady,
                    "Hero Rage below maximum must keep BoardFrame fire off.");

                battle.SetRageForDebug(BattleActor.Boss, 100);
                Require(!fire.IsRageReady,
                    "Boss Rage at maximum must not ignite the BoardFrame.");

                battle.SetRageForDebug(BattleActor.Player, 100);
                Require(fire.IsRageReady && fire.enabled && fire.ActiveEmberCount == 12,
                    "Hero Rage at maximum must start the BoardFrame fire fade-in and bounded ember layer.");

                battle.SetRageForDebug(BattleActor.Player, 0);
                Require(!fire.IsRageReady
                    && fire.raycastTarget == false
                    && frame.sizeDelta == originalSize
                    && frame.anchoredPosition == originalPosition,
                    "Consuming Hero Rage must request fire fade-out without moving the frame or blocking input.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(battleObject);
            }
        }

        static void CheckIdleLoopClips()
        {
            CheckIdleLoopClip("Assets/Animations/Characters/Hero2/Hero2_Idle.anim", 24);
            CheckIdleLoopClip("Assets/Animations/Bosses/Region01/Boss01/Boss01_Idle.anim", 17);
            CheckIdleLoopClip("Assets/Animations/Bosses/Region01/Boss02/Boss02_Idle.anim", 15);
        }

        static void CheckIdleLoopClip(string path, int expectedFrames)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            Require(clip != null, $"Idle animation clip is missing: {path}");
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip,
                EditorCurveBinding.PPtrCurve(string.Empty, typeof(Image), "m_Sprite"));
            Require(keys.Length == expectedFrames && keys.All(key => key.value is Sprite),
                $"Idle clip must contain exactly {expectedFrames} non-null Sprite frames: {path}");
            Require(AnimationUtility.GetAnimationClipSettings(clip).loopTime,
                $"Idle clip must loop directly: {path}");
            for (int index = 0; index < keys.Length; index++)
            {
                Sprite sprite = (Sprite)keys[index].value;
                Require(sprite.rect.width > 0f && sprite.rect.height > 0f,
                    $"Idle clip contains a zero-size Sprite at frame {index}: {path}");
                Require(Mathf.Abs(keys[index].time - index / clip.frameRate) < .001f,
                    $"Idle clip has an unexpected keyframe interval at frame {index}: {path}");
            }

            float expectedDuration = expectedFrames / clip.frameRate;
            Require(clip.length >= expectedDuration - .001f
                && clip.length < expectedDuration + 1f / clip.frameRate,
                $"Idle clip contains an unexpected blank tail: {path}");
        }

        static void CheckSharedBattleGroundLine()
        {
            GameObject heroRoot = new GameObject("TurnDebugGroundHeroRoot", typeof(RectTransform));
            GameObject bossRoot = new GameObject("TurnDebugGroundBossRoot", typeof(RectTransform));
            try
            {
                RectTransform heroRootRect = (RectTransform)heroRoot.transform;
                RectTransform bossRootRect = (RectTransform)bossRoot.transform;
                heroRootRect.sizeDelta = bossRootRect.sizeDelta = new Vector2(300f, 400f);
                CharacterView hero = CreateGroundTestCharacter(heroRoot, .85f, 16f / 256f,
                    out RectTransform heroVisual);
                CharacterView boss = CreateGroundTestCharacter(bossRoot, 1.9505f, 53f / 256f,
                    out RectTransform bossVisual);
                hero.CaptureHomePosition();
                boss.CaptureHomePosition();

                float heroFeetY = heroVisual.anchoredPosition.y
                    + 16f / 256f * heroVisual.rect.height * Mathf.Abs(heroVisual.localScale.y);
                float bossFeetY = bossVisual.anchoredPosition.y
                    + 53f / 256f * bossVisual.rect.height * Mathf.Abs(bossVisual.localScale.y);
                Require(Mathf.Abs(heroFeetY) <= .01f && Mathf.Abs(bossFeetY) <= .01f,
                    "Hero and Boss visible Idle bases must share their CharacterRoot ground line.");

                Vector2 heroHome = heroVisual.anchoredPosition;
                Vector2 bossHome = bossVisual.anchoredPosition;
                for (int index = 0; index < 10; index++)
                {
                    hero.CaptureHomePosition();
                    boss.CaptureHomePosition();
                    Require(heroVisual.anchoredPosition == heroHome
                        && bossVisual.anchoredPosition == bossHome,
                        "Repeated layout synchronization must not accumulate Hero or Boss ground offsets.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(heroRoot);
                UnityEngine.Object.DestroyImmediate(bossRoot);
            }
        }

        static CharacterView CreateGroundTestCharacter(GameObject root, float scale,
            float feetPadding, out RectTransform visualRect)
        {
            GameObject visual = new GameObject("CharacterVisual",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            visualRect = (RectTransform)visual.transform;
            visualRect.SetParent(root.transform, false);
            visualRect.anchorMin = Vector2.zero;
            visualRect.anchorMax = Vector2.one;
            visualRect.pivot = new Vector2(.5f, 0f);
            visualRect.localScale = Vector3.one * scale;
            CharacterView view = root.AddComponent<CharacterView>();
            view.Configure(visual.GetComponent<Image>(), visualRect, 12f,
                CharacterFacing.Right, CharacterFacing.Right,
                idleFeetPadding: feetPadding);
            return view;
        }

        static void CheckBattleLayoutGridLayout()
        {
            const float gridWidth = 891.75f;
            const float gridHeight = 880.5f;
            const int size = BoardManager.DefaultBoardSize;
            GameObject gridObject = new GameObject("TurnDebugBattleLayoutGrid",
                typeof(RectTransform), typeof(GridLayoutGroup));
            try
            {
                RectTransform gridRect = (RectTransform)gridObject.transform;
                gridRect.sizeDelta = new Vector2(gridWidth, gridHeight);
                GridLayoutGroup layout = gridObject.GetComponent<GridLayoutGroup>();
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = size;
                layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
                layout.startAxis = GridLayoutGroup.Axis.Horizontal;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.padding = new RectOffset();
                layout.spacing = Vector2.zero;

                float cellSize = Mathf.Min(
                    (gridRect.rect.width - layout.spacing.x * (size - 1)) / size,
                    (gridRect.rect.height - layout.spacing.y * (size - 1)) / size);
                layout.cellSize = Vector2.one * cellSize;

                RectTransform[,] cells = new RectTransform[size, size];
                for (int row = 0; row < size; row++)
                for (int column = 0; column < size; column++)
                {
                    GameObject cell = new GameObject($"Cell_{row}_{column}", typeof(RectTransform));
                    cell.transform.SetParent(gridRect, false);
                    cells[row, column] = (RectTransform)cell.transform;
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
                Canvas.ForceUpdateCanvases();
                float left = float.PositiveInfinity;
                float right = float.NegativeInfinity;
                float bottom = float.PositiveInfinity;
                float top = float.NegativeInfinity;
                for (int row = 0; row < size; row++)
                for (int column = 0; column < size; column++)
                {
                    Vector3[] corners = new Vector3[4];
                    cells[row, column].GetWorldCorners(corners);
                    for (int corner = 0; corner < corners.Length; corner++)
                    {
                        Vector3 local = gridRect.InverseTransformPoint(corners[corner]);
                        left = Mathf.Min(left, local.x);
                        right = Mathf.Max(right, local.x);
                        bottom = Mathf.Min(bottom, local.y);
                        top = Mathf.Max(top, local.y);
                    }
                }

                float leftMargin = left - gridRect.rect.xMin;
                float rightMargin = gridRect.rect.xMax - right;
                float bottomMargin = bottom - gridRect.rect.yMin;
                float topMargin = gridRect.rect.yMax - top;
                float horizontalCellGap = Mathf.Abs(cells[0, 1].anchoredPosition.x
                    - cells[0, 0].anchoredPosition.x) - cellSize;
                float verticalCellGap = Mathf.Abs(cells[1, 0].anchoredPosition.y
                    - cells[0, 0].anchoredPosition.y) - cellSize;
                Vector2 oppositeCellCenter = (cells[0, 0].anchoredPosition
                    + cells[size - 1, size - 1].anchoredPosition) * .5f;
                Require(Mathf.Approximately(cellSize, gridHeight / size)
                    && Mathf.Abs(leftMargin - rightMargin) < .01f
                    && Mathf.Abs(topMargin - bottomMargin) < .01f
                    && Mathf.Abs(topMargin) < .01f
                    && Mathf.Abs(bottomMargin) < .01f
                    && Mathf.Abs(horizontalCellGap) < .01f
                    && Mathf.Abs(verticalCellGap) < .01f
                    && oppositeCellCenter.magnitude < .01f
                    && cells.Length == size * size
                    && layout.constraint == GridLayoutGroup.Constraint.FixedColumnCount
                    && layout.constraintCount == size
                    && layout.startCorner == GridLayoutGroup.Corner.UpperLeft
                    && layout.startAxis == GridLayoutGroup.Axis.Horizontal
                    && layout.childAlignment == TextAnchor.MiddleCenter
                    && gridRect.localScale == Vector3.one
                    && Mathf.Approximately(cellSize, cells[0, 0].rect.height)
                    && cells.Cast<RectTransform>().All(cell =>
                        cell.localScale == Vector3.one
                        && cell.GetComponent<Mask>() == null
                        && cell.GetComponent<RectMask2D>() == null),
                    "The BattleLayout grid must align 49 centered, unscaled 7x7 cells with zero spacing.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gridObject);
            }
        }

        static void CheckBoardCreatedSpecialsPersist()
        {
                GameObject root = new GameObject("TurnDebugCreatedSpecialRoot", typeof(RectTransform));
                GameObject prefabObject = new GameObject("TurnDebugCreatedSpecialGem",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(GemView));
                GameObject boardObject = new GameObject("TurnDebugCreatedSpecialBoard");
                Sprite[] baseSprites = CreateTestSprites();
                Sprite[] specialSprites = CreateTestSprites(7);
                Sprite[] ownedSprites = baseSprites.Concat(specialSprites).ToArray();
                UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
                try
                {
                    baseSprites[(int)GemType.Rainbow].name = "Base_08_Spiral";
                    specialSprites[(int)GemType.Attack].name = "Base_01_Sword_special";
                    GridLayoutGroup layout = root.AddComponent<GridLayoutGroup>();
                    layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    layout.constraintCount = DebugBoardColumns;
                    layout.cellSize = new Vector2(100f, 100f);
                    Image baseImage = prefabObject.GetComponent<Image>();
                    prefabObject.GetComponent<Button>().targetGraphic = baseImage;

                    GemVisualDatabase visuals = new GemVisualDatabase(baseSprites, specialSprites, null);
                    BoardManager board = ConfigureDebugBoard(root, prefabObject, boardObject,
                        baseSprites, visuals);

                    GemType[] matchTypes =
                    {
                        GemType.Attack, GemType.Mana, GemType.ExpDrain, GemType.Rage,
                        GemType.Health, GemType.Shield, GemType.Soul
                    };
                    foreach (GemType matchType in matchTypes)
                    foreach (int length in new[] { 4, 5, 6 })
                    {
                        GemType[,] types = BaselineTypes();
                        GemSpecialType[,] specials = new GemSpecialType[DebugBoardRows, DebugBoardColumns];
                        for (int column = 0; column < length - 1; column++)
                            types[3, column] = matchType;
                        types[3, length - 1] = matchType == GemType.Mana
                            ? GemType.Attack
                            : GemType.Mana;
                        types[3, length] = matchType;
                        board.SetBoardForDebug(types, specials);

                        MoveResult move = board.EvaluateMove(new BoardPos(3, length - 1),
                            new BoardPos(3, length));
                        Require(move.IsValid && move.InitialMatch.CreatedSpecials.Count == 1,
                            $"The deterministic Match-{length} board must create exactly one special.");
                        SpecialGemCreation creation = move.InitialMatch.CreatedSpecials[0];
                        GemSpecialType expectedSpecial = length == 4
                            ? GemSpecialType.Line
                            : GemSpecialType.Rainbow;
                        GemView before = board.GetGemsInLayoutOrder().Single(gem =>
                            gem.Position.Equals(creation.Position));
                        Require(creation.SpecialType == expectedSpecial
                            && !move.InitialMatch.ClearPositions.Contains(creation.Position),
                            $"Match-{length} creation must have the expected type and be excluded from destruction.");

                        UnityEngine.Random.InitState(20261025 + length + (int)matchType * 100);
                        MoveResult resolved = null;
                        Finish(board.ResolveMove(move, result => resolved = result));
                        GemView after = board.GetGemsInLayoutOrder().SingleOrDefault(gem =>
                            gem.Position.Equals(creation.Position));
                        Sprite expectedSprite = length == 4
                            ? specialSprites[(int)matchType]
                            : baseSprites[(int)GemType.Rainbow];
                        Require(resolved != null && resolved.WasValid
                            && ReferenceEquals(before, after)
                            && after.RuntimeId == before.RuntimeId
                            && after.SpecialType == expectedSpecial
                            && after.SpriteImage.sprite == expectedSprite,
                            $"Match-{length} {matchType} must keep the correct special sprite after resolution.");
                        if (length >= 5)
                        {
                            Require(after.Type == GemType.Rainbow && after.SpriteImage.sprite == baseSprites[7],
                                $"A created Match-{length} Rainbow must be GemType.Rainbow and display the Spiral base Sprite.");
                        }
                        Require(board.GetGemsInLayoutOrder().Where(gem =>
                                gem.SpecialType != GemSpecialType.None)
                            .All(gem => gem.SpriteImage.sprite
                                == visuals.GetSprite(gem.Type, gem.SpecialType)),
                            "Every special created or rolled during refill must display its matching special sprite.");
                    }

                    GemType[,] tlTypes = BaselineTypes();
                    GemSpecialType[,] tlSpecials =
                        new GemSpecialType[DebugBoardRows, DebugBoardColumns];
                    tlTypes[3, 0] = GemType.Mana;
                    tlTypes[1, 3] = GemType.Attack;
                    tlTypes[2, 3] = GemType.Mana;
                    tlTypes[3, 2] = GemType.Attack;
                    tlTypes[3, 3] = GemType.Attack;
                    tlTypes[3, 4] = GemType.Attack;
                    tlTypes[4, 3] = GemType.Attack;
                    board.SetBoardForDebug(tlTypes, tlSpecials);
                    MoveResult tlMove = board.EvaluateMove(
                        new BoardPos(1, 3), new BoardPos(2, 3));
                    Require(tlMove.IsValid && tlMove.InitialMatch.CreatedSpecials.Any(
                            creation => creation.SpecialType == GemSpecialType.AreaBomb
                                && creation.IsTLBomb
                                && creation.Position.Equals(new BoardPos(3, 3))),
                        "A T/L match must create its AreaBomb at the intersection.");
                    MoveResult resolvedTL = null;
                    Finish(board.ResolveMove(tlMove, result => resolvedTL = result));
                    GemView tlBomb = board.GetGemsInLayoutOrder().SingleOrDefault(gem =>
                        gem.Position.Equals(new BoardPos(3, 3)));
                    Require(resolvedTL != null && resolvedTL.WasValid
                        && tlBomb != null && tlBomb.SpecialType == GemSpecialType.AreaBomb
                        && tlBomb.SpriteImage.sprite == specialSprites[(int)GemType.Attack],
                        "A T/L-created AreaBomb must retain the matching Attack seal sprite after board resolution.");

                    GemType[,] cascadeTypes = BaselineTypes();
                    GemSpecialType[,] cascadeSpecials =
                        new GemSpecialType[DebugBoardRows, DebugBoardColumns];
                    cascadeTypes[4, 2] = GemType.Health;
                    cascadeTypes[4, 3] = GemType.Health;
                    cascadeTypes[5, 0] = GemType.Health;
                    cascadeTypes[5, 1] = GemType.Health;
                    cascadeTypes[5, 2] = GemType.Attack;
                    cascadeTypes[5, 3] = GemType.Attack;
                    cascadeTypes[5, 4] = GemType.Health;
                    cascadeTypes[6, 4] = GemType.Attack;
                    board.SetBoardForDebug(cascadeTypes, cascadeSpecials);
                    MoveResult cascadeMove = board.EvaluateMove(
                        new BoardPos(5, 4), new BoardPos(6, 4));
                    Require(cascadeMove.IsValid
                        && cascadeMove.InitialMatch.CreatedSpecials.Count == 0,
                        "The cascade visual scenario must begin with a Match-3 and create no initial special.");
                    UnityEngine.Random.InitState(20261031);
                    MoveResult resolvedCascade = null;
                    Finish(board.ResolveMove(cascadeMove, result => resolvedCascade = result));
                    GemView cascadeLine = board.GetGemsInLayoutOrder().FirstOrDefault(gem =>
                        gem.SpecialType == GemSpecialType.Line);
                    Require(resolvedCascade != null && resolvedCascade.WasValid
                        && resolvedCascade.CascadeCount >= 1
                        && resolvedCascade.CreatedLineCount >= 1
                        && cascadeLine != null
                        && cascadeLine.SpriteImage.sprite == specialSprites[(int)cascadeLine.Type],
                        "A Line created in a cascade must immediately display its own color's seal art.");
                }
                finally
                {
                    UnityEngine.Random.state = previousRandomState;
                    UnityEngine.Object.DestroyImmediate(root);
                    UnityEngine.Object.DestroyImmediate(prefabObject);
                    UnityEngine.Object.DestroyImmediate(boardObject);
                    foreach (Sprite sprite in ownedSprites.Distinct())
                    {
                        if (sprite == null) continue;
                        Texture2D texture = sprite.texture;
                        UnityEngine.Object.DestroyImmediate(sprite);
                        if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
                    }
                }
        }

        static void CheckLightningBeamGeometry()
        {
            const float screenWidth = 1080f;
            const float screenHeight = 1920f;
            float bottomHeight = Mathf.Clamp(screenHeight * .12f, 200f, 280f);
            float turnHeight = Mathf.Clamp(screenHeight * .05f, 80f, 108f);
            float gap = Mathf.Clamp(screenHeight * .009f, 16f, 22f);
            float minimumBattleHeight = Mathf.Max(screenHeight * .26f, 500f);
            float boardSide = Mathf.Min(screenWidth * .92f,
                screenHeight - bottomHeight - turnHeight - minimumBattleHeight - gap * 2f - 24f);
            float boardInnerSize = boardSide - 16f;
            float cellSize = boardInnerSize / DebugBoardColumns;

            GameObject viewportObject = new GameObject("TurnDebugBoardViewport",
                typeof(RectTransform), typeof(RectMask2D), typeof(GemSpecialVFXManager));
            GameObject cellGridObject = new GameObject("TurnDebugCellGrid", typeof(RectTransform));
            GameObject boardRootObject = new GameObject("TurnDebugGemLayer", typeof(RectTransform));
            GameObject vfxLayerObject = new GameObject("TurnDebugBoardVFXLayer", typeof(RectTransform));
            try
            {
                RectTransform viewport = (RectTransform)viewportObject.transform;
                viewport.anchorMin = viewport.anchorMax = new Vector2(.5f, .5f);
                viewport.sizeDelta = Vector2.one * boardInnerSize;
                RectTransform cellGrid = (RectTransform)cellGridObject.transform;
                cellGrid.SetParent(viewport, false);
                cellGrid.anchorMin = Vector2.zero;
                cellGrid.anchorMax = Vector2.one;
                cellGrid.offsetMin = cellGrid.offsetMax = Vector2.zero;
                GridLayoutGroup layout = cellGridObject.AddComponent<GridLayoutGroup>();
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = DebugBoardColumns;
                layout.cellSize = Vector2.one * cellSize;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
                layout.startAxis = GridLayoutGroup.Axis.Horizontal;

                RectTransform boardRoot = (RectTransform)boardRootObject.transform;
                boardRoot.SetParent(viewport, false);
                boardRoot.anchorMin = Vector2.zero;
                boardRoot.anchorMax = Vector2.one;
                boardRoot.offsetMin = boardRoot.offsetMax = Vector2.zero;
                GridLayoutGroup gemLayout = boardRootObject.AddComponent<GridLayoutGroup>();
                gemLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gemLayout.constraintCount = DebugBoardColumns;
                gemLayout.cellSize = layout.cellSize;

                RectTransform vfxLayer = (RectTransform)vfxLayerObject.transform;
                vfxLayer.SetParent(viewport, false);
                vfxLayer.anchorMin = Vector2.zero;
                vfxLayer.anchorMax = Vector2.one;
                vfxLayer.offsetMin = vfxLayer.offsetMax = Vector2.zero;

                BoardCellView[,] cells = new BoardCellView[DebugBoardRows, DebugBoardColumns];
                for (int row = 0; row < DebugBoardRows; row++)
                for (int column = 0; column < DebugBoardColumns; column++)
                {
                    GameObject cellObject = new GameObject($"TurnDebugCell_{row}_{column}",
                        typeof(RectTransform), typeof(BoardCellView));
                    cellObject.transform.SetParent(cellGrid, false);
                    BoardCellView cell = cellObject.GetComponent<BoardCellView>();
                    cell.Initialize(new BoardPos(row, column));
                    cells[row, column] = cell;
                }

                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(cellGrid);
                GemSpecialVFXManager vfx = viewportObject.GetComponent<GemSpecialVFXManager>();
                vfx.Configure(viewport, boardRoot, null, null, cells, vfxLayer);
                BoardPos[] crossCenters =
                {
                    new BoardPos(3, 3)
                };
                int seed = 0;
                foreach (BoardPos center in crossCenters)
                foreach (bool horizontal in new[] { true, false })
                {
                    Require(vfx.TryGetLineGeometryForDebug(center, horizontal,
                        out Vector2 start, out Vector2 end, out float gemCellSize),
                        $"Line-cross VFX at ({center.Row},{center.Col}) must have valid bounds.");
                    Vector3 expectedCenter = vfxLayer.InverseTransformPoint(
                        cells[center.Row, center.Col].RectTransform.TransformPoint(
                            cells[center.Row, center.Col].RectTransform.rect.center));
                    Vector2 actualCenter = (start + end) * .5f;
                    BoardPos expectedStartCell = horizontal
                        ? new BoardPos(3, 2)
                        : new BoardPos(4, 3);
                    BoardPos expectedEndCell = horizontal
                        ? new BoardPos(3, 4)
                        : new BoardPos(2, 3);
                    Vector2 expectedStart = vfxLayer.InverseTransformPoint(
                        cells[expectedStartCell.Row, expectedStartCell.Col].RectTransform.TransformPoint(
                            cells[expectedStartCell.Row, expectedStartCell.Col].RectTransform.rect.center));
                    Vector2 expectedEnd = vfxLayer.InverseTransformPoint(
                        cells[expectedEndCell.Row, expectedEndCell.Col].RectTransform.TransformPoint(
                            cells[expectedEndCell.Row, expectedEndCell.Col].RectTransform.rect.center));
                    Require(Vector2.Distance(actualCenter, expectedCenter) <= .01f
                        && Vector2.Distance(start, expectedStart) <= .01f
                        && Vector2.Distance(end, expectedEnd) <= .01f
                        && Mathf.Abs(horizontal ? end.y - start.y : end.x - start.x) <= .01f,
                        "A Line must draw zig-zag beams across exactly the five cells in its plus.");
                    LightningLineVFX lightning = CreateDebugLightning(vfxLayer, start, end,
                        horizontal, gemCellSize, seed++);
                    Require(lightning.PathPointCount == 13 && !lightning.raycastTarget
                        && Mathf.Approximately(lightning.Lifetime, LightningLineVFX.TotalLifetime)
                        && HasVisibleZigZag(lightning, horizontal, gemCellSize),
                        "Each Line-cross beam must animate without blocking board input.");
                    UnityEngine.Object.DestroyImmediate(lightning.gameObject);
                }

                Require(Mathf.Approximately(LightningLineVFX.ActivationDelay, .03f)
                    && LightningLineVFX.FlickerIntervalMin >= .025f
                    && LightningLineVFX.FlickerIntervalMax <= .05f,
                    "Lightning must start shortly after activation and regenerate paths several times per second.");

                Require(vfx.TryGetAreaExplosionGeometryForDebug(new BoardPos(0, 0),
                        out Vector2 explosionCenter, out Vector2 explosionCellSize,
                        out Vector2[] cornerCells)
                    && cornerCells.Length == 4,
                    "A corner AreaBomb must remain limited to its four valid cells.");

                Require(vfx.TryGetAreaExplosionGeometryForDebug(new BoardPos(3, 3),
                        out explosionCenter, out explosionCellSize, out Vector2[] centerCells)
                    && centerCells.Length == 9,
                    "A centered AreaBomb must remain limited to its nine valid cells.");
                CheckBombGlowAndCatalog(vfx, cells);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cellGridObject);
                UnityEngine.Object.DestroyImmediate(boardRootObject);
                UnityEngine.Object.DestroyImmediate(vfxLayerObject);
                UnityEngine.Object.DestroyImmediate(viewportObject);
            }
        }

        static LightningLineVFX CreateDebugLightning(RectTransform layer, Vector2 start,
            Vector2 end, bool horizontal, float cellSize, int seed)
        {
            GameObject lightningObject = new GameObject("TurnDebugLightning",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(LightningLineVFX));
            lightningObject.transform.SetParent(layer, false);
            LightningLineVFX lightning = lightningObject.GetComponent<LightningLineVFX>();
            lightning.SetUniqueSeed(seed + 0x51f15e);
            lightning.Play(start, end, horizontal, cellSize, .34f);
            return lightning;
        }

        static void CheckBombGlowAndCatalog(GemSpecialVFXManager vfx, BoardCellView[,] cells)
        {
            BoardVFXCatalog catalog = Resources.Load<BoardVFXCatalog>(
                "BoardVFX/BoardVFXCatalog");
            Require(catalog != null
                && catalog.TryGetEffect(BoardVFXEffect.BombRadiusGlow,
                    out BoardVFXCatalog.EffectEntry glowEntry)
                && glowEntry.mode == BoardVFXMode.CodeDrawn
                && Mathf.Approximately(glowEntry.scale, 3f),
                "BombRadiusGlow must be CodeDrawn and span three cells.");
            Require(catalog.TryGetEffect(BoardVFXEffect.Bomb3x3,
                    out BoardVFXCatalog.EffectEntry explosionEntry)
                && explosionEntry.mode == BoardVFXMode.CodeDrawn
                && Mathf.Approximately(explosionEntry.scale, 3f)
                && explosionEntry.scale < DebugBoardRows,
                "Bomb3x3 must be CodeDrawn and must not scale to the full board.");

            Dictionary<GemType, BoardBurstStyle> expectedStyles =
                new Dictionary<GemType, BoardBurstStyle>
                {
                    { GemType.Attack, BoardBurstStyle.Fire },
                    { GemType.Mana, BoardBurstStyle.Water },
                    { GemType.ExpDrain, BoardBurstStyle.Lotus },
                    { GemType.Rage, BoardBurstStyle.Sun },
                    { GemType.Health, BoardBurstStyle.Leaf },
                    { GemType.Shield, BoardBurstStyle.ShieldShards },
                    { GemType.Soul, BoardBurstStyle.Soul }
                };
            foreach (KeyValuePair<GemType, BoardBurstStyle> mapping in expectedStyles)
            {
                BoardVFXCatalog.GemStyleEntry style = vfx.GetBombStyleForDebug(mapping.Key);
                Require(style.gemType == mapping.Key && style.style == mapping.Value
                    && style.color.a > 0f,
                    $"Bomb3x3 must use the catalog's {mapping.Key} style and tint.");
            }

            GemView centerBomb = CreateDebugBombGem(new BoardPos(3, 3), GemType.Attack);
            GemView adjacentBomb = CreateDebugBombGem(new BoardPos(3, 4), GemType.Mana);
            try
            {
                vfx.RefreshBombRadiusGlows(new[] { centerBomb });
                Require(CountGlowingCells(cells) == 8
                    && cells[3, 3].GetOrCreateBombRadiusGlow().OverlapCount == 0,
                    "A center Bomb must glow exactly its eight neighboring cells, not its center.");
                bool hasAttackGlow = catalog.TryGetBombGlowColor(
                    GemType.Attack, out Color attackGlow);
                Require(hasAttackGlow
                    && cells[2, 3].GetOrCreateBombRadiusGlow().GlowColor == attackGlow,
                    "Bomb radius glow color must follow the Bomb's GemType.");

                centerBomb.SetPosition(new BoardPos(0, 3));
                vfx.RefreshBombRadiusGlows(new[] { centerBomb });
                Require(CountGlowingCells(cells) == 5,
                    "A Bomb on an edge must glow exactly five valid neighboring cells.");
                centerBomb.SetPosition(new BoardPos(0, 0));
                vfx.RefreshBombRadiusGlows(new[] { centerBomb });
                Require(CountGlowingCells(cells) == 3,
                    "A corner Bomb must glow exactly three valid neighboring cells.");

                centerBomb.SetPosition(new BoardPos(3, 3));
                vfx.RefreshBombRadiusGlows(new[] { centerBomb, adjacentBomb });
                Require(cells[2, 3].GetOrCreateBombRadiusGlow().OverlapCount == 2,
                    "Overlapping Bomb radii must intensify shared cells without removing either contribution.");
                vfx.RefreshBombRadiusGlows(Array.Empty<GemView>());
                Require(CountGlowingCells(cells) == 0,
                    "Clearing Bombs must remove their radius glow immediately.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(centerBomb.gameObject);
                UnityEngine.Object.DestroyImmediate(adjacentBomb.gameObject);
            }
        }

        static int CountGlowingCells(BoardCellView[,] cells)
        {
            int count = 0;
            for (int row = 0; row < cells.GetLength(0); row++)
            for (int column = 0; column < cells.GetLength(1); column++)
                if (cells[row, column].GetOrCreateBombRadiusGlow().OverlapCount > 0)
                    count++;
            return count;
        }

        static GemView CreateDebugBombGem(BoardPos position, GemType type)
        {
            GameObject gemObject = new GameObject($"TurnDebugBomb_{position.Row}_{position.Col}",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button), typeof(GemView));
            GemView gem = gemObject.GetComponent<GemView>();
            gem.Initialize(null, position, type, GemSpecialType.AreaBomb, null);
            return gem;
        }

        static bool HasVisibleZigZag(LightningLineVFX lightning, bool horizontal, float cellSize)
        {
            bool hasDeviation = false;
            float maximumDeviation = cellSize * .12f;
            for (int index = 1; index < lightning.PathPointCount - 1; index++)
            {
                Vector2 point = lightning.GetPointForDebug(index);
                if (horizontal)
                    hasDeviation |= Mathf.Abs(point.y - lightning.GetPointForDebug(0).y) > .001f;
                else
                    hasDeviation |= Mathf.Abs(point.x - lightning.GetPointForDebug(0).x) > .001f;

                float progress = index / (float)(lightning.PathPointCount - 1);
                float baseline = horizontal
                    ? Mathf.Lerp(lightning.GetPointForDebug(0).y, lightning.GetPointForDebug(lightning.PathPointCount - 1).y, progress)
                    : Mathf.Lerp(lightning.GetPointForDebug(0).x, lightning.GetPointForDebug(lightning.PathPointCount - 1).x, progress);
                float deviation = horizontal ? Mathf.Abs(point.y - baseline) : Mathf.Abs(point.x - baseline);
                if (deviation > maximumDeviation + .001f) return false;
            }
            return hasDeviation;
        }

        static void SetHorizontalRun(GemType[,] types, int row, int startColumn,
            int length, GemType type)
        {
            for (int column = startColumn; column < startColumn + length; column++)
                types[row, column] = type;
        }

        static void CheckSevenBySevenBoard()
        {
            GameObject root = new GameObject("TurnDebugSevenBySevenRoot", typeof(RectTransform));
            GameObject prefabObject = new GameObject("TurnDebugSevenBySevenGem",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(GemView));
            GameObject boardObject = new GameObject("TurnDebugSevenBySevenBoard");
            Sprite[] sprites = CreateTestSprites();
            UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
            try
            {
                RectTransform rootRect = (RectTransform)root.transform;
                rootRect.sizeDelta = new Vector2(800f, 800f);
                GridLayoutGroup layout = root.AddComponent<GridLayoutGroup>();
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = BoardManager.DefaultBoardSize;
                layout.cellSize = new Vector2(100f, 100f);

                prefabObject.GetComponent<Button>().targetGraphic = prefabObject.GetComponent<Image>();
                BoardManager board = boardObject.AddComponent<BoardManager>();
                board.Configure(root.transform, prefabObject.GetComponent<GemView>(), sprites);
                UnityEngine.Random.InitState(20261024);
                board.EnsureBoardReady();

                IReadOnlyList<GemView> gems = board.GetGemsInLayoutOrder();
                Require(board.Rows == BoardManager.DefaultBoardSize
                    && board.Columns == BoardManager.DefaultBoardSize
                    && gems.Count == BoardManager.DefaultBoardSize * BoardManager.DefaultBoardSize,
                    "Default BoardManager configuration must create a 7x7 board with 49 cells.");
                Require(!board.SoulGemEnabled && gems.All(gem => gem.Type != GemType.Soul),
                    "A board below Hero Level 10 must not spawn Soul gems.");
                Require(gems.Any(gem => gem.Type == GemType.Rage),
                    "Rage must be included in normal random gem generation.");
                Require(board.IsValidGemOrder(gems),
                    "The initial board must have no automatic match and at least one legal move.");
                Require(Enum.GetNames(typeof(GemType)).Length == 10
                    && Enum.IsDefined(typeof(GemType), nameof(GemType.Rainbow)),
                    "The gem enum must preserve all ten art IDs.");
                Require(gems.All(gem => IsSpawnableGemType(gem.Type)),
                    "Random board generation must use only the six configured normal gem types.");
            }
            finally
            {
                UnityEngine.Random.state = previousRandomState;
                DestroyDebugBoard(root, prefabObject, boardObject, sprites);
            }
        }

        static void CheckSingleColumnGravityPreservesUnaffectedColumns()
        {
            CheckGravityInSingleColumn(2);
            CheckGravityInSingleColumn(0);
            CheckGravityInSingleColumn(6);
        }

        static void CheckGravityInSingleColumn(int affectedColumn)
        {
            GameObject root = new GameObject($"TurnDebugGravityColumn{affectedColumn}Root", typeof(RectTransform));
            GameObject prefabObject = new GameObject($"TurnDebugGravityColumn{affectedColumn}Gem",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(GemView));
            GameObject boardObject = new GameObject($"TurnDebugGravityColumn{affectedColumn}Board");
            Sprite[] sprites = CreateTestSprites();
            UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
            try
            {
                GridLayoutGroup layout = root.AddComponent<GridLayoutGroup>();
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = DebugBoardColumns;
                layout.cellSize = new Vector2(100f, 100f);
                BoardManager board = ConfigureDebugBoard(root, prefabObject, boardObject, sprites);

                GemType[,] types = BaselineTypes();
                GemSpecialType[,] specials = new GemSpecialType[DebugBoardRows, DebugBoardColumns];
                types[3, affectedColumn] = GemType.Attack;
                types[4, affectedColumn] = GemType.Mana;
                types[5, affectedColumn] = GemType.Attack;
                types[6, affectedColumn] = GemType.Attack;
                board.SetBoardForDebug(types, specials);

                List<GemView> beforeGems = board.GetGemsInLayoutOrder();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform);
                GemView[,] unaffectedGems = new GemView[DebugBoardRows, DebugBoardColumns];
                Vector2[,] unaffectedPositions = new Vector2[DebugBoardRows, DebugBoardColumns];
                long[,] originalRuntimeIds = new long[DebugBoardRows, DebugBoardColumns];
                GemType[,] originalTypes = new GemType[DebugBoardRows, DebugBoardColumns];
                GemSpecialType[,] originalSpecials = new GemSpecialType[DebugBoardRows, DebugBoardColumns];
                Sprite[,] originalSprites = new Sprite[DebugBoardRows, DebugBoardColumns];
                List<GemView> survivingColumnGems = new List<GemView>();
                Dictionary<GemView, int> originalRows = new Dictionary<GemView, int>();
                HashSet<long> originalIds = new HashSet<long>();
                for (int row = 0; row < DebugBoardRows; row++)
                for (int column = 0; column < DebugBoardColumns; column++)
                {
                    GemView gem = beforeGems[row * DebugBoardColumns + column];
                    originalIds.Add(gem.RuntimeId);
                    originalRuntimeIds[row, column] = gem.RuntimeId;
                    originalTypes[row, column] = gem.Type;
                    originalSpecials[row, column] = gem.SpecialType;
                    originalSprites[row, column] = gem.SpriteImage.sprite;
                    if (column == affectedColumn)
                    {
                        if (row <= 2 || row == 4)
                        {
                            survivingColumnGems.Add(gem);
                            originalRows.Add(gem, row);
                        }
                        continue;
                    }
                    unaffectedGems[row, column] = gem;
                    unaffectedPositions[row, column] = ((RectTransform)gem.transform).anchoredPosition;
                }

                MoveResult move = board.EvaluateMove(new BoardPos(3, affectedColumn),
                    new BoardPos(4, affectedColumn));
                Require(move.IsValid,
                    $"The deterministic gravity board must contain a valid Match-3 move in column {affectedColumn}.");
                UnityEngine.Random.InitState(20261025);
                MoveResult resolved = null;
                Finish(board.ResolveMove(move, result => resolved = result));
                Require(resolved != null && resolved.WasValid,
                    "The deterministic conservation Match-3 must resolve successfully.");
                Require(resolved.CascadeCount == 0 && resolved.InitialMatch.ClearedCount == 3,
                    "The deterministic conservation scenario must resolve exactly one Match-3 wave.");

                List<GemView> afterGems = board.GetGemsInLayoutOrder();
                int newGemCount = afterGems.Count(gem =>
                    !beforeGems.Any(previous => ReferenceEquals(previous, gem)));
                int removedGemCount = beforeGems.Count(gem =>
                    !afterGems.Any(current => ReferenceEquals(current, gem)));
                Require(newGemCount == 3 && removedGemCount == 3,
                    "A single Match-3 must replace exactly its three destroyed GemViews.");
                Require(board.LastGravityMovementRecords.Count == 4,
                    $"Gravity in column {affectedColumn} must record exactly four surviving Gem movements.");
                foreach (BoardManager.GravityMovementDebugRecord movement in board.LastGravityMovementRecords)
                {
                    Require(movement.Source.Col == affectedColumn && movement.Destination.Col == affectedColumn,
                        "Every gravity movement record must stay in the affected column.");
                    Require(Mathf.Abs(movement.SourcePosition.x - movement.DestinationPosition.x) <= .01f,
                        "Every surviving Gem must have zero horizontal gravity displacement.");
                }

                foreach (GemView original in survivingColumnGems)
                {
                    int sourceRow = originalRows[original];
                    int destinationRow = sourceRow == 4 ? 6 : sourceRow + 3;
                    GemView fallen = afterGems[destinationRow * DebugBoardColumns + affectedColumn];
                    Require(ReferenceEquals(fallen, original)
                        && fallen.Position.Equals(new BoardPos(destinationRow, affectedColumn))
                        && fallen.RuntimeId == originalRuntimeIds[sourceRow, affectedColumn]
                        && fallen.Type == originalTypes[sourceRow, affectedColumn]
                        && fallen.SpecialType == originalSpecials[sourceRow, affectedColumn]
                        && fallen.SpriteImage.sprite == originalSprites[sourceRow, affectedColumn],
                        $"Surviving Gems in column {affectedColumn} must retain identity while falling.");
                }
                int createdAtTop = 0;
                for (int row = 0; row < DebugBoardRows; row++)
                {
                    GemView gem = afterGems[row * DebugBoardColumns + affectedColumn];
                    bool existedBefore = originalIds.Contains(gem.RuntimeId);
                    if (row < 3)
                    {
                        Require(!existedBefore && IsSpawnableGemType(gem.Type),
                            "Each top refill position must contain exactly one newly spawned normal Gem.");
                        createdAtTop++;
                    }
                    else
                    {
                        Require(existedBefore,
                            "Gravity must not create new Gems outside the exact top refill positions.");
                    }
                }
                Require(createdAtTop == 3,
                    $"Gravity in column {affectedColumn} must create exactly three Gems at the top.");

                for (int row = 0; row < DebugBoardRows; row++)
                for (int column = 0; column < DebugBoardColumns; column++)
                {
                    if (column == affectedColumn) continue;
                    GemView actual = afterGems[row * DebugBoardColumns + column];
                    GemView expected = unaffectedGems[row, column];
                    Require(ReferenceEquals(actual, expected),
                        $"Unaffected column {column} row {row} must retain the same GemView.");
                    Require(actual.Position.Equals(new BoardPos(row, column))
                        && actual.RuntimeId == originalRuntimeIds[row, column]
                        && actual.Type == originalTypes[row, column]
                        && actual.SpecialType == originalSpecials[row, column]
                        && actual.SpriteImage.sprite == originalSprites[row, column],
                        $"Unaffected column {column} row {row} must retain its complete logical identity.");
                    Require(((RectTransform)actual.transform).anchoredPosition
                            == unaffectedPositions[row, column],
                        $"Unaffected column {column} row {row} must not shift visually during refill.");
                }
                Debug.Log($"Single-column gravity regression passed for column {affectedColumn}.");
            }
            finally
            {
                UnityEngine.Random.state = previousRandomState;
                DestroyDebugBoard(root, prefabObject, boardObject, sprites);
            }
        }

        static void CheckSpecialGemGravityPreservesState()
        {
            GameObject root = new GameObject("TurnDebugSpecialGravityRoot", typeof(RectTransform));
            GameObject prefabObject = new GameObject("TurnDebugSpecialGravityGem",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(GemView));
            GameObject boardObject = new GameObject("TurnDebugSpecialGravityBoard");
            Sprite[] sprites = CreateTestSprites();
            UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
            try
            {
                GridLayoutGroup layout = root.AddComponent<GridLayoutGroup>();
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = DebugBoardColumns;
                layout.cellSize = new Vector2(100f, 100f);
                BoardManager board = ConfigureDebugBoard(root, prefabObject, boardObject, sprites);
                GemType[,] types = BaselineTypes();
                GemSpecialType[,] specials = new GemSpecialType[DebugBoardRows, DebugBoardColumns];
                types[0, 0] = GemType.Attack;
                types[1, 0] = GemType.ExpDrain;
                types[2, 0] = GemType.Health;
                types[3, 0] = GemType.Rainbow;
                types[4, 0] = GemType.Attack;
                types[5, 0] = GemType.Attack;
                types[6, 0] = GemType.Mana;
                types[6, 1] = GemType.Attack;
                specials[0, 0] = GemSpecialType.Line;
                specials[1, 0] = GemSpecialType.Line;
                specials[2, 0] = GemSpecialType.AreaBomb;
                specials[3, 0] = GemSpecialType.Rainbow;
                board.SetBoardForDebug(types, specials);

                List<GemView> before = board.GetGemsInLayoutOrder();
                GemView[] specialGems =
                {
                    before[0], before[DebugBoardColumns], before[2 * DebugBoardColumns],
                    before[3 * DebugBoardColumns]
                };
                long[] runtimeIds = specialGems.Select(gem => gem.RuntimeId).ToArray();
                GemType[] gemTypes = specialGems.Select(gem => gem.Type).ToArray();
                GemSpecialType[] specialTypes = specialGems.Select(gem => gem.SpecialType).ToArray();
                Sprite[] gemSpritesBefore = specialGems.Select(gem => gem.SpriteImage.sprite).ToArray();

                MoveResult move = board.EvaluateMove(new BoardPos(6, 1), new BoardPos(6, 0));
                Require(move.IsValid, "The special-gravity scenario must create one bottom-column Match-3.");
                UnityEngine.Random.InitState(20261025);
                MoveResult resolved = null;
                Finish(board.ResolveMove(move, result => resolved = result));
                Require(resolved != null && resolved.WasValid && resolved.CascadeCount == 0,
                    "The special-gravity scenario must resolve exactly one wave.");

                List<GemView> after = board.GetGemsInLayoutOrder();
                for (int index = 0; index < specialGems.Length; index++)
                {
                    GemView fallen = after[(index + 3) * DebugBoardColumns];
                    Require(ReferenceEquals(fallen, specialGems[index])
                        && fallen.RuntimeId == runtimeIds[index]
                        && fallen.Type == gemTypes[index]
                        && fallen.SpecialType == specialTypes[index]
                        && fallen.SpriteImage.sprite == gemSpritesBefore[index],
                        $"{specialTypes[index]} must retain its full state while falling.");
                }
                Require(after.Take(3).All(gem => !specialGems.Contains(gem))
                    && after.Take(3).All(gem => IsSpawnableGemType(gem.Type)),
                    "Gravity must refill only the three top holes with configured normal Gems.");
            }
            finally
            {
                UnityEngine.Random.state = previousRandomState;
                DestroyDebugBoard(root, prefabObject, boardObject, sprites);
            }
        }

        static void CheckRageAndMultiHitCombat()
        {
            GameObject battleObject = new GameObject("TurnDebugRageBattle");
            try
            {
                BattleController battle = battleObject.AddComponent<BattleController>();
                battle.Configure(null, null, null);
                battle.ConfigureDebugStats(500, 1000);

                Finish(battle.ResolveMove(BattleActor.Player, ResourceMove(GemType.Rage, 3)));
                Finish(battle.ResolveMove(BattleActor.Boss, ResourceMove(GemType.Rage, 4)));
                Finish(battle.ResolveMove(BattleActor.Player, ResourceMove(GemType.Rage, 5)));
                Finish(battle.ResolveMove(BattleActor.Boss, ResourceMove(GemType.Rage, 6)));
                Require(battle.HeroRage == 40 && battle.BossRage == 50,
                    "Orange matches must grant 5 Rage per gem independently to the acting character.");

                battle.SetRageForDebug(BattleActor.Player, 98);
                Finish(battle.ResolveMove(BattleActor.Player, ResourceMove(GemType.Rage, 1)));
                Require(battle.HeroRage == battle.MaxRage && battle.BossRage == 50,
                    "Orange Rage must cap at 100 without changing the opponent's Rage.");

                Finish(battle.ResolveMove(BattleActor.Player, ResourceMove(GemType.Mana, 1)));
                Require(battle.HeroRage == battle.MaxRage,
                    "Mana actions must not consume Critical-ready Rage.");

                Finish(battle.ResolveMove(BattleActor.Player, DamageMove(3)));
                Require(battle.BossHealth == 640 && battle.BossRage == 53 && battle.HeroRage == 0,
                    "A Critical Match-3 must apply three 120-damage hits, grant three received-hit Rage, then consume Hero Rage.");

                Finish(battle.ResolveMove(BattleActor.Boss, DamageMove(5)));
                Require(battle.HeroHealth == 400 && battle.HeroRage == 5,
                    "A normal Match-5 must apply five 20-damage hits and grant five Rage to Hero.");

                battle.SetRageForDebug(BattleActor.Boss, 99);
                Finish(battle.ResolveMove(BattleActor.Player, DamageMove(1)));
                Require(battle.BossHealth == 580 && battle.BossRage == battle.MaxRage,
                    "Incoming hits must clamp Rage at its maximum.");
                Finish(battle.ResolveMove(BattleActor.Boss, DamageMove(1)));
                Require(battle.BossHealth == 580 && battle.HeroHealth == 360
                    && battle.BossRage == 0 && battle.HeroRage == 6,
                    "A Boss at max Rage must critical-hit for 40, consume Rage after its action, and grant the receiver one Rage.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(battleObject);
            }
        }

        static void CheckBattleGemValues()
        {
            GameObject battleObject = new GameObject("TurnDebugGemValuesBattle");
            try
            {
                BattleController battle = battleObject.AddComponent<BattleController>();
                battle.Configure(null, null, null);
                battle.ConfigureDebugStats(500, 1000);

                Finish(battle.ResolveMove(BattleActor.Player, ResourceMove(GemType.Shield, 1)));
                Require(battle.HeroShield == 50, "A Shield match must grant 50 shield HP per gem.");
                battle.AdvancePlayerTurn();
                Finish(battle.ResolveMove(BattleActor.Player, ResourceMove(GemType.Shield, 2)));
                Require(battle.HeroShield == 150,
                    "New Shield gems must stack onto the remaining shield and refresh its duration.");
                Finish(battle.ResolveMove(BattleActor.Boss, DamageMove(1)));
                Require(battle.HeroShield == 130 && battle.HeroHealth == 500,
                    "Stacked Shield HP must absorb ordinary incoming damage before Hero HP.");
                battle.AdvancePlayerTurn();
                Require(battle.HeroShield == 130, "Shield must remain active for its second player turn.");
                battle.AdvancePlayerTurn();
                Require(battle.HeroShield == 0, "Shield must expire after two player turns without a refresh.");

                Finish(battle.ResolveMove(BattleActor.Boss, DamageMove(1)));
                Finish(battle.ResolveMove(BattleActor.Player, ResourceMove(GemType.Health, 1)));
                Require(battle.HeroHealth == 500, "A Health match must restore 50 HP and respect the health cap.");
                Finish(battle.ResolveMove(BattleActor.Player, ResourceMove(GemType.Mana, 1)));
                Require(battle.HeroMana == 10, "A Mana match must grant 10 Mana per gem.");
                Finish(battle.ResolveMove(BattleActor.Player, ResourceMove(GemType.Rage, 1)));
                Require(battle.HeroRage == 5, "A Rage match must grant 5 Rage per gem.");

                Finish(battle.ResolveMove(BattleActor.Boss, DamageMove(1)));
                Finish(battle.ResolveMove(BattleActor.Boss, ResourceMove(GemType.Mana, 10)));
                battle.SetRageForDebug(BattleActor.Boss, 10);
                Finish(battle.ResolveMove(BattleActor.Player, ResourceMove(GemType.ExpDrain, 1)));
                Require(battle.BossHealth == 980 && battle.HeroHealth == 500
                    && battle.BossMana == 99 && battle.HeroMana == 11
                    && battle.BossRage == 5 && battle.HeroRage == 12,
                    "ExpDrain must transfer only available resources within the receiving caps.");

                int bossHealthBeforeAttack = battle.BossHealth;
                Finish(battle.ResolveMove(BattleActor.Player, DamageMove(1)));
                Require(bossHealthBeforeAttack - battle.BossHealth == 60,
                    "A Level-1 Attack gem must deal exactly 60 damage.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(battleObject);
            }
        }

        static void CheckShieldBlocksSoulDamage()
        {
            GameObject battleObject = new GameObject("TurnDebugShieldSoulBattle");
            HeroData heroData = ScriptableObject.CreateInstance<HeroData>();
            BossData bossData = ScriptableObject.CreateInstance<BossData>();
            try
            {
                heroData.Configure("debug_hero", "Debug Hero", null, null, null, null,
                    500, 20, 100, 100, 1f, Vector2.zero, CharacterFacing.Right);
                bossData.Configure("debug_boss", "Debug Boss", null, 1000, 20, 100,
                    0, 5, 1, true, 2f, BossStatusEffectCapability.None);
                RuntimeHeroStats heroStats = new RuntimeHeroStats(heroData,
                    new HeroProgressSaveData { level = 10 });
                RuntimeBossStats bossStats = new RuntimeBossStats(bossData, 10);
                BattleController battle = battleObject.AddComponent<BattleController>();
                battle.Configure(null, null, null,
                    runtimeHeroStats: heroStats, runtimeBossStats: bossStats);

                Finish(battle.ResolveMove(BattleActor.Player, ResourceMove(GemType.Shield, 1)));
                int soulBefore = battle.HeroSoulHealth;
                Finish(battle.ResolveMove(BattleActor.Boss, ResourceMove(GemType.Soul, 1)));
                Require(battle.HeroSoulHealth == soulBefore && battle.HeroHealth == battle.HeroMaxHealth
                    && battle.HeroShield == 30,
                    "Shield must absorb Soul damage before it reaches Soul HP.");
                Finish(battle.ResolveMove(BattleActor.Boss, DamageMove(1)));
                Require(battle.HeroHealth == battle.HeroMaxHealth && battle.HeroShield == 10,
                    "The same Shield must also absorb ordinary HP damage.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(battleObject);
                UnityEngine.Object.DestroyImmediate(heroData);
                UnityEngine.Object.DestroyImmediate(bossData);
            }
        }

        static void CheckSoulLevelRules()
        {
            HeroData heroData = ScriptableObject.CreateInstance<HeroData>();
            BossData bossData = ScriptableObject.CreateInstance<BossData>();
            CampaignCatalog catalog = ScriptableObject.CreateInstance<CampaignCatalog>();
            RegionData[] regions = new RegionData[3];
            GameObject root = new GameObject("TurnDebugSoulBoardRoot", typeof(RectTransform));
            GameObject prefabObject = new GameObject("TurnDebugSoulGem",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(GemView));
            GameObject boardObject = new GameObject("TurnDebugSoulBoard");
            GameObject lowLevelRoot = new GameObject("TurnDebugLowLevelSoulBoardRoot",
                typeof(RectTransform));
            GameObject lowLevelBoardObject = new GameObject("TurnDebugLowLevelSoulBoard");
            Sprite[] sprites = CreateTestSprites();
            UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
            try
            {
                heroData.Configure("debug_hero", "Debug Hero", null, null, null, null,
                    500, 20, 100, 100, 1f, Vector2.zero, CharacterFacing.Right);
                RuntimeHeroStats levelNine = new RuntimeHeroStats(heroData,
                    new HeroProgressSaveData { level = 9 });
                RuntimeHeroStats levelTen = new RuntimeHeroStats(heroData,
                    new HeroProgressSaveData { level = 10 });
                Require(levelNine.MaxHP == 1300 && levelNine.MaxSoulHP == 1360
                    && levelTen.MaxHP == 1400 && levelTen.MaxSoulHP == 1480,
                    "Hero soul HP must use 20% MaxHP + 200 + Level * 100.");

                bossData.Configure("debug_boss", "Debug Boss", null, 2500, 20, 100,
                    0, 5, 1, true, 2f, BossStatusEffectCapability.None);
                for (int index = 0; index < regions.Length; index++)
                {
                    regions[index] = ScriptableObject.CreateInstance<RegionData>();
                    regions[index].Configure($"region_{index + 1:00}",
                        $"Region {index + 1}", null, Vector2.zero,
                        new StageData[10], string.Empty, Color.white, false);
                }
                catalog.Configure(null, regions, null, null, null, null, null, null,
                    null, null, null, null, null, null, null);
                StageData finalStage = new StageData();
                finalStage.Configure("region_03_stage_10", 10, "Stage 10",
                    regions[2].RegionId, true, Vector2.zero, bossData);
                int regionIndex = catalog.GetRegionIndex(finalStage.RegionId);
                int bossLevel = regionIndex * 10 + finalStage.StageNumber;
                RuntimeBossStats bossStats = new RuntimeBossStats(bossData, bossLevel);
                Require(regionIndex == 2 && bossLevel == (3 - 1) * 10 + 10
                    && bossStats.Level == 30 && bossStats.MaxSoulHP == 16500,
                    "Boss level must follow (Region - 1) * 10 + Stage and use that level for Soul HP.");

                GridLayoutGroup lowLevelLayout = lowLevelRoot.AddComponent<GridLayoutGroup>();
                lowLevelLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                lowLevelLayout.constraintCount = DebugBoardColumns;
                lowLevelLayout.cellSize = new Vector2(100f, 100f);
                BoardManager lowLevelBoard = ConfigureDebugBoard(lowLevelRoot,
                    prefabObject, lowLevelBoardObject, sprites,
                    enableSoulGem: true);
                UnityEngine.Random.InitState(20261031);
                lowLevelBoard.EnsureBoardReady();
                Require(lowLevelBoard.SoulGemEnabled,
                    "Soul gems must be available from the starting hero level.");

                GridLayoutGroup layout = root.AddComponent<GridLayoutGroup>();
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = DebugBoardColumns;
                layout.cellSize = new Vector2(100f, 100f);
                BoardManager board = ConfigureDebugBoard(root, prefabObject, boardObject,
                    sprites, enableSoulGem: true);
                GemType[,] types = BaselineTypes();
                types[3, 3] = GemType.Soul;
                board.SetBoardForDebug(types, new GemSpecialType[DebugBoardRows, DebugBoardColumns]);
                Require(board.SoulGemEnabled && board.GetTypeAt(new BoardPos(3, 3)) == GemType.Soul,
                    "A starting-level board must allow Soul gems to spawn.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(prefabObject);
                UnityEngine.Object.DestroyImmediate(boardObject);
                UnityEngine.Object.DestroyImmediate(lowLevelRoot);
                UnityEngine.Object.DestroyImmediate(lowLevelBoardObject);
                UnityEngine.Random.state = previousRandomState;
                foreach (Sprite sprite in sprites)
                {
                    if (sprite == null) continue;
                    Texture2D texture = sprite.texture;
                    UnityEngine.Object.DestroyImmediate(sprite);
                    if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
                }
                UnityEngine.Object.DestroyImmediate(heroData);
                UnityEngine.Object.DestroyImmediate(bossData);
                UnityEngine.Object.DestroyImmediate(catalog);
                foreach (RegionData region in regions)
                    if (region != null)
                        UnityEngine.Object.DestroyImmediate(region);
            }
        }

        static void CheckRageHud()
        {
            GameObject root = new GameObject("TurnDebugRageHud");
            try
            {
                Slider heroHp = CreateDebugSlider(root.transform, "HeroHP");
                Slider bossHp = CreateDebugSlider(root.transform, "BossHP");
                bossHp.direction = Slider.Direction.RightToLeft;
                Slider heroRage = CreateDebugSlider(root.transform, "HeroRage");
                Slider bossRage = CreateDebugSlider(root.transform, "BossRage");
                TMP_Text heroHpText = CreateDebugText(root.transform, "HeroHPText");
                TMP_Text bossHpText = CreateDebugText(root.transform, "BossHPText");
                TMP_Text heroRageText = CreateDebugText(root.transform, "HeroRageText");
                TMP_Text bossRageText = CreateDebugText(root.transform, "BossRageText");
                BattleHUD hud = root.AddComponent<BattleHUD>();
                hud.Configure(heroHp, bossHp, heroRage, bossRage, null, null,
                    heroHpText, bossHpText, heroRageText, bossRageText, null, null);

                hud.Render(400, 500, 750, 1000, 15, 65, 100, 0, 0);
                Require(heroHp.value == 400 && bossHp.value == 750
                    && heroRage.value == 15 && bossRage.value == 65
                    && heroRageText.text == "15 / 100" && bossRageText.text == "65 / 100",
                    "HP and each character's Rage bar/text must update independently.");
                hud.Render(300, 500, 500, 1000, 40, 100, 100, 0, 0);
                Require(heroHp.value == 300 && bossHp.value == 500
                    && heroRage.value == 40 && bossRage.value == 100
                    && heroHpText.text == "300 / 500" && bossHpText.text == "500 / 1000"
                    && bossRageText.text == "RAGE MAX"
                    && Mathf.Approximately(heroHp.fillRect.anchorMax.x, .6f)
                    && Mathf.Approximately(bossHp.fillRect.anchorMin.x, .5f)
                    && Mathf.Approximately(bossHp.fillRect.anchorMax.x, 1f),
                    "HP loss, healing, and Rage gain must update each bound Slider and value label.");
                hud.Render(400, 500, 750, 1000, 100, 100, 100, 0, 0);
                Require(heroRageText.text == "RAGE MAX" && bossRageText.text == "RAGE MAX"
                    && heroRage.fillRect.GetComponent<Image>().color != new Color32(242, 112, 43, 255),
                    "Full Rage must show readiness and brighten its bar.");
                hud.Render(0, 0, 0, 0, 0, 0, 0, 0, 0);
                Require(heroHp.value == 0 && bossHp.value == 0
                    && heroRage.value == 0 && bossRage.value == 0
                    && heroRageText.text == "0 / 0" && bossRageText.text == "0 / 0",
                    "HUD bars must safely clamp to zero when a maximum value is unavailable.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        static Slider CreateDebugSlider(Transform parent, string name)
        {
            GameObject sliderObject = new GameObject(name,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
            sliderObject.transform.SetParent(parent, false);
            Slider slider = sliderObject.GetComponent<Slider>();
            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.transform.SetParent(sliderObject.transform, false);
            slider.fillRect = (RectTransform)fillObject.transform;
            return slider;
        }

        static TMP_Text CreateDebugText(Transform parent, string name)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            return textObject.GetComponent<TextMeshProUGUI>();
        }

        static void CheckCascadeMatchFourDoesNotGrantExtraTurn(SpecialGemResolver resolver)
        {
            MatchResult match = resolver.ResolveCurrent(LineBoard(4, true, GemSpecialType.None),
                new MoveResult(default, default), false, 2, false);
            Require(match.CreatedSpecial == GemSpecialType.Line && !match.GrantsExtraTurn,
                "Cascade Match-4 may create a line special but cannot grant an extra turn.");
            MatchResult initialMatch = resolver.ResolveCurrent(LineBoard(3, true, GemSpecialType.None),
                new MoveResult(default, default), true, 1, false);
            MoveResult action = new MoveResult(default, default);
            action.AddMatch(initialMatch);
            action.AddMatch(match);
            Require(action.ExtraTurnsGranted == 0 && action.CreatedMatch4 && action.CascadeCount == 1,
                "Cascade Match-4 must report creation without granting an extra turn.");
        }

        static void CheckLineSpecialRequiresMatch(SpecialGemResolver resolver)
        {
            BoardSnapshot board = SpecialBoard(new BoardPos(3, 2), GemSpecialType.Line);
            MoveResult move = resolver.EvaluateDirectActivation(board, new BoardPos(3, 2));
            Require(!move.IsValid,
                "Line specials must not activate directly; they activate when included in a match.");
        }

        static void CheckLineClearsCross(SpecialGemResolver resolver)
        {
            GemType[,] types = BaselineTypes();
            GemSpecialType[,] specials = new GemSpecialType[DebugBoardRows, DebugBoardColumns];
            for (int column = 2; column <= 4; column++)
                types[3, column] = GemType.Attack;
            specials[3, 3] = GemSpecialType.Line;
            MatchResult match = resolver.ResolveCurrent(new BoardSnapshot(types, specials),
                new MoveResult(new BoardPos(3, 3), new BoardPos(3, 4)), true, 1, false);
            HashSet<BoardPos> expected = new HashSet<BoardPos>
            {
                new BoardPos(3, 2), new BoardPos(3, 3), new BoardPos(3, 4),
                new BoardPos(2, 3), new BoardPos(4, 3)
            };
            Require(match.SpecialActivations.Any(activation =>
                    activation.Kind == SpecialActivationKind.Line)
                && match.ClearPositions.Count == 5
                && expected.SetEquals(match.ClearPositions),
                "A matched Line must clear exactly the five cells in a plus, with its center counted once.");
        }

        static void CheckDirectAreaBomb(SpecialGemResolver resolver)
        {
            BoardSnapshot board = SpecialBoard(new BoardPos(3, 3), GemSpecialType.AreaBomb);
            MoveResult move = resolver.EvaluateDirectActivation(board, new BoardPos(3, 3));
            Require(move.IsValid && move.InitialMatch.ClearPositions.Count == 9,
                "Direct AreaBomb activation must clear its in-bounds 3x3 area.");
        }

        static void CheckLightningAndBaguaActivation(SpecialGemResolver resolver)
        {
            GemType[,] types = new GemType[7, 7];
            GemSpecialType[,] specials = new GemSpecialType[7, 7];
            for (int row = 0; row < 7; row++)
            for (int column = 0; column < 7; column++)
                types[row, column] = (GemType)((row + column) % 6);
            types[0, 0] = GemType.Lightning;
            specials[0, 0] = GemSpecialType.Lightning;
            BoardSnapshot board = new BoardSnapshot(types, specials);

            MoveResult lightning = resolver.EvaluateDirectActivation(board, new BoardPos(0, 0));
            MatchResult lightningResult = resolver.ResolveCurrent(board, lightning,
                false, 1, true);
            int destroyedOrdinaryGems = lightningResult.GemCounts.Values.Sum();
            Require(lightning.IsValid && lightningResult.ClearPositions.Count >= 6
                && lightningResult.ClearPositions.Count <= 11
                && lightningResult.ClearPositions.Contains(new BoardPos(0, 0))
                && destroyedOrdinaryGems >= 5 && destroyedOrdinaryGems <= 10,
                "Lightning must consume itself and clear 5-10 ordinary gems.");
            Require(TurnManager.ConsumeTurn(1, lightning.ExtraTurnsGranted, 5) == 0,
                "Direct Lightning activation must consume a turn without granting an Extra Turn.");
            Require(lightningResult.ClearPositions.All(position =>
                    position.Equals(new BoardPos(0, 0))
                    || board.SpecialAt(position) == GemSpecialType.None)
                && !lightningResult.GemCounts.ContainsKey(GemType.Lightning),
                "Lightning must not clear other specials or count itself as a normal gem.");

            types[0, 0] = GemType.Bagua;
            specials[0, 0] = GemSpecialType.Bagua;
            MoveResult bagua = resolver.EvaluateDirectActivation(
                new BoardSnapshot(types, specials), new BoardPos(0, 0));
            Require(bagua.IsValid && bagua.IsBaguaShuffle
                && bagua.ExtraTurnsGranted == 1
                && TurnManager.ConsumeTurn(1, bagua.ExtraTurnsGranted, 5) == 1,
                "Bagua must be directly activatable and grant exactly one extra turn.");
        }

        static void CheckBaguaClickConsumesGem()
        {
            GameObject root = new GameObject("TurnDebugBaguaBoardRoot",
                typeof(RectTransform));
            GameObject prefabObject = new GameObject("TurnDebugBaguaGem",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button), typeof(GemView));
            GameObject boardObject = new GameObject("TurnDebugBaguaBoard");
            Sprite[] sprites = CreateTestSprites();
            UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
            try
            {
                GridLayoutGroup layout = root.AddComponent<GridLayoutGroup>();
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = DebugBoardColumns;
                layout.cellSize = new Vector2(100f, 100f);
                BoardManager board = ConfigureDebugBoard(root, prefabObject,
                    boardObject, sprites);

                GemType[,] types = BaselineTypes();
                GemSpecialType[,] specials =
                    new GemSpecialType[DebugBoardRows, DebugBoardColumns];
                BoardPos consumedPosition = new BoardPos(6, 4);
                types[consumedPosition.Row, consumedPosition.Col] = GemType.Bagua;
                specials[consumedPosition.Row, consumedPosition.Col] = GemSpecialType.Bagua;
                board.SetBoardForDebug(types, specials);
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform);
                UnityEngine.Random.InitState(20261031);

                List<GemView> beforeGems = board.GetGemsInLayoutOrder();
                GemView consumedGem = beforeGems[consumedPosition.Row * DebugBoardColumns
                    + consumedPosition.Col];
                Dictionary<long, (GemType Type, GemSpecialType Special, BoardPos Position)> preservedGems =
                    beforeGems.Where(gem => !ReferenceEquals(gem, consumedGem))
                        .ToDictionary(gem => gem.RuntimeId,
                            gem => (gem.Type, gem.SpecialType, gem.Position));
                MoveResult activation = new SpecialGemResolver().EvaluateDirectActivation(
                    board.CreateSnapshot(), consumedPosition);
                MoveResult resolved = null;
                Finish(board.ResolveMove(activation, result => resolved = result));

                List<GemView> afterGems = board.GetGemsInLayoutOrder();
                HashSet<long> preservedIds = new HashSet<long>(preservedGems.Keys);
                GemView[] refilledGems = afterGems
                    .Where(gem => !preservedIds.Contains(gem.RuntimeId))
                    .ToArray();
                Require(resolved != null && resolved.WasValid
                    && resolved.IsBaguaShuffle && resolved.ExtraTurnsGranted == 1
                    && TurnManager.ConsumeTurn(1, resolved.ExtraTurnsGranted, 5) == 1,
                    "A Bagua click must shuffle the board and grant exactly one extra player turn.");
                Require(!consumedGem.gameObject.activeSelf
                    && afterGems.Count == DebugBoardRows * DebugBoardColumns
                    && afterGems.All(gem => gem != null)
                    && !afterGems.Any(gem => ReferenceEquals(gem, consumedGem))
                    && afterGems.All(gem => gem.SpecialType != GemSpecialType.Bagua),
                    "Bagua must be consumed before refill and must not remain available after shuffling.");
                Require(refilledGems.Length == 1
                    && IsSpawnableGemType(refilledGems[0].Type)
                    && refilledGems[0].SpecialType == GemSpecialType.None,
                    "The consumed Bagua cell must refill with exactly one ordinary, non-special Gem.");
                foreach (KeyValuePair<long, (GemType Type, GemSpecialType Special, BoardPos Position)> preserved
                    in preservedGems)
                {
                    GemView gem = afterGems.FirstOrDefault(candidate =>
                        candidate.RuntimeId == preserved.Key);
                    Require(gem != null && gem.Type == preserved.Value.Type
                        && gem.SpecialType == preserved.Value.Special,
                        "Shuffling after Bagua consumption must preserve every other Gem's type and special.");
                }
                Require(preservedGems.Any(preserved =>
                    afterGems.Any(gem => gem.RuntimeId == preserved.Key
                        && !gem.Position.Equals(preserved.Value.Position))),
                    "Bagua must actually reorder the surviving Gem instances, not merely refill the consumed cell.");
                Require(board.IsValidGemOrder(afterGems)
                    && !new SpecialGemResolver().EvaluateDirectActivation(
                        board.CreateSnapshot(), consumedPosition).IsValid,
                    "The post-Bagua board must have no immediate match, retain a legal move, and not reactivate at the old cell.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(prefabObject);
                UnityEngine.Object.DestroyImmediate(boardObject);
                UnityEngine.Random.state = previousRandomState;
                foreach (Sprite sprite in sprites)
                {
                    if (sprite == null) continue;
                    Texture2D texture = sprite.texture;
                    UnityEngine.Object.DestroyImmediate(sprite);
                    if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        static void CheckBonusSpecialRollOdds()
        {
            Require(SpecialGemResolver.ResolveBonusRoll(0, false) == GemSpecialType.None
                && SpecialGemResolver.ResolveBonusRoll(69, false) == GemSpecialType.None
                && SpecialGemResolver.ResolveBonusRoll(70, false) == GemSpecialType.Lightning
                && SpecialGemResolver.ResolveBonusRoll(84, false) == GemSpecialType.Lightning
                && SpecialGemResolver.ResolveBonusRoll(85, false) == GemSpecialType.Bagua
                && SpecialGemResolver.ResolveBonusRoll(99, false) == GemSpecialType.Bagua,
                "Match-5+ bonus rolls must use 70% none, 15% Lightning, and 15% Bagua buckets.");
            Require(SpecialGemResolver.ResolveBonusRoll(0, true) == GemSpecialType.None
                && SpecialGemResolver.ResolveBonusRoll(49, true) == GemSpecialType.None
                && SpecialGemResolver.ResolveBonusRoll(50, true) == GemSpecialType.Lightning
                && SpecialGemResolver.ResolveBonusRoll(74, true) == GemSpecialType.Lightning
                && SpecialGemResolver.ResolveBonusRoll(75, true) == GemSpecialType.Bagua
                && SpecialGemResolver.ResolveBonusRoll(99, true) == GemSpecialType.Bagua,
                "T/L bonus rolls must use 50% none, 25% Lightning, and 25% Bagua buckets.");
        }

        static void CheckRainbowSwapClearsColor(SpecialGemResolver resolver)
        {
            BoardSnapshot board = Baseline();
            GemType[,] types = CopyTypes(board);
            GemSpecialType[,] specials = CopySpecials(board);
            types[0, 5] = GemType.Attack;
            specials[0, 4] = GemSpecialType.Rainbow;
            BoardSnapshot rainbowBoard = new BoardSnapshot(types, specials);
            int redCount = CountNormal(rainbowBoard, GemType.Attack);
            MoveResult move = resolver.EvaluateSwap(rainbowBoard, new BoardPos(0, 4), new BoardPos(0, 5));
            Require(move.IsValid && move.InitialMatch.GemCounts.TryGetValue(GemType.Attack, out int cleared) && cleared == redCount,
                "Rainbow swapped with Attack must clear every Attack gem.");
            Require(move.InitialMatch.SpecialActivations.Any(activation =>
                activation.Kind == SpecialActivationKind.RainbowColor
                && activation.TargetGemType == GemType.Attack),
                "Rainbow activation must preserve the swapped target color for its lightning VFX.");

            types[0, 5] = GemType.Rage;
            BoardSnapshot orangeBoard = new BoardSnapshot(types, specials);
            int orangeCount = CountNormal(orangeBoard, GemType.Rage);
            MoveResult orangeMove = resolver.EvaluateSwap(orangeBoard, new BoardPos(0, 4), new BoardPos(0, 5));
            Require(orangeMove.IsValid && orangeMove.InitialMatch.GemCounts.TryGetValue(GemType.Rage, out int clearedRage)
                && clearedRage == orangeCount, "Rainbow swapped with Rage must clear every Rage gem.");
            Require(orangeMove.InitialMatch.SpecialActivations.Any(activation =>
                activation.Kind == SpecialActivationKind.RainbowColor
                && activation.TargetGemType == GemType.Rage),
                "Rainbow activation must preserve the swapped target color when clearing another gem type.");
        }

        static void CheckRainbowRequiresSwap(SpecialGemResolver resolver)
        {
            BoardSnapshot board = SpecialBoard(new BoardPos(3, 3), GemSpecialType.Rainbow);
            MoveResult move = resolver.EvaluateDirectActivation(board, new BoardPos(3, 3));
            Require(!move.IsValid, "Rainbow must activate only through a valid swap.");
        }

        static void CheckTimerAndStoredTurns()
        {
            GameObject timerObject = new GameObject("TurnDebugTimer");
            try
            {
                TurnTimer timer = timerObject.AddComponent<TurnTimer>();
                int timeoutCount = 0;
                timer.TimedOut += () => timeoutCount++;
                timer.StartTimer(10f);
                Require(Mathf.Approximately(timer.RemainingTime, 10f) && timer.IsRunning,
                    "A new Player decision must start with 10 seconds.");
                timer.AdvanceTime(10f);
                timer.Pause();
                timer.AdvanceTime(20f);
                Require(Mathf.Approximately(timer.RemainingTime, 0f) && timeoutCount == 1,
                    "A running decision timer must expire after 10 seconds.");
                timer.StartTimer(10f);
                timer.AdvanceTime(4f);
                timer.Pause();
                timer.AdvanceTime(20f);
                Require(Mathf.Approximately(timer.RemainingTime, 6f) && timeoutCount == 1,
                    "Paused timer time must not elapse during board work, shuffle, or an open menu.");
                timer.Resume();
                timer.AdvanceTime(5.2f);
                Require(Mathf.CeilToInt(timer.RemainingTime) == 1 && timeoutCount == 1,
                    "The timer must display one second while a positive fraction remains.");
                timer.StartTimer(10f);
                Require(Mathf.Approximately(timer.RemainingTime, 10f), "A new action must reset the timer to 10 seconds.");
                timer.AdvanceTime(10f);
                timer.AdvanceTime(1f);
                Require(timeoutCount == 2 && !timer.IsRunning, "Timeout must report once per decision.");
                timer.StartTimer(10f);
                Require(Mathf.Approximately(timer.RemainingTime, 10f), "A new stored turn must receive a fresh timer.");

                Require(TurnManager.ConsumeTurn(2, 0, 5) == 1, "Timeout must consume exactly one Player turn.");
                Require(TurnManager.GetStateAfterPlayerAction(0) == BattleTurnState.BossThinking,
                    "Timeout with no turns remaining must pass control to the Boss.");
                Require(Mathf.Approximately(TurnManager.RestorePlayerTimeAfterShuffle(2f), 5f)
                    && Mathf.Approximately(TurnManager.RestorePlayerTimeAfterShuffle(8f), 8f),
                    "Player shuffle recovery must restore the saved time with a five-second minimum.");
                Require(TurnManager.ConsumeTurn(1, 1, 5) == 1,
                    "A Match-5 Extra Turn must be granted once after consuming the action.");
                Require(TurnManager.ConsumeTurn(5, 1, 5) == 5, "Stored turns must be capped at five.");
                Require(TurnManager.ConsumeTurn(1, 0, 5) == 0, "A normal action must pass control when no turns remain.");
                Require(TurnIndicator.FormatTurnCount(1) == "x1", "The counter must display a single stored turn as x1.");
                Require(TurnIndicator.FormatTurnCount(2) == "x2", "The counter must format multiple turns as x2.");
                Require(TurnIndicator.FormatTime(14.1f) == "15"
                    && TurnIndicator.FormatTime(0f) == "0",
                    "The strip timer must show the ceiling of remaining seconds down to zero.");
                Require(TurnManager.AllowsPlayerInput(BattleTurnState.PlayerTurn)
                    && !TurnManager.AllowsPlayerInput(BattleTurnState.PlayerTimedOut),
                    "Player input must be disabled immediately after timeout.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(timerObject);
            }
        }

        static void CheckTimeoutConsumesTurn()
        {
            GameObject battleObject = new GameObject("TurnDebugTimeoutBattle");
            GameObject turnObject = new GameObject("TurnDebugTimeoutManager");
            try
            {
                BattleController battle = battleObject.AddComponent<BattleController>();
                battle.Configure(null, null, null);
                battle.SetRageForDebug(BattleActor.Player, 63);
                battle.SetRageForDebug(BattleActor.Boss, 42);
                TurnManager turnManager = turnObject.AddComponent<TurnManager>();
                turnManager.Configure(null, battle, null, null);
                turnManager.ConfigureDebugPlayerTurn(2, 10f);
                turnManager.TriggerDebugTimeout();
                Require(turnManager.State == BattleTurnState.PlayerTimedOut && turnManager.PlayerTurnsRemaining == 1,
                    "Timeout must consume one stored Player turn and enter PlayerTimedOut.");
                Require(!turnManager.Timer.IsRunning,
                    "Timeout must stop the decision timer before the Boss counterattack.");
                turnManager.enabled = false;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(battleObject);
                UnityEngine.Object.DestroyImmediate(turnObject);
            }
        }

        static void CheckScannerDoesNotMutate()
        {
            GemType[,] deadTypes = new GemType[DebugBoardRows, DebugBoardColumns];
            GemSpecialType[,] noSpecials = new GemSpecialType[DebugBoardRows, DebugBoardColumns];
            for (int row = 0; row < DebugBoardRows; row++)
            for (int column = 0; column < DebugBoardColumns; column++)
                deadTypes[row, column] = (row + column) % 2 == 0 ? GemType.Attack : GemType.Mana;
            BoardSnapshot deadBoard = new BoardSnapshot(deadTypes, noSpecials);
            BoardMoveScanner scanner = new BoardMoveScanner(new SpecialGemResolver());
            BoardSnapshot before = deadBoard.Clone();
            Require(!scanner.HasAnyLegalMove(deadBoard), "A checkerboard with no specials must be detected as dead.");
            Require(SnapshotsEqual(before, deadBoard), "Move simulation must restore the original logical board.");

            GemSpecialType[,] withRainbow = new GemSpecialType[DebugBoardRows, DebugBoardColumns];
            withRainbow[3, 3] = GemSpecialType.Rainbow;
            BoardSnapshot specialBoard = new BoardSnapshot(deadTypes, withRainbow);
            Require(scanner.HasAnyLegalMove(specialBoard), "A directly activatable special prevents dead-board status.");
        }

        static void CheckInvalidSwapReturnsGems()
        {
            GameObject root = new GameObject("TurnDebugInvalidRoot", typeof(RectTransform));
            GameObject prefabObject = new GameObject("TurnDebugInvalidGem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(GemView));
            GameObject boardObject = new GameObject("TurnDebugInvalidBoard");
            Sprite[] sprites = CreateTestSprites();
            try
            {
                BoardManager board = ConfigureDebugBoard(root, prefabObject, boardObject, sprites);
                GemType[,] types = new GemType[DebugBoardRows, DebugBoardColumns];
                GemSpecialType[,] specials = new GemSpecialType[DebugBoardRows, DebugBoardColumns];
                for (int row = 0; row < DebugBoardRows; row++)
                for (int column = 0; column < DebugBoardColumns; column++)
                    types[row, column] = (row + column) % 2 == 0 ? GemType.Attack : GemType.Mana;
                board.SetBoardForDebug(types, specials, 0f);
                board.SetInputEnabled(false);
                BoardSnapshot before = board.CreateSnapshot();
                MoveResult attempted = board.EvaluateMove(new BoardPos(0, 0), new BoardPos(0, 1));
                Require(!attempted.IsValid, "The deterministic checkerboard swap must be invalid.");
                MoveResult resolved = null;
                Finish(board.ResolveMove(attempted, result => resolved = result));
                Require(resolved != null && !resolved.WasValid && SnapshotsEqual(before, board.CreateSnapshot()),
                    "An invalid Player swap must return both gems to their original cells.");
            }
            finally
            {
                DestroyDebugBoard(root, prefabObject, boardObject, sprites);
            }
        }

        static void CheckShufflePreservesGemData(TurnManager activeTurnManager)
        {
            GameObject root = new GameObject("TurnDebugShuffleRoot", typeof(RectTransform));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(560, 560);
            GridLayoutGroup layout = root.AddComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = DebugBoardColumns;
            layout.cellSize = new Vector2(70, 70);
            GameObject prefabObject = new GameObject("TurnDebugShuffleGem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(GemView));
            GameObject boardObject = new GameObject("TurnDebugShuffleBoard");
            Sprite[] sprites = CreateTestSprites();
            try
            {
                BattleTurnState stateBefore = activeTurnManager.State;
                BattleActor actorBefore = activeTurnManager.CurrentActor;
                int playerTurnsBefore = activeTurnManager.PlayerTurnsRemaining;
                int bossTurnsBefore = activeTurnManager.BossTurnsRemaining;
                float remainingTimeBefore = activeTurnManager.RemainingTime;
                int heroHpBefore = activeTurnManager.Battle.HeroHealth;
                int bossHpBefore = activeTurnManager.Battle.BossHealth;
                int heroManaBefore = activeTurnManager.Battle.HeroMana;
                int heroUltimateBefore = activeTurnManager.Battle.HeroUltimate;
                int bossManaBefore = activeTurnManager.Battle.BossMana;
                int bossUltimateBefore = activeTurnManager.Battle.BossUltimate;
                int heroRageBefore = activeTurnManager.Battle.HeroRage;
                int bossRageBefore = activeTurnManager.Battle.BossRage;

                BoardManager board = ConfigureDebugBoard(root, prefabObject, boardObject, sprites);
                GemType[,] types = BaselineTypes();
                types[0, 0] = GemType.Attack;
                types[0, 1] = GemType.Mana;
                types[0, 2] = GemType.Attack;
                types[0, 3] = GemType.Attack;
                types[1, 1] = GemType.Attack;
                GemSpecialType[,] specials = new GemSpecialType[DebugBoardRows, DebugBoardColumns];
                specials[3, 3] = GemSpecialType.Bagua;
                board.SetBoardForDebug(types, specials);
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform);

                Dictionary<GemType, int> colorCounts = CountColors(board.GetGemsInLayoutOrder());
                Dictionary<GemSpecialType, int> specialCounts = CountSpecials(board.GetGemsInLayoutOrder());
                Dictionary<string, int> gemTypeSpecialPairCounts =
                    CountGemTypeSpecialPairs(board.GetGemsInLayoutOrder());
                bool completed = false;
                IEnumerator shuffle = new BoardShuffleService(100, 0f).Shuffle(
                    board, result => completed = result, allowRegeneration: false);
                Finish(shuffle);
                Require(completed && board.IsValidGemOrder(board.GetGemsInLayoutOrder()),
                    "Shuffle must finish on a board with no immediate match and at least one legal move.");
                Require(DictionaryEquals(colorCounts, CountColors(board.GetGemsInLayoutOrder())),
                    "Shuffle must preserve the count of every gem color.");
                Require(DictionaryEquals(specialCounts, CountSpecials(board.GetGemsInLayoutOrder())),
                    "Bagua shuffle must preserve special gems without activating them.");
                Require(DictionaryEquals(gemTypeSpecialPairCounts,
                        CountGemTypeSpecialPairs(board.GetGemsInLayoutOrder())),
                    "Bagua shuffle must preserve each gem type/special pairing.");
                Require(activeTurnManager.State == stateBefore && activeTurnManager.CurrentActor == actorBefore
                    && activeTurnManager.PlayerTurnsRemaining == playerTurnsBefore
                    && activeTurnManager.BossTurnsRemaining == bossTurnsBefore,
                    "Shuffle must preserve current actor and both stored-turn counts.");
                Require(Mathf.Approximately(activeTurnManager.RemainingTime, remainingTimeBefore),
                    "The shuffle service must not change the active decision timer.");
                Require(activeTurnManager.Battle.HeroHealth == heroHpBefore && activeTurnManager.Battle.BossHealth == bossHpBefore
                    && activeTurnManager.Battle.HeroMana == heroManaBefore && activeTurnManager.Battle.HeroUltimate == heroUltimateBefore
                    && activeTurnManager.Battle.BossMana == bossManaBefore && activeTurnManager.Battle.BossUltimate == bossUltimateBefore
                    && activeTurnManager.Battle.HeroRage == heroRageBefore && activeTurnManager.Battle.BossRage == bossRageBefore,
                    "Shuffle must preserve all battle stats.");
            }
            finally
            {
                DestroyDebugBoard(root, prefabObject, boardObject, sprites);
            }
        }

        static void CheckBossMoveUsesBoardPipeline()
        {
            GameObject root = new GameObject("TurnDebugBoardRoot", typeof(RectTransform));
            GameObject prefabObject = new GameObject("TurnDebugGem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(GemView));
            GameObject boardObject = new GameObject("TurnDebugBoard");
            GameObject aiObject = new GameObject("TurnDebugAI");
            Sprite[] sprites = new Sprite[10];
            try
            {
                for (int index = 0; index < sprites.Length; index++)
                {
                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                    texture.Apply();
                    sprites[index] = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f));
                }

                Image image = prefabObject.GetComponent<Image>();
                prefabObject.GetComponent<Button>().targetGraphic = image;
                BoardManager board = boardObject.AddComponent<BoardManager>();
                board.Configure(root.transform, prefabObject.GetComponent<GemView>(), sprites);
                GemType[,] types = BaselineTypes();
                GemSpecialType[,] specials = new GemSpecialType[DebugBoardRows, DebugBoardColumns];
                types[0, 0] = GemType.Attack;
                types[0, 1] = GemType.Mana;
                types[0, 2] = GemType.Attack;
                types[0, 3] = GemType.Attack;
                types[1, 1] = GemType.Attack;
                board.SetBoardForDebug(types, specials);

                BossAI ai = aiObject.AddComponent<BossAI>();
                board.SetInputEnabled(false);
                Require(!board.CanAcceptPlayerInput, "Board input must be disabled while the Boss acts.");
                List<MoveResult> legalMoves = board.GetLegalMoves();
                List<MoveResult> rankedMoves = legalMoves.OrderByDescending(move => ai.Score(move, .8f)).ToList();
                MoveResult chosen = ai.ChooseMove(rankedMoves, 0f, 1);
                Require(legalMoves.Count > 0 && chosen != null && legalMoves.Any(move => SameMove(move, chosen)),
                    "BossAI must choose one of the BoardManager legal moves.");
                if (rankedMoves.Count > 1)
                {
                    MoveResult alternate = ai.ChooseMove(rankedMoves, 1f, 1);
                    Require(alternate != null && !SameMove(alternate, rankedMoves[0]),
                        "BossAI's alternate difficulty choice must select another legal move.");
                }

                bool completed = false;
                MoveResult resolved = null;
                IEnumerator resolution = board.ResolveMove(chosen, result => { resolved = result; completed = true; });
                while (resolution.MoveNext()) { }
                Require(completed && resolved != null && resolved.ComboCount > 0,
                    "Boss move must execute through BoardManager's shared resolution pipeline.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(prefabObject);
                UnityEngine.Object.DestroyImmediate(boardObject);
                UnityEngine.Object.DestroyImmediate(aiObject);
                foreach (Sprite sprite in sprites)
                {
                    if (sprite == null) continue;
                    Texture2D texture = sprite.texture;
                    UnityEngine.Object.DestroyImmediate(sprite);
                    if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        static void CheckBossDamageAndBattleEnd()
        {
            GameObject battleObject = new GameObject("TurnDebugBattle");
            GameObject victoryObject = new GameObject("TurnDebugVictory");
            try
            {
                BattleController battle = battleObject.AddComponent<BattleController>();
                battle.Configure(null, null, null);
                battle.ConfigureDebugStats(500, 1000);
                MoveResult hitHero = DamageMove(1);
                Finish(battle.ResolveMove(BattleActor.Boss, hitHero));
                Require(battle.HeroHealth == 480, "Boss Red matches must damage Hero HP.");
                Finish(battle.ResolveMove(BattleActor.Boss, DamageMove(49)));
                Require(battle.HeroHealth == 0 && battle.IsGameOver && !battle.PlayerWon,
                    "Battle must stop in Defeat when Hero HP reaches zero.");

                BattleController victory = victoryObject.AddComponent<BattleController>();
                victory.Configure(null, null, null);
                victory.ConfigureDebugStats(500, 1000);
                Finish(victory.ResolveMove(BattleActor.Player, DamageMove(100)));
                Require(victory.BossHealth == 0 && victory.IsGameOver && victory.PlayerWon,
                    "Battle must stop in Victory when Boss HP reaches zero.");

                BattleController resources = new GameObject("TurnDebugResources").AddComponent<BattleController>();
                try
                {
                    resources.Configure(null, null, null);
                    resources.ConfigureDebugStats(500, 1000);
                    Finish(resources.ResolveMove(BattleActor.Player, DamageMove(1)));
                    MoveResult bossResources = new MoveResult(default, default) { IsValid = true };
                    bossResources.GemCounts[GemType.Mana] = 1;
                    bossResources.GemCounts[GemType.Health] = 1;
                    bossResources.GemCounts[GemType.Rage] = 1;
                    Finish(resources.ResolveMove(BattleActor.Boss, bossResources));
                    Require(resources.HeroHealth == 500 && resources.BossHealth == 990
                        && resources.BossMana == 10 && resources.BossRage == 5,
                        "Boss Mana, Health, and Rage effects must apply to the Boss actor.");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(resources.gameObject);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(battleObject);
                UnityEngine.Object.DestroyImmediate(victoryObject);
            }
        }

        static void CheckInvalidMovePenalty()
        {
            GameObject battleObject = new GameObject("TurnDebugPenaltyBattle");
            try
            {
                BattleController battle = battleObject.AddComponent<BattleController>();
                battle.Configure(null, null, null);
                Finish(battle.ApplyInvalidMovePenalty());
                Require(battle.HeroHealth == 470 && battle.InvalidMovePenaltyCount == 1,
                    "One invalid Player swap must apply exactly 30 penalty damage once.");
                Require(battle.BossHealth == 1000, "Invalid-move counterattack must not damage the Boss.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(battleObject);
            }
        }

        static void CheckPlayerInputStates()
        {
            Require(TurnManager.AllowsPlayerInput(BattleTurnState.PlayerTurn), "PlayerTurn must allow player input.");
            Require(!TurnManager.AllowsPlayerInput(BattleTurnState.BattleStart)
                && !TurnManager.AllowsPlayerInput(BattleTurnState.PlayerResolving)
                && !TurnManager.AllowsPlayerInput(BattleTurnState.PlayerInvalidMove)
                && !TurnManager.AllowsPlayerInput(BattleTurnState.PlayerTimedOut)
                && !TurnManager.AllowsPlayerInput(BattleTurnState.BossCounterAttack)
                && !TurnManager.AllowsPlayerInput(BattleTurnState.BossThinking)
                && !TurnManager.AllowsPlayerInput(BattleTurnState.BossTurn)
                && !TurnManager.AllowsPlayerInput(BattleTurnState.BossResolving)
                && !TurnManager.AllowsPlayerInput(BattleTurnState.BoardShuffle)
                && !TurnManager.AllowsPlayerInput(BattleTurnState.Victory)
                && !TurnManager.AllowsPlayerInput(BattleTurnState.Defeat),
                "Player input must be disabled outside PlayerTurn.");
        }

        static void CheckCharacterAnimationPositions()
        {
            GameObject heroRoot = new GameObject("TurnDebugHeroRoot", typeof(RectTransform));
            GameObject bossRoot = new GameObject("TurnDebugBossRoot", typeof(RectTransform));
            GameObject flipRoot = new GameObject("TurnDebugFlipRoot", typeof(RectTransform));
            try
            {
                CharacterView hero = CreateDebugCharacter(heroRoot, "HeroVisual", CharacterFacing.Right,
                    CharacterFacing.Right, out RectTransform heroVisual);
                CharacterView boss = CreateDebugCharacter(bossRoot, "BossVisual", CharacterFacing.Left,
                    CharacterFacing.Left, out RectTransform bossVisual);
                CharacterView flippedSource = CreateDebugCharacter(flipRoot, "FlippedSourceVisual",
                    CharacterFacing.Left, CharacterFacing.Right, out RectTransform flippedVisual);
                RectTransform heroRootRect = (RectTransform)heroRoot.transform;
                RectTransform bossRootRect = (RectTransform)bossRoot.transform;
                heroRootRect.anchoredPosition = new Vector2(-120f, 35f);
                bossRootRect.anchoredPosition = new Vector2(120f, 35f);
                heroVisual.anchoredPosition = new Vector2(13f, -7f);
                bossVisual.anchoredPosition = new Vector2(-9f, 11f);
                heroVisual.localScale = new Vector3(1.25f, .85f, 1f);
                bossVisual.localScale = new Vector3(.75f, 1.1f, 1f);
                flippedVisual.localScale = new Vector3(.6f, .9f, 1f);
                hero.CaptureHomePosition();
                boss.CaptureHomePosition();
                flippedSource.CaptureHomePosition();

                Vector2 heroHome = heroVisual.anchoredPosition;
                Vector2 bossHome = bossVisual.anchoredPosition;
                Vector3 heroScale = new Vector3(1.25f, .85f, 1f);
                Vector3 bossScale = new Vector3(.75f, 1.1f, 1f);
                Vector2 heroRootHome = heroRootRect.anchoredPosition;
                Vector2 bossRootHome = bossRootRect.anchoredPosition;
                Require(hero.FacingDirection == CharacterFacing.Right && heroVisual.localScale == heroScale,
                    "Hero must face right with its original visual scale magnitude.");
                Require(boss.FacingDirection == CharacterFacing.Left && bossVisual.localScale == bossScale,
                    "A naturally left-facing Boss sprite must keep positive visual scale.");
                Require(heroRootRect.localScale.x > 0f && bossRootRect.localScale.x > 0f,
                    "Character roots must never receive negative scale.");
                Require(flippedSource.FacingDirection == CharacterFacing.Left
                    && flippedVisual.localScale == new Vector3(-.6f, .9f, 1f)
                    && flipRoot.transform.localScale.x > 0f,
                    "Only a visual whose source faces the wrong way must be horizontally flipped.");

                EnumeratorStepper heroAttack = new EnumeratorStepper(hero.Attack());
                Require(heroAttack.StepUntilYield() && heroVisual.anchoredPosition.x > heroHome.x,
                    "Hero attack must move right from its visual home position.");
                heroAttack.RunToCompletion();
                Require(heroVisual.anchoredPosition.Equals(heroHome) && heroVisual.localScale == heroScale
                    && hero.FacingDirection == CharacterFacing.Right,
                    "Hero attack must return home without changing its right-facing scale.");

                EnumeratorStepper bossAttack = new EnumeratorStepper(boss.Attack());
                Require(bossAttack.StepUntilYield() && bossVisual.anchoredPosition.x < bossHome.x,
                    "Boss attack must move left from its visual home position.");
                bossAttack.RunToCompletion();
                Require(bossVisual.anchoredPosition.Equals(bossHome) && bossVisual.localScale == bossScale
                    && boss.FacingDirection == CharacterFacing.Left,
                    "Boss attack must return home without changing its left-facing scale.");

                for (int repetition = 0; repetition < 10; repetition++)
                {
                    Finish(hero.Attack());
                    Require(heroVisual.anchoredPosition.Equals(heroHome) && heroVisual.localScale == heroScale,
                        "Repeated Hero attacks must not drift or change facing.");
                    Finish(hero.Hit());
                    Require(heroVisual.anchoredPosition.Equals(heroHome) && heroVisual.localScale == heroScale,
                        "Repeated Hero hits must not drift or change facing.");
                    Finish(boss.Attack());
                    Require(bossVisual.anchoredPosition.Equals(bossHome) && bossVisual.localScale == bossScale,
                        "Repeated Boss attacks must not drift or change facing.");
                    Finish(boss.Hit());
                    Require(bossVisual.anchoredPosition.Equals(bossHome) && bossVisual.localScale == bossScale,
                        "Repeated Boss hits must not drift or change facing.");
                }

                Finish(hero.Skill());
                Finish(boss.Skill());
                Require(heroVisual.anchoredPosition.Equals(heroHome) && heroVisual.localScale == heroScale
                    && bossVisual.anchoredPosition.Equals(bossHome) && bossVisual.localScale == bossScale,
                    "Hero and Boss skills must return home without changing facing.");

                EnumeratorStepper interruptedAttack = new EnumeratorStepper(hero.Attack());
                Require(interruptedAttack.StepUntilYield(), "Hero attack must begin before interruption.");
                Finish(hero.Hit());
                interruptedAttack.RunToCompletion();
                Require(heroVisual.anchoredPosition.Equals(heroHome) && heroVisual.localScale == heroScale,
                    "An interrupted Hero attack must not override the next animation's home position or facing.");

                heroRootRect.anchoredPosition += new Vector2(24f, 18f);
                bossRootRect.anchoredPosition -= new Vector2(24f, 18f);
                Vector2 heroResizedRootPosition = heroRootRect.anchoredPosition;
                Vector2 bossResizedRootPosition = bossRootRect.anchoredPosition;
                hero.CaptureHomePosition();
                boss.CaptureHomePosition();
                Finish(hero.Attack());
                Finish(boss.Attack());
                Require(heroVisual.anchoredPosition.Equals(heroHome) && bossVisual.anchoredPosition.Equals(bossHome),
                    "Responsive layout changes must preserve each visual's local combat home.");
                Require(heroRootRect.anchoredPosition != heroRootHome && bossRootRect.anchoredPosition != bossRootHome,
                    "Layout-owned character roots must remain independently positioned during combat animations.");
                Require(heroRootRect.anchoredPosition.Equals(heroResizedRootPosition)
                    && bossRootRect.anchoredPosition.Equals(bossResizedRootPosition),
                    "Combat animations must not move the layout-owned character roots.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(heroRoot);
                UnityEngine.Object.DestroyImmediate(bossRoot);
                UnityEngine.Object.DestroyImmediate(flipRoot);
            }
        }

        static void CheckHeroScaleTransitions()
        {
            GameObject heroRoot = new GameObject("TurnDebugScaledHeroRoot", typeof(RectTransform));
            try
            {
                CharacterView hero = CreateDebugCharacter(heroRoot, "ScaledHeroVisual",
                    CharacterFacing.Right, CharacterFacing.Right, out RectTransform visual);
                visual.localScale = new Vector3(1.2f, .9f, 1f);
                visual.pivot = new Vector2(.5f, 0f);
                hero.Configure(hero.GetComponentInChildren<Image>(), visual, 12f,
                    CharacterFacing.Right, CharacterFacing.Right, null,
                    HeroPresentationConfig.DefaultIdleNormalizationScaleMultiplier,
                    HeroPresentationConfig.DefaultVisualScaleTransitionDuration);
                hero.CaptureHomePosition();

                float idleNormalization = HeroPresentationConfig.DefaultIdleNormalizationScaleMultiplier;
                Vector3 expectedIdleScale = new Vector3(1.2f * idleNormalization,
                    .9f * idleNormalization, 1f);
                Vector3 expectedAttackScale = new Vector3(1.2f, .9f, 1f);
                Vector2 homePosition = visual.anchoredPosition;
                Vector3 rootScale = heroRoot.transform.localScale;
                Require(Vector3.Distance(visual.localScale, expectedIdleScale) < .001f,
                    "Hero Idle scale must match the measured body-size normalization.");

                for (int repetition = 1; repetition <= 10; repetition++)
                {
                    EnumeratorStepper approach = new EnumeratorStepper(hero.BeginAttackSequence(1, 0f, 0f));
                    Require(approach.StepUntilYield(),
                        "Hero attack scale transition must yield during its approach.");
                    Require(visual.localScale.x > expectedIdleScale.x
                        && visual.localScale.x < expectedAttackScale.x,
                        "Hero attack scale must transition smoothly from Idle toward Attack scale.");
                    approach.RunToCompletion();
                    Require(Vector3.Distance(visual.localScale, expectedAttackScale) < .001f,
                        "Hero Attack scale must remain at its captured reference baseline.");

                    Finish(hero.EndAttackSequence());
                    Require(Vector3.Distance(visual.localScale, expectedIdleScale) < .001f,
                        $"Hero Idle scale drifted after {repetition} attacks.");
                    Require(visual.anchoredPosition.Equals(homePosition)
                        && hero.FacingDirection == CharacterFacing.Right
                        && heroRoot.transform.localScale == rootScale,
                        $"Hero position, facing, or root scale changed after {repetition} attacks.");
                }

                float idleBodyHeight = HeroPresentationConfig.IdleBodyHeightPixels
                    / HeroPresentationConfig.IdleFrameSizePixels * idleNormalization;
                float attackBodyHeight = HeroPresentationConfig.AttackBodyHeightPixels
                    / HeroPresentationConfig.AttackFrameSizePixels;
                float bodyHeightDifference = Mathf.Abs(idleBodyHeight - attackBodyHeight)
                    / attackBodyHeight;
                Require(bodyHeightDifference <= .05f,
                    $"Normalized Idle body height differs from Attack by {bodyHeightDifference:P2}.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(heroRoot);
            }
        }

        static void CheckDeathAnimationPriority()
        {
            GameObject heroRoot = new GameObject("TurnDebugDeadHeroRoot", typeof(RectTransform));
            try
            {
                CharacterView hero = CreateDebugCharacter(heroRoot, "DeadHeroVisual",
                    CharacterFacing.Right, CharacterFacing.Right, out RectTransform visual);
                Finish(hero.Death());
                Finish(hero.PlaySkill());
                Finish(hero.PlaySpecAttack());
                Finish(hero.Attack());
                Finish(hero.Hit());
                Require(hero.State == CharacterView.CharacterState.Death
                    && Mathf.Approximately(visual.GetComponent<Image>().color.a, .35f),
                    "Death must remain terminal and ignore queued Skill, SpecAttack, Attack, and Hit animations.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(heroRoot);
            }
        }

        static CharacterView CreateDebugCharacter(GameObject root, string visualName, CharacterFacing direction,
            CharacterFacing spriteDirection, out RectTransform visualRect)
        {
            GameObject visual = new GameObject(visualName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            visualRect = (RectTransform)visual.transform;
            visualRect.SetParent(root.transform, false);
            visualRect.anchorMin = Vector2.zero;
            visualRect.anchorMax = Vector2.one;
            Image portrait = visual.GetComponent<Image>();
            portrait.color = Color.white;
            CharacterView view = root.AddComponent<CharacterView>();
            view.Configure(portrait, visualRect, 72f, direction, spriteDirection);
            return view;
        }

        static BoardSnapshot LineBoard(int length, bool horizontal, GemSpecialType special)
        {
            GemType[,] types = BaselineTypes();
            GemSpecialType[,] specials = new GemSpecialType[DebugBoardRows, DebugBoardColumns];
            for (int index = 0; index < length; index++)
            {
                BoardPos position = horizontal ? new BoardPos(0, index) : new BoardPos(index, 0);
                types[position.Row, position.Col] = GemType.Attack;
                specials[position.Row, position.Col] = special;
            }
            return new BoardSnapshot(types, specials);
        }

        static BoardSnapshot SpecialBoard(BoardPos position, GemSpecialType special)
        {
            GemSpecialType[,] specials = new GemSpecialType[DebugBoardRows, DebugBoardColumns];
            specials[position.Row, position.Col] = special;
            return new BoardSnapshot(BaselineTypes(), specials);
        }

        static BoardSnapshot Baseline()
        {
            return new BoardSnapshot(BaselineTypes(), new GemSpecialType[DebugBoardRows, DebugBoardColumns]);
        }

        static GemType[,] BaselineTypes()
        {
            GemType[,] types = new GemType[DebugBoardRows, DebugBoardColumns];
            for (int row = 0; row < DebugBoardRows; row++)
            for (int column = 0; column < DebugBoardColumns; column++)
                types[row, column] = (GemType)((row * 2 + column) % 6);
            return types;
        }

        static GemType[,] CopyTypes(BoardSnapshot board)
        {
            GemType[,] copy = new GemType[board.Rows, board.Columns];
            for (int row = 0; row < board.Rows; row++)
            for (int column = 0; column < board.Columns; column++)
                copy[row, column] = board.TypeAt(new BoardPos(row, column));
            return copy;
        }

        static GemSpecialType[,] CopySpecials(BoardSnapshot board)
        {
            GemSpecialType[,] copy = new GemSpecialType[board.Rows, board.Columns];
            for (int row = 0; row < board.Rows; row++)
            for (int column = 0; column < board.Columns; column++)
                copy[row, column] = board.SpecialAt(new BoardPos(row, column));
            return copy;
        }

        static int CountNormal(BoardSnapshot board, GemType type)
        {
            int count = 0;
            for (int row = 0; row < board.Rows; row++)
            for (int column = 0; column < board.Columns; column++)
            {
                BoardPos position = new BoardPos(row, column);
                if (board.SpecialAt(position) == GemSpecialType.None && board.TypeAt(position) == type) count++;
            }
            return count;
        }

        static GemType MostCommonNormal(BoardSnapshot board)
        {
            return new[]
                {
                    GemType.Attack, GemType.Mana, GemType.ExpDrain,
                    GemType.Rage, GemType.Health, GemType.Shield
                }
                .OrderByDescending(type => CountNormal(board, type)).First();
        }

        static bool IsSpawnableGemType(GemType type)
        {
            return type == GemType.Attack || type == GemType.Mana || type == GemType.ExpDrain
                || type == GemType.Rage || type == GemType.Health || type == GemType.Shield;
        }

        static Sprite[] CreateTestSprites(int count = 10)
        {
            string[] names =
            {
                "Base_01_Sword", "Base_02_YinYang", "Base_03_Star", "Base_04_GoldIngot",
                "Base_05_Heart", "Base_06_Fire", "Base_07_Diamond", "Base_08_Spiral",
                "Base_09_PurpleLightning", "Base_10_YinYang_Black"
            };
            Sprite[] sprites = new Sprite[count];
            for (int index = 0; index < sprites.Length; index++)
            {
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                texture.Apply();
                sprites[index] = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f));
                sprites[index].name = names[index];
            }
            return sprites;
        }

        static BoardManager ConfigureDebugBoard(GameObject root, GameObject prefabObject,
            GameObject boardObject, Sprite[] sprites, GemVisualDatabase visualDatabase = null,
            bool enableSoulGem = false)
        {
            Image image = prefabObject.GetComponent<Image>();
            prefabObject.GetComponent<Button>().targetGraphic = image;
            BoardManager board = boardObject.AddComponent<BoardManager>();
            BoardCellView[,] stableCells = CreateDebugCellLookup(root, boardObject);
            board.Configure(root.transform, prefabObject.GetComponent<GemView>(), sprites,
                gemVisualDatabase: visualDatabase, stableBoardCells: stableCells,
                enableSoulGem: enableSoulGem);
            return board;
        }

        static BoardCellView[,] CreateDebugCellLookup(GameObject root, GameObject owner)
        {
            GridLayoutGroup sourceLayout = root.GetComponent<GridLayoutGroup>();
            if (sourceLayout == null)
                return null;

            RectTransform sourceRect = (RectTransform)root.transform;
            GameObject gridObject = new GameObject("DebugStableCellGrid", typeof(RectTransform));
            RectTransform gridRect = (RectTransform)gridObject.transform;
            gridRect.SetParent(owner.transform, false);
            gridRect.anchorMin = gridRect.anchorMax = new Vector2(.5f, .5f);
            gridRect.pivot = sourceRect.pivot;
            gridRect.sizeDelta = sourceRect.rect.size;
            gridRect.localPosition = owner.transform.InverseTransformPoint(sourceRect.position);
            gridRect.localRotation = Quaternion.Inverse(owner.transform.rotation) * sourceRect.rotation;
            gridRect.localScale = sourceRect.lossyScale;

            GridLayoutGroup stableLayout = gridObject.AddComponent<GridLayoutGroup>();
            stableLayout.constraint = sourceLayout.constraint;
            stableLayout.constraintCount = sourceLayout.constraintCount;
            stableLayout.cellSize = sourceLayout.cellSize;
            stableLayout.spacing = sourceLayout.spacing;
            stableLayout.padding = sourceLayout.padding;
            stableLayout.childAlignment = sourceLayout.childAlignment;
            stableLayout.startCorner = sourceLayout.startCorner;
            stableLayout.startAxis = sourceLayout.startAxis;

            BoardCellView[,] cells = new BoardCellView[DebugBoardRows, DebugBoardColumns];
            for (int row = 0; row < DebugBoardRows; row++)
            for (int column = 0; column < DebugBoardColumns; column++)
            {
                GameObject cellObject = new GameObject($"DebugCell_{row}_{column}",
                    typeof(RectTransform), typeof(BoardCellView));
                cellObject.transform.SetParent(gridRect, false);
                BoardCellView cell = cellObject.GetComponent<BoardCellView>();
                cell.Initialize(new BoardPos(row, column));
                cells[row, column] = cell;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
            return cells;
        }

        static void DestroyDebugBoard(GameObject root, GameObject prefabObject, GameObject boardObject, Sprite[] sprites)
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(prefabObject);
            UnityEngine.Object.DestroyImmediate(boardObject);
            foreach (Sprite sprite in sprites)
            {
                if (sprite == null) continue;
                Texture2D texture = sprite.texture;
                UnityEngine.Object.DestroyImmediate(sprite);
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        static Dictionary<GemType, int> CountColors(IReadOnlyList<GemView> gems)
        {
            return gems.GroupBy(gem => gem.Type).ToDictionary(group => group.Key, group => group.Count());
        }

        static Dictionary<GemSpecialType, int> CountSpecials(IReadOnlyList<GemView> gems)
        {
            return gems.GroupBy(gem => gem.SpecialType).ToDictionary(group => group.Key, group => group.Count());
        }

        static Dictionary<string, int> CountGemTypeSpecialPairs(IReadOnlyList<GemView> gems)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (GemView gem in gems)
            {
                string key = $"{gem.Type}:{gem.SpecialType}";
                counts.TryGetValue(key, out int count);
                counts[key] = count + 1;
            }
            return counts;
        }

        static bool DictionaryEquals<TKey>(Dictionary<TKey, int> first, Dictionary<TKey, int> second)
        {
            return first.Count == second.Count && first.All(pair => second.TryGetValue(pair.Key, out int count) && count == pair.Value);
        }

        static bool SnapshotsEqual(BoardSnapshot first, BoardSnapshot second)
        {
            if (first.Rows != second.Rows || first.Columns != second.Columns) return false;
            for (int row = 0; row < first.Rows; row++)
            for (int column = 0; column < first.Columns; column++)
            {
                BoardPos position = new BoardPos(row, column);
                if (first.TypeAt(position) != second.TypeAt(position) || first.SpecialAt(position) != second.SpecialAt(position))
                    return false;
            }
            return true;
        }

        static bool SameMove(MoveResult first, MoveResult second)
        {
            return first.From.Equals(second.From) && first.To.Equals(second.To);
        }

        static MoveResult DamageMove(int redCount)
        {
            MoveResult move = new MoveResult(default, default) { IsValid = true };
            move.GemCounts[GemType.Attack] = redCount;
            return move;
        }

        static MoveResult ResourceMove(GemType type, int count)
        {
            MoveResult move = new MoveResult(default, default) { IsValid = true };
            move.GemCounts[type] = count;
            return move;
        }

        static void Finish(IEnumerator routine)
        {
            Stack<IEnumerator> stack = new Stack<IEnumerator>();
            stack.Push(routine);
            while (stack.Count > 0)
            {
                IEnumerator current = stack.Peek();
                if (!current.MoveNext())
                {
                    stack.Pop();
                    continue;
                }
                if (current.Current is IEnumerator nested) stack.Push(nested);
            }
        }

        sealed class EnumeratorStepper
        {
            readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();

            public EnumeratorStepper(IEnumerator routine)
            {
                stack.Push(routine);
            }

            public bool StepUntilYield()
            {
                while (stack.Count > 0)
                {
                    IEnumerator current = stack.Peek();
                    if (!current.MoveNext())
                    {
                        stack.Pop();
                        continue;
                    }
                    if (current.Current is IEnumerator nested)
                    {
                        stack.Push(nested);
                        continue;
                    }
                    return true;
                }
                return false;
            }

            public void RunToCompletion()
            {
                while (StepUntilYield()) { }
            }
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif