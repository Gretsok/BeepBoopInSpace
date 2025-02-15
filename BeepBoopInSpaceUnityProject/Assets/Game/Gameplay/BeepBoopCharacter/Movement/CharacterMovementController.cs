using Game.ArchitectureTools.ActivatableSystem;
using UnityEngine;

namespace Game.Gameplay.BeepBoopCharacter.Movement
{
     public class CharacterMovementController : AActivatableSystem
     {
          [field: SerializeField]
          public float Acceleration { get; private set; } = 30f;
          [field: SerializeField]
          public float MaximumSpeed { get; private set; } = 8f;
          [field: SerializeField]
          public float DecelerationWhenNotAccelerating { get; private set; } = 60f;
          [field: SerializeField]
          public float JumpSpeed  { get; private set; } = 8f;
          [field: SerializeField]
          public float JumpCooldown { get; private set; } = 0.2f;
          
          public Rigidbody Rigidbody { get; private set; }
          public CharacterGroundDetector GroundDetector { get; private set; }

          public void SetDependencies(Rigidbody associatedRigidbody, CharacterGroundDetector groundDetector)
          {
               Rigidbody = associatedRigidbody;
               GroundDetector = groundDetector;
          }

          public Vector2 MovementInput { get; private set; } = Vector2.zero;
          public void SetMovementInput(Vector2 movementInput)
          {
               MovementInput = movementInput;
          }

          private float m_lastJumpTime = 0f;
          public void Jump()
          {
               if (!GroundDetector.IsGrounded)
                    return;
               
               if (Time.time < m_lastJumpTime + JumpCooldown)
                    return;
               
               Rigidbody.AddForce(JumpSpeed * Vector3.up, ForceMode.VelocityChange);
               m_lastJumpTime = Time.time;
          }
          
          private void FixedUpdate()
          {
               if (!IsActivated)
                    return;

               if (!GroundDetector.IsGrounded) // No air control for the moment
                    return;
               
               var planarVelocity = new Vector3(Rigidbody.linearVelocity.x, 0f, Rigidbody.linearVelocity.z);
               var heightVelocity = new Vector3(0f, Rigidbody.linearVelocity.y, 0f);

               if (MovementInput == Vector2.zero)
               {
                    if (planarVelocity.magnitude > 0.001f)
                    {
                         var newPlanarVelocity = planarVelocity - planarVelocity.normalized * (DecelerationWhenNotAccelerating * Time.deltaTime);
                         // Does decelerating make us go back?
                         if (Vector3.Angle(newPlanarVelocity, planarVelocity) > 90f)
                         {
                              planarVelocity = Vector3.zero;
                         }
                         else
                         {
                              planarVelocity = newPlanarVelocity;
                         }
                    }
               }
               else
               {
                    planarVelocity += (transform.right * MovementInput.x + transform.forward * MovementInput.y) * (Acceleration * Time.deltaTime);
               }

               if (planarVelocity.magnitude > MaximumSpeed)
               {
                    planarVelocity = planarVelocity.normalized * MaximumSpeed;
               }
               
               Rigidbody.linearVelocity = planarVelocity + heightVelocity;
          }
     }
}