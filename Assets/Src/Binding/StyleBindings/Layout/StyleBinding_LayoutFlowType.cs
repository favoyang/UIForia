﻿using Rendering;

namespace Src.StyleBindings {

    public class StyleBinding_LayoutFlowType : StyleBinding {

        public readonly Expression<LayoutFlowType> expression;

        public StyleBinding_LayoutFlowType(StyleState state, Expression<LayoutFlowType> expression) : base(state) {
            this.expression = expression;
        }

        public override void Execute(UIElement element, UITemplateContext context) {
            LayoutFlowType flow = element.style.GetLayoutFlow(state);
            LayoutFlowType newFlow = expression.EvaluateTyped(context);
            if (flow != newFlow) {
                element.style.SetLayoutFlow(newFlow, state);
            }
        }

        public override bool IsConstant() {
            return expression.IsConstant();
        }

        public override void Apply(UIStyle style, UITemplateContext context) {
            style.layoutParameters.flow = expression.EvaluateTyped(context);
        }

        public override void Apply(UIStyleSet styleSet, UITemplateContext context) {
            styleSet.SetLayoutFlow(expression.EvaluateTyped(context), state);
        }

    }

}