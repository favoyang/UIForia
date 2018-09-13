using System;
using System.Reflection;

namespace Src.Input {

    public abstract class DragEventCreator : IComparable<DragEventCreator> {

        public readonly KeyboardModifiers requiredModifiers;
        public readonly EventPhase eventPhase;
#if DEBUG
        public MethodInfo methodInfo;
#endif
        protected DragEventCreator(KeyboardModifiers requiredModifiers, EventPhase eventPhase) {
            this.requiredModifiers = requiredModifiers;
            this.eventPhase = eventPhase;
        }

        public abstract DragEvent Invoke(object target, UITemplateContext context, MouseInputEvent evt);

        public int CompareTo(DragEventCreator other) {
            int modifierResult = CompareModifiers(other);
            if (modifierResult != 0) return modifierResult;

            return 1;
        }

        protected bool ShouldRun(MouseInputEvent evt) {
            // if all required modifiers are present these should be equal
            return (requiredModifiers & evt.modifiers) == requiredModifiers;
        }

        private int CompareModifiers(DragEventCreator other) {
            if (requiredModifiers == KeyboardModifiers.None && other.requiredModifiers == KeyboardModifiers.None) return 0;
            if (requiredModifiers != KeyboardModifiers.None && other.requiredModifiers != KeyboardModifiers.None) return 0;
            return (requiredModifiers != KeyboardModifiers.None) ? 1 : -1;
        }

    }

    public class DragEventCreator_Expression : DragEventCreator {

        private readonly Expression<DragEvent> expression;

        public DragEventCreator_Expression(Expression<DragEvent> expression, KeyboardModifiers modifiers, EventPhase phase)
            : base(modifiers, phase) {
            this.expression = expression;
        }

        public override DragEvent Invoke(object target, UITemplateContext context, MouseInputEvent evt) {
            return ShouldRun(evt) ? expression.EvaluateTyped(context) : null;
        }

    }

    public class DragEventCreator_IgnoreEvent<T> : DragEventCreator {

        private readonly Func<T, DragEvent> handler;

        public DragEventCreator_IgnoreEvent(KeyboardModifiers requiredModifiers, EventPhase phase, Func<T, DragEvent> handler)
            : base(requiredModifiers, phase) {
            this.handler = handler;
        }

        public override DragEvent Invoke(object target, UITemplateContext context, MouseInputEvent evt) {
            return ShouldRun(evt) ? handler((T) target) : null;
        }

    }

    public class DragEventCreator_WithEvent<T> : DragEventCreator {

        private readonly Func<T, MouseInputEvent, DragEvent> handler;

        public DragEventCreator_WithEvent(KeyboardModifiers requiredModifiers, EventPhase phase, Func<T, MouseInputEvent, DragEvent> handler)
            : base(requiredModifiers, phase) {
            this.handler = handler;
        }

        public override DragEvent Invoke(object target, UITemplateContext context, MouseInputEvent evt) {
            return ShouldRun(evt) ? handler((T) target, evt) : null;
        }

    }

}