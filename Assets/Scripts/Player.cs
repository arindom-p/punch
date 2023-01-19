using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Animations.Rigging;

public class Player : MonoBehaviour
{
    private static Dictionary<int, int> ParameterDictionary;
    private Dictionary<RigLayer, float> rigDefaultValueDictionary;

    [SerializeField]
    private Transform lifeBarTransform,
        ownHeadTargetPointerTransform,
        headTargetPointTransform;
    [SerializeField] private Rig chestRig;

    private PlayerController playerController;
    private Transform ownTransform, mainCamT;
    private Animator animator;
    private RigBuilder rigBuilder;
    private int lifeRemaining,
        totalLife = 100,
        lastFlag;
    private bool __isRigActive = false,
        movingToEnd = false;
    private float movingDuration = 0.4f,
        distanceBetweenCharacters;
    private Vector3 initPos,
        initRot,
        opponentPos,
        stepToPos,
        rotationTo;

    public bool IsDied => lifeRemaining <= 0;
    private bool isRigActive
    {
        get => __isRigActive;
        set
        {
            if (__isRigActive == value) return;
            __isRigActive = value;
            ApplyRigLayersActivationStatus();
        }
    }

    void Start()
    {
        ownTransform = transform;
        mainCamT = Camera.main.transform;
        animator = GetComponent<Animator>();
        rigBuilder = GetComponent<RigBuilder>();
        playerController = PlayerController.Instance;
        MapRigLayerDefaultValues();
        CreateParameterDictionary();
        SetLife(totalLife);

        initPos = ownTransform.position;
        initRot = ownTransform.eulerAngles;
    }

    private void Update()
    {
        lifeBarTransform.parent.LookAt(mainCamT);
        ownHeadTargetPointerTransform.position = headTargetPointTransform.position;
    }

    private void MapRigLayerDefaultValues()
    {
        rigDefaultValueDictionary = new Dictionary<RigLayer, float>();
        foreach (var layer in rigBuilder.layers)
        {
            rigDefaultValueDictionary[layer] = layer.rig.weight;
        }
    }

    private void CreateParameterDictionary()
    {
        ParameterDictionary = new Dictionary<int, int>();
        var keys = Enum.GetNames(typeof(ActionKeywords));
        var values = Enum.GetValues(typeof(ActionKeywords));
        for (int i = 0; i < keys.Length; i++)
        {
            ParameterDictionary[keys[i].GetHashCode()] = (int)values.GetValue(i);
        }
    }

    private void SetAnimatorFlags(int flags)
    {
        bool flag;
        int len = animator.parameterCount;
        AnimatorControllerParameter param;
        for (int i = 0; i < len; i++)
        {
            param = animator.GetParameter(i);
            flag = (ParameterDictionary[param.GetHashCode()] & flags) > 0;
            if (param.type == AnimatorControllerParameterType.Trigger)
            {
                if (flag) animator.SetTrigger(param.name);
            }
            else if (param.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(param.name, flag);
            }
        }
    }

    private void ApplyRigLayersActivationStatus()
    {
        bool active = isRigActive;
        float duration = 0.5f;
        float startVal = 0;
        float endVal = 0;
        foreach (var layer in rigBuilder.layers)
        {
            if (active) endVal = rigDefaultValueDictionary[layer];
            else startVal = rigDefaultValueDictionary[layer];
            DOTween.To((x) =>
            {
                layer.rig.weight = x;
            }, startVal, endVal, duration);
        }
    }

    public void SetLife(int life)
    {
        ChangeLife(life - lifeRemaining);
    }

    public void ChangeLife(int deltaLife)
    {
        lifeRemaining += deltaLife;
        if (lifeRemaining < 0) lifeRemaining = 0;
        lifeBarTransform.DOScaleX(lifeRemaining / (float)totalLife, 0.2f);
    }

    public void SetOpponentPos(Vector3 opponentPos)
    {
        this.opponentPos = opponentPos;
        distanceBetweenCharacters = Vector3.Distance(initPos, opponentPos);
    }

    public void DidHitOpponent()
    {
        playerController.OnCollisionBetweenPlayer(this);
    }

    public void DoHit(int flags)
    {
        lastFlag = flags;
        SetAnimatorFlags(flags);
    }

    public void GotHit(int flags)
    {
        print(name + "gotHit::toEnd: " + movingToEnd + ", isRigActive: " + isRigActive);
        if (movingToEnd || !isRigActive) return;
        lastFlag = flags;
        isRigActive = false;
        ChangeLife(-18);
        if (lifeRemaining <= 0)
        {
            flags |= (int)ActionKeywords.Died;
            DOTween.Sequence().AppendInterval(2).AppendCallback(() => playerController.OnAPlayerDied(this));
        }
        SetAnimatorFlags(flags);
    }

    public void MatchEnd(bool won)
    {
        isRigActive = false;
        lifeBarTransform.parent.gameObject.SetActive(false);
        if (won)
        {
            animator.SetBool(ActionKeywords.Victory.ToString(), true);
        }
    }

    #region Animation Callbacks

    public void AnimationBackOnFightIdle()
    {
        isRigActive = true;
    }

    /// <summary> delta is negative if it goes back </summary>
    public void DisplaceAsReaction()
    {
        print("DisplaceAsReaction action");
        float deltaRot = 0, deltaPos = 0, snapDuration = 0, duration = 0, intervalDuration = 0;
        int flags = lastFlag;
        Vector3 pos = initPos, rot = initRot;
        Ease ease = Ease.Linear;
        if (PlayerController.GetActionFlagStatus(flags, ActionKeywords.DoHitFromLeft))
        {
            deltaPos = -0.8f;
            deltaRot = 0f;

            duration = 1.5f;
            snapDuration = 0.2f;
            intervalDuration = 0.5f;
            ease = Ease.OutExpo;
        }
        else
        {
            deltaPos = -1.5f;
            deltaRot = 0f;

            duration = 2.5f;
            snapDuration = 0f;
            intervalDuration = 0.5f;
            ease = Ease.OutExpo;
        }
        pos = Vector3.MoveTowards(initPos, opponentPos, deltaPos);
        rot = initRot + deltaRot * Vector3.up;

        DOTween.Sequence().AppendInterval(snapDuration).
            Append(ownTransform.DOMove(pos, duration).SetEase(ease)).
            Join(ownTransform.DORotate(rot, duration)).
            AppendInterval(intervalDuration).
            Append(ownTransform.DOMove(initPos, duration).SetEase(ease)).
            Join(ownTransform.DORotate(initRot, duration));
    }

    public void ForwardStart()
    {
        if (movingToEnd || PlayerController.GetActionFlagStatus(lastFlag, ActionKeywords.GotHit)) return;
        print("forward start");
        movingToEnd = true;
        AdditionalActionOnAnimation(true, lastFlag);
    }

    public void BackEnd()
    {
        print("back end");
        movingToEnd = false;
        AdditionalActionOnAnimation(false, lastFlag);
    }

    #endregion

    private void AdditionalActionOnAnimation(bool toEnd, int flags)
    {
        print("additional action " + toEnd + " " + flags);
        bool changePos = false, changeRot = false;
        float deltaRot = 0, deltaPos = 0, snapDuration = 0;
        Vector3 pos = initPos, rot = initRot;
        Ease ease = Ease.Linear;
        if (!toEnd)
        {
            changePos = changeRot = true;
            pos = initPos;
            rot = initRot;
            snapDuration = 0.15f;
            ease = Ease.OutSine;
        }
        else if (!PlayerController.GetActionFlagStatus(flags, ActionKeywords.Died) &&
            PlayerController.GetActionFlagStatus(flags, ActionKeywords.DoHit))
        {
            if (PlayerController.GetActionFlagStatus(flags, ActionKeywords.UpperCut)) //uppercut
            {
                deltaPos = 0.6f;
                deltaRot = 10f;
            }
            else
            {
                deltaPos = 0.6f;
                deltaRot = 10f;
                if (PlayerController.GetActionFlagStatus(flags, ActionKeywords.DoHitByHand)) //hand
                {
                    if (PlayerController.GetActionFlagStatus(flags, ActionKeywords.DoHitFromLeft)) //left
                    {
                        deltaPos = 0.55f;
                    }
                    else //right
                    {
                        deltaRot = -60;

                        float prevVal = chestRig.weight;
                        float newVal = 1;
                        DOTween.To((x) =>
                        {
                            chestRig.weight = x;
                        }, prevVal, newVal, movingDuration).OnComplete(() =>
                        {
                            DOTween.To((x) =>
                            {
                                chestRig.weight = x;
                            }, newVal, prevVal, movingDuration);
                        });
                    }
                }
                else //leg
                {
                    if (PlayerController.GetActionFlagStatus(flags, ActionKeywords.DoHitFromLeft)) //left
                    {
                        deltaPos = 0.4f;
                        deltaRot = 30f;
                    }
                    else //right
                    {
                        deltaPos = 0.35f;
                        deltaRot = 0f;
                    }
                }
            }
            changePos = changeRot = true;
            pos = Vector3.Lerp(initPos, opponentPos, deltaPos);
            rot = initRot + deltaRot * Vector3.up;
            snapDuration = 0.09f;
            ease = Ease.OutExpo;
        }

        if (changePos)
        {
            if (snapDuration > 0)
            {
                DOTween.Sequence().AppendInterval(snapDuration).Append(ownTransform.DOMove(pos, movingDuration).SetEase(ease));
            }
            else ownTransform.DOMove(pos, movingDuration).SetEase(ease);
        }
        if (changeRot)
        {
            if (snapDuration > 0)
            {
                DOTween.Sequence().AppendInterval(snapDuration).Append(ownTransform.DORotate(rot, movingDuration));
            }
            else ownTransform.DORotate(rot, movingDuration);
        }
    }
}
