using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using Match3Tray.Core;
using Match3Tray.Logging;
using UnityEngine;
using UnityEngine.Events;

namespace Match3Tray.Binding
{
    /// <summary>
    ///     A UI binding component that parses boolean expressions and invokes UnityEvents based on the result.
    ///     Supports expressions like:
    ///     - Direct boolean properties: 'MenuScene.IsActive'
    ///     - Numeric comparisons: 'MenuScene.Gold > 10', 'MenuScene.Health <= 100'
    /// - Property comparisons: 'MenuScene.Gold >=
    ///     MenuScene.LevelThreshold'
    ///     - Equality checks: 'MenuScene.Score == MenuScene.HighScore'
    ///     - Inequality checks: 'MenuScene.CurrentHealth != MenuScene.MaxHealth'
    ///     - Logical AND: 'MenuScene.IsActive && MenuScene.HasGold'
    ///     - Logical OR: 'MenuScene.IsPaused || MenuScene.IsGameOver'
    ///     - Nested expressions: 'MenuScene.IsActive && (MenuScene.Gold > 10)'
    ///     - Complex conditions: '(MenuScene.Gold > MenuScene.Bet) && (MenuScene.Level > MenuScene.LevelThreshold)'
    ///     Supported operators:
    ///     - Comparison: >, <, >=, <=, ==, !=
    ///     - Logical: &&, ||
    ///     - Grouping: ( )
    ///     Format examples:
    ///     - Single boolean: [ContextName].[PropertyName]
    ///     - Comparison: [ContextName].[PropertyName] [operator] [ContextName].[PropertyName] or [number]
    ///     - Logical: [expression] [&& or ||] [expression]
    ///     - Nested: [expression] [&& or ||] ([expression] [operator] [expression])
    /// </summary>
    [RequireComponent(typeof(MonoBehaviour))]
    [DefaultExecutionOrder(-300)]
    public class UIBoolBinding : UIBinding
    {
        private static readonly string[] ComparisonOperators = { ">=", "<=", "==", "!=", ">", "<" };
        private static readonly string[] LogicalOperators = { "&&", "||" };

        /// <summary>
        ///     Cache of property information to avoid repeated reflection lookups.
        /// </summary>
        private static readonly Dictionary<(Type, string), PropertyInfo> _propCache = new(31);

        /// <summary>
        ///     Cache of event information to avoid repeated reflection lookups.
        /// </summary>
        private static readonly Dictionary<(Type, string), EventInfo> _eventCache = new(31);

        /// <summary>
        ///     Shared object array pool for efficient memory usage during binding updates.
        /// </summary>
        private static readonly ArrayPool<object> sPool = ArrayPool<object>.Shared;

        // üst seviye karşılaştırma
        private static readonly string[] ComparisonOps = { ">=", "<=", "==", "!=", ">", "<" };

        /// <summary>
        ///     The expression to evaluate. Examples:
        ///     - 'MenuScene.IsActive'
        ///     - 'MenuScene.Gold >= 10'
        ///     - 'MenuScene.Health <= MenuScene.MaxHealth'
        /// - 'MenuScene.Score == MenuScene.HighScore'
        /// - 'MenuScene.IsActive && MenuScene.HasGold'
        /// - 'MenuScene.IsPaused || MenuScene.IsGameOver'
        /// </summary>
        [Tooltip("Expression to evaluate. Examples:\n" +
                 "- 'MenuScene.IsActive'\n" +
                 "- 'MenuScene.Gold >= 10'\n" +
                 "- 'MenuScene.Health <= MenuScene.MaxHealth'\n" +
                 "- 'MenuScene.Score == MenuScene.HighScore'\n" +
                 "- 'MenuScene.IsActive && MenuScene.HasGold'\n" +
                 "- 'MenuScene.IsPaused || MenuScene.IsGameOver'")]
        [SerializeField]
        private string _expression;

        /// <summary>
        ///     Event invoked when the expression evaluates to true.
        /// </summary>
        [SerializeField] private UnityEvent _onTrue = new();

        /// <summary>
        ///     Event invoked when the expression evaluates to false.
        /// </summary>
        [SerializeField] private UnityEvent _onFalse = new();

        /// <summary>
        ///     List of active bindings for this UI component.
        /// </summary>
        private new readonly List<BindingInfo> _bindings = new(8);

        private ExpressionNode _expressionTree;

        /// <summary>
        ///     Initializes the binding system by parsing the expression and setting up event handlers.
        /// </summary>
        public override void Start()
        {
            if (string.IsNullOrWhiteSpace(_expression))
            {
                LoggerExtra.LogError($"[{name}] UIBoolBinding: no expression set", this);
                return;
            }

            try
            {
                _expressionTree = ParseExpressionTree(_expression);
                if (_expressionTree == null) throw new ArgumentException($"Failed to parse expression: {_expression}");

                CollectBindings(_expressionTree);
                EvaluateExpressionTree();
            }
            catch (Exception ex)
            {
                LoggerExtra.LogError($"[{name}] UIBoolBinding: {ex.Message}", this);
            }
        }

        /// <summary>
        ///     Cleans up event listeners when the component is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (_bindings != null)
            {
                foreach (var binding in _bindings)
                    if (binding.Event != null && binding.Source != null && binding.Handler != null)
                        binding.Event.RemoveEventHandler(binding.Source, binding.Handler);
                _bindings.Clear();
            }
        }

        /// <summary>
        ///     Called when any binding is updated.
        /// </summary>
        protected override void OnBindingUpdated(int bindingIndex, object[] args)
        {
            EvaluateExpressionTree();
        }


        /// <summary>
        ///     Generic callback method invoked by reflection whenever any Bindable<T>.OnValueChanged fires.
        /// </summary>
        /// <typeparam name="T">The type of the bound value</typeparam>
        /// <param name="_">The new value (unused)</param>
        private void OnValueChanged<T>(T _)
        {
            EvaluateExpressionTree();
        }

        /// <summary>
        ///     ParseExpressionTree: tüm logical, comparison ve parantezli ifadeleri ele alır.
        /// </summary>
        private ExpressionNode ParseExpressionTree(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new ArgumentException("Expression is null or empty");

            return ParseLogicalOr(expression.Trim());
        }

        // en düşük öncelik: ||
        private ExpressionNode ParseLogicalOr(string expr)
        {
            var idx = FindOperatorOutsideParens(expr, "||");
            if (idx >= 0)
                return new ExpressionNode
                {
                    Type = ExpressionNode.NodeType.Logical,
                    Operator = "||",
                    Left = ParseLogicalOr(expr.Substring(0, idx)),
                    Right = ParseLogicalAnd(expr.Substring(idx + 2))
                };
            return ParseLogicalAnd(expr);
        }

        // orta öncelik: &&
        private ExpressionNode ParseLogicalAnd(string expr)
        {
            var idx = FindOperatorOutsideParens(expr, "&&");
            if (idx >= 0)
                return new ExpressionNode
                {
                    Type = ExpressionNode.NodeType.Logical,
                    Operator = "&&",
                    Left = ParseLogicalAnd(expr.Substring(0, idx)),
                    Right = ParseComparison(expr.Substring(idx + 2))
                };
            return ParseComparison(expr);
        }

        private ExpressionNode ParseComparison(string expr)
        {
            foreach (var op in ComparisonOps)
            {
                var idx = FindOperatorOutsideParens(expr, op);
                if (idx >= 0)
                    return new ExpressionNode
                    {
                        Type = ExpressionNode.NodeType.Comparison,
                        Operator = op,
                        Left = ParseComparison(expr.Substring(0, idx)),
                        Right = ParsePrimary(expr.Substring(idx + op.Length))
                    };
            }

            return ParsePrimary(expr);
        }

        // en yüksek öncelik: parantez, literal veya bindable
        private ExpressionNode ParsePrimary(string expr)
        {
            expr = expr.Trim();
            // parantezli
            if (expr.Length >= 2 && expr[0] == '(' && expr[^1] == ')')
            {
                // derinliği kontrol ederek gerçekten tüm ifade paranteze sarılı mı diye bak
                var depth = 0;
                for (var i = 0; i < expr.Length; i++)
                {
                    if (expr[i] == '(') depth++;
                    else if (expr[i] == ')') depth--;
                    if (depth == 0 && i < expr.Length - 1)
                    {
                        depth = -1; // içte kapanma var, parantez tüm expr'ı sarmıyor
                        break;
                    }
                }

                if (depth == 0)
                    return ParseExpressionTree(expr.Substring(1, expr.Length - 2));
            }

            // bool literal
            if (bool.TryParse(expr, out var b))
                return new ExpressionNode { Type = ExpressionNode.NodeType.Value, Value = b };

            // sayısal literal
            if (double.TryParse(expr, out var d))
                return new ExpressionNode { Type = ExpressionNode.NodeType.Value, Value = d };

            // aksi halde binding ifadesi (örneğin MenuScene.Gold)
            var info = ParseExpression(expr);
            return new ExpressionNode
            {
                Type = ExpressionNode.NodeType.Value,
                Binding = info,
                Value = info.Getter()
            };
        }

        /// <summary>
        ///     Parantez derinliğine bakarak operatörü expr içinde derinlik 0 iken arar.
        /// </summary>
        private int FindOperatorOutsideParens(string expr, string op)
        {
            var depth = 0;
            // logical operatörler için sağdan sola doğru bakmak lazım (lowest-precedence split için)
            for (var i = expr.Length - op.Length; i >= 0; i--)
            {
                var c = expr[i];
                if (c == ')')
                {
                    depth++;
                }
                else if (c == '(')
                {
                    depth--;
                }
                else if (depth == 0)
                {
                    // substring ile op araması
                    var match = true;
                    for (var j = 0; j < op.Length; j++)
                        if (expr[i + j] != op[j])
                        {
                            match = false;
                            break;
                        }

                    if (match) return i;
                }
            }

            return -1;
        }

        /// <summary>
        ///     Creates a handler for the Bindable<T>.OnValueChanged event.
        /// </summary>
        private Delegate CreateValueChangedHandler(Type bindableType)
        {
            // 1) bindableType == typeof(Bindable<T>), ondan gerçek T tipini alıyoruz:
            var valueType = bindableType.GetGenericArguments()[0];

            // 2) EventInfo’den gerçek handler tipini (Action<T>) çek:
            var eventInfo = bindableType.GetEvent("OnValueChanged", BindingFlags.Instance | BindingFlags.Public)!;
            var handlerType = eventInfo.EventHandlerType!;

            // 3) Bu sınıftaki OnValueChanged<T>(T _) metodunu T ile oluştur:
            var method = GetType()
                .GetMethod(nameof(OnValueChanged), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(valueType);

            // 4) Delegate’i Action<T> olarak yarat:
            return Delegate.CreateDelegate(handlerType, this, method);
        }


        private ExpressionNode ParseValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                LoggerExtra.LogError($"[{name}] UIBoolBinding: Value is null or empty", this);
                return null;
            }

            // 1) Bool literal?
            if (bool.TryParse(value, out var boolValue))
                return new ExpressionNode
                {
                    Type = ExpressionNode.NodeType.Value,
                    Value = boolValue
                };

            // 2) Numeric literal?
            if (double.TryParse(value, out var numValue))
                return new ExpressionNode
                {
                    Type = ExpressionNode.NodeType.Value,
                    Value = numValue
                };

            // 3) O zaman binding deniyoruz:
            var binding = ParseExpression(value);
            if (binding.Prop != null)
            {
                var bindingValue = binding.Getter();
                return new ExpressionNode
                {
                    Type = ExpressionNode.NodeType.Value,
                    Binding = binding,
                    Value = bindingValue
                };
            }

            // Hiçbiri değilse gerçekten geçersiz:
            LoggerExtra.LogError($"[{name}] UIBoolBinding: Invalid value: {value}", this);
            return null;
        }

        private void CollectBindings(ExpressionNode node)
        {
            if (node == null) return;

            if (node.Type == ExpressionNode.NodeType.Value && node.Binding.Source != null && node.Binding.Prop != null)
                try
                {
                    _bindings.Add(node.Binding);
                    if (node.Binding.Event != null && node.Binding.Source != null && node.Binding.Handler != null) node.Binding.Event.AddEventHandler(node.Binding.Source, node.Binding.Handler);
                }
                catch (Exception ex)
                {
                    LoggerExtra.LogError($"[{name}] UIBoolBinding: Failed to add event handler: {ex.Message}", this);
                }

            CollectBindings(node.Left);
            CollectBindings(node.Right);
        }

        private void EvaluateExpressionTree()
        {
            try
            {
                var result = EvaluateNode(_expressionTree);
                if (result) _onTrue.Invoke();
                else _onFalse.Invoke();
            }
            catch (Exception ex)
            {
                LoggerExtra.LogError($"[{name}] UIBoolBinding: Error evaluating expression: {ex.Message}", this);
            }
        }

        private bool EvaluateNode(ExpressionNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            switch (node.Type)
            {
                case ExpressionNode.NodeType.Value:
                    if (node.Binding.Prop == null) return node.Value.ToBool();
                    var value = node.Binding.Getter();
                    if (value == null) throw new InvalidOperationException($"Binding value is null for {node.Binding.Prop.Name}");
                    return value.ToBool();

                case ExpressionNode.NodeType.Comparison:
                    var leftValue = GetNodeValue(node.Left);
                    var rightValue = GetNodeValue(node.Right);

                    if (leftValue == null || rightValue == null) throw new InvalidOperationException("Comparison operands cannot be null");

                    var leftNum = leftValue.ToDouble();
                    var rightNum = rightValue.ToDouble();

                    switch (node.Operator)
                    {
                        case ">=": return leftNum >= rightNum;
                        case "<=": return leftNum <= rightNum;
                        case ">": return leftNum > rightNum;
                        case "<": return leftNum < rightNum;
                        case "==": return leftNum == rightNum;
                        case "!=": return leftNum != rightNum;
                        default: throw new InvalidOperationException($"Unknown comparison operator: {node.Operator}");
                    }

                case ExpressionNode.NodeType.Logical:
                    var leftResult = EvaluateNode(node.Left);
                    var rightResult = EvaluateNode(node.Right);

                    switch (node.Operator)
                    {
                        case "&&": return leftResult && rightResult;
                        case "||": return leftResult || rightResult;
                        default: throw new InvalidOperationException($"Unknown logical operator: {node.Operator}");
                    }

                default:
                    throw new InvalidOperationException($"Unknown node type: {node.Type}");
            }
        }

        private object GetNodeValue(ExpressionNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            if (node.Type == ExpressionNode.NodeType.Value)
            {
                if (node.Binding.Prop != null)
                {
                    var value = node.Binding.Getter();
                    if (value == null) throw new InvalidOperationException($"Binding value is null for {node.Binding.Prop.Name}");
                    return value;
                }

                return node.Value;
            }

            return EvaluateNode(node);
        }

        /// <summary>
        ///     Parses an expression into a BindingInfo.
        /// </summary>
        private BindingInfo ParseExpression(string expr)
        {
            var dot = expr.IndexOf('.');
            if (dot < 1 || dot == expr.Length - 1)
            {
                LoggerExtra.LogError($"[{name}] UIBoolBinding: invalid expression '{expr}'", this);
                return default;
            }

            var ctxName = expr.Substring(0, dot).Trim();
            var propName = expr.Substring(dot + 1).Trim();

            // Get context from registry
            var ctx = BindingContextRegistry.Get(ctxName);
            if (ctx == null)
            {
                LoggerExtra.LogError($"[{name}] UIBoolBinding: context '{ctxName}' not found in registry", this);
                return default;
            }

            var prop = ctx.GetType().GetProperty(propName);
            if (prop == null)
            {
                LoggerExtra.LogError($"[{name}] UIBoolBinding: property '{propName}' not found on {ctx.GetType().Name}", this);
                return default;
            }

            // Get the Bindable property value
            var bindableValue = prop.GetValue(ctx);
            if (bindableValue == null)
            {
                LoggerExtra.LogError($"[{name}] UIBoolBinding: property '{propName}' is null on {ctx.GetType().Name}", this);
                return default;
            }

            // Get the Value property from Bindable
            var valueProp = bindableValue.GetType().GetProperty("Value");
            if (valueProp == null)
            {
                LoggerExtra.LogError($"[{name}] UIBoolBinding: Value property not found on {bindableValue.GetType().Name}", this);
                return default;
            }

            // Get the OnValueChanged event from the Bindable instance
            var eventInfo = bindableValue.GetType().GetEvent("OnValueChanged");
            if (eventInfo == null)
            {
                LoggerExtra.LogError($"[{name}] UIBoolBinding: OnValueChanged event not found on {bindableValue.GetType().Name}", this);
                return default;
            }

            var handler = CreateValueChangedHandler(bindableValue.GetType());
            if (handler == null)
            {
                LoggerExtra.LogError($"[{name}] UIBoolBinding: failed to create handler for {bindableValue.GetType().Name}", this);
                return default;
            }

            LoggerExtra.Log($"[{name}] UIBoolBinding: Created binding for {expr} with handler type {handler.GetType().Name}");

            return new BindingInfo(
                bindableValue,
                valueProp,
                () => valueProp.GetValue(bindableValue),
                eventInfo,
                handler
            );
        }

        private class ExpressionNode
        {
            public enum NodeType
            {
                Value,
                Comparison,
                Logical
            }

            public NodeType Type { get; set; }
            public string Operator { get; set; }
            public ExpressionNode Left { get; set; }
            public ExpressionNode Right { get; set; }
            public BindingInfo Binding { get; set; }
            public object Value { get; set; }
        }
    }
}