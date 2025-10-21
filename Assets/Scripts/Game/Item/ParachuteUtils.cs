using UnityEngine;

namespace Game.Item
{
    public static class ParachuteUtils
    {
        /// <summary>
        /// 销毁指定玩家身上的所有降落伞（要求生成时把降落伞设为玩家的子物体）。
        /// </summary>
        public static void DestroyAllParachutesUnder(Transform ownerRoot)
        {
            if (ownerRoot == null) return;

            var growers = ownerRoot.GetComponentsInChildren<ParachuteGrower>(includeInactive: true);
            foreach (var g in growers)
            {
                // 优先走优雅的收尾接口（含停止协程等）
                g.StopAndDestroy();
            }
        }

        /// <summary>
        /// 没把降落伞挂到玩家节点下时使用。
        /// </summary>
        public static void DestroyAllParachutesInScene()
        {
            var growers = Object.FindObjectsOfType<ParachuteGrower>(includeInactive: true);
            foreach (var g in growers) g.StopAndDestroy();
        }
    }
}