using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Player : MonoBehaviour
{
    private static Dictionary<string, int> ParameterDictionary = new Dictionary<string, int>();

    [SerializeField]
    private Transform lifeBarTransform,
        ownHeadTargetPointerTransform,
        headTargetPointTransform;

    private Transform mainCamT;
    private Animator animator;
    private int lifeRemaining, totalLife = 100;

    void Start()
    {
        mainCamT = Camera.main.transform;
        animator = GetComponent<Animator>();
        CreateParameterDictionary();
        SetLife(totalLife);
    }

    private void Update()
    {
        lifeBarTransform.parent.LookAt(mainCamT);
        ownHeadTargetPointerTransform.position = headTargetPointTransform.position;
    }

    private void CreateParameterDictionary()
    {
        var keys = Enum.GetNames(typeof(ActionKeywords));
        var values = Enum.GetValues(typeof(ActionKeywords));
        for (int i = 0; i < keys.Length; i++)
        {
            ParameterDictionary[keys[i]] = (int)values.GetValue(i);
        }
    }

    private bool GetFlagStatus(in string flagName, in int flagStatuses)
    {
        return ((ParameterDictionary[flagName] & flagStatuses) > 0);
    }

    public void SetAnimatorFlags(int flags)
    {
        bool flag;
        int len = animator.parameterCount;
        AnimatorControllerParameter param;
        for (int i = 0; i < len; i++)
        {
            param = animator.GetParameter(i);
            flag = GetFlagStatus(param.name, flags);
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

    public void AnimationBackOnFightIdle()
    {
        // print("Animation back to FightIdle");
    }

    public void DidHitOpponent()
    {
        PlayerController.Instance.OnCollisionBetweenPlayer(this);
    }

    public void GotHit(int flags)
    {
        ChangeLife(-40);
        if (lifeRemaining <= 0) flags |= (int)ActionKeywords.Died;
        SetAnimatorFlags(flags);
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
}