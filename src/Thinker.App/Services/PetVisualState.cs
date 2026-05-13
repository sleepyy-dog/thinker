using Thinker.Models;

namespace Thinker.Services;

public enum PetMood
{
    Sleepy,
    Alert,
    Steady,
    Error
}

public sealed record PetVisualState(PetMood Mood, string BadgeText, string Caption)
{
    public static PetVisualState FromAppState(AppState state)
    {
        if (state.Status == ModeStatus.Error)
        {
            return new PetVisualState(PetMood.Error, "!", "需要处理");
        }

        if (!state.Active)
        {
            return new PetVisualState(PetMood.Sleepy, "Zz", "正常睡眠");
        }

        return state.ActiveMode switch
        {
            RunMode.Permanent => new PetVisualState(PetMood.Steady, "∞", "永久运行"),
            RunMode.Timed2Hours => new PetVisualState(PetMood.Alert, "2h", "合盖继续"),
            _ => new PetVisualState(PetMood.Alert, "30m", "合盖继续")
        };
    }
}
