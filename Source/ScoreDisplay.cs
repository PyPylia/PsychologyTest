using Microsoft.Xna.Framework;
using Monocle;
using static Celeste.Mod.PsychologyTest.PsychologyTestModuleSettings;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Celeste.Mod.PsychologyTest;

public class ScoreDisplay : Entity
{
    private static float scoreWidth;
    private static float numberWidth;
    private int prevScore;
    private float drawLerp;
    private Shaker shaker;
    private MTexture bg = GFX.Gui["strawberryCountBGFlipped"];

    public ScoreDisplay()
    {
        Tag = (int)Tags.HUD | (int)Tags.Global | (int)Tags.PauseUpdate | (int)Tags.TransitionUpdate;
        Depth = -101;
        prevScore = PsychologyTestModule.Session.Score;

        CalculateBaseSizes();
        X = 1930f - scoreWidth - numberWidth * 7;
        Y = 60f;

        Add(shaker = new Shaker(on: false));
    }

    public static void CalculateBaseSizes()
    {
        PixelFont font = Dialog.Languages["english"].Font;
        float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
        PixelFontSize pixelFontSize = font.Get(fontFaceSize);
        for (int i = 0; i < 10; i++)
        {
            float x = pixelFontSize.Measure(i.ToString()).X;
            if (x > numberWidth)
            {
                numberWidth = x;
            }
        }
        scoreWidth = pixelFontSize.Measure("Score: ").X + 20f;
    }

    public override void Update()
    {
        int lerpTarget = PsychologyTestModule.Settings.CurrentTestMode == TestMode.Punishment ? -1 : 1;
        if (drawLerp != 0)
        {
            shaker.ShakeFor(0.5f, removeOnFinish: false);
            if (drawLerp == lerpTarget)
            {
                drawLerp = 0;
            }
            else
            {
                drawLerp = Calc.Approach(drawLerp, lerpTarget, Engine.DeltaTime * 2f);
            }
        }
        if (PsychologyTestModule.Session.Score != prevScore)
        {
            prevScore = PsychologyTestModule.Session.Score;
            drawLerp = Engine.DeltaTime * 4f * lerpTarget;
        }
        base.Update();
    }

    public override void Render()
    {
        bg.Draw(Position);

        float t = 0;
        Color scoreColour = Color.White;
        if (drawLerp < 0)
        {
            t = 4 * drawLerp * (-drawLerp - 1);
            scoreColour = Color.Lerp(scoreColour, Color.Red, t);
        }
        else if (drawLerp > 0)
        {
            t = -4 * drawLerp * (drawLerp - 1);
            scoreColour = Color.Lerp(scoreColour, Color.Lime, t);
        }

        PixelFont font = Dialog.Languages["english"].Font;
        float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
        Vector2 textPosition = new(X + numberWidth * 3, Y + 44f);
        textPosition += shaker.Value * t * 4f;

        font.DrawOutline(
            fontFaceSize,
            "Score: ",
            textPosition,
            new Vector2(0.5f, 1f),
            Vector2.One,
            Color.LightGray,
            2f,
            Color.Black
        );

        font.DrawOutline(
            fontFaceSize,
            PsychologyTestModule.Session.Score.ToString("D6"),
            new Vector2(textPosition.X + scoreWidth, textPosition.Y),
            new Vector2(0.5f, 1f),
            Vector2.One,
            scoreColour,
            2f,
            Color.Black
        );
    }
}