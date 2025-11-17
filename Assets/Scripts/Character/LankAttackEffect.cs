using UnityEngine;
using Character;

public class LankAttackEffect : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private float _damage;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Enemy")
        {
            collision.GetComponent<CharacterHealth>().ApplyDamage(_damage, Common.DamageType.None);
        }
    }
    private void Update()
    {
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        if(stateInfo.normalizedTime >= 1f && !stateInfo.loop)
        {
            Destroy(gameObject);
            //photonnetwork.destroy();
        }
    }
}
