using UnityEngine;
using UnityEngine.UI;

public enum ActionKeywords
{
    Died = 1 << 0,
    DoHit = 1 << 1,
    GotHit = 1 << 2,
    DoHitByHand = 1 << 3,
    DoHitFromLeft = 1 << 4,
    UpperCut = 1 << 5,
    KnockOut = 1 << 6,
    Victory = 1 << 7,
}

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; } = null;

    [SerializeField] private Player player, bot;
    [SerializeField] private Toggle tgl_Hand, tgl_Left, tgl_UpperCut;
    private int currentFlags;

    public int CurrentFlags => currentFlags;

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

    private void Start()
    {
        player.SetOpponentPos(bot.transform.position);
        bot.SetOpponentPos(player.transform.position);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void DoHit()
    {
        GetFlags();
        player.DoHit(currentFlags | (int)ActionKeywords.DoHit);
    }

    private void GetFlags()
    {
        currentFlags = 0;
        if (tgl_Hand.isOn) currentFlags |= (int)ActionKeywords.DoHitByHand;
        if (tgl_Left.isOn) currentFlags |= (int)ActionKeywords.DoHitFromLeft;
        if (tgl_UpperCut.isOn) currentFlags |= (int)ActionKeywords.UpperCut;
    }


    public static bool GetActionFlagStatus(in int flagStatuses, in ActionKeywords actionKeyword)
    {
        return (((int)actionKeyword & flagStatuses) > 0);
    }

    public void OnCollisionBetweenPlayer(Player hitBy)
    {
        Player hitTo = hitBy == player ? bot : player;
        hitTo.GotHit(currentFlags | (int)ActionKeywords.GotHit);
    }

    public void OnAPlayerDied(Player diedPlayer){
        Player winPlayer = diedPlayer == player ? bot : player;
        diedPlayer.MatchEnd(false);
        winPlayer.MatchEnd(true);
    }
}
