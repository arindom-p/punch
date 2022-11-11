using UnityEngine;
using UnityEngine.UI;

public enum ActionKeywords
{
    Died = 1 << 0,
    DoHit = 1 << 1,
    GotHit = 1 << 2,
    DoHitByHand = 1 << 3,
    DoHitFromLeft = 1 << 4,
    PushedBack = 1 << 5,
    FallDown = 1 << 6,
    UpperCut = 1 << 7,
    KnockOut = 1 << 8,
    Victory = 1 << 9,
    ChangeFromIdle = 1 << 10
}

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; } = null;
    
    [SerializeField] private Player player, bot;
    [SerializeField] private Toggle tgl_Hand, tgl_Left, tgl_UpperCut;
    private int currentFlags;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Duplicate PlayerController detected");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void DoHit()
    {
        GetFlags();
        player.SetAnimatorFlags(currentFlags | (int)ActionKeywords.DoHit);
    }

    private void GetFlags()
    {
        currentFlags = 0;
        if (tgl_Hand.isOn) currentFlags |= (int)ActionKeywords.DoHitByHand;
        if (tgl_Left.isOn) currentFlags |= (int)ActionKeywords.DoHitFromLeft;
        if (tgl_UpperCut.isOn) currentFlags |= (int)ActionKeywords.UpperCut;
    }

    public void OnCollisionBetweenPlayer(Player hitBy){
        Player hitTo = hitBy == player ? bot : player;
        hitTo.GotHit(currentFlags | (int)ActionKeywords.GotHit);
    }
}
