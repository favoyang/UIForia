using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Tests.Mocks;
using UIForia;
using UIForia.Attributes;
using UIForia.Elements;
using UIForia.Exceptions;
using UIForia.Parsing;
using UIForia.Parsing.Expressions.AstNodes;
using UIForia.Util;
using UnityEngine;
using Application = UnityEngine.Application;

namespace TemplateParsing_XML {

    public class TemplateParsing_XMLTests {

        public TemplateSettings Setup(string appName) {
            TemplateSettings settings = new TemplateSettings();
            settings.applicationName = appName;
            settings.assemblyName = GetType().Assembly.GetName().Name;
            settings.outputPath = Path.Combine(Application.dataPath, "..", "Packages", "UIForia", "Tests", "UIForiaGenerated");
            settings.codeFileExtension = "cs";
            settings.preCompiledTemplatePath = "Assets/UIForia_Generated/" + appName;
            settings.templateResolutionBasePath = Path.Combine(Application.dataPath, "..", "Packages", "UIForia", "Tests");
            return settings;
        }

        [Template("Data/XMLTemplateParsing/XMLTemplateParsing_CollapseTextNode_Simple.xml")]
        public class XMLTemplateParsing_CollapseTextNode_Simple : UIElement { }

        [Test]
        public void CollapseTextNode_Simple() {
            TemplateCache cache = new TemplateCache(Setup("App"));
            TemplateRootNode templateRootRoot = cache.GetParsedTemplate(TypeProcessor.GetProcessedType(typeof(XMLTemplateParsing_CollapseTextNode_Simple)));

            Assert.AreEqual(1, templateRootRoot.ChildCount);

            ContainerNode child = AssertAndReturn<ContainerNode>(templateRootRoot[0]);
            TextNode text = AssertAndReturn<TextNode>(child[0]);

            Assert.AreEqual("Hello Templates", text.textExpressionList[0].text);
        }

        [Template("Data/XMLTemplateParsing/XMLTemplateParsing_CollapseTextNode_Complex.xml")]
        public class XMLTemplateParsing_CollapseTextNode_Complex : UIElement { }

        [Test]
        public void CollapseTextNode_Complex() {
            TemplateCache cache = new TemplateCache(Setup("App"));
            TemplateRootNode templateRootRoot = cache.GetParsedTemplate(TypeProcessor.GetProcessedType(typeof(XMLTemplateParsing_CollapseTextNode_Complex)));

            Assert.AreEqual(1, templateRootRoot.ChildCount);

            ContainerNode child = AssertAndReturn<ContainerNode>(templateRootRoot[0]);
            TextNode text = AssertAndReturn<TextNode>(child[0]);

            Assert.AreEqual(2, text.ChildCount);
            Assert.AreEqual("Hello", text.textExpressionList[0].text.Trim());

            ContainerNode terminalNode = AssertAndReturn<ContainerNode>(text[0]);
            TextNode subText = AssertAndReturn<TextNode>(text[1]);
            Assert.AreEqual("Templates", subText.textExpressionList[0].text.Trim());
        }

        [Template("Data/XMLTemplateParsing/XMLTemplateParsing_DefineSlot.xml")]
        public class XMLTemplateParsing_DefineSlot : UIElement { }

        [Test]
        public void DefineSlot() {
            TemplateCache cache = new TemplateCache(Setup("App"));
            TemplateRootNode templateRootRoot = cache.GetParsedTemplate(TypeProcessor.GetProcessedType(typeof(XMLTemplateParsing_DefineSlot)));

            Assert.AreEqual(1, templateRootRoot.ChildCount);

            ContainerNode child = AssertAndReturn<ContainerNode>("Div", templateRootRoot[0]);
            SlotNode node = AssertAndReturn<SlotNode>(child[0]);

            Assert.AreEqual("my-slot", node.slotName);
        }

        [Template("Data/XMLTemplateParsing/XMLTemplateParsing_DefineSlotNameTwice.xml")]
        public class XMLTemplateParsing_DefineSlotNameTwice : UIElement { }

        // [Test]
        // public void DefineSlotNameTwice() {
        //     ProcessedType processedType = TypeProcessor.GetProcessedType(typeof(XMLTemplateParsing_DefineSlotNameTwice));
        //     TemplateCache cache = new TemplateCache(Setup("App"));
        //     ParseException parseException = Assert.Throws<ParseException>(() => { cache.GetParsedTemplate(processedType); });
        //     Assert.AreEqual(ParseException.MultipleSlotsWithSameName(processedType.templateAttr.template, "my-slot").Message, parseException.Message);
        // }

        [Test]
        public void DefineSlotNameTwice() {
            ProcessedType processedType = TypeProcessor.GetProcessedType(typeof(XMLTemplateParsing_DefineSlotNameTwice));
            TemplateCache cache = new TemplateCache(Setup("App"));
            Assert.DoesNotThrow(() => { cache.GetParsedTemplate(processedType); });
        }

        [Template("Data/XMLTemplateParsing/XMLTemplateParsing_OverrideSlot.xml")]
        public class XMLTemplateParsing_OverrideSlot : UIElement { }

        [Test]
        public void OverrideSlot() {
            ProcessedType processedType = TypeProcessor.GetProcessedType(typeof(XMLTemplateParsing_OverrideSlot));
            TemplateCache cache = new TemplateCache(Setup("App"));
            TemplateRootNode templateRootRoot = cache.GetParsedTemplate(processedType);

            Assert.AreEqual(3, templateRootRoot.ChildCount);

            AssertTrimmedText("Hello Before", templateRootRoot[0]);
            AssertTrimmedText("Hello After", templateRootRoot[2]);

            ExpandedTemplateNode expandedTemplateNode = AssertAndReturn<ExpandedTemplateNode>(templateRootRoot[1]);
            SlotNode overrideNode = AssertAndReturn<SlotNode>(expandedTemplateNode.slotOverrideNodes[0]);
            Assert.AreEqual("my-slot", overrideNode.slotName);
            Assert.AreEqual(SlotType.Override, overrideNode.slotType);
            AssertTrimmedText("Hello Between", overrideNode[0]);
        }

        [Template("Data/XMLTemplateParsing/XMLTemplateParsing_ExpandTemplate.xml")]
        public class XMLTemplateParsing_ExpandTemplate : UIElement { }

        [Template("Data/XMLTemplateParsing/XMLTemplateParsing_ExpandedTemplateChild.xml")]
        public class XMLTemplateParsing_ExpandedTemplateChild : UIElement { }

        [Test]
        public void ExpandedTemplate() {
            ProcessedType processedType = TypeProcessor.GetProcessedType(typeof(XMLTemplateParsing_ExpandTemplate));
            TemplateCache cache = new TemplateCache(Setup("App"));
            TemplateRootNode templateRootRoot = cache.GetParsedTemplate(processedType);

            Assert.AreEqual(3, templateRootRoot.ChildCount);

            AssertAndReturn<TextNode>(templateRootRoot[0]);

            ExpandedTemplateNode expandedTemplate = AssertAndReturn<ExpandedTemplateNode>(templateRootRoot[1]);

            Assert.AreEqual(typeof(XMLTemplateParsing_ExpandedTemplateChild), expandedTemplate.processedType.rawType);

            AssertAndReturn<TextNode>(templateRootRoot[2]);
        }

        [Template("Data/XMLTemplateParsing/XMLTemplateParsing_Namespaces.xml")]
        public class XMLTemplateParsing_Namespace : UIElement { }

        [Test]
        public void ParseNamespace() {
            ProcessedType processedType = TypeProcessor.GetProcessedType(typeof(XMLTemplateParsing_Namespace));
            TemplateCache cache = new TemplateCache(Setup("App"));
            TemplateRootNode templateRoot = cache.GetParsedTemplate(processedType);

            Assert.AreEqual(typeof(UIDivElement), templateRoot.children[0].processedType.rawType);
        }

        [Template("Data/XMLTemplateParsing/XMLTemplateParsing_Namespaces.xml#unknown")]
        public class XMLTemplateParsing_Namespace_Unknown : UIElement { }

        [Test]
        public void ParseNamespace_NotThere() {
            ParseException exception = Assert.Throws<ParseException>(() => {
                ProcessedType processedType = TypeProcessor.GetProcessedType(typeof(XMLTemplateParsing_Namespace_Unknown));
                TemplateCache cache = new TemplateCache(Setup("App"));
                cache.GetParsedTemplate(processedType);
            });
            Assert.IsTrue(exception.Message.Contains(ParseException.UnresolvedTagName("Data/XMLTemplateParsing/XMLTemplateParsing_Namespaces.xml", new TemplateLineInfo(11, 10), "NotHere:Div").Message));
        }

        [Template("Data/XMLTemplateParsing/XMLTemplateParsing_UseDynamicElement.xml")]
        public class XMLTemplateParsing_UseDynamicElement : UIElement { }

        [Test]
        public void ParseDynamicElement() {
            MockApplication app = MockApplication.Setup<XMLTemplateParsing_UseDynamicElement>();
            XMLTemplateParsing_UseDynamicElement e = (XMLTemplateParsing_UseDynamicElement) app.RootElement;

            app.Update();

            Assert.AreEqual("Hello Matt!", GetText(e[0][0]));
        }


        [Template("Data/XMLTemplateParsing/XMLTemplateParsing_UseDynamicElement.xml#generic_main")]
        public class XMLTemplateParsing_UseDynamicElement_Generic : UIElement {

            public Vector2 vector;

        }

        [Test]
        public void ParseDynamicGenericElement() {
            MockApplication app = MockApplication.Setup<XMLTemplateParsing_UseDynamicElement_Generic>();
            XMLTemplateParsing_UseDynamicElement_Generic e = (XMLTemplateParsing_UseDynamicElement_Generic) app.RootElement;

            app.Update();

            Assert.AreEqual(typeof(float), e[0].GetType().GetGenericArguments()[0]);
            Assert.AreEqual(typeof(int), e[0].GetType().GetGenericArguments()[1]);

            Assert.AreEqual(typeof(int), e[1].GetType().GetGenericArguments()[0]);
            Assert.AreEqual(typeof(string), e[1].GetType().GetGenericArguments()[1]);

            Assert.AreEqual(typeof(string), e[2].GetType().GetGenericArguments()[0]);
            Assert.AreEqual(typeof(Vector2), e[2].GetType().GetGenericArguments()[1]);
        }

        [Template("Data/XMLTemplateParsing/XMLTemplateParsing_UsingElement.xml")]
        public class XMLTemplateParsing_UsingElement : UIElement { }

        [Test]
        public void ParseUsingElement() {
            MockApplication app = MockApplication.Setup<XMLTemplateParsing_UsingElement>();
            XMLTemplateParsing_UsingElement e = (XMLTemplateParsing_UsingElement) app.RootElement;

            app.Update();

            Assert.AreEqual("Hello 1!", GetText(e[0][0]));
            Assert.AreEqual("Hello 2!", GetText(e[1][0]));
            Assert.AreEqual("Hello Same File 3!", GetText(e[2][0]));
            Assert.AreEqual("Hello Same File 4!", GetText(e[3][0]));
        }

        [Test]
        public void GenerateTypeSkeleton() {
            ClassBuilder builder = new ClassBuilder();
            Type type = builder.CreateRuntimeType("SkeletonTest", typeof(object),
                new List<ReflectionUtil.FieldDefinition>() { },
                new List<ReflectionUtil.MethodDefinition>() {
                    new ReflectionUtil.MethodDefinition() {
                        methodName = "InstanceMethod0",
                        returnType = new TypeLookup(typeof(string)),
                        arguments = new LambdaArgument[0],
                        // body = 
                    }
                },
                null);
            
            
        }


        public static string GetText(UIElement element) {
            UITextElement textEl = element as UITextElement;
            return textEl.text.Trim();
        }

        private static void AssertText(string expected, TemplateNode templateNode) {
            TextNode textNode = AssertAndReturn<TextNode>(templateNode);
            Assert.AreEqual(expected, textNode.rawTextContent);
        }

        private static void AssertTrimmedText(string expected, TemplateNode templateNode) {
            TextNode textNode = AssertAndReturn<TextNode>(templateNode);
            Assert.AreEqual(expected, textNode.rawTextContent.Trim());
        }

        private static T AssertAndReturn<T>(object b) where T : TemplateNode {
            Assert.IsInstanceOf<T>(b);
            return (T) b;
        }

        private static T AssertAndReturn<T>(string tagName, object b) where T : TemplateNode {
            Assert.IsInstanceOf<T>(b);
            T a = (T) b;
            if (!string.IsNullOrEmpty(a.namespaceName)) {
                Assert.AreEqual(tagName, a.namespaceName + ":" + a.tagName);
            }
            else {
                Assert.AreEqual(tagName, a.tagName);
            }

            return a;
        }

    }

}