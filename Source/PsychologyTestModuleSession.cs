using System.Collections.Generic;
using Microsoft.Xna.Framework;
using static Celeste.Mod.PsychologyTest.PsychologyTestModuleSettings;

namespace Celeste.Mod.PsychologyTest;

public class PsychologyTestModuleSession : EverestModuleSession {
    public int Score =
        PsychologyTestModule.Settings.CurrentTestMode == TestMode.Punishment ? 250_000 : 0;
    
    public HashSet<string> levelsReached = [];
}