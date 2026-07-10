using System;

using R3;
using VContainer.Unity;

using SplitRun.Audio;
using SplitRun.Character;
using SplitRun.Item;

namespace SplitRun.Game
{
    public sealed class GameAudioBinder : IStartable, IDisposable
    {
        private readonly GameService _gameService;
        private readonly ItemService _itemService;

        private DisposableBag _disposables;
        private DisposableBag _characterDisposables;

        public GameAudioBinder(GameService gameService, ItemService itemService)
        {
            _gameService = gameService;
            _itemService = itemService;
        }

        public void Start()
        {
            AudioEvents.RequestBgm(BgmType.Game);

            CharacterEvents.OnSpawned   += OnCharacterSpawned;
            CharacterEvents.OnDespawned += OnCharacterDespawned;

            _gameService.Phase
                .Where(phase => phase == GamePhase.GameOver)
                .Subscribe(_ => AudioEvents.RequestSfx(SfxType.GameOver))
                .AddTo(ref _disposables);

            BindItemCues();
        }

        public void Dispose()
        {
            CharacterEvents.OnSpawned   -= OnCharacterSpawned;
            CharacterEvents.OnDespawned -= OnCharacterDespawned;

            _characterDisposables.Dispose();
            _disposables.Dispose();
        }

        // The run-local coin count resets to 0 between runs, so only a rise is a pickup.
        private void BindItemCues()
        {
            _itemService.Coins
                .Pairwise()
                .Where(pair => pair.Current > pair.Previous)
                .Subscribe(_ => AudioEvents.RequestSfx(SfxType.Coin))
                .AddTo(ref _disposables);

            _itemService.MagnetRemaining
                .Pairwise()
                .Where(pair => pair.Previous <= 0f && pair.Current > 0f)
                .Subscribe(_ => AudioEvents.RequestSfx(SfxType.Magnet))
                .AddTo(ref _disposables);
        }

        // Cues read synced NetworkVariable mirrors, never local input, so both devices hear the same run.
        private void OnCharacterSpawned(ICharacter character)
        {
            // A drop to zero is the run's end, covered by the GameOver cue instead.
            character.HpReactive
                .Skip(1)
                .Where(hp => hp > 0)
                .Subscribe(_ => AudioEvents.RequestSfx(SfxType.Hit))
                .AddTo(ref _characterDisposables);

            character.LaneReactive
                .Skip(1)
                .Subscribe(_ => AudioEvents.RequestSfx(SfxType.LaneChange))
                .AddTo(ref _characterDisposables);

            character.VerticalStateReactive
                .Skip(1)
                .Where(state => state != VerticalState.Ground)
                .Subscribe(state => AudioEvents.RequestSfx(SfxForVertical(state)))
                .AddTo(ref _characterDisposables);

            character.SkillStateReactive
                .Skip(1)
                .Where(state => state == SkillState.Active)
                .Subscribe(_ => PlaySkillSfx(character.ActiveSkill))
                .AddTo(ref _characterDisposables);
        }

        private void OnCharacterDespawned(ICharacter character)
        {
            _characterDisposables.Dispose();

            // A disposed bag disposes anything added later — reset it for the next spawn.
            _characterDisposables = new DisposableBag();
        }

        private static SfxType SfxForVertical(VerticalState state)
            => state == VerticalState.Jumping ? SfxType.Jump : SfxType.Slide;

        private static void PlaySkillSfx(SkillType skill)
        {
            switch (skill)
            {
                case SkillType.Shield:
                    AudioEvents.RequestSfx(SfxType.ShieldActivate);
                    break;
                case SkillType.Dash:
                    AudioEvents.RequestSfx(SfxType.DashActivate);
                    break;
            }
        }
    }
}
