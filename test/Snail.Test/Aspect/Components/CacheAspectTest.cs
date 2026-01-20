using Snail.Aspect.Distribution.Attributes;
using Snail.Aspect.Distribution.Enumerations;
using Snail.Test.Aspect.DataModels;
using Snail.Utilities.Common.Interfaces;

namespace Snail.Test.Aspect.Components
{
    /// <summary>
    /// 
    /// </summary>
    [CacheAspect(Workspace = "Test", Code = "Default")]
    // [CacheAspect(Workspace = "Test", Code = "Default", Analyzer = "xxx")]
    [Component]
    public abstract class CacheAspectTest
    {
        #region 抽象类型测试
        [CacheMethod<TestCache>(Action = CacheActionType.Load, MasterKey = "123")]
        public virtual ValueTask<TestCache> GenericAttributeTest([CacheKey] string id)
        {
            return ValueTask.FromResult<TestCache>(null!);
        }
        #endregion

        #region Object-List
        [CacheMethod(Type = CacheType.ObjectCache, Action = CacheActionType.Load, DataType = typeof(TestCache), MasterKey = "12312")]
        public abstract Task<List<TestCache>> LoadListAbstract([CacheKey] params List<string> keys);
        [CacheMethod(Type = CacheType.ObjectCache, Action = CacheActionType.Load, DataType = typeof(TestCache), MasterKey = "12312")]
        public abstract Task<List<TestCache>> LoadListAbstract([CacheKey] string key, string bugData);

        [CacheMethod(Type = CacheType.ObjectCache, Action = CacheActionType.LoadSave, DataType = typeof(TestCache), MasterKey = "12312")]
        [Expire(Seconds = 100)]
        public virtual async Task<List<TestCache>> LoadList([CacheKey] params List<string> keys)
        {
            await Task.Yield();
            return keys.Select(id => new TestCache() { Id = id }).ToList();
        }
        [CacheMethod(Type = CacheType.ObjectCache, Action = CacheActionType.Load, DataType = typeof(TestCache), MasterKey = "12312")]
        public virtual async Task<List<TestCache>> LoadList([CacheKey] string key)
        {
            await Task.Yield();
            return new string[] { key }.Select(id => new TestCache() { Id = id }).ToList();
        }
        [CacheMethod(Type = CacheType.ObjectCache, Action = CacheActionType.Delete, DataType = typeof(TestCache), MasterKey = "12312")]
        public abstract Task<List<TestCache>> DeleteListAbstract([CacheKey] params List<string> keys);
        [CacheMethod(Type = CacheType.ObjectCache, Action = CacheActionType.Delete, DataType = typeof(TestCache), MasterKey = "12312")]
        public async virtual Task<List<TestCache>> DeleteList([CacheKey] params List<string> keys)
        {
            await Task.Yield();
            return keys.Select(id => new TestCache() { Id = id }).ToList();
        }
        [CacheMethod(Type = CacheType.ObjectCache, Action = CacheActionType.Save, DataType = typeof(TestCache), MasterKey = "12312")]
        public virtual async Task<List<TestCache>> SaveList([CacheKey] params List<string> keys)
        {
            await Task.Yield();
            return keys.Select(id => new TestCache() { Id = id, Name = "SaveList" }).ToList();
        }
        #endregion

        #region hash-list
        [CacheMethod(Type = CacheType.HashCache, Action = CacheActionType.Load, DataType = typeof(TestCache), MasterKey = "12312")]
        public abstract Task<List<TestCache>> LoadHashListAbstract([CacheKey] params List<string> keys);
        [CacheMethod(Type = CacheType.HashCache, Action = CacheActionType.Load, DataType = typeof(TestCache), MasterKey = "12312")]
        public abstract Task<List<TestCache>> LoadHashListAbstract([CacheKey] string key);
        [CacheMethod(Type = CacheType.HashCache, Action = CacheActionType.LoadSave, DataType = typeof(TestCache), MasterKey = "12312")]
        public virtual async Task<List<TestCache>> LoadHashList([CacheKey] params List<string> keys)
        {
            await Task.Yield();
            return keys.Select(id => new TestCache() { Id = id }).ToList();
        }
        [CacheMethod(Type = CacheType.HashCache, Action = CacheActionType.Load, DataType = typeof(TestCache), MasterKey = "12312")]
        public virtual async Task<List<TestCache>> LoadHashList([CacheKey] string key)
        {
            await Task.Yield();
            return new string[] { key }.Select(id => new TestCache() { Id = id }).ToList();
        }
        [CacheMethod(Type = CacheType.HashCache, Action = CacheActionType.Delete, DataType = typeof(TestCache), MasterKey = "12312")]
        public abstract Task<List<TestCache>> DeleteHashListAbstract([CacheKey] params List<string> keys);
        [CacheMethod(Type = CacheType.HashCache, Action = CacheActionType.Delete, DataType = typeof(TestCache), MasterKey = "12312")]
        public async virtual Task<List<TestCache>> DeleteHashList([CacheKey] params List<string> keys)
        {
            await Task.Yield();
            return keys.Select(id => new TestCache() { Id = id }).ToList();
        }
        [CacheMethod(Type = CacheType.HashCache, Action = CacheActionType.Save, DataType = typeof(TestCache), MasterKey = "12312")]
        public virtual async Task<List<TestCache>> SaveHashList([CacheKey] params List<string> keys)
        {
            await Task.Yield();
            return keys.Select(id => new TestCache() { Id = id, Name = "SaveList" }).ToList();
        }
        #endregion

        #region 字典：只有object缓存测试即可，涉及类型转换的代码都是公共的
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Load)]
        public abstract Task<Dictionary<string, TestCache>> LoadDictAbstract([CacheKey] string key);
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Load)]
        public abstract Task<Dictionary<string, TestCache>> LoadDictAbstract([CacheKey] IList<string> key);
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Load)]
        public async virtual Task<Dictionary<string, TestCache>> LoadDict([CacheKey] string key)
        {
            await Task.Yield(); ;
            return new Dictionary<string, TestCache>() { { key, new TestCache() { Id = key } } };
        }
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Load)]
        public async virtual Task<Dictionary<string, TestCache>> LoadDict([CacheKey] List<string> key)
        {
            await Task.Yield(); ;
            return key.ToDictionary(key => key, key => new TestCache() { Id = key });
        }
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Delete)]
        public abstract Task<Dictionary<string, TestCache>> DeleteDictAbstract([CacheKey] string key);
        #endregion

        #region 单个对象：只有object缓存测试即可，涉及类型转换的代码都是公共的
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Load)]
        public abstract Task<TestCache> LoadAbstract([CacheKey] string key);
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Load)]
        public abstract Task<TestCache> LoadAbstract([CacheKey] string[] key);
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.LoadSave)]
        public async virtual Task<TestCache> Load([CacheKey] string key)
        {
            await Task.Yield(); ;
            return new TestCache() { Id = key };
        }
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Load)]
        public async virtual Task<TestCache> Load([CacheKey] string[] key)
        {
            await Task.Yield(); ;
            return new TestCache() { Id = key.First() };
        }
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Delete)]
        public abstract Task<TestCache> DeleteAbstract([CacheKey] string key);
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Save)]
        public async virtual Task<TestCache> Save([CacheKey] string key)
        {
            await Task.Yield(); ;
            return new TestCache() { Id = key, Name = "Save" };
        }
        #endregion

        #region 数组：只有object缓存测试即可，涉及类型转换的代码都是公共的

        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Load)]
        public abstract Task<TestCache[]> LoadArrayAbstract([CacheKey] string[] keys);

        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Load)]
        public abstract Task<TestCache[]> LoadArrayAbstract([CacheKey] string keys);

        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.LoadSave)]
        public virtual async Task<TestCache[]> LoadArray([CacheKey] string[] keys)
        {
            await Task.Yield();
            return keys.Select(key => new TestCache() { Id = key }).ToArray();
        }
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Load)]
        public virtual async Task<TestCache[]> LoadArray([CacheKey] string keys)
        {
            await Task.Yield();
            return new string[] { keys }.Select(key => new TestCache() { Id = key }).ToArray();
        }

        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Delete)]
        public abstract Task<TestCache[]> DeleteArrayAbstract([CacheKey] string[] keys);
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Delete)]
        public virtual async Task<TestCache[]> DeleteArray([CacheKey] string[] keys)
        {
            await Task.Yield();
            return keys.Select(key => new TestCache() { Id = key }).ToArray();
        }
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Save)]
        public virtual async Task<TestCache[]> SaveArray([CacheKey] string[] keys)
        {
            await Task.Yield();
            return keys.Select(key => new TestCache() { Id = key, Name = "SaveArray" }).ToArray();
        }
        #endregion

        #region 测试载荷数据对象-单个对象：只有object缓存测试即可，涉及类型转换的代码都是公共的
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Load)]
        public abstract Task<TestPayload2> LoadPayloadAbstract([CacheKey] string key);
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Load)]
        public abstract Task<TestPayload<TestCache>> LoadPayloadAbstract([CacheKey] string[] key, string payload);
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.LoadSave)]
        public virtual async Task<TestPayload<TestCache>> LoadPayload([CacheKey] string key)
        {
            await Task.Yield();
            var bag = new TestPayload<TestCache>();
            bag.Payload = new TestCache() { Id = key };
            return bag;
        }
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Delete)]
        public abstract Task DeletePayloadAbstract([CacheKey] string key);
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Delete)]
        public abstract Task<TestPayload<TestCache>> DeletePayloadAbstract2([CacheKey] string key);
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Delete)]
        public async virtual Task<TestPayload<TestCache>> DeletePayloadAbstract3([CacheKey] string key)
        {
            await Task.Yield();
            var bag = new TestPayload<TestCache>();
            bag.Payload = new TestCache() { Id = key, Name = "SavePayload" };
            return bag;
        }
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Save)]
        public virtual async Task<TestPayload<TestCache>> SavePayload(string key)
        {
            await Task.Yield();
            var bag = new TestPayload<TestCache>();
            bag.Payload = new TestCache() { Id = key, Name = "SavePayload" };
            return bag;
        }
        #endregion

        #region 载荷数据对象-list：：只有object缓存测试即可，涉及类型转换的代码都是公共的
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Load)]
        public abstract Task<TestPayload<List<TestCache>>> LoadPayloadListAbstract([CacheKey] params List<string> keys);
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Load)]
        public abstract Task<TestPayload<List<TestCache>>> LoadPayloadListAbstract([CacheKey] string key);
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.LoadSave)]
        public virtual async Task<TestPayload<ListChild2<TestCache>>> LoadPayloadList([CacheKey] params List<string> keys)
        {
            await Task.Yield();
            ListChild2<TestCache> lsg = new ListChild2<TestCache>();
            foreach (var key in keys)
            {
                lsg.Add(new TestCache() { Id = key });
            }
            var bags = new TestPayload<ListChild2<TestCache>>();
            bags.Payload = lsg;
            return bags;
        }
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Delete)]
        public abstract Task<TestPayload<List<TestCache>>> DeletePayloadListAbstract([CacheKey] params List<string> keys);
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Delete)]
        public virtual async Task<TestPayload<List<TestCache>>> DeletePayloadList([CacheKey] params List<string> keys)
        {
            await Task.Yield();
            var bags = new TestPayload<List<TestCache>>();
            bags.Payload = keys.Select(key => new TestCache() { Id = key }).ToList();
            return bags;
        }
        [CacheMethod(DataType = typeof(TestCache), Action = CacheActionType.Save)]
        public virtual async Task<TestPayload<List<TestCache>>> SavePayloadList([CacheKey] params List<string> keys)
        {
            await Task.Yield();
            var bags = new TestPayload<List<TestCache>>();
            bags.Payload = keys.Select(key => new TestCache() { Id = key, Name = "SavePayloadList" }).ToList();
            return bags;
        }
        #endregion

        #region 载荷数据对象-Array、Dictionary逻辑，不用测试
        //  整理逻辑和List重合，Array、Dictionary自身初始化等逻辑，和LoadArray、LoadDictionary等一致
        #endregion

        #region 构造方法
        public CacheAspectTest(string metho)
        {

        }
        #endregion


    }

    #region 内部类型
    public interface ITestPayload<T> : IPayload<T>
    {

    }
    public class TestPayload<T> : ITestPayload<T>
    {
        public T? Payload { get => field; set => field = value; }
    }

    public class TestPayload2 : TestPayload<List<TestCache>>
    {

    }

    public class TestCacheChild1 : TestCache
    {

    }
    public class TestCacheChild12 : TestCacheChild1
    {

    }
    #endregion


    public sealed class ListChild2<T> : List<T>
    {

    }
}
