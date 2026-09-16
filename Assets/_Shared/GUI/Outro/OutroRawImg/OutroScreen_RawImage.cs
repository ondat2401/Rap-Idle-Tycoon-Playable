using System;
using System.Collections.Generic;
using Amanotes.Core;
using DG.Tweening;
using OrientationTracking.Core;
using UnityEngine;

namespace Amanotes.MagicTiles3
{
    public class OutroScreen_RawImage : OutroScreen
    {
        
        private void OnEnable()
        {
            EventBus.Subscribe<OnOrientationChangedEvent>(OnOrientationChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnOrientationChangedEvent>(OnOrientationChanged);
        }

        public override void OnScreenShow()
        {
            base.OnScreenShow();
        }

        public override void OnScreenHide()
        {
            base.OnScreenHide();
        }
        
        private void OnOrientationChanged(OnOrientationChangedEvent evt)
        {
          
        }
    }
}
