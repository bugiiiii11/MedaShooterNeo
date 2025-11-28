using System;
using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;
using ElRaccoone.Tweens;
public class F3DCharacterAvatar : MonoBehaviour
{
    public bool RandomizeAtStart;

    public int CharacterId;
    public SpriteRenderer Head;
    public SpriteRenderer Body, LegL, LegTopL, LegR, LegTopR;
    public SkinName SkinUsed = SkinName.None;
    private sbyte[] DefaultOrder;
    //

    [SerializeField]
    private Transform sideCheck;
    private WeaponController _weaponController;

    public enum SkinName: byte
    {
        None,
        Cryptomeda,
        Tank,
        Basic,
        Zombiechad,
        Sniper,
        Multishooter,
        FlailBoss,

        // minibosses
        MultishooterMiniboss,
        MissilerMiniboss,
        SnipingMiniboss
    }
    
    [Serializable]
    public struct ArmatureSettings
    {
        public Vector3 Position, Rotation, Scale;
        public Sprite sprite;

        internal void ApplyTransform(Transform transform)
        {
            transform.localPosition = Position;
            transform.localEulerAngles = Rotation;
            transform.localScale = Scale;
        }

        internal void SetFrom(Transform transform)
        {
            Position = transform.localPosition;
            Rotation = transform.localEulerAngles;
            Scale = transform.localScale;
        }
    }
    //
    [Serializable]
    public class CharacterArmature
    {
        public SkinName Name;
        public ArmatureSettings Head;
        public ArmatureSettings Body;
        public ArmatureSettings LegLeft, LegLeftTop;
        public ArmatureSettings LegRight, LegRightTop;
        public ArmatureSettings Hand1, Hand2, Hand3, Hand4;
    }

    //
    private void Awake()
    {
        _weaponController = GetComponent<WeaponController>();
        if (RandomizeAtStart) CharacterId = UnityEngine.Random.Range(0, 6);

        SwitchCharacter(SkinManager.Skins[SkinUsed]);
    }
    private void Start()
    {
        // handle orders
        DefaultOrder = new sbyte[9];
        DefaultOrder[0] = (sbyte)Head.sortingOrder;
        DefaultOrder[1] = (sbyte)Body.sortingOrder;
        DefaultOrder[2] = (sbyte)LegL.sortingOrder;
        DefaultOrder[3] = (sbyte)LegR.sortingOrder;
        DefaultOrder[4] = (sbyte)LegTopL.sortingOrder;
        DefaultOrder[5] = (sbyte)LegTopR.sortingOrder;

        // from weapon
        var weap = _weaponController.GetCurrentWeapon();

        DefaultOrder[6] = (sbyte)weap.LeftHand.sortingOrder;
        DefaultOrder[7] = (sbyte)weap.RightHand.sortingOrder;
        DefaultOrder[8] = (sbyte)weap.WeaponRenderer.sortingOrder;

        InvokeRepeating(nameof(UpdateSpriteOrder), 0, 0.1f);

        //if (NamesEasterEgg.instance.IsActive)
        //{
        //    ChangeToEasterEgg();
        //}
    }

    private void ChangeToEasterEgg()
    {
        var text = NamesEasterEgg.instance.Names.Random();
        var tmp = Instantiate(NamesEasterEgg.instance.Prefab, transform);
        tmp.GetComponent<TMPro.TextMeshPro>().text = text;
        tmp.transform.localPosition = new Vector3(0, 0.1803f, 0);
        transform.GetChild(0).Find("root/body").gameObject.SetActive(false);
        transform.GetChild(0).Find("root/head").gameObject.SetActive(false);
    }

    public void TweenAlpha(float duration, float to, float from, float delay)
    {
        var weap = _weaponController.GetCurrentWeapon();

        var col = Color.white;
        col.a = from;
        SetColor(col);

        Head.TweenSpriteRendererAlpha(to, duration).SetFrom(from).SetDelay(delay);
        Body.TweenSpriteRendererAlpha(to, duration).SetFrom(from).SetDelay(delay);
        LegL.TweenSpriteRendererAlpha(to, duration).SetFrom(from).SetDelay(delay);
        LegR.TweenSpriteRendererAlpha(to, duration).SetFrom(from).SetDelay(delay);
        LegTopL.TweenSpriteRendererAlpha(to, duration).SetFrom(from).SetDelay(delay);
        LegTopR.TweenSpriteRendererAlpha(to, duration).SetFrom(from).SetDelay(delay);
        weap.LeftHand.TweenSpriteRendererAlpha(to, duration).SetFrom(from).SetDelay(delay);
        weap.RightHand.TweenSpriteRendererAlpha(to, duration).SetFrom(from).SetDelay(delay);
        weap.WeaponRenderer.TweenSpriteRendererAlpha(to, duration).SetFrom(from).SetDelay(delay);
    }

    private void CancelAllTweens(Weapon weap)
    {
        Head.TweenCancelAll();
        Body.TweenCancelAll();
        LegL.TweenCancelAll();
        LegR.TweenCancelAll();
        LegTopL.TweenCancelAll();
        LegTopR.TweenCancelAll();
        weap.LeftHand.TweenCancelAll();
        weap.RightHand.TweenCancelAll();
        weap.WeaponRenderer.TweenCancelAll();

        SetColor(Color.white);
    }

    public void TweenColor(Color goal, float halfDuration)
    {
        var weap = _weaponController.GetCurrentWeapon();
        var defColor = Head.color;
        SetColor(Color.white);

        Head.TweenSpriteRendererColor(goal, halfDuration).SetOnComplete( () => Head.TweenSpriteRendererColor(defColor, halfDuration));
        Body.TweenSpriteRendererColor(goal, halfDuration).SetOnComplete(() => Body.TweenSpriteRendererColor(defColor, halfDuration));
        LegL.TweenSpriteRendererColor(goal, halfDuration).SetOnComplete(() => LegL.TweenSpriteRendererColor(defColor, halfDuration));
        LegR.TweenSpriteRendererColor(goal, halfDuration).SetOnComplete(() => LegR.TweenSpriteRendererColor(defColor, halfDuration));
        LegTopL.TweenSpriteRendererColor(goal, halfDuration).SetOnComplete(() => LegTopL.TweenSpriteRendererColor(defColor, halfDuration));
        LegTopR.TweenSpriteRendererColor(goal, halfDuration).SetOnComplete(() => LegTopR.TweenSpriteRendererColor(defColor, halfDuration));
        weap.LeftHand.TweenSpriteRendererColor(goal, halfDuration).SetOnComplete(() => weap.LeftHand.TweenSpriteRendererColor(defColor, halfDuration));
        weap.RightHand.TweenSpriteRendererColor(goal, halfDuration).SetOnComplete(() => weap.RightHand.TweenSpriteRendererColor(defColor, halfDuration));
        weap.WeaponRenderer.TweenSpriteRendererColor(goal, halfDuration).SetOnComplete(() => weap.WeaponRenderer.TweenSpriteRendererColor(defColor, halfDuration));
    }

    public void SetColor(Color color)
    {
        var weap = _weaponController.GetCurrentWeapon();
        Head.color = color;
        Body.color = color;
        LegL.color = color;
        LegR.color = color;
        LegTopL.color = color;
        LegTopR.color = color;
        weap.LeftHand.color = color;
        weap.RightHand.color = color;
        weap.WeaponRenderer.color = color;
    }

    private void UpdateSpriteOrder()
    {
        var position = Mathf.RoundToInt(sideCheck.position.y * 100);
        var weap = _weaponController.GetCurrentWeapon();

        Head.sortingOrder = DefaultOrder[0] - position;
        Body.sortingOrder = DefaultOrder[1] - position;
        LegL.sortingOrder = DefaultOrder[2] - position;
        LegR.sortingOrder = DefaultOrder[3] - position;
        LegTopL.sortingOrder = DefaultOrder[4] - position;
        LegTopR.sortingOrder = DefaultOrder[5] - position;
        weap.LeftHand.sortingOrder = DefaultOrder[6] - position;
        weap.RightHand.sortingOrder = DefaultOrder[7] - position;
        weap.WeaponRenderer.sortingOrder = DefaultOrder[8] - position;
    }

    private void SwitchCharacter(CharacterArmature armature)
    {
        if (Head == null) return;
        if (Body == null) return;
        if (armature == null) return;

        Head.sprite = armature.Head.sprite;
        Body.sprite = armature.Body.sprite;
        LegL.sprite = armature.LegLeft.sprite;
        LegTopL.sprite = armature.LegLeftTop.sprite;
        LegR.sprite = armature.LegRight.sprite;
        LegTopR.sprite = armature.LegRightTop.sprite;

        armature.Head.ApplyTransform(Head.transform);
        armature.Body.ApplyTransform(Body.transform);
        armature.LegLeft.ApplyTransform(LegL.transform);
        armature.LegLeftTop.ApplyTransform(LegTopL.transform);
        armature.LegRight.ApplyTransform(LegR.transform);
        armature.LegRightTop.ApplyTransform(LegTopR.transform);

#if UNITY_EDITOR
        GetComponent<WeaponController>().UpdateCharacterHands(armature);
#else
        _weaponController.UpdateCharacterHands(armature);
#endif
    }

    public void SwitchToChar()
    {
        SwitchCharacter(SkinManager.Skins[SkinUsed]);
    }

#if UNITY_EDITOR
    public Sprite Hands3, Hands4, OverrideHand1, OverrideHand2;


    [Button("Assign fields")]
    public void PopulateAvatar()
    {
        UnityEditor.EditorUtility.SetDirty(this);
        Head = transform.Find("Generic_Armature (1)/root/head/head/head_1").GetComponent<SpriteRenderer>();
        Body = transform.Find("Generic_Armature (1)/root/body/body/body").GetComponent<SpriteRenderer>();
        LegL = transform.Find("Generic_Armature (1)/root/leg left top1/leg left bottom1/leg left bottom1/leg left bottom1").GetComponent<SpriteRenderer>();
        LegR = transform.Find("Generic_Armature (1)/root/leg right top2/leg right bottom1/leg right bottom1/leg right bottom1").GetComponent<SpriteRenderer>();

        LegTopL = transform.Find("Generic_Armature (1)/root/leg left top1/leg left top1/leg left top1").GetComponent<SpriteRenderer>();
        LegTopR = transform.Find("Generic_Armature (1)/root/leg right top2/leg right top2/leg right top2").GetComponent<SpriteRenderer>();
    }

    [Button("Switch to skin")]
    public void SwitchChar()
    {
        UnityEditor.EditorUtility.SetDirty(this);

        var skinMan = FindObjectOfType<SkinManager>();
        SwitchCharacter(skinMan[SkinUsed]);
    }

    [Button("Build into manager")]
    public void BuildFromCurrent()
    {

        var skinManagerInstance = FindObjectOfType<SkinManager>();
        CharacterArmature armature;
        UnityEditor.EditorUtility.SetDirty(skinManagerInstance);

        bool exists = false;

        if (skinManagerInstance.Characters.Exists(x => x.Name == SkinUsed))
        {
            armature = skinManagerInstance[SkinUsed];
            exists = true;
        }
        else
        {
            armature = new CharacterArmature
            {
                Head = new ArmatureSettings(),
                Body = new ArmatureSettings(),
                LegLeft = new ArmatureSettings(),
                LegLeftTop = new ArmatureSettings(),
                LegRight = new ArmatureSettings(),
                LegRightTop = new ArmatureSettings(),
                Hand1 = new ArmatureSettings(),
                Hand2 = new ArmatureSettings(),
                Hand3 = new ArmatureSettings(),
                Hand4 = new ArmatureSettings(),
            };
        }

        armature.Head.sprite = Head.sprite;
        armature.Head.SetFrom(Head.transform);

        armature.Body.sprite = Body.sprite;
        armature.Body.SetFrom(Body.transform);

        armature.LegLeft.sprite = LegL.sprite;
        armature.LegLeft.SetFrom(LegL.transform);

        armature.LegLeftTop.sprite = LegTopL.sprite;
        armature.LegLeftTop.SetFrom(LegTopL.transform);

        armature.LegRight.sprite = LegR.sprite;
        armature.LegRight.SetFrom(LegR.transform);

        armature.LegRightTop.sprite = LegTopR.sprite;
        armature.LegRightTop.SetFrom(LegTopR.transform);

        armature.Hand1.sprite = skinManagerInstance.GenericHands1;
        armature.Hand2.sprite = skinManagerInstance.GenericHands2;
        armature.Hand3.sprite = Hands3;
        armature.Hand4.sprite = Hands4;

        if (OverrideHand1)
            armature.Hand1.sprite = OverrideHand1;
        if (OverrideHand2)
            armature.Hand2.sprite = OverrideHand2;


        armature.Name = SkinUsed;

        if (!exists)
            skinManagerInstance.Characters.Add(armature);
    }
#endif
}