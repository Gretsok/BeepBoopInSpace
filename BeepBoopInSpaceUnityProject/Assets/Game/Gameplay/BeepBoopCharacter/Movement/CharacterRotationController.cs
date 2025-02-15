using Game.ArchitectureTools.ActivatableSystem;
using UnityEngine;

namespace Game.Gameplay.BeepBoopCharacter.Movement
{
    public class CharacterRotationController : AActivatableSystem
    {
        [field: SerializeField]
        public Vector2 RotationRange { get; set; } = new Vector2(85f, 85f);
        [field: SerializeField]
        public Vector2 RotationSensivities { get; private set; } = new Vector2(3f, 3f);
        public Transform Root { get; private set; }
        public Camera Camera { get; private set; }

        public void SetDependencies(Transform root, Camera associatedCamera)
        {
            Root = root;
            Camera = associatedCamera;
        }

        public Vector2 LookAroundInput { get; private set; }
        public void SetLookAroundInput(Vector2 lookAroundInput)
        {
            LookAroundInput = lookAroundInput;
        }

        private void FixedUpdate()
        {
            if (!IsActivated)
                return;

            var lastEulerAngles = Camera.transform.localEulerAngles;
            
            Root.Rotate(Vector3.up, LookAroundInput.x * RotationSensivities.x * Time.deltaTime, Space.Self);
            Camera.transform.Rotate(Vector3.right, -LookAroundInput.y * RotationSensivities.y * Time.deltaTime, Space.Self);

            if (Vector3.Angle(Camera.transform.forward, Root.transform.forward) > 90f)
            {
                Camera.transform.localEulerAngles = lastEulerAngles;
            }
            
            if (Camera.transform.localEulerAngles.x > 180f)
            {
                if (Camera.transform.localEulerAngles.x < 360f - RotationRange.y)
                {
                    var eulerAngle = Camera.transform.localEulerAngles;
                    eulerAngle.x = 360f - RotationRange.y;
                    eulerAngle.y = 0f;
                    eulerAngle.z = 0f;
                    Camera.transform.localEulerAngles = eulerAngle;
                }
            }
            else
            {
                if (Camera.transform.localEulerAngles.x > RotationRange.x)
                {
                    var eulerAngle = Camera.transform.localEulerAngles;
                    eulerAngle.x = RotationRange.x;
                    eulerAngle.y = 0f;
                    eulerAngle.z = 0f;
                    Camera.transform.localEulerAngles = eulerAngle; 
                }
            }
        }
    }
}