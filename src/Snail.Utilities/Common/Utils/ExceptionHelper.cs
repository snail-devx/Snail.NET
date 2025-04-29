namespace Snail.Utilities.Common.Utils
{
    /// <summary>
    /// <see cref="Exception"/>助手类
    /// </summary>
    public static class ExceptionHelper
    {
        #region Throw 条件
        /// <summary>
        /// <paramref name="ex"/>非空时，抛出异常
        /// </summary>
        /// <param name="ex"></param>
        public static void TryThrow(Exception? ex)
        {
            if (ex != null)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 条件成立时，抛出异常
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="ex"></param>
        public static void ThrowIf(bool condition, Exception ex)
        {
            if (condition == true)
            {
                throw ex;
            }
        }
        #endregion
    }
}
