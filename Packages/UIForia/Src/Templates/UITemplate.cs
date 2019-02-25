using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UIForia.Compilers;
using UIForia.Compilers.Style;
using UIForia.Input;
using UIForia.Rendering;
using UIForia.StyleBindings;
using UIForia.Util;
using UnityEngine;

namespace UIForia {

    public abstract class UITemplate {

        public const string k_SpecialAttrPrefix = "x-";

        public bool isCompiled;

        public readonly List<UITemplate> childTemplates;
        public readonly List<AttributeDefinition> attributes; // all attributes
        public List<ElementAttribute> templateAttributes; // actual attributes

        public string elementName;

        public Binding[] perFrameBindings;
        public Binding[] triggeredBindings;

        public UIStyleGroupContainer[] baseStyles;

        public DragEventCreator[] dragEventCreators;
        public DragEventHandler[] dragEventHandlers;
        public MouseEventHandler[] mouseEventHandlers;
        public KeyboardEventHandler[] keyboardEventHandlers;

        protected static readonly LightList<Binding> s_BindingList = new LightList<Binding>();
        protected static readonly StyleBindingCompiler s_StyleCompiler = new StyleBindingCompiler();
        protected static readonly InputBindingCompiler s_InputCompiler = new InputBindingCompiler();
        protected static readonly PropertyBindingCompiler s_PropCompiler = new PropertyBindingCompiler();
        
        public readonly Application app;
        
        protected UITemplate(Application app, List<UITemplate> childTemplates, List<AttributeDefinition> attributes = null) {
            this.app = app;
            this.childTemplates = childTemplates;
            this.attributes = attributes;
            this.perFrameBindings = Binding.EmptyArray;
            this.triggeredBindings = Binding.EmptyArray;
            this.baseStyles = ArrayPool<UIStyleGroupContainer>.Empty;
            this.dragEventCreators = ArrayPool<DragEventCreator>.Empty;
            this.dragEventHandlers = ArrayPool<DragEventHandler>.Empty;
            this.mouseEventHandlers = ArrayPool<MouseEventHandler>.Empty;
            this.keyboardEventHandlers = ArrayPool<KeyboardEventHandler>.Empty;
        }

        protected abstract Type elementType { get; }

        public abstract UIElement CreateScoped(TemplateScope inputScope);

        protected void BuildBindings() {
            int triggeredCount = 0;
            int perFrameCount = 0;

            for (int i = 0; i < s_BindingList.Count; i++) {
                if (s_BindingList[i].IsTriggered) {
                    triggeredCount++;
                }
                else {
                    perFrameCount++;
                }
            }

            if (triggeredCount > 0) {
                triggeredBindings = new Binding[triggeredCount];
            }

            if (perFrameCount > 0) {
                perFrameBindings = new Binding[perFrameCount];
            }

            perFrameCount = 0;
            triggeredCount = 0;

            for (int i = 0; i < s_BindingList.Count; i++) {
                if (s_BindingList[i].IsTriggered) {
                    triggeredBindings[triggeredCount++] = s_BindingList[i];
                }
                else {
                    // swap enabled binding to the front
                    perFrameBindings[perFrameCount++] = s_BindingList[i];
                    if (s_BindingList[i] is EnabledBinding) {
                        Binding tmp = perFrameBindings[0];
                        perFrameBindings[0] = s_BindingList[i];
                        perFrameBindings[perFrameCount - 1] = tmp;
                    }
                }
            }

            s_BindingList.Clear();
        }

        protected static void CreateChildren(UIElement element, IList<UITemplate> templates, TemplateScope inputScope) {
            for (int i = 0; i < templates.Count; i++) {
                if (templates[i] is UISlotTemplate slotTemplate) {
                    UISlotContentTemplate contentTemplate = inputScope.FindSlotContent(slotTemplate.SlotName);
                    if (contentTemplate != null) {
                        element.children.Add(slotTemplate.CreateWithContent(inputScope, contentTemplate.childTemplates));
                    }
                    else {
                        element.children.Add(slotTemplate.CreateWithDefault(inputScope));
                    }
                }
                else {
                    element.children.Add(templates[i].CreateScoped(inputScope));
                }

                element.children[i].parent = element;
            }
        }

        public virtual void Compile(ParsedTemplate template) {
            if (isCompiled) return;
            isCompiled = true;
            if (!(typeof(UIElement).IsAssignableFrom(elementType))) {
                Debug.Log($"{elementType} must be a subclass of {typeof(UIElement)} in order to be used in templates");
                return;
            }

            ResolveBaseStyles(template);
            CompileStyleBindings(template);
            CompileInputBindings(template, false);
            CompilePropertyBindings(template);
            ResolveActualAttributes();
            BuildBindings();
        }

        protected void CompileStyleBindings(ParsedTemplate template) {
            if (attributes == null || attributes.Count == 0) return;

            s_StyleCompiler.SetCompiler(template.compiler);

            for (int i = 0; i < attributes.Count; i++) {
                AttributeDefinition attr = attributes[i];
                StyleBinding binding = s_StyleCompiler.Compile(template.RootType, elementType, attr);

                if (binding == null) {
                    continue;
                }

                s_BindingList.Add(binding);
                attr.isCompiled = true;

                if (binding.IsConstant()) {
                    binding.bindingType = BindingType.Constant;
                }
            }
        }

        [PublicAPI]
        public AttributeDefinition GetAttribute(string attributeName) {
            if (attributes == null) return null;

            for (int i = 0; i < attributes.Count; i++) {
                if (attributes[i].key == attributeName) return attributes[i];
            }

            return null;
        }

        [PublicAPI]
        public List<AttributeDefinition> GetUncompiledAttributes() {
            return attributes.Where((attr) => !attr.isCompiled).ToList();
        }

        protected void ResolveActualAttributes() {
            if (attributes == null) return;
            IEnumerable<AttributeDefinition> realAttributes = attributes.Where(a => a.isRealAttribute).ToArray();
            if (realAttributes.Any()) {
                templateAttributes = new List<ElementAttribute>();
                foreach (AttributeDefinition s in realAttributes) {
                    templateAttributes.Add(new ElementAttribute(s.key.Substring(k_SpecialAttrPrefix.Length), s.value));
                }                
            }
        }

        protected void CompileInputBindings(ParsedTemplate template, bool attributesOnly) {
            // todo can't pool the lists since they get cached... make this better
            Type rootType = template.RootType;
            s_InputCompiler.SetCompiler(template.compiler);
            List<MouseEventHandler> mouseHandlers = s_InputCompiler.CompileMouseEventHandlers(rootType, elementType, attributes, attributesOnly);
            List<KeyboardEventHandler> keyboardHandlers = s_InputCompiler.CompileKeyboardEventHandlers(rootType, elementType, attributes, attributesOnly);
            List<DragEventCreator> dragCreators = s_InputCompiler.CompileDragEventCreators(rootType, elementType, attributes, attributesOnly);
            List<DragEventHandler> dragHandlers = s_InputCompiler.CompileDragEventHandlers(rootType, elementType, attributes, attributesOnly);

            if (mouseHandlers != null) {
                mouseEventHandlers = mouseHandlers.ToArray();
            }

            if (keyboardHandlers != null) {
                keyboardEventHandlers = keyboardHandlers.ToArray();
            }

            if (dragCreators != null) {
                dragEventCreators = dragCreators.ToArray();
            }

            if (dragHandlers != null) {
                dragEventHandlers = dragHandlers.ToArray();
            }
        }

        protected void CompilePropertyBindings(ParsedTemplate template) {
            if (attributes == null || attributes.Count == 0) return;

            try {
                s_PropCompiler.SetCompiler(template.compiler);
                for (int i = 0; i < attributes.Count; i++) {
                    if (attributes[i].isCompiled) continue;

                    if (attributes[i].key.StartsWith("x-") || attributes[i].key == "style") {
                        continue;
                    }

                    attributes[i].isCompiled = true;
                    Binding binding = s_PropCompiler.CompileAttribute(template.rootElementTemplate.RootType, elementType, attributes[i]);
                    if (binding != null) {
                        if (binding.IsConstant()) {
                            binding.bindingType = BindingType.Constant;
                        }

                        s_BindingList.Add(binding);
                    }
                }
            }
            catch (Exception e) {
                Debug.Log(e);
            }
        }

        protected void ResolveBaseStyles(ParsedTemplate template) {
            List<UIStyleGroupContainer> list = ListPool<UIStyleGroupContainer>.Get();

            if (elementName != null) {
                UIStyleGroupContainer styleGroup = template.ResolveElementStyle(elementName);
                if (styleGroup != default) {
                    styleGroup.styleType = StyleType.Implicit;
                    list.Add(styleGroup);
                }
            }

            AttributeDefinition styleAttr = GetAttribute("style");
            if (styleAttr != null) {
                if (styleAttr.value[0] == '[') {
                    Expression<string[]> styleExpressions = template.compiler.Compile<string[]>(template.RootType, styleAttr.value);

                    if (styleExpressions is ArrayLiteralExpression<string> arrayExpr) {
                        DynamicStyleBinding styleBinding = new DynamicStyleBinding(template, arrayExpr);
                        if (styleBinding.IsConstant()) {
                            styleBinding.bindingType = BindingType.Constant;
                        }
                        else {
                            styleBinding.bindingType = BindingType.Normal;
                        }

                        s_BindingList.Add(styleBinding);
                    }

                    return;
                }

                // a list of styles has been defined
                if (styleAttr.value.IndexOf(' ') != -1) {
                    string[] names = styleAttr.value.Split(' ');
                    foreach (string part in names) {
                        UIStyleGroupContainer style = template.ResolveStyleGroup(part.Trim());
                        if (style != null) {
                            list.Add(style);
                        }
                    }
                }
                else {
                    UIStyleGroupContainer style = template.ResolveStyleGroup(styleAttr.value);
                    if (style != null) {
                        list.Add(style);
                    }
                }
            }

            if (list.Count > 0) {
                baseStyles = list.ToArray();
            }

            ListPool<UIStyleGroupContainer>.Release(ref list);
        }

        public virtual void PostCompile(ParsedTemplate template) { }

    }

}