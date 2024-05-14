namespace Celeste.Mod.PsychologyTest;

public class PsychologyTestModuleSettings : EverestModuleSettings {
    public enum TestMode {
        Control,
        Reinforcement,
        Punishment,
    }


    [SettingInGame(false)]
    public TestMode CurrentTestMode { get; set; } = TestMode.Control;
}