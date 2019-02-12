using System;
using System.Collections.Generic;
using System.Text;
using UIForia.Util;
using UnityEngine;

namespace UIForia.Style.Parsing {

    public abstract class StyleASTNode {

        protected static readonly ObjectPool<LiteralNode> s_LiteralPool = new ObjectPool<LiteralNode>();
        protected static readonly ObjectPool<StyleOperatorNode> s_OperatorPool = new ObjectPool<StyleOperatorNode>();
        protected static readonly ObjectPool<IdentifierNode> s_IdentifierPool = new ObjectPool<IdentifierNode>();
        protected static readonly ObjectPool<ParenNode> s_ParenPool = new ObjectPool<ParenNode>();
        protected static readonly ObjectPool<DotAccessNode> s_DotAccessPool = new ObjectPool<DotAccessNode>();
        protected static readonly ObjectPool<MemberAccessExpressionNode> s_MemberAccessExpressionPool = new ObjectPool<MemberAccessExpressionNode>();
        protected static readonly ObjectPool<IndexNode> s_IndexExpressionPool = new ObjectPool<IndexNode>();
        protected static readonly ObjectPool<InvokeNode> s_InvokeNodePool = new ObjectPool<InvokeNode>();
        protected static readonly ObjectPool<UnaryExpressionNode> s_UnaryNodePool = new ObjectPool<UnaryExpressionNode>();
        protected static readonly ObjectPool<ImportNode> s_ImportNodePool = new ObjectPool<ImportNode>();
        protected static readonly ObjectPool<PropertyNode> s_PropertyNodePool = new ObjectPool<PropertyNode>();
        protected static readonly ObjectPool<StyleContainer> s_StyleContainerNodePool = new ObjectPool<StyleContainer>();
        protected static readonly ObjectPool<StyleRootNode> s_StyleRootNodePool = new ObjectPool<StyleRootNode>();
        protected static readonly ObjectPool<AttributeGroupContainer> s_AttributeGroupContainerNodePool = new ObjectPool<AttributeGroupContainer>();
        protected static readonly ObjectPool<GroupSpecifierNode> s_GroupSpecifierNodePool = new ObjectPool<GroupSpecifierNode>();
        protected static readonly ObjectPool<UnitNode> s_UnitNodePool = new ObjectPool<UnitNode>();
        protected static readonly ObjectPool<ReferenceNode> s_ReferenceNodePool = new ObjectPool<ReferenceNode>();
        protected static readonly ObjectPool<RgbaNode> s_RgbaNodePool = new ObjectPool<RgbaNode>();
        protected static readonly ObjectPool<RgbNode> s_RgbNodePool = new ObjectPool<RgbNode>();
        protected static readonly ObjectPool<UrlNode> s_UrlNodePool = new ObjectPool<UrlNode>();
        protected static readonly ObjectPool<ExportNode> s_ExportNodePool = new ObjectPool<ExportNode>();

        public StyleASTNodeType type;

        public bool IsCompound {
            get {
                if (type == StyleASTNodeType.Operator) {
                    return true;
                }

                return false;
            }
        }
        
        public virtual void AddChildNode(StyleASTNode child) {
            throw new NotImplementedException();
        }

        public abstract void Release();

        internal static StyleRootNode StyleRootNode(string identifier, string tagName) {
            StyleRootNode rootNode = s_StyleRootNodePool.Get();
            rootNode.identifier = identifier;
            rootNode.tagName = tagName;
            return rootNode;
        }

        internal static AttributeGroupContainer AttributeGroupRootNode(string identifier, string value) {
            AttributeGroupContainer rootNode = s_AttributeGroupContainerNodePool.Get();
            rootNode.type = StyleASTNodeType.AttributeGroup;
            rootNode.identifier = identifier;
            rootNode.value = value;
            return rootNode;
        }

        internal static StyleContainer StateGroupRootNode(string identifier) {
            StyleContainer rootNode = s_StyleContainerNodePool.Get();
            rootNode.type = StyleASTNodeType.StateGroup;
            rootNode.identifier = identifier;
            return rootNode;
        }

        internal static StyleContainer ExpressionGroupRootNode(string identifier) {
            StyleContainer rootNode = s_StyleContainerNodePool.Get();
            rootNode.type = StyleASTNodeType.ExpressionGroup;
            rootNode.identifier = identifier;
            return rootNode;
        }

        internal static PropertyNode PropertyNode(string propertyName, LightList<StyleASTNode> propertyValueParts) {
            PropertyNode propertyNode = s_PropertyNodePool.Get();
            propertyNode.propertyName = propertyName;
            propertyNode.propertyValueParts = propertyValueParts;
            return propertyNode;
        }

        internal static GroupSpecifierNode GroupSpecifierNode(UIForia.Rendering.GroupOperatorType groupOperatorType) {
            GroupSpecifierNode groupNode = s_GroupSpecifierNodePool.Get();
            groupNode.groupOperatorType = groupOperatorType;
            return groupNode;
        }

        internal static ImportNode ImportNode() {
            ImportNode importNode = s_ImportNodePool.Get();
            return importNode;
        }
        

        internal static ReferenceNode ReferenceNode(string value) {
            ReferenceNode referenceNode = s_ReferenceNodePool.Get();
            referenceNode.referenceName = value;
            return referenceNode;
        }

        public static LiteralNode StringLiteralNode(string value) {
            LiteralNode retn = s_LiteralPool.Get();
            retn.type = StyleASTNodeType.StringLiteral;
            retn.rawValue = value;
            return retn;
        }

        public static LiteralNode BooleanLiteralNode(string value) {
            LiteralNode retn = s_LiteralPool.Get();
            retn.type = StyleASTNodeType.BooleanLiteral;
            retn.rawValue = value;
            return retn;
        }

        public static RgbaNode RgbaNode(StyleASTNode red, StyleASTNode green, StyleASTNode blue, StyleASTNode alpha) {
            RgbaNode retn = s_RgbaNodePool.Get();
            retn.red = red;
            retn.green = green;
            retn.blue = blue;
            retn.alpha = alpha;
            return retn;
        }

        public static RgbNode RgbNode(StyleASTNode red, StyleASTNode green, StyleASTNode blue) {
            RgbNode retn = s_RgbNodePool.Get();
            retn.red = red;
            retn.green = green;
            retn.blue = blue;
            return retn;
        }

        public static UrlNode UrlNode(StyleASTNode url) {
            UrlNode retn = s_UrlNodePool.Get();
            retn.url = url;
            return retn;
        }

        public static UnitNode UnitNode(string value) {
            UnitNode retn = s_UnitNodePool.Get();
            retn.value = value;
            return retn;
        }

        public static LiteralNode NumericLiteralNode(string value) {
            LiteralNode retn = s_LiteralPool.Get();
            retn.type = StyleASTNodeType.NumericLiteral;
            retn.rawValue = value;
            return retn;
        }

        public static StyleOperatorNode OperatorNode(StyleOperatorType operatorType) {
            StyleOperatorNode operatorNode = s_OperatorPool.Get();
            operatorNode.type = StyleASTNodeType.Operator;
            operatorNode.operatorType = operatorType;
            return operatorNode;
        }

        public static IdentifierNode IdentifierNode(string name) {
            IdentifierNode idNode = s_IdentifierPool.Get();
            idNode.name = name;
            idNode.type = StyleASTNodeType.Identifier;
            return idNode;
        }

        public static ParenNode ParenNode(StyleASTNode expression) {
            ParenNode parenNode = s_ParenPool.Get();
            parenNode.expression = expression;
            return parenNode;
        }

        public static DotAccessNode DotAccessNode(string propertyName) {
            DotAccessNode dotAccessNode = s_DotAccessPool.Get();
            dotAccessNode.propertyName = propertyName;
            return dotAccessNode;
        }

        public static InvokeNode InvokeNode(List<StyleASTNode> parameters) {
            InvokeNode invokeNode = s_InvokeNodePool.Get();
            invokeNode.parameters = parameters;
            return invokeNode;
        }

        public static MemberAccessExpressionNode MemberAccessExpressionNode(string identifier, List<StyleASTNode> parts) {
            MemberAccessExpressionNode accessExpressionNode = s_MemberAccessExpressionPool.Get();
            accessExpressionNode.identifier = identifier;
            accessExpressionNode.parts = parts;
            return accessExpressionNode;
        }

        public static IndexNode IndexExpressionNode(StyleASTNode expression) {
            IndexNode indexNode = s_IndexExpressionPool.Get();
            indexNode.expression = expression;
            return indexNode;
        }

        public static UnaryExpressionNode UnaryExpressionNode(StyleASTNodeType nodeType, StyleASTNode expr) {
            UnaryExpressionNode unaryNode = s_UnaryNodePool.Get();
            unaryNode.type = nodeType;
            unaryNode.expression = expr;
            return unaryNode;
        }
    }

    public struct TypePath {

        public List<string> path;
        public List<TypePath> genericArguments;

        public void Release() {
            ListPool<string>.Release(ref path);
            ReleaseGenerics();
        }

        public void ReleaseGenerics() {
            if (genericArguments != null && genericArguments.Count > 0) {
                for (int i = 0; i < genericArguments.Count; i++) {
                    genericArguments[i].Release();
                }

                ListPool<TypePath>.Release(ref genericArguments);
                genericArguments = null;
            }
        }

        private static readonly StringBuilder s_Builder = new StringBuilder(128);

        private void GetConstructedPathStep() {
            for (int i = 0; i < path.Count - 1; i++) {
                s_Builder.Append(path[i]);
                s_Builder.Append('.');
            }

            s_Builder.Append(path[path.Count - 1]);
            if (genericArguments != null && genericArguments.Count > 0) {
                s_Builder.Append('`');
                s_Builder.Append(genericArguments.Count);
                s_Builder.Append('[');
                for (int i = 0; i < genericArguments.Count; i++) {
                    genericArguments[i].GetConstructedPathStep();
                    if (i != genericArguments.Count - 1) {
                        s_Builder.Append(',');
                    }
                }

                s_Builder.Append(']');
            }
        }

        public string GetConstructedPath() {
            if (path == null) {
                return string.Empty;
            }

            GetConstructedPathStep();
            string retn = s_Builder.ToString();
            s_Builder.Clear();
            return retn;
        }

    }

    public class ImportNode : StyleASTNode {

        public string alias;
        public string importedProperty;
        public string source;

        public override void Release() {
            s_ImportNodePool.Release(this);
        }
    }

    /// <summary>
    /// Container for all the things inside a style node: 'style xy { children... }'
    /// </summary>
    public class StyleRootNode : StyleASTNode {
        public string identifier;
        public string tagName;
        public LightList<StyleASTNode> children { get; private set; }

        public StyleRootNode() {
            this.children = LightListPool<StyleASTNode>.Get();
        }

        public override void AddChildNode(StyleASTNode child) {
            children.Add(child);
        }

        public override void Release() {
            for (int index = 0; index < children.Count; index++) {
                StyleASTNode child = children[index];
                child.Release();
            }

            children.Clear();
            children = null;
            s_StyleRootNodePool.Release(this);
        }
    }

    public class AttributeGroupContainer : StyleASTNode {
        
        public string identifier;
        public string value;
        
        public LightList<StyleASTNode> children { get; private set; }

        public AttributeGroupContainer() {
            this.children = LightListPool<StyleASTNode>.Get();
        }

        public override void AddChildNode(StyleASTNode child) {
            children.Add(child);
        }

        public override void Release() {
            for (int index = 0; index < children.Count; index++) {
                StyleASTNode child = children[index];
                child.Release();
            }

            children.Clear();
            children = null;
            s_AttributeGroupContainerNodePool.Release(this);
        }
    }
    
    public class StyleContainer : StyleASTNode {
        public string identifier;
        public LightList<StyleASTNode> children { get; private set; }

        public StyleContainer() {
            this.children = LightListPool<StyleASTNode>.Get();
        }

        public override void AddChildNode(StyleASTNode child) {
            children.Add(child);
        }

        public override void Release() {
            for (int index = 0; index < children.Count; index++) {
                StyleASTNode child = children[index];
                child.Release();
            }

            children.Clear();
            children = null;
            s_StyleContainerNodePool.Release(this);
        }
    }

    public class ConstNode : StyleASTNode {
        
        public string constName;

        public string constType;

        public override void Release() {
            throw new NotImplementedException();
        }
    } 

    public class ExportNode : StyleASTNode {

        public ConstNode constNode;

        public override void Release() {
            throw new NotImplementedException();
        }
    }

    public class GroupSpecifierNode : StyleASTNode {

        public GroupOperatorType groupOperatorType;

        public string attributeName;

        public string attributeValue;

        public string state;

        public override void Release() {
            throw new NotImplementedException();
        }
    }

    public class PropertyNode : StyleASTNode {

        public string propertyName;
        public LightList<StyleASTNode> propertyValueParts;

        public PropertyNode() {
            type = StyleASTNodeType.Property;
        }

        public override void Release() {
            propertyValueParts.Clear();
            propertyValueParts = null;
        }
    }

    public class ReferenceNode : StyleASTNode {

        public string referenceName;

        public override void Release() {
            s_ReferenceNodePool.Release(this);
        }

        protected bool Equals(ReferenceNode other) {
            return string.Equals(referenceName, other.referenceName);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ReferenceNode) obj);
        }

        public override int GetHashCode() {
            return (referenceName != null ? referenceName.GetHashCode() : 0);
        }
    }

    public class UnaryExpressionNode : StyleASTNode {

        public StyleASTNode expression;
        public TypePath typePath;

        public override void Release() {
            typePath.Release();
            expression?.Release();
            s_UnaryNodePool.Release(this);
        }

    }

    public class MemberAccessExpressionNode : StyleASTNode {

        public string identifier;
        public List<StyleASTNode> parts;

        public MemberAccessExpressionNode() {
            type = StyleASTNodeType.AccessExpression;
        }

        public override void Release() {
            s_MemberAccessExpressionPool.Release(this);
            for (int i = 0; i < parts.Count; i++) {
                parts[i].Release();
            }

            ListPool<StyleASTNode>.Release(ref parts);
        }

    }

    public class ParenNode : StyleASTNode {

        public StyleASTNode expression;

        public ParenNode() {
            type = StyleASTNodeType.Paren;
        }

        public override void Release() {
            expression?.Release();
            s_ParenPool.Release(this);
        }

    }

    public class InvokeNode : StyleASTNode {
        public List<StyleASTNode> parameters;

        public override void Release() {
            for (int i = 0; i < parameters.Count; i++) {
                parameters[i].Release();
            }

            ListPool<StyleASTNode>.Release(ref parameters);
            s_InvokeNodePool.Release(this);
        }

    }

    public class IndexNode : StyleASTNode {
        public StyleASTNode expression;

        public IndexNode() {
            type = StyleASTNodeType.IndexExpression;
        }

        public override void Release() {
            expression?.Release();
            s_IndexExpressionPool.Release(this);
        }
    }

    public class DotAccessNode : StyleASTNode {
        public string propertyName;

        public DotAccessNode() {
            type = StyleASTNodeType.DotAccess;
        }

        public override void Release() {
            s_DotAccessPool.Release(this);
        }
    }

    public class UnitNode : StyleASTNode {
        public string value;

        public UnitNode() {
            type = StyleASTNodeType.Unit;
        }

        public override void Release() {
            s_UnitNodePool.Release(this);
        }
    }

    public class RgbaNode : StyleASTNode {

        public StyleASTNode red;
        public StyleASTNode green;
        public StyleASTNode blue;
        public StyleASTNode alpha;

        public RgbaNode() {
            type = StyleASTNodeType.Rgba;
        }

        public override void Release() {
            s_RgbaNodePool.Release(this);
        }
    }

    public class RgbNode : StyleASTNode {

        public StyleASTNode red;
        public StyleASTNode green;
        public StyleASTNode blue;

        public RgbNode() {
            type = StyleASTNodeType.Rgb;
        }

        public override void Release() {
            s_RgbNodePool.Release(this);
        }
    }

    public class UrlNode : StyleASTNode {

        public StyleASTNode url;

        public UrlNode() {
            type = StyleASTNodeType.Url;
        }

        public override void Release() {
            s_UrlNodePool.Release(this);
        }
    }

}
