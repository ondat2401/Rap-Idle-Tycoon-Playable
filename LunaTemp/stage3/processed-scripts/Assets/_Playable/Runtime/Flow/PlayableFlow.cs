using System;
using System.Collections;
using _Playable.Runtime.Config;
using _Playable.Runtime.Core;
using _Playable.Runtime.View;
using UnityEngine;
using UnityEngine.UI;

namespace _Playable.Runtime.Flow
{
    /// <summary>
    /// Dieu phoi toan bo kich ban playable - 14 buoc mo ta trong Docs/Playable-Clone-Plan.md muc 1.3.
    ///
    /// Ban Cocos goc rai kich ban ra hang chuc callback tren mot event bus; o day viet lai thanh mot
    /// coroutine tuan tu cho de doc va de chinh nhip. Event bus chi con giu nhung tin hieu that su
    /// cat ngang (cham man hinh, tien day, ket thuc) de host cam vao neu can.
    ///
    /// Ten class KHONG dat la PlayableDirector vi trung UnityEngine.Playables.PlayableDirector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayableFlow : MonoBehaviour
    {
        /// <summary>Vi tri cua tung bong bong trong mang <see cref="_bubbles"/>.</summary>
        private const int BubbleGirlTalk = 0;
        private const int BubbleAnger = 1;
        private const int BubbleRapHint = 2;
        private const int BubblePraiseA = 3;
        private const int BubblePraiseB = 4;
        private const int BubbleCount = 5;

        private const int PromptTapToRap = 0;
        private const int PromptChooseGirl = 1;

        [Header("Config")]
        [SerializeField] private PlayableConfig _config;

        [Header("He thong")]
        [SerializeField] private PlayableAudio _audio;
        [SerializeField] private PlayableExit _exit;

        [Header("View")]
        [SerializeField] private PlayableStageView _stage;
        [SerializeField] private PlayableHudView _hud;
        [SerializeField] private PlayableGuideHand _guideHand;
        [SerializeField] private PlayablePraiseView _praise;
        [SerializeField] private PlayableEmojiSpawner _emoji;
        [SerializeField] private PlayableFadeOverlay _fade;

        [Tooltip("Dung 5 phan tu: 0 thoai ban gai, 1 gian du, 2 goi y rap, 3 va 4 loi khen khi rap.")]
        [SerializeField] private PlayableBubbleView[] _bubbles = new PlayableBubbleView[0];

        [Tooltip("Dung 2 phan tu: 0 'TAP TO RAP', 1 'CHOOSE YOUR GIRL'.")]
        [SerializeField] private PlayableFloatingText[] _prompts = new PlayableFloatingText[0];

        [Header("Nhom lua chon")]
        [SerializeField] private PlayableChoiceGroup _girlGroup;
        [SerializeField] private PlayableChoiceGroup _dialogGroup;
        [SerializeField] private PlayableChoiceGroup _carGroup;
        [SerializeField] private PlayableChoiceGroup _houseGroup;
        [SerializeField] private PlayableChoiceGroup _clothesGroup;
        [SerializeField] private PlayableChoiceGroup _pardonGroup;

        [Header("End card")]
        [SerializeField] private GameObject _endCard;

        [Tooltip("Bam bat ky luc nao cung ket thuc playable - giong logo va nut Download ban goc.")]
        [SerializeField] private Button[] _exitButtons = new Button[0];

        private readonly PlayableEventBus _bus = new PlayableEventBus();

        private PlayableWallet _wallet;
        private Coroutine _moneyRoutine;
        private PlayableTiming _timing;
        private float _runStartedAt;

        /// <summary>Cho host dang ky nghe tin hieu cua playable.</summary>
        public PlayableEventBus Bus => this._bus;

        private void Start()
        {
            if (!this.Validate())
            {
                return;
            }

            this.Prepare();
            this.StartCoroutine(this.Run());
        }

        private void OnDestroy()
        {
            foreach (Button button in this._exitButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(this.RequestExit);
                }
            }

            if (this._hud != null)
            {
                this._hud.MoneyFull -= this.HandleMoneyFull;
            }

            this._bus.Clear();
        }

        private bool Validate()
        {
            if (this._config == null)
            {
                Debug.LogError("[Playable] Chua gan PlayableConfig.", this);
                return false;
            }

            if (this._stage == null || this._hud == null)
            {
                Debug.LogError("[Playable] Chua gan Stage hoac Hud.", this);
                return false;
            }

            if (this._bubbles.Length < BubbleCount)
            {
                Debug.LogError($"[Playable] Can du {BubbleCount} bong bong, dang co {this._bubbles.Length}.", this);
                return false;
            }

            if (this._prompts.Length < 2)
            {
                Debug.LogError($"[Playable] Can du 2 dong chu goi y, dang co {this._prompts.Length}.", this);
                return false;
            }

            return true;
        }

        private void Prepare()
        {
            this._timing = this._config.Timing;
            this._wallet = new PlayableWallet(this._config.StartMoney, this._config.MoneyGoal);

            this._bus.Subscribe(PlayableSignal.Touch, this.HandleTouch);
            this._hud.MoneyFull += this.HandleMoneyFull;

            foreach (Button button in this._exitButtons)
            {
                if (button != null)
                {
                    button.onClick.AddListener(this.RequestExit);
                }
            }

            this._stage.ResetToInitial();
            this._stage.Man.Initialize(this._config.StartSkin);

            this._hud.Initialize(this._config.MoneyGoal, this._config.StartMoney);
            this._hud.Show();

            this._guideHand?.Initialize(this._config);
            this._guideHand?.ClearTargets();

            foreach (PlayableBubbleView bubble in this._bubbles)
            {
                bubble?.HideImmediate();
            }

            foreach (PlayableFloatingText prompt in this._prompts)
            {
                prompt?.Hide();
            }

            this.HideGroups();

            if (this._endCard != null)
            {
                this._endCard.SetActive(false);
            }

            this._audio?.PlayBeatLoop();
        }

        private void HideGroups()
        {
            this._girlGroup?.Hide();
            this._dialogGroup?.Hide();
            this._carGroup?.Hide();
            this._houseGroup?.Hide();
            this._clothesGroup?.Hide();
            this._pardonGroup?.Hide();
        }

        private IEnumerator Run()
        {
            this._runStartedAt = Time.realtimeSinceStartup;
            this.Trace("Run started.");

            yield return this.RunStep("ChooseGirl", this.StepChooseGirl());
            yield return this.RunStep("Dialogue", this.StepDialogue());
            yield return this.RunStep("BreakUp", this.StepBreakUp());
            yield return this.RunStep("Rap", this.StepRap());
            yield return this.RunStep("BuyCar", this.StepBuyCar());
            yield return this.RunStep("BuyHouse", this.StepBuyHouse());
            yield return this.RunStep("BuyClothes", this.StepBuyClothes());
            yield return this.RunStep("ComeBack", this.StepComeBack());
            yield return this.RunStep("Pardon", this.StepPardon());

            if (this._endCard != null)
            {
                this._endCard.SetActive(true);
            }

            this.RequestExit();
        }

        // Buoc 1-2: chon ban gai, nang vao cho, an HUD tien.
        private IEnumerator StepChooseGirl()
        {
            this._prompts[PromptChooseGirl].Show();

            this._girlGroup.Configure(this._config.GetGirlPrice);
            yield return this._girlGroup.Show();
            this._guideHand?.SetTargets(this._girlGroup.ActiveTargets());

            yield return this._girlGroup.WaitForChoice(
                index => this._wallet.CanAfford(this._config.GetGirlPrice(index)),
                this.OnTouched,
                _ => this.StartCoroutine(this._hud.PlayNotEnough()));

            int selected = this._girlGroup.SelectedIndex;
            this._guideHand?.ClearTargets();
            this._girlGroup.Hide();
            this._prompts[PromptChooseGirl].Hide();

            this.Spend(this._config.GetGirlPrice(selected));

            PlayableWomanView woman = this._stage.SelectWoman(selected);
            if (woman != null)
            {
                yield return woman.PlayEnter(this._timing.GirlEnterOffsetX, this._timing.GirlEnterDuration);
            }

            // Ban goc an thanh tien ngay sau CHOOSE_WIFE_END.
            this._hud.Hide();
        }

        // Buoc 2-3: bong thoai roi hai lua chon tra loi (ca hai deu dan toi cung ket cuc, giong ban goc).
        private IEnumerator StepDialogue()
        {
            yield return this._bubbles[BubbleGirlTalk].Show(0f, this._timing.LoopHalfDuration);
            yield return PlayableTween.Delay(this._timing.DialogButtonDelay);

            this._dialogGroup.Configure(_ => 0L);
            yield return this._dialogGroup.Show();
            this._guideHand?.SetTargets(this._dialogGroup.ActiveTargets());

            yield return this._dialogGroup.WaitForChoice(null, this.OnTouched, null);

            this._guideHand?.ClearTargets();
            this._dialogGroup.Hide();
        }

        // Buoc 3-5: nu gian bo di, nam khoc roi ve idle, HUD tien hien lai.
        private IEnumerator StepBreakUp()
        {
            this.StartCoroutine(this._bubbles[BubbleGirlTalk].Hide());
            this.StartCoroutine(this.AngerRoutine());

            yield return PlayableTween.Delay(this._timing.CryStartDelay);
            this._audio?.Play(PlayableSfx.ManCrying);
            yield return this._stage.Man.PlaySad(this._timing.CryDuration);

            this._hud.Show();
        }

        private IEnumerator AngerRoutine()
        {
            this.StartCoroutine(this._bubbles[BubbleAnger].Show(this._timing.AngerBubbleDelay,
                this._timing.LoopHalfDuration));

            yield return PlayableTween.Delay(this._timing.AngerStartDelay);
            this._audio?.Play(PlayableSfx.WomanAngry);

            PlayableWomanView woman = this._stage.Woman;
            if (woman != null)
            {
                yield return woman.PlayAngerAndLeave(this._timing.AngerExitOffsetX,
                    this._timing.AngerExitDuration);
            }

            yield return this._bubbles[BubbleAnger].Hide();
        }

        // Buoc 6-8: tap de rap cho den khi tien day.
        private IEnumerator StepRap()
        {
            this.StartCoroutine(this._bubbles[BubbleRapHint].Show(0f, this._timing.LoopHalfDuration));
            this._prompts[PromptTapToRap].Show();

            yield return this._stage.TapTarget.Appear();
            this._guideHand?.SetTargets(this._stage.TapTarget.Rect, true);

            bool first = true;
            while (!this._wallet.IsFull)
            {
                yield return this._stage.TapTarget.WaitForTap();

                this.OnTouched();
                this._audio?.Play(PlayableSfx.Money);
                this._stage.TapTarget.PlayRipple();

                if (first)
                {
                    first = false;
                    this.EnterRapMode();
                }
                else
                {
                    this._emoji?.Spawn(this._config.EmojiRiseDistance, this._config.EmojiSpreadX,
                        this._config.EmojiDuration);
                }

                this.Earn(this._config.TapReward);
            }

            yield return this.ExitRapMode();
        }

        private void EnterRapMode()
        {
            this._stage.TapTarget.StartSpin();
            this._stage.SetMusicFxVisible(true);
            this._stage.Man.StartRap();
            this._audio?.PlayVocalLoop();

            this.StartCoroutine(this._bubbles[BubbleRapHint].Hide());
            this.StartCoroutine(this._bubbles[BubblePraiseA].Show(0f, this._timing.LoopHalfDuration));
            this.StartCoroutine(this._bubbles[BubblePraiseB].Show(0.3f, this._timing.LoopHalfDuration));
        }

        private IEnumerator ExitRapMode()
        {
            this._guideHand?.ClearTargets();
            this._audio?.StopVocal();
            this._stage.SetMusicFxVisible(false);
            this._stage.Man.SetIdle();
            this._prompts[PromptTapToRap].Hide();

            this.StartCoroutine(this._bubbles[BubblePraiseA].Hide());
            this.StartCoroutine(this._bubbles[BubblePraiseB].Hide());

            yield return this._stage.TapTarget.Disappear();

            // Cho tween so tien chay het roi moi mo cac nut mua.
            if (this._moneyRoutine != null)
            {
                yield return this._moneyRoutine;
            }

            yield return PlayableTween.Delay(this._timing.ChoiceArmDelay);
        }

        // Buoc 9: mua xe.
        private IEnumerator StepBuyCar()
        {
            yield return this.RunPurchase(this._carGroup, this._config.PriceCar);

            this._audio?.Play(PlayableSfx.Whoosh);
            this.StartCoroutine(this.DelayThenSfx(this._timing.ShinyDelay, PlayableSfx.Shiny));
            this.StartCoroutine(this.DelayThen(this._timing.PraiseDelay, this._praise?.PlayNice()));
            this.StartCoroutine(this.CelebrateAfter(this._timing.CelebrateDelay));

            yield return this._stage.SwapCar(this._carGroup.SelectedIndex, this._timing);
        }

        // Buoc 10: mua nha (keo theo doi nen).
        private IEnumerator StepBuyHouse()
        {
            yield return this.RunPurchase(this._houseGroup, this._config.PriceHouse);

            this._audio?.Play(PlayableSfx.Whoosh);
            this.StartCoroutine(this.DelayThenSfx(this._timing.ShinyDelay, PlayableSfx.Shiny));
            this.StartCoroutine(this.DelayThen(this._timing.PraiseDelay, this._praise?.PlayNice()));
            this.StartCoroutine(this.CelebrateAfter(this._timing.CelebrateDelay));

            yield return this._stage.SwapRoom(this._houseGroup.SelectedIndex, this._timing);
        }

        // Buoc 11: mua quan ao - doi skin nhan vat.
        private IEnumerator StepBuyClothes()
        {
            yield return this.RunPurchase(this._clothesGroup, this._config.PriceClothes);

            this.StartCoroutine(this.DelayThenSfx(this._timing.ShinyDelay, PlayableSfx.Shiny));
            this.StartCoroutine(this.DelayThen(this._timing.PraiseDelay, this._praise?.PlayAwesome()));
            this.StartCoroutine(this.DelayThenSfx(this._timing.CelebrateDelay, PlayableSfx.ManHappy));
            this.StartCoroutine(this.DelayThen(this._timing.CelebrateDelay,
                this._stage.Man.PlayDressUp(this._config.GetClothesSkin(this._clothesGroup.SelectedIndex))));
        }

        // Buoc 12: nu quay lai.
        private IEnumerator StepComeBack()
        {
            float delayStartedAt = Time.realtimeSinceStartup;
            this.Trace($"ComeBackDelay begin | configured={this._timing.ComeBackDelay:0.###}s");
            yield return PlayableTween.Delay(this._timing.ComeBackDelay);
            this.Trace($"ComeBackDelay end | actual={Time.realtimeSinceStartup - delayStartedAt:0.###}s");

            PlayableWomanView woman = this._stage.Woman;
            if (woman == null)
            {
                this.Trace("ComeBack skipped: no selected woman.");
                yield break;
            }

            float enterStartedAt = Time.realtimeSinceStartup;
            this.Trace($"Woman return begin | moveDuration={this._timing.GirlEnterDuration:0.###}s, " +
                $"offsetX={this._timing.GirlEnterOffsetX:0.###}");
            yield return woman.PlayComeBack(this._timing.GirlEnterOffsetX, this._timing.GirlEnterDuration);
            this.Trace($"Woman return end | actual={Time.realtimeSinceStartup - enterStartedAt:0.###}s");
            this._audio?.Play(PlayableSfx.WomanSatisfied);
        }

        // Buoc 13: tha thu hay khong - ca hai deu ket thuc playable.
        private IEnumerator StepPardon()
        {
            this._pardonGroup.Configure(_ => 0L);
            yield return this._pardonGroup.Show();
            this._guideHand?.SetTargets(this._pardonGroup.ActiveTargets());

            yield return this._pardonGroup.WaitForChoice(null, this.OnTouched, null);

            this._guideHand?.ClearTargets();
            this._pardonGroup.Hide();
        }

        /// <summary>Hien nhom nut, cho chon, tru tien. Caller doc <c>group.SelectedIndex</c> sau do.</summary>
        private IEnumerator RunPurchase(PlayableChoiceGroup group, long price)
        {
            group.Configure(_ => price);
            yield return group.Show();
            this._guideHand?.SetTargets(group.ActiveTargets());

            yield return group.WaitForChoice(
                _ => this._wallet.CanAfford(price),
                this.OnTouched,
                _ => this.StartCoroutine(this._hud.PlayNotEnough()));

            this._guideHand?.ClearTargets();
            group.Hide();

            this.Spend(price);
        }

        private IEnumerator CelebrateAfter(float delay)
        {
            yield return PlayableTween.Delay(delay);
            this._audio?.Play(PlayableSfx.ManHappy);
            yield return this._stage.Man.PlayCelebrate();
        }

        private IEnumerator DelayThen(float delay, IEnumerator routine)
        {
            yield return PlayableTween.Delay(delay);
            if (routine != null)
            {
                yield return routine;
            }
        }

        private IEnumerator DelayThenSfx(float delay, PlayableSfx sfx)
        {
            yield return PlayableTween.Delay(delay);
            this._audio?.Play(sfx);
        }

        private IEnumerator RunStep(string name, IEnumerator routine)
        {
            float startedAt = Time.realtimeSinceStartup;
            this.Trace($"Step begin: {name}");
            yield return routine;
            this.Trace($"Step end: {name} | actual={Time.realtimeSinceStartup - startedAt:0.###}s");
        }

        private void Trace(string message)
        {
            Debug.Log($"[Playable][Flow][t={Time.realtimeSinceStartup - this._runStartedAt:0.###}s] {message}", this);
        }

        private void Earn(long amount)
        {
            long from = this._wallet.Current;
            this._wallet.Add(amount);
            this.StartMoneyTween(from);
        }

        private void Spend(long amount)
        {
            long from = this._wallet.Current;
            if (!this._wallet.TrySpend(amount))
            {
                return;
            }

            this.StartMoneyTween(from);
        }

        private void StartMoneyTween(long from)
        {
            if (this._moneyRoutine != null)
            {
                this.StopCoroutine(this._moneyRoutine);
            }

            this._moneyRoutine = this.StartCoroutine(
                this._hud.AnimateTo(from, this._wallet.Current, this._timing.MoneyTweenDuration));
        }

        private void OnTouched()
        {
            this._bus.Emit(PlayableSignal.Touch);
        }

        private void HandleTouch()
        {
            this._guideHand?.NotifyInteraction();
            this._audio?.Play(PlayableSfx.Click);
        }

        private void HandleMoneyFull()
        {
            this._bus.Emit(PlayableSignal.MoneyFull);
        }

        private void RequestExit()
        {
            this._bus.Emit(PlayableSignal.GameOver);
            this._exit?.RequestExit();
        }
    }
}
