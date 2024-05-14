using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using static Celeste.Mod.PsychologyTest.PsychologyTestModuleSettings;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Celeste.Mod.PsychologyTest;

public class PsychologyTestModule : EverestModule
{
    public static PsychologyTestModule Instance { get; private set; }

    public override Type SettingsType => typeof(PsychologyTestModuleSettings);
    public static PsychologyTestModuleSettings Settings => (PsychologyTestModuleSettings)Instance._Settings;

    public override Type SessionType => typeof(PsychologyTestModuleSession);
    public static PsychologyTestModuleSession Session => (PsychologyTestModuleSession)Instance._Session;

    public override Type SaveDataType => typeof(PsychologyTestModuleSaveData);
    public static PsychologyTestModuleSaveData SaveData => (PsychologyTestModuleSaveData)Instance._SaveData;

    public PsychologyTestModule()
    {
        Instance = this;
#if DEBUG
        Logger.SetLogLevel("PsychologyTest", LogLevel.Verbose);
#else
        Logger.SetLogLevel("PsychologyTest", LogLevel.Info);
#endif
    }

    private ILHook CS00EndingHook;

    public override void Load()
    {
        On.Celeste.CS00_Granny.OnBegin += SkipGranny;
        On.Celeste.CS01_Ending.OnBegin += SkipCS01Ending;
        On.Celeste.IntroVignette.ctor += SkipIntroVignette;
        On.Celeste.Postcard.DisplayRoutine += SkipPostcard;
        On.Celeste.Strawberry.OnCollect += CollectStrawberry;
        On.Celeste.NPC01_Theo.OnTalk += CancelTheo;

        CS00EndingHook = new ILHook(
            typeof(CS00_Ending).GetMethod("Cutscene", BindingFlags.NonPublic | BindingFlags.Instance).GetStateMachineTarget(),
            ModCS00Ending
        );

        Everest.Events.LevelLoader.OnLoadingThread += AddScoreDisplay;
        Everest.Events.Player.OnDie += DecrementPoints;
        Everest.Events.Level.OnTransitionTo += TransitionLevel;
        Everest.Events.Level.OnExit += LogStats;
    }

    public override void Unload()
    {
        On.Celeste.CS00_Granny.OnBegin -= SkipGranny;
        On.Celeste.CS01_Ending.OnBegin -= SkipCS01Ending;
        On.Celeste.IntroVignette.ctor -= SkipIntroVignette;
        On.Celeste.Postcard.DisplayRoutine -= SkipPostcard;
        On.Celeste.Strawberry.OnCollect -= CollectStrawberry;
        On.Celeste.NPC01_Theo.OnTalk -= CancelTheo;

        CS00EndingHook?.Dispose();

        Everest.Events.LevelLoader.OnLoadingThread -= AddScoreDisplay;
        Everest.Events.Player.OnDie -= DecrementPoints;
        Everest.Events.Level.OnTransitionTo -= TransitionLevel;
        Everest.Events.Level.OnExit -= LogStats;
    }

    private static void AddScoreDisplay(Level level)
    {
        level.Session.FirstLevel = false;
        if (Settings.CurrentTestMode != TestMode.Control && level.Session.Area.ID != 0)
        {
            level.Add(new ScoreDisplay());
        }
    }

    private static void DecrementPoints(Player player)
    {
        if (Settings.CurrentTestMode == TestMode.Punishment)
        {
            Session.Score -= 1000;
        }
    }

    private static void TransitionLevel(Level level, LevelData next, Vector2 direction)
    {
        if (
            Session.levelsReached.Add(next.Name) &&
            Settings.CurrentTestMode == TestMode.Reinforcement
        )
        {
            Audio.Play(SFX.game_gen_strawberry_touch);
            Session.Score += 100;
        }
    }

    private static void LogStats(Level _level, LevelExit _exit, LevelExit.Mode _mode, Session session, HiresSnow _snow)
    {
        string tag = "PsychologyTest/LevelStats";
        Logger.Log(LogLevel.Info, tag, $"Current Test Mode: {Settings.CurrentTestMode}");
        Logger.Log(LogLevel.Info, tag, $"Area ID: {session.Area.ID}");
        Logger.Log(LogLevel.Info, tag, $"Time: {TimeSpan.FromTicks(session.Time).ShortGameplayFormat()}");
        Logger.Log(LogLevel.Info, tag, $"Strawberries: {session.Strawberries.Count}");
        Logger.Log(LogLevel.Info, tag, $"Levels Reached: {Session.levelsReached.Count}");
        Logger.Log(LogLevel.Info, tag, $"Deaths: {session.Deaths}");
    }

    private static void SkipGranny(On.Celeste.CS00_Granny.orig_OnBegin _orig, CS00_Granny _self, Level level)
    {
        level.CancelCutscene();
    }

    private static void SkipCS01Ending(On.Celeste.CS01_Ending.orig_OnBegin _orig, CS01_Ending _self, Level level)
    {
        level.SkipCutscene();
    }

    private static void SkipIntroVignette(On.Celeste.IntroVignette.orig_ctor orig, IntroVignette self, Session session, HiresSnow snow)
    {
        orig(self, session, snow);
        self.timer = 18.683f;
    }

    private static IEnumerator SkipPostcard(On.Celeste.Postcard.orig_DisplayRoutine _orig, Postcard _self)
    {
        yield return null;
    }

    private static void CollectStrawberry(On.Celeste.Strawberry.orig_OnCollect orig, Strawberry self)
    {
        orig(self);
        if (Settings.CurrentTestMode == TestMode.Reinforcement)
        {
            Session.Score += 1000;
        }
    }

    private static void CancelTheo(On.Celeste.NPC01_Theo.orig_OnTalk _orig, NPC01_Theo _self, Player _player) {}

    private static void ModCS00Ending(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(MoveType.Before, instr => instr.MatchLdstr("event:/music/lvl0/title_ping"));
        cursor.GotoPrev(MoveType.Before, instr => instr.MatchLdcR4(2));
        cursor.Index -= 1;

        cursor.FindNext(out ILCursor[] endCutscenes, instr => instr.MatchCallvirt(typeof(CutsceneEntity).GetMethod("EndCutscene")));
        if (endCutscenes.Length == 1)
        {
            ILCursor endCutsceneInst = endCutscenes.First();
            endCutsceneInst.GotoPrev(MoveType.Before, instr => instr.MatchLdloc1());

            while (endCutsceneInst.Next != null)
            {
                cursor.Emit(endCutsceneInst.Next.OpCode, endCutsceneInst.Next.Operand);
                endCutsceneInst.Index += 1;
            }
        }
    }
}