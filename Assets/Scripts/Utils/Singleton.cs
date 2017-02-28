

namespace Utils
{
    public class Singleton<T> where T : new()
    {
        #region Data Members
        // ---------------------------- //
        // Const data Members
        // ---------------------------- //


        // ---------------------------- //
        // Public data Members
        // ---------------------------- //
        public static T instance
        {
            get
            {
                if (sInstance == null) {
                    sInstance = new T();
                }
                return sInstance;
            }
        }


        // ---------------------------- //
        // Private data Members
        // ---------------------------- //
        private static T sInstance;
        #endregion
    }
}
