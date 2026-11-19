using PrimeTween;
using UnityEngine;

namespace PrimeTweenDemo {
    public class SequenceExample : MonoBehaviour {
        [SerializeField] int sequenceCycles = 2;
        [SerializeField] TweenSettings tweenSettings = new TweenSettings(0.15f, endDelay: 0.1f);
        [SerializeField] Vector3[] points = {
            new Vector3(1, 0),
            new Vector3(1, 1),
            new Vector3(0, 1),
            new Vector3(0, 0),
        };
        Sequence sequence;

        void Awake() {
            StartSequence();
        }

        void Update() {
            if (BasicExample.GetInputDown()) {
                if (sequence.isAlive) {
                    sequence.Complete();
                } else {
                    StartSequence();
                }
            }
        }

        void StartSequence() {
            sequence = Sequence.Create(sequenceCycles);
            foreach (var point in points) {
                sequence.Chain(Tween.Position(transform, point, tweenSettings));
            }
        }
    }
}
