﻿using Rendering;

namespace Src.Systems {

    public class LifeCycleData : IHierarchical {

        public readonly UIElement element;

        public LifeCycleData(UIElement element) {
            this.element = element;
        }

        public int UniqueId => element.id;
        public IHierarchical Element => element;
        public IHierarchical Parent => element.parent;

    }

    public class LifeCycleSystem : ISystem {

        private SkipTree<LifeCycleData> updateTree;
        private SkipTree<LifeCycleData> enableTree;
        private SkipTree<LifeCycleData> disableTree;
        private SkipTree<LifeCycleData> createTree;
        private SkipTree<LifeCycleData> destroyTree;

        public LifeCycleSystem() {
            this.updateTree = new SkipTree<LifeCycleData>();
            this.enableTree = new SkipTree<LifeCycleData>();
            this.disableTree = new SkipTree<LifeCycleData>();
            this.createTree = new SkipTree<LifeCycleData>();
            this.destroyTree = new SkipTree<LifeCycleData>();
        }

        public void OnReset() {
            updateTree.Clear();
            enableTree.Clear();
            disableTree.Clear();
            createTree.Clear();
            destroyTree.Clear();
        }

        public void OnUpdate() {
            updateTree.TraversePreOrder((data) => data.element.OnUpdate());
        }

        public void OnDestroy() {
            destroyTree.Clear();
            createTree.Clear();
            updateTree.Clear();
            enableTree.Clear();
            disableTree.Clear();
        }

        public void OnInitialize() { }

        public void OnElementCreated(UIElementCreationData elementData) {
            UIElement element = elementData.element;
            if (ReflectionUtil.IsOverride(element, nameof(UIElement.OnUpdate))) {
                updateTree.AddItem(new LifeCycleData(element));
            }
            if (ReflectionUtil.IsOverride(element, nameof(UIElement.OnEnable))) {
                enableTree.AddItem(new LifeCycleData(element));
            }
            if (ReflectionUtil.IsOverride(element, nameof(UIElement.OnDisable))) {
                disableTree.AddItem(new LifeCycleData(element));
            }
            if (ReflectionUtil.IsOverride(element, nameof(UIElement.OnCreate))) {
                createTree.AddItem(new LifeCycleData(element));
            }
            if (ReflectionUtil.IsOverride(element, nameof(UIElement.OnDestroy))) {
                destroyTree.AddItem(new LifeCycleData(element));
            }
        }

        // skip tree hierarchy nodes need to enable all ancestors even 
        // if the parent is not in the tree itself
        public void OnElementEnabled(UIElement element) {
            // todo -- not quite right, need to revisit skip tree to call enable 
            // todo    on all nodes that are children of this. Right now the skip 
            // todo    just sets disabled on the node and doesn't traverse, which needs to happen
            LifeCycleData data = enableTree.GetItem(element) ?? new LifeCycleData(element);
            enableTree.EnableHierarchy(data);
            updateTree.EnableHierarchy(data);
        }

        // this awkwardness of newLifeCycleData can be fixed by allowing
        // skip tree to take an item type (element in this case)
        // instead of or in addition to an item instance
        public void OnElementDisabled(UIElement element) {
            LifeCycleData data = enableTree.GetItem(element) ?? new LifeCycleData(element);
            disableTree.DisableHierarchy(data);
        }

        public void OnElementDestroyed(UIElement element) {

            LifeCycleData data = destroyTree.GetItem(element) ?? new LifeCycleData(element);
            destroyTree.TraversePreOrder(data, (item) => item.element.OnDestroy(), true);

            createTree.RemoveHierarchy(data);
            disableTree.RemoveHierarchy(data);
            enableTree.RemoveHierarchy(data);
            updateTree.RemoveHierarchy(data);
            destroyTree.RemoveHierarchy(data);

        }

        public void OnElementShown(UIElement element) {
        }

        public void OnElementHidden(UIElement element) {
        }

    }

}