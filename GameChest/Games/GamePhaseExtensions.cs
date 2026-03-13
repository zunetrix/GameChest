using System;

namespace GameChest;

public static class GamePhaseExtensions {
    public static GamePhase ToGamePhase<T>(this T phase) where T : Enum =>
        phase.ToString() switch {
            "Idle" => GamePhase.Idle,
            "Registering" => GamePhase.Registering,
            "Finished" => GamePhase.Finished,
            _ => GamePhase.Active,
        };
}
