using MixedReality.Toolkit;
using MixedReality.Toolkit.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using ZenFulcrum.EmbeddedBrowser;

namespace Assets.Code
{
    [RequireComponent(typeof(MRTKBaseInteractable))]
    public class BrowserVRInput : PointerUIMesh
    {
        MRTKBaseInteractable baseInteractable;
        public override void Awake()
        {
            baseInteractable = GetComponent<MRTKBaseInteractable>();
            base.Awake();
        }
        //TODO: Replace VRBrowserHand with MRTKBaseInteractable backed solution for input

        protected override void FeedVRPointers()
        {
            //if (vrHands == null)
            //{
            //    vrHands = FindObjectsOfType<VRBrowserHand>();
            //    if (vrHands.Length == 0 && XRSettings.enabled)
            //    {
            //        Debug.LogWarning("VR input is enabled, but no VRBrowserHands were found in the scene", this);
            //    }
            //}

            if (!baseInteractable.isHovered) return;
            Debug.Log(baseInteractable.interactorsHovering.Count);
            for (int i = 0; i < baseInteractable.interactorsHovering.Count; i++)
            {
                if (baseInteractable.interactorsHovering[i] is not MRTKRayInteractor r) continue;
                Debug.Log(r.rayEndPoint);
                FeedPointerState(new PointerState
                {
                    id = 100 + i,
                    is2D = true,
                    position2D = (Vector2)Camera.main.WorldToScreenPoint(r.rayEndPoint, Camera.MonoOrStereoscopicEye.Mono),
                    activeButtons = baseInteractable.isSelected ? MouseButton.Left : 0,
                    scrollDelta = Vector2.zero,
                }); ;
            }
        }
    }
}
