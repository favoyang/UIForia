﻿using System;
using System.Collections.Generic;
using UIForia.Animation;
using UIForia.Compilers.Style;
using UIForia.Elements;
using UIForia.Rendering;
using UIForia.Util;
using UnityEngine;

namespace UIForia.Systems {

    public interface IStylePropertiesWillChangeHandler {

        void OnStylePropertiesWillChange();

    }
    
    public interface IStylePropertiesDidChangeHandler {

        void OnStylePropertiesDidChange();

    }
    
    public interface IStyleChangeHandler {

        void OnStylePropertyChanged(in StyleProperty property);

    }
    
    public class StyleSystem : IStyleSystem {

        protected readonly StyleAnimator animator;

        public event Action<UIElement, LightList<StyleProperty>> onStylePropertyChanged;

        private static readonly Stack<UIElement> s_ElementStack = new Stack<UIElement>();

        private readonly IntMap<ChangeSet> m_ChangeSets;

        public StyleSystem() {
            this.animator = new StyleAnimator();
            this.m_ChangeSets = new IntMap<ChangeSet>();
        }

        public void PlayAnimation(UIStyleSet styleSet, StyleAnimation animation, AnimationOptions overrideOptions = default(AnimationOptions)) {
            int animationId = animator.PlayAnimation(styleSet, animation, overrideOptions);
        }

        public void SetViewportRect(Rect viewport) {
            animator.SetViewportRect(viewport);
        }

        public void OnReset() {
            animator.Reset();
        }

        public void OnElementCreated(UIElement element) {

            UIStyleGroupContainer[] baseStyles = element.OriginTemplate.baseStyles;

            element.style.styleSystem = this;

            element.style.Initialize(baseStyles);

            if (element.children != null) {
                for (int i = 0; i < element.children.Count; i++) { 
                    OnElementCreated(element.children[i]);
                }
            }
            // todo need to trickle inherited properties into newly created elements (repeat children, etc)
        }

        public void OnUpdate() {
            animator.OnUpdate();

            if (onStylePropertyChanged == null) {
                return;
            }

            m_ChangeSets.ForEach(this, (id, changeSet, self) => {

                if (changeSet.element is IStylePropertiesWillChangeHandler willChangeHandler) {
                    willChangeHandler.OnStylePropertiesWillChange();
                }
                
                if (changeSet.element.isEnabled) {
                    self.onStylePropertyChanged.Invoke(changeSet.element, changeSet.changes);
                }

                if (changeSet.element is IStyleChangeHandler changeHandler) {
                    StyleProperty[] properties = changeSet.changes.Array;
                    int count = changeSet.changes.Count;
                    for (int i = 0; i < count; i++) {
                        changeHandler.OnStylePropertyChanged(properties[i]);
                    }
                }

                if (changeSet.element is IStylePropertiesDidChangeHandler didChangeHandler) {
                    didChangeHandler.OnStylePropertiesDidChange();
                }
                
                LightListPool<StyleProperty>.Release(ref changeSet.changes);
                changeSet.element = null;
            });

            m_ChangeSets.Clear();
        }

        public void OnDestroy() {
        }

        public void OnViewAdded(UIView view) {
        }

        public void OnViewRemoved(UIView view) {
        }

        public void OnElementEnabled(UIElement element) {
        }

        public void OnElementDisabled(UIElement element) {
        }

        public void OnElementDestroyed(UIElement element) {
        }

        public void OnAttributeSet(UIElement element, string attributeName, string currentValue, string attributeValue) {
            element.style.UpdateApplicableAttributeRules(attributeName, attributeValue);
        }

        private void AddToChangeSet(UIElement element, StyleProperty property) {
            ChangeSet changeSet;
            if (!m_ChangeSets.TryGetValue(element.id, out changeSet)) {
                changeSet = new ChangeSet(element, LightListPool<StyleProperty>.Get());
                m_ChangeSets[element.id] = changeSet;
            }

            changeSet.changes.Add(property);
        }

        // todo -- buffer & flush these instead of doing it all at once
        public void SetStyleProperty(UIElement element, StyleProperty property) {
            AddToChangeSet(element, property);

            if (!StyleUtil.IsInherited(property.propertyId) || element.children == null || element.children.Count == 0) {
                return;
            }

            if (property.IsUnset) {
                UIElement ptr = element.parent;
                StyleProperty parentProperty = StyleProperty.Unset(property.propertyId);

                while (ptr != null) {
                    parentProperty = ptr.style.GetPropertyValue(property.propertyId);
                    if (parentProperty.IsDefined) {
                        break;
                    }

                    ptr = ptr.parent;
                }

                if (!parentProperty.IsDefined) {
                    parentProperty = DefaultStyleValues_Generated.GetPropertyValue(property.propertyId);
                }

                property = parentProperty;
            }

            for (int i = 0; i < element.children.Count; i++) {
                s_ElementStack.Push(element.children[i]);
            }

            while (s_ElementStack.Count > 0) {
                UIElement descendent = s_ElementStack.Pop();

                if (!descendent.style.SetInheritedStyle(property)) {
                    continue;
                }

                AddToChangeSet(descendent, property);

                if (descendent.children == null) {
                    continue;
                }

                for (int i = 0; i < descendent.children.Count; i++) {
                    s_ElementStack.Push(descendent.children[i]);
                }
            }
        }

        private struct ChangeSet {

            public UIElement element;
            public LightList<StyleProperty> changes;

            public ChangeSet(UIElement element, LightList<StyleProperty> changes) {
                this.element = element;
                this.changes = changes;
            }

        }

    }

}
