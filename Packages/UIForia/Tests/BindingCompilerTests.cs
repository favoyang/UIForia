﻿using NUnit.Framework;
using UIForia;
using UIForia.Compilers;
using Tests;
using static Tests.TestUtils;

[TestFixture]
public class BindingCompilerTests {

    [Test]
    public void CreatesBinding_FieldSetter() {
        ContextDefinition context = new ContextDefinition(typeof(TestUIElementType));
        PropertyBindingCompiler compiler = new PropertyBindingCompiler(context);
        Binding binding = compiler.CompileAttribute(typeof(TestUtils.TestUIElementType), new AttributeDefinition("intValue", "{1 + 1}"));
        Assert.IsNotNull(binding);
    }

    [Test]
    public void CreatesBinding_Event_0Args() {
        // <RootElement>
        //     <FakeElement onSomething="{HandleSomething($event)}"/>
        // </RootElement>

        ContextDefinition context = new ContextDefinition(typeof(FakeRootElement));
        PropertyBindingCompiler compiler = new PropertyBindingCompiler(context);
        FakeRootElement rootElement = new FakeRootElement();
        FakeElement childElement = new FakeElement();
        UITemplateContext ctx = new UITemplateContext(null);

        ctx.rootElement = rootElement;

        AttributeDefinition attrDef = new AttributeDefinition("onSomeEventArg0", "{HandleSomeEventArg0()}");

        Binding binding = compiler.CompileAttribute(typeof(FakeElement), attrDef);

        binding.Execute(childElement, ctx);

        childElement.InvokeEvtArg0();
        Assert.AreEqual(1, rootElement.arg0CallCount);
    }
    
    [Test]
    public void CreatesBinding_Event_1Args() {
        // <RootElement>
        //     <FakeElement onSomething="{HandleSomething($event)}"/>
        // </RootElement>

        ContextDefinition context = new ContextDefinition(typeof(FakeRootElement));
        PropertyBindingCompiler compiler = new PropertyBindingCompiler(context);
        FakeRootElement rootElement = new FakeRootElement();
        FakeElement childElement = new FakeElement();
        UITemplateContext ctx = new UITemplateContext(null);

        ctx.rootElement = rootElement;

        AttributeDefinition attrDef = new AttributeDefinition("onSomeEventArg1", "{HandleSomeEventArg1($eventArg0)}");

        Binding binding = compiler.CompileAttribute(typeof(FakeElement), attrDef);

        binding.Execute(childElement, ctx);

        childElement.InvokeEvtArg1("hello");
        Assert.AreEqual(new [] { "hello"}, rootElement.arg1Params);
    }
    
    [Test]
    public void CreatesBinding_Event_2Args() {
        // <RootElement>
        //     <FakeElement onSomething="{HandleSomething($event)}"/>
        // </RootElement>

        ContextDefinition context = new ContextDefinition(typeof(FakeRootElement));
        PropertyBindingCompiler compiler = new PropertyBindingCompiler(context);
        FakeRootElement rootElement = new FakeRootElement();
        FakeElement childElement = new FakeElement();
        UITemplateContext ctx = new UITemplateContext(null);

        ctx.rootElement = rootElement;

        AttributeDefinition attrDef = new AttributeDefinition("onSomeEventArg2", "{HandleSomeEventArg2($eventArg0, $eventArg1)}");

        Binding binding = compiler.CompileAttribute(typeof(FakeElement), attrDef);

        binding.Execute(childElement, ctx);

        childElement.InvokeEvtArg2("hello", "there");
        Assert.AreEqual(new [] { "hello", "there"}, rootElement.arg2Params);
    }
    
    [Test]
    public void CreatesBinding_Event_3Args() {
        // <RootElement>
        //     <FakeElement onSomething="{HandleSomething($event)}"/>
        // </RootElement>

        ContextDefinition context = new ContextDefinition(typeof(FakeRootElement));
        PropertyBindingCompiler compiler = new PropertyBindingCompiler(context);
        FakeRootElement rootElement = new FakeRootElement();
        FakeElement childElement = new FakeElement();
        UITemplateContext ctx = new UITemplateContext(null);

        ctx.rootElement = rootElement;

        AttributeDefinition attrDef = new AttributeDefinition("onSomeEventArg3", "{HandleSomeEventArg3($eventArg0, $eventArg1, $eventArg2)}");

        Binding binding = compiler.CompileAttribute(typeof(FakeElement), attrDef);

        binding.Execute(childElement, ctx);

        childElement.InvokeEvtArg3("hello", "there", "buddy");
        Assert.AreEqual(new [] { "hello", "there", "buddy"}, rootElement.arg3Params);
    }
    
    [Test]
    public void CreatesBinding_Event_4Args() {
        // <RootElement>
        //     <FakeElement onSomething="{HandleSomething($event)}"/>
        // </RootElement>

        ContextDefinition context = new ContextDefinition(typeof(FakeRootElement));
        PropertyBindingCompiler compiler = new PropertyBindingCompiler(context);
        FakeRootElement rootElement = new FakeRootElement();
        FakeElement childElement = new FakeElement();
        UITemplateContext ctx = new UITemplateContext(null);

        ctx.rootElement = rootElement;

        AttributeDefinition attrDef = new AttributeDefinition("onSomeEventArg4", "{HandleSomeEventArg4($eventArg0, $eventArg1, $eventArg2, $eventArg3)}");

        Binding binding = compiler.CompileAttribute(typeof(FakeElement), attrDef);

        binding.Execute(childElement, ctx);

        childElement.InvokeEvtArg4("hello", "there", "buddy", "boy");
        Assert.AreEqual(new [] { "hello", "there", "buddy", "boy"}, rootElement.arg4Params);
    }
    
    public class TestedThing1 : UIElement {

        public string prop0;
        public bool didProp0Change;
        
        [OnPropertyChanged(nameof(prop0))]
        public void OnProp0Changed(string prop) {
            didProp0Change = true;
        }

    }

    [Test]
    public void OnPropertyChanged() {
        AttributeDefinition attrDef = new AttributeDefinition("prop0", "'some-string'");
        PropertyBindingCompiler c = new PropertyBindingCompiler(new ContextDefinition(typeof(TestedThing1)));
        Binding b = c.CompileAttribute(typeof(TestedThing1), attrDef);
        Assert.IsInstanceOf<FieldSetterBinding_WithCallbacks<TestedThing1, string>>(b);
        TestedThing1 t = new TestedThing1();
        Assert.IsFalse(t.didProp0Change);
        Assert.AreEqual(t.prop0, null);
        b.Execute(t, new UITemplateContext(null));
        Assert.AreEqual(t.prop0, "some-string");
        Assert.IsTrue(t.didProp0Change);
        t.didProp0Change = false;
        b.Execute(t, new UITemplateContext(null));
        Assert.AreEqual(t.prop0, "some-string");
        Assert.IsFalse(t.didProp0Change);
    }

}