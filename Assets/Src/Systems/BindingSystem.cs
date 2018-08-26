﻿using Rendering;

namespace Src.Systems {

    public class BindingSystem : ISystem {

        private readonly SkipTree<TemplateBinding> bindingSkipTree;
        private readonly SkipTree<TemplateBinding> conditionalSkipTree;

        public BindingSystem() {
            this.bindingSkipTree = new SkipTree<TemplateBinding>();
            this.conditionalSkipTree = new SkipTree<TemplateBinding>();
        }

        public void OnReset() {
            bindingSkipTree.Clear();
            conditionalSkipTree.Clear();
        }

        public void OnUpdate() {

            conditionalSkipTree.ConditionalTraversePreOrder((item) => {
                for (int i = 0; i < item.bindings.Length; i++) {
                    item.bindings[i].Execute(item.element, item.context);
                }
                return (item.element.flags & UIElementFlags.Enabled) != 0;
            });

            bindingSkipTree.TraversePreOrder((item) => {
                for (int i = 0; i < item.bindings.Length; i++) {
                    item.bindings[i].Execute(item.element, item.context);
                }
            });
        }

        public void OnDestroy() {
            conditionalSkipTree.Clear();
            bindingSkipTree.Clear();
        }

        public void OnInitialize() { }

        public void OnElementCreated(UIElementCreationData data) {
            if (data.constantBindings.Length != 0) {
                for (int i = 0; i < data.constantBindings.Length; i++) {
                    data.constantBindings[i].Execute(data.element, data.context);
                }
            }

            if (data.conditionalBindings.Length != 0) {
                conditionalSkipTree.AddItem(new TemplateBinding(data.element, data.conditionalBindings, data.context));
            }

            if (data.bindings.Length == 0) return;

            if (data.element is UIRepeatChild) {
                TemplateBinding repeatChildBinding = new TemplateBinding(data.element, data.bindings, data.context);
                TemplateBinding parent = bindingSkipTree.GetItem(data.element.parent);
                int childCount = bindingSkipTree.GetChildCount(parent);
                bindingSkipTree.AddItem(repeatChildBinding);
                bindingSkipTree.SetSiblingIndex(repeatChildBinding, childCount - 1);
                return;
            }

            if (data.element is UIRepeatTerminal) {
                TemplateBinding terminalBinding = new TemplateBinding(data.element, data.bindings, data.context);
                bindingSkipTree.AddItem(terminalBinding);
                bindingSkipTree.SetSiblingIndex(terminalBinding, int.MaxValue);
                return;
            }

            bindingSkipTree.AddItem(new TemplateBinding(data.element, data.bindings, data.context));
        }

        public void OnElementEnabled(UIElement element) {
            bindingSkipTree.EnableHierarchy(element);
//            conditionalSkipTree.EnableHierarchy(element);
        }

        public void OnElementDisabled(UIElement element) {
            bindingSkipTree.DisableHierarchy(element);
//            conditionalSkipTree.DisableHierarchy(element);
        }

        public void OnElementDestroyed(UIElement element) {
            bindingSkipTree.RemoveHierarchy(element);
            conditionalSkipTree.RemoveHierarchy(element);
        }

        public void OnElementShown(UIElement element) {
        }

        public void OnElementHidden(UIElement element) {
        }

    }

}