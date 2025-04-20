using System.Collections.Generic;
using Chris.Gameplay;
using UnityEngine;
using UnityEngine.Pool;
namespace Chris.AI.EQS
{
    public interface IFieldViewQueryComponent
    {
        /// <summary>
        /// Request a new query from target's field view
        /// </summary>
        /// <returns></returns>
        bool RequestFieldViewQuery();

        /// <summary>
        /// Detect whether it can see the target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="fromPosition"></param>
        /// <param name="fromRotation"></param>
        /// <param name="filterTags"></param>
        /// <returns></returns>
        bool Detect(Vector3 target, Vector3 fromPosition, Quaternion fromRotation, string[] filterTags = null);
        
        /// <summary>
        /// Query actors overlap in field of view from cache
        /// </summary>
        /// <param name="actors"></param>
        void CollectViewActors(List<Actor> actors);
        
        /// <summary>
        /// Query actors overlap in field of view from cache
        /// </summary>
        /// <param name="actors"></param>
        void CollectViewActors<T>(List<T> actors) where T : Actor;
    }
    
    /// <summary>
    /// Field view prime query data provider associated with an Actor as component
    /// </summary>
    public class FieldViewQueryComponent : ActorComponent, IFieldViewQueryComponent
    {
        [Header("Data")]
        public FieldView fieldView = new()
        {
            radius = 20,
            angle = 120,
            sides = 8,
            blend = 0.5f
        };
        
        public LayerMask queryLayerMask;
        
        private FieldViewQuerySystem _system;
        
        [Header("Gizmos")]
        public Vector3 offset;
        
        private void Start()
        {
            _system = WorldSubsystem.GetOrCreate<FieldViewQuerySystem>();
            if (_system == null)
            {
                Debug.LogError($"[FieldViewPrimeQueryComponent] Can not get FieldViewPrimeQuerySystem dynamically.");
            }
        }
        
        public bool RequestFieldViewQuery()
        {
            if (_system == null)
            {
                return false;
            }
            _system.EnqueueCommand(new FieldViewQueryCommand()
            {
                Self = GetActor().GetActorHandle(),
                FieldView = fieldView,
                LayerMask = queryLayerMask
            });
            return true;
        }
        
        public void CollectViewActors(List<Actor> actors)
        {
            _system.GetActorsInFieldView(GetActor().GetActorHandle(), actors);
        }
        
        public void CollectViewActors<T>(List<T> actors) where T : Actor
        {
            var list = ListPool<Actor>.Get();
            CollectViewActors(list);
            foreach (var actor in list)
            {
                if (actor is T tActor) actors.Add(tActor);
            }
            ListPool<Actor>.Release(list);
        }
        
        public bool Detect(Vector3 target, Vector3 fromPosition, Quaternion fromRotation, string[] filterTags = null)
        {
            return fieldView.Detect(target, fromPosition, fromRotation, queryLayerMask, filterTags);
        }
        
        private void OnDrawGizmos()
        {
            fieldView.DrawGizmos(transform.position + offset, transform.rotation);
        }
    }
}
