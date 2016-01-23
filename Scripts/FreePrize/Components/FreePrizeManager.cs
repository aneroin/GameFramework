﻿//----------------------------------------------
// Flip Web Apps: Game Framework
// Copyright © 2016 Flip Web Apps / Mark Hewitt
//----------------------------------------------

using System;
using FlipWebApps.GameFramework.Scripts.GameObjects.Components;
using FlipWebApps.GameFramework.Scripts.GameStructure;
using FlipWebApps.GameFramework.Scripts.Localisation;
using FlipWebApps.GameFramework.Scripts.UI.Dialogs.Components;
using UnityEngine;

namespace FlipWebApps.GameFramework.Scripts.FreePrize.Components
{
    public class FreePrizeManager : SingletonPersistantSavedState<FreePrizeManager>
    {
        [Header("Time")]
        public MinMax DelayRangeToNextCountdown = new MinMax {Min= 0, Max = 0};       // wait range before starting next countdown. 0 = no wait
        public MinMax TimeRangeToNextPrize = new MinMax { Min = 600, Max = 1800 };    // countdown range to next prize. 10 minutes to 30 minutes

        [Header("Prize")]
        public MinMax ValueRange = new MinMax { Min = 10, Max = 20 };                // value defaults
        public AudioClip PrizeDialogClosedAudioClip;

        [Header("Display")]
        public GameObject ContentPrefab;
        public RuntimeAnimatorController ContentAnimatorController;
        public bool ContentShowsButtons;

        public DateTime NextCountdownStart { get; set; }
        public DateTime NextFreePrizeAvailable { get; set; }
        public int CurrentPrizeAmount {get; set; }

        protected override void GameSetup()
        {
            base.GameSetup();

            StartNewCountdown();

            NextCountdownStart = DateTime.Parse(PlayerPrefs.GetString("FreePrize.NextCountdownStart", DateTime.UtcNow.ToString())); // start countdown immediately if new game
            NextFreePrizeAvailable = DateTime.Parse(PlayerPrefs.GetString("FreePrize.NextPrize", NextFreePrizeAvailable.ToString()));
        }

        public override void SaveState()
        {
            Debug.Log("FreePrizeManager: SaveState");

            PlayerPrefs.SetString("FreePrize.NextCountdownStart", NextCountdownStart.ToString());
            PlayerPrefs.SetString("FreePrize.NextPrize", NextFreePrizeAvailable.ToString());
            PlayerPrefs.Save();
        }

        public void MakePrizeAvailable()
        {
            NextCountdownStart = DateTime.UtcNow;
            NextFreePrizeAvailable = DateTime.UtcNow;

            SaveState();
        }

        public void StartNewCountdown()
        {
            CurrentPrizeAmount = UnityEngine.Random.Range(ValueRange.Min, ValueRange.Max);

            NextCountdownStart = DateTime.UtcNow.AddSeconds(UnityEngine.Random.Range(DelayRangeToNextCountdown.Min, DelayRangeToNextCountdown.Max + 1));

            NextFreePrizeAvailable = NextCountdownStart.AddSeconds(UnityEngine.Random.Range(TimeRangeToNextPrize.Min, TimeRangeToNextPrize.Max + 1));

            SaveState();
        }

        public bool IsCountingDown()
        {
            return GetTimeToPrize().TotalSeconds > 0 && GetTimeToCountdown().TotalSeconds <= 0;
        }

        public bool IsPrizeAvailable()
        {
            return GetTimeToPrize().TotalSeconds <= 0;
        }

        TimeSpan GetTimeToCountdown()
        {
            return NextCountdownStart.Subtract(DateTime.UtcNow);
        }

        public TimeSpan GetTimeToPrize()
        {
            return NextFreePrizeAvailable.Subtract(DateTime.UtcNow);
        }

        /// <summary>
        /// Free prize dialog that giges the user coins. We default to the standard General Message window, adding any additional
        /// content as setup in the FreePrizeManager configuration.
        /// </summary>
        public void ShowFreePrizeDialog()
        {
            var dialogInstance = DialogManager.Instance.Create(null, null, ContentPrefab, null, runtimeAnimatorController: ContentAnimatorController);
            string text = LocaliseText.Format("FreePrize.Text1", CurrentPrizeAmount);

            dialogInstance.Show(title: LocaliseText.Get("FreePrize.Title"), text: text, text2Key: "FreePrize.Text2", doneCallback: ShowFreePrizeDone, dialogButtons: ContentShowsButtons ? DialogInstance.DialogButtonsType.Custom : DialogInstance.DialogButtonsType.Ok);
        }

        public virtual void ShowFreePrizeDone(DialogInstance dialogInstance)
        {
            GameManager.Instance.GetPlayer().Coins += CurrentPrizeAmount;
            if (PrizeDialogClosedAudioClip != null)
                GameManager.Instance.PlayEffect(PrizeDialogClosedAudioClip);
            GameManager.Instance.Player.UpdatePlayerPrefs();

            StartNewCountdown();
        }

        [Serializable]
        public class MinMax
        {
            public int Min;
            public int Max;
            public float Difference { get { return Max - Min; }}
        }
    }
}