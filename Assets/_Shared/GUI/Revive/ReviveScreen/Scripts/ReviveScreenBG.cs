using System;
using System.Collections;
using System.Collections.Generic;
using Amanotes.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Amanotes.MagicTiles3
{
    public class ReviveScreenBG : MonoBehaviour
    {
        [SerializeField] private Image reviveBg;
        [SerializeField] private float reviveBgShowTime;
        private void OnEnable()
        {
            EventBus.Subscribe<ReviveBGEvent>(OnEvent);
            reviveBg.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ReviveBGEvent>(OnEvent);
        }

        private void OnEvent(ReviveBGEvent evt)
        {
            if (evt.isRevive)
            {
                reviveBg.DOFade(0, 0);
                reviveBg.gameObject.SetActive(true);
                reviveBg.DOFade(.5f, reviveBgShowTime);
                return;
            }

            reviveBg.DOFade(0f, reviveBgShowTime)
                .OnComplete(() => reviveBg.gameObject.SetActive(false));
        }
    }

    public struct ReviveBGEvent
    {
        public bool isRevive;
    }
}
