using System;
using System.Collections;
using Game.Buff;
using Game.Player;
using ShrinkEventBus;
using UnityEngine;

namespace Game.Item
{
    public class Parachute : ItemBase
    {
        // === 新增：可调参数与Prefab引用 ===
        [SerializeField] private GameObject parachutePrefab;   // 降落伞预制体
        [SerializeField] private float spawnHeight = 7f;     // 距离玩家头顶的高度
        [SerializeField] private float sideOffset  = 0.8f;     // 左右偏移量

        private int _parachuteUsedIndex;//使用了几次降落伞
        private int _maxParachuteUsedIndex = 3;//最多使用多少次(建议3)
        private bool _parachuteHoverEnd;

        public Parachute()
        {
            Name = "降落伞";
            Description = "";
            //滞空开始
            WindupDuration = 0.2f;
            Duration = 0f;
            //滞空结束 -> 进入后摇（生成降落伞）
            RecoveryDuration = 2f;
            //降落伞变到最大
            BuffDuration = 5f;
            //降落伞消失
            Cooldown = 10f;
            IsBuff = true;
            IsHoverStart = true;
            IsHoverEnd = false;
            _parachuteHoverEnd = true;
        }

        private void Awake()
        {
            ItemSystem.Instance.ItemsPlayerHad.Add(this);//测试
            _parachuteUsedIndex = 0;

            // 如果没在Inspector赋值，兜底用Resources（按需修改路径）
            if (parachutePrefab == null)
                parachutePrefab = Resources.Load<GameObject>("Prefabs/Items/Parachute");
        }

        public override void Start()
        {
            base.Start();
            Sprite = Resources.Load<Sprite>("Sprites/Items/Parachute");
        }

        public override void OnUseStart()
        {
            base.OnUseStart();
            print(Name+"开始使用");
        }

        public override void OnUseEnd()
        {
            base.OnUseEnd();
            StopHover(_parachuteHoverEnd);

            // 计数：1 -> 2 -> 3（不再增加）
            if (_parachuteUsedIndex < _maxParachuteUsedIndex)
                ++_parachuteUsedIndex;
            else
                _parachuteUsedIndex = _maxParachuteUsedIndex;
        }

        public override void OnRecoveryStart()
        {
            base.OnRecoveryStart();

            // 根据使用次数生成：1=正上方，2=左上，3=右上
            if (_parachuteUsedIndex >= 1 && parachutePrefab != null)
            {
                SpawnParachuteAtIndex(_parachuteUsedIndex);
            }

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
                Buff = new ParachuteBuff(BuffDuration, -3f, 140f),
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
            Vector3 top     = p + Vector3.up * spawnHeight;                  // 正上方
            Vector3 topLeft = p + Vector3.up * spawnHeight + Vector3.left * sideOffset; // 左上
            Vector3 topRight= p + Vector3.up * spawnHeight + Vector3.right * sideOffset;// 右上

            // 第一次：只生成正上方
            if (usedIndex == 1)
            {
                Instantiate(parachutePrefab, top, Quaternion.identity, player.transform);
            }
            // 第二次：在左上额外生成一个（若你也想保留第一次生成的，可只生成左上）
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
