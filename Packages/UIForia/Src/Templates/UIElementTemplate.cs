using System;
using System.Collections.Generic;
using UIForia.Util;
using UnityEngine;

namespace UIForia {

    // wraps a ParsedTemplate, this is what is created by parent templates
    // properties need to be merged from the parsed template <Content/> tag (root)
    public class UIElementTemplate : UITemplate {

        private Type rootType;
        private readonly string typeName;
        private ParsedTemplate templateToExpand;
        private TemplateType templateType;
        private List<UISlotContentTemplate> slotContentTemplates;
        
        public UIElementTemplate(Application app, string typeName, List<UITemplate> childTemplates, List<AttributeDefinition> attributes = null)
            : base(app, childTemplates, attributes) {
            this.typeName = typeName;
        }

        public UIElementTemplate(Application app, Type rootType, List<UITemplate> childTemplates, List<AttributeDefinition> attributes = null)
            : base(app, childTemplates, attributes) {
            this.rootType = rootType;
        }

        public Type RootType => rootType;
        protected override Type elementType => rootType;

        public override void Compile(ParsedTemplate template) {
            if (isCompiled) return;
            isCompiled = true;

            if (rootType == null) {
                rootType = TypeProcessor.GetType(typeName, template.Imports).rawType;
            }

            if (!(typeof(UIElement).IsAssignableFrom(elementType))) {
                Debug.Log($"{elementType} must be a subclass of {typeof(UIElement)} in order to be used in templates");
                return;
            }

            templateToExpand = app.templateParser.GetParsedTemplate(rootType);
                        
            ResolveBaseStyles(template);
            CompileStyleBindings(template);
            CompileInputBindings(template, templateToExpand.rootElementTemplate != this);
            CompilePropertyBindings(template);
            ResolveActualAttributes(); // todo combine with <Contents/> attrs
            BuildBindings();            

            if (templateToExpand.rootElementTemplate == this) {
                return;
            }

            templateToExpand.Compile();
            
            UITemplate expandedRoot = templateToExpand.rootElementTemplate;

            triggeredBindings = MergeBindingArray(triggeredBindings, expandedRoot.triggeredBindings);
            perFrameBindings = MergeBindingArray(perFrameBindings, expandedRoot.perFrameBindings);
            mouseEventHandlers = MergeArray(mouseEventHandlers, expandedRoot.mouseEventHandlers);
            keyboardEventHandlers = MergeArray(keyboardEventHandlers, expandedRoot.keyboardEventHandlers);
            dragEventCreators = MergeArray(dragEventCreators, expandedRoot.dragEventCreators);
            dragEventHandlers = MergeArray(dragEventHandlers, expandedRoot.dragEventHandlers);
            baseStyles = MergeArray(baseStyles, expandedRoot.baseStyles);
            Array.Reverse(baseStyles);
        }

        public override void PostCompile(ParsedTemplate template) {
              
            for (int i = 0; i < childTemplates.Count; i++) {
                if (childTemplates[i] is UISlotContentTemplate slotContent) {
                    childTemplates.RemoveAt(i--);
                    slotContentTemplates = slotContentTemplates ?? new List<UISlotContentTemplate>(2);
                    slotContentTemplates.Add(slotContent);
                }
            }
        }
        
        private static Binding[] MergeBindingArray(Binding[] a, Binding[] b) {
            int startCount = a.Length;
            a = ResizeToMerge(a, b);
            int idx = 0;
            for (int i = startCount; i < a.Length; i++) {
                a[i] = b[idx++];
            }

            return a;
        }

        private static T[] MergeArray<T>(T[] a, T[] b) {
            int startCount = a.Length;
            a = ResizeToMerge(a, b);
            int idx = 0;

            for (int i = startCount; i < a.Length; i++) {
                a[i] = b[idx++];
            }

            return a;
        }

        private static T[] ResizeToMerge<T>(T[] a, T[] b) {
            if (a.Length == 0 && b.Length == 0) {
                return a;
            }

            if (a.Length > 0 && b.Length == 0) {
                return a;
            }

            if (a.Length == 0 && b.Length > 0) {
                return new T[b.Length];
            }

            if (a.Length > 0 && b.Length > 0) {
                Array.Resize(ref a, a.Length + b.Length);
            }

            return a;
        }

        public UIElement CreateUnscoped(Type overrideRootType = null, List<UISlotContentTemplate> inputSlotContent = null) {
            Type actualRootType = overrideRootType ?? rootType;
            UIElement element = (UIElement) Activator.CreateInstance(actualRootType);
            element.flags |= UIElementFlags.TemplateRoot;
            element.OriginTemplate = this;
            templateToExpand.Compile();

            List<UITemplate> actualChildren = childTemplates;

            TemplateScope scope = new TemplateScope(element);
            if (inputSlotContent != null) {
                scope.slotContents = inputSlotContent;
            }

            element.children = LightListPool<UIElement>.Get();

            element.templateContext = new ExpressionContext(element, element);

            for (int i = 0; i < actualChildren.Count; i++) {
                element.children.Add(actualChildren[i].CreateScoped(scope));
                element.children[i].parent = element;
            }

            UIChildrenElement childrenElement = element.TranscludedChildren;
            if (childrenElement != null) {
                childrenElement.children = LightListPool<UIElement>.Get();
            }

            return element;
        }

        // children of this element are transcluded
        // actual children are built from parsed template's root children
        public override UIElement CreateScoped(TemplateScope inputScope) {
            UIElement element = (UIElement) Activator.CreateInstance(rootType);
            element.flags |= UIElementFlags.TemplateRoot;
            element.OriginTemplate = this;

            templateToExpand.Compile();

            TemplateScope scope = new TemplateScope(element);

            //int childCount = templateToExpand.childTemplates.Count;

            element.children =  LightListPool<UIElement>.Get();
            element.templateContext = new ExpressionContext(inputScope.rootElement, element);
            scope.slotContents = slotContentTemplates;
            
            CreateChildren(element, templateToExpand.childTemplates, scope); // recycle scope here?

            UIChildrenElement childrenElement = element.TranscludedChildren;

            if (childrenElement != null) {
                childrenElement.children = LightListPool<UIElement>.Get();
                for (int i = 0; i < childTemplates.Count; i++) {
                    childrenElement.children.Add(childTemplates[i].CreateScoped(inputScope));
                    childrenElement.children[i].parent = childrenElement;
                }
            }

            return element;
        }

    }

}