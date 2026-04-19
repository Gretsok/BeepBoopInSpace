using UnityEngine;
using System.Collections;

public class AlienAnimationController : MonoBehaviour {

    Animator animator;
    [Range(0, 1)]
    public int stance;
    // Use this for initialization
    void Start () 
    {
        animator = GetComponent<Animator>();
    }

    private void OnValidate () 
    {
        // Si la case est vide, on cherche le composant sur l'objet
        if (animator == null) animator = GetComponent<Animator>();
        
        animator.SetFloat("Stance",stance);

    }
        
    // Update is called once per frame
    void Update () 
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool fire = Input.GetKey("up");

        animator.SetFloat("Forward",v);
        animator.SetFloat("Strafe",h);
        animator.SetBool("Stance", fire);
    }
}