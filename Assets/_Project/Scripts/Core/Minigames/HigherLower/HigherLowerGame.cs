using System;
using SITG.Core.Balance;
using SITG.Core.Run;
using SITG.Core.Stakes;

namespace SITG.Core.Minigames.HigherLower
{
    public enum Guess { Higher, Lower }
    public enum GuessResult { Correct, Wrong }

    /// <summary>
    /// Higher or Lower. A card is shown, the player guesses if the next is higher or lower.
    /// A correct guess grows the pot by the odds. Cash out any time after one correct guess.
    /// A wrong guess loses everything. Equal cards are redrawn.
    /// Pure rules: the table in Unity shows it, this decides it.
    /// </summary>
    public sealed class HigherLowerGame : IMinigame
    {
        private const int MaxRedraws = 100;
        private readonly IDeck _deck;
        private readonly GameBalance _balance;
        private int _roundCounter;

        public string TableId { get; }
        public string GameId => "higher_lower";
        public MinigamePhase Phase { get; private set; } = MinigamePhase.Waiting;
        public PlayerKey CurrentPlayer { get; private set; }

        public string CurrentRoundKey { get; private set; }
        public Stake CurrentStake { get; private set; }
        public long StakeValue { get; private set; }
        public int CurrentCard { get; private set; }
        public int LastDrawnCard { get; private set; }
        public long Pot { get; private set; }
        public int Streak { get; private set; }
        public RoundOutcome LastOutcome { get; private set; }

        public event Action<RoundOpened> RoundOpened;
        public event Action<RoundOutcome> RoundResolved;

        public HigherLowerGame(string tableId, IDeck deck, GameBalance balance)
        {
            if (string.IsNullOrEmpty(tableId)) throw new ArgumentException("Table needs an id.", nameof(tableId));
            TableId = tableId;
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
        }

        /// <summary>
        /// Starts a round. The caller has already taken the stake (see RoundSettler) and passes its value.
        /// </summary>
        public bool TryStart(PlayerKey player, Stake stake, long stakeValue)
        {
            if (Phase != MinigamePhase.Waiting || !player.IsValid || stakeValue <= 0) return false;

            _roundCounter++;
            CurrentRoundKey = TableId + "#" + _roundCounter;
            CurrentPlayer = player;
            CurrentStake = stake;
            StakeValue = stakeValue;
            Pot = stakeValue;
            Streak = 0;
            CurrentCard = _deck.Draw();
            LastDrawnCard = CurrentCard;
            Phase = MinigamePhase.Playing;

            RoundOpened?.Invoke(new RoundOpened
            {
                RoundKey = CurrentRoundKey,
                TableId = TableId,
                Player = player,
                Stake = stake,
                StakeValue = stakeValue
            });
            return true;
        }

        /// <summary>Chance of the guess being right, ignoring equal cards (they are redrawn).</summary>
        public double ChanceOf(Guess guess)
        {
            int c = CurrentCard;
            int options = InfiniteDeck.Highest - InfiniteDeck.Lowest; // 12 cards that are not equal
            int good = guess == Guess.Higher ? InfiniteDeck.Highest - c : c - InfiniteDeck.Lowest;
            return (double)good / options;
        }

        /// <summary>How much the pot grows on a correct guess. 0 means the guess is impossible.</summary>
        public double MultiplierFor(Guess guess)
        {
            double chance = ChanceOf(guess);
            return chance <= 0 ? 0 : _balance.HigherLowerHouseEdge / chance;
        }

        public bool CanGuess(Guess guess) => Phase == MinigamePhase.Playing && MultiplierFor(guess) > 0;

        public bool CanCashOut => Phase == MinigamePhase.Playing && Streak > 0;

        public GuessResult MakeGuess(PlayerKey player, Guess guess)
        {
            RequireCurrentPlayer(player);
            if (!CanGuess(guess)) throw new InvalidOperationException($"Cannot guess {guess} on card {CurrentCard}.");

            double multiplier = MultiplierFor(guess);
            int next = DrawDifferentFrom(CurrentCard);
            LastDrawnCard = next;
            bool correct = guess == Guess.Higher ? next > CurrentCard : next < CurrentCard;

            if (!correct)
            {
                Resolve(won: false, payout: 0);
                return GuessResult.Wrong;
            }

            Pot = (long)Math.Floor(Pot * multiplier);
            Streak++;
            CurrentCard = next;
            return GuessResult.Correct;
        }

        public void CashOut(PlayerKey player)
        {
            RequireCurrentPlayer(player);
            if (!CanCashOut) throw new InvalidOperationException("Cash out needs at least one correct guess.");
            Resolve(won: true, payout: Pot);
        }

        /// <summary>
        /// Ends a round without a winner, for example when the night ends or the player disconnects.
        /// Counts as a loss so nobody escapes the house by leaving.
        /// </summary>
        public void Abort()
        {
            if (Phase != MinigamePhase.Playing) return;
            Resolve(won: false, payout: 0);
        }

        private int DrawDifferentFrom(int card)
        {
            for (int i = 0; i < MaxRedraws; i++)
            {
                int next = _deck.Draw();
                if (next != card) return next;
            }
            throw new InvalidOperationException("Deck keeps drawing the same card.");
        }

        private void Resolve(bool won, long payout)
        {
            var outcome = new RoundOutcome
            {
                RoundKey = CurrentRoundKey,
                TableId = TableId,
                Player = CurrentPlayer,
                Stake = CurrentStake,
                StakeValue = StakeValue,
                Won = won,
                Payout = won ? payout : 0
            };
            LastOutcome = outcome;
            Phase = MinigamePhase.Waiting;
            CurrentPlayer = default;
            Pot = 0;
            RoundResolved?.Invoke(outcome);
        }

        private void RequireCurrentPlayer(PlayerKey player)
        {
            if (Phase != MinigamePhase.Playing) throw new InvalidOperationException("No round in progress.");
            if (player != CurrentPlayer) throw new InvalidOperationException($"{player} is not playing at this table.");
        }
    }
}
