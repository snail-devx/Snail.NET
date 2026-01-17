using Snail.Test.Database.DataModels;
using System.Linq.Expressions;

namespace Snail.Test.Database.Components
{
    /// <summary>
    /// 数据库筛选条件表达式代理
    /// </summary>
    public sealed class DbFilterExpressProxy
    {
        #region 编译错误列表；这里面测试代码较多，先禁用掉
#pragma warning disable CS0169
#pragma warning disable CS0183
#pragma warning disable CS0414
#pragma warning disable CS0649
#pragma warning disable CS8602
#pragma warning disable CS8604
#pragma warning disable CS8625
#pragma warning disable CA1822
#pragma warning disable CA1825
#pragma warning disable CA1829
#pragma warning disable CA2211
#pragma warning disable IDE0044
#pragma warning disable IDE0051
#pragma warning disable IDE0052
#pragma warning disable IDE0054
#pragma warning disable IDE0059
#pragma warning disable IDE0060
#pragma warning disable IDE0071
#pragma warning disable IDE0075
#pragma warning disable IDE0090
        #endregion

        #region 测试linq需要的各种实例/静态级变量、属性、方法、委托
        private int Ins_Int = 100;
        private static int St_Int = 1000;
        private TestDbModel Ins_Model = new();
        private static TestDbModel St_Model = new();
        private static string[] ST_Strs = new string[] { };
        private string[] Ins_Strs = new string[] { };

        private static int GetInt() => 100;
        private int GetTmpInt() => 10;
        private static TestDbModel GetModel() => new();
        private static TestDbModel GetTmpModel() => new();
        private static List<string> GetStringList() => ["1", "2", "3"];
        private string[] GetStringArray() => ["1", "2", "3"];
        private int[] GetIntArray() => [1, 3, 5, 7, 9];
        private static List<int> GetIntList() => [2, 4, 6, 8, 0];
        private List<int> GetTmpIntList() => [2, 4, 6, 8, 0];
        public bool Contains(string str) => true;
        //public static bool Contains(string str) => true;
        #endregion

        #region 公共方法
        /// <summary>
        /// 构建过滤条件表达式
        /// </summary>
        /// <returns></returns>
        internal List<DbFilterExpressDescriptor> BuildFilterExpress()
        {
            List<DbFilterExpressDescriptor> filters = new();
            //      临时需要用到的变量
            object? d = null;
            int tmpInt = 100;
            int? intNull = null, intNull10 = 10;
            string tmpString = "xxxxxxxxxxxx";
            string[] strArray = new string[] { "1", "2", "3", "4" };
            int[] intArray = new int[] { 1, 2, 3, 6, 8, 5 };
            bool? boolNullFalse = false, boolNull = null;
            TestDbModel newModel = new();
            Func<int, int> tmpIntFunc = (intValue) => 10 + intValue;
            Random random = new(DateTime.Now.Second);
            string randomStr = random.Next(1, 100).ToString();/*不能直接把变量表达式中，否则在多次查询时，结果有差异*/
            ExpressionType nodeType = (ExpressionType)(random.Next(1, 20));
            //  添加过滤条件
            var addFilter = (bool isSurppot, bool isTextFilter, Expression<Func<TestDbModel, bool>> express) =>
            {
                filters.Add(new DbFilterExpressDescriptor(isSurppot, isTextFilter, express));
            };
            addFilter(true, false, item => new ExpressionType?[] { ExpressionType.Add, null, ExpressionType.GreaterThanOrEqual }.Contains(item.NodeTypeNull) == true);
            addFilter(true, false, item => new ExpressionType?[] { }.Contains(item.NodeTypeNull) == true);
            // return filters;
            // sql下这个值有问题： select* from snail_testmodel where string not like "%_%"
            addFilter(true, true, item => !(item.Int > 0 || (item.DateTime > DateTime.Now || item.String!.Contains("1  "))));
            //  开发时，先搞几个特例调试
            addFilter(true, false, item => new List<ExpressionType?>() { ExpressionType.Add, null, ExpressionType.GreaterThanOrEqual }.Contains(item.NodeTypeNull) == true);
            addFilter(true, false, item => new ExpressionType?[] { ExpressionType.Add, null, ExpressionType.GreaterThanOrEqual }.Contains(item.NodeTypeNull) == true);
            addFilter(true, false, item => new List<bool?>() { true, false, null }.Contains(item.BoolNull) == true);
            addFilter(true, false, item => item.NodeTypeNull != nodeType);
            addFilter(true, false, item => item.NodeTypeNull < nodeType);
            //      测试复杂条件 ！())(_()(*)
            addFilter(true, false, item => item.Int <= 1 >> 2);
            addFilter(true, true, item =>
                    (item.IdValue!.Contains($"_{randomStr}") == true && item.IdValue != null)
                 || (item.NodeType == nodeType || item.NodeType > nodeType)
                 || (
                        item.Override!.StartsWith("OVERRIDE", StringComparison.OrdinalIgnoreCase) == true
                     && !(item.Override.EndsWith("OVERRIDE"))
                 )
            );
            //      搜索不在数据库中的字段
            addFilter(false, false, item => item.ParentIgnore == null);

            #region 单个类型的测试
            //  Boolean类型：做一些not和！=组合
            addFilter(true, false, item => item.Bool);
            addFilter(true, false, item => item.Bool == true);
            addFilter(true, false, item => item.Bool == false);
            addFilter(true, false, item => !item.Bool);
            addFilter(true, false, item => !item.Bool == false);
            addFilter(true, false, item => !item.Bool == true);
            addFilter(true, false, item => !item.Bool != false);
            addFilter(true, false, item => !item.Bool != true);
            addFilter(true, false, item => item.Bool == boolNull);
            addFilter(true, false, item => item.Bool == boolNullFalse);
            addFilter(true, false, item => item.BoolNull == boolNull);
            addFilter(true, false, item => item.BoolNull == boolNullFalse);
            addFilter(true, false, item => item.BoolNull == null);
            addFilter(true, false, item => item.BoolNull == true);
            addFilter(true, false, item => item.BoolNull == false);
            addFilter(false, false, item => !item.BoolNull == null);
            addFilter(false, false, item => !item.BoolNull == false);
            addFilter(false, false, item => !item.BoolNull == true);
            addFilter(false, false, item => !item.BoolNull != null);
            addFilter(false, false, item => !item.BoolNull != false);
            addFilter(false, false, item => !item.BoolNull != true);
            addFilter(false, false, item => !item.BoolNull != true || item.BoolNull == false);
            //      in查询
            //exps.Add(item => new List<bool>() { true,false}.Contains(item.BoolNull)==true);
            addFilter(true, false, item => new List<bool>() { true, false }.Contains(item.Bool) == true);
            //exps.Add(item => new bool[] { true,false}.Contains(item.BoolNull)==true);
            addFilter(true, false, item => new bool[] { true, false }.Contains(item.Bool) == true);
            addFilter(true, false, item => new List<bool?>() { true, false, null }.Contains(item.BoolNull) == true);
            addFilter(true, false, item => new List<bool?>() { true, false, null }.Contains(item.Bool) == true);
            addFilter(true, false, item => new bool?[] { true, false, null }.Contains(item.BoolNull) == true);
            addFilter(true, false, item => new bool?[] { true, false, null }.Contains(item.Bool) == true);

            //  Int值：测试int包含，且Int字段数据库重命名了
            addFilter(true, false, item => item.Int == 1);
            addFilter(true, false, item => item.Int == intNull);
            addFilter(true, false, item => item.Int != intNull10);
            addFilter(true, false, item => item.Int >= intNull10);
            addFilter(true, false, item => item.IntNull == 1);
            addFilter(true, false, item => item.IntNull == null);
            addFilter(true, false, item => item.IntNull != intNull);
            addFilter(true, false, item => item.IntNull >= intNull10);
            //      in查询：附带一些静态、实例方法测试
            addFilter(true, false, item => new List<int?>() { 1, -1, null }.Contains(item.IntNull) == true);
            addFilter(true, false, item => new List<int?>() { 1, -1, null }.Contains(item.Int) == true);
            //exps.Add(item => new List<int>() { 1,-1}.Contains(item.IntNull)==true);
            addFilter(true, false, item => new List<int>() { 1, -1 }.Contains(item.Int) == true);
            addFilter(true, false, item => new int?[] { 1, -1, null }.Contains(item.IntNull) == true);
            addFilter(true, false, item => new int?[] { 1, -1, null }.Contains(item.Int) == true);
            //exps.Add(item => new int[] { 1, -1 }.Contains(item.IntNull) == true);
            addFilter(true, false, item => new int[] { 1, -1 }.Contains(item.Int) == true);
            addFilter(true, false, item => !GetIntArray().Contains(item.Int) == false);
            addFilter(true, false, item => GetIntArray().Contains(item.Int) == false);
            addFilter(true, false, item => GetIntArray().Contains(item.Int));
            addFilter(true, false, item => intArray.Contains(item.Int));
            addFilter(true, false, item => GetTmpIntList().Contains(item.Int));
            addFilter(true, false, item => GetIntList().Contains(item.Int));
            int?[] intNullArray = [1, 2, 3, 4, 5, 6, null, 9];
            addFilter(true, false, item => intNullArray.Contains(item.Int) == false);
            addFilter(true, false, item => intNullArray.Contains(item.IntNull) == false);
            addFilter(true, false, item => intNullArray.Contains(item.IntNull));
            addFilter(true, false, item => intArray.Contains(item.Int));
            //addFilter(true, false, item => intArray.Contains(item.IntNull));
            addFilter(true, false, item => GetTmpIntList().Contains(item.Int));
            addFilter(true, false, item => GetIntList().Contains(item.Int));

            //  枚举值：：内部针对比较做了特定适配的，lambda表达式会将 item.NodeType==nodeType 转换成 Convert(item.NodeType)==Convert(nodeType)
            addFilter(true, false, item => item.NodeType == nodeType);
            addFilter(true, false, item => item.NodeType != nodeType);
            addFilter(true, false, item => item.NodeType > nodeType);
            addFilter(true, false, item => item.NodeType >= nodeType);
            addFilter(true, false, item => item.NodeType < nodeType);
            addFilter(true, false, item => item.NodeType <= nodeType);
            addFilter(true, false, item => item.NodeTypeNull == null);
            addFilter(true, false, item => item.NodeTypeNull != null);
            addFilter(true, false, item => item.NodeTypeNull == nodeType);
            addFilter(true, false, item => item.NodeTypeNull != nodeType);
            addFilter(true, false, item => item.NodeTypeNull > nodeType);
            addFilter(true, false, item => item.NodeTypeNull >= nodeType);
            addFilter(true, false, item => item.NodeTypeNull < nodeType);
            addFilter(true, false, item => item.NodeTypeNull <= nodeType);
            addFilter(true, false, item => item.NodeType == ExpressionType.Add);
            addFilter(true, false, item => item.NodeTypeNull == ExpressionType.Add);
            //      in查询
            addFilter(true, false, item => new ExpressionType?[] { ExpressionType.Add, null, ExpressionType.GreaterThanOrEqual }.Contains(item.NodeType) == true);
            addFilter(true, false, item => new ExpressionType?[] { ExpressionType.Add, null, ExpressionType.GreaterThanOrEqual }.Contains(item.NodeTypeNull) == true);
            //addFilter(true, false, item => new ExpressionType[] { ExpressionType.Add, ExpressionType.GreaterThanOrEqual }.Contains(item.NodeTypeNull) == true);// 语法就过不去
            addFilter(true, false, item => new ExpressionType[] { ExpressionType.Add, ExpressionType.GreaterThanOrEqual }.Contains(item.NodeType) == true);
            addFilter(true, false, item => new List<ExpressionType?>() { ExpressionType.Add, null, ExpressionType.GreaterThanOrEqual }.Contains(item.NodeTypeNull) == true);
            addFilter(true, false, item => new List<ExpressionType?>() { ExpressionType.Add, null, ExpressionType.GreaterThanOrEqual }.Contains(item.NodeType) == true);
            //addFilter(true, false, item => new List<ExpressionType>() { ExpressionType.Add, ExpressionType.GreaterThanOrEqual }.Contains(item.NodeTypeNull) == true);// 语法就过不去
            addFilter(true, false, item => new List<ExpressionType>() { ExpressionType.Add, ExpressionType.GreaterThanOrEqual }.Contains(item.NodeType) == true);

            //  文本字符串：测试一些特殊字符的like
            addFilter(true, false, item => item.String == null);
            addFilter(true, false, item => item.String != "ddddd");
            //      测试字符串的包含：Contains、StartsWith、EndsWith
            addFilter(false, true, item => "xxxx".Contains(item.String));// 不支持
            addFilter(true, true, item => item.String.Contains(" _") == (d is string));
            addFilter(true, true, item => item.String.Contains("x_"));
            addFilter(true, true, item => item.String.Contains('%') == false);
            addFilter(true, true, item => item.String.Contains('_') == false);
            addFilter(true, true, item => item.String.Contains('[') == false);
            addFilter(true, true, item => item.String.Contains('^') == false);
            addFilter(true, true, item => item.String.Contains('&'.ToString()) == false);
            addFilter(true, false, item => new List<string>() { "1", "2" }.Contains<string>(item.String));
            addFilter(true, false, item => new ExpressionType[] { ExpressionType.Add, ExpressionType.Convert }.Contains(item.NodeType) == true);
            addFilter(true, false, item => new List<ExpressionType>() { ExpressionType.Add, ExpressionType.Convert }.Contains(item.NodeType) == true);
            addFilter(true, false, item => new bool?[] { true, false, null }.Contains(item.BoolNull) == true);
            addFilter(true, false, item => new List<string>() { tmpString, null, "sdddd" }.Contains(item.ParentName) == false);
            addFilter(true, false, item => new List<string>() { tmpString, null, "sdddd" }.Contains(item.ParentName) == true);
            addFilter(true, false, item => new string[] { tmpString, null, "sdddd" }.Contains(item.ParentName) == false);
            addFilter(true, false, item => new List<string>() { tmpString, null, "sdddd" }.Contains(item.ParentName) == false);
            addFilter(true, false, item => !GetStringArray().Contains(item.String));
            addFilter(true, true, item => item.IdValue.EndsWith($"_{randomStr}", StringComparison.OrdinalIgnoreCase));
            addFilter(true, true, item => !item.ParentName.StartsWith("1", StringComparison.OrdinalIgnoreCase));
            //addFilter(true, true, item => item.Override.Contains("OVERRIDE")); 这个暂时不要，不同数据库下，区分大小写不一致
            addFilter(true, true, item => item.Override.Contains("OVERRIDE", StringComparison.OrdinalIgnoreCase));
            addFilter(true, false, item => GetStringArray().Contains(item.String));
            addFilter(true, false, item => GetStringArray().Contains(item.String) == true);
            addFilter(true, false, item => !GetStringArray().Contains(item.String));
            addFilter(true, false, item => GetStringArray().Contains(item.String) == false);
            addFilter(true, false, item => GetStringList().Contains(item.String));
            addFilter(true, false, item => GetStringList().Contains(item.String) == true);
            addFilter(true, false, item => !GetStringList().Contains(item.String));
            addFilter(true, false, item => GetStringList().Contains(item.String) == false);
            addFilter(true, false, item => strArray.Contains(item.ParentName) == true);
            addFilter(true, false, item => strArray.Contains(item.String));
            addFilter(true, false, item => new List<string>() { tmpString, null, "sdddd" }.Contains(item.ParentName) == true);
            addFilter(true, false, item => new List<string>() { tmpString, null, "sdddd" }.Contains(item.ParentName) == false);
            #endregion

            #region 对linq的其他操作符做测试，主要针对不支持情况
            //  1、二元表达式测试
            //      二进制算数运算:+ - * / %  ^  强制求运算结果，失败则报错
            addFilter(true, false, item => item.Int > tmpInt);//有效，item.Int>100;
            addFilter(true, false, item => item.Int > tmpInt + 100);//有效，item.Int>200;
            addFilter(false, false, item => item.Int + 1 > 10);//无效，item.Int+1 为算数运算，mongo中构建太麻烦，不支持
                                                               //      按位运算  xor 和二进制运算保持一样。不知道咋写的  o(╥﹏╥)o
                                                               //      按位运算  & | 和&& || 保持一致。不知道咋写的  o(╥﹏╥)o
                                                               //      移位运算 << >> 和二进制运算保持一致
            addFilter(false, false, item => item.Int >> 1 > 10);// 无效，item.Int>>1 无法构建查询
            addFilter(true, false, item => item.Int > tmpInt >> 2);//   有效 tmInt>>2 能算出具体固定值
                                                                   //      条件布尔运算 && || 
            addFilter(false, false, item => 1 > 100);//   无效 两边都是恒true、false的条件，不构建数据库
            addFilter(false, false, item => item.Int > 10 && tmpInt < 1);//   无效 tmpInt<1 是一个恒成立条件，后期考虑tmpInt<1恒为true时直接剔除掉
            addFilter(false, false, item => item.Int > 10 || tmpInt < 1);//   无效 tmpInt<1 是一个恒成立条件，后期考虑tmpInt<1恒为false直接剔除掉
            addFilter(true, false, item => item.Int > 10 && item.IdValue == null);//   有效
                                                                                  //      比较运算  > < == != >= <=  一边是变量，一边时常量才符合需求，否则容易出现动态或者恒true、false
            addFilter(false, false, item => tmpInt + 10 > tmpInt + 10);//   无效；两边都是可计算成常量的数据；不能构建数据库条件，做出来就是恒true、false
            addFilter(false, false, item => item.IdValue == item.String);//   无效；两边都是变量，在数据库构建时比较麻烦，不支持
            addFilter(true, false, item => item.Int <= tmpInt);//   有效 
            addFilter(true, false, item => item.String == tmpString);//   有效 
                                                                     //      合并？？
            addFilter(false, false, item => (item.IdValue ?? "") == "11");//   无效 涉及到动态计算
            addFilter(true, false, item => item.IdValue == (tmpString ?? ""));//   有效 能计算出结果值，等效与 item.value== 固定值
                                                                              //      数组索引取值：计算出结果值作为比较条件，计算失败则不支持
            addFilter(false, false, item => strArray[item.Int] == null);//   无效；涉及动态计算，不支持
            addFilter(true, false, item => item.String == strArray[0]);//   有效；能计算出结果值，等效与 item.value== 固定值
                                                                       //  三元表达式：三元表达式，不能涉及变量字段转换，只能用于得到确切值做筛选
            addFilter(true, false, item => item.IdValue == (tmpString == null ? "不合格" : "及格"));//   有效
            addFilter(false, false, item => item.IdValue == null ? false : true);//   无效
            addFilter(false, false, item => item.IdValue == null ? true : false);//   无效
            addFilter(false, false, item => item.IdValue == (item.ParentName == "xxx" ? "合个" : "不合格"));//   无效
            addFilter(false, false, item => item.IdValue == (item.ParentName == "xxx" ? item.String : "不合格"));//   无效
            addFilter(false, false, item => item.IdValue == (item.ParentName == "xxx" ? item.String : "不合格"));//   无效
                                                                                                              //  MemberAccess  访问成员：变量值，临时实体、实例字段属性、静态变量属性
                                                                                                              //      实体class中再取属性值，静态、实例、变量
            addFilter(true, false, item => item.Int > 10);//   有效
            addFilter(false, false, item => newModel.Int < 1);//   无效 无效 newModel.Int<1 是一个恒成立条件
            addFilter(false, false, item => St_Model.Int > 10);//   无效
            addFilter(false, false, item => Ins_Model.Int > 10);//   无效
                                                                //      属性、变量、字段：静态、实例
            addFilter(false, false, item => tmpInt > 10);// 无效 计算出来为恒成立条件
            addFilter(false, false, item => St_Int > 10);// 无效 计算出来为恒成立条件
            addFilter(false, false, item => Ins_Int > 10);// 无效 计算出来为恒成立条件
                                                          //      通过方法返回值，然后取属性
            addFilter(false, false, item => GetModel().Int > 10);// 无效 计算出来为恒成立条件
            addFilter(false, false, item => GetTmpModel().Int > 10);// 无效 计算出来为恒成立条件
                                                                    //  MethodCall 方法调用
                                                                    //      前面是item.Name动态变量时
            addFilter(true, true, item => item.IdValue.Contains("_3"));
            addFilter(true, true, item => item.IdValue.Contains(tmpString));
            addFilter(true, true, item => item.IdValue.Contains(GetInt().ToString()));
            addFilter(true, true, item => item.IdValue.Contains(newModel.Int.ToString()));
            addFilter(true, true, item => item.IdValue.Contains("_".ToString(), StringComparison.OrdinalIgnoreCase));
            addFilter(true, true, item => item.IdValue.StartsWith("_t", StringComparison.OrdinalIgnoreCase));
            addFilter(true, true, item => item.IdValue.EndsWith("_", StringComparison.OrdinalIgnoreCase));
            addFilter(false, true, item => item.IdValue.Contains(null));
            addFilter(false, false, item => item.Int.ToString() == "_");// 不能对item.Int进行值计算，涉及数据库操作。后续可以考虑进行类型转换
            addFilter(false, false, item => item.String.Any(ch => ch == '1'));// 不支持此方法
            addFilter(false, true, item => item.IdValue.Contains(null));// 参数值不能为null
                                                                        //      前面是固定值时、或者方法调用时
            addFilter(true, false, item => new string?[3] { "", "", null }.Contains(item.String) == true);
            addFilter(true, false, item => new List<string>() { "1", "2" }.Contains(item.String) == false);
            addFilter(true, false, item => new List<string>() { "1", "2" }.Contains<string>(item.String));
            addFilter(true, false, item => new List<string>().Contains(item.String) == false);
            addFilter(true, false, item => ST_Strs.Contains(item.String) == false);
            addFilter(true, false, item => strArray.Contains(item.String) == false);
            addFilter(true, false, item => Ins_Strs.Contains(item.String) == false);
            addFilter(true, false, item => GetStringList().Contains(item.String) == true);
            addFilter(true, false, item => GetStringArray().Contains(item.String) == true);
            addFilter(false, false, item => GetInt() > 10);
            addFilter(false, false, item => GetTmpInt() > 10);//   不支持，常量恒等
            addFilter(false, false, item => new string?[3] { "", "", null }.Contains("_") == true);//   不支持，常量恒等
            addFilter(false, false, item => new List<string?>() { "1", "2" }.Contains(null) == false);// 不支持，常量恒等
            addFilter(false, false, item => new List<string>() { "1", "2" }.Contains<string>("_"));//  不支持，常量恒等
            addFilter(false, false, item => new List<string>().Contains(item.String.ToString()) == false);// 不支持，对item.String做了再次操作
            addFilter(false, false, item => ST_Strs.Contains("_X") == false);//   不支持，常量恒等  
            addFilter(false, false, item => strArray.Contains("_XX") == false);//   不支持，常量恒等
            addFilter(false, false, item => Ins_Strs.Contains("x") == false);//   不支持，常量恒等
            addFilter(false, false, item => GetStringList().Contains(item.String + "11") == true);// 不支持，对item.String做了再次操作
            addFilter(false, false, item => GetStringArray().Contains(Convert.ToString(item.Int)) == true);// 不支持，对item.Int做了再次操作
                                                                                                           //  委托调用给值：VisitInvocation
            addFilter(false, false, item => tmpIntFunc(100) > 10);//   结果都是固定值
            addFilter(false, false, item => tmpIntFunc(item.Int) > 100);// 无效，无法计算固定值
            addFilter(true, false, item => item.Int > tmpIntFunc(1000));
            //  VisitLambda：针对计算结果做校正
            addFilter(false, false, item => Contains("_"));// 这个做特例，需要在VisitLambda中处理
            addFilter(true, true, item => item.String.Contains(" _"));
            //  VisitDefault
            //addFilter(true,(item => item.String == default(String));// 这种是不会走VisitDefault的
            //  VisitTypeBinary
            addFilter(false, false, item => GetInt() is int);//  不支持，计算出来是常量
            addFilter(false, false, item => item.String is string);// 不支持，无法计算值
            addFilter(true, true, item => item.String.Contains("_x") == (d is string));
            //  VisitLambda：确保数据的标准化 
            addFilter(true, true, item => item.String.Contains($"xxx:{tmpString}"));// 转成 item.BsonFieldName.Contains($"xxx")==true
            addFilter(true, true, item => item.String.Contains($"xxx") == false);
            //  联合测试
            //      二元表达式+一元表达式
            addFilter(true, true, item => !item.String.Contains("_c") != true);// item=>item.BsonFieldName.Contains("_")!=false
            addFilter(true, true, item => !(item.String == null));// item.BsonFieldName!=null
            addFilter(true, true, item => !(item.String.Contains("_x") != true));// 这个组装出来就是 item.BsonFieldName.Contains("_")==true;
            addFilter(true, false, item => !(item.String == null && item.Int == 1));// item.BsonFieldName!=null || item.Double !=1
                                                                                    //  VisitUnary
            addFilter(true, true, item => !item.String.Contains("111"));
            addFilter(true, true, item => !(item.Int > 0 || (item.DateTime > DateTime.Now || item.String.Contains("1  "))));
            addFilter(true, true, item => !(item.String.Contains("_.") && item.String.Contains('_') == false));
            addFilter(true, false, item => !(item.IdValue == null || item.String == null));
            #endregion

            return filters;
        }
        #endregion

        #region 编译错误列表；这里面测试代码较多，先禁用掉
#pragma warning restore CS0169
#pragma warning restore CS0183
#pragma warning restore CS0414
#pragma warning restore CS0649
#pragma warning restore CS8602
#pragma warning restore CS8604
#pragma warning restore CS8625
#pragma warning restore CA1822
#pragma warning restore CA1825
#pragma warning restore CA1829
#pragma warning restore CA2211
#pragma warning restore IDE0044
#pragma warning restore IDE0051
#pragma warning restore IDE0052
#pragma warning restore IDE0054
#pragma warning restore IDE0059
#pragma warning restore IDE0060
#pragma warning restore IDE0071
#pragma warning restore IDE0075
#pragma warning restore IDE0090
        #endregion
    }
}
