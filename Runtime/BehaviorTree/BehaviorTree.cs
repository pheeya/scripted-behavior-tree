using System.Collections.Generic;
using CleverCrow.Fluid.BTs.TaskParents;
using CleverCrow.Fluid.BTs.Tasks;
using UnityEngine;

namespace CleverCrow.Fluid.BTs.Trees
{
    public interface IBehaviorTree
    {
        string Name { get; }
        TaskRoot Root { get; }
        int TickCount { get; }

        void OnTaskRan(ITask task);

        void AddActiveTask(ITask task);
        void RemoveActiveTask(ITask task);
    }

    [System.Serializable]
    public class BehaviorTree : IBehaviorTree
    {
        private readonly GameObject _owner;
        private readonly List<ITask> _tasks = new List<ITask>();

        public int TickCount { get; private set; }

        public string Name { get; set; }
        public TaskRoot Root { get; } = new TaskRoot();
        public IReadOnlyList<ITask> ActiveTasks => _tasks;

        List<ITask> m_tasksRanLastTick;

        List<ITask> m_allNodes;
        public BehaviorTree(GameObject owner)
        {
            _owner = owner;
            SyncNodes(Root);

        }

        public void OnBuilt()
        {
            m_totalNodes = 1;
            m_allNodes = new();
            m_allNodes.Add(Root);
            count_nodes(Root);
            m_tasksRanLastTick = new List<ITask>(m_totalNodes);

        }

        private void count_nodes(ITaskParent taskParent)
        {
            foreach (var child in taskParent.Children)
            {
                m_allNodes.Add(child);
                m_totalNodes++;
                var parent = child as ITaskParent;

                if (parent != null)
                {
                    count_nodes(parent);
                }
            }
        }

        public TaskStatus Tick()
        {
            m_tasksRanLastTick.Clear();

            var status = Root.Update();
            if (status != TaskStatus.Continue)
            {
                Reset();
            }

            return status;
        }
        public void OnFixedUpdate()
        {
            // Root.OnFixedUpdate();


// NOTE: 
// even if a task failed, it will be considered in the m_tasksRanListTick list
// Here's an example that explains a situation which requires the node to have this option: 
// An enemy NPC can perform a melee attack, but only if the raycast its shooting hits the player. 
// The raycasting is done in fixed update
// so the behavior tree will reach the melee action, but then return Failure upon the raycast not hitting 
// but the NPC needs to keep checking every fixed update if the raycast hit the player or not. If it does hit, it starts returning Success in its update 
// function
            for (int i = 0; i < m_tasksRanLastTick.Count; i++)
            {
                m_tasksRanLastTick[i].OnFixedUpdate();
            }
        }

        public void OnDrawGizmos()
        {
            // Root.OnDrawGizmos();
            for (int i = 0; i < m_tasksRanLastTick.Count; i++)
            {
                m_tasksRanLastTick[i].OnDrawGizmos();
            }
        }
        public void OnEnable()
        {
            for (int i = 0; i < m_allNodes.Count; i++)
            {
                m_allNodes[i].OnEnable();
            }
        }
        public void OnDisable()
        {
            for (int i = 0; i < m_allNodes.Count; i++)
            {
                m_allNodes[i].OnDisable();
            }
        }
        public void Reset()
        {

            foreach (var task in _tasks)
            {
                task.End();
            }

            _tasks.Clear();
            TickCount++;
        }

        public void AddNode(ITaskParent parent, ITask child)
        {
            parent.AddChild(child);
            child.ParentTree = this;
            child.Owner = _owner;
        }

        public void Splice(ITaskParent parent, BehaviorTree tree)
        {
            parent.AddChild(tree.Root);

            SyncNodes(tree.Root);
        }
        int m_totalNodes = 0;
        private void SyncNodes(ITaskParent taskParent)
        {
            taskParent.Owner = _owner;
            taskParent.ParentTree = this;
            foreach (var child in taskParent.Children)
            {
                child.Owner = _owner;
                child.ParentTree = this;

                var parent = child as ITaskParent;
                if (parent != null)
                {
                    SyncNodes(parent);
                }
            }
        }

        public void AddActiveTask(ITask task)
        {
            _tasks.Add(task);
        }

        public void RemoveActiveTask(ITask task)
        {
            _tasks.Remove(task);
        }

        public void OnTaskRan(ITask task)
        {
            m_tasksRanLastTick.Add(task);
        }

    }
}