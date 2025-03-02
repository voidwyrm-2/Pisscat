using Menu.Remix.MixedUI;
using static Nuktils.Options;

namespace Pisscat;

sealed class Options : OptionInterface
{
    public static Configurable<int> pissTime;

    public Options()
    {
        pissTime = config.Bind("nuclear_pisscat_pisstime", 40, new ConfigAcceptableRange<int>(1, 60 * 60));
    }

    public override void Initialize()
    {
        base.Initialize();

        Tabs = new OpTab[] { new(this) };

        var labelTitle = new OpLabel(20, 600 - 30, "Pisscat Options", true);

        var top = 550;
        ILabeledPair[] labelCheckboxPairs =
        {
            new LabeledIntSliderPair("Piss time", "The length of the piss timer in seconds", pissTime, 480),
        };

        Tabs[0].AddItems(
            labelTitle
        );

        int yOffset = 0;
        for (int i = 0; i < labelCheckboxPairs.Length; i++)
        {
            var res = labelCheckboxPairs[i].Generate(new(30, top - (i * 42) - yOffset));
            yOffset += res.Item2;
            Tabs[0].AddItems(res.Item1);
        }
    }
}
