
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class MainMenuBirdAnimation : MonoBehaviour
{
    [Header("Idle")]
    public string idleState = "IdleOneWeapon";
    [Min(1)] public int idleRepeatCount = 2;

    [Header("Random Animations")]
    public string lookState = "LookAroundOneWeapon";
    public string dizzyState = "DizzyOneWeapon";
    public string jumpState = "JumpFull_Default";

    [Header("Transition")]
    [Min(0f)] public float transitionDuration = 0.15f;

    private Animator animator;
    private Coroutine animationRoutine;

    private int idleHash;
    private int[] randomHashes;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        idleHash = Animator.StringToHash(idleState);

        randomHashes = new int[]
        {
            Animator.StringToHash(lookState),
            Animator.StringToHash(dizzyState),
            Animator.StringToHash(jumpState)
        };
    }

    private void OnEnable()
    {
        animationRoutine = StartCoroutine(AnimationLoop());
    }

    private void OnDisable()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }
    }

    private IEnumerator AnimationLoop()
    {
        yield return null;

        if (!animator.HasState(0, idleHash))
        {
            Debug.LogError(
                "[MainMenuBirdAnimation] Không tìm thấy Idle!"
            );
            yield break;
        }

        while (true)
        {
            // 1. Chạy Idle đúng 2 lần
            animator.CrossFadeInFixedTime(
                idleHash, transitionDuration, 0, 0f
            );

            yield return WaitForStateCycles(
                idleHash, idleRepeatCount
            );

            // 2. Random animation
            int randomIndex = Random.Range(
                0, randomHashes.Length
            );

            int selectedHash = randomHashes[randomIndex];

            if (!animator.HasState(0, selectedHash))
            {
                Debug.LogWarning(
                    "[MainMenuBirdAnimation] State không tồn tại!"
                );
                yield return null;
                continue;
            }

            // 3. Chuyển sang animation random
            animator.CrossFadeInFixedTime(
                selectedHash, transitionDuration, 0, 0f
            );

            // 4. Chờ animation random chạy 1 lần
            yield return WaitForStateCycles(selectedHash, 1);
        }
    }

    private IEnumerator WaitForStateCycles(
        int stateHash,
        int cycles
    )
    {
        // Chờ State trở thành State hiện tại
        while (true)
        {
            AnimatorStateInfo info =
                animator.GetCurrentAnimatorStateInfo(0);

            if (!animator.IsInTransition(0) &&
                info.shortNameHash == stateHash)
                break;

            yield return null;
        }

        // Đếm từ đầu State đến đủ số chu kỳ
        while (animator.GetCurrentAnimatorStateInfo(0)
                       .normalizedTime < cycles)
        {
            yield return null;
        }
    }
}
