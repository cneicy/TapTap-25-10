using System;
using Data;
using Game.Level.CheckPoint;
using ShrinkEventBus;
using UnityEngine;

namespace Game.Level
{
    public class LevelLoadedEvent : EventBase
    {
        public LevelBase Level { get; }
        public string LevelName { get; }
        public bool IsTrueWorld { get; }

        public LevelLoadedEvent(LevelBase level, string levelName, bool isTrueWorld)
        {
            Level = level;
            IsTrueWorld = isTrueWorld;
            LevelName = levelName;
        }
    }

    public abstract class LevelBase : MonoBehaviour
    {
        public CheckPointBase CurrentCheckPoint
        {
            get => _currentCheckPoint;
            set => _currentCheckPoint = value;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }
        public bool IsTrueWorld
        {
            get => _isTrueWorld;
            set => _isTrueWorld = value;
        }
        
        [SerializeField] private string _name;
        [SerializeField] private CheckPointBase _currentCheckPoint;
        [SerializeField] private bool _isTrueWorld;
        
        public virtual void OnEnable()
        {
            EventBus.TriggerEvent(new LevelLoadedEvent(this, Name, IsTrueWorld));
            DataManager.Instance.SetData("CurrentLevel",_name);
        }
    }
}