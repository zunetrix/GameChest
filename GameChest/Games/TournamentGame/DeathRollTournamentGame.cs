using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Text;

using GameChest.Extensions;

namespace GameChest;

public record DeathRollTournamentResult(string Winner, int PlayerCount, DateTime PlayedAt);

public class DeathRollTournamentGame : GameBase {
    public override string Name => "DeathRoll Tournament";
    public override GameMode Mode => GameMode.DeathRollTournament;
    public override DeathRollTournamentState State => _state;
    public override bool IsRegistering => _state.Phase == DeathRollTournamentPhase.Registration;
    public override IReadOnlyList<PhraseCategoryMeta> PhraseCategories => DeathRollTournamentPhraseCategories.All;

    public List<DeathRollTournamentResult> MatchHistory { get; } = new();

    private readonly DeathRollTournamentState _state = new();
    private readonly Random _rng = new();
    private Configuration.DeathRollTournamentConfiguration Cfg => Plugin.Config.DeathRollTournament;
    protected override List<PhrasePool> ConfiguredPhrases => Cfg.Phrases;
    protected override XivChatType OutputChannel => Cfg.OutputChannel;
    public override void SetOutputChannel(XivChatType channel) => Cfg.OutputChannel = channel;

    public void SimulateRoll() {
        if (_state.Phase != DeathRollTournamentPhase.Match) return;
        if (_state.MatchPlayer1 == null || _state.MatchPlayer2 == null) return;
        var chain = _state.MatchChain;
        var outOf = chain.Count == 0 ? _state.MatchStartingRoll : chain[^1].Result;
        if (outOf <= 1) return;
        var lastPlayer = chain.Count > 0 ? chain[^1].PlayerName : _state.MatchPlayer2;
        var player = lastPlayer == _state.MatchPlayer1 ? _state.MatchPlayer2 : _state.MatchPlayer1;
        Plugin.RollManager?.ProcessIncomingRollMessage(player, _rng.Next(1, outOf + 1), outOf);
    }

    internal DeathRollTournamentGame(IPluginContext plugin) : base(plugin) {
        EnsurePhraseDefaults();
        ReloadPhrases();
    }

    public override void Start() {
        // Not used directly - BeginRegistration is the entry point
        BeginRegistration();
    }

    public override void Stop() {
        if (!_state.IsActive) return;
        PublishPhrase(DeathRollTournamentPhraseCategories.TournamentCanceled, new Dictionary<string, string>());
        _state.Reset();
    }

    public override void Reset() {
        base.Reset();
        MatchHistory.Clear();
    }

    public override void ProcessRoll(Roll roll) {
        switch (_state.Phase) {
            case DeathRollTournamentPhase.Registration:
                ProcessRegistrationRoll(roll);
                break;
            case DeathRollTournamentPhase.Match when _state.MatchWinner == null:
                ProcessMatchRoll(roll);
                break;
        }
    }

    public void BeginRegistration() {
        _state.Reset();
        _state.Phase = DeathRollTournamentPhase.Registration;
        PublishPhrase(DeathRollTournamentPhraseCategories.RegistrationOpen, new Dictionary<string, string>());
    }

    public override bool TryJoin(string fullName, JoinSource source) {
        if (_state.Phase != DeathRollTournamentPhase.Registration && _state.Phase != DeathRollTournamentPhase.Preparing)
            return false;
        if (_state.RegisteredPlayers.ContainsPlayer(fullName))
            return false;
        _state.RegisteredPlayers.Add(fullName);
        return true;
    }

    public bool RemovePlayer(string fullName) {
        var idx = _state.RegisteredPlayers.FindIndex(p =>
            string.Equals(p, fullName, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return false;
        _state.RegisteredPlayers.RemoveAt(idx);
        return true;
    }

    public void CloseRegistration() {
        if (_state.RegisteredPlayers.Count < 2) return;
        Shuffle();
        GenerateBrackets();
        _state.Phase = DeathRollTournamentPhase.Preparing;
    }

    private void ProcessRegistrationRoll(Roll roll) {
        // Registration via plain /random (no number → OutOf -1 → effective 999)
        var effective = roll.OutOf == -1 ? 999 : roll.OutOf;
        if (effective != 999) return;
        TryJoin(roll.PlayerName, JoinSource.Roll);
    }

    private void Shuffle() {
        var players = new List<string>(_state.RegisteredPlayers);
        // Pad to next power of 2
        var slots = NextPowerOfTwo(players.Count);
        while (players.Count < slots)
            players.Add(DeathRollTournamentMatch.PlaceholderPlayer);
        // Fisher-Yates
        for (var i = players.Count - 1; i > 0; i--) {
            var j = _rng.Next(i + 1);
            (players[i], players[j]) = (players[j], players[i]);
        }
        // Write back shuffled non-BYE names as the registered order
        _state.RegisteredPlayers.Clear();
        _state.RegisteredPlayers.AddRange(players);
    }

    private void GenerateBrackets() {
        _state.Rounds.Clear();
        _state.CurrentRoundIndex = 0;
        _state.CurrentMatchIndex = 0;

        var players = new List<string>(_state.RegisteredPlayers);
        var slots = players.Count; // already power-of-2 padded by Shuffle

        var round = new DeathRollTournamentRound { RoundNumber = 1 };
        for (var i = 0; i < slots / 2; i++) {
            round.Matches.Add(new DeathRollTournamentMatch {
                Player1 = players[i],
                Player2 = players[slots - 1 - i],
            });
        }
        _state.Rounds.Add(round);

        // Auto-resolve BYE matches in round 1
        foreach (var match in round.Matches.Where(m => m.IsBye)) {
            match.Winner = match.Player1 == DeathRollTournamentMatch.PlaceholderPlayer ? match.Player2 : match.Player1;
        }
    }

    public void StartMatch() {
        var match = _state.CurrentMatch;
        if (match == null) return;

        _state.MatchPlayer1 = match.Player1;
        _state.MatchPlayer2 = match.Player2;
        _state.MatchChain.Clear();
        _state.MatchWinner = null;
        _state.MatchStartingRoll = Cfg.StartingRoll;
        _state.Phase = DeathRollTournamentPhase.Match;

        var vars = new Dictionary<string, string> {
            ["player1"] = PlayerName.Short(_state.MatchPlayer1),
            ["player2"] = PlayerName.Short(_state.MatchPlayer2),
            ["round"] = (_state.CurrentRoundIndex + 1).ToString(),
            ["max"] = Cfg.StartingRoll == 999 ? "" : Cfg.StartingRoll.ToString(),
        };
        PublishPhrase(DeathRollTournamentPhraseCategories.MatchStart, vars);
    }

    public void AdvanceToNextMatch() {
        var match = _state.CurrentMatch;
        if (match == null) return;

        // Record winner in bracket
        match.Winner = _state.MatchWinner;

        // Publish match end phrase
        if (_state.MatchWinner != null) {
            var loser = match.Player1 == _state.MatchWinner ? match.Player2 : match.Player1;
            var vars = new Dictionary<string, string> {
                ["winner"] = PlayerName.Short(_state.MatchWinner),
                ["loser"] = PlayerName.Short(loser),
                ["round"] = (_state.CurrentRoundIndex + 1).ToString(),
            };
            PublishPhrase(DeathRollTournamentPhraseCategories.MatchEnd, vars);
        }

        _state.MatchChain.Clear();
        _state.MatchWinner = null;
        _state.MatchPlayer1 = null;
        _state.MatchPlayer2 = null;

        // Advance index
        _state.CurrentMatchIndex++;
        var currentRound = _state.CurrentRound!;

        if (_state.CurrentMatchIndex >= currentRound.Matches.Count) {
            // Stage exhausted - collect winners and build next round
            var winners = currentRound.Matches
                .Select(m => m.Winner!)
                .Where(w => w != null)
                .ToList();

            if (winners.Count <= 1) {
                // Tournament done
                _state.TournamentWinner = winners.FirstOrDefault();
                _state.Phase = DeathRollTournamentPhase.Done;
                if (_state.TournamentWinner != null) {
                    var vars = new Dictionary<string, string> {
                        ["winner"] = PlayerName.Short(_state.TournamentWinner),
                    };
                    PublishPhrase(DeathRollTournamentPhraseCategories.TournamentEnd, vars);
                    MatchHistory.Insert(0, new DeathRollTournamentResult(PlayerName.Short(_state.TournamentWinner), _state.RegisteredPlayers.Count(p => p != DeathRollTournamentMatch.PlaceholderPlayer), DateTime.Now));
                    if (MatchHistory.Count > 10) MatchHistory.RemoveAt(MatchHistory.Count - 1);
                }
                return;
            }

            // Build next round from winners
            _state.CurrentRoundIndex++;
            _state.CurrentMatchIndex = 0;

            var slots = NextPowerOfTwo(winners.Count);
            while (winners.Count < slots) winners.Add(DeathRollTournamentMatch.PlaceholderPlayer);

            var nextRound = new DeathRollTournamentRound { RoundNumber = _state.CurrentRoundIndex + 1 };
            for (var i = 0; i < slots / 2; i++) {
                nextRound.Matches.Add(new DeathRollTournamentMatch {
                    Player1 = winners[i],
                    Player2 = winners[slots - 1 - i],
                });
            }
            _state.Rounds.Add(nextRound);

            // Auto-resolve BYEs in new round
            foreach (var m in nextRound.Matches.Where(m => m.IsBye))
                m.Winner = m.Player1 == DeathRollTournamentMatch.PlaceholderPlayer ? m.Player2 : m.Player1;
        }

        // Skip auto-resolved BYE matches
        SkipByeMatches();

        _state.Phase = DeathRollTournamentPhase.Preparing;
    }

    public void ForfeitToPlayer(string winner) {
        _state.MatchWinner = winner;
        AdvanceToNextMatch();
    }

    // After bracket is built or stage advances, skip BYE matches automatically
    private void SkipByeMatches() {
        var round = _state.CurrentRound;
        if (round == null) return;
        while (_state.CurrentMatchIndex < round.Matches.Count &&
               round.Matches[_state.CurrentMatchIndex].IsComplete) {
            _state.CurrentMatchIndex++;
        }
        if (_state.CurrentMatchIndex >= round.Matches.Count) {
            // All matches in this round were BYEs - recurse to next stage
            AdvanceToNextMatch();
        }
    }


    private void ProcessMatchRoll(Roll roll) {
        if (_state.MatchPlayer1 == null || _state.MatchPlayer2 == null) return;

        var isP1 = new[] { _state.MatchPlayer1! }.ContainsPlayer(roll.PlayerName);
        var isP2 = new[] { _state.MatchPlayer2! }.ContainsPlayer(roll.PlayerName);
        if (!isP1 && !isP2) return;

        var effective = roll.OutOf == -1 ? 999 : roll.OutOf;
        var chain = _state.MatchChain;

        if (chain.Count == 0) {
            if (effective != _state.MatchStartingRoll) return;
            chain.Add(new DeathRollEntry(roll.PlayerName, roll.Result, effective, roll.At));
            LogRoll(roll);
            if (roll.Result == 1) SetMatchWinner();
            return;
        }

        var last = chain[^1];
        if (roll.PlayerName == last.PlayerName) return;
        if (effective != last.Result) return;

        chain.Add(new DeathRollEntry(roll.PlayerName, roll.Result, effective, roll.At));
        LogRoll(roll);

        if (roll.Result == 1) SetMatchWinner();
    }

    private void SetMatchWinner() {
        var chain = _state.MatchChain;
        _state.MatchWinner = chain.Count >= 2
            ? chain[^2].PlayerName
            : _state.MatchPlayer1;  // shouldn't happen in practice
    }

    private static int NextPowerOfTwo(int n) {
        if (n <= 1) return 1;
        var p = 1;
        while (p < n) p <<= 1;
        return p;
    }

    private void PublishPhrase(string categoryId, Dictionary<string, string> vars) {
        var text = GetPhrase(categoryId, vars);
        if (text != null) Publish(text);
    }
}
