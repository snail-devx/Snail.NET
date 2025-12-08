using Snail.Utilities.Collections.Extensions;
using Snail.Utilities.Common.Extensions;
using Snail.Utilities.Linq.Extensions;
using System.Linq.Expressions;

namespace Snail.Database.Components
{
    /// <summary>
    /// 数据库过滤表达式格式化程序 <br />
    ///     1、Where条件表达式做计算，如将new <see cref="List{String}"/>(){}等变量计算出来 <br />
    ///     2、对Where条件表达式做验证，不支持的表达式梳理提示出来 <br />
    /// </summary>
    public sealed class DbFilterFormatter : ExpressionVisitor
    {
        #region 属性变量
        /// <summary>
        /// 默认的格式化器
        /// </summary>
        public static readonly DbFilterFormatter Default = new DbFilterFormatter();

        /// <summary>
        /// 节点类型反序：在进行比较运算表达式移位时处理
        ///     如 &gt;= 反序后 &lt;=
        /// </summary>
        private static readonly IReadOnlyDictionary<ExpressionType, ExpressionType> _reverseType = new Dictionary<ExpressionType, ExpressionType>()
        {
            { ExpressionType.Equal,ExpressionType.Equal},
            { ExpressionType.NotEqual,ExpressionType.NotEqual},
            { ExpressionType.GreaterThan,ExpressionType.LessThan},
            { ExpressionType.GreaterThanOrEqual,ExpressionType.LessThanOrEqual},
            { ExpressionType.LessThan,ExpressionType.GreaterThan},
            { ExpressionType.LessThanOrEqual,ExpressionType.GreaterThanOrEqual},
        };
        /// <summary>
        /// 节点类型取反；如 = 取反 !=
        /// </summary>
        private static readonly IReadOnlyDictionary<ExpressionType, ExpressionType> _notType = new Dictionary<ExpressionType, ExpressionType>()
        {
            //  位运算：做特殊处理
            { ExpressionType.And,ExpressionType.OrElse},
            { ExpressionType.Or,ExpressionType.AndAlso},
            //  boolean逻辑运算
            { ExpressionType.AndAlso,ExpressionType.OrElse},
            { ExpressionType.OrElse,ExpressionType.AndAlso},
            //  比较判断
            { ExpressionType.Equal,ExpressionType.NotEqual},
            { ExpressionType.NotEqual,ExpressionType.Equal},
            { ExpressionType.GreaterThan,ExpressionType.LessThanOrEqual},
            { ExpressionType.GreaterThanOrEqual,ExpressionType.LessThan},
            { ExpressionType.LessThan,ExpressionType.GreaterThanOrEqual},
            { ExpressionType.LessThanOrEqual,ExpressionType.GreaterThan},
        };
        /// <summary>
        /// Boolean类型值
        /// </summary>
        private static readonly Type _boolType = typeof(bool);
        #endregion

        #region 重写父类方法：重写所有方法，尽可能全的进行排查测试

        #region 复合处理：基于不同节点类型分发
        /// <summary>
        /// 访问一元表达式
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            //  根据微软官方文档；穷举能够进行一元表达式构建的情况进行分析
            //  https://docs.microsoft.com/zh-cn/dotnet/api/system.linq.expressions.unaryexpression?view=net-6.0
            switch (node.NodeType)
            {
                //  获取一维数组的长度：动态计算出常量值
                case ExpressionType.ArrayLength:
                    return BuildConstant("ArrayLength", node);
                //  进行类型转换
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    /*
                     *  需要对枚举类型做特例
                     *      1、item.NodeType 会自动翻译成 Convert(item.NodeType,Int32);   成员保持现状
                     *      2、ExpressionType.Add 会自动翻译成 Convert(ExpressionType.Add,Int32)   结算常量
                     */
                    return TryAnalysisMemberExpress(node, out _) == true
                        ? node
                        : BuildConstant("Convert", node);
                //  求反：1的反 -1
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    return BuildConstant("Negate", node);
                //  Not：!item.Contains("") 构建二元条件表达式
                case ExpressionType.Not:
                    {
                        //  判断node的type必须得是boolean
                        if (node.Type != _boolType)
                        {
                            string msg = $"不支持非Boolean类型的not操作，即使Nullable<Boolean>：{node}";
                            throw new NotSupportedException(msg);
                        }
                        //  对数据做一些简化：如 !(item.String==null) 构建成 item.String!=null
                        Expression newNode = Visit(node.Operand)!;
                        //  确保构建为二元表达式后进行取反操作；null值进行默认处理： 构建完成后，对其进行visit操作
                        newNode = BuildNotBinaryExpression(ConvertToBinaryExpression(newNode))!;
                        newNode ??= Expression.MakeBinary(ExpressionType.Equal, node.Operand, Expression.Constant(false));
                        return newNode;
                    }
                //  常量值的表达式:具体没搞懂，先计算常量值试试
                case ExpressionType.Quote:
                    return BuildConstant("Quote", node);
                //  类型转换，装箱拆箱。先计算常量值
                case ExpressionType.TypeAs:
                    return BuildConstant("TypeAs", node);
                //  一元正运算：没搞懂干啥，先计算常量值
                case ExpressionType.UnaryPlus:
                    return BuildConstant("UnaryPlus", node);
                //  兜底，做不支持处理
                default: throw new NotSupportedException($"不支持的一元表达式：{node}");
            }
        }

        /// <summary>
        /// 访问二元表达式
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            //  根据微软官方文档；穷举能够进行二元表达式构建的情况进行分析
            //  https://docs.microsoft.com/zh-cn/dotnet/api/system.linq.expressions.binaryexpression?view=net-6.0#remarks
            switch (node.NodeType)
            {
                //  算数运算符：强制计算出结果，否则进行运算是无意义上的；数据库那边暂时不支持动态计算
                //      可构建出 item.IntValue+3，这种用法mongo数据库支持起来特别麻烦，先不考虑
                case ExpressionType.Add:// 加法
                case ExpressionType.AddChecked:// 加法，但进行溢出检查
                case ExpressionType.Divide:// 除法
                case ExpressionType.Modulo:// 求余
                case ExpressionType.Multiply:// 乘法
                case ExpressionType.MultiplyChecked:// 乘法，但进行溢出检查
                case ExpressionType.Power:// 幂运算
                case ExpressionType.Subtract:// 减法
                case ExpressionType.SubtractChecked:// 减法，但进行溢出检查
                    return BuildConstant("算数运算", node);
                //  位运算符：
                //      这几个涉及位运算，强制求结果值
                case ExpressionType.ExclusiveOr:// 异或 xor
                case ExpressionType.LeftShift:// 左移位运算 <<
                case ExpressionType.RightShift:// 右移位运算 >>
                    return BuildConstant("位运算", node);
                //      and or 位运算，和 && || 效果一致
                case ExpressionType.And://  and  true and false
                    return BuildAndOrAlso(this, node, ExpressionType.AndAlso);
                case ExpressionType.Or://   or   false or flase
                    return BuildAndOrAlso(this, node, ExpressionType.OrElse);
                //  条件布尔运算,
                case ExpressionType.AndAlso:// &&
                case ExpressionType.OrElse: // ||
                    return BuildAndOrAlso(this, node, node.NodeType);
                //  比较运算符：
                case ExpressionType.Equal://    ==
                case ExpressionType.NotEqual:// !=
                case ExpressionType.GreaterThanOrEqual:// >=
                case ExpressionType.GreaterThan://  >
                case ExpressionType.LessThan:// <
                case ExpressionType.LessThanOrEqual:// <=
                    return BuildCompare(this, node, node.NodeType);
                //  合并运算：??必须动态计算为一个常量。str??"默认值"，出现这种强制报错  item.Name??"默认值"不支持啊
                case ExpressionType.Coalesce:
                    return BuildConstant("合并运算", node);
                //  索引运算：计算常量值。 array[0]
                case ExpressionType.ArrayIndex:
                    return BuildConstant("数组索引操作", node);
                //  排除情况，强制不支持
                default: throw new NotSupportedException($"不支持的二元表达式：{node}");
            }
        }

        /// <summary>
        /// 方法调用
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            /* 支持情况的动态，其他情况强制求固定值
            *  1、Contains
            *      item.Name.Contains(固定值）   Name是字符串  实现 sql 的like条件
            *          item.Name.Contains("测试")
            *      new List<String>().Contains(数据库字典）   实现 in查询
            *          new List<String>().Contains(item.Name)
            *  2、StartWith和EndWith
            *      item.Name.StartWith("固定值")
            *      item.Name.EndWith("固定值")  
            *  其他情况标记为固定值解析
            *  注意：
            *      item => item.String.Any(ch => ch == '1')  此种解析时，Any 方法调用时 Object为null
            */
            return BuildMethodCallByMember(node)
                ?? BuildMethodCallByContains(node)
                ?? BuildConstant("方法调用", node);
        }

        /// <summary>
        /// 访问lambda表达式
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            //  对结果进行容错
            Expression newNode = base.VisitLambda(node);
            if (newNode == null)
            {
                string msg = $"lambda计算后返回null。node：{node}";
                throw new NotSupportedException(msg);
            }
            //  计算结果仍然应该是lambda表达式
            if (newNode is LambdaExpression lambda == true)
            {
                Expression binary = ConvertToBinaryExpression(lambda.Body)!;
                newNode = Expression.Lambda<T>(binary, lambda.Parameters);
                return newNode;
            }
            else
            {
                string msg = $"lambda计算后返回节点无效。新节点：{newNode}；原始节点：{node}";
                throw new NotSupportedException(msg);
            }
        }
        #endregion

        #region 直接动态计算求常量值
        /// <summary>
        /// 访问三元表达式
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            /*  只要时访问三元表达式，都强制求值出来
             *      1、item=>item.Name=="测试"?false:true，这种格式针对字段的三元比较，强制不支持
             *      2、item.ParentName == (tmpString == "xxx" ? item.String : "xxxxxxx") 这种强制求值仍然不支持
             *      3、item.ParentName==(tmpString=="xxx"?"合个":"不合格"); 支持，最终解析成 item.ParentName== 固定值
             *  后续考虑优化：现在先不支持
             *      1、item=>item.Name==null?true:false 返回 item=>item.Name==null
             *      2、item=>item.Name==null?false:true 返回 item=>item.Name!=null
             */
            return BuildConstant("三元表达式", node);
        }

        /// <summary>
        /// 尝试类型转换
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            //  尝试做一下常量值转换；  GetInt() is Int32
            //  ！！mongo可基于此做数据库字段类型判断，但其他数据库不支持，先不做处理
            return BuildConstant("Is表达式", node);
        }

        /// <summary>
        /// 访问成员字段、属性；访问变量也是这个
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            /*  做一下唯一值，只有item.Name这种情况，才有效；其他时候强制求固定值，失败就算不支持
             *  以下情况举例：node.Expression.NodeType取值
             *      1、item=>item.Name                           Parameter
             *      2、item=>GetModel().Name                     Call
             *      3、item=>newModel.Name分情况
             *          newModel上下文变量时                     Constant
             *          newModel为实例字段、属性时               MemberAccess 继续向上取Expression会最终取到Constant
             *          newModel为静态字段、属性时               node.Expression值为null
             *      其他情况未知，且未测试；但1的情况算是有效过滤条件，其他都是求值处理。后续不支持的再说
             */
            //  不用三元表达式，这样调试好一下
            if (TryAnalysisMemberExpress(node, out _) == true)
            {
                return node;
            }
            return BuildConstant("成员固定值", node);

            /*  下面是作废代码，仅供参考
                Expression newNode = node;
                while (newNode != null)
                {
                    //  根据节点类型做特例处理
                    switch (newNode.NodeType)
                    {
                        //  继续访问成员，关注静态成员访问  static TestModel ST=new TestModel();访问时  ST.Int>10;
                        case ExpressionType.MemberAccess:
                            newNode = (newNode as MemberExpression).Expression;
                            if (newNode == null) return BuildConstant("静态成员",node);
                            else break;
                        //  访问已定义非静态的变量、属性、字段，一直往上会得到常量节点  如 int tmpInt;item=>tmpInt>10;
                        case ExpressionType.Constant:
                            return BuildConstant( "临时变量/属性/字段",node);
                        //  访问成员时，下个节点是调用方法时，通用构建常量节点
                        case ExpressionType.Call:
                            return BuildConstant("方法调用",node);
                        //  默认情况不做处理；基于往下执行
                        default: break;
                    }
                }
                //  默认处理逻辑
                return base.VisitMember(node);
            */
        }

        /// <summary>
        /// 解析 指定类型默认值
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitDefault(DefaultExpression node)
        {
            /*
             * 动态计算出具体值出来
             *  这种不会走：default(String)；这个在生成lambda表达式之前就计算出来了
             *  这种才会走：formatter.Visit(Expression.Default(typeof(String)));
             */
            Expression newNode = BuildConstant($"{node.Type.Name}默认值", node);
            return newNode;
        }
        /// <summary>
        /// new语句
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitNew(NewExpression node)
        {
            return BuildConstant($"New", node);
        }
        /// <summary>
        /// new 构建数组
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            return BuildConstant($"NewArray", node);
        }

        /// <summary>
        /// 访问代码块
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitBlock(BlockExpression node)
        {
            //  进行动态计算，得到常量固定值
            return BuildConstant($"Block", node);
        }

        /// <summary>
        /// 访问上下文定义的委托Func、Action等lambda表达式
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitInvocation(InvocationExpression node)
        {
            //  始终调用方法，直接上固定值解析
            //      如上下文定义了 Func<Int32> tmpIntFunc = () => 10;
            //      item=>tmpIntFunc()>10 Visit此表达式时，会进入此方法中
            return BuildConstant("委托调用", node);
        }

        /// <summary>
        /// List初始化
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitListInit(ListInitExpression node)
        {
            return BuildConstant($"ListInit", node);
        }
        /// <summary>
        /// 调用构造函数并初始化新对象的一个或多个成员。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            return BuildConstant($"MemberInit", node);
        }
        #endregion

        #region 暂时不用处理；直接调用基类方法
        /// <summary>
        /// 进行表达式访问
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public override Expression? Visit(Expression? node)
        {
            // 总控入口，暂时不做其他逻辑处理
            return base.Visit(node);
        }

        /// <summary>
        /// 访问常量
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            //  没啥可操作的，直接父级返回即可
            return base.VisitConstant(node);
        }

        /// <summary>
        /// 访问变量
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            //  没啥可操作的，直接父级返回即可
            return base.VisitParameter(node);
        }
        #endregion

        #region 强制不做支撑识别的
        /// <summary>
        /// goto语句；不支持
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitGoto(GotoExpression node)
        {
            throw new NotSupportedException($"过滤条件中，不支持goto语句。node：{node}");
        }
        /// <summary>
        /// 标签语句；不支持
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitLabel(LabelExpression node)
        {
            throw new NotSupportedException($"过滤条件中，不支持label语句。node：{node}");
        }
        /// <summary>
        /// goto语句的目标
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override LabelTarget VisitLabelTarget(LabelTarget? node)
        {
            throw new NotSupportedException($"过滤条件中，不支持goto:target语句。node：{node}");
        }
        /// <summary>
        /// loop循环
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitLoop(LoopExpression node)
        {
            throw new NotSupportedException($"过滤条件中，不支持Loop语句。node：{node}");
        }

        /// <summary>
        /// Try语句
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitTry(TryExpression node)
        {
            throw new NotSupportedException($"过滤条件中，不支持Try语句。node：{node}");
        }
        /// <summary>
        /// catch语句
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override CatchBlock VisitCatchBlock(CatchBlock node)
        {
            throw new NotSupportedException($"过滤条件中，不支持Catch语句。node：{node}");
        }

        /// <summary>
        /// 调试信息
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitDebugInfo(DebugInfoExpression node)
        {
            throw new NotSupportedException($"过滤条件中，不支持DebugInfo语句。node：{node}");
        }

        /// <summary>
        /// 扩展的表达式
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitExtension(Expression node)
        {
            throw new NotSupportedException($"过滤条件中，不支持扩展的表达式。node：{node}");
        }

        /// <summary>
        /// switch语句
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitSwitch(SwitchExpression node)
        {
            throw new NotSupportedException($"过滤条件中，不支持Switch语句。node：{node}");
        }
        /// <summary>
        /// switch的case语句
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override SwitchCase VisitSwitchCase(SwitchCase node)
        {
            throw new NotSupportedException($"过滤条件中，不支持SwitchCase语句。node：{node}");
        }

        /// <summary>
        /// 表示 IEnumerable 集合的单个元素的初始值设定项。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override ElementInit VisitElementInit(ElementInit node)
        {
            //  暂时没搞懂能干啥，先不支持
            throw new NotSupportedException($"过滤条件中，不支持ElementInit语句。node：{node}");
        }

        /// <summary>
        /// dynamic动态操作
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitDynamic(DynamicExpression node)
        {
            //  暂时没搞懂能干啥，先不支持
            throw new NotSupportedException($"过滤条件中，不支持Dynamic语句。node：{node}");
        }

        /// <summary>
        /// 数组、集合的索引操作
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitIndex(IndexExpression node)
        {
            // 暂时不支持
            throw new NotSupportedException($"过滤条件中，不支持Index语句。node：{node}");
        }

        /// <summary>
        /// 对字段和属性赋值操作
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            //  筛选过滤条件，不需要对属性字段赋值；不支持
            throw new NotSupportedException($"过滤条件中，不支持MemberAssignment语句。node：{node}");
        }
        /// <summary>
        /// 成员派生绑定
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override MemberBinding VisitMemberBinding(MemberBinding node)
        {
            //  暂时没搞懂能干啥，先不支持
            throw new NotSupportedException($"过滤条件中，不支持MemberBinding语句。node：{node}");
        }
        /// <summary>
        /// 初始化新创建对象的一个集合成员的元素。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
        {
            //  暂时没搞懂能干啥，先不支持
            throw new NotSupportedException($"过滤条件中，不支持MemberListBinding语句。node：{node}");
        }
        /// <summary>
        /// 初始化新创建对象的一个成员的成员
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
        {
            //  暂时没搞懂能干啥，先不支持
            throw new NotSupportedException($"过滤条件中，不支持MemberMemberBinding语句。node：{node}");
        }

        /// <summary>
        /// 为变量提供运行时读/写权限的表达式。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        {
            //  暂时没搞懂能干啥，先不支持
            throw new NotSupportedException($"过滤条件中，不支持RuntimeVariables语句。node：{node}");
        }
        #endregion

        #endregion

        #region 公共方法
        /// <summary>
        /// 动态计算表达式，构建其常量表达式
        ///     如 new <see cref="List{String}"/>(){"1","2"} 直接构建成一个常量表达式节点
        /// </summary>
        /// <param name="titile">操作标题，在报错时做提示语</param>
        /// <param name="expression">要执行的表达式</param>
        /// <returns></returns>

        public static ConstantExpression BuildConstant(string titile, Expression expression)
        {
            ThrowIfNull(expression);
            //  常量直接返回，不用浪费性能了
            if (expression.NodeType == ExpressionType.Constant)
            {
                return (expression as ConstantExpression)!;
            }
            //  执行动态计算，报错则标记表达式无效
            try
            {
                var result = Expression.Lambda(expression).Compile().DynamicInvoke();
                return Expression.Constant(result, expression.Type);
                //if (ReflectHelper.IsNullAble(expression.Type) != true) return Expression.Constant(result);
                //else return Expression.Constant(result, expression.Type);
            }
            catch (Exception ex)
            {
                string msg = $"解析{titile}结果失败：无法计算其'{expression}'结果值";
                throw new NotSupportedException(msg, ex);
            }
        }
        /// <summary>
        /// 尝试从表达式中分析成员节点
        /// </summary>
        /// <param name="node">要分析的表达式节点</param>
        /// <param name="member">分析成功后的成员节点；若分析失败则为null</param>
        /// <returns>是否分析成功</returns>
        public static bool TryAnalysisMemberExpress(Expression? node, out MemberExpression? member)
        {
            /* 表达式中成员节点的情况
             *  1、直接是成员节点   item.Name                  遍历到item时为param
             *  2、实例类的成员     tmpModel.Name              遍历到tmpModel时为常量
             *  3、转换类型         Convert
             *      1、枚举         Convert(item.NodeType)     需要分析出item.NodeType
             *      2、可空转换     List<Boolean?>.Contains(item.Boolean)  此时 item.Boolean 为 Convert(item.boolean,nullable)
             *          Convert(False, Nullable`1)
             *          Convert(item.Boolean, Nullable`1)
             */
            member = null;
            Expression? tmpNode = node;
            int index = 0;
            while (tmpNode != null)
            {
                switch (tmpNode.NodeType)
                {
                    //  成员访问，继续往上
                    case ExpressionType.MemberAccess:
                        index += 1;
                        member = tmpNode as MemberExpression;
                        tmpNode = member!.Expression;
                        break;
                    //  参数变量访问，判定为成员属性
                    case ExpressionType.Parameter:
                        if (index == 1) return true;
                        else
                        {
                            String msg = $"暂不支持子文档/子表操作:{node}";
                            throw new NotSupportedException(msg);
                        }
                    //  Convert：不增加层级，找其Operand
                    case ExpressionType.Convert:
                    case ExpressionType.ConvertChecked:
                        tmpNode = (tmpNode as UnaryExpression)!.Operand;
                        break;
                    default:
                        //  其他情况，暂时不做处理
                        tmpNode = null;
                        break;
                }
            }
            //  兜底强制返回false；并将member强制置为null
            member = null;
            return false;
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 构建&amp;&amp;、||表达式
        /// </summary>
        /// <param name="formatter">格式化程序</param>
        /// <param name="node">原始表达式节点</param>
        /// <param name="targetType">组件后的目标节点类型，只支持&amp;&amp;和 ||</param>
        /// <returns></returns>
        private static BinaryExpression BuildAndOrAlso(DbFilterFormatter formatter, BinaryExpression node, ExpressionType targetType)
        {
            if (targetType != ExpressionType.AndAlso && targetType != ExpressionType.OrElse)
            {
                string msg = $"targetType：{targetType}。有效值：AndAlso、OrElse";
                throw new NotSupportedException(msg);
            }
            //  解析左右两侧
            Expression? left = formatter.Visit(node.Left)!,
                       right = formatter.Visit(node.Right)!;
            //  方便后续标准化取反等，对 item.Contains("_")这种数据，构建成 item.Contains("_")==true
            //      内部会做验证，节点类型必须是Boolean、且不能是常量等逻辑
            left = ConvertToBinaryExpression(left);
            right = ConvertToBinaryExpression(right);
            //  构建新的表达式
            return Expression.MakeBinary(targetType, left!, right!);
        }

        /// <summary>
        /// 构建比较表达式：item.name>1
        /// </summary>
        /// <param name="formatter">格式化程序</param>
        /// <param name="node">原始表达式节点</param>
        /// <param name="targetType">组件后的目标节点类型，只支持&lt;  &lt;=  !=  &gt;=  &lt;= </param>
        /// <returns>新的表达式</returns>
        private static BinaryExpression BuildCompare(DbFilterFormatter formatter, BinaryExpression node, ExpressionType targetType)
        {
            //  解析左侧和右侧
            Expression left = formatter.Visit(node.Left)!,
                       right = formatter.Visit(node.Right)!;
            bool leftIsConstant = left is ConstantExpression,
                    rightIsConstant = right is ConstantExpression;
            //  验证表达式
            //      1、不能同时都是常量，至少得有一个是常量：
            //          全常量；1>2这种没有意义，强制排查出来
            if (leftIsConstant == true && rightIsConstant == true)
            {
                string msg = $"{node}计算后【{left} {targetType} {right}】为恒常量条件，不能作为筛选条件使用";
                throw new NotSupportedException(msg);
            }
            //      2、全是变量：则涉及到数据库中动态字段比较，mongo支持会复杂一下，先不支持
            /*          后续考虑针对全是变量的做特例如返回值都是boolean时，做拆分；如p.String.Contains("_")==p.Id.Contains("X")*/
            if (leftIsConstant != true && rightIsConstant != true)
            {
                string msg = $"{node}计算后【{left} {targetType} {right}】为恒变量条件，不能作为筛选条件使用";
                throw new NotSupportedException(msg);
            }
            //  构建表达式，并返回
            BinaryExpression binary;
            //      1、当前左侧时常量时，需要移位处理；本逻辑处理完，左侧始终是常量
            if (leftIsConstant == true)
            {
                binary = _reverseType.TryGetValue(targetType, out _) == true
                    ? Expression.MakeBinary(targetType, right, left)
                    : throw new NotSupportedException($"不支持的ExpressionType:{targetType}");
            }
            else
            {
                binary = Expression.MakeBinary(targetType, left, right);
            }
            //      2、当左右两侧都是Boolean类型时、且左侧得是二元表达式才行，考虑做精简优化处理
            /*      精简优化目的：减少多个无效嵌套判断，方便后期简化sql拼接
             *          1、item=>(item.Name.Contains("")==true)==false    item.Name.Contains("")==false
             *          2、item=>(item.Name.Contains("")==false)==true    item.Name.Contains("")==false
             *          3、item=>(item.Name.Contains("")==true)==true    item.Name.Contains("")==true
             *          4、item=>(item.Name.Contains("")==false)==false    item.Name.Contains("")==true
             *          5、item=>(item.Int>100)==true                        item.Int>100
             *          6、item=>(item.Int>100)==false                       item.Int<=100
             */
            bool needOptimize = left.Type == _boolType && right.Type == _boolType && (left is BinaryExpression) == true;
            if (needOptimize == false) return binary;
            //      3、优化表达式：左侧都是二元表达式，整理后只能是 比较  和  and/|| 运算
            bool constValue = right.GetConstValue<bool>();
            switch (binary.NodeType)
            {
                //  == 值时；右侧为false取反；右侧为true时只要左侧
                case ExpressionType.Equal:
                    {
                        BinaryExpression bx = (binary.Left as BinaryExpression)!;
                        if (constValue == false)
                        {
                            bx = BuildNotBinaryExpression(bx) ?? binary;
                        }
                        return bx;
                    }
                //  != 值时；右侧为true取反；右侧为false时只要左侧
                case ExpressionType.NotEqual:
                    {
                        BinaryExpression bx = (binary.Left as BinaryExpression)!;
                        if (constValue == true)
                        {
                            bx = BuildNotBinaryExpression(bx) ?? binary;
                        }
                        return bx;
                    }
                //  其他情况实际上不支持，先忽略
                default: return binary;
            }
        }

        /// <summary>
        /// 针对二元表达式取反操作
        ///     如 &gt;=       取反 &lt;
        ///        &amp;&amp;  取反 ||
        ///        ==          取反 !=
        /// </summary>
        /// <param name="binary"></param>
        /// <remarks>
        ///     调用前，请确保对表达式执行了visit操作；
        ///         1、避免出现 item=>!item.Contains("")这种表达式，调用visit之后，会格式化成 item=>!(item.Contains("")==true)
        ///         2、后续考虑对可空类型取反做适配；
        /// </remarks>
        /// <returns></returns>
        private static BinaryExpression? BuildNotBinaryExpression(BinaryExpression? binary)
        {
            //  无效值，不支持的，则不做处理
            if (binary == null)
            {
                return binary;
            }
            if (_notType.TryGetValue(binary.NodeType, out ExpressionType targetType) != true)
            {
                return binary;
            }
            //  对左右两侧进行visit，确保格式化成表转化的binary格式
            Expression left = binary.Left,
                       right = binary.Right;
            //  穷举左右两侧，进行判断，做递归；如果还是binary，则继续递归做处理
            //      如这种，显示andelse、再是==和不等于 item=> !( item.Contains("_1")&&item.Name==null)
            if (left is BinaryExpression)
            {
                left = BuildNotBinaryExpression(left as BinaryExpression)!;
            }
            if (right is BinaryExpression)
            {
                right = BuildNotBinaryExpression(right as BinaryExpression)!;
            }
            //  组装表达式：内部临时接收，方便调试
            binary = Expression.MakeBinary(targetType, left, right);
            return binary;
        }

        /// <summary>
        /// 尝试将节点转成二元表达式
        ///     如item.Name.Contains("_")转成 item.Name.Contains("_")==true;
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static BinaryExpression? ConvertToBinaryExpression(Expression node)
        {
            //  若是常量，不能作为过滤条件 item=>true
            if (node == null)
            {
                return null;
            }
            if (node is BinaryExpression binary == true)
            {
                return binary;
            }
            //  尝试做判断处理，然后尽可能转换成功
            if (node.NodeType == ExpressionType.Constant)
            {
                string msg = $"表达式为常量值；无法构建二元表达式。node：{node}";
                throw new NotSupportedException(msg);
            }
            if (node.Type != _boolType)
            {
                string msg = $"表达式不为Boolean值；无法转成二元表达式。node：{node}";
                throw new NotSupportedException(msg);
            }
            //  构建成 ==true 的二元表达式
            binary = Expression.MakeBinary(ExpressionType.Equal, node, Expression.Constant(true));
            return binary;
        }

        /// <summary>
        /// 构建类型成员的方法调用，如item.Name.Contains("_")
        /// </summary>
        /// <param name="node"></param>
        /// <returns>不符合条件返回null</returns>
        private static Expression? BuildMethodCallByMember(MethodCallExpression node)
        {
            /*  先仅支持String类型成员的字符串匹配
             *      1、item.Name.Contains
             *      2、item.Name.StartsWith
             *      3、item.Name.EndsWith
             *  参数格式和参数位置都有要求
             *      1、第一个参数必须的能计算成常量，不能是 item.Name.Contains(item.String)
             *      2、第二个参数可选，暂不处理
             */
            //  判定出来，不是成员表达式，则直接返回
            if (TryAnalysisMemberExpress(node.Object, out _) != true)
            {
                return null;
            }
            //  1、对调用的数据类型，方法名称、参数格式做支持性判断
            if (node.Method.DeclaringType?.IsString() != true)
            {
                string msg = $"暂不支持非String类型调用{node.Method.Name}，且不支持扩展方法调用。节点：{node}";
                throw new NotSupportedException(msg);
            }
            if (node.Arguments.Count == 0)
            {
                string msg = $"{node.Method.Name}方法无参数。节点：{node}";
                throw new ArgumentNullException(msg);
            }
            bool isSupport = node.Method.Name switch
            {
                "Contains" => true,
                "StartsWith" => true,
                "EndsWith" => true,
                _ => false,
            };
            if (isSupport != true)
            {
                string msg = $"不支持的方法“{node.Method.Name}”。节点：{node}";
                throw new NotSupportedException(msg);
            }
            //  2、解析参数值，并确保能计算成常量值：如果参数是 item.Name 这种，直接报错；否则解析常量
            Expression arg = node.Arguments.First();
            if (TryAnalysisMemberExpress(arg, out _) == true)
            {
                string msg = $"{node.Method.Name}方法调用时，第一个参数必须是可计算值。{node}";
                throw new NotSupportedException(msg);
            }
            ConstantExpression @const = BuildConstant("方法参数", arg);
            //      判断一下，如果值为null，则报错；char格式时，始终不会是空的，这里不用管
            if (@const == null || @const.Value == null || (@const.Value as String) == String.Empty)
            {
                string msg = $"{node.Method.Name}方法调用时，第一个参数计算结果不能为null/空字符串。{node}";
                throw new NotSupportedException(msg);
            }
            //  3、重新构建表达式
            //      将node中其他参数加入进来
            List<Expression> args = [@const, .. node.Arguments];
            args.RemoveAt(1);
            //      动态生成新的表达式；临时缓存一下，查看效果
            node = Expression.Call(node.Object, node.Method, args);
            return node;
        }
        /// <summary>
        /// 构建数据集合的方法调用，如 newList.Contains(item.Name)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static Expression? BuildMethodCallByContains(MethodCallExpression node)
        {
            /*  先仅支持Contains方法；其他方法直接走固定值
             *  1、静态方法调用时：methodCallNode为null
             *      1、Array的Contains方法，是在【Enumerable】中定义的
             *      2、List的Contains方法，是自己的List<>中定义的
             *      3、ReadOnlySpan<T>的Contains方法，是在[System.MemoryExtensions]定义的
             *          在.net10中，对数组做了性能优化，编译时隐式转为 ReadOnlySpan<T>；此时.contains方法位于System.MemoryExtensions
             *          仅针对编译时能够确定的数组做了优化，如动态查询条件中的数组，仍然编译时确定不了，仍然会保持为数组，而不是ReadOnlySpan<T>
             *          源码：(TestDbModel item) => new ExpressionType?[] { ExpressionType.Add, null,  }.Contains(item.NodeTypeNull) == true
             *          编译后： 
             *               (TestDbModel item) => MemoryExtensions.Contains(
             *                   new ExpressionType?[3]{ExpressionType.Add,null,ExpressionType.GreaterThanOrEqual}, 
             *                   item.NodeTypeNull, null
             *               ) == true);
             *          调试时，可以看到数组被隐式转换了：op_Implicit(new [] {Convert(Add, Nullable`1), null, Convert(GreaterThanOrEqual, Nullable`1)})
             *  2、下面集中情况调用时，methodCallNode非空，为调用前变量方法
             *      1、临时构建list等变量  item=>new List<String>{}.Contains
             *      2、上下文定义的Contains实例方法  item=>Contains(item.String)==true;      无效
             *  这里的成员节点有一下几种情况
             *      1、成员自身           item.Name               正常属性访问
             *      2、Convert           item.NodeType           枚举的Convert节点
             *      3、Nullable<>        item.BooleanNull        成员可空类型
             *      4、Convert+Nullable  item.NodeTypeNull       枚举类型可空时
             */
            //  1、分析Contains的调用方节点和调用参数：只支持List和Array，其他情况不支持
            if (node.Method.Name == "Contains")
            {
                Type methodType = node.Method.DeclaringType!;
                //  String.Contains调用，则直接提示不支持
                if (methodType.IsString() == true)
                {
                    string msg = $"不支持常量字符串的Contains调用。node：{node}";
                    throw new NotSupportedException(msg);
                }
                //  List<>类型数据；自身实现了Contains方法
                if (methodType.IsList(out _) == true)
                {
                    Expression constValue = BuildConstant("Contains调用方", node.Object!);
                    ValidateMemberNodeOfContainsMethod(node, node.Arguments[0], true);
                    return Expression.Call(constValue, node.Method, node.Arguments[0]);
                }
                //  Array、List类型的Contains，扩展子 System.Linq.Enumerable；或者list类型调用 Contains<>
                if (methodType == typeof(Enumerable))
                {
                    Expression constValue = node.Arguments[0];
                    if (constValue.Type.IsArray != true && constValue.Type.IsList(out _) != true)
                    {
                        string msg = $"不支持Array、List外的IEnumerable类型调用Contains。node：{node}";
                        throw new NotSupportedException(msg);
                    }
                    constValue = BuildConstant("Contains调用方", constValue);
                    ValidateMemberNodeOfContainsMethod(node, node.Arguments[1], true);
                    return Expression.Call(node.Method, constValue, node.Arguments[1]);
                }
                //  ReadOnlySpan<T>类型数据：不进行常量计算处理，比较麻烦（需要反射构建出Contains方法调用Lambda表达式），运行时使用时再分析
                if (methodType == typeof(MemoryExtensions))
                {
                    ValidateMemberNodeOfContainsMethod(node, node.Arguments[1], false);
                    return node;
                }
            }

            return null;
        }
        /// <summary>
        /// 验证cotnains方法的成员节点 <br />
        /// 1、验证成员节点是否合法，是否支持 <br />
        /// 2、如 new ExpressionType?[] { ExpressionType.Add, null }.Contains(item.NodeTypeNull) 的 item.NodeTypeNull 节点
        /// </summary>
        /// <param name="node"></param>
        /// <param name="memberNode"></param>
        /// <param name="mustLastParam">成员节点是否必须是最后一个参数</param>
        /// <exception cref="NotSupportedException"></exception>
        private static void ValidateMemberNodeOfContainsMethod(MethodCallExpression node, Expression memberNode, bool mustLastParam)
        {
            //  检测参数节点是否是第一个
            if (mustLastParam == true && node.Arguments.Last() != memberNode)
            {
                string msg = $"Contains的{node.Arguments.Last()}参数无效。node：{node}";
                throw new NotSupportedException(msg);
            }
            //  验证是 成员节点 item.Node；但要注意成员为枚举和可空类型时
            if (TryAnalysisMemberExpress(memberNode, out _) == false)
            {
                string msg = $"Contains参数无效，正确示例:xxx.Contains(item.Name)。node：{node}";
                throw new NotSupportedException(msg);
            }
            //  验证成员节点为基础数据类型
            if (memberNode.Type.IsBaseType() == false)
            {
                string msg = $"Contains不支持的数据类型{memberNode.Type.Name}。node：{node}";
                throw new NotSupportedException(msg);
            }
        }
        #endregion
    }
}
