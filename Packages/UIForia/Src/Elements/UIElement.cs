﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using UIForia.Elements.Routing;
using UIForia.Expressions;
using UIForia.Layout;
using UIForia.Rendering;
using UIForia.Routing;
using UIForia.Templates;
using UIForia.UIInput;
using UIForia.Util;
using UnityEngine;

namespace UIForia.Elements {

    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    public class UIElement : IHierarchical {

        public readonly int id;
        public readonly UIStyleSet style;

        internal LightList<UIElement> children; // todo -- replace w/ linked list & child count

        public ExpressionContext templateContext; // todo -- can probably be moved to binding system

        public int enablePhase; // todo -- can probably be removed

        internal UIElementFlags flags;
        public UIElement parent;

        public readonly LayoutResult layoutResult;

        internal static IntMap<ElementColdData> s_ColdDataMap = new IntMap<ElementColdData>();

        public UIView View { get; internal set; }
        
        protected internal UIElement() {
            this.id = Application.NextElementId;
            this.style = new UIStyleSet(this);
            this.layoutResult = new LayoutResult();
            this.flags = UIElementFlags.Enabled;
            this.children = LightList<UIElement>.Get();
        }

        public Application Application => View.Application;

        public UIChildrenElement TranscludedChildren {
            get { return s_ColdDataMap.GetOrDefault(id).transcludedChildren; }
            internal set {
                ElementColdData coldData = s_ColdDataMap.GetOrDefault(id);
                coldData.transcludedChildren = value;
                s_ColdDataMap[id] = coldData;
            }
        }

        public UITemplate OriginTemplate {
            get { return s_ColdDataMap.GetOrDefault(id).templateRef; }
            internal set {
                ElementColdData coldData = s_ColdDataMap.GetOrDefault(id);
                coldData.templateRef = value;
                coldData.InitializeAttributes();
                s_ColdDataMap[id] = coldData;
            }
        }

        public Vector2 scrollOffset { get; internal set; }

        public int depth { get; internal set; }
        public int siblingIndex { get; internal set; }

        public IInputProvider Input => View.Application.InputSystem;

        public int ChildCount => children?.Count ?? 0;

        public bool isSelfEnabled => (flags & UIElementFlags.Enabled) != 0;

        public bool isSelfDisabled => (flags & UIElementFlags.Enabled) == 0;

        public bool isEnabled => !isDestroyed && (flags & UIElementFlags.SelfAndAncestorEnabled) == UIElementFlags.SelfAndAncestorEnabled;

        public bool isDisabled => isDestroyed || (flags & UIElementFlags.Enabled) == 0 || (flags & UIElementFlags.AncestorEnabled) == 0;

        public bool hasDisabledAncestor => (flags & UIElementFlags.AncestorEnabled) == 0;

        public bool isDestroyed => (flags & UIElementFlags.Destroyed) != 0;

        public bool isBuiltIn => (flags & UIElementFlags.BuiltIn) != 0;

        internal bool isPrimitive => (flags & UIElementFlags.Primitive) != 0;

        public bool isCreated => (flags & UIElementFlags.Created) != 0;

        public bool isReady => (flags & UIElementFlags.Ready) != 0;

        public bool isRegistered => (flags & UIElementFlags.Registered) != 0;

        public virtual void OnCreate() { }

        public virtual void OnReady() { }

        public virtual void OnUpdate() { }

        public virtual void OnEnable() { }

        public virtual void OnDisable() { }

        public virtual void OnDestroy() { }

        public virtual void HandleUIEvent(UIEvent evt) { }

        public void Destroy() {
            View.Application.DoDestroyElement(this);
        }

        public UIElement InsertChild(uint idx, UIElement element) {
            if (element == null || element == this || element.isDestroyed) {
                return null;
            }

            if (View == null) {
                element.parent = this;
                element.View = null;
                element.siblingIndex = children.Count;
                element.depth = depth + 1;
                children.Insert((int) idx, element);
            }
            else {
                Application.InsertChild(this, element, (uint) children.Count);
            }

            return element;
        }

        public UIElement AddChild(UIElement element) {
            // todo -- if <Children/> is defined in the template, attach child to that element instead
            if (element == null || element == this || element.isDestroyed) {
                return null;
            }

            if (View == null) {
                element.parent = this;
                element.View = null;
                element.siblingIndex = children.Count;
                element.depth = depth + 1;
                children.Add(element);
            }
            else {
                Application.InsertChild(this, element, (uint) children.Count);
            }

            return element;
        }

        public void TriggerEvent(UIEvent evt) {
            evt.origin = this;
            UIElement ptr = this.parent;
            while (evt.IsPropagating() && ptr != null) {
                ptr.HandleUIEvent(evt);
                ptr = ptr.parent;
            }
        }

        public void SetEnabled(bool active) {
            if (View == null) {
                flags &= ~UIElementFlags.Enabled;
                return;
            }

            if (active && isSelfDisabled) {
                View.Application.DoEnableElement(this);
            }
            else if (!active && isSelfEnabled) {
                View.Application.DoDisableElement(this);
            }
        }

        public UIElement GetChild(int index) {
            if (children == null || (uint) index >= (uint) children.Count) {
                return null;
            }

            return children[index];
        }

        public UIElement FindById(string id) {
            return FindById<UIElement>(id);
        }

        [PublicAPI]
        public T FindById<T>(string id) where T : UIElement {
            if (isPrimitive || children == null) {
                return null;
            }

            for (int i = 0; i < children.Count; i++) {
                if (children[i].GetAttribute("id") == id) {
                    return children[i] as T;
                }

                UIElement childResult = children[i].FindById(id);

                if (childResult != null) {
                    return childResult as T;
                }
            }

            return null;
        }

        [PublicAPI]
        public T FindFirstByType<T>() where T : UIElement {
            if (isPrimitive || children == null) {
                return null;
            }

            for (int i = 0; i < children.Count; i++) {
                if (children[i] is T) {
                    return (T) children[i];
                }

                if (children[i] is UIChildrenElement) {
                    continue;
                }

                if (children[i]?.OriginTemplate is UIElementTemplate) {
                    continue;
                }

                UIElement childResult = children[i].FindFirstByType<T>();
                if (childResult != null) {
                    return (T) childResult;
                }
            }

            return null;
        }

        public List<T> FindByType<T>(List<T> retn = null) where T : UIElement {
            retn = retn ?? new List<T>();
            if (isPrimitive || children == null) {
                return retn;
            }

            for (int i = 0; i < children.Count; i++) {
                if (children[i] is T) {
                    retn.Add((T) children[i]);
                }


                if (children[i] is UIChildrenElement) {
                    continue;
                }

                if (children[i]?.OriginTemplate is UIElementTemplate) {
                    continue;
                }

                children[i].FindByType<T>(retn);
            }

            return retn;
        }

        public override string ToString() {
            if (HasAttribute("id")) {
                return "<" + GetDisplayName() + ":" + GetAttribute("id") + " " + id + ">";
            }

            if (style != null && style.HasBaseStyles) {
                return "<" + GetDisplayName() + ">"; // + style.BaseStyleNames;
            }
            else {
                return "<" + GetDisplayName() + " " + id + ">";
            }
        }

        public virtual string GetDisplayName() {
            return GetType().Name;
        }

        public List<ElementAttribute> GetAttributes(List<ElementAttribute> retn = null) {
            return s_ColdDataMap.GetOrDefault(id).GetAttributes(retn);
        }

        public void SetAttribute(string name, string value) {
            ElementColdData coldData = s_ColdDataMap.GetOrDefault(id);
            string oldValue = coldData.GetAttribute(name).value;
            coldData.SetAttribute(name, value);
            s_ColdDataMap[id] = coldData;
            Application.OnAttributeSet(this, name, value, oldValue);
        }

        public string GetAttribute(string attr) {
            ElementColdData coldData = s_ColdDataMap.GetOrDefault(id);
            if (coldData.TryGetAttribute(attr, out ElementAttribute retn)) {
                return retn.value;
            }

            return null;
        }

        public void OnAttributeAdded(Action<ElementAttribute> handler) {
            ElementColdData coldData = s_ColdDataMap.GetOrDefault(id);
            coldData.onAttributeAdded += handler;
            s_ColdDataMap[id] = coldData;
        }

        public void OnAttributeChanged(Action<ElementAttribute> handler) {
            ElementColdData coldData = s_ColdDataMap.GetOrDefault(id);
            coldData.onAttributeChanged += handler;
            s_ColdDataMap[id] = coldData;
        }

        public void OnAttributeRemoved(Action<ElementAttribute> handler) {
            ElementColdData coldData = s_ColdDataMap.GetOrDefault(id);
            coldData.onAttributeRemoved += handler;
            s_ColdDataMap[id] = coldData;
        }

        public bool HasAttribute(string name) {
            return s_ColdDataMap.GetOrDefault(id).GetAttribute(name).value != null;
        }

        // todo -- remove
        public IRouterElement GetNearestRouter() {
            UIElement ptr = this;
            ElementColdData coldData = s_ColdDataMap.GetOrDefault(id);
            if (coldData.nearestRouter != null) {
                return coldData.nearestRouter;
            }

            while (ptr != null) {
                if (ptr is IRouterElement routeElement) {
                    coldData.nearestRouter = routeElement;
                    s_ColdDataMap[id] = coldData;
                    return routeElement;
                }

                ptr = ptr.parent;
            }

            return null;
        }

        // todo remove
        public RouteParameter GetRouteParameter(string name) {
            UIElement ptr = this;

            while (ptr != null) {
                if (ptr is RouteElement routeElement) {
                    return routeElement.CurrentMatch.GetParameter(name);
                }

                ptr = ptr.parent;
            }

            return default;
        }

        public int UniqueId => id;
        public IHierarchical Element => this;
        public IHierarchical Parent => parent;

        public List<UIElement> GetChildren(List<UIElement> retn = null) {
            retn = ListPool<UIElement>.Get();

            if (children == null) {
                return retn;
            }

            UIElement[] childArray = children.Array;
            for (int i = 0; i < children.Count; i++) {
                retn.Add(childArray[i]);
            }

            return retn;
        }

        internal void InternalDestroy() {
            ElementColdData coldData = s_ColdDataMap.GetOrDefault(id);
            coldData.Destroy();
            s_ColdDataMap.Remove(id);
            LightList<UIElement>.Release(ref children);
            parent = null;
        }

        public bool IsAncestorOf(UIElement potentialParent) {
            if (potentialParent == this || potentialParent == null) {
                return false;
            }

            UIElement ptr = this;
            while (ptr != null) {
                if (ptr.parent == potentialParent) {
                    return true;
                }

                ptr = ptr.parent;
            }

            return false;
        }

        internal UIElementTypeData GetTypeData() {
            UIElementTypeData typeData = default;
            Type elementType = GetType();
            if (s_TypeDataMap.TryGetValue(elementType, out typeData)) {
                return typeData;
            }
            else {
                typeData.requiresUpdate = ReflectionUtil.IsOverride(elementType.GetMethod(nameof(OnUpdate)));
                //typeData.attributes = elementType.GetCustomAttributes();
                s_TypeDataMap[elementType] = typeData;
                return typeData;
            }
        }

        private static readonly Dictionary<Type, UIElementTypeData> s_TypeDataMap = new Dictionary<Type, UIElementTypeData>();

        internal struct UIElementTypeData {

            public bool requiresUpdate;
            // public Attribute[] attributes;

        }

    }

}

