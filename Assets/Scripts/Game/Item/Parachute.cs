using Data;
using Game.Buff;
using ShrinkEventBus;
using UnityEngine;

namespace Game.Item
{
    public class Parachute : ItemBase
    {
        // === 新增：可调参数与Prefab引用 ===
        [SerializeField] private GameObject parachutePrefab; // 降落伞预制体
        [Header("降落伞视觉上距离玩家头顶的高度")]
        [SerializeField] private float spawnHeight;     // 降落伞视觉上距离玩家头顶的高度
        [Header("降落伞视觉上左右偏移量")]
        [SerializeField] private float sideOffset;     // 降落伞视觉上左右偏移量

        private int _parachuteUsedIndex;//使用了几次降落伞
        private int _maxParachuteUsedIndex = 3;//最多使用多少

        //前摇/道具使用/后摇时间
        [Header("前摇/道具使用/后摇时间")]
        public float parachuteWindupDuration;
        public float parachuteDuration;
        public float parachuteRecoveryDuration;
        //降落伞buff持续时间
        [Header("降落伞buff持续时间/道具冷却时间")]
        public float parachuteBuffDuration;
        public float parachuteCooldown;
        //降落伞Buff相关
        [Header("降落伞将玩家减速到的最低速度 (正数)")]
        public float parachuteMinSpeed;//降落伞将玩家减速到的最低速度 (正数)
        [Header("降落伞减速能力")]
        public float parachuteFallAcceleration;//降落伞减速能力

        private void InitParachute()
        {
            //滞空开始
            WindupDuration = parachuteWindupDuration;
            Duration = parachuteDuration;
            //滞空结束 -> 进入后摇（生成降落伞）
            RecoveryDuration = parachuteRecoveryDuration;
            //降落伞变到最大
            BuffDuration = parachuteBuffDuration;
            //降落伞消失
            Cooldown = parachuteCooldown;
        }
        
        public Parachute()
        {
            Name = "降落伞";
            Description = "";
            
            IsBuff = true;
        }

        private void Awake()
        {
            InitParachute();
            //ItemSystem.Instance.ItemsPlayerHad.Add(this);//测试
            _parachuteUsedIndex = 0;
            
            if (parachutePrefab == null)
                parachutePrefab = Resources.Load<GameObject>("Prefabs/Items/Parachute");
        }

        public override void Start()
        {
            base.Start();
            
        }

        public override void OnUseStart()
        {
            base.OnUseStart();
            print(Name+"开始使用");
        }

        public override void OnWindupStart()
        {
            base.OnWindupStart();
            SoundManager.Instance.Play("parachute");
        }

        public override void OnUseEnd()
        {
            base.OnUseEnd();

            // 计数：1 -> 2 -> 3（不再增加）
            if (_parachuteUsedIndex < _maxParachuteUsedIndex)
                _parachuteUsedIndex++;
            else
                _parachuteUsedIndex = _maxParachuteUsedIndex;

            if (_parachuteUsedIndex < 2) return;
            var currentCount = DataManager.Instance.GetData<int>("ParachuteFloatCount");
            DataManager.Instance.SetData("ParachuteFloatCount", currentCount + 1, true);
        }

        public override void OnRecoveryStart()
        {
            base.OnRecoveryStart();

            // 根据使用次数生成：1=正上方，2=左上，3=右上
            if (_parachuteUsedIndex >= 1 && parachutePrefab != null)
            {
                SpawnParachuteAtIndex(_parachuteUsedIndex);
            }
            SoundManager.Instance.Play("parachuteuse");
            // 仅打印提示（可保留/删除）
            switch (_parachuteUsedIndex)
            {
                case 1: print("生成1顶伞"); break;
                case 2: print("生成2顶伞"); break;
                case 3: print("生成3顶伞"); break;
            }
        }

        public override void OnRecoveryEnd()
        {
            base.OnRecoveryEnd();
            _parachuteUsedIndex = 0;
        }
        
        public override void ApplyBuffEffect()
        {
            var player = FindAnyObjectByType<Player.Player>();
            EventBus.TriggerEvent(new BuffAppliedEvent
            {
                Buff = new ParachuteBuff(BuffDuration, -parachuteMinSpeed, parachuteFallAcceleration),
                Player = player
            });
        }

        public override void ApplyEffectTick()
        {
            base.ApplyEffectTick();
        }
        
        // 根据降落伞使用次数
        private void SpawnParachuteAtIndex(int usedIndex)
        {
            // 获取玩家位置
            var player = UnityEngine.Object.FindObjectOfType<Player.Player>();
            if (player == null) return;
            var p = player.transform.position;

            // 计算三个候选点位
            var top     = p + Vector3.up * spawnHeight;                  // 正上方
            var topLeft = p + Vector3.up * spawnHeight + Vector3.left * sideOffset; // 左上
            var topRight= p + Vector3.up * spawnHeight + Vector3.right * sideOffset;// 右上

            // 第一次：只生成正上方
            if (usedIndex == 1)
            {
                Instantiate(parachutePrefab, top, Quaternion.identity, player.transform);
            }
            // 第二次：在左上额外生成一个
            else if (usedIndex == 2)
            {
                Instantiate(parachutePrefab, topLeft, Quaternion.identity, player.transform);
            }
            // 第三次：在右上再生成一个
            else // usedIndex >= 3
            {
                Instantiate(parachutePrefab, topRight, Quaternion.identity, player.transform);
            }
        }
    }
}
