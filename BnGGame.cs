using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screen;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;

namespace BreadAndGames
{
    public class BnGGameType : GameType
    {
        protected override void OnInitialize()
        {
            GameTextManager gameTextManager = CurrentGame.GameTextManager;
            this.InitializeGameTexts(gameTextManager);
            IGameStarter gameStarter = new BasicGameStarter();
            this.InitializeGameModels(gameStarter);
            base.GameManager.InitializeGameStarter(CurrentGame, gameStarter);
            base.GameManager.OnGameStart(base.CurrentGame, gameStarter);
            MBObjectManager objectManager = CurrentGame.ObjectManager;
            CurrentGame.SetBasicModels(gameStarter.Models);
            CurrentGame.CreateGameManager();
            base.GameManager.BeginGameStart(base.CurrentGame);
            base.CurrentGame.SetRandomGenerators();
            CurrentGame.InitializeDefaultGameObjects();
            CurrentGame.LoadBasicFiles();
            LoadXMLs();
            objectManager.UnregisterNonReadyObjects();
            CurrentGame.SetDefaultEquipments(new Dictionary<string, Equipment>());
            objectManager.UnregisterNonReadyObjects();
            base.GameManager.OnNewCampaignStart(base.CurrentGame, null);
            base.GameManager.OnAfterCampaignStart(base.CurrentGame);
            base.GameManager.OnGameInitializationFinished(base.CurrentGame);
        }

        private void LoadXMLs()
        {
            ObjectManager.LoadXML("Items", null, false);
            ObjectManager.LoadXML("EquipmentRosters", null, false);
            ObjectManager.LoadXML("NPCCharacters", null, false);
            ObjectManager.LoadXML("SPCultures", null, false);
        }

        public override void OnDestroy()
        {

        }

        public override void OnStateChanged(GameState oldState) { }


        protected override void DoLoadingForGameType(GameTypeLoadingStates gameTypeLoadingState, out GameTypeLoadingStates nextState)
        {
            nextState = GameTypeLoadingStates.None;
            switch (gameTypeLoadingState)
            {
                case GameTypeLoadingStates.InitializeFirstStep:
                    base.CurrentGame.Initialize();
                    nextState = GameTypeLoadingStates.WaitSecondStep;
                    return;
                case GameTypeLoadingStates.WaitSecondStep:
                    nextState = GameTypeLoadingStates.LoadVisualsThirdState;
                    return;
                case GameTypeLoadingStates.LoadVisualsThirdState:
                    nextState = GameTypeLoadingStates.PostInitializeFourthState;
                    break;
                case GameTypeLoadingStates.PostInitializeFourthState:
                    break;
                default:
                    return;
            }
        }
        private void InitializeGameModels(IGameStarter basicGameStarter)
        {
            basicGameStarter.AddModel(new MultiplayerAgentDecideKilledOrUnconsciousModel());
            basicGameStarter.AddModel(new CustomBattleAgentStatCalculateModel());
            basicGameStarter.AddModel(new DefaultMissionDifficultyModel());
            basicGameStarter.AddModel(new CustomBattleApplyWeatherEffectsModel());
            basicGameStarter.AddModel(new MultiplayerAgentApplyDamageModel());
            basicGameStarter.AddModel(new DefaultRidingModel());
            basicGameStarter.AddModel(new DefaultStrikeMagnitudeModel());
            basicGameStarter.AddModel(new CustomBattleMoraleModel());
            basicGameStarter.AddModel(new CustomBattleInitializationModel());
            basicGameStarter.AddModel(new DefaultDamageParticleModel());
        }
        private void InitializeGameTexts(GameTextManager gameTextManager)
        {
            gameTextManager.LoadGameTexts(ModuleHelper.GetModuleFullPath("Native") + "ModuleData/multiplayer_strings.xml");
            gameTextManager.LoadGameTexts(ModuleHelper.GetModuleFullPath("Native") + "ModuleData/global_strings.xml");
            gameTextManager.LoadGameTexts(ModuleHelper.GetModuleFullPath("Native") + "ModuleData/module_strings.xml");
            gameTextManager.LoadGameTexts(ModuleHelper.GetModuleFullPath("Native") + "ModuleData/native_strings.xml");
        }

        protected override void OnRegisterTypes(MBObjectManager objectManager)
        {
            objectManager.RegisterType<BasicCharacterObject>("NPCCharacter", "NPCCharacters", 43U, true, false);
            objectManager.RegisterType<BasicCultureObject>("Culture", "SPCultures", 17U, true, false);
        }
        protected override void BeforeRegisterTypes(MBObjectManager objectManager) { }
    }

    public class BnGGameManager : MBGameManager
    {
        public override void OnLoadFinished()
        {
            base.OnLoadFinished();
            Game.GameStateManager.CleanAndPushState(Game.GameStateManager.CreateState<BnGState>());
        }
        protected override void DoLoadingForGameManager(GameManagerLoadingSteps gameManagerLoadingStep, out GameManagerLoadingSteps nextStep)
        {
            nextStep = GameManagerLoadingSteps.None;
            switch (gameManagerLoadingStep)
            {
                case GameManagerLoadingSteps.PreInitializeZerothStep:
                    MBGameManager.LoadModuleData(false);
                    MBGlobals.InitializeReferences();
                    Game.CreateGame(new BnGGameType(), this).DoLoading();
                    nextStep = GameManagerLoadingSteps.FirstInitializeFirstStep;
                    return;
                case GameManagerLoadingSteps.FirstInitializeFirstStep:
                    {
                        bool flag = true;
                        foreach (MBSubModuleBase mbsubModuleBase in Module.CurrentModule.SubModules)
                        {
                            flag = (flag && mbsubModuleBase.DoLoading(Game.Current));
                        }
                        nextStep = (flag ? GameManagerLoadingSteps.WaitSecondStep : GameManagerLoadingSteps.FirstInitializeFirstStep);
                        return;
                    }
                case GameManagerLoadingSteps.WaitSecondStep:
                    MBGameManager.StartNewGame();
                    nextStep = GameManagerLoadingSteps.SecondInitializeThirdState;
                    return;
                case GameManagerLoadingSteps.SecondInitializeThirdState:
                    nextStep = (Game.Current.DoLoading() ? GameManagerLoadingSteps.PostInitializeFourthState : GameManagerLoadingSteps.SecondInitializeThirdState);
                    return;
                case GameManagerLoadingSteps.PostInitializeFourthState:
                    nextStep = GameManagerLoadingSteps.FinishLoadingFifthStep;
                    return;
                case GameManagerLoadingSteps.FinishLoadingFifthStep:
                    nextStep = GameManagerLoadingSteps.None;
                    return;
                default:
                    return;
            }
        }
    }

    public class BnGState : GameState
    {
    }

    [GameStateScreen(typeof(BnGState))]
    public class BnGScreen : ScreenBase, IGameStateListener
    {
        BnGState _state;

        BnGGameVM _dataSource;
        GauntletLayer _layer;
        IGauntletMovie _movie;

        public BnGScreen(BnGState state)
        {
            _state = state;
        }
        void IGameStateListener.OnActivate() { }

        void IGameStateListener.OnDeactivate() { }

        void IGameStateListener.OnFinalize() { }

        void IGameStateListener.OnInitialize()
        {
            base.OnInitialize();

            _dataSource = new BnGGameVM();
            _layer = new GauntletLayer(1, "GauntletLayer", true);
            _layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _movie = _layer.LoadMovie("BnGScreen", _dataSource);
            AddLayer(_layer);

            LoadingWindow.DisableGlobalLoadingWindow();
        }
    }

    public class BnGGameVM : ViewModel
    {

        void ExecuteDone()
        {
            Game.Current.GameStateManager.PopState(0);
        }
        void ExecuteCancel()
        {
            Game.Current.GameStateManager.PopState(0);
        }
    }
}