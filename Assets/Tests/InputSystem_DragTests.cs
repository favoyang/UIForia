using System;
using System.Collections.Generic;
using NUnit.Framework;
using Src;
using Src.Input;
using Tests.Mocks;
using UnityEngine;
using static Tests.TestUtils;

[TestFixture]
public class InputSystem_DragTests {

    public class TestDragEvent : DragEvent {

        public string source;

        public TestDragEvent(string source) {
            this.source = source;
        }

    }

    [Template(TemplateType.String, @"
    
        <UITemplate>
            <Contents style.layoutType='Fixed'>
                <Group onDragCreate='{CreateDragFromChild($event, 0)}' style.translation='vec2(0, 0)'   style.size='size(100, 100)'/>
            </Contents>
        </UITemplate>

    ")]
    public class DragTestThing : UIElement {

        public bool ignoreChildDrag;
        
        [OnDragCreate]
        public DragEvent CreateDragEvent() {
            return new TestDragEvent("root");
        }

        public DragEvent CreateDragFromChild(MouseInputEvent evt, int index) {
            if (ignoreChildDrag) return null;
            return new TestDragEvent("child" + index);
        }

    }
    
    [Template(TemplateType.String, @"
    
        <UITemplate>
            <Contents style.layoutType='Fixed'>
            </Contents>
        </UITemplate>

    ")]
    public class FailDragTestThing : UIElement {
      
        [OnDragCreate]
        public void CreateDragEvent() {}

    }

    [Test]
    public void DragCreate_CreateFromAnnotation() {
        MockView testView = new MockView(typeof(DragTestThing));
        testView.Initialize();
        testView.layoutSystem.SetViewportRect(new Rect(0, 0, 1000, 1000));
        DragTestThing root = (DragTestThing) testView.RootElement;
        root.ignoreChildDrag = true;
        testView.inputSystem.MouseDown(new Vector2(20, 20));
        testView.Update();

        Assert.IsNull(testView.inputSystem.CurrentDragEvent);

        testView.inputSystem.MouseDragMove(new Vector2(30, 30));
        testView.Update();

        Assert.IsInstanceOf<TestDragEvent>(testView.inputSystem.CurrentDragEvent);
        Assert.AreEqual("root", As<TestDragEvent>(testView.inputSystem.CurrentDragEvent).source);
    }
    
    [Test]
    public void DragCreate_CreateFromChildTemplate() {
        MockView testView = new MockView(typeof(DragTestThing));
        testView.Initialize();
        testView.layoutSystem.SetViewportRect(new Rect(0, 0, 1000, 1000));

        testView.inputSystem.MouseDown(new Vector2(20, 20));
        testView.Update();

        Assert.IsNull(testView.inputSystem.CurrentDragEvent);

        testView.inputSystem.MouseDragMove(new Vector2(25, 20));
        testView.Update();

        Assert.IsInstanceOf<TestDragEvent>(testView.inputSystem.CurrentDragEvent);
        Assert.AreEqual("child0", As<TestDragEvent>(testView.inputSystem.CurrentDragEvent).source);

    }
    
    [Test]
    public void DragCreate_MustReturnDragEvent() {
        var exception = Assert.Throws<Exception>(() => {
            new MockView(typeof(FailDragTestThing)).Initialize();
        });
        Assert.AreEqual($"Methods annotated with {nameof(OnDragCreateAttribute)} must return an instance of {nameof(DragEvent)}", exception.Message);
    }

    [Template(TemplateType.String, @"
    
        <UITemplate>
            <Contents style.layoutType='Fixed'>
                <Group onDragEnter='{HandleDragEnterChild(0)}' onDragExit='{HandleDragExitChild(0)}' style.translation='vec2(0,   0)' style.size='size(100, 100)'/>
                <Group onDragEnter='{HandleDragEnterChild(1)}' onDragExit='{HandleDragExitChild(1)}' style.translation='vec2(100, 0)' style.size='size(100, 100)'/>
                <Group onDragEnter='{HandleDragEnterChild(2)}' onDragExit='{HandleDragExitChild(2)}' style.translation='vec2(200, 0)' style.size='size(100, 100)'/>
            </Contents>
        </UITemplate>

    ")]
    public class DragHandlerTestThing : UIElement {

        public List<string> dragList = new List<string>();
        public bool ignoreEnter;
        public bool ignoreExit;
        
        public void HandleDragEnterChild(int index) {
            if (ignoreEnter) return;
            dragList.Add("enter:child" + index);
        }

        public void HandleDragExitChild(int index) {
            if (ignoreExit) return;
            dragList.Add("exit:child" + index);
        }

        [OnDragCreate]
        public TestDragEvent OnDragCreate() {
            return new TestDragEvent("root");
        }

    }

    [Test]
    public void DragEnter_Fires() {
        MockView testView = new MockView(typeof(DragHandlerTestThing));
        testView.Initialize();
        testView.layoutSystem.SetViewportRect(new Rect(0, 0, 1000, 1000));
        DragHandlerTestThing root = (DragHandlerTestThing) testView.RootElement;
        root.ignoreExit = true;
        testView.inputSystem.MouseDown(new Vector2(10, 10));
        testView.Update();

        testView.inputSystem.MouseDragMove(new Vector2(30, 30));
        testView.Update();
        
        Assert.AreEqual(new[] {"enter:child0"}, root.dragList.ToArray());
        
        testView.inputSystem.MouseDragMove(new Vector2(130, 30));
        testView.Update();
        
        Assert.AreEqual(new[] {"enter:child0", "enter:child1"}, root.dragList.ToArray());

    }

    [Test]
    public void DragEnter_DoesNotFireAgainForSamePosition() {
        MockView testView = new MockView(typeof(DragHandlerTestThing));
        testView.Initialize();
        testView.layoutSystem.SetViewportRect(new Rect(0, 0, 1000, 1000));
        DragHandlerTestThing root = (DragHandlerTestThing) testView.RootElement;
        root.ignoreExit = true;
        testView.inputSystem.MouseDown(new Vector2(10, 10));
        testView.Update();

        testView.inputSystem.MouseDragMove(new Vector2(30, 30));
        testView.Update();
        
        testView.inputSystem.MouseDragMove(new Vector2(30, 30));
        testView.Update();
        
        Assert.AreEqual(new[] {"enter:child0"}, root.dragList.ToArray());

    }
    
    [Test]
    public void DragEnter_DoesNotFireAgainForPositionSameElement() {
        MockView testView = new MockView(typeof(DragHandlerTestThing));
        testView.Initialize();
        testView.layoutSystem.SetViewportRect(new Rect(0, 0, 1000, 1000));
        DragHandlerTestThing root = (DragHandlerTestThing) testView.RootElement;
        root.ignoreExit = true;
        testView.inputSystem.MouseDown(new Vector2(10, 10));
        testView.Update();

        testView.inputSystem.MouseDragMove(new Vector2(30, 30));
        testView.Update();
        
        testView.inputSystem.MouseDragMove(new Vector2(30, 30));
        testView.Update();
        
        Assert.AreEqual(new[] {"enter:child0"}, root.dragList.ToArray());
    }

    [Test]
    public void DragEnter_FiresForNewElement() {
        MockView testView = new MockView(typeof(DragHandlerTestThing));
        testView.Initialize();
        testView.layoutSystem.SetViewportRect(new Rect(0, 0, 1000, 1000));
        DragHandlerTestThing root = (DragHandlerTestThing) testView.RootElement;
        root.ignoreExit = true;

        testView.inputSystem.MouseDown(new Vector2(10, 10));
        testView.Update();        
        
        testView.inputSystem.MouseDragMove(new Vector2(30, 30));
        testView.Update();
        
        testView.inputSystem.MouseDragMove(new Vector2(130, 30));
        testView.Update();
        
        Assert.AreEqual(new[] {"enter:child0", "enter:child1"}, root.dragList.ToArray());
    }

    [Test]
    public void DragEnter_FiresForReEnteringElement() {
        MockView testView = new MockView(typeof(DragHandlerTestThing));
        testView.Initialize();
        testView.layoutSystem.SetViewportRect(new Rect(0, 0, 1000, 1000));
        DragHandlerTestThing root = (DragHandlerTestThing) testView.RootElement;
        root.ignoreExit = true;

        testView.inputSystem.MouseDown(new Vector2(10, 10));
        testView.Update();        
        
        testView.inputSystem.MouseDragMove(new Vector2(30, 30));
        testView.Update();
        
        testView.inputSystem.MouseDragMove(new Vector2(130, 30));
        testView.Update();
        
        testView.inputSystem.MouseDragMove(new Vector2(30, 30));
        testView.Update();
        
        Assert.AreEqual(new[] {"enter:child0", "enter:child1", "enter:child0"}, root.dragList.ToArray());
    }
    
    [Test]
    public void DragExit_FiresAndPropagates() {
        MockView testView = new MockView(typeof(DragHandlerTestThing));
        testView.Initialize();
        testView.layoutSystem.SetViewportRect(new Rect(0, 0, 1000, 1000));
        DragHandlerTestThing root = (DragHandlerTestThing) testView.RootElement;
        
        testView.inputSystem.MouseDown(new Vector2(10, 10));
        testView.Update();

        testView.inputSystem.MouseDragMove(new Vector2(30, 30));
        testView.Update();
        
        Assert.AreEqual(new[] {"enter:child0"}, root.dragList.ToArray());
        
        testView.inputSystem.MouseDragMove(new Vector2(130, 30));
        testView.Update();
        
        Assert.AreEqual(new[] {"enter:child0", "exit:child0", "enter:child1"}, root.dragList.ToArray());
    }

    [Test]
    public void DragExit_FireOnlyForExitedElement() {
        MockView testView = new MockView(typeof(DragHandlerTestThing));
        testView.Initialize();
        testView.layoutSystem.SetViewportRect(new Rect(0, 0, 1000, 1000));
        DragHandlerTestThing root = (DragHandlerTestThing) testView.RootElement;
        
        testView.inputSystem.MouseDown(new Vector2(10, 10));
        testView.Update();

        testView.inputSystem.MouseDragMove(new Vector2(30, 30));
        testView.Update();
        
        Assert.AreEqual(new[] {"enter:child0"}, root.dragList.ToArray());
        
        testView.inputSystem.MouseDragMove(new Vector2(40, 30));
        testView.Update();
        
        Assert.AreEqual(new[] {"enter:child0"}, root.dragList.ToArray());
    }

    [Test]
    public void DragExit_FireAgainWhenReenteredElement() {
        MockView testView = new MockView(typeof(DragHandlerTestThing));
        testView.Initialize();
        testView.layoutSystem.SetViewportRect(new Rect(0, 0, 1000, 1000));
        DragHandlerTestThing root = (DragHandlerTestThing) testView.RootElement;
        
        testView.inputSystem.MouseDown(new Vector2(10, 10));
        testView.Update();

        testView.inputSystem.MouseDragMove(new Vector2(30, 30));
        testView.Update();
                
        testView.inputSystem.MouseDragMove(new Vector2(130, 30));
        testView.Update();
        
        testView.inputSystem.MouseDragMove(new Vector2(30, 30));
        testView.Update();
        
        Assert.AreEqual(new[] {"enter:child0", "exit:child0", "enter:child1", "exit:child1", "enter:child0"}, root.dragList.ToArray());
    }

    [Template(TemplateType.String, @"
    
        <UITemplate>
            <Contents style.layoutType='Fixed'>
                <Group onDragMove='{HandleDragMoveChild(0)}' onDragHover='{HandleDragHoverChild(0)}' style.translation='vec2(0,   0)' style.size='size(100, 100)'/>
                <Group onDragMove='{HandleDragMoveChild(1)}' onDragHover='{HandleDragHoverChild(1)}' style.translation='vec2(100, 0)' style.size='size(100, 100)'/>
                <Group onDragMove='{HandleDragMoveChild(2)}' onDragHover='{HandleDragHoverChild(2)}' style.translation='vec2(200, 0)' style.size='size(100, 100)'/>
            </Contents>
        </UITemplate>

    ")]
    public class DragHandlerTestThing_Move : UIElement {

        public List<string> dragList = new List<string>();

        public void HandleDragMoveChild(int index) {
            dragList.Add("move:child" + index);
        }

        public void HandleDragHoverChild(int index) {
            dragList.Add("hover:child" + index);
        }
        
        [OnDragCreate]
        public TestDragEvent OnDragCreate() {
            return new TestDragEvent("root");
        }

    }

    [Test]
    public void DragMove_FiresAndPropagates() {
        MockView testView = new MockView(typeof(DragHandlerTestThing_Move));
        testView.Initialize();
        testView.layoutSystem.SetViewportRect(new Rect(0, 0, 1000, 1000));
        DragHandlerTestThing_Move root = (DragHandlerTestThing_Move) testView.RootElement;

        testView.inputSystem.MouseDown(new Vector2(10, 10));
        testView.Update();
        
        testView.inputSystem.MouseDragMove(new Vector2(30, 30));
        testView.Update();

        Assert.AreEqual(new string[0], root.dragList.ToArray());
        
        testView.inputSystem.MouseDragMove(new Vector2(30, 20));
        testView.Update();
        
        Assert.AreEqual(new[] {"move:child0"}, root.dragList.ToArray());
    }

    [Test]
    public void DragMove_FiresAgainWhenMovedAndContains() {
        MockView testView = new MockView(typeof(DragHandlerTestThing_Move));
        testView.Initialize();
        testView.layoutSystem.SetViewportRect(new Rect(0, 0, 1000, 1000));
        DragHandlerTestThing_Move root = (DragHandlerTestThing_Move) testView.RootElement;

        testView.inputSystem.MouseDown(new Vector2(10, 10));
        testView.Update();
        
        testView.inputSystem.MouseDragMove(new Vector2(30, 30));
        testView.Update();
        
        testView.inputSystem.MouseDragMove(new Vector2(30, 20));
        testView.Update();
        
        testView.inputSystem.MouseDragMove(new Vector2(31, 20));
        testView.Update();
        
        Assert.AreEqual(new[] {"move:child0", "move:child0"}, root.dragList.ToArray());
    }

    [Test]
    public void DragMove_DoesNotFireAgainWhenNotMovedAndContains() {
        MockView testView = new MockView(typeof(DragHandlerTestThing_Move));
        testView.Initialize();
        testView.layoutSystem.SetViewportRect(new Rect(0, 0, 1000, 1000));
        DragHandlerTestThing_Move root = (DragHandlerTestThing_Move) testView.RootElement;

        testView.inputSystem.MouseDown(new Vector2(10, 10));
        testView.Update();
        
        testView.inputSystem.MouseDragMove(new Vector2(30, 30));
        testView.Update();
        
        testView.inputSystem.MouseDragMove(new Vector2(31, 20));
        testView.Update();
        
        testView.inputSystem.MouseDragMove(new Vector2(31, 20));
        testView.Update();
        
        Assert.AreEqual(new[] {"move:child0", "hover:child0"}, root.dragList.ToArray());
    }

}